using System;

namespace PCL.Core.IO.Storage;

public readonly record struct CacheResult<T>
{
	public static readonly CacheResult<T> Miss;

	public readonly bool Found;

	public readonly T? Value;

	public readonly DateTime CachedAt;

	public static CacheResult<T> Hit(T value, DateTime cachedAt)
	{
		return new CacheResult<T>(value, cachedAt);
	}

	private CacheResult(T value, DateTime cachedAt)
	{
		Found = true;
		Value = value;
		CachedAt = cachedAt;
	}
}
