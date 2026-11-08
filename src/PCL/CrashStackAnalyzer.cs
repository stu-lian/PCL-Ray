using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using PCL.Core.Logging;

namespace PCL;

internal sealed class CrashStackAnalyzer
{
	private static readonly HashSet<string> IgnoredPrefixes = new HashSet<string>(StringComparer.Ordinal)
	{
		"java", "sun", "javax", "jdk", "com.sun", "com.mojang", "net.minecraft", "MojangTricksIntelDriversForPerformance_javaw", "net.minecraftforge", "cpw.mods",
		"net.fabricmc", "org.quiltmc", "org.spongepowered", "com.mumfrey", "org.lwjgl", "paulscode.sound", "com.google", "org.apache", "com.electronwill.nightconfig", "it.unimi.dsi",
		"oolloo"
	};

	private static readonly HashSet<string> IgnoredWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"com", "org", "net", "top", "dev", "gitlab", "github", "sun", "lib", "nio",
		"api", "asm", "reflect", "internal", "microsoft", "mcp", "fml", "forge", "fabricmc", "neoforge",
		"neoforged", "minecraftforge", "minecraft", "mojang", "mod", "mods", "modapi", "jar", "loader", "launch",
		"preinit", "preload", "init", "setup", "plugin", "mixin", "mixins", "injection", "transformer", "transformers",
		"spongepowered", "game", "world", "server", "client", "common", "entity", "block", "item", "tile",
		"blockentity", "gui", "model", "render", "shader", "optifine", "config", "data", "file", "read",
		"recipe", "content", "general", "event", "events", "handler", "listeners", "assist", "override", "netty",
		"packet", "channel", "task", "pool", "scheduler", "systems", "system", "modules", "service", "platform",
		"core", "main", "base", "util", "impl", "done", "map", "load", "machine", "dsi",
		"unimi", "fastutil", "lwjgl", "oshi", "electronwill", "compat", "universal", "multipart"
	};

	public CrashFinding? Analyze(CrashLogSet logs, CrashModIndex modIndex)
	{
		if (!_ShouldAnalyzeStack(logs.All))
		{
			LogWrapper.Info("Crash", "可能并未安装 Mod，不进行堆栈分析");
			return null;
		}
		List<string> list = _ExtractKeywords(_ExtractPackages(_CollectStackText(logs)).ToList()).ToList();
		int count = list.Count;
		if ((count > 10 || count == 0) ? true : false)
		{
			if (list.Count > 10)
			{
				LogWrapper.Info("Crash", "关键词过多，考虑匹配出错，不纳入考虑");
			}
			return null;
		}
		List<CrashModInfo> list2 = modIndex.ResolveMany(list).ToList();
		if (list2.Count != 0)
		{
			return new CrashFinding(CrashCause.StackModNameFound, CrashConfidence.Medium, list2.Select((CrashModInfo mod) => new CrashEvidence
			{
				Source = CrashLogKind.All,
				Value = mod.DisplayNameOrFileName,
				DisplayKind = "mod"
			}))
			{
				ShouldStop = true
			};
		}
		return new CrashFinding(CrashCause.StackKeywordFound, CrashConfidence.Low, list.Select((string keyword) => new CrashEvidence
		{
			Source = CrashLogKind.All,
			Value = keyword,
			DisplayKind = "keyword"
		}))
		{
			ShouldStop = true
		};
	}

	private static bool _ShouldAnalyzeStack(string allLogs)
	{
		if (!allLogs.Contains("forge", StringComparison.OrdinalIgnoreCase) && !allLogs.Contains("fabric", StringComparison.OrdinalIgnoreCase) && !allLogs.Contains("quilt", StringComparison.OrdinalIgnoreCase))
		{
			return allLogs.Contains("liteloader", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string _CollectStackText(CrashLogSet logs)
	{
		List<string> list = new List<string>();
		if (logs.CrashReport?.Text != null)
		{
			LogWrapper.Info("Crash", "开始进行崩溃日志堆栈分析");
			list.Add(CrashText.BeforeFirst(logs.CrashReport.Text, "System Details"));
		}
		if (logs.Game?.Text != null)
		{
			List<string> list2 = CrashRegex.All(logs.Game.Text, "/FATAL] .+?(?=[\\n]+\\[)");
			if (logs.Game.Text.Contains("Unreported exception thrown!", StringComparison.Ordinal))
			{
				list2.Add(CrashText.Between(logs.Game.Text, "Unreported exception thrown!", "at oolloo.jlw.Wrapper"));
			}
			LogWrapper.Info("Crash", "开始进行 Minecraft 日志堆栈分析，发现 " + list2.Count + " 个报错项");
			list.AddRange(list2);
		}
		if (logs.HsErr?.Text != null)
		{
			LogWrapper.Info("Crash", "开始进行虚拟机堆栈分析");
			list.Add(CrashText.Between(logs.HsErr.Text, "T H R E A D", "Registers:"));
		}
		return "\n" + string.Join("\n", list) + "\n";
	}

	private static IEnumerable<string> _ExtractPackages(string stackText)
	{
		List<string> list = new List<string>();
		list.AddRange(from match in PackageRegex().Matches(stackText)
			select match.Value);
		list.AddRange(from match in MixinStackRegex().Matches(stackText)
			select match.Value.Replace("$", "."));
		List<string> list2 = (from item in (from item in list
				select item.Trim() into item
				where !string.IsNullOrWhiteSpace(item)
				select item).Distinct(StringComparer.Ordinal)
			where !IgnoredPrefixes.Any((string prefix) => item.StartsWith(prefix, StringComparison.Ordinal))
			select item).ToList();
		LogWrapper.Info("Crash", "找到 " + list2.Count + " 条可能的堆栈信息");
		foreach (string item in list2)
		{
			LogWrapper.Info("Crash", " - " + item);
		}
		return list2;
	}

	private static IEnumerable<string> _ExtractKeywords(IEnumerable<string> packages)
	{
		List<string> list = new List<string>();
		foreach (string package in packages)
		{
			string[] array = package.Split('.');
			for (int i = 0; i <= Math.Min(3, array.Length - 1); i++)
			{
				string text = array[i].Trim();
				if (text.Length > 2 && !text.StartsWith("func_", StringComparison.Ordinal) && !IgnoredWords.Contains(text))
				{
					list.Add(text);
				}
			}
		}
		List<string> list2 = list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		LogWrapper.Info("Crash", "从堆栈信息中找到 " + list2.Count + " 个可能的 Mod ID 关键词");
		if (list2.Count != 0)
		{
			LogWrapper.Info("Crash", " - " + string.Join(", ", list2));
		}
		return list2;
	}

	private static Regex PackageRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__PackageRegex_8.Instance;
	}

	private static Regex MixinStackRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__MixinStackRegex_9.Instance;
	}
}
