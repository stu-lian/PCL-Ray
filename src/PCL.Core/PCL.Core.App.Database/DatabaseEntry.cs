using System;
using LiteDB;

namespace PCL.Core.App.Database;

public abstract class DatabaseEntry : IDisposable
{
	protected readonly LiteDatabase Db;

	private bool _disposed;

	protected DatabaseEntry(string connPath)
	{
		LiteDatabase connection = DatabaseService.GetConnection(connPath);
		Db = connection ?? throw new ArgumentNullException("db", "Database variable can not be null.");
	}

	protected DatabaseEntry(LiteDatabase database)
	{
		Db = database ?? throw new ArgumentNullException("database", "Database variable can not be null.");
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			_disposed = true;
		}
	}
}
