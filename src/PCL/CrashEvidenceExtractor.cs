using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PCL;

internal sealed class CrashEvidenceExtractor
{
	public IEnumerable<CrashFinding> Extract(DetectionPhase phase, CrashLogSet logs, CrashModIndex modIndex)
	{
		return phase switch
		{
			DetectionPhase.Fatal => _ExtractFatal(logs, modIndex), 
			DetectionPhase.Primary => _ExtractPrimary(logs, modIndex), 
			DetectionPhase.Secondary => _ExtractSecondary(logs, modIndex), 
			_ => Array.Empty<CrashFinding>(), 
		};
	}

	private static IEnumerable<CrashFinding> _ExtractFatal(CrashLogSet logs, CrashModIndex modIndex)
	{
		foreach (CrashFinding item in _ExtractOutOfMemory(logs))
		{
			yield return item;
		}
		CrashFinding crashFinding = _ExtractJvmMemoryReservation(logs);
		if (crashFinding != null)
		{
			yield return crashFinding;
		}
		foreach (CrashFinding item2 in _ExtractAccessViolation(logs))
		{
			yield return item2;
		}
		CrashFinding crashFinding2 = _ExtractSignerValidationFailure(logs);
		if (crashFinding2 != null)
		{
			yield return crashFinding2;
		}
		CrashFinding crashFinding3 = _ExtractOptiFineMissingMods(logs);
		if (crashFinding3 != null)
		{
			yield return crashFinding3;
		}
		foreach (CrashFinding item3 in _ExtractConfirmedModCrash(logs, modIndex))
		{
			yield return item3;
		}
		foreach (CrashFinding item4 in _ExtractDuplicateMods(logs))
		{
			yield return item4;
		}
		foreach (CrashFinding item5 in _ExtractMissingDependency(logs))
		{
			yield return item5;
		}
	}

	private static IEnumerable<CrashFinding> _ExtractPrimary(CrashLogSet logs, CrashModIndex modIndex)
	{
		foreach (CrashFinding item in _ExtractMixinFailure(logs, modIndex))
		{
			yield return item;
		}
		foreach (CrashFinding item2 in _ExtractForgeError(logs))
		{
			yield return item2;
		}
		foreach (CrashFinding item3 in _ExtractFabricSolution(logs))
		{
			yield return item3;
		}
		foreach (CrashFinding item4 in _ExtractFabricProvidedModCrash(logs, modIndex))
		{
			yield return item4;
		}
		foreach (CrashFinding item5 in _ExtractSuspectedMods(logs, modIndex))
		{
			yield return item5;
		}
	}

	private static IEnumerable<CrashFinding> _ExtractSecondary(CrashLogSet logs, CrashModIndex modIndex)
	{
		CrashFinding crashFinding = _ExtractShortOutput(logs);
		if (crashFinding != null)
		{
			yield return crashFinding;
		}
		CrashFinding crashFinding2 = _ExtractModLoaderError(logs);
		if (crashFinding2 != null)
		{
			yield return crashFinding2;
		}
		CrashFinding crashFinding3 = _ExtractModInitializationFailure(logs, modIndex);
		if (crashFinding3 != null)
		{
			yield return crashFinding3;
		}
		foreach (CrashFinding item in _ExtractSpecificBlockAndEntity(logs))
		{
			yield return item;
		}
	}

	private static IEnumerable<CrashFinding> _ExtractOutOfMemory(CrashLogSet logs)
	{
		List<CrashEvidence> list = _FindPatterns(logs, new _003C_003Ez__ReadOnlyArray<(CrashLogKind, string)>(new (CrashLogKind, string)[5]
		{
			(CrashLogKind.Game, "java.lang.OutOfMemoryError"),
			(CrashLogKind.Game, "an out of memory error"),
			(CrashLogKind.HsErr, "The system is out of physical RAM or swap space"),
			(CrashLogKind.HsErr, "Out of Memory Error"),
			(CrashLogKind.CrashReport, "java.lang.OutOfMemoryError")
		})).ToList();
		if (list.Count != 0)
		{
			yield return new CrashFinding(CrashCause.OutOfMemory, CrashConfidence.High, list)
			{
				ShouldStop = true
			};
		}
	}

