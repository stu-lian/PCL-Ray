using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PCL.Core.App;
using PCL.Core.Logging;

namespace PCL;

internal sealed class CrashLogCollector(CrashAnalysisContext context)
{
	public void Collect(string versionPathIndie, IList<string>? latestLog)
	{
		LogWrapper.Info("Crash", "步骤 1：收集日志文件");
		List<string> list = _FindPossibleLogFiles(versionPathIndie).Distinct(StringComparer.OrdinalIgnoreCase).ToList().Where(IsRecentNonEmptyFile)
			.ToList();
		if (list.Count == 0)
		{
			LogWrapper.Info("Crash", "未发现可能可用的日志文件");
		}
		foreach (string item in list)
		{
			TryAddLogFile(item, "读取可能的崩溃日志文件失败");
		}
		AddCapturedOutput(latestLog);
		LogWrapper.Info("Crash", "步骤 1：收集日志文件完成，收集到 " + context.RawFiles.Count + " 个文件");
	}

	private static IEnumerable<string> _FindPossibleLogFiles(string versionPathIndie)
	{
		List<string> list = new List<string>();
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(versionPathIndie, "crash-reports"));
			if (directoryInfo.Exists)
			{
				list.AddRange(from file in directoryInfo.EnumerateFiles()
					select file.FullName);
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash", "收集 Minecraft 崩溃日志文件夹下的日志失败");
		}
		try
		{
			DirectoryInfo directoryInfo2 = new DirectoryInfo(versionPathIndie).Parent?.Parent;
			if (directoryInfo2 != null && directoryInfo2.Exists)
			{
				list.AddRange(from file in directoryInfo2.EnumerateFiles()
					where file.Extension == ".log"
					select file.FullName);
			}
		}
		catch (Exception ex2)
		{
			LogWrapper.Warn(ex2, "Crash", "收集 Minecraft 主文件夹下的日志失败");
		}
		try
		{
			DirectoryInfo directoryInfo3 = new DirectoryInfo(versionPathIndie);
			if (directoryInfo3.Exists)
			{
				list.AddRange(from file in directoryInfo3.EnumerateFiles()
					where file.Extension == ".log"
					select file.FullName);
			}
		}
		catch (Exception ex3)
		{
			LogWrapper.Warn(ex3, "Crash", "收集 Minecraft 隔离文件夹下的日志失败");
		}
		list.Add(Path.Combine(versionPathIndie, "logs", "latest.log"));
		if (CrashFileIo.ReadText(Path.Combine(Basics.ExecutableDirectory, "PCL", "LatestLaunch.bat")).Contains("-Dlog4j2.formatMsgNoLookups=false", StringComparison.OrdinalIgnoreCase))
		{
			list.Add(Path.Combine(versionPathIndie, "logs", "debug.log"));
		}
		return list;
	}

	private static bool IsRecentNonEmptyFile(string filePath)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists || fileInfo.Length <= 0)
			{
				return false;
			}
			double num = Math.Abs((fileInfo.LastWriteTime - DateTime.Now).TotalMinutes);
			if (num >= 3.0)
			{
				return false;
			}
			LogWrapper.Info("Crash", "可能可用的日志文件：" + filePath + "（" + Math.Round(num, 1) + " 分钟）");
			return true;
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash", "确认崩溃日志时间失败（" + filePath + "）");
			return false;
		}
	}

	private void TryAddLogFile(string filePath, string errorMessage)
	{
		try
		{
			context.RawFiles.Add(new CrashLogEntry(filePath, CrashFileIo.ReadText(filePath).Split("\r\n".ToCharArray())));
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash", errorMessage + "（" + filePath + "）");
		}
	}

	private void AddCapturedOutput(IList<string>? latestLog)
	{
		if (latestLog != null && latestLog.Any())
		{
			string text = string.Join("\r\n", latestLog);
			LogWrapper.Info("Crash", "以下为游戏输出的最后一段内容：\r\n" + text);
			string text2 = Path.Combine(context.TempFolder, "RawOutput.log");
			CrashFileIo.WriteText(text2, text);
			context.RawFiles.Add(new CrashLogEntry(text2, latestLog.ToArray()));
			latestLog.Clear();
		}
	}
}
