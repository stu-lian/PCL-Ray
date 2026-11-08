using System;
using System.Linq;
using PCL.Core.Logging;

namespace PCL;

internal sealed class CrashDetector
{
	private readonly CrashEvidenceExtractor _extractor = new CrashEvidenceExtractor();

	private readonly CrashStackAnalyzer _stackAnalyzer = new CrashStackAnalyzer();

	public CrashAnalysisResult Analyze(CrashLogSet logs, McInstance? instance)
	{
		LogWrapper.Info("Crash", "步骤 3：分析崩溃原因");
		CrashAnalysisResult crashAnalysisResult = new CrashAnalysisResult();
		if (!logs.HasAnalyzableLog)
		{
			crashAnalysisResult.Add(new CrashFinding(CrashCause.NoAnalyzableFile, CrashConfidence.High)
			{
				ShouldStop = true
			});
			_LogSummary(crashAnalysisResult);
			return crashAnalysisResult;
		}
		CrashLogSet logs2 = _Normalize(logs);
		CrashModIndex modIndex = CrashModIndex.Create(logs2, instance);
		_RunPhase(DetectionPhase.Fatal, logs2, modIndex, crashAnalysisResult);
		if (_ShouldStop(crashAnalysisResult))
		{
			_LogSummary(crashAnalysisResult);
			return crashAnalysisResult;
		}
		_RunPhase(DetectionPhase.Primary, logs2, modIndex, crashAnalysisResult);
		if (_ShouldStop(crashAnalysisResult))
		{
			_LogSummary(crashAnalysisResult);
			return crashAnalysisResult;
		}
		CrashFinding crashFinding = _stackAnalyzer.Analyze(logs2, modIndex);
		if (crashFinding != null)
		{
			crashAnalysisResult.Add(crashFinding);
		}
		if (_ShouldStop(crashAnalysisResult))
		{
			_LogSummary(crashAnalysisResult);
			return crashAnalysisResult;
		}
		_RunPhase(DetectionPhase.Secondary, logs2, modIndex, crashAnalysisResult);
		_LogSummary(crashAnalysisResult);
		return crashAnalysisResult;
	}

	private static CrashLogSet _Normalize(CrashLogSet logs)
	{
		string text = logs.All;
		if (text.Contains("quilt", StringComparison.OrdinalIgnoreCase) && text.Contains("Mod Table Version", StringComparison.Ordinal))
		{
			LogWrapper.Info("Crash", "处理 Quilt Mod Table 后再继续分析");
			text = CrashText.BeforeFirst(text, "| Index") + CrashText.AfterFirst(text, "Mod Table Version:");
		}
		return new CrashLogSet
		{
			Game = logs.Game,
			Debug = logs.Debug,
			CrashReport = logs.CrashReport,
			HsErr = logs.HsErr,
			All = text
		};
	}

	private void _RunPhase(DetectionPhase phase, CrashLogSet logs, CrashModIndex modIndex, CrashAnalysisResult result)
	{
		CrashRuleInput arg = new CrashRuleInput
		{
			Logs = logs,
			ModIndex = modIndex
		};
		foreach (CrashRule item in CrashRuleCatalog.Rules.Where((CrashRule rule) => rule.Phase == phase))
		{
			CrashFinding crashFinding = item.Evaluate(arg);
			if (crashFinding != null)
			{
				_AddAndLog(result, crashFinding);
			}
		}
		foreach (CrashFinding item2 in _extractor.Extract(phase, logs, modIndex))
		{
			_AddAndLog(result, item2);
		}
	}

	private static void _AddAndLog(CrashAnalysisResult result, CrashFinding finding)
	{
		result.Add(finding);
		_LogFinding(finding);
	}

	private static bool _ShouldStop(CrashAnalysisResult result)
	{
		return result.Findings.Any((CrashFinding finding) => finding.ShouldStop);
	}

	private static void _LogFinding(CrashFinding finding)
	{
		string text = string.Join("；", finding.Details);
		LogWrapper.Info("Crash", $"可能的崩溃原因：{finding.Cause}{(string.IsNullOrEmpty(text) ? "" : ("（" + text + "）"))}");
	}

	private static void _LogSummary(CrashAnalysisResult result)
	{
		if (!result.Any)
		{
			LogWrapper.Info("Crash", "步骤 3：分析崩溃原因完成，未找到可能的原因");
			return;
		}
		LogWrapper.Info("Crash", $"步骤 3：分析崩溃原因完成，找到 {result.Findings.Count} 条可能的原因");
		foreach (CrashFinding finding in result.Findings)
		{
			_LogFinding(finding);
		}
	}
}
