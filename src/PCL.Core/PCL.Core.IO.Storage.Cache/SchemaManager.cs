using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PCL.Core.IO.Storage.Cache;

public class SchemaManager(string connectionString)
{
	private const int CurrentSchemaVersion = 1;

	private const string DdlV1CreateAllTables = "\n        CREATE TABLE IF NOT EXISTS cache_entries (\n            cache_key       TEXT NOT NULL PRIMARY KEY,\n            entry_type      INTEGER NOT NULL DEFAULT 0,\n            content_type    TEXT NOT NULL DEFAULT '',\n            data            BLOB,\n            data_size       INTEGER NOT NULL DEFAULT 0,\n            file_hash       TEXT,\n            file_path       TEXT,\n            content_hash    TEXT,\n            content_version INTEGER NOT NULL DEFAULT 1,\n            created_at      TEXT NOT NULL DEFAULT (datetime('now')),\n            last_access_at  TEXT NOT NULL DEFAULT (datetime('now')),\n            expires_at      TEXT,\n            hit_count       INTEGER NOT NULL DEFAULT 0,\n            tags            TEXT NOT NULL DEFAULT '',\n            group_name      TEXT NOT NULL DEFAULT '',\n            priority        INTEGER NOT NULL DEFAULT 1\n        );\n\n        CREATE TABLE IF NOT EXISTS instance_cache (\n            instance_path   TEXT NOT NULL PRIMARY KEY,\n            instance_name   TEXT NOT NULL,\n            instance_state  TEXT NOT NULL DEFAULT 'Error',\n            card_type       INTEGER NOT NULL DEFAULT 0,\n            is_starred      INTEGER NOT NULL DEFAULT 0,\n            logo            TEXT,\n            description     TEXT,\n            release_time    TEXT,\n            vanilla_name    TEXT,\n            vanilla_version TEXT,\n            drop_number     INTEGER NOT NULL DEFAULT 0,\n            reliable        INTEGER NOT NULL DEFAULT 1,\n            loaders_json    TEXT NOT NULL DEFAULT '[]',\n            main_class      TEXT,\n            assets_index    TEXT,\n            inherits_from   TEXT,\n            java_version    INTEGER,\n            source_json_hash TEXT NOT NULL,\n            format_version  INTEGER NOT NULL DEFAULT 1,\n            cached_at       TEXT NOT NULL DEFAULT (datetime('now')),\n            last_loaded_at  TEXT\n        );\n\n        CREATE TABLE IF NOT EXISTS component_cache (\n            instance_path   TEXT NOT NULL,\n            comp_type       TEXT NOT NULL,\n            file_name       TEXT NOT NULL,\n            relative_path   TEXT NOT NULL,\n            file_hash       TEXT,\n            file_size       INTEGER NOT NULL DEFAULT 0,\n            last_modified   TEXT NOT NULL,\n            enabled         INTEGER NOT NULL DEFAULT 1,\n            mod_name        TEXT,\n            mod_version     TEXT,\n            mod_author      TEXT,\n            mod_description TEXT,\n            mod_loader      TEXT,\n            mod_deps        TEXT,\n            cache_version   INTEGER NOT NULL DEFAULT 7,\n            scanned_at      TEXT NOT NULL DEFAULT (datetime('now')),\n            scan_hash       TEXT,\n            PRIMARY KEY (instance_path, comp_type, file_name)\n        );\n        \n        CREATE TABLE IF NOT EXISTS cache_stats (\n            stat_name  TEXT NOT NULL PRIMARY KEY,\n            stat_value INTEGER NOT NULL DEFAULT 0\n        );\n    ";

	private const string DdlV1CreateIndexes = "\n        CREATE INDEX IF NOT EXISTS idx_ce_expires  ON cache_entries(expires_at);\n        CREATE INDEX IF NOT EXISTS idx_ce_group    ON cache_entries(group_name);\n        CREATE INDEX IF NOT EXISTS idx_ce_tags     ON cache_entries(tags);\n        CREATE INDEX IF NOT EXISTS idx_ce_access   ON cache_entries(last_access_at);\n        CREATE INDEX IF NOT EXISTS idx_ce_priority ON cache_entries(priority);\n        CREATE INDEX IF NOT EXISTS idx_ce_hits     ON cache_entries(hit_count);\n        CREATE INDEX IF NOT EXISTS idx_cc_instance ON component_cache(instance_path);\n        CREATE INDEX IF NOT EXISTS idx_cc_scanhash ON component_cache(instance_path, comp_type, scan_hash);\n    ";

