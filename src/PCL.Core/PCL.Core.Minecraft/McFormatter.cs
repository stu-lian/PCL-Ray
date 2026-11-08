using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.App.Localization;

namespace PCL.Core.Minecraft;

public static class McFormatter
{
	private static readonly Dictionary<string, string> _WikiPrefixMap = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["de"] = "de",
		["es"] = "es",
		["fr"] = "fr",
		["it"] = "it",
		["ja"] = "ja",
		["ko"] = "ko",
		["lzh"] = "lzh",
		["nl"] = "nl",
		["pt"] = "pt",
		["ru"] = "ru",
		["th"] = "th",
		["uk"] = "uk",
		["zh"] = "zh"
	};

	public static string GetWikiBaseUrl()
	{
		string langCode = LocalizationService.CurrentLanguage.Code;
		string text = _WikiPrefixMap.Where((KeyValuePair<string, string> kvp) =>
		{
			string text2 = langCode;
			KeyValuePair<string, string> keyValuePair = kvp;
			return text2.StartsWith(keyValuePair.Key, StringComparison.Ordinal);
		}).Select((KeyValuePair<string, string> kvp) =>
		{
			KeyValuePair<string, string> keyValuePair = kvp;
			return keyValuePair.Value;
		}).FirstOrDefault();
		if (text != null)
		{
			return "https://" + text + ".minecraft.wiki";
		}
		return "https://minecraft.wiki";
	}

	public static string GetWikiUrlSuffix(string gameVersion)
	{
		string text = FormatVersion(gameVersion);
		if (!LocalizationService.CurrentLanguage.Code.StartsWith("zh", StringComparison.Ordinal))
		{
			return "/w/Special:Search?search=" + Uri.EscapeDataString("Java Edition " + text);
		}
		if (gameVersion.Contains('w'))
		{
			return text;
		}
		return "Java版" + text;
	}

	public static string FormatVersion(string gameVersion)
	{
		string text = gameVersion.ToLowerInvariant();
		switch (text)
		{
		case "0.30-1":
		case "0.30-2":
		case "c0.30_01c":
			return "Classic_0.30";
		case "in-20100206-2103":
			return "Indev_20100206";
		case "inf-20100630-1":
			return "Infdev_20100630";
		case "inf-20100630-2":
			return "Alpha_v1.0.0";
		case "1.19_deep_dark_experimental_snapshot-1":
			return "1.19-exp1";
		case "in-20100130":
			return "Indev_0.31_20100130";
		case "b1.6-tb3":
			return "Beta_1.6_Test_Build_3";
		case "1_14_combat-212796":
			return "1.14.3_-_Combat_Test";
		case "1_14_combat-0":
			return "Combat_Test_2";
		case "1_14_combat-3":
			return "Combat_Test_3";
		case "1_15_combat-1":
			return "Combat_Test_4";
		case "1_15_combat-6":
			return "Combat_Test_5";
		case "1_16_combat-0":
			return "Combat_Test_6";
		case "1_16_combat-1":
			return "Combat_Test_7";
		case "1_16_combat-2":
			return "Combat_Test_7b";
		case "1_16_combat-3":
			return "Combat_Test_7c";
		case "1_16_combat-4":
			return "Combat_Test_8";
		case "1_16_combat-5":
			return "Combat_Test_8b";
		case "1_16_combat-6":
			return "Combat_Test_8c";
		default:
			if (text.StartsWith("1.0.0-rc2"))
			{
				return "RC2";
			}
			if (text.StartsWith("2.0") || text.StartsWith("2point0"))
			{
				return "2.0";
			}
			if (text.StartsWith("b1.8-pre1"))
			{
				return "Beta_1.8-pre1";
			}
			if (text.StartsWith("b1.1-"))
			{
				return "Beta_1.1";
			}
			if (text.StartsWith("a1.1.0"))
			{
				return "Alpha_v1.1.0";
			}
			if (text.StartsWith("a1.0.14"))
			{
				return "Alpha_v1.0.14";
			}
			if (text.StartsWith("a1.0.13_01"))
			{
				return "Alpha_v1.0.13_01";
			}
			if (text.StartsWith("in-20100214"))
			{
				return "Indev_20100214";
			}
			if (text.Contains("experimental-snapshot"))
			{
				return text.Replace("_experimental-snapshot-", "-exp");
			}
			if (text.StartsWith("inf-"))
			{
				return "Infdev_" + text.Substring(4);
			}
			if (text.StartsWith("in-"))
			{
				return "Indev_" + text.Substring(3);
			}
			if (text.StartsWith("rd-"))
			{
				return "pre-Classic_" + text;
			}
			if (text.StartsWith('b'))
			{
				return "Beta_" + text.Substring(1);
			}
			if (text.StartsWith('a'))
			{
				return "Alpha_v" + text.Substring(1);
			}
			if (!text.StartsWith('c'))
			{
				return text;
			}
			return ("Classic_" + text.Substring(1)).Replace("st", "SURVIVAL_TEST");
		}
	}
}
