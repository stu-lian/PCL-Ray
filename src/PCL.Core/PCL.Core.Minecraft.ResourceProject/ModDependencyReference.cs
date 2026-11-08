namespace PCL.Core.Minecraft.ResourceProject;

public sealed record ModDependencyReference
{
	public string ProjectId { get; init; } = string.Empty;

	public string Source { get; init; } = string.Empty;

	public bool IsRequired { get; init; } = true;
}
