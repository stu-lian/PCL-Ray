using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject;

public sealed record InstalledModIdentity
{
	public string? SourceProjectId { get; init; }

	public string? Source { get; init; }

	public string? ModId { get; init; }

	public List<string> GameVersions { get; init; } = new List<string>();

	public List<string> Loaders { get; init; } = new List<string>();
}
