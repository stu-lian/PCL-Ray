using System;

namespace PCL.Core.IO.Storage.Cache;

public record CachePolicy
{
	public TimeSpan? AbsoluteExpiration { get; init; } = TimeSpan.FromHours(1);

	public TimeSpan? SlidingExpiration { get; init; }

	public CachePriority Priority { get; init; } = CachePriority.Normal;

	public CacheStorageMode StorageMode { get; init; }

	public string? Group { get; init; }

	public string? Tags { get; init; }

	public int ContentVersion { get; init; } = 1;

	public TimeSpan? MinTimeToLive { get; init; }

	public static readonly CachePolicy Default = new CachePolicy();

	public static readonly CachePolicy NeverExpire = new CachePolicy
	{
		SlidingExpiration = null,
		AbsoluteExpiration = null,
		Priority = CachePriority.NeverEvict
	};
}
