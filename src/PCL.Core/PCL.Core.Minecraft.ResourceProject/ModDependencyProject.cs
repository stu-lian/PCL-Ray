using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject;

public sealed record ModDependencyProject
{
	public string ProjectId { get; init; } = string.Empty;

	public string Source { get; init; } = string.Empty;

	public string? ProjectName { get; init; }

	public List<ModDependencyFile> Files { get; init; } = new List<ModDependencyFile>();
}
