using System.IO;
using System.Linq;

namespace PCL.Core.Minecraft;

public static class SaveImportHelper
{
	public static string? GetSaveRootDirectory(string extractedDirectory)
	{
		if (string.IsNullOrWhiteSpace(extractedDirectory) || !Directory.Exists(extractedDirectory))
		{
			return null;
		}
		string fullPath = Path.GetFullPath(extractedDirectory);
		if (File.Exists(Path.Combine(fullPath, "level.dat")))
		{
			return fullPath;
		}
		string[] directories = Directory.GetDirectories(fullPath);
		if (directories.Length != 1)
		{
			return null;
		}
		string fullPath2 = Path.GetFullPath(directories.Single());
		if (!File.Exists(Path.Combine(fullPath2, "level.dat")))
		{
			return null;
		}
		return fullPath2;
	}
}
