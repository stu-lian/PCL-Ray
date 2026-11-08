using System;

namespace PCL.Core.IO.Storage.Cache;

public record CacheOptions
{
	public required string DatabasePath { get; init; }

	public required string FileCacheRoot { get; init; }

	public long MaxCacheSize { get; init; } = 2147483648L;

	public int MaxInlineSize { get; init; } = 262144;

	public TimeSpan EvictionInterval { get; init; } = TimeSpan.FromMinutes(5L);

	public long ReserveBytes { get; init; } = 268435456L;

	public bool EnableCompression { get; init; } = true;
}
