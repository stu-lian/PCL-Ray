using System;
using System.IO;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Java;

public sealed record JavaInstallation(string JavaFolder, Version Version, JavaBrandType Brand, MachineType Architecture, bool Is64Bit, bool IsJre)
{
	public string JavaExePath => Path.Combine(JavaFolder, "java.exe");

	public string? JavawExePath
	{
		get
		{
			string text = Path.Combine(JavaFolder, "javaw.exe");
			if (!File.Exists(text))
			{
				return null;
			}
			return text;
		}
	}

	public int MajorVersion
	{
		get
		{
			if (Version.Major != 1)
			{
				return Version.Major;
			}
			return Version.Minor;
		}
	}

	public bool IsStillAvailable => File.Exists(JavaExePath);

	public override string ToString()
	{
		return $"{(IsJre ? "JRE" : "JDK")} {MajorVersion} {Brand} {(Is64Bit ? "64 Bit" : "32 Bit")} | {JavaFolder}";
	}

	public string ToDetailedString()
	{
		return $"{(IsJre ? "JRE" : "JDK")} {Version} {Brand} {(Is64Bit ? "64 Bit" : "32 Bit")} | {JavaFolder}";
	}
}
