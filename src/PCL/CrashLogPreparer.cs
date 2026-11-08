using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PCL.Core.Logging;

namespace PCL;

internal sealed class CrashLogPreparer(CrashAnalysisContext context)
{
	private sealed class ClassifiedCrashLog(CrashLogEntry file, CrashLogKind? kind)
	{
		public CrashLogEntry File { get; } = file;

		public CrashLogKind? Kind { get; } = kind;
	}

	private static readonly HashSet<string> KnownGameLogNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "latest.log", "latest log.txt", "游戏崩溃前的输出.txt", "rawoutput.log" };

	private static readonly HashSet<string> KnownDebugLogNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "debug.log", "debug log.txt" };

	private static readonly HashSet<string> KnownLauncherLogNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "启动器日志.txt", "PCL2 启动器日志.txt", "PCL 启动器日志.txt", "PCL Ray 启动器日志.txt", "PCL CE 启动器日志.txt", "log1.txt", "log-ce1.log" };

	public bool Prepare()
	{
		LogWrapper.Info("Crash", "步骤 2：准备日志文本");
		context.DirectOpenFile = null;
		context.PreparedLogs = null;
		context.OutputFiles.Clear();
		List<ClassifiedCrashLog> source = _ClassifyFiles();
		List<ClassifiedCrashLog> list = source.Where((ClassifiedCrashLog item) => item.Kind.HasValue).ToList();
		context.DirectOpenFile = (from item in list
			orderby _IsGeneratedLog(item.File), _GetLastWriteTime(item.File) descending
			select item.File).FirstOrDefault();
		List<CrashLogEntry> list2 = (from item in source
			where !item.Kind.HasValue
			select item.File).ToList();
		if (list.Count == 0 && list2.Count > 0)
		{
			LogWrapper.Info("Crash", "由于仅发现了额外日志，将它们视作 Minecraft 日志进行分析");
			list = list2.Select((CrashLogEntry file) => new ClassifiedCrashLog(file, CrashLogKind.Game)).ToList();
			list2.Clear();
		}
		CrashLogText crashLogText = _SelectGameLog(list);
		CrashLogText crashLogText2 = _SelectDebugLog(list);
		CrashLogText crashLogText3 = _SelectNewest(list, CrashLogKind.CrashReport, 300, 700);
		CrashLogText crashLogText4 = _SelectNewest(list, CrashLogKind.HsErr, 200, 100);
		foreach (CrashLogEntry item in list2)
		{
			context.OutputFiles.Add(item.FullPath);
			LogWrapper.Info("Crash", "输出报告：" + item.FullPath + "，不用作分析");
		}
		CrashLogSet crashLogSet = new CrashLogSet
		{
			Game = crashLogText,
			Debug = crashLogText2,
			CrashReport = crashLogText3,
			HsErr = crashLogText4,
			All = (crashLogText?.Text ?? crashLogText2?.Text ?? string.Empty) + (crashLogText4?.Text ?? string.Empty) + (crashLogText3?.Text ?? string.Empty)
		};
		context.PreparedLogs = crashLogSet;
		context.LogAll = crashLogSet.All;
		if (crashLogSet.HasAnalyzableLog)
		{
			LogWrapper.Info("Crash", ("步骤 2：准备日志文本完成，找到" + ((crashLogText == null) ? "" : "游戏日志、") + ((crashLogText2 == null) ? "" : "游戏 Debug 日志、") + ((crashLogText4 == null) ? "" : "虚拟机日志、") + ((crashLogText3 == null) ? "" : "崩溃日志、")).TrimEnd('、') + "用作分析");
		}
		else
		{
			LogWrapper.Info("Crash", "步骤 2：准备日志文本完成，没有任何可供分析的日志");
		}
		return crashLogSet.HasAnalyzableLog;
	}

	private List<ClassifiedCrashLog> _ClassifyFiles()
	{
		List<ClassifiedCrashLog> list = new List<ClassifiedCrashLog>();
		foreach (CrashLogEntry rawFile in context.RawFiles)
		{
			string text = rawFile.FileName.ToLowerInvariant();
			if (!rawFile.Lines.Any())
			{
				LogWrapper.Info("Crash", text + " 由于内容为空跳过");
				continue;
			}
			CrashLogKind? crashLogKind = _ClassifyLogKind(rawFile);
			if (!crashLogKind.HasValue && !_IsExtraLogLike(rawFile))
			{
				LogWrapper.Info("Crash", text + " 分类为 Ignore");
				continue;
			}
			list.Add(new ClassifiedCrashLog(rawFile, crashLogKind));
			LogWrapper.Info("Crash", text + " 分类为 " + (crashLogKind?.ToString() ?? "Extra"));
		}
		return list;
	}

	private static CrashLogKind? _ClassifyLogKind(CrashLogEntry file)
	{
		string text = file.FileName.ToLowerInvariant();
		if (text.StartsWith("hs_err", StringComparison.Ordinal))
		{
			return CrashLogKind.HsErr;
		}
		if (text.StartsWith("crash-", StringComparison.Ordinal))
		{
			return CrashLogKind.CrashReport;
		}
		if (KnownDebugLogNames.Contains(text))
		{
			return CrashLogKind.Debug;
		}
		if (KnownGameLogNames.Contains(text))
		{
			return CrashLogKind.Game;
		}
		if (KnownLauncherLogNames.Contains(text) && file.Lines.Any((string line) => line.Contains("以下为游戏输出的最后一段内容")))
		{
			return CrashLogKind.Game;
		}
		return null;
	}

	private static bool _IsExtraLogLike(CrashLogEntry file)
	{
		string text = file.FileName.ToLowerInvariant();
		if (!text.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
		{
			return text.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private CrashLogText? _SelectGameLog(IReadOnlyList<ClassifiedCrashLog> files)
	{
		List<CrashLogEntry> list = (from item in files.Where((ClassifiedCrashLog item) =>
			{
				CrashLogKind? kind = item.Kind;
				if (kind.HasValue)
				{
					CrashLogKind valueOrDefault = kind.GetValueOrDefault();
					if ((uint)valueOrDefault <= 1u)
					{
						return true;
					}
				}
				return false;
			})
			select item.File).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		Dictionary<string, CrashLogEntry> dictionary = (from file in list
			group file by file.FileName.ToLowerInvariant()).ToDictionary((IGrouping<string, CrashLogEntry> group) => group.Key, (IGrouping<string, CrashLogEntry> group) => group.OrderBy(_GetLastWriteTime).Last(), StringComparer.OrdinalIgnoreCase);
		string text = "";
		string[] array = new string[9] { "rawoutput.log", "启动器日志.txt", "log1.txt", "log-ce1.log", "游戏崩溃前的输出.txt", "PCL Ray 启动器日志.txt", "PCL CE 启动器日志.txt", "PCL2 启动器日志.txt", "PCL 启动器日志.txt" };
		foreach (string key in array)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				text += _ExtractLauncherGameOutput(value);
				context.OutputFiles.Add(value.FullPath);
				LogWrapper.Info("Crash", "导入分析：" + value.FullPath + "，作为启动器日志");
				break;
			}
		}
		array = new string[4] { "latest.log", "latest log.txt", "debug.log", "debug log.txt" };
		foreach (string key2 in array)
		{
			if (dictionary.TryGetValue(key2, out var value2))
			{
				text += _GetHeadTailLines(value2.Lines, 1500, 500);
				context.OutputFiles.Add(value2.FullPath);
				LogWrapper.Info("Crash", "导入分析：" + value2.FullPath + "，作为 Minecraft 日志");
				break;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			CrashLogEntry crashLogEntry = list.OrderBy(_GetLastWriteTime).Last();
			text = _GetHeadTailLines(crashLogEntry.Lines, 1500, 500);
			context.OutputFiles.Add(crashLogEntry.FullPath);
			LogWrapper.Info("Crash", "导入分析：" + crashLogEntry.FullPath + "，作为兜底日志");
		}
		foreach (CrashLogEntry item in list.Where((CrashLogEntry file) => !context.OutputFiles.Contains(file.FullPath)))
		{
			context.OutputFiles.Add(item.FullPath);
			LogWrapper.Info("Crash", "输出报告：" + item.FullPath + "，作为 Minecraft 或启动器日志");
		}
		return new CrashLogText
		{
			Kind = CrashLogKind.Game,
			Text = text.TrimEnd(new char[2] { '\r', '\n' }),
			FilePath = list.FirstOrDefault()?.FullPath
		};
	}

	private CrashLogText? _SelectDebugLog(IReadOnlyList<ClassifiedCrashLog> files)
	{
		List<CrashLogEntry> list = (from item in files
			where item.Kind == CrashLogKind.Debug
			select item.File).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		CrashLogEntry crashLogEntry = list.OrderBy(_GetLastWriteTime).Last();
		if (!context.OutputFiles.Contains(crashLogEntry.FullPath))
		{
			context.OutputFiles.Add(crashLogEntry.FullPath);
		}
		LogWrapper.Info("Crash", "导入分析：" + crashLogEntry.FullPath + "，作为 Minecraft Debug 日志");
		return new CrashLogText
		{
			Kind = CrashLogKind.Debug,
			Text = _GetHeadTailLines(crashLogEntry.Lines, 1000, 0),
			FilePath = crashLogEntry.FullPath
		};
	}

	private CrashLogText? _SelectNewest(IReadOnlyList<ClassifiedCrashLog> files, CrashLogKind kind, int head, int tail)
	{
		List<CrashLogEntry> list = (from item in files
			where item.Kind == kind
			select item.File).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		CrashLogEntry crashLogEntry = list.OrderBy(_GetLastWriteTime).Last();
		context.OutputFiles.Add(crashLogEntry.FullPath);
		LogWrapper.Info("Crash", "输出报告：" + crashLogEntry.FullPath + ((kind == CrashLogKind.HsErr) ? "，作为虚拟机错误信息" : "，作为 Minecraft 崩溃报告"));
		LogWrapper.Info("Crash", "导入分析：" + crashLogEntry.FullPath + ((kind == CrashLogKind.HsErr) ? "，作为虚拟机错误信息" : "，作为 Minecraft 崩溃报告"));
		return new CrashLogText
		{
			Kind = kind,
			Text = _GetHeadTailLines(crashLogEntry.Lines, head, tail),
			FilePath = crashLogEntry.FullPath
		};
	}

	private static string _ExtractLauncherGameOutput(CrashLogEntry log)
	{
		string text = "";
		bool flag = false;
		foreach (string line in log.Lines)
		{
			if (flag)
			{
				text = text + line + "\n";
			}
			else if (line.Contains("以下为游戏输出的最后一段内容"))
			{
				flag = true;
				LogWrapper.Info("Crash", "找到 PCL 输出的游戏实时日志头");
			}
		}
		if (!flag)
		{
			return _GetHeadTailLines(log.Lines, 0, 500);
		}
		return text.TrimEnd(new char[2] { '\r', '\n' });
	}

	private static DateTime _GetLastWriteTime(CrashLogEntry file)
	{
		try
		{
			return new FileInfo(file.FullPath).LastWriteTime;
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash", "获取日志文件修改时间失败");
			return new DateTime(1900, 1, 1);
		}
	}

	private bool _IsGeneratedLog(CrashLogEntry file)
	{
		return file.FullPath.StartsWith(context.TempFolder, StringComparison.OrdinalIgnoreCase);
	}

	private static string _GetHeadTailLines(IReadOnlyList<string> raw, int headLines, int tailLines)
	{
		if (raw.Count <= headLines + tailLines)
		{
			return string.Join("\n", raw.Distinct());
		}
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		int num = 0;
		int i;
		for (i = 0; i <= raw.Count - 1; i++)
		{
			if (hashSet.Add(raw[i]))
			{
				num++;
				list.Add(raw[i]);
				if (num >= headLines)
				{
					break;
				}
			}
		}
		int num2 = 0;
		for (int num3 = raw.Count - 1; num3 >= i; num3--)
		{
			if (hashSet.Add(raw[num3]))
			{
				num2++;
				list.Insert(num, raw[num3]);
				if (num2 >= tailLines)
				{
					break;
				}
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in list.Where((string line) => !string.IsNullOrEmpty(line)))
		{
			stringBuilder.Append(item);
			stringBuilder.Append('\n');
		}
		return stringBuilder.ToString();
	}
}
