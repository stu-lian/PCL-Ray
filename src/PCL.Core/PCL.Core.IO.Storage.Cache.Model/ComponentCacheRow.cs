using System;

namespace PCL.Core.IO.Storage.Cache.Model;

public record ComponentCacheRow
{
	public string InstancePath { get; init; } = string.Empty;

	public string CompType { get; init; } = string.Empty;

	public string FileName { get; init; } = string.Empty;

	public string RelativePath { get; init; } = string.Empty;

	public string? FileHash { get; init; }

	public long FileSize { get; init; }

	public DateTime LastModified { get; init; }

	public bool Enabled { get; init; }

	public string? ModName { get; init; }

	public string? ModVersion { get; init; }

	public string? ModAuthor { get; init; }

	public string? ModDescription { get; init; }

	public string? ModLoader { get; init; }

	public string? ModDependencies { get; init; }

	public int CacheVersion { get; init; } = 7;

	public DateTime ScannedAt { get; init; }

	public string? ScanHash { get; init; }
}
