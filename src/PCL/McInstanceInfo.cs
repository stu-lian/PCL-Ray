using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.App.Localization;

namespace PCL;

public class McInstanceInfo
{
	public string Cleanroom = "";

	public string Fabric = "";

	public string Forge = "";

	public bool HasCleanroom;

	public bool HasFabric;

	public bool HasForge;

	public bool HasLabyMod;

	public bool HasLegacyFabric;

	public bool HasLiteLoader;

	public bool HasNeoForge;

	public bool HasOptiFine;

	public bool HasQuilt;

	public string LabyMod = "";

	public string LegacyFabric = "";

	public string NeoForge = "";

	public string OptiFine = "";

	public string Quilt = "";

	public bool Reliable = true;

	public Version vanilla;

	public string VanillaName;

	public bool Valid => vanilla.Major < 1000;

	public int Drop
	{
		get
		{
			if (!Valid)
			{
				return 209;
			}
			return vanilla.Major * 10 + vanilla.Minor;
		}
	}

	public int OptiFineCode
	{
		get
		{
			if (string.IsNullOrEmpty(OptiFine) || OptiFine == Lang.Text("Minecraft.Version.Unknown"))
			{
				return 0;
			}
			int num = char.ToUpperInvariant(OptiFine.First()) - 65 + 1;
			num *= 100;
			num = (int)Math.Round((double)num + ModBase.Val(OptiFine.Substring(1).RegexSeek("[0-9]+")));
			num *= 100;
			if (OptiFine.ContainsF("pre", ignoreCase: true))
			{
				num += 50;
			}
			if (OptiFine.ContainsF("pre", ignoreCase: true) || OptiFine.ContainsF("beta", ignoreCase: true))
			{
				string optiFine = OptiFine;
				string text = optiFine.Substring(optiFine.Length - 1);
				if (ModBase.Val(text) == 0.0 && text != "0")
				{
					return num + 1;
				}
				return (int)Math.Round((double)num + ModBase.Val(OptiFine.ToLower().RegexSeek("(?<=((pre)|(beta)))[0-9]+")));
			}
			return num + 99;
		}
	}

	public bool HasForgelike
	{
		get
		{
			if (!HasForge && !HasNeoForge)
			{
				return HasCleanroom;
			}
			return true;
		}
	}

	public int ForgelikeCode
	{
		get
		{
			if (!HasForgelike)
			{
				return 0;
			}
			if ((string.IsNullOrEmpty(Forge) || Forge == Lang.Text("Minecraft.Version.Unknown")) && (string.IsNullOrEmpty(NeoForge) || NeoForge == Lang.Text("Minecraft.Version.Unknown")))
			{
				return 0;
			}
			List<string> list = (HasForge ? Forge : NeoForge).RegexSearch("\\d+");
			int count = list.Count;
			if (count <= 4)
			{
				return count switch
				{
					3 => (int)Math.Round(ModBase.Val(list[0]) * 1000000.0 + ModBase.Val(list[1]) * 10000.0 + ModBase.Val(list[2])), 
					2 => (int)Math.Round(ModBase.Val(list[0]) * 1000000.0 + ModBase.Val(list[1]) * 10000.0), 
					_ => (int)Math.Round(ModBase.Val(list[0]) * 1000000.0), 
				};
			}
			return (int)Math.Round(ModBase.Val(list[0]) * 1000000.0 + ModBase.Val(list[1]) * 10000.0 + ModBase.Val(list[3]));
		}
	}

	public bool HasFabriclike
	{
		get
		{
			if (!HasFabric && !HasQuilt)
			{
				return HasLegacyFabric;
			}
			return true;
		}
	}

	public override string ToString()
	{
		string text = null;
		text = "";
		if (HasForge)
		{
			text = text + ", Forge" + ((Forge == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Forge));
		}
		if (HasNeoForge)
		{
			text = text + ", NeoForge" + ((NeoForge == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + NeoForge));
		}
		if (HasCleanroom)
		{
			text = text + ", Cleanroom" + ((Cleanroom == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Cleanroom));
		}
		if (HasFabric)
		{
			text = text + ", Fabric" + ((Fabric == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Fabric));
		}
		if (HasLegacyFabric)
		{
			text = text + ", LegacyFabric" + ((LegacyFabric == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + LegacyFabric));
		}
		if (HasQuilt)
		{
			text = text + ", Quilt" + ((Quilt == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Quilt));
		}
		if (HasLabyMod)
		{
			text = text + ", LabyMod" + ((LabyMod == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + LabyMod));
		}
		if (HasOptiFine)
		{
			text = text + ", OptiFine" + ((OptiFine == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + OptiFine));
		}
		if (HasLiteLoader)
		{
			text += ", LiteLoader";
		}
		if (string.IsNullOrEmpty(text))
		{
			return Lang.Text("Minecraft.Version.Vanilla") + " " + VanillaName;
		}
		return VanillaName + text;
	}

	public static bool IsFormatFit(string version)
	{
		if (version == null)
		{
			return false;
		}
		if (version.RegexCheck("^1\\.\\d"))
		{
			return true;
		}
		if (ModBase.Val(version.RegexSeek("^[2-9]\\d\\.\\d+")) > 25.0)
		{
			return true;
		}
		return false;
	}

	public static int VersionToDrop(string? version, bool allowSnapshot = false)
	{
		if (string.IsNullOrEmpty(version))
		{
			return -1;
		}
		string text = version.ToLowerInvariant();
		if (text.StartsWith("1.rv"))
		{
			return 90;
		}
		if (text.StartsWith("3d shareware"))
		{
			return 140;
		}
		if (!allowSnapshot && text.Contains('-'))
		{
			return -1;
		}
		string[] array = text.BeforeFirst("-").Split('.');
		if (allowSnapshot && array.Length == 1 && array[0].Length >= 3 && array[0][2] == 'w' && char.IsDigit(array[0][0]) && char.IsDigit(array[0][1]))
		{
			int num = int.Parse(array[0].Substring(0, 2));
			if (num <= 16)
			{
				return Math.Max(num * 10 - 70, 20);
			}
			if (num == 17)
			{
				return 120;
			}
			return Math.Min(130 + (num - 18) * 10, 210);
		}
		if (array.Length < 2)
		{
			return -1;
		}
		int num2 = (int)Math.Round(ModBase.Val(array[0]));
		int num3 = (int)Math.Round(ModBase.Val(array[1]));
		if (num2 == 1)
		{
			return num3 * 10;
		}
		if (num2 >= 25)
		{
			return num2 * 10 + num3;
		}
		if (num2 == 2)
		{
			return 50;
		}
		return -1;
	}

	public static string DropToVersion(int drop)
	{
		if (drop < 250)
		{
			return $"1.{drop / 10}";
		}
		return $"{drop / 10}.{drop % 10}";
	}
}
