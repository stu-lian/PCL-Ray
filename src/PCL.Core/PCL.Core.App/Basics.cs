using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows;
using PCL.Core.Logging;
using PCL.Core.Utils;

namespace PCL.Core.App;

public static class Basics
{
	private const string AssemblyImagePath = "pack://application:,,,/Plain Craft Launcher 2;component/Images/";

	public static MetadataModel Metadata { get; } = JsonSerializer.Deserialize<MetadataModel>(Assembly.GetEntryAssembly().GetManifestResourceStream("PCL.metadata.json"), JsonCompat.SerializerOptions);

	public static string VersionName => Metadata.Version.BaseName;

	public static int VersionCode => Metadata.Version.Code;

	public static string VersionBranch => Metadata.Version.BranchName;

	public static bool IsAprilFool
	{
		get
		{
			DateTime now = DateTime.Now;
			if (now.Month == 4)
			{
				return now.Day == 1;
			}
			return false;
		}
	}

	public static Process CurrentProcess { get; } = Process.GetCurrentProcess();

	public static int CurrentProcessId { get; } = CurrentProcess.Id;

	public static string ExecutablePath { get; } = Environment.ProcessPath;

	public static string ExecutableDirectory { get; } = GetParentPath(ExecutablePath) ?? CurrentDirectory;

	public static string ExecutableName { get; } = Path.GetFileName(ExecutablePath);

	public static string ExecutableNameWithoutExtension { get; } = Path.GetFileNameWithoutExtension(ExecutablePath);

	public static string[] FullCommandLineArguments { get; } = Environment.GetCommandLineArgs();

	public static string[] CommandLineArguments { get; } = FullCommandLineArguments[1..];

	public static string CurrentDirectory => Environment.CurrentDirectory;

	public static Thread RunInNewThread(Action action, string? name = null, ThreadPriority priority = ThreadPriority.Normal)
	{
		AtomicVariable<string> threadName = new AtomicVariable<string>(name);
		Thread thread = new Thread(() =>
		{
			try
			{
				action();
			}
			catch (ThreadInterruptedException)
			{
				LogWrapper.Trace("Thread", threadName.Value + ": 已中止");
			}
			catch (Exception ex2)
			{
				LogWrapper.Error(ex2, "Thread", threadName.Value + ": 抛出异常");
			}
		})
		{
			Priority = priority
		};
		AtomicVariable<string> atomicVariable = threadName;
		if (atomicVariable.Value == null)
		{
			string text = (atomicVariable.Value = $"Worker#{thread.ManagedThreadId}");
		}
		thread.Name = threadName.Value;
		thread.Start();
		return thread;
	}

	public static string? GetParentPath(string path)
	{
		return Path.GetDirectoryName(path) ?? Path.GetPathRoot(path);
	}

	public static string GetParentPathOrEmpty(string path)
	{
		return GetParentPath(path) ?? string.Empty;
	}

	public static string GetParentPathOrDefault(string path)
	{
		return GetParentPath(path) ?? CurrentDirectory;
	}

	public static void OpenPath(string path, string? workingDirectory = null)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo(path)
		{
			WorkingDirectory = (workingDirectory ?? CurrentDirectory),
			UseShellExecute = true,
			CreateNoWindow = true
		};
		try
		{
			Process.Start(startInfo);
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode == 1155 && File.Exists(path))
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "notepad.exe",
				Arguments = "\"" + path + "\"",
				UseShellExecute = false
			});
		}
	}

	public static Stream? GetResourceStream(string path)
	{
		return Application.GetResourceStream(new Uri("pack://application:,,,/" + path, UriKind.Absolute))?.Stream;
	}

	public static string GetAppImagePath(string imageName)
	{
		return "pack://application:,,,/Plain Craft Launcher 2;component/Images/" + imageName;
	}
}