	public async Task EnsureCurrentSchemaAsync()
	{
		SqliteConnection conn = new SqliteConnection(connectionString);
		try
		{
			await ((DbConnection)(object)conn).OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
			await _ExecutePragmaAsync(conn, "journal_mode", "WAL").ConfigureAwait(continueOnCapturedContext: false);
			await _ExecutePragmaAsync(conn, "synchronous", "NORMAL").ConfigureAwait(continueOnCapturedContext: false);
			await _ExecutePragmaAsync(conn, "cache_size", "-8000").ConfigureAwait(continueOnCapturedContext: false);
			SqliteCommand cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "CREATE TABLE IF NOT EXISTS cache_meta (\n    key   TEXT NOT NULL PRIMARY KEY,\n    value TEXT NOT NULL\n)";
				await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			int currentVersion = 0;
			cmd = conn.CreateCommand();
			try
			{
				((DbCommand)(object)cmd).CommandText = "SELECT value FROM cache_meta WHERE key = 'schema_version'";
				object obj = await ((DbCommand)(object)cmd).ExecuteScalarAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (obj != null)
				{
					currentVersion = int.Parse(obj.ToString() ?? throw new InvalidOperationException("Invalid schema version"));
				}
			}
			finally
			{
				if (cmd != null)
				{
					await ((DbCommand)(object)cmd).DisposeAsync();
				}
			}
			if (currentVersion < 1)
			{
				await _ApplyMigrationsAsync(conn, currentVersion).ConfigureAwait(continueOnCapturedContext: false);
			}
			await _EnsureAllTablesExistAsync(conn).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (conn != null)
			{
				await ((DbConnection)(object)conn).DisposeAsync();
			}
		}
	}

	private static async Task<int> _ExecutePragmaAsync(SqliteConnection conn, string pragma, string value)
	{
		SqliteCommand cmd = conn.CreateCommand();
		int result;
		try
		{
			((DbCommand)(object)cmd).CommandText = "PRAGMA " + pragma + " = " + value;
			result = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (cmd != null)
			{
				await ((DbCommand)(object)cmd).DisposeAsync();
			}
		}
		return result;
	}

	private async Task _ApplyMigrationsAsync(SqliteConnection conn, int fromVersion)
	{
		int v = fromVersion;
		while (v < 1)
		{
			v++;
			SqliteTransaction tx = conn.BeginTransaction();
			try
			{
				await _ApplyMigrationAsync(conn, v).ConfigureAwait(continueOnCapturedContext: false);
				SqliteCommand setVer = conn.CreateCommand();
				try
				{
					((DbCommand)(object)setVer).CommandText = "INSERT OR REPLACE INTO cache_meta (key, value) VALUES ('schema_version', @v)";
					setVer.Parameters.AddWithValue("@v", (object)v);
					await ((DbCommand)(object)setVer).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
					await ((DbTransaction)(object)tx).CommitAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (setVer != null)
					{
						await ((DbCommand)(object)setVer).DisposeAsync();
					}
				}
			}
			catch
			{
				await ((DbTransaction)(object)tx).RollbackAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
				throw;
			}
			finally
			{
				if (tx != null)
				{
					await ((DbTransaction)(object)tx).DisposeAsync();
				}
			}
		}
	}

