using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PCL.Core.App.Configuration;

public struct ConfigValueCache<TValue>
{
	private TValue? _cachedValue = default;

	private bool _hasCachedValue = false;

	private readonly ConcurrentDictionary<object, TValue> _cacheWithContext = new ConcurrentDictionary<object, TValue>();

	public ConfigValueCache()
	{
	}

	public bool Exists(object? argument = null)
	{
		if (argument != null)
		{
			return _cacheWithContext.ContainsKey(argument);
		}
		return _hasCachedValue;
	}

	public bool TryRead([NotNullWhen(true)] out TValue? value, object? argument = null)
	{
		if (argument != null)
		{
			return _cacheWithContext.TryGetValue(argument, out value);
		}
		if (_hasCachedValue)
		{
			value = _cachedValue;
			return true;
		}
		value = default;
		return false;
	}

	public void Write(TValue value, object? argument = null)
	{
		if (argument != null)
		{
			_cacheWithContext[argument] = value;
			return;
		}
		_hasCachedValue = true;
		_cachedValue = value;
	}

	public bool Invalidate(object? argument)
	{
		TValue value;
		if (argument != null)
		{
			return _cacheWithContext.TryRemove(argument, out value);
		}
		if (!_hasCachedValue)
		{
			return false;
		}
		_cachedValue = default;
		_hasCachedValue = false;
		return true;
	}

	public void InvalidateAll()
	{
		_cacheWithContext.Clear();
		_cachedValue = default;
		_hasCachedValue = false;
	}
}
