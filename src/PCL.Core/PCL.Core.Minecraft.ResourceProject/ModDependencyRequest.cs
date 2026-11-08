using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject;

public sealed record ModDependencyRequest
{
	public string TargetMinecraftVersion { get; init; } = string.Empty;

	public List<string> TargetLoaders { get; init; } = new List<string>();

	public List<ModDependencyReference> RequiredDependencies { get; init; } = new List<ModDependencyReference>();

	public bool IncludeOptionalDependencies { get; init; }

	public List<InstalledModIdentity> InstalledMods { get; init; } = new List<InstalledModIdentity>();

	public Func<string, string, ModDependencyProject?> ProjectResolver { get; init; } = (string _, string _) => (ModDependencyProject?)null;
}
