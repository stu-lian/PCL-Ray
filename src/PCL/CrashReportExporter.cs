using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Utils.Codecs;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;

namespace PCL;

internal sealed class CrashReportExporter
{
	private const string ReportFolderName = "Report";

	private const string LaunchScriptFileName = "启动脚本.bat";

	private const string RawOutputFileName = "游戏崩溃前的输出.txt";

	private const string LauncherLogFileName = "PCL Ray 启动器日志.txt";

	private const string EnvironmentFileName = "环境与启动信息.txt";

	private const string ModInfoFileName = "模组列表.txt";

	public void Export(CrashAnalysisContext context, string targetZipPath, IEnumerable<string>? extraFiles)
	{
		Directory.CreateDirectory(Basics.GetParentPathOrEmpty(targetZipPath));
		if (File.Exists(targetZipPath))
		{
			File.Delete(targetZipPath);
		}
		ModBase.FeedbackInfo();
		string text = Path.Combine(context.TempFolder, "Report");
		if (Directory.Exists(text))
		{
			CrashFileIo.DeleteDirectory(text);
		}
		Directory.CreateDirectory(text);
		try
		{
			foreach (string item in _CollectOutputFiles(context, extraFiles))
			{
				_CopyFileToReport(text, item);
			}
			_WriteEnvironmentInfo(text);
			_WriteModInfo(text, context.Instance);
			ZipFile.CreateFromDirectory(text, targetZipPath);
		}
		finally
		{
			CrashFileIo.DeleteDirectory(text);
		}
	}

	private static void _CopyFileToReport(string reportFolder, string outputFile)
	{
		if (File.Exists(outputFile))
		{
			string text = _GetExportFileName(outputFile, out Encoding fileEncoding);
			if (fileEncoding == null)
			{
				fileEncoding = EncodingDetector.DetectEncoding(CrashFileIo.ReadBytes(outputFile));
			}
			string fileContent = CrashFileIo.ReadText(outputFile, fileEncoding);
			fileContent = _SanitizeFileContent(fileContent, text);
			CrashFileIo.WriteText(Path.Combine(reportFolder, text), fileContent, fileEncoding);
			LogWrapper.Info("Crash", "导出文件：" + text + "，编码：" + fileEncoding.HeaderName);
		}
	}

	private static string _GetExportFileName(string outputFile, out Encoding? fileEncoding)
	{
		fileEncoding = null;
		string fileName = Path.GetFileName(outputFile);
		if (!(fileName == "LatestLaunch.bat"))
		{
			if (fileName == "RawOutput.log")
			{
				fileEncoding = Encoding.UTF8;
				return "游戏崩溃前的输出.txt";
			}
			string text = LogWrapper.CurrentLogger.CurrentLogFiles.LastOrDefault();
			if (((text == null) ? null : CrashText.AfterLast(text, "\\")) != fileName)
			{
				return fileName;
			}
			fileEncoding = Encoding.UTF8;
			return "PCL Ray 启动器日志.txt";
		}
		return "启动脚本.bat";
	}

	private static IReadOnlyList<string> _CollectOutputFiles(CrashAnalysisContext context, IEnumerable<string>? extraFiles)
	{
		if (extraFiles != null)
		{
			context.OutputFiles.AddRange(extraFiles);
		}
		return context.OutputFiles;
	}

	private static string _SanitizeFileContent(string fileContent, string fileName)
	{
		char filterChar = ((fileName == "启动脚本.bat") ? 'F' : '*');
		fileContent = McLogFilter.FilterAccessToken(fileContent, filterChar);
		return McLogFilter.FilterUserName(fileContent, '*');
	}

	private static void _WriteEnvironmentInfo(string reportFolder)
	{
		string launcherLog = CrashText.BeforeFirst(CrashText.AfterLast(_ReadReportFile(reportFolder, "PCL Ray 启动器日志.txt"), "[Launch] ~ 基础参数 ~"), "开始 Minecraft 日志监控");
		string launchScript = _ReadReportFile(reportFolder, "启动脚本.bat");
		StringBuilder stringBuilder = new StringBuilder();
		_AppendLauncherInfo(stringBuilder);
		_AppendProfileInfo(stringBuilder, launcherLog);
		_AppendInstanceInfo(stringBuilder, launcherLog, launchScript);
		_AppendEnvironmentInfo(stringBuilder, launcherLog);
		CrashFileIo.WriteText(Path.Combine(reportFolder, "环境与启动信息.txt"), stringBuilder.ToString(), Encoding.UTF8);
	}

