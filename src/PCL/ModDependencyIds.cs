using System;
using System.Collections.Generic;

namespace PCL;

public static class ModDependencyIds
{
	private static readonly HashSet<string> _loaderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "forge", "neoforge", "fabricloader", "quilt", "quilt_loader", "java", "mcp" };

	public static bool IsLoaderId(string id)
	{
		return _loaderIds.Contains(id);
	}

	public static bool IsPlatform(string id)
	{
		if (!string.Equals(id, "minecraft", StringComparison.OrdinalIgnoreCase))
		{
			return _loaderIds.Contains(id);
		}
		return true;
	}
}
