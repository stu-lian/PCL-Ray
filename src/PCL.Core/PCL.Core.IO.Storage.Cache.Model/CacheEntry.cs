using System;

namespace PCL.Core.IO.Storage.Cache.Model;

public record CacheEntry
{
	public string CacheKey { get; init; } = string.Empty;

	public EntryType EntryType { get; init; }

	public string ContentType { get; init; } = string.Empty;

	public byte[]? Data { get; init; }

	public long DataSize { get; init; }

	public string? FileHash { get; set; }

	public string? FilePath { get; init; }

	public string? ContentHash { get; set; }

	public int ContentVersion { get; init; }

	public DateTime CachedAt { get; init; }

	public DateTime LastAccessAt { get; init; }

	public DateTime? ExpiresAt { get; init; }

	public long HitCount { get; init; }

	public string Tags { get; init; } = string.Empty;

	public string GroupName { get; init; } = string.Empty;

	public int Priority { get; init; }
}