	private static CrashFinding? _ExtractJvmMemoryReservation(CrashLogSet logs)
	{
		string text = logs.Game?.Text;
		if (string.IsNullOrEmpty(text) || !text.Contains("Could not reserve enough space", StringComparison.Ordinal))
		{
			return null;
		}
		return new CrashFinding(text.Contains("for 1048576KB object heap", StringComparison.Ordinal) ? CrashCause.X86JavaMemoryLimit : CrashCause.OutOfMemory, CrashConfidence.High, new _003C_003Ez__ReadOnlySingleElementList<CrashEvidence>(_Evidence("Could not reserve enough space", CrashLogKind.Game, "jvm-message")))
		{
			ShouldStop = true
		};
	}

	private static IEnumerable<CrashFinding> _ExtractAccessViolation(CrashLogSet logs)
	{
		string hsLog = logs.HsErr?.Text;
		if (!string.IsNullOrEmpty(hsLog) && hsLog.Contains("EXCEPTION_ACCESS_VIOLATION", StringComparison.Ordinal))
		{
			if (hsLog.Contains("# C  [ig", StringComparison.Ordinal))
			{
				yield return _DriverFinding(CrashCause.IntelDriverAccessViolation, "# C  [ig");
			}
			if (hsLog.Contains("# C  [atio", StringComparison.Ordinal))
			{
				yield return _DriverFinding(CrashCause.AmdDriverAccessViolation, "# C  [atio");
			}
			if (hsLog.Contains("# C  [nvoglv", StringComparison.Ordinal))
			{
				yield return _DriverFinding(CrashCause.NvidiaDriverAccessViolation, "# C  [nvoglv");
			}
		}
	}

	private static CrashFinding _DriverFinding(CrashCause cause, string pattern)
	{
		return new CrashFinding(cause, CrashConfidence.High, new _003C_003Ez__ReadOnlySingleElementList<CrashEvidence>(_Evidence(pattern, CrashLogKind.HsErr, "driver-signature")))
		{
			ShouldStop = true
		};
	}

	private static CrashFinding? _ExtractSignerValidationFailure(CrashLogSet logs)
	{
		string text = logs.Game?.Text;
		if (string.IsNullOrEmpty(text) || !text.Contains("signer information does not match signer information of other classes in the same package", StringComparison.Ordinal))
		{
			return null;
		}
		string value = (CrashRegex.First(text, "(?<=class \")[^']+(?=\"'s signer information)") ?? "").TrimEnd(new char[2] { '\r', '\n' });
		return new CrashFinding(CrashCause.FileOrContentValidationFailed, CrashConfidence.High, new _003C_003Ez__ReadOnlySingleElementList<CrashEvidence>(_Evidence(value, CrashLogKind.Game, "class")))
		{
			ShouldStop = true
		};
	}

	private static CrashFinding? _ExtractOptiFineMissingMods(CrashLogSet logs)
	{
		string text = logs.CrashReport?.Text;
		if (string.IsNullOrEmpty(text) || !text.Contains("has mods that were not found", StringComparison.Ordinal) || !CrashRegex.IsMatch(text, "The Mod File [^\\n]+optifine\\\\OptiFine[^\\n]+ has mods that were not found"))
		{
			return null;
		}
		return new CrashFinding(CrashCause.OptiFineForgeIncompatible, CrashConfidence.High, new _003C_003Ez__ReadOnlySingleElementList<CrashEvidence>(_Evidence("OptiFine", CrashLogKind.CrashReport, "mod")))
		{
			ShouldStop = true
		};
	}

