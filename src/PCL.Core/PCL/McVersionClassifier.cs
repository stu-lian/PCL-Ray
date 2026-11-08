using System;
using System.Text.Json.Nodes;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public static class McVersionClassifier
{
	public static string GetCategoryDisplayName(McVersionCategory cat)
	{
		return cat switch
		{
			McVersionCategory.Release => Lang.Text("Download.Version.Type.Release"), 
			McVersionCategory.Snapshot => Lang.Text("Download.Version.Type.Development"), 
			McVersionCategory.BeforeRelease => Lang.Text("Download.Version.Type.BeforeRelease"), 
			McVersionCategory.AprilFools => Lang.Text("Download.Version.Type.AprilFools"), 
			_ => throw new ArgumentOutOfRangeException("cat"), 
		};
	}

	public static McVersionCategory ClassifyVersion(JsonObject version)
	{
		string text = _GetString(version, "type");
		string idLower = _GetString(version, "id").ToLowerInvariant();
		switch (text)
		{
		case "release":
			return McVersionCategory.Release;
		case "special":
			return _RefreshAprilFools(version, idLower);
		case "snapshot":
		case "pending":
			return _ClassifySnapshotOrPending(version, idLower);
		default:
			return McVersionCategory.BeforeRelease;
		}
	}

	private static McVersionCategory _RefreshAprilFools(JsonObject version, string idLower)
	{
		_TryMarkAprilFoolsVersion(version, idLower);
		return McVersionCategory.AprilFools;
	}

	public static DateTime GetReleaseTime(JsonObject version)
	{
		return _GetDateTime(version, "releaseTime");
	}

	private static McVersionCategory _ClassifySnapshotOrPending(JsonObject version, string idLower)
	{
		McVersionCategory result = McVersionCategory.Snapshot;
		if (idLower.StartsWith("1.") && !idLower.Contains("combat") && !idLower.Contains("rc") && !idLower.Contains("experimental") && idLower != "1.2" && !idLower.Contains("pre"))
		{
			result = McVersionCategory.Release;
			version["type"] = "release";
		}
		if (!_TryMarkAprilFoolsVersion(version, idLower))
		{
			return result;
		}
		return McVersionCategory.AprilFools;
	}

	private static bool _TryMarkAprilFoolsVersion(JsonObject version, string idLower)
	{
		switch (idLower)
		{
		case "2point0_red":
		case "2point0_purple":
		case "2point0_blue":
		case "2.0_blue":
		case "2.0_red":
		case "2.0_purple":
		case "2.0":
			version["id"] = _GetString(version, "id").Replace("point", ".");
			_MarkAsAprilFools(version, addLore: true);
			return true;
		case "20w14∞":
		case "20w14infinite":
			version["id"] = "20w14∞";
			_MarkAsAprilFools(version, addLore: true);
			return true;
		case "23w13a_or_b":
		case "24w14potato":
		case "25w14craftmine":
		case "15w14a":
		case "26w14a":
		case "3d shareware v1.34":
		case "1.rv-pre1":
		case "22w13oneblockatatime":
			_MarkAsAprilFools(version, addLore: true);
			return true;
		default:
		{
			DateTime dateTime = GetReleaseTime(version).ToUniversalTime().AddHours(2.0);
			if (dateTime.Month != 4 || dateTime.Day != 1)
			{
				return false;
			}
			_MarkAsAprilFools(version, addLore: false);
			return true;
		}
		}
	}

	private static void _MarkAsAprilFools(JsonObject version, bool addLore)
	{
		version["type"] = "special";
		if (addLore)
		{
			version["lore"] = GetMcFoolName(_GetString(version, "id"));
		}
	}

	public static string GetMcFoolName(string name)
	{
		name = name.ToLowerInvariant();
		string text = name;
		if (!name.StartsWith("2.0") && !name.StartsWith("2point0"))
		{
			switch (text)
			{
			case "15w14a":
				return Lang.Text("Minecraft.Fool.Description.2015");
			case "1.rv-pre1":
				return Lang.Text("Minecraft.Fool.Description.2016");
			case "3d shareware v1.34":
				return Lang.Text("Minecraft.Fool.Description.2019");
			default:
				if (!name.StartsWith("20w14inf") && !(name == "20w14∞"))
				{
					return text switch
					{
						"22w13oneblockatatime" => Lang.Text("Minecraft.Fool.Description.2022"), 
						"23w13a_or_b" => Lang.Text("Minecraft.Fool.Description.2023"), 
						"24w14potato" => Lang.Text("Minecraft.Fool.Description.2024"), 
						"25w14craftmine" => Lang.Text("Minecraft.Fool.Description.2025"), 
						"26w14a" => Lang.Text("Minecraft.Fool.Description.2026"), 
						_ => "", 
					};
				}
				return Lang.Text("Minecraft.Fool.Description.2020");
			}
		}
		string text2 = Lang.Text("Minecraft.Fool.Description.2013");
		string text3;
		if (name.EndsWith("red"))
		{
			text3 = Lang.Text("Minecraft.Fool.Tag.Red");
		}
		else if (name.EndsWith("blue"))
		{
			text3 = Lang.Text("Minecraft.Fool.Tag.Blue");
		}
		else
		{
			text3 = ((!name.EndsWith("purple")) ? "" : Lang.Text("Minecraft.Fool.Tag.Purple"));
		}
		return text2 + text3;
	}

	private static DateTime _GetDateTime(JsonObject obj, string key)
	{
		if (!JsonCompat.TryGetDateTime(obj[key], out var dateTime))
		{
			return DateTime.MinValue;
		}
		return dateTime;
	}

	private static string _GetString(JsonObject obj, string key)
	{
		JsonNode jsonNode = obj[key];
		if (jsonNode == null)
		{
			return "";
		}
		if (!(jsonNode is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
		{
			return jsonNode.ToString();
		}
		return value;
	}
}
