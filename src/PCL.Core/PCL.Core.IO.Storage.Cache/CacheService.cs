using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PCL.Core.App;
using PCL.Core.IO.Storage.Cache.Model;

namespace PCL.Core.IO.Storage.Cache;

public class CacheService : ICacheService, IAsyncDisposable
{
	private readonly CacheOptions _options;

	private readonly SchemaManager _schemaManager;

	private readonly SqliteCacheStorage _db;

	private readonly FileCacheStorage _files;

	private readonly CacheEvictionService _eviction;

	private long _hits;

	private long _misses;

	private bool _disposed;

	private static readonly JsonSerializerOptions _JsonOpts = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public event EventHandler<CacheEntryEvictedEventArgs>? EntryEvicted;

	public CacheService()
	{
		_options = new CacheOptions
		{
			DatabasePath = Path.Combine(Paths.Temp, "Cache", "pcl_ce_cache.db"),
			FileCacheRoot = Path.Combine(Paths.Temp, "Cache", "files")
		};
		_schemaManager = new SchemaManager("Data Source=" + _options.DatabasePath);
		_db = new SqliteCacheStorage(_options.DatabasePath);
		_files = new FileCacheStorage(_options.FileCacheRoot, _options.EnableCompression);
		_eviction = new CacheEvictionService(_db, _files, _options);
	}

	[Obsolete("This constructor is for testing purposes only.")]
	public CacheService(CacheOptions options)
	{
		_options = options;
		_schemaManager = new SchemaManager("Data Source=" + _options.DatabasePath);
		_db = new SqliteCacheStorage(_options.DatabasePath);
		_files = new FileCacheStorage(_options.FileCacheRoot, _options.EnableCompression);
		_eviction = new CacheEvictionService(_db, _files, _options);
	}

	internal async Task InitializeAsync()
	{
		Directory.CreateDirectory(_options.FileCacheRoot);
		Directory.CreateDirectory(Path.GetDirectoryName(_options.DatabasePath));
		await _schemaManager.EnsureCurrentSchemaAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _db.CleanupStartupAsync().ConfigureAwait(continueOnCapturedContext: false);
		_eviction.Start();
	}

	public async Task SetAsync<T>(string key, T value, CachePolicy? policy = null, CancellationToken ct = default(CancellationToken))
	{
		_ThrowIfNotReady();
		if ((object)policy == null)
		{
			policy = CachePolicy.Default;
		}
		(byte[], bool) tuple = _Serialize(value);
		byte[] bytes = tuple.Item1;
		bool item = tuple.Item2;
		CacheEntry cacheEntry = new CacheEntry
		{
			CacheKey = key,
			ContentType = typeof(T).Name,
			ContentVersion = policy.ContentVersion,
			DataSize = bytes.Length,
			Tags = (policy.Tags ?? string.Empty),
			GroupName = (policy.Group ?? string.Empty),
			Priority = (int)policy.Priority,
			ExpiresAt = _ComputeExpiry(policy)
		};
		if (item && bytes.Length <= _options.MaxInlineSize)
		{
			cacheEntry = cacheEntry with
			{
				EntryType = EntryType.Inline,
				Data = bytes,
				ContentHash = _ComputeSha256(bytes)
			};
		}
		else
		{
			using MemoryStream ms = new MemoryStream(bytes);
			CacheEntry cacheEntry2 = cacheEntry with
			{
				EntryType = EntryType.FileRef
			};
			CacheEntry cacheEntry3 = cacheEntry2;
			cacheEntry3.FileHash = await _files.StoreAsync(ms).ConfigureAwait(continueOnCapturedContext: false);
			cacheEntry2.ContentHash = _ComputeSha256(bytes);
			cacheEntry = cacheEntry2;
		}
		await _db.UpsertAsync(cacheEntry, ct).ConfigureAwait(continueOnCapturedContext: false);
		_eviction.CheckThreshold();
	}