	private static IEnumerable<CrashFinding> _ExtractConfirmedModCrash(CrashLogSet logs, CrashModIndex modIndex)
	{
		string text = logs.Game?.Text;
		if (!string.IsNullOrEmpty(text) && text.Contains("Caught exception from ", StringComparison.Ordinal))
		{
			string text2 = CrashRegex.First(text, "(?<=Caught exception from )[^\\n]+?")?.TrimEnd(new char[3] { '\r', '\n', ' ' });
			yield return _ModFinding(CrashCause.ConfirmedModCrash, modIndex.ResolveToDisplayNames(new _003C_003Ez__ReadOnlySingleElementList<string>(text2 ?? "")), CrashLogKind.Game, shouldStop: true);
		}
		string crash = logs.CrashReport?.Text;
		if (string.IsNullOrEmpty(crash))
		{
			yield break;
		}
		if (crash.Contains("-- MOD ", StringComparison.Ordinal))
		{
			string text3 = CrashText.Between(crash, "-- MOD ", "Failure message:");
			if (text3.Contains(".jar", StringComparison.OrdinalIgnoreCase))
			{
				string item = (CrashRegex.First(text3, "(?<=Mod File: ).+") ?? "").TrimEnd(new char[3] { '\r', '\n', ' ' });
				yield return _ModFinding(CrashCause.ConfirmedModCrash, new _003C_003Ez__ReadOnlySingleElementList<string>(item), CrashLogKind.CrashReport, shouldStop: true);
			}
			else
			{
				string item2 = (CrashRegex.First(crash, "(?<=Failure message: )[\\w\\W]+?(?=\\tMod)") ?? "").Replace("\t", " ").TrimEnd(new char[3] { '\r', '\n', ' ' });
				yield return _ModFinding(CrashCause.ModLoaderError, new _003C_003Ez__ReadOnlySingleElementList<string>(item2), CrashLogKind.CrashReport, shouldStop: true, "loader-message");
			}
		}
		if (crash.Contains("Multiple entries with same key: ", StringComparison.Ordinal))
		{
			string item3 = (CrashRegex.First(crash, "(?<=Multiple entries with same key: )[^=]+") ?? "").TrimEnd(new char[3] { '\r', '\n', ' ' });
			yield return _ModFinding(CrashCause.ConfirmedModCrash, modIndex.ResolveToDisplayNames(new _003C_003Ez__ReadOnlySingleElementList<string>(item3)), CrashLogKind.CrashReport, shouldStop: true);
		}
		if (crash.Contains("LoaderExceptionModCrash: Caught exception from ", StringComparison.Ordinal))
		{
			string item4 = (CrashRegex.First(crash, "(?<=LoaderExceptionModCrash: Caught exception from )[^\\n]+") ?? "").TrimEnd(new char[3] { '\r', '\n', ' ' });
			yield return _ModFinding(CrashCause.ConfirmedModCrash, modIndex.ResolveToDisplayNames(new _003C_003Ez__ReadOnlySingleElementList<string>(item4)), CrashLogKind.CrashReport, shouldStop: true);
		}
		if (crash.Contains("Failed loading config file ", StringComparison.Ordinal))
		{
			string item5 = (CrashRegex.First(crash, "(?<=Failed loading config file .+ for modid )[^\\n]+") ?? "").TrimEnd(new char[2] { '\r', '\n' });
			string value = (CrashRegex.First(crash, "(?<=Failed loading config file ).+(?= of type)") ?? "").TrimEnd(new char[2] { '\r', '\n' });
			List<string> source = modIndex.ResolveToDisplayNames(new _003C_003Ez__ReadOnlySingleElementList<string>(item5));
			List<CrashEvidence> list = new List<CrashEvidence>();
			list.AddRange(source.Select((string value2) => _Evidence(value2, CrashLogKind.CrashReport, "mod")));
			if (!string.IsNullOrWhiteSpace(value))
			{
				list.Add(_Evidence(value, CrashLogKind.CrashReport, "config"));
			}
			yield return new CrashFinding(CrashCause.ModConfigCrash, CrashConfidence.High, list)
			{
				ShouldStop = true
			};
		}
	}

	private static IEnumerable<CrashFinding> _ExtractDuplicateMods(CrashLogSet logs)
	{
		string gameLog = logs.Game?.Text;
		if (!string.IsNullOrEmpty(gameLog))
		{
			if (gameLog.Contains("DuplicateModsFoundException", StringComparison.Ordinal))
			{
				yield return _ModFinding(CrashCause.DuplicateMods, CrashRegex.All(gameLog, "(?<=\\n\\t[\\w]+ : [A-Z]:[^\\n]+(/|\\\\))[^/\\\\\\n]+?.jar", RegexOptions.IgnoreCase), CrashLogKind.Game, shouldStop: true);
			}
			if (gameLog.Contains("Found a duplicate mod", StringComparison.Ordinal))
			{
				yield return _ModFinding(CrashCause.DuplicateMods, CrashRegex.All(CrashRegex.First(gameLog, "Found a duplicate mod[^\\n]+") ?? "", "[^\\\\/]+.jar", RegexOptions.IgnoreCase), CrashLogKind.Game, shouldStop: true);
			}
			if (gameLog.Contains("Found duplicate mods", StringComparison.Ordinal))
			{
				yield return _ModFinding(CrashCause.DuplicateMods, CrashRegex.All(gameLog, "(?<=Mod ID: ')\\w+?(?=' from mod files:)").Distinct().ToList(), CrashLogKind.Game, shouldStop: true);
			}
			if (gameLog.Contains("ModResolutionException: Duplicate", StringComparison.Ordinal))
			{
				yield return _ModFinding(CrashCause.DuplicateMods, CrashRegex.All(CrashRegex.First(gameLog, "ModResolutionException: Duplicate[^\\n]+") ?? "", "[^\\\\/]+.jar", RegexOptions.IgnoreCase), CrashLogKind.Game, shouldStop: true);
			}
		}
	}

