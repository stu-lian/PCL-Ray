namespace PCL.Core.Minecraft.ResourceProject;

public sealed record IgnoredDependency
{
	public string ProjectId { get; init; } = string.Empty;

	public string Source { get; init; } = string.Empty;

	public string Reason { get; init; } = string.Empty;
}
