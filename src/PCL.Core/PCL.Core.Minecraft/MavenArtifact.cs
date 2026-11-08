using System;

namespace PCL.Core.Minecraft;

public class MavenArtifact(string mavenId)
{
	public string Resolve(string uriOrPath)
	{
		return uriOrPath.TrimEnd('/') + _GetMavenPath(mavenId);
	}

	private static string _GetMavenPath(string packageId)
	{
		string[] array = packageId.Split(":");
		switch (array.Length)
		{
		case 3:
			return $"/{array[0].Replace(".", "/")}/{array[1]}/{array[1]}-{array[2]}.jar";
		case 4:
			if (!_IsCommonPackaging(array[2]))
			{
				return $"/{array[0].Replace(".", "/")}/{packageId[1]}/{array[1]}-{array[2]}-{array[3]}.jar";
			}
			return $"/{array[0].Replace(".", "/")}/{array[1]}/{array[1]}-{array[3]}.{array[2]}";
		case 5:
			return $"{array[0].Replace(".", "/")}/{array[1]}/{array[1]}-{array[3]}-{packageId[4]}.{packageId[2]}";
		default:
			throw new FormatException($"Invalid maven package id: Length is {array.Length}");
		}
	}

	private static bool _IsCommonPackaging(string name)
	{
		if (!(name == "jar") && !(name == "zip"))
		{
			return name == "pom";
		}
		return true;
	}
}