	private static IEnumerable<CrashFinding> _ExtractMissingDependency(CrashLogSet logs)
	{
		string gameLog = logs.Game?.Text;
		if (string.IsNullOrEmpty(gameLog))
		{
			yield break;
		}
		if (gameLog.Contains("Incompatible mods found!", StringComparison.Ordinal))
		{
			yield return _ModFinding(CrashCause.IncompatibleMods, new _003C_003Ez__ReadOnlySingleElementList<string>(CrashRegex.First(gameLog, "(?<=Incompatible mods found![\\s\\S]+: )[\\s\\S]+?(?=\\tat )") ?? ""), CrashLogKind.Game, shouldStop: true, "loader-message");
		}
		if (gameLog.Contains("Missing or unsupported mandatory dependencies:", StringComparison.Ordinal))
		{
			List<string> values = (from item in CrashRegex.All(gameLog, "(?<=Missing or unsupported mandatory dependencies:)([\\n\\r]+\\t(.*))+", RegexOptions.IgnoreCase)
				select item.Trim(new char[4] { '\r', '\n', '\t', ' ' })).Distinct().ToList();
			yield return _ModFinding(CrashCause.MissingDependencyOrWrongMcVersion, values, CrashLogKind.Game, shouldStop: true, "loader-message");
		}
	}

	private static IEnumerable<CrashFinding> _ExtractMixinFailure(CrashLogSet logs, CrashModIndex modIndex)
	{
		return from source in new CrashLogKind[2]
			{
				CrashLogKind.Game,
				CrashLogKind.CrashReport
			}
			let text = logs.GetText(source)
			where !string.IsNullOrEmpty(text) && _LooksLikeMixinFailure(text)
			let modHints = _ExtractMixinModHints(text).Distinct(StringComparer.Ordinal).ToList()
			let resolved = modIndex.ResolveToDisplayNames(modHints)
			let hasResolved = modHints.Count > 0 && !modHints.SequenceEqual(resolved)
			let evidence = ((resolved.Count != 0) ? resolved : modHints).Select((string item) => _Evidence(item, source, hasResolved ? "mod" : "mod-hint"))
			select new CrashFinding(CrashCause.ModMixinFailed, (!hasResolved) ? CrashConfidence.Medium : CrashConfidence.High, evidence)
			{
				ShouldStop = true
			};
	}

	private static bool _LooksLikeMixinFailure(string text)
	{
		if (!text.Contains("Mixin prepare failed ", StringComparison.Ordinal) && !text.Contains("Mixin apply failed ", StringComparison.Ordinal) && !text.Contains("MixinApplyError", StringComparison.Ordinal) && !text.Contains("MixinTransformerError", StringComparison.Ordinal) && !text.Contains("mixin.injection.throwables.", StringComparison.Ordinal))
		{
			return text.Contains(".json] FAILED during )", StringComparison.Ordinal);
		}
		return true;
	}

