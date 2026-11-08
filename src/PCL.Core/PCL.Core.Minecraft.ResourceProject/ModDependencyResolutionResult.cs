using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject;

public sealed record ModDependencyResolutionResult
{
	public List<ResolvedDependencyInstall> ToInstall { get; } = new List<ResolvedDependencyInstall>();

	public List<UnresolvedDependency> Unresolved { get; } = new List<UnresolvedDependency>();

	public List<IgnoredDependency> Satisfied { get; } = new List<IgnoredDependency>();
}
