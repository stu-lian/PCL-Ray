using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.Hash;

public class HashCache
{
	private sealed class CacheEntry
	{
		public string FilePath { get; init; } = "";

		public long FileSize { get; init; }

		public string LastWriteTime { get; init; } = "";

		public string? MD5 { get; init; }

		public string? SHA1 { get; init; }

		public string? SHA256 { get; init; }

		public string? SHA512 { get; init; }

		public string? MurmurHash2 { get; init; }
	}

	private readonly string _dbPath;

	private static readonly ConcurrentDictionary<string, Task<string>> _FileHashComputePending = new ConcurrentDictionary<string, Task<string>>();

	public HashCache(string dbPath)
	{
		_dbPath = dbPath ?? throw new ArgumentNullException("dbPath");
		string directoryName = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
		if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		_Initialize();
	}

	private void _Initialize()
	{
		SqliteConnection val = _CreateConnection();
		try
		{
			SqliteCommand val2 = val.CreateCommand();
			try
			{
				((DbCommand)(object)val2).CommandText = "CREATE TABLE IF NOT EXISTS HashCache (\n    FilePath TEXT NOT NULL PRIMARY KEY,\n    FileSize INTEGER NOT NULL,\n    LastWriteTime TEXT NOT NULL,\n    MD5 TEXT NULL,\n    SHA1 TEXT NULL,\n    SHA256 TEXT NULL,\n    SHA512 TEXT NULL,\n    MurmurHash2 TEXT NULL\n)";
				((DbCommand)(object)val2).ExecuteNonQuery();
				SqliteCommand val3 = val.CreateCommand();
				try
				{
					((DbCommand)(object)val3).CommandText = "PRAGMA journal_mode=WAL";
					((DbCommand)(object)val3).ExecuteNonQuery();
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private SqliteConnection _CreateConnection()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected Obj, but got Unknown
		//IL_0021: Expected Obj, but got Unknown
		SqliteConnection val = new SqliteConnection("Data Source=" + _dbPath + ";Pooling=True");
		((DbConnection)val).Open();
		return val;
	}

	public Task<string> GetMD5Async(string filePath)
	{
		return _GetHashWithPending(filePath, MD5Provider.Instance, "MD5");
	}

	public Task<string> GetSHA1Async(string filePath)
	{
		return _GetHashWithPending(filePath, SHA1Provider.Instance, "SHA1");
	}

	public Task<string> GetSHA256Async(string filePath)
	{
		return _GetHashWithPending(filePath, SHA256Provider.Instance, "SHA256");
	}

	public Task<string> GetSHA512Async(string filePath)
	{
		return _GetHashWithPending(filePath, SHA512Provider.Instance, "SHA512");
	}

	public Task<string> GetMurmurHash2Async(string filePath)
	{
		return _GetHashWithPending(filePath, MurmurHash2Provider.Instance, "MurmurHash2");
	}

	public async Task<string> GetHashAsync(string filePath, IHashProvider provider)
	{
		string text;
		if (!(provider is MD5Provider))
		{
			if (!(provider is SHA1Provider))
			{
				if (!(provider is SHA256Provider))
				{
					if (!(provider is SHA512Provider))
					{
						if (!(provider is MurmurHash2Provider))
						{
							throw new ArgumentException("不支持的哈希算法: " + provider.GetType().Name);
						}
						text = "MurmurHash2";
					}
					else
					{
						text = "SHA512";
					}
				}
				else
				{
					text = "SHA256";
				}
			}
			else
			{
				text = "SHA1";
			}
		}
		else
		{
			text = "MD5";
		}
		string text2 = text;
		_ = filePath + ":" + text2;
		return await _GetHashWithPending(filePath, provider, text2).ConfigureAwait(continueOnCapturedContext: false);
	}

	private Task<string> _GetHashWithPending(string filePath, IHashProvider provider, string algoName)
	{
		string computeKey = filePath + ":" + algoName;
		return _FileHashComputePending.GetOrAdd(computeKey, (string key) =>
		{
			Task<string> task = _GetHashAsync(filePath, provider, algoName);
			task.ContinueWith((Task<string> t) =>
			{
				_FileHashComputePending.TryRemove(computeKey, out Task<string> _);
			}, TaskContinuationOptions.ExecuteSynchronously);
			return task;
		});
	}

	private async Task<string> _GetHashAsync(string filePath, IHashProvider provider, string algoName)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			throw new ArgumentNullException("filePath");
		}
		string fullPath = Path.GetFullPath(filePath);
		try
		{
			FileInfo fileInfo = new FileInfo(fullPath);
			long fileSize = fileInfo.Length;
			string lastWrite = fileInfo.LastWriteTimeUtc.ToString("O");
			CacheEntry cacheEntry = await _FindCacheEntryAsync(fullPath).ConfigureAwait(continueOnCapturedContext: false);
			if (cacheEntry != null)
			{
				if (cacheEntry.FileSize == fileSize && cacheEntry.LastWriteTime == lastWrite)
				{
					string text = _GetHashFromEntry(cacheEntry, algoName);
					if (text != null)
					{
						return text;
					}
					string computedHash = await _ComputeHashAsync(fullPath, provider).ConfigureAwait(continueOnCapturedContext: false);
					await _InsertOrUpdateHashAsync(fullPath, fileSize, lastWrite, algoName, computedHash).ConfigureAwait(continueOnCapturedContext: false);
					return computedHash;
				}
				await _DeleteCacheEntryAsync(fullPath).ConfigureAwait(continueOnCapturedContext: false);
			}
			string computed = await _ComputeHashAsync(fullPath, provider).ConfigureAwait(continueOnCapturedContext: false);
			await _InsertOrUpdateHashAsync(fullPath, fileSize, lastWrite, algoName, computed).ConfigureAwait(continueOnCapturedContext: false);
			return computed;
		}
		catch (FileNotFoundException)
		{
			await _DeleteCacheEntryAsync(fullPath).ConfigureAwait(continueOnCapturedContext: false);
			throw;
		}
	}

	private static async Task<string> _ComputeHashAsync(string fullPath, IHashProvider provider)
	{
		using FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		return (await provider.ComputeHashAsync(fs).ConfigureAwait(continueOnCapturedContext: false)).ToHexString();
	}

	private async Task<CacheEntry?> _FindCacheEntryAsync(string fullPath)
	{
		SqliteConnection conn = _CreateConnection();
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT FilePath, FileSize, LastWriteTime, MD5, SHA1, SHA256, SHA512, MurmurHash2 FROM HashCache WHERE FilePath = @FilePath";
				cmd.Parameters.AddWithValue("@FilePath", (object)fullPath);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(continueOnCapturedContext: false);
				try
				{
					if (!(await ((DbDataReader)(object)reader).ReadAsync().ConfigureAwait(continueOnCapturedContext: false)))
					{
						return null;
					}
					return new CacheEntry
					{
						FilePath = ((DbDataReader)(object)reader).GetString(0),
						FileSize = ((DbDataReader)(object)reader).GetInt64(1),
						LastWriteTime = ((DbDataReader)(object)reader).GetString(2),
						MD5 = (((DbDataReader)(object)reader).IsDBNull(3) ? null : ((DbDataReader)(object)reader).GetString(3)),
						SHA1 = (((DbDataReader)(object)reader).IsDBNull(4) ? null : ((DbDataReader)(object)reader).GetString(4)),
						SHA256 = (((DbDataReader)(object)reader).IsDBNull(5) ? null : ((DbDataReader)(object)reader).GetString(5)),
						SHA512 = (((DbDataReader)(object)reader).IsDBNull(6) ? null : ((DbDataReader)(object)reader).GetString(6)),
						MurmurHash2 = (((DbDataReader)(object)reader).IsDBNull(7) ? null : ((DbDataReader)(object)reader).GetString(7))
					};
				}
				finally
				{
					((IDisposable)reader)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)cmd)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)conn)?.Dispose();
		}
	}

	private static string? _GetHashFromEntry(CacheEntry entry, string algoName)
	{
		return algoName switch
		{
			"MD5" => entry.MD5, 
			"SHA1" => entry.SHA1, 
			"SHA256" => entry.SHA256, 
			"SHA512" => entry.SHA512, 
			"MurmurHash2" => entry.MurmurHash2, 
			_ => null, 
		};
	}

	private async Task _InsertOrUpdateHashAsync(string fullPath, long fileSize, string lastWrite, string algoName, string hash)
	{
		if (hash.IsNullOrWhiteSpace())
		{
			return;
		}
		SqliteConnection conn = _CreateConnection();
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "INSERT INTO HashCache (FilePath, FileSize, LastWriteTime, MD5, SHA1, SHA256, SHA512, MurmurHash2)\nVALUES (@FilePath, @FileSize, @LastWriteTime, @MD5, @SHA1, @SHA256, @SHA512, @MurmurHash2)\nON CONFLICT(FilePath) DO UPDATE SET\n    FileSize = excluded.FileSize,\n    LastWriteTime = excluded.LastWriteTime,\n    MD5 = COALESCE(excluded.MD5, HashCache.MD5),\n    SHA1 = COALESCE(excluded.SHA1, HashCache.SHA1),\n    SHA256 = COALESCE(excluded.SHA256, HashCache.SHA256),\n    SHA512 = COALESCE(excluded.SHA512, HashCache.SHA512),\n    MurmurHash2 = COALESCE(excluded.MurmurHash2, HashCache.MurmurHash2)";
				cmd.Parameters.AddWithValue("@FilePath", (object)fullPath);
				cmd.Parameters.AddWithValue("@FileSize", (object)fileSize);
				cmd.Parameters.AddWithValue("@LastWriteTime", (object)lastWrite);
				cmd.Parameters.AddWithValue("@MD5", (object)((algoName == "MD5") ? ((IConvertible)hash) : ((IConvertible)DBNull.Value)));
				cmd.Parameters.AddWithValue("@SHA1", (object)((algoName == "SHA1") ? ((IConvertible)hash) : ((IConvertible)DBNull.Value)));
				cmd.Parameters.AddWithValue("@SHA256", (object)((algoName == "SHA256") ? ((IConvertible)hash) : ((IConvertible)DBNull.Value)));
				cmd.Parameters.AddWithValue("@SHA512", (object)((algoName == "SHA512") ? ((IConvertible)hash) : ((IConvertible)DBNull.Value)));
				cmd.Parameters.AddWithValue("@MurmurHash2", (object)((algoName == "MurmurHash2") ? ((IConvertible)hash) : ((IConvertible)DBNull.Value)));
				await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				((IDisposable)cmd)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)conn)?.Dispose();
		}
	}

	private async Task _DeleteCacheEntryAsync(string fullPath)
	{
		SqliteConnection conn = _CreateConnection();
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "DELETE FROM HashCache WHERE FilePath = @FilePath";
				cmd.Parameters.AddWithValue("@FilePath", (object)fullPath);
				await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				((IDisposable)cmd)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)conn)?.Dispose();
		}
	}
}