	private static IEnumerable<string> _ExtractMixinModHints(string text)
	{
		Regex[] array = new Regex[3]
		{
			new Regex("(?<=from mod )[^.\\/ ]+(?=\\] from)", RegexOptions.Compiled),
			new Regex("(?<=for mod )[^.\\/ ]+(?= failed)", RegexOptions.Compiled),
			new Regex("(?<=^[^\\t]+[ \\[{(]{1})[^ \\[{(]+\\.[^ ]+(?=\\.json)", RegexOptions.Multiline | RegexOptions.Compiled)
		};
		Regex[] array2 = array;
		foreach (Regex regex in array2)
		{
			foreach (Match item in regex.Matches(text))
			{
				string text2 = item.Value.Replace("mixins", "mixin").Replace(".mixin", "").Replace("mixin.", "")
					.Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					yield return text2;
				}
			}
		}
	}

	private static IEnumerable<CrashFinding> _ExtractForgeError(CrashLogSet logs)
	{
		string text = logs.Game?.Text;
		if (!string.IsNullOrEmpty(text) && text.Contains("An exception was thrown, the game will display an error screen and halt.", StringComparison.Ordinal))
		{
			string item = CrashRegex.First(text, "(?<=the game will display an error screen and halt.[\\n\\r]+[^\\n]+?Exception: )[\\s\\S]+?(?=\\n\\tat)")?.Trim(new char[2] { '\r', '\n' }) ?? "";
			yield return _ModFinding(CrashCause.ForgeError, new _003C_003Ez__ReadOnlySingleElementList<string>(item), CrashLogKind.Game, shouldStop: true, "loader-message");
		}
	}

	private static IEnumerable<CrashFinding> _ExtractFabricSolution(CrashLogSet logs)
	{
		string text = logs.Game?.Text;
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = "";
			if (text.Contains("A potential solution has been determined:", StringComparison.Ordinal))
			{
				text2 = string.Join("\n", CrashRegex.All(CrashRegex.First(text, "(?<=A potential solution has been determined:\\n)(\\s+ - [^\\n]+\\n)+") ?? "", "(?<=\\s+)[^\\n]+"));
			}
			else if (text.Contains("A potential solution has been determined, this may resolve your problem:", StringComparison.Ordinal))
			{
				text2 = string.Join("\n", CrashRegex.All(CrashRegex.First(text, "(?<=A potential solution has been determined, this may resolve your problem:\\n)(\\s+ - [^\\n]+\\n)+") ?? "", "(?<=\\s+)[^\\n]+"));
			}
			else if (text.Contains("确定了一种可能的解决方法，这样做可能会解决你的问题：", StringComparison.Ordinal))
			{
				text2 = string.Join("\n", CrashRegex.All(CrashRegex.First(text, "(?<=确定了一种可能的解决方法，这样做可能会解决你的问题：\\n)(\\s+ - [^\\n]+\\n)+") ?? "", "(?<=\\s+)[^\\n]+"));
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				yield return _ModFinding(CrashCause.FabricSolutionProvided, new _003C_003Ez__ReadOnlySingleElementList<string>(text2), CrashLogKind.Game, shouldStop: true, "loader-message");
			}
		}
	}

	private static IEnumerable<CrashFinding> _ExtractFabricProvidedModCrash(CrashLogSet logs, CrashModIndex modIndex)
	{
		string text = logs.Game?.Text;
		if (!string.IsNullOrEmpty(text) && text.Contains("due to errors, provided by ", StringComparison.Ordinal) && !_LooksLikeMixinFailure(text))
		{
			string item = (CrashRegex.First(text, "(?<=due to errors, provided by ')[^']+") ?? "").TrimEnd(new char[3] { '\r', '\n', ' ' });
			yield return _ModFinding(CrashCause.ConfirmedModCrash, modIndex.ResolveToDisplayNames(new _003C_003Ez__ReadOnlySingleElementList<string>(item)), CrashLogKind.Game, shouldStop: true);
		}
	}

	private static IEnumerable<CrashFinding> _ExtractSuspectedMods(CrashLogSet logs, CrashModIndex modIndex)
	{
		string text = logs.CrashReport?.Text;
		if (string.IsNullOrEmpty(text) || !text.Contains("Suspected Mod", StringComparison.Ordinal))
		{
			yield break;
		}
		string text2 = CrashText.Between(text, "Suspected Mod", "Stacktrace");
		if (!text2.StartsWith("s: None", StringComparison.Ordinal))
		{
			List<string> list = CrashRegex.All(text2, "(?<=\\n\\t[^(\\t]+\\()[^)\\n]+");
			if (list.Count != 0)
			{
				yield return _ModFinding(CrashCause.SuspectedModCrash, modIndex.ResolveToDisplayNames(list), CrashLogKind.CrashReport, shouldStop: true);
			}
		}
	}

	private static CrashFinding? _ExtractShortOutput(CrashLogSet logs)
	{
		string text = logs.Game?.Text;
		if (string.IsNullOrEmpty(text) || logs.HsErr != null || logs.CrashReport != null || text.Contains("at net.", StringComparison.Ordinal) || text.Contains("INFO]", StringComparison.Ordinal) || text.Length >= 100)
		{
			return null;
		}
		return _ModFinding(CrashCause.VeryShortOutput, new _003C_003Ez__ReadOnlySingleElementList<string>(text), CrashLogKind.Game, shouldStop: false, "raw-output");
	}

	private static CrashFinding? _ExtractModLoaderError(CrashLogSet logs)
	{
		string text = logs.Game?.Text;
		if (string.IsNullOrEmpty(text) || !text.Contains("Mod resolution failed", StringComparison.Ordinal))
		{
			return null;
		}
		return new CrashFinding(CrashCause.ModLoaderError, CrashConfidence.High);
	}

	private static CrashFinding? _ExtractModInitializationFailure(CrashLogSet logs, CrashModIndex modIndex)
	{
		string text = logs.Game?.Text;
		if (string.IsNullOrEmpty(text) || !text.Contains("Failed to create mod instance.", StringComparison.Ordinal))
		{
			return null;
		}
		string item = (CrashRegex.First(text, "(?<=Failed to create mod instance. ModID: )[^,]+") ?? CrashRegex.First(text, "(?<=Failed to create mod instance. ModId )[^\\n]+(?= for )") ?? "").TrimEnd(new char[2] { '\r', '\n' });
		return _ModFinding(CrashCause.ModInitializationFailed, modIndex.ResolveToDisplayNames(new _003C_003Ez__ReadOnlySingleElementList<string>(item)), CrashLogKind.Game, shouldStop: false);
	}

	private static IEnumerable<CrashFinding> _ExtractSpecificBlockAndEntity(CrashLogSet logs)
	{
		string crash = logs.CrashReport?.Text;
		if (!string.IsNullOrEmpty(crash))
		{
			if (crash.Contains("\tBlock location: World: ", StringComparison.Ordinal))
			{
				string item = CrashRegex.First(crash, "(?<=\\tBlock: Block\\{)[^\\}]+") + " " + CrashRegex.First(crash, "(?<=\\tBlock location: World: )\\([^\\)]+\\)");
				yield return _ModFinding(CrashCause.SpecificBlockCrash, new _003C_003Ez__ReadOnlySingleElementList<string>(item), CrashLogKind.CrashReport, shouldStop: false, "block");
			}
			if (crash.Contains("\tEntity's Exact location: ", StringComparison.Ordinal))
			{
				string item2 = CrashRegex.First(crash, "(?<=\\tEntity Type: )[^\\n]+(?= \\()") + " (" + (CrashRegex.First(crash, "(?<=\\tEntity's Exact location: )[^\\n]+") ?? "").TrimEnd(new char[2] { '\r', '\n' }) + ")";
				yield return _ModFinding(CrashCause.SpecificEntityCrash, new _003C_003Ez__ReadOnlySingleElementList<string>(item2), CrashLogKind.CrashReport, shouldStop: false, "entity");
			}
		}
	}

	private static IEnumerable<CrashEvidence> _FindPatterns(CrashLogSet logs, IEnumerable<(CrashLogKind Source, string Pattern)> patterns)
	{
		foreach (var (crashLogKind, text) in patterns)
		{
			if (logs.GetText(crashLogKind)?.Contains(text, StringComparison.Ordinal) ?? false)
			{
				yield return _Evidence(text, crashLogKind, "pattern", text);
			}
		}
	}

	private static CrashFinding _ModFinding(CrashCause cause, IEnumerable<string?> values, CrashLogKind source, bool shouldStop, string displayKind = "mod")
	{
		return new CrashFinding(cause, CrashConfidence.High, from value in values
			where !string.IsNullOrWhiteSpace(value)
			select _Evidence(value, source, displayKind))
		{
			ShouldStop = shouldStop
		};
	}

	private static CrashEvidence _Evidence(string value, CrashLogKind source, string displayKind, string? pattern = null)
	{
		return new CrashEvidence
		{
			Value = value,
			Source = source,
			Pattern = pattern,
			DisplayKind = displayKind
		};
	}
}