	public async Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken ct = default(CancellationToken))
	{
		_ThrowIfNotReady();
		CacheEntry entry = await _db.LookupAsync(key, ct).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)entry == null)
		{
			Interlocked.Increment(ref _misses);
			return CacheResult<T>.Miss;
		}
		if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
		{
			await _db.DeleteAsync(key, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (entry.FileHash != null)
			{
				await _files.ReleaseAsync(entry.FileHash).ConfigureAwait(continueOnCapturedContext: false);
			}
			Interlocked.Increment(ref _misses);
			return CacheResult<T>.Miss;
		}
		Interlocked.Increment(ref _hits);
		byte[] data;
		if ((object)entry != null && entry.EntryType == EntryType.FileRef && entry.FileHash != null)
		{
			await using Stream stream = _files.Retrieve(entry.FileHash);
			if (stream == null)
			{
				await _db.DeleteAsync(key, ct).ConfigureAwait(continueOnCapturedContext: false);
				Interlocked.Increment(ref _misses);
				return CacheResult<T>.Miss;
			}
			using MemoryStream ms = new MemoryStream();
			await stream.CopyToAsync(ms, ct).ConfigureAwait(continueOnCapturedContext: false);
			data = ms.ToArray();
		}
		else
		{
			data = entry.Data ?? Array.Empty<byte>();
		}
		T value = _Deserialize<T>(data);
		await _db.TouchAsync(key, ct).ConfigureAwait(continueOnCapturedContext: false);
		return CacheResult<T>.Hit(value, entry.CachedAt);
	}

	public async Task<bool> ExistsAsync(string key)
	{
		_ThrowIfNotReady();
		CacheEntry cacheEntry = await _db.LookupAsync(key, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)cacheEntry == null)
		{
			return false;
		}
		return !cacheEntry.ExpiresAt.HasValue || cacheEntry.ExpiresAt.Value >= DateTime.UtcNow;
	}

	public async Task<bool> DeleteAsync(string key)
	{
		_ThrowIfNotReady();
		CacheEntry cacheEntry = await _db.LookupAsync(key, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)cacheEntry == null)
		{
			return false;
		}
		if (cacheEntry.FileHash != null)
		{
			await _files.ReleaseAsync(cacheEntry.FileHash).ConfigureAwait(continueOnCapturedContext: false);
		}
		return await _db.DeleteAsync(key, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<string?> GetCachedFilePathAsync(string key)
	{
		CacheEntry cacheEntry = await _db.LookupAsync(key, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		if (cacheEntry?.FileHash == null)
		{
			return null;
		}
		return _files.GetFilePath(cacheEntry.FileHash);
	}

	public async Task<string> CacheFileAsync(string key, Stream source, CachePolicy? policy = null, CancellationToken ct = default(CancellationToken))
	{
		_ThrowIfNotReady();
		if ((object)policy == null)
		{
			policy = CachePolicy.Default;
		}
		string hash = await _files.StoreAsync(source).ConfigureAwait(continueOnCapturedContext: false);
		CacheEntry entry = new CacheEntry
		{
			CacheKey = key,
			EntryType = EntryType.FileRef,
			ContentType = "application/octet-stream",
			DataSize = source.Length,
			FileHash = hash,
			ContentHash = hash,
			Tags = (policy.Tags ?? ""),
			GroupName = (policy.Group ?? ""),
			Priority = (int)policy.Priority,
			ExpiresAt = _ComputeExpiry(policy)
		};
		await _db.UpsertAsync(entry, ct).ConfigureAwait(continueOnCapturedContext: false);
		_eviction.CheckThreshold();
		return hash;
	}

	public async Task<int> DeleteByGroupAsync(string groupName)
	{
		_ThrowIfNotReady();
		foreach (string item in await _db.GetFileHashesByGroupAsync(groupName, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false))
		{
			if (item != null)
			{
				await _files.ReleaseAsync(item).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		return await _db.DeleteByGroupAsync(groupName, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task<int> DeleteByTagAsync(string tag)
	{
		_ThrowIfNotReady();
		return _db.DeleteByTagAsync(tag, CancellationToken.None);
	}

	public async Task<int> DeleteExpiredAsync()
	{
		_ThrowIfNotReady();
		int result = await _db.DeleteExpiredAsync(DateTime.UtcNow, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		_eviction.CheckThreshold();
		return result;
	}

	public async Task<CacheStats> GetStatsAsync()
	{
		CacheStatsRow cacheStatsRow = await _db.GetStatsAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		return new CacheStats
		{
			TotalEntries = cacheStatsRow.TotalEntries,
			TotalSizeBytes = cacheStatsRow.TotalSizeBytes,
			ExpiredEntries = cacheStatsRow.ExpiredEntries,
			InlineEntries = cacheStatsRow.InlineEntries,
			FileEntries = cacheStatsRow.FileEntries,
			CacheHits = Interlocked.Read(in _hits),
			CacheMisses = Interlocked.Read(in _misses)
		};
	}

	public async Task ClearAsync()
	{
		_ThrowIfNotReady();
		foreach (string item in await _db.GetAllFileHashesAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false))
		{
			if (item != null)
			{
				await _files.ReleaseAsync(item).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		SqliteConnection conn = new SqliteConnection("Data Source=" + _options.DatabasePath);
		try
		{
			await ((DbConnection)(object)conn).OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
			SqliteCommand cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "DELETE FROM cache_entries;\nDELETE FROM instance_cache;\nDELETE FROM component_cache;\nVACUUM;";
				await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
	}

	public Task CompactAsync()
	{
		_ThrowIfNotReady();
		return _db.CompactAsync(CancellationToken.None);
	}

	internal void OnEntryEvicted(CacheEntryEvictedEventArgs args)
	{
		EntryEvicted?.Invoke(this, args);
	}

	private void _ThrowIfNotReady()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	private static DateTime? _ComputeExpiry(CachePolicy policy)
	{
		if (!policy.AbsoluteExpiration.HasValue && !policy.SlidingExpiration.HasValue)
		{
			return null;
		}
		DateTime utcNow;
		TimeSpan? absoluteExpiration;
		if (policy.AbsoluteExpiration.HasValue)
		{
			utcNow = DateTime.UtcNow;
			absoluteExpiration = policy.AbsoluteExpiration;
			return utcNow + absoluteExpiration;
		}
		utcNow = DateTime.UtcNow;
		absoluteExpiration = policy.SlidingExpiration;
		return utcNow + absoluteExpiration;
	}

	private static (byte[] bytes, bool isSmall) _Serialize<T>(T value)
	{
		if (!(value is byte[] array))
		{
			if (value is string text)
			{
				return (bytes: Encoding.UTF8.GetBytes(text), isSmall: text.Length <= 262144);
			}
			return (bytes: JsonSerializer.SerializeToUtf8Bytes(value, _JsonOpts), isSmall: true);
		}
		return (bytes: array, isSmall: array.Length <= 262144);
	}

	private static T _Deserialize<T>(byte[] data)
	{
		if (typeof(T) == typeof(byte[]))
		{
			return (T)(object)data;
		}
		if (typeof(T) == typeof(string))
		{
			return (T)(object)Encoding.UTF8.GetString(data);
		}
		return JsonSerializer.Deserialize<T>(data, _JsonOpts);
	}

	private static string _ComputeSha256(byte[] data)
	{
		return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
	}

	public Task UpsertInstanceAsync(InstanceCacheRow row, CancellationToken ct = default(CancellationToken))
	{
		return _db.UpsertInstanceAsync(row, ct);
	}

	public Task<InstanceCacheRow?> LookupInstanceAsync(string instancePath, CancellationToken ct = default(CancellationToken))
	{
		return _db.LookupInstanceAsync(instancePath, ct);
	}

	public Task DeleteInstanceAsync(string instancePath, CancellationToken ct = default(CancellationToken))
	{
		return _db.DeleteInstanceAsync(instancePath, ct);
	}

	public Task UpsertComponentAsync(ComponentCacheRow row, CancellationToken ct = default(CancellationToken))
	{
		return _db.UpsertComponentAsync(row, ct);
	}

	public Task<List<ComponentCacheRow>> GetComponentsByInstanceAsync(string instancePath, string compType, CancellationToken ct = default(CancellationToken))
	{
		return _db.GetComponentsByInstanceAsync(instancePath, compType, ct);
	}

	public Task<ComponentCacheRow?> GetComponentAsync(string instancePath, string compType, string fileName, CancellationToken ct = default(CancellationToken))
	{
		return _db.GetComponentAsync(instancePath, compType, fileName, ct);
	}

	public Task DeleteComponentsByInstanceAsync(string instancePath, CancellationToken ct = default(CancellationToken))
	{
		return _db.DeleteComponentsByInstanceAsync(instancePath, ct);
	}

	public Task<string?> GetComponentScanHashAsync(string instancePath, string compType, CancellationToken ct = default(CancellationToken))
	{
		return _db.GetComponentScanHashAsync(instancePath, compType, ct);
	}

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_eviction.Stop();
		try
		{
			SqliteConnection conn = new SqliteConnection("Data Source=" + _options.DatabasePath);
			try
			{
				await ((DbConnection)(object)conn).OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
				SqliteCommand cmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)cmd).CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
		}
		catch
		{
		}
		await CastAndDispose(_db).ConfigureAwait(continueOnCapturedContext: false);
		await CastAndDispose(_files).ConfigureAwait(continueOnCapturedContext: false);
		static async ValueTask CastAndDispose(IDisposable resource)
		{
			if (resource is IAsyncDisposable asyncDisposable)
			{
				await asyncDisposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				resource.Dispose();
			}
		}
	}
}