	private async Task _ApplyMigrationAsync(SqliteConnection conn, int targetVersion)
	{
		if (targetVersion == 1)
		{
			await _ExecuteDdlAsync(conn, "\n        CREATE TABLE IF NOT EXISTS cache_entries (\n            cache_key       TEXT NOT NULL PRIMARY KEY,\n            entry_type      INTEGER NOT NULL DEFAULT 0,\n            content_type    TEXT NOT NULL DEFAULT '',\n            data            BLOB,\n            data_size       INTEGER NOT NULL DEFAULT 0,\n            file_hash       TEXT,\n            file_path       TEXT,\n            content_hash    TEXT,\n            content_version INTEGER NOT NULL DEFAULT 1,\n            created_at      TEXT NOT NULL DEFAULT (datetime('now')),\n            last_access_at  TEXT NOT NULL DEFAULT (datetime('now')),\n            expires_at      TEXT,\n            hit_count       INTEGER NOT NULL DEFAULT 0,\n            tags            TEXT NOT NULL DEFAULT '',\n            group_name      TEXT NOT NULL DEFAULT '',\n            priority        INTEGER NOT NULL DEFAULT 1\n        );\n\n        CREATE TABLE IF NOT EXISTS instance_cache (\n            instance_path   TEXT NOT NULL PRIMARY KEY,\n            instance_name   TEXT NOT NULL,\n            instance_state  TEXT NOT NULL DEFAULT 'Error',\n            card_type       INTEGER NOT NULL DEFAULT 0,\n            is_starred      INTEGER NOT NULL DEFAULT 0,\n            logo            TEXT,\n            description     TEXT,\n            release_time    TEXT,\n            vanilla_name    TEXT,\n            vanilla_version TEXT,\n            drop_number     INTEGER NOT NULL DEFAULT 0,\n            reliable        INTEGER NOT NULL DEFAULT 1,\n            loaders_json    TEXT NOT NULL DEFAULT '[]',\n            main_class      TEXT,\n            assets_index    TEXT,\n            inherits_from   TEXT,\n            java_version    INTEGER,\n            source_json_hash TEXT NOT NULL,\n            format_version  INTEGER NOT NULL DEFAULT 1,\n            cached_at       TEXT NOT NULL DEFAULT (datetime('now')),\n            last_loaded_at  TEXT\n        );\n\n        CREATE TABLE IF NOT EXISTS component_cache (\n            instance_path   TEXT NOT NULL,\n            comp_type       TEXT NOT NULL,\n            file_name       TEXT NOT NULL,\n            relative_path   TEXT NOT NULL,\n            file_hash       TEXT,\n            file_size       INTEGER NOT NULL DEFAULT 0,\n            last_modified   TEXT NOT NULL,\n            enabled         INTEGER NOT NULL DEFAULT 1,\n            mod_name        TEXT,\n            mod_version     TEXT,\n            mod_author      TEXT,\n            mod_description TEXT,\n            mod_loader      TEXT,\n            mod_deps        TEXT,\n            cache_version   INTEGER NOT NULL DEFAULT 7,\n            scanned_at      TEXT NOT NULL DEFAULT (datetime('now')),\n            scan_hash       TEXT,\n            PRIMARY KEY (instance_path, comp_type, file_name)\n        );\n        \n        CREATE TABLE IF NOT EXISTS cache_stats (\n            stat_name  TEXT NOT NULL PRIMARY KEY,\n            stat_value INTEGER NOT NULL DEFAULT 0\n        );\n    ").ConfigureAwait(continueOnCapturedContext: false);
			await _ExecuteDdlAsync(conn, "\n        CREATE INDEX IF NOT EXISTS idx_ce_expires  ON cache_entries(expires_at);\n        CREATE INDEX IF NOT EXISTS idx_ce_group    ON cache_entries(group_name);\n        CREATE INDEX IF NOT EXISTS idx_ce_tags     ON cache_entries(tags);\n        CREATE INDEX IF NOT EXISTS idx_ce_access   ON cache_entries(last_access_at);\n        CREATE INDEX IF NOT EXISTS idx_ce_priority ON cache_entries(priority);\n        CREATE INDEX IF NOT EXISTS idx_ce_hits     ON cache_entries(hit_count);\n        CREATE INDEX IF NOT EXISTS idx_cc_instance ON component_cache(instance_path);\n        CREATE INDEX IF NOT EXISTS idx_cc_scanhash ON component_cache(instance_path, comp_type, scan_hash);\n    ").ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task<int> _EnsureAllTablesExistAsync(SqliteConnection conn)
	{
		await _ExecuteDdlAsync(conn, "\n        CREATE TABLE IF NOT EXISTS cache_entries (\n            cache_key       TEXT NOT NULL PRIMARY KEY,\n            entry_type      INTEGER NOT NULL DEFAULT 0,\n            content_type    TEXT NOT NULL DEFAULT '',\n            data            BLOB,\n            data_size       INTEGER NOT NULL DEFAULT 0,\n            file_hash       TEXT,\n            file_path       TEXT,\n            content_hash    TEXT,\n            content_version INTEGER NOT NULL DEFAULT 1,\n            created_at      TEXT NOT NULL DEFAULT (datetime('now')),\n            last_access_at  TEXT NOT NULL DEFAULT (datetime('now')),\n            expires_at      TEXT,\n            hit_count       INTEGER NOT NULL DEFAULT 0,\n            tags            TEXT NOT NULL DEFAULT '',\n            group_name      TEXT NOT NULL DEFAULT '',\n            priority        INTEGER NOT NULL DEFAULT 1\n        );\n\n        CREATE TABLE IF NOT EXISTS instance_cache (\n            instance_path   TEXT NOT NULL PRIMARY KEY,\n            instance_name   TEXT NOT NULL,\n            instance_state  TEXT NOT NULL DEFAULT 'Error',\n            card_type       INTEGER NOT NULL DEFAULT 0,\n            is_starred      INTEGER NOT NULL DEFAULT 0,\n            logo            TEXT,\n            description     TEXT,\n            release_time    TEXT,\n            vanilla_name    TEXT,\n            vanilla_version TEXT,\n            drop_number     INTEGER NOT NULL DEFAULT 0,\n            reliable        INTEGER NOT NULL DEFAULT 1,\n            loaders_json    TEXT NOT NULL DEFAULT '[]',\n            main_class      TEXT,\n            assets_index    TEXT,\n            inherits_from   TEXT,\n            java_version    INTEGER,\n            source_json_hash TEXT NOT NULL,\n            format_version  INTEGER NOT NULL DEFAULT 1,\n            cached_at       TEXT NOT NULL DEFAULT (datetime('now')),\n            last_loaded_at  TEXT\n        );\n\n        CREATE TABLE IF NOT EXISTS component_cache (\n            instance_path   TEXT NOT NULL,\n            comp_type       TEXT NOT NULL,\n            file_name       TEXT NOT NULL,\n            relative_path   TEXT NOT NULL,\n            file_hash       TEXT,\n            file_size       INTEGER NOT NULL DEFAULT 0,\n            last_modified   TEXT NOT NULL,\n            enabled         INTEGER NOT NULL DEFAULT 1,\n            mod_name        TEXT,\n            mod_version     TEXT,\n            mod_author      TEXT,\n            mod_description TEXT,\n            mod_loader      TEXT,\n            mod_deps        TEXT,\n            cache_version   INTEGER NOT NULL DEFAULT 7,\n            scanned_at      TEXT NOT NULL DEFAULT (datetime('now')),\n            scan_hash       TEXT,\n            PRIMARY KEY (instance_path, comp_type, file_name)\n        );\n        \n        CREATE TABLE IF NOT EXISTS cache_stats (\n            stat_name  TEXT NOT NULL PRIMARY KEY,\n            stat_value INTEGER NOT NULL DEFAULT 0\n        );\n    ").ConfigureAwait(continueOnCapturedContext: false);
		await _ExecuteDdlAsync(conn, "\n        CREATE INDEX IF NOT EXISTS idx_ce_expires  ON cache_entries(expires_at);\n        CREATE INDEX IF NOT EXISTS idx_ce_group    ON cache_entries(group_name);\n        CREATE INDEX IF NOT EXISTS idx_ce_tags     ON cache_entries(tags);\n        CREATE INDEX IF NOT EXISTS idx_ce_access   ON cache_entries(last_access_at);\n        CREATE INDEX IF NOT EXISTS idx_ce_priority ON cache_entries(priority);\n        CREATE INDEX IF NOT EXISTS idx_ce_hits     ON cache_entries(hit_count);\n        CREATE INDEX IF NOT EXISTS idx_cc_instance ON component_cache(instance_path);\n        CREATE INDEX IF NOT EXISTS idx_cc_scanhash ON component_cache(instance_path, comp_type, scan_hash);\n    ").ConfigureAwait(continueOnCapturedContext: false);
		SqliteCommand cmd = conn.CreateCommand();
		int result;
		try
		{
			((DbCommand)(object)cmd).CommandText = "INSERT OR IGNORE INTO cache_meta (key, value) VALUES ('schema_version', @v)";
			cmd.Parameters.AddWithValue("@v", (object)1);
			result = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (cmd != null)
			{
				await ((DbCommand)(object)cmd).DisposeAsync();
			}
		}
		return result;
	}

	private static async Task<int> _ExecuteDdlAsync(SqliteConnection conn, string ddl)
	{
		SqliteCommand cmd = conn.CreateCommand();
		int result;
		try
		{
			((DbCommand)(object)cmd).CommandText = ddl;
			result = await ((DbCommand)(object)cmd).ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (cmd != null)
			{
				await ((DbCommand)(object)cmd).DisposeAsync();
			}
		}
		return result;
	}
}
