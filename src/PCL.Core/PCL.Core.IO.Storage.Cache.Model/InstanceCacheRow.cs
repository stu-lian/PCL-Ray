using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PCL.Core.IO.Storage.Cache.Model;

public record InstanceCacheRow
{
	public string InstancePath { get; init; } = string.Empty;

	public string InstanceName { get; init; } = string.Empty;

	public string InstanceState { get; init; } = "Error";

	public int CardType { get; init; }

	public bool IsStarred { get; init; }

	public string? Logo { get; init; }

	public string? Description { get; init; }

	public string? ReleaseTime { get; init; }

	public string? VanillaName { get; init; }

	public string? VanillaVersion { get; init; }

	public int DropNumber { get; init; }

	public bool Reliable { get; init; }

	public string LoaderJson { get; internal set; } = "[]";

	public string? MainClass { get; init; }

	public string? AssetsIndex { get; init; }

	public string? InheritsFrom { get; init; }

	public int? JavaVersion { get; init; }

	public string SourceJsonHash { get; init; } = string.Empty;

	public int FormatVersion { get; init; } = 1;

	public DateTime CachedAt { get; init; }

	public DateTime? LastLoadedAt { get; init; }

	public List<LoaderEntry> GetLoaders()
	{
		return JsonSerializer.Deserialize<List<LoaderEntry>>(LoaderJson) ?? new List<LoaderEntry>();
	}

	public bool HasLoader(string type)
	{
		return GetLoaders().Any((LoaderEntry l) => l.Type == type);
	}

	public string? GetLoaderVersion(string type)
	{
		return GetLoaders().FirstOrDefault((LoaderEntry l) => l.Type == type)?.Version;
	}

	public void SetLoaders(List<LoaderEntry> loaders)
	{
		LoaderJson = JsonSerializer.Serialize(loaders);
	}
}
