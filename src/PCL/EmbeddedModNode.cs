using System.Collections.Generic;

namespace PCL;

public class EmbeddedModNode
{
	public string FileName { get; set; }

	public string ModId { get; set; }

	public string Name { get; set; }

	public string Version { get; set; }

	public string Loader { get; set; }

	public string TargetMcVersion { get; set; }

	public List<EmbeddedDependency> DependencyRows { get; set; } = new List<EmbeddedDependency>();

	public List<EmbeddedConflict> Conflicts { get; set; } = new List<EmbeddedConflict>();

	public List<string> ProvidedIds { get; set; } = new List<string>();

	public Dictionary<string, string> ProvidedVersions { get; set; } = new Dictionary<string, string>();

	public string JijIdentifier { get; set; }

	public string JijVersionRange { get; set; }

	public string JijArtifactVersion { get; set; }

	public List<EmbeddedModNode> Children { get; set; } = new List<EmbeddedModNode>();
}
