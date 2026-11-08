using System;
using System.Collections.Concurrent;
using LiteDB;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Database;

[LifecycleService(LifecycleState.Loading)]
public class DatabaseService : GeneralService
{
	private static readonly ConcurrentDictionary<string, LiteDatabase> _Instances = new ConcurrentDictionary<string, LiteDatabase>();

	public DatabaseService()
		: base("database", "数据库管理")
	{
	}

	public override void Stop()
	{
		foreach (LiteDatabase value in _Instances.Values)
		{
			value.Dispose();
		}
		_Instances.Clear();
	}

	public static LiteDatabase GetConnection(string connectionPath)
	{
		if (string.IsNullOrWhiteSpace(connectionPath))
		{
			throw new ArgumentException("Connection string canot be null or whitespace.", "connectionPath");
		}
		return _Instances.GetOrAdd(connectionPath, (string cp) =>
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Expected Obj, but got Unknown
			return new LiteDatabase(cp, (BsonMapper)null);
		});
	}
}