	private static void _AppendLauncherInfo(StringBuilder builder)
	{
		builder.AppendLine(Lang.Text("Crash.Report.Environment.LauncherVersion", new object[1] { Basics.VersionName }));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.LauncherId", new object[1] { Identify.LauncherId }));
		builder.AppendLine();
	}

	private static void _AppendProfileInfo(StringBuilder builder, string launcherLog)
	{
		builder.AppendLine(Lang.Text("Crash.Report.Environment.ProfileSection"));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.ProfileName", new object[2]
		{
			_ExtractLauncherValue(launcherLog, "玩家用户名："),
			_ExtractLauncherValue(launcherLog, "验证方式：")
		}));
		builder.AppendLine();
	}

	private static void _AppendInstanceInfo(StringBuilder builder, string launcherLog, string launchScript)
	{
		builder.AppendLine(Lang.Text("Crash.Report.Environment.InstanceSection"));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.SelectedJava", new object[1] { _ExtractLauncherValue(launcherLog, "Java 信息：") }));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.Log4j2NoLookups", new object[1] { !launchScript.Contains("-Dlog4j2.formatMsgNoLookups=false", StringComparison.OrdinalIgnoreCase) }));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.MinecraftFolder", new object[1] { _ExtractLauncherValue(launcherLog, "MC 文件夹：") }));
		builder.AppendLine();
	}

	private static void _AppendEnvironmentInfo(StringBuilder builder, string launcherLog)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		builder.AppendLine(Lang.Text("Crash.Report.Environment.EnvironmentSection"));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.OperatingSystem", new object[3]
		{
			SystemInfo.OSInfo,
			!SystemInfo.Is32BitSystem,
			SystemInfo.IsArm64System
		}));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.Cpu", new object[1] { HardwareInfo.CPUName }));
		builder.AppendLine(Lang.Text("Crash.Report.Environment.MemoryAllocation", new object[3]
		{
			_ExtractLauncherValue(launcherLog, "分配的内存："),
			Lang.Number<double>((double)HardwareInfo.SystemMemorySize / 1024.0, "N2"),
			Lang.Number<long>(HardwareInfo.SystemMemorySize, "N0")
		}));
		for (int i = 0; i < HardwareInfo.GPUs.Count; i++)
		{
			HardwareInfo.GPUInfo val = HardwareInfo.GPUs[i];
			builder.AppendLine(Lang.Text("Crash.Report.Environment.Gpu", new object[4]
			{
				i,
				val.Name,
				_FormatGpuMemory(val.Memory),
				val.DriverVersion
			}));
		}
	}

	private static string _ExtractLauncherValue(string launcherLog, string key)
	{
		return CrashText.Between(launcherLog, key, "[").TrimEnd('[').Trim();
	}

	private static string _FormatGpuMemory(long memory)
	{
		if (memory < 4095)
		{
			return memory.ToString();
		}
		return ">= " + memory;
	}

	private static string _ReadReportFile(string reportFolder, string fileName)
	{
		string text = Path.Combine(reportFolder, fileName);
		if (!File.Exists(text))
		{
			return "";
		}
		return CrashFileIo.ReadText(text);
	}

	private static void _WriteModInfo(string reportFolder, McInstance? instance)
	{
		if ((object)instance == null)
		{
			return;
		}
		try
		{
			ModJarInJarCache.UseInstance(instance.PathInstance);
			ModLocalComp.JijScanContext = new ModJarInJar.ScanContext();
			string pathNameByCompType = ModLocalComp.GetPathNameByCompType(ModComp.CompType.Mod);
			string text;
			if (!instance.Info.HasLabyMod)
			{
				text = Path.Combine(instance.PathIndie, pathNameByCompType);
			}
			else
			{
				InlineArray5<string> buffer = default;
				buffer[0] = instance.PathIndie;
				buffer[1] = "labymod-neo";
				buffer[2] = "fabric";
				buffer[3] = instance.Info.VanillaName;
				buffer[4] = pathNameByCompType;
				text = Path.Combine(buffer);
			}
			string text2 = text;
			if (!Directory.Exists(text2))
			{
				return;
			}
			List<string> list = new List<string> { text2 };
			if (instance.Info.HasForge && instance.Info.Drop < 130)
			{
				string text3 = Path.Combine(text2, instance.Info.VanillaName);
				if (Directory.Exists(text3))
				{
					list.Add(text3);
				}
			}
			List<ModLocalComp.LocalCompFile> list2 = new List<ModLocalComp.LocalCompFile>();
			string preferredLoader = ModLocalComp.GetPreferredLoader(instance);
			foreach (string item in list)
			{
				string[] files = Directory.GetFiles(item);
				foreach (string path in files)
				{
					if (ModLocalComp.LocalCompFile.IsModFile(path))
					{
						ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(path);
						localCompFile.PreferredLoader = preferredLoader;
						localCompFile.Load();
						if (localCompFile.State != ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
						{
							list2.Add(localCompFile);
						}
					}
				}
			}
			List<ModLocalComp.LocalCompFile> list3 = list2.Where((ModLocalComp.LocalCompFile m) => m.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine).OrderBy((ModLocalComp.LocalCompFile m) => m.Name ?? m.FileName, StringComparer.OrdinalIgnoreCase).ToList();
			ModJarInJarIndex modJarInJarIndex = new ModJarInJarIndex(list2, instance.Info.VanillaName);
			StringBuilder stringBuilder = new StringBuilder();
			_AppendDependencyIssues(stringBuilder, modJarInJarIndex, list3);
			Dictionary<string, List<ModLocalComp.LocalCompFile>> dictionary = new Dictionary<string, List<ModLocalComp.LocalCompFile>>(StringComparer.OrdinalIgnoreCase);
			foreach (ModLocalComp.LocalCompFile item2 in list3)
			{
				foreach (string item3 in from id in new string[1] { item2.ModId }.Concat(item2.ProvidedIds)
					where !string.IsNullOrEmpty(id)
					select id)
				{
					if (!dictionary.TryGetValue(item3, out var value))
					{
						value = (dictionary[item3] = new List<ModLocalComp.LocalCompFile>());
					}
					if (!value.Contains(item2))
					{
						value.Add(item2);
					}
				}
			}
			List<string> list5 = new List<string>();
			foreach (ModLocalComp.LocalCompFile item4 in list3)
			{
				foreach (ModLocalComp.LocalCompFile item5 in modJarInJarIndex.GetLoadableEmbedded(item4))
				{
					foreach (string item6 in from id in new string[1] { item5.ModId }.Concat(item5.ProvidedIds)
						where !string.IsNullOrEmpty(id)
						select id)
					{
						if (!dictionary.TryGetValue(item6, out var value2))
						{
							continue;
						}
						foreach (ModLocalComp.LocalCompFile item7 in value2)
						{
							if (item7 != item4)
							{
								list5.Add(Lang.Text("Crash.Report.JarInJarMod.DuplicateEntry", new object[4]
								{
									item7.Name,
									item7.FileName,
									item4.Name,
									item5.Name ?? item5.ModId
								}));
							}
						}
					}
				}
			}
			stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.DuplicateSection"));
			if (list5.Count == 0)
			{
				stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.DuplicateNone"));
			}
			else
			{
				stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.DuplicateDescription"));
				stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.DuplicateHeuristic"));
				foreach (string item8 in list5.Distinct())
				{
					stringBuilder.AppendLine("\t|-> " + item8);
				}
			}
			stringBuilder.AppendLine().AppendLine("----------------------------").AppendLine();
			stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.ModListSection"));
			stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.ModListDirectory", new object[1] { text2 }));
			stringBuilder.AppendLine("|-> mods");
			foreach (ModLocalComp.LocalCompFile item9 in list3)
			{
				string text4 = "|  |-> " + (item9.Name ?? item9.FileName);
				string text5 = _CleanVersion(item9.Version);
				if (!string.IsNullOrWhiteSpace(text5))
				{
					text4 = text4 + " (" + text5 + ")";
				}
				if (item9.Name != item9.FileName)
				{
					text4 = text4 + " [" + item9.FileName + "]";
				}
				stringBuilder.AppendLine(text4);
			}
			stringBuilder.AppendLine().AppendLine("----------------------------").AppendLine();
			stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.JarInJarSection"));
			bool flag = false;
			foreach (ModLocalComp.LocalCompFile item10 in list3)
			{
				if (item10.EmbeddedMods.Any())
				{
					flag = true;
					stringBuilder.AppendLine(item10.Name ?? item10.FileName);
					_AppendEmbeddedMods(stringBuilder, item10.EmbeddedMods, 1);
					stringBuilder.AppendLine();
				}
			}
			if (!flag)
			{
				stringBuilder.AppendLine(Lang.Text("Crash.Report.JarInJarMod.JarInJarNone"));
			}
			CrashFileIo.WriteText(Path.Combine(reportFolder, "模组列表.txt"), stringBuilder.ToString(), Encoding.UTF8);
			ModJarInJarCache.Flush();
			LogWrapper.Info("Crash", "已导出模组列表及 Jar-in-Jar 信息");
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash", "导出模组信息失败");
		}
		finally
		{
			ModLocalComp.JijScanContext = null;
			ModJarInJarCache.UseInstance(null);
		}
	}

	private static void _AppendDependencyIssues(StringBuilder sb, ModJarInJarIndex jij, List<ModLocalComp.LocalCompFile> activeMods)
	{
		sb.AppendLine(Lang.Text("Crash.Report.JarInJarMod.WarningSection"));
		bool flag = false;
		foreach (ModLocalComp.LocalCompFile activeMod in activeMods)
		{
			List<string> missing = new List<string>();
			List<string> mismatch = new List<string>();
			List<string> disabled = new List<string>();
			Probe(activeMod, activeMod.DetectedLoader);
			foreach (ModLocalComp.LocalCompFile item in jij.GetLoadableEmbedded(activeMod))
			{
				Probe(item, item.JijLoader);
			}
			if (missing.Count != 0 || mismatch.Count != 0 || disabled.Count != 0)
			{
				List<string> list = new List<string>();
				if (missing.Count > 0)
				{
					list.Add(Lang.Text("Crash.Report.JarInJarMod.WarningMissing", new object[1] { string.Join(", ", missing.Distinct()) }));
				}
				if (mismatch.Count > 0)
				{
					list.Add(Lang.Text("Crash.Report.JarInJarMod.WarningMismatch", new object[1] { string.Join(", ", mismatch.Distinct()) }));
				}
				if (disabled.Count > 0)
				{
					list.Add(Lang.Text("Crash.Report.JarInJarMod.WarningDisabled", new object[1] { string.Join(", ", disabled.Distinct()) }));
				}
				sb.AppendLine("|-> " + (activeMod.Name ?? activeMod.FileName) + ": " + string.Join("; ", list));
				flag = true;
			}
			void Probe(ModLocalComp.LocalCompFile who, string loader)
			{
				foreach (ModJarInJarIndex.DepRow item2 in ModJarInJarIndex.BuildOwnDependencies(who, loader))
				{
					if (!ModDependencyIds.IsPlatform(item2.DepId) && !item2.Optional)
					{
						switch (jij.Analyze(who, item2))
						{
						case JijDepStatus.Missing:
							missing.Add(item2.DepId);
							break;
						case JijDepStatus.VersionMismatch:
							mismatch.Add((item2.Raw == null) ? item2.DepId : (item2.DepId + " " + item2.Raw));
							break;
						case JijDepStatus.Disabled:
							disabled.Add(item2.DepId);
							break;
						}
					}
				}
			}
		}
		if (!flag)
		{
			sb.AppendLine(Lang.Text("Crash.Report.JarInJarMod.WarningNone"));
		}
		sb.AppendLine().AppendLine("----------------------------").AppendLine();
		sb.AppendLine(Lang.Text("Crash.Report.JarInJarMod.ConflictSection"));
		List<(ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, bool)> list2 = jij.FindActiveConflicts();
		if (list2.Count == 0)
		{
			sb.AppendLine(Lang.Text("Crash.Report.JarInJarMod.ConflictNone"));
		}
		else
		{
			foreach (var (localCompFile, localCompFile2, flag2) in list2)
			{
				sb.AppendLine("|-> [" + Lang.Text(flag2 ? "Crash.Report.JarInJarMod.ConflictIncompatible" : "Crash.Report.JarInJarMod.ConflictDiscouraged") + "] " + (localCompFile.Name ?? localCompFile.FileName) + " / " + (localCompFile2.Name ?? localCompFile2.FileName));
			}
		}
		sb.AppendLine().AppendLine("----------------------------").AppendLine();
	}

	private static void _AppendEmbeddedMods(StringBuilder builder, List<ModLocalComp.LocalCompFile> mods, int depth)
	{
		string text = new string('\t', depth);
		foreach (ModLocalComp.LocalCompFile mod in mods)
		{
			string text2 = text + "|-> " + (mod.Name ?? mod.ModId ?? "?");
			string text3 = _CleanVersion(mod.Version);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text2 = text2 + " (" + text3 + ")";
			}
			builder.AppendLine(text2);
			if (mod.EmbeddedMods.Any())
			{
				_AppendEmbeddedMods(builder, mod.EmbeddedMods, depth + 1);
			}
		}
	}

	private static string? _CleanVersion(string? version)
	{
		if (!string.IsNullOrEmpty(version) && !version.Contains("${"))
		{
			return version;
		}
		return null;
	}
}
