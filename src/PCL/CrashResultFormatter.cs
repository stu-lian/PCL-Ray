using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;

namespace PCL;

internal sealed class CrashResultFormatter
{
	public CrashDialogContent Format(CrashAnalysisContext context, bool isHandAnalyze)
	{
		CrashAnalysisResult crashAnalysisResult = context.Result ?? new CrashAnalysisResult();
		if (!crashAnalysisResult.Any)
		{
			return _CreateUnknownContent(isHandAnalyze);
		}
		List<CrashMessageSpec> list = crashAnalysisResult.Findings.Select((CrashFinding finding) => _CreateMessageSpec(finding, context.LogAll)).ToList();
		string text = _JoinParagraphs(list.Select(_Render));
		if (!isHandAnalyze)
		{
			text = _AppendFollowUps(text, list);
		}
		return new CrashDialogContent(_NormalizeLineEndings(text).Trim(new char[2] { '\r', '\n' }), list[0].SuggestedAction);
	}

	private static CrashDialogContent _CreateUnknownContent(bool isHandAnalyze)
	{
		return new CrashDialogContent(_NormalizeLineEndings(_Render(isHandAnalyze ? _Spec("Crash.Reason.Unknown.Manual") : _Spec("Crash.Reason.Unknown.Auto", "Crash.Suggestion.ExportReport"))).Trim(new char[2] { '\r', '\n' }), CrashSuggestedAction.None);
	}

