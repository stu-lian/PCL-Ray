using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PCL.Core.IO.Storage.Cache.Model;

namespace PCL.Core.IO.Storage.Cache;

public class SqliteCacheStorage(string dbPath) : IDisposable
{
	private readonly string _connectionString = "Data Source=" + dbPath + ";Pooling=True";

	private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

	private bool _disposed;

	private async Task<SqliteConnection> _CreateConnectionAsync()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("SqliteCacheStorage");
		}
		SqliteConnection conn = new SqliteConnection(_connectionString);
		await ((DbConnection)(object)conn).OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
		return conn;
	}

	public async Task CleanupStartupAsync()
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "DELETE FROM cache_entries WHERE expires_at IS NOT NULL AND expires_at < datetime('now')";
				await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
				SqliteCommand walCmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)walCmd).CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
					await ((DbCommand)(object)walCmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (walCmd != null)
					{
						await ((DbCommand)(object)walCmd).DisposeAsync();
					}
				}
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

	public async Task UpsertAsync(CacheEntry entry, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)cmd).CommandText = "INSERT OR REPLACE INTO cache_entries (\n    cache_key, entry_type, content_type, data, data_size, file_hash, file_path,\n    content_hash, content_version, created_at, last_access_at, expires_at,\n    hit_count, tags, group_name, priority\n) VALUES (\n    @cache_key, @entry_type, @content_type, @data, @data_size, @file_hash, @file_path,\n    @content_hash, @content_version, COALESCE((SELECT created_at FROM cache_entries WHERE cache_key = @cache_key), datetime('now')),\n    datetime('now'), @expires_at, 0, @tags, @group_name, @priority\n)";
					_BindEntryParams(cmd, entry);
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
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
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<CacheEntry?> LookupAsync(string cacheKey, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		CacheEntry result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			CacheEntry cacheEntry2;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT * FROM cache_entries WHERE cache_key = @cache_key";
				cmd.Parameters.AddWithValue("@cache_key", (object)cacheKey);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				CacheEntry cacheEntry;
				try
				{
					cacheEntry = ((await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false)) ? _ReadEntry(reader) : null);
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				cacheEntry2 = cacheEntry;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = cacheEntry2;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task TouchAsync(string key, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)cmd).CommandText = "UPDATE cache_entries SET last_access_at = datetime('now'), hit_count = hit_count + 1 WHERE cache_key = @cache_key";
					cmd.Parameters.AddWithValue("@cache_key", (object)key);
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
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
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<bool> DeleteAsync(string cacheKey, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			bool result;
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				bool flag;
				try
				{
					((DbCommand)(object)cmd).CommandText = "DELETE FROM cache_entries WHERE cache_key = @cache_key";
					cmd.Parameters.AddWithValue("@cache_key", (object)cacheKey);
					flag = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false) > 0;
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
				result = flag;
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
			return result;
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				int num;
				try
				{
					((DbCommand)(object)cmd).CommandText = "DELETE FROM cache_entries WHERE expires_at IS NOT NULL AND datetime(expires_at) < datetime('now')";
					num = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
				result = num;
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
			return result;
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<int> DeleteByGroupAsync(string groupName, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				int num;
				try
				{
					((DbCommand)(object)cmd).CommandText = "DELETE FROM cache_entries WHERE group_name = @group_name";
					cmd.Parameters.AddWithValue("@group_name", (object)groupName);
					num = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
				result = num;
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
			return result;
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<List<string?>> GetFileHashesByGroupAsync(string groupName, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		List<string?> result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			List<string?> list2;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT DISTINCT file_hash FROM cache_entries WHERE group_name = @group_name AND file_hash IS NOT NULL";
				cmd.Parameters.AddWithValue("@group_name", (object)groupName);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				List<string?> list;
				try
				{
					List<string?> hashes = new List<string>();
					while (await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
					{
						hashes.Add(((DbDataReader)(object)reader).GetString(0));
					}
					list = hashes;
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				list2 = list;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = list2;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task<List<string?>> GetAllFileHashesAsync(CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		List<string?> result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			List<string?> list2;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT DISTINCT file_hash FROM cache_entries WHERE file_hash IS NOT NULL";
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				List<string?> list;
				try
				{
					List<string?> hashes = new List<string>();
					while (await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
					{
						hashes.Add(((DbDataReader)(object)reader).GetString(0));
					}
					list = hashes;
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				list2 = list;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = list2;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task<int> DeleteByTagAsync(string tag, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				int num;
				try
				{
					((DbCommand)(object)cmd).CommandText = "DELETE FROM cache_entries WHERE tags = @t OR tags LIKE @p1 OR tags LIKE @p2 OR tags LIKE @p3";
					cmd.Parameters.AddWithValue("@t", (object)tag);
					cmd.Parameters.AddWithValue("@p1", (object)(tag + ",%"));
					cmd.Parameters.AddWithValue("@p2", (object)("%," + tag + ",%"));
					cmd.Parameters.AddWithValue("@p3", (object)("%," + tag));
					num = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
				result = num;
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
			return result;
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<CacheStatsRow> GetStatsAsync(CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		CacheStatsRow result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			CacheStatsRow cacheStatsRow2;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT COUNT(*) AS total, COALESCE(SUM(data_size), 0) AS total_size, COALESCE(SUM(CASE WHEN expires_at < datetime('now') THEN 1 ELSE 0 END), 0) AS expired, COALESCE(SUM(CASE WHEN entry_type = 0 THEN 1 ELSE 0 END), 0) AS inline, COALESCE(SUM(CASE WHEN entry_type = 1 THEN 1 ELSE 0 END), 0) AS file_ref FROM cache_entries";
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				CacheStatsRow cacheStatsRow;
				try
				{
					cacheStatsRow = ((!(await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false))) ? new CacheStatsRow(0L, 0L, 0L, 0L, 0L) : new CacheStatsRow(((DbDataReader)(object)reader).GetInt64(0), ((DbDataReader)(object)reader).GetInt64(1), ((DbDataReader)(object)reader).GetInt64(2), ((DbDataReader)(object)reader).GetInt64(3), ((DbDataReader)(object)reader).GetInt64(4)));
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				cacheStatsRow2 = cacheStatsRow;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = cacheStatsRow2;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task<List<EvictionCandidate>> GetEvictionCandidatesAsync(int limit, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		List<EvictionCandidate> result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			List<EvictionCandidate> list3;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT cache_key, entry_type, file_hash, data_size, priority, hit_count\nFROM cache_entries\nWHERE priority < 3        -- 不淘汰 NeverEvict\nORDER BY priority ASC, hit_count ASC, last_access_at ASC\nLIMIT @limit";
				cmd.Parameters.AddWithValue("@limit", (object)limit);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				List<EvictionCandidate> list2;
				try
				{
					List<EvictionCandidate> list = new List<EvictionCandidate>();
					while (await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
					{
						list.Add(new EvictionCandidate(((DbDataReader)(object)reader).GetString(0), (EntryType)((DbDataReader)(object)reader).GetInt32(1), ((DbDataReader)(object)reader).IsDBNull(2) ? null : ((DbDataReader)(object)reader).GetString(2), ((DbDataReader)(object)reader).GetInt64(3), ((DbDataReader)(object)reader).GetInt32(4), ((DbDataReader)(object)reader).GetInt64(5)));
					}
					list2 = list;
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				list3 = list2;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = list3;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task CompactAsync(CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)cmd).CommandText = "VACUUM";
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
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
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task UpsertInstanceAsync(InstanceCacheRow row, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)cmd).CommandText = "\n                INSERT OR REPLACE INTO instance_cache\n                    (instance_path, instance_name, instance_state, card_type, is_starred,\n                     logo, description, release_time,\n                     vanilla_name, vanilla_version, drop_number, reliable,\n                     loaders_json,\n                     main_class, assets_index, inherits_from, java_version,\n                     source_json_hash, format_version, cached_at, last_loaded_at)\n                VALUES\n                    (@p, @n, @st, @cty, @is,\n                     @lo, @de, @rt,\n                     @vn, @vv, @dn, @re,\n                     @lj,\n                     @mc, @ai, @if, @jv,\n                     @sh, @fv, datetime('now'), datetime('now'))";
					_BindInstanceParams(cmd, row);
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
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
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<InstanceCacheRow?> LookupInstanceAsync(string instancePath, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		InstanceCacheRow result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			InstanceCacheRow instanceCacheRow2;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT * FROM instance_cache WHERE instance_path = @p";
				cmd.Parameters.AddWithValue("@p", (object)instancePath);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				InstanceCacheRow instanceCacheRow;
				try
				{
					instanceCacheRow = ((await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false)) ? _ReadInstanceRow(reader) : null);
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				instanceCacheRow2 = instanceCacheRow;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = instanceCacheRow2;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task<int> DeleteInstanceAsync(string instancePath, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				int num;
				try
				{
					((DbCommand)(object)cmd).CommandText = "DELETE FROM instance_cache WHERE instance_path = @p";
					cmd.Parameters.AddWithValue("@p", (object)instancePath);
					num = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
				result = num;
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
			return result;
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task UpsertComponentAsync(ComponentCacheRow row, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				try
				{
					((DbCommand)(object)cmd).CommandText = "\n                INSERT OR REPLACE INTO component_cache\n                    (instance_path, comp_type, file_name, relative_path,\n                     file_hash, file_size, last_modified, enabled,\n                     mod_name, mod_version, mod_author, mod_description,\n                     mod_loader, mod_deps,\n                     cache_version, scanned_at, scan_hash)\n                VALUES\n                    (@ip, @ct, @fn, @rp,\n                     @fh, @fs, @lm, @en,\n                     @mn, @mv, @ma, @md,\n                     @ml, @mdeps,\n                     @cv, datetime('now'), @sh)";
					_BindComponentParams(cmd, row);
					await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
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
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<List<ComponentCacheRow>> GetComponentsByInstanceAsync(string instancePath, string compType, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		List<ComponentCacheRow> result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			List<ComponentCacheRow> list3;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT * FROM component_cache WHERE instance_path = @ip AND comp_type = @ct";
				cmd.Parameters.AddWithValue("@ip", (object)instancePath);
				cmd.Parameters.AddWithValue("@ct", (object)compType);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				List<ComponentCacheRow> list2;
				try
				{
					List<ComponentCacheRow> list = new List<ComponentCacheRow>();
					while (await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
					{
						list.Add(_ReadComponentRow(reader));
					}
					list2 = list;
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				list3 = list2;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = list3;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task<ComponentCacheRow?> GetComponentAsync(string instancePath, string compType, string fileName, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		ComponentCacheRow result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			ComponentCacheRow componentCacheRow2;
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT * FROM component_cache WHERE instance_path = @ip AND comp_type = @ct AND file_name = @fn";
				cmd.Parameters.AddWithValue("@ip", (object)instancePath);
				cmd.Parameters.AddWithValue("@ct", (object)compType);
				cmd.Parameters.AddWithValue("@fn", (object)fileName);
				SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				ComponentCacheRow componentCacheRow;
				try
				{
					componentCacheRow = ((await ((DbDataReader)(object)reader).ReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false)) ? _ReadComponentRow(reader) : null);
				}
				finally
				{
					if (reader != null)
					{
						await ((DbDataReader)(object)reader).DisposeAsync();
					}
				}
				componentCacheRow2 = componentCacheRow;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = componentCacheRow2;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	public async Task<int> DeleteComponentsByInstanceAsync(string instancePath, CancellationToken ct)
	{
		await _writeLock.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				SqliteCommand cmd = conn.CreateCommand();
				int num;
				try
				{
					((DbCommand)(object)cmd).CommandText = "DELETE FROM component_cache WHERE instance_path = @ip";
					cmd.Parameters.AddWithValue("@ip", (object)instancePath);
					num = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (cmd != null)
					{
						await ((DbCommand)(object)cmd).DisposeAsync();
					}
				}
				result = num;
			}
			finally
			{
				if (conn != null)
				{
					await ((DbConnection)(object)conn).DisposeAsync();
				}
			}
			return result;
		}
		finally
		{
			_writeLock.Release();
		}
	}

	public async Task<string?> GetComponentScanHashAsync(string instancePath, string compType, CancellationToken ct)
	{
		SqliteConnection conn = await _CreateConnectionAsync().ConfigureAwait(continueOnCapturedContext: false);
		string result;
		try
		{
			SqliteCommand cmd = conn.CreateCommand();
			string text;
			try
			{
				((DbCommand)(object)cmd).CommandText = "\n            SELECT DISTINCT scan_hash\n            FROM component_cache\n            WHERE instance_path = @ip AND comp_type = @ct AND scan_hash IS NOT NULL\n            ORDER BY scanned_at DESC\n            LIMIT 1";
				cmd.Parameters.AddWithValue("@ip", (object)instancePath);
				cmd.Parameters.AddWithValue("@ct", (object)compType);
				text = (await ((DbCommand)(object)cmd).ExecuteScalarAsync(ct).ConfigureAwait(continueOnCapturedContext: false)) as string;
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			result = text;
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
		return result;
	}

	private static void _BindEntryParams(SqliteCommand cmd, CacheEntry e)
	{
		cmd.Parameters.AddWithValue("@cache_key", (object)e.CacheKey);
		cmd.Parameters.AddWithValue("@entry_type", (object)e.EntryType);
		cmd.Parameters.AddWithValue("@content_type", (object)e.ContentType);
		cmd.Parameters.AddWithValue("@data", ((object)e.Data) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@data_size", (object)e.DataSize);
		cmd.Parameters.AddWithValue("@file_hash", ((object)e.FileHash) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@file_path", ((object)e.FilePath) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@content_hash", ((object)e.ContentHash) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@content_version", (object)e.ContentVersion);
		cmd.Parameters.AddWithValue("@expires_at", (object)(e.ExpiresAt.HasValue ? ((IConvertible)e.ExpiresAt.Value.ToString("O")) : ((IConvertible)DBNull.Value)));
		cmd.Parameters.AddWithValue("@tags", (object)e.Tags);
		cmd.Parameters.AddWithValue("@group_name", (object)e.GroupName);
		cmd.Parameters.AddWithValue("@priority", (object)e.Priority);
	}

	private static DateTime _ParseDateTime(string value)
	{
		if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
		{
			return DateTime.MinValue;
		}
		return result;
	}

	private static CacheEntry _ReadEntry(SqliteDataReader r)
	{
		return new CacheEntry
		{
			CacheKey = ((DbDataReader)(object)r).GetString(0),
			EntryType = (EntryType)((DbDataReader)(object)r).GetInt32(1),
			ContentType = ((DbDataReader)(object)r).GetString(2),
			Data = (((DbDataReader)(object)r).IsDBNull(3) ? null : ((byte[])((DbDataReader)(object)r).GetValue(3))),
			DataSize = ((DbDataReader)(object)r).GetInt64(4),
			FileHash = (((DbDataReader)(object)r).IsDBNull(5) ? null : ((DbDataReader)(object)r).GetString(5)),
			FilePath = (((DbDataReader)(object)r).IsDBNull(6) ? null : ((DbDataReader)(object)r).GetString(6)),
			ContentHash = (((DbDataReader)(object)r).IsDBNull(7) ? null : ((DbDataReader)(object)r).GetString(7)),
			ContentVersion = ((DbDataReader)(object)r).GetInt32(8),
			CachedAt = _ParseDateTime(((DbDataReader)(object)r).GetString(9)),
			LastAccessAt = _ParseDateTime(((DbDataReader)(object)r).GetString(10)),
			ExpiresAt = (((DbDataReader)(object)r).IsDBNull(11) ? ((DateTime?)null) : new DateTime?(_ParseDateTime(((DbDataReader)(object)r).GetString(11)))),
			HitCount = ((DbDataReader)(object)r).GetInt64(12),
			Tags = ((DbDataReader)(object)r).GetString(13),
			GroupName = ((DbDataReader)(object)r).GetString(14),
			Priority = ((DbDataReader)(object)r).GetInt32(15)
		};
	}

	private static void _BindInstanceParams(SqliteCommand cmd, InstanceCacheRow r)
	{
		cmd.Parameters.AddWithValue("@p", (object)r.InstancePath);
		cmd.Parameters.AddWithValue("@n", (object)r.InstanceName);
		cmd.Parameters.AddWithValue("@st", (object)r.InstanceState);
		cmd.Parameters.AddWithValue("@cty", (object)r.CardType);
		cmd.Parameters.AddWithValue("@is", (object)(r.IsStarred ? 1 : 0));
		cmd.Parameters.AddWithValue("@lo", ((object)r.Logo) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@de", ((object)r.Description) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@rt", ((object)r.ReleaseTime) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@vn", ((object)r.VanillaName) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@vv", ((object)r.VanillaVersion) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@dn", (object)r.DropNumber);
		cmd.Parameters.AddWithValue("@re", (object)(r.Reliable ? 1 : 0));
		cmd.Parameters.AddWithValue("@lj", (object)r.LoaderJson);
		cmd.Parameters.AddWithValue("@mc", ((object)r.MainClass) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@ai", ((object)r.AssetsIndex) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@if", ((object)r.InheritsFrom) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@jv", ((object)r.JavaVersion) ?? DBNull.Value);
		cmd.Parameters.AddWithValue("@sh", (object)r.SourceJsonHash);
		cmd.Parameters.AddWithValue("@fv", (object)r.FormatVersion);
	}

	private static InstanceCacheRow _ReadInstanceRow(SqliteDataReader r)
	{
		return new InstanceCacheRow
		{
			InstancePath = ((DbDataReader)(object)r).GetString(0),
			InstanceName = ((DbDataReader)(object)r).GetString(1),
			InstanceState = ((DbDataReader)(object)r).GetString(2),
			CardType = ((DbDataReader)(object)r).GetInt32(3),
			IsStarred = (((DbDataReader)(object)r).GetInt32(4) != 0),
			Logo = (((DbDataReader)(object)r).IsDBNull(5) ? null : ((DbDataReader)(object)r).GetString(5)),
			Description = (((DbDataReader)(object)r).IsDBNull(6) ? null : ((DbDataReader)(object)r).GetString(6)),
			ReleaseTime = (((DbDataReader)(object)r).IsDBNull(7) ? null : ((DbDataReader)(object)r).GetString(7)),
			VanillaName = (((DbDataReader)(object)r).IsDBNull(8) ? null : ((DbDataReader)(object)r).GetString(8)),
			VanillaVersion = (((DbDataReader)(object)r).IsDBNull(9) ? null : ((DbDataReader)(object)r).GetString(9)),
			DropNumber = ((DbDataReader)(object)r).GetInt32(10),
			Reliable = (((DbDataReader)(object)r).GetInt32(11) != 0),
			LoaderJson = ((DbDataReader)(object)r).GetString(12),
			MainClass = (((DbDataReader)(object)r).IsDBNull(13) ? null : ((DbDataReader)(object)r).GetString(13)),
			AssetsIndex = (((DbDataReader)(object)r).IsDBNull(14) ? null : ((DbDataReader)(object)r).GetString(14)),
			InheritsFrom = (((DbDataReader)(object)r).IsDBNull(15) ? null : ((DbDataReader)(object)r).GetString(15)),
			JavaVersion = (((DbDataReader)(object)r).IsDBNull(16) ? ((int?)null) : new int?(((DbDataReader)(object)r).GetInt32(16))),
			SourceJsonHash = ((DbDataReader)(object)r).GetString(17),
			FormatVersion = ((DbDataReader)(object)r).GetInt32(18),
			CachedAt = _ParseDateTime(((DbDataReader)(object)r).GetString(19)),
			LastLoadedAt = (((DbDataReader)(object)r).IsDBNull(20) ? ((DateTime?)null) : new DateTime?(_ParseDateTime(((DbDataReader)(object)r).GetString(20))))
		};
	}

	private static void _BindComponentParams(SqliteCommand cmd, ComponentCacheRow r)
	{
		cmd.Parameters.AddWithValue("@instance_path", (object)r.InstancePath);
		cmd.Parameters.AddWithValue("@comp_type", (object)r.CompType);
		cmd.Parameters.AddWithValue("@file_name", (object)r.FileName);
		cmd.Parameters.AddWithValue("@relative_path", (object)r.RelativePath);
		cmd.Parameters.AddWithValue("@file_hash", ((object)r.FileHash) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@file_size", (object)r.FileSize);
		cmd.Parameters.AddWithValue("@last_modified", (object)r.LastModified.ToString("O"));
		cmd.Parameters.AddWithValue("@enabled", (object)(r.Enabled ? 1 : 0));
		cmd.Parameters.AddWithValue("@mod_name", ((object)r.ModName) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@mod_version", ((object)r.ModVersion) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@mod_author", ((object)r.ModAuthor) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@mod_description", ((object)r.ModDescription) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@mod_loader", ((object)r.ModLoader) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@mod_dependencies", ((object)r.ModDependencies) ?? ((object)DBNull.Value));
		cmd.Parameters.AddWithValue("@cache_version", (object)r.CacheVersion);
		cmd.Parameters.AddWithValue("@scan_hash", ((object)r.ScanHash) ?? ((object)DBNull.Value));
	}

	private static ComponentCacheRow _ReadComponentRow(SqliteDataReader r)
	{
		return new ComponentCacheRow
		{
			InstancePath = ((DbDataReader)(object)r).GetString(0),
			CompType = ((DbDataReader)(object)r).GetString(1),
			FileName = ((DbDataReader)(object)r).GetString(2),
			RelativePath = ((DbDataReader)(object)r).GetString(3),
			FileHash = (((DbDataReader)(object)r).IsDBNull(4) ? null : ((DbDataReader)(object)r).GetString(4)),
			FileSize = ((DbDataReader)(object)r).GetInt64(5),
			LastModified = _ParseDateTime(((DbDataReader)(object)r).GetString(6)),
			Enabled = (((DbDataReader)(object)r).GetInt32(7) != 0),
			ModName = (((DbDataReader)(object)r).IsDBNull(8) ? null : ((DbDataReader)(object)r).GetString(8)),
			ModVersion = (((DbDataReader)(object)r).IsDBNull(9) ? null : ((DbDataReader)(object)r).GetString(9)),
			ModAuthor = (((DbDataReader)(object)r).IsDBNull(10) ? null : ((DbDataReader)(object)r).GetString(10)),
			ModDescription = (((DbDataReader)(object)r).IsDBNull(11) ? null : ((DbDataReader)(object)r).GetString(11)),
			ModLoader = (((DbDataReader)(object)r).IsDBNull(12) ? null : ((DbDataReader)(object)r).GetString(12)),
			ModDependencies = (((DbDataReader)(object)r).IsDBNull(13) ? null : ((DbDataReader)(object)r).GetString(13)),
			CacheVersion = ((DbDataReader)(object)r).GetInt32(14),
			ScannedAt = _ParseDateTime(((DbDataReader)(object)r).GetString(15)),
			ScanHash = (((DbDataReader)(object)r).IsDBNull(16) ? null : ((DbDataReader)(object)r).GetString(16))
		};
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_writeLock.Dispose();
		}
	}
}
