using System;
using System.IO;

namespace PCL.Core.Utils.OS;

public static class SystemPaths
{
	public static string DriveLetter { get; } = Path.GetPathRoot(Environment.SystemDirectory);
}
