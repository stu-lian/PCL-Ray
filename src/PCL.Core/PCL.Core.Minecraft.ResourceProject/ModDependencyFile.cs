using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject;

public sealed record ModDependencyFile
{
	public string Id { get; init; } = string.Empty;

	public string DisplayName { get; init; } = string.Empty;

	public string? Version { get; init; }

	public List<string> GameVersions { get; init; } = new List<string>();

	public List<string> Loaders { get; init; } = new List<string>();

	public int ReleaseType { get; init; }

	public DateTime ReleaseDate { get; init; }

	public List<ModDependencyReference> RequiredDependencies { get; init; } = new List<ModDependencyReference>();
}