	private static string _AppendFollowUps(string text, IReadOnlyCollection<CrashMessageSpec> specs)
	{
		List<string> list = new List<string>();
		if (specs.Any((CrashMessageSpec spec) => spec.AppendReportHint))
		{
			list.Add(Lang.Text("Crash.Suggestion.ViewReport"));
		}
		if (specs.Any((CrashMessageSpec spec) => spec.AppendHelpHint))
		{
			list.Add(Lang.Text("Crash.Suggestion.ExportReport"));
		}
		string text2 = _GetLauncherOutdatedSuggestion();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add(text2);
		}
		if (list.Count != 0)
		{
			return Lang.Text("Crash.Presentation.WithFollowUps", new object[2]
			{
				text,
				_JoinParagraphs(list)
			});
		}
		return text;
	}

	private static string _Render(CrashMessageSpec spec)
	{
		string text = Lang.Text(spec.ReasonKey, spec.ReasonArgs);
		if (spec.SuggestionKey == null)
		{
			return Lang.Text("Crash.Presentation.ReasonOnly", new object[1] { text });
		}
		return Lang.Text("Crash.Presentation.WithSuggestion", new object[2]
		{
			text,
			Lang.Text(spec.SuggestionKey, spec.SuggestionArgs)
		});
	}

	private static CrashMessageSpec _CreateMessageSpec(CrashFinding finding, string combinedLogText)
	{
		List<string> list = finding.Details.ToList();
		switch (finding.Cause)
		{
		case CrashCause.ExtractedModFile:
			return _Spec("Crash.Reason.ExtractedModFile", "Crash.Suggestion.ExtractedModFile");
		case CrashCause.OutOfMemory:
			return _Spec("Crash.Reason.OutOfMemory", "Crash.Suggestion.OutOfMemory", null, null, report: false, help: true);
		case CrashCause.UsingOpenJ9:
			return _Spec("Crash.Reason.UsingOpenJ9", "Crash.Suggestion.UsingOpenJ9");
		case CrashCause.UsingJdk:
			return _Spec("Crash.Reason.UsingJdk", "Crash.Suggestion.UsingJdk");
		case CrashCause.JavaTooNew:
			return _Spec("Crash.Reason.JavaTooNew", "Crash.Suggestion.JavaTooNew");
		case CrashCause.JavaIncompatible:
			return _Spec("Crash.Reason.JavaIncompatible", "Crash.Suggestion.JavaIncompatible");
		case CrashCause.InvalidModFileName:
			return _Spec("Crash.Reason.InvalidModFileName", "Crash.Suggestion.InvalidModFileName");
		case CrashCause.MissingMixinBootstrap:
			return _Spec("Crash.Reason.MissingMixinBootstrap", "Crash.Suggestion.MissingMixinBootstrap");
		case CrashCause.X86JavaMemoryLimit:
			if (!Environment.Is64BitOperatingSystem)
			{
				return _Spec("Crash.Reason.X86JavaMemoryLimit.OnX86Os", "Crash.Suggestion.X86JavaMemoryLimit.OnX86Os", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.X86JavaMemoryLimit.OnX64Os", "Crash.Suggestion.X86JavaMemoryLimit.OnX64Os");
		case CrashCause.MissingDependencyOrWrongMcVersion:
			return _FormatMissingDependency(list);
		case CrashCause.StackKeywordFound:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.StackKeyword.Multiple", "Crash.Suggestion.StackKeyword", _Args(_BulletList(list)), null, report: false, help: true);
			}
			return _Spec("Crash.Reason.StackKeyword.Single", "Crash.Suggestion.StackKeyword", _Args(list[0]), null, report: false, help: true);
		case CrashCause.SuspectedModCrash:
		case CrashCause.StackModNameFound:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.SuspectedMod.Multiple", "Crash.Suggestion.DisableTheseMods", _Args(_BulletList(list)), null, report: true, help: true);
			}
			return _Spec("Crash.Reason.SuspectedMod.Single", "Crash.Suggestion.DisableThisMod", _Args(list[0]), null, report: true, help: true);
		case CrashCause.ConfirmedModCrash:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.ConfirmedMod.Multiple", "Crash.Suggestion.DisableTheseMods", _Args(_BulletList(list)), null, report: true, help: true);
			}
			return _Spec("Crash.Reason.ConfirmedMod.Single", "Crash.Suggestion.DisableThisMod", _Args(list[0]), null, report: true, help: true);
		case CrashCause.ModMixinFailed:
			if (list.Count == 0)
			{
				return _Spec("Crash.Reason.ModMixinFailed.None", "Crash.Suggestion.ModMixinFailed", null, null, report: true, help: true);
			}
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.ModMixinFailed.Multiple", "Crash.Suggestion.DisableTheseMods", _Args(_BulletList(list)), null, report: true, help: true);
			}
			return _Spec("Crash.Reason.ModMixinFailed.Single", "Crash.Suggestion.DisableThisMod", _Args(list[0]), null, report: true, help: true);
		case CrashCause.ModConfigCrash:
			if (list.Count <= 1 || list[1] == null)
			{
				return _Spec("Crash.Reason.ModConfigCrash.Simple", "Crash.Suggestion.DisableThisMod", _Args(list.FirstOrDefault() ?? string.Empty), null, report: true, help: true);
			}
			return _Spec("Crash.Reason.ModConfigCrash.WithConfig", "Crash.Suggestion.ModConfigCrash", _Args(list[0], list[1]));
		case CrashCause.ModInitializationFailed:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.ModInitializationFailed.Multiple", "Crash.Suggestion.DisableTheseMods", _Args(_BulletList(list)), null, report: true, help: true);
			}
			return _Spec("Crash.Reason.ModInitializationFailed.Single", "Crash.Suggestion.DisableThisMod", _Args(list[0]), null, report: true, help: true);
		case CrashCause.SpecificBlockCrash:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.SpecificBlock.Multiple", "Crash.Suggestion.SpecificBlock.Multiple", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.SpecificBlock.Single", "Crash.Suggestion.SpecificBlock.Single", _Args(list[0]), null, report: false, help: true);
		case CrashCause.DuplicateMods:
			if (list.Count < 2)
			{
				return _Spec("Crash.Reason.DuplicateMods.Unknown", "Crash.Suggestion.DuplicateMods", null, null, report: true, help: true);
			}
			return _Spec("Crash.Reason.DuplicateMods.Known", "Crash.Suggestion.DuplicateMods", _Args(_BulletList(list)));
		case CrashCause.SpecificEntityCrash:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.SpecificEntity.Multiple", "Crash.Suggestion.SpecificEntity.Multiple", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.SpecificEntity.Single", "Crash.Suggestion.SpecificEntity.Single", _Args(list[0]), null, report: false, help: true);
		case CrashCause.OptiFineForgeIncompatible:
			return _Spec("Crash.Reason.OptiFineForgeIncompatible", "Crash.Suggestion.OptiFineForgeIncompatible");
		case CrashCause.ShadersModWithOptiFine:
			return _Spec("Crash.Reason.ShadersModWithOptiFine", "Crash.Suggestion.ShadersModWithOptiFine");
		case CrashCause.OldForgeNewJavaIncompatible:
			return _Spec("Crash.Reason.OldForgeNewJavaIncompatible", "Crash.Suggestion.OldForgeNewJavaIncompatible");
		case CrashCause.MultipleForgeInInstanceJson:
			return _Spec("Crash.Reason.MultipleForgeInInstanceJson", "Crash.Suggestion.MultipleForgeInInstanceJson");
		case CrashCause.ManualDebugCrash:
			return _Spec("Crash.Reason.ManualDebugCrash");
		case CrashCause.ModRequiresJava11:
			return _Spec("Crash.Reason.ModRequiresJava11", "Crash.Suggestion.ModRequiresJava11");
		case CrashCause.VeryShortOutput:
			return _Spec("Crash.Reason.VeryShortOutput", "Crash.Suggestion.ExportReport", _Args(list.FirstOrDefault() ?? string.Empty));
		case CrashCause.OptiFineWorldLoadCrash:
			return _Spec("Crash.Reason.OptiFineWorldLoadCrash", "Crash.Suggestion.OptiFineWorldLoadCrash", null, null, report: false, help: true);
		case CrashCause.UnsupportedOpenGl:
		case CrashCause.PixelFormatNotSupported:
		case CrashCause.IntelDriverAccessViolation:
		case CrashCause.AmdDriverAccessViolation:
		case CrashCause.NvidiaDriverAccessViolation:
			if (!combinedLogText.Contains("hd graphics "))
			{
				return _Spec("Crash.Reason.GraphicsDriver.Generic", "Crash.Suggestion.GraphicsDriver.Generic", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.GraphicsDriver.IntelOrIntegrated", "Crash.Suggestion.GraphicsDriver.IntelOrIntegrated", null, null, report: false, help: true);
		case CrashCause.ResourcePackTooLarge:
			return _Spec("Crash.Reason.ResourcePackTooLarge", "Crash.Suggestion.ResourcePackTooLarge", null, null, report: false, help: true);
		case CrashCause.NightConfigBug:
			return _Spec("Crash.Reason.NightConfigBug", "Crash.Suggestion.NightConfigBug", null, null, report: false, help: true);
		case CrashCause.OpenGl1282:
			return _Spec("Crash.Reason.OpenGl1282", "Crash.Suggestion.OpenGl1282", null, null, report: false, help: true);
		case CrashCause.TooManyModsIdLimit:
			return _Spec("Crash.Reason.TooManyModsIdLimit", "Crash.Suggestion.TooManyModsIdLimit");
		case CrashCause.FileOrContentValidationFailed:
			return _Spec("Crash.Reason.FileOrContentValidationFailed", "Crash.Suggestion.FileOrContentValidationFailed", null, null, report: false, help: true);
		case CrashCause.IncompleteForgeInstallation:
			return _Spec("Crash.Reason.IncompleteForgeInstallation", "Crash.Suggestion.IncompleteForgeInstallation", null, null, report: false, help: true);
		case CrashCause.FabricError:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.FabricError.Generic", "Crash.Suggestion.FollowLoaderInstructions", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.FabricError.WithDetail", "Crash.Suggestion.FollowLoaderInstructions", _Args(list[0]));
		case CrashCause.IncompatibleMods:
			return _FormatIncompatibleMods(list);
		case CrashCause.ModLoaderError:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.ModLoaderError.Generic", "Crash.Suggestion.FollowLoaderInstructions", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.ModLoaderError.WithDetail", "Crash.Suggestion.FollowLoaderInstructions", _Args(list[0]));
		case CrashCause.FabricSolutionProvided:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.FabricSolution.Generic", "Crash.Suggestion.FollowLoaderInstructions", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.FabricSolution.WithDetail", "Crash.Suggestion.FollowLoaderInstructions", _Args(list[0]));
		case CrashCause.ForgeError:
			if (list.Count != 1)
			{
				return _Spec("Crash.Reason.ForgeError.Generic", "Crash.Suggestion.FollowLoaderInstructions", null, null, report: false, help: true);
			}
			return _Spec("Crash.Reason.ForgeError.WithDetail", "Crash.Suggestion.FollowLoaderInstructions", _Args(list[0]));
		case CrashCause.NoAnalyzableFile:
			return _Spec("Crash.Reason.NoAnalyzableFile", "Crash.Suggestion.ExportReport", null, null, report: false, help: true);
		default:
			return _Spec("Crash.Reason.UnknownFinding", "Crash.Suggestion.ExportReport", _Args(finding.Cause.ToString()), null, report: false, help: true);
		}
	}

	private static CrashMessageSpec _FormatMissingDependency(List<string> additional)
	{
		if (additional.Count == 0)
		{
			return _Spec("Crash.Reason.MissingDependency.Generic", "Crash.Suggestion.FollowLoaderInstructions", null, null, report: false, help: true);
		}
		string text = _BulletList(additional);
		if (!StringExtension.IsMatch(text, RegexPatterns.IncompatibleModLoaderErrorHint))
		{
			return _Spec("Crash.Reason.MissingDependency.WithDetail", "Crash.Suggestion.FollowLoaderInstructions", _Args(text));
		}
		return _Spec("Crash.Reason.ModLoaderIncompatible", "Crash.Suggestion.ModLoaderIncompatible", _Args(text), null, report: false, help: false, CrashSuggestedAction.OpenInstanceSettings);
	}

	private static CrashMessageSpec _FormatIncompatibleMods(List<string> additional)
	{
		if (additional.Count != 1)
		{
			return _Spec("Crash.Reason.IncompatibleMods.Generic", "Crash.Suggestion.FollowLoaderInstructions", null, null, report: false, help: true);
		}
		string text = additional[0];
		if (!StringExtension.IsMatch(text, RegexPatterns.IncompatibleModLoaderErrorHint))
		{
			return _Spec("Crash.Reason.IncompatibleMods.WithDetail", "Crash.Suggestion.FollowLoaderInstructions", _Args(text));
		}
		return _Spec("Crash.Reason.ModLoaderIncompatible", "Crash.Suggestion.ModLoaderIncompatible", _Args(text), null, report: false, help: false, CrashSuggestedAction.OpenInstanceSettings);
	}

	private static CrashMessageSpec _Spec(string reasonKey, string? suggestionKey = null, object?[]? reasonArgs = null, object?[]? suggestionArgs = null, bool report = false, bool help = false, CrashSuggestedAction action = CrashSuggestedAction.None)
	{
		return new CrashMessageSpec(reasonKey, reasonArgs ?? Array.Empty<object>(), suggestionKey, suggestionArgs ?? Array.Empty<object>(), report, help, action);
	}

	private static object?[] _Args(params object?[] args)
	{
		return args;
	}

	private static string _BulletList(IEnumerable<string> items)
	{
		return string.Join(Environment.NewLine, items.Select((string item) => Lang.Text("Crash.Presentation.ListItem", new object[1] { item })));
	}

	private static string _JoinParagraphs(IEnumerable<string> items)
	{
		return string.Join(Environment.NewLine + Environment.NewLine, items.Where((string item) => !string.IsNullOrWhiteSpace(item)));
	}

	private static string _NormalizeLineEndings(string text)
	{
		return text.Replace("\r\n", "\r").Replace("\n", "\r").Replace("\r", "\r\n");
	}

	private static string? _GetLauncherOutdatedSuggestion()
	{
		try
		{
			return (UpdateManager.GetVersionStatus() == UpdateEnums.VersionStatus.Latest) ? null : Lang.Text("Crash.Suggestion.LauncherOutdated");
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Crash", "确认启动器更新失败");
			return null;
		}
	}
}
