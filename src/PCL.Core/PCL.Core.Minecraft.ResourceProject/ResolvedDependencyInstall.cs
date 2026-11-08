namespace PCL.Core.Minecraft.ResourceProject;

public sealed record ResolvedDependencyInstall
{
	public string ProjectId { get; init; } = string.Empty;

	public string Source { get; init; } = string.Empty;

	public string? ProjectName { get; init; }

	public ModDependencyFile File { get; init; } = new ModDependencyFile();

	public bool IsOptional { get; init; }
}
