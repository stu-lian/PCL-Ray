using System;

namespace PCL.Core.IO.Storage.Cache;

public record CacheStats
{
	public long TotalEntries { get; init; }

	public long TotalSizeBytes { get; init; }

	public long ExpiredEntries { get; init; }

	public long InlineEntries { get; init; }

	public long FileEntries { get; init; }

	public long CacheHits { get; init; }

	public long CacheMisses { get; init; }

	public double HitRate
	{
		get
		{
			if (CacheHits + CacheMisses <= 0)
			{
				return 0.0;
			}
			return (double)CacheHits / (double)(CacheHits + CacheMisses);
		}
	}

	public DateTime? LastCleanup { get; init; }
}
