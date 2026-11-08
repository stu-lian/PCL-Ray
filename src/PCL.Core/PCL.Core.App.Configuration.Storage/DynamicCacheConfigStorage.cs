using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PCL.Core.App.Configuration.Storage;

public class DynamicCacheConfigStorage : ConfigStorage
{
	private readonly Dictionary<object, ConfigStorage> _cache = new Dictionary<object, ConfigStorage>();

	private ConfigStorage? _nullContextCache;

	public required Func<object?, ConfigStorage> StorageFactory { get; init; }

	protected override bool OnAccess<TKey, TValue>(StorageAction action, ref TKey key, [NotNullWhen(true)] ref TValue value, object? context)
	{
		ConfigStorage value2;
		if (context == null)
		{
			value2 = _nullContextCache;
		}
		else
		{
			_cache.TryGetValue(context, out value2);
		}
		if (value2 == null)
		{
			try
			{
				value2 = StorageFactory(context);
				if (context == null)
				{
					_nullContextCache = value2;
				}
				else
				{
					_cache[context] = value2;
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Failed to invoke storage factory", innerException);
			}
		}
		return value2.Access(action, ref key, ref value, context);
	}

	protected override void OnStop()
	{
		foreach (ConfigStorage value in _cache.Values)
		{
			value.Stop();
		}
		_cache.Clear();
	}

	public bool InvalidateCache(object context)
	{
		bool flag = _cache.TryGetValue(context, out ConfigStorage value);
		if (flag)
		{
			value?.Stop();
			_cache.Remove(context);
		}
		return flag;
	}
}
