using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;

namespace PCL;

public static class ModWatcher
{
	public class LogOutputEventArgs : EventArgs
	{
		public SolidColorBrush color;

		public string logText;

		public LogOutputEventArgs(string logText, SolidColorBrush color)
		{
			this.logText = logText;
			this.color = color;
		}
	}

	private enum GameLogLevel
	{
		Debug,
		Info,
		Warn,
		Error,
		Fatal
	}

	public class Watcher
	{
		public delegate void GameExitEventHandler();

		public delegate void LogOutputEventHandler(Watcher sender, LogOutputEventArgs e);

		public enum MinecraftState
		{
			Loading,
			Running,
			Crashed,
			Ended,
			Canceled
		}

		private delegate bool EnumWindowsSub(nint hwnd, nint lParam);

		private readonly int pid;

		private readonly bool realTime;

		private readonly object waitingLogLock = new object();

		public uint countDebug;

		public uint countError;

		public uint countFatal;

		public uint countInfo;

		public uint countWarn;

		public List<string> fullLog = new List<string>();

		public Process gameProcess;

		private bool isWindowAppeared;

		private bool isWindowFinished;

		public string jStackPath;

		private GameLogLevel lastLevel = GameLogLevel.Info;

		public Queue<string> latestLog = new Queue<string>();

		public ModLoader.LoaderTask<Process, int> loader;

		private int logProgress;

		public McInstance version;

		public List<string> waitingLog = new List<string>(1000);

		private nint windowHandle;

		private string windowTitle = "";

		public MinecraftState State
		{
			get
			{
				return field;
			}
			set
			{
				if (field != value)
				{
					field = value;
					WatcherStateChanged();
				}
			}
		}

		public bool RealTimeLog => realTime;

		public event GameExitEventHandler? GameExit;

		public event LogOutputEventHandler? LogOutput;

		public Watcher(ModLoader.LoaderTask<Process, int> loader, McInstance version, string windowTitle, string jStackPath, bool outputRealTime = false)
		{
			Watcher watcher = this;
			this.loader = loader;
			this.version = version;
			this.windowTitle = windowTitle;
			realTime = outputRealTime;
			pid = loader.input.Id;
			this.jStackPath = jStackPath;
			WatcherLog(Lang.Text("Watcher.Start"));
			if (string.IsNullOrWhiteSpace(windowTitle))
			{
				WatcherLog("要求窗口标题：" + windowTitle);
			}
			List<Watcher> list = new List<Watcher>();
			foreach (Watcher mcWatcher in mcWatcherList)
			{
				if (mcWatcher.State != MinecraftState.Crashed && mcWatcher.State != MinecraftState.Ended && mcWatcher.State != MinecraftState.Canceled)
				{
					list.Add(mcWatcher);
				}
			}
			list.Add(this);
			mcWatcherList = list;
			WatcherStateChanged();
			gameProcess = loader.input;
			gameProcess.BeginOutputReadLine();
			gameProcess.BeginErrorReadLine();
			gameProcess.OutputDataReceived += LogReceived;
			gameProcess.ErrorDataReceived += LogReceived;
			ModBase.RunInNewThread(() =>
			{
				try
				{
					while (watcher.State != MinecraftState.Ended && watcher.State != MinecraftState.Crashed && watcher.State != MinecraftState.Canceled && loader.State != ModBase.LoadState.Aborted)
					{
						watcher.TimerWindow();
						watcher.TimerLog();
						if (!string.IsNullOrWhiteSpace(windowTitle))
						{
							for (int i = 1; i <= 3; i++)
							{
								if (watcher.State == MinecraftState.Running && !watcher.gameProcess.HasExited)
								{
									string lpString = windowTitle.Replace("{date}", Lang.Date(DateTime.Now, "d")).Replace("{time}", Lang.Date(DateTime.Now, "T"));
									SetWindowText(watcher.windowHandle, lpString);
								}
								Thread.Sleep(64);
							}
						}
						Thread.Sleep(10);
					}
					watcher.WatcherLog(Lang.Text("Watcher.Exited"));
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "Minecraft 日志监控主循环出错", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Error.WatcherOperationFailed"));
					watcher.State = MinecraftState.Ended;
				}
			}, "Minecraft Watcher PID " + pid);
		}

		private void LogReceived(object sender, DataReceivedEventArgs e)
		{
			lock (waitingLogLock)
			{
				waitingLog.Add(e.Data);
			}
			if (realTime)
			{
				LogRealTime(e.Data, ref lastLevel);
				if (e.Data != null)
				{
					fullLog.Add(e.Data);
				}
			}
		}

		private void LogRealTime(string line, ref GameLogLevel level)
		{
			if (line != null)
			{
				level = ((line.StartsWithF("\tat ") || line.StartsWithF("Caused by: ") || line.StartsWithF("\t... ")) ? level : GetLevel(line, level));
				SolidColorBrush color = GetColor(level);
				switch (level)
				{
				case GameLogLevel.Debug:
					countDebug = (uint)((ulong)countDebug + 1uL);
					break;
				case GameLogLevel.Info:
					countInfo = (uint)((ulong)countInfo + 1uL);
					break;
				case GameLogLevel.Warn:
					countWarn = (uint)((ulong)countWarn + 1uL);
					break;
				case GameLogLevel.Error:
					countError = (uint)((ulong)countError + 1uL);
					break;
				case GameLogLevel.Fatal:
					countFatal = (uint)((ulong)countFatal + 1uL);
					break;
				}
				LogOutput?.Invoke(this, new LogOutputEventArgs(line, color));
			}
		}

		private void TimerLog()
		{
			try
			{
				List<string> list = new List<string>();
				lock (waitingLogLock)
				{
					if (!waitingLog.Any())
					{
						return;
					}
					list = waitingLog;
					waitingLog = new List<string>(1000);
				}
				foreach (string item in list)
				{
					GameLog(item);
				}
				if (State == MinecraftState.Loading)
				{
					ProgressUpdate();
				}
				if (gameProcess.HasExited)
				{
					WatcherLog(Lang.Text("Watcher.ProcessExited", new object[1] { gameProcess.ExitCode }));
					if (realTime)
					{
						GameLogLevel level = GameLogLevel.Info;
						LogRealTime(Lang.Text("Watcher.ProcessExited", new object[1] { gameProcess.ExitCode }), ref level);
					}
					GameExit?.Invoke();
					if (State == MinecraftState.Loading)
					{
						WatcherLog(Lang.Text("Watcher.Crash.Suspected"));
						Crashed();
					}
					else if (gameProcess.ExitCode != 0 && State == MinecraftState.Running && version.releaseTime.Year >= 2012)
					{
						WatcherLog(Lang.Text("Watcher.Crash.AbnormalExit"));
						Crashed();
					}
					else if (State != MinecraftState.Crashed)
					{
						State = MinecraftState.Ended;
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "输出 Minecraft 日志失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Error.WatcherOperationFailed"));
			}
		}

		private void GameLog(string text)
		{
			if (text == null)
			{
				return;
			}
			text = text.Replace("\r\n", "\r").Replace("\n", "\r").Replace("\r", "\r\n");
			latestLog.Enqueue(text);
			if (latestLog.Count >= 501)
			{
				latestLog.Dequeue();
			}
			if (logProgress < 1)
			{
				WatcherLog(Lang.Text("Watcher.Progress.LogAppeared"));
				logProgress = 1;
			}
			if (logProgress < 2 && text.Contains("Setting user:"))
			{
				WatcherLog(Lang.Text("Watcher.Progress.UserSet"));
				logProgress = 2;
			}
			else if (logProgress < 3 && text.ContainsF("lwjgl version", ignoreCase: true))
			{
				WatcherLog(Lang.Text("Watcher.Progress.LwjglConfirmed"));
				logProgress = 3;
			}
			else if (logProgress < 4 && (text.Contains("OpenAL initialized") || text.Contains("Starting up SoundSystem")))
			{
				WatcherLog(Lang.Text("Watcher.Progress.OpenAlLoaded"));
				logProgress = 4;
			}
			else if (logProgress < 5 && ((text.Contains("Created") && text.Contains("textures") && text.Contains("-atlas")) || text.Contains("Found animation info")))
			{
				WatcherLog(Lang.Text("Watcher.Progress.TexturesLoaded"));
				logProgress = 5;
			}
			if (!text.Contains("[CHAT]"))
			{
				if (text.Contains("Someone is closing me!") || text.Contains("Restarting Minecraft with command"))
				{
					WatcherLog(Lang.Text("Watcher.Log.CloseDetected", new object[1] { text }));
					State = MinecraftState.Ended;
				}
				else if (text.Contains("Crash report saved to") || text.Contains("This crash report has been saved to:"))
				{
					WatcherLog(Lang.Text("Watcher.Log.CrashDetected", new object[1] { text }));
					Crashed();
				}
				else if (text.Contains("Could not save crash report to"))
				{
					WatcherLog(Lang.Text("Watcher.Log.CrashDetected", new object[1] { text }));
					Crashed();
				}
				else if (text.Contains("/ERROR]: Unable to launch") || text.Contains("An exception was thrown, the game will display an error screen and halt."))
				{
					WatcherLog(Lang.Text("Watcher.Log.CrashDetected", new object[1] { text }));
					Crashed();
				}
			}
		}

		private void WatcherLog(string text)
		{
			ModLaunch.McLaunchLog("[" + pid + "] " + text);
		}

		private void ProgressUpdate()
		{
			double progress;
			if (isWindowAppeared || logProgress >= 4)
			{
				progress = 0.95;
				WatcherLog(Lang.Text("Watcher.LoadComplete"));
				State = MinecraftState.Running;
			}
			else
			{
				progress = (double)Math.Min(logProgress, 3) / 3.0 * 0.9;
			}
			loader.Progress = progress;
		}

		private void TimerWindow()
		{
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Invalid comparison between Unknown and I4
			try
			{
				if (gameProcess.HasExited || isWindowFinished)
				{
					return;
				}
				KeyValuePair<nint, string>? keyValuePair = null;
				try
				{
					keyValuePair = TryGetMinecraftWindow();
				}
				catch (Win32Exception ex)
				{
					ModBase.Log(ex, Lang.Text("Watcher.SecurityBlocked"), ModBase.LogLevel.Hint, null, Lang.Text("Watcher.SecurityBlocked"));
					isWindowFinished = true;
				}
				if (!keyValuePair.HasValue)
				{
					return;
				}
				string value = keyValuePair.Value.Value;
				nint minecraftWindowHandle = keyValuePair.Value.Key;
				if (!value.StartsWithF("FML") && !value.StartsWithF("Quilt Loader"))
				{
					windowHandle = minecraftWindowHandle;
					WatcherLog(Lang.Text("Watcher.WindowLoaded", new object[2]
					{
						value,
						((IntPtr)minecraftWindowHandle).ToInt64()
					}));
					isWindowFinished = true;
					if ((int)Config.Launch.GameWindowMode == 4)
					{
						ModBase.RunInNewThread(() =>
						{
							try
							{
								Thread.Sleep(2000);
								ShowWindow(windowHandle, 3u);
								WatcherLog(Lang.Text("Watcher.WindowMaximized", new object[1] { ((IntPtr)minecraftWindowHandle).ToInt64() }));
							}
							catch (Exception ex3)
							{
								ModBase.Log(ex3, "最大化 Minecraft 窗口时出现错误");
							}
						}, "MinecraftWindowMaximize");
					}
				}
				else if (!isWindowAppeared)
				{
					WatcherLog(Lang.Text("Watcher.FmlWindowLoaded", new object[2]
					{
						value,
						((IntPtr)minecraftWindowHandle).ToInt64()
					}));
				}
				isWindowAppeared = true;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "检查 Minecraft 窗口失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Error.WatcherOperationFailed"));
			}
		}

		private KeyValuePair<nint, string>? TryGetMinecraftWindow()
		{
			KeyValuePair<nint, string>? tryGetMinecraftWindowRet = null;
			tryGetMinecraftWindowRet = null;
			EnumWindows((nint hwnd, nint lParam) =>
			{
				if (tryGetMinecraftWindowRet.HasValue)
				{
					return false;
				}
				StringBuilder stringBuilder = new StringBuilder(512);
				GetClassName(hwnd, stringBuilder, stringBuilder.Capacity);
				switch (stringBuilder.ToString())
				{
				default:
					return true;
				case "GLFW30":
				case "LWJGL":
				case "SunAwtFrame":
				{
					stringBuilder = new StringBuilder(512);
					GetWindowText(hwnd, stringBuilder, stringBuilder.Capacity);
					string text = stringBuilder.ToString();
					if (!text.StartsWithF("FML") && (!(text != "PopupMessageWindow") || text.StartsWithF("GLFW")))
					{
						return true;
					}
					int lpdwProcessId = 0;
					GetWindowThreadProcessId(hwnd, ref lpdwProcessId);
					try
					{
						if (lpdwProcessId != gameProcess.Id)
						{
							return true;
						}
					}
					catch (Exception)
					{
						return true;
					}
					tryGetMinecraftWindowRet = new KeyValuePair<nint, string>(hwnd, text);
					return false;
				}
				}
			}, IntPtr.Zero);
			return tryGetMinecraftWindowRet;
		}

		[DllImport("user32")]
		private static extern bool EnumWindows(EnumWindowsSub lpEnumFunc, nint lParam);

		[DllImport("user32", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
		private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
		private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32", CharSet = CharSet.Unicode, EntryPoint = "SetWindowTextW")]
		private static extern bool SetWindowText(nint hWnd, string lpString);

		[DllImport("user32")]
		private static extern bool ShowWindow(nint hWnd, uint cmdWindow);

		[DllImport("user32")]
		private static extern int GetWindowThreadProcessId(nint hWnd, ref int lpdwProcessId);

		private void Crashed()
		{
			MinecraftState state = State;
			if ((uint)(state - 2) <= 1u)
			{
				return;
			}
			State = MinecraftState.Crashed;
			if (!Config.Launch.DisableCrashAnalysis)
			{
				WatcherLog(Lang.Text("Watcher.Crash.Detected"));
				HintService.Hint(Lang.Text("Watcher.Crash.Hint"));
				ModBase.FeedbackInfo();
				ModBase.RunInNewThread(() =>
				{
					try
					{
						Thread.Sleep(2000);
						WatcherLog(Lang.Text("Watcher.Crash.AnalysisStart"));
						CrashAnalyzer crashAnalyzer = new CrashAnalyzer(pid);
						crashAnalyzer.Collect(version.PathIndie, latestLog.ToList());
						crashAnalyzer.Prepare();
						crashAnalyzer.Analyze(version);
						int num = 3;
						List<string> list = new List<string>(num);
						CollectionsMarshal.SetCount(list, num);
						Span<string> span = CollectionsMarshal.AsSpan(list);
						span[0] = version.PathInstance + version.Name + ".json";
						span[1] = LogWrapper.CurrentLogger.CurrentLogFiles.Last();
						span[2] = ModBase.exePath + "PCL\\LatestLaunch.bat";
						crashAnalyzer.Output(isHandAnalyze: false, list);
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "崩溃分析失败", ModBase.LogLevel.Feedback, null, Lang.Text("Crash.Analysis.Error.Failed"));
					}
				}, "Crash Analyzer");
			}
			else
			{
				WatcherLog(Lang.Text("Watcher.Crash.DetectedDisabled"));
			}
		}

		public bool CheckAlive(Process p)
		{
			if (!p.HasExited)
			{
				return true;
			}
			if (Array.Exists(Process.GetProcesses(), (Process item) => item.Id == p.Id))
			{
				return true;
			}
			return false;
		}

		public void Kill()
		{
			State = MinecraftState.Canceled;
			ModBase.RunInNewThread(() =>
			{
				WatcherLog(Lang.Text("Watcher.Kill.Attempt"));
				try
				{
					if (CheckAlive(gameProcess))
					{
						gameProcess.Kill();
					}
					gameProcess.WaitForExit(5000);
					if (CheckAlive(gameProcess))
					{
						WatcherLog(Lang.Text("Watcher.Kill.TaskkillAttempt"));
						string text = Process.Start(new ProcessStartInfo
						{
							FileName = "taskkill.exe",
							Arguments = $"/PID {gameProcess.Id} /F /T",
							RedirectStandardOutput = true,
							UseShellExecute = false
						}).StandardOutput.ReadToEnd();
						WatcherLog(Lang.Text("Watcher.Kill.TaskkillResult", new object[1] { text }));
						gameProcess.WaitForExit(5000);
						if (CheckAlive(gameProcess))
						{
							WatcherLog(Lang.Text("Watcher.Kill.Timeout"));
							return;
						}
					}
					WatcherLog(Lang.Text("Watcher.Kill.Success"));
					if (realTime)
					{
						GameLogLevel level = GameLogLevel.Info;
						LogRealTime(Lang.Text("Watcher.ProcessExited", new object[1] { gameProcess.ExitCode }), ref level);
					}
					GameExit?.Invoke();
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, Lang.Text("Watcher.Kill.Failed"), ModBase.LogLevel.Hint, null, Lang.Text("Watcher.Kill.Failed"));
				}
			});
		}

		public List<string> ExportStackDump(string savePath)
		{
			List<string> list = new List<string>();
			for (int i = 1; i <= 3; i++)
			{
				list.Add(ModBase.ShellAndGetOutput(jStackPath, "-l -e " + gameProcess.Id));
				Thread.Sleep(3000);
			}
			return list;
		}
	}

	public static List<Watcher> mcWatcherList = new List<Watcher>();

	private static bool isWatcherRunning;

	public static bool hasRunningMinecraft;

	private static void WatcherStateChanged()
	{
		bool flag = false;
		bool triggerLauncherShutdown = true;
		foreach (Watcher mcWatcher in mcWatcherList)
		{
			if (mcWatcher.State == Watcher.MinecraftState.Loading || mcWatcher.State == Watcher.MinecraftState.Running)
			{
				flag = true;
				break;
			}
			if (mcWatcher.State == Watcher.MinecraftState.Crashed || mcWatcher.State == Watcher.MinecraftState.Canceled)
			{
				triggerLauncherShutdown = false;
			}
		}
		if (isWatcherRunning != flag)
		{
			isWatcherRunning = flag;
			if (isWatcherRunning)
			{
				MinecraftStart();
			}
			else
			{
				MinecraftStop(triggerLauncherShutdown);
			}
		}
	}

	private static void MinecraftStart()
	{
		ModLaunch.McLaunchLog("[全局] 出现运行中的 Minecraft");
		hasRunningMinecraft = true;
		ModMain.frmMain.BtnExtraShutdown.ShowRefresh();
	}

	private static void MinecraftStop(bool triggerLauncherShutdown)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Invalid comparison between Unknown and I4
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		ModLaunch.McLaunchLog("[全局] 已无运行中的 Minecraft");
		hasRunningMinecraft = false;
		ModMain.frmMain.BtnExtraShutdown.ShowRefresh();
		if (Config.Preference.Music.StopInGame)
		{
			ModBase.RunInUi(() =>
			{
				if (ModMusic.MusicResume())
				{
					ModBase.Log("[Music] 已根据设置，在结束后开始音乐播放");
				}
			});
		}
		else if (Config.Preference.Music.StartInGame)
		{
			ModBase.RunInUi(() =>
			{
				if (ModMusic.MusicPause())
				{
					ModBase.Log("[Music] 已根据设置，在结束后暂停音乐播放");
				}
			});
		}
		ModVideoBack.IsGaming = false;
		ModVideoBack.VideoPlay();
		LauncherVisibility launcherVisibility = Config.Launch.LauncherVisibility;
		if ((int)launcherVisibility != 2)
		{
			if ((int)launcherVisibility == 3)
			{
				ModBase.RunInUi(() =>
				{
					ModMain.frmMain.Hidden = false;
				});
			}
		}
		else if (triggerLauncherShutdown)
		{
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.EndProgram(sendWarning: false);
			});
		}
		else
		{
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.Hidden = false;
			});
		}
	}

	private static GameLogLevel GetLevel(string line, GameLogLevel lastLevel)
	{
		_ = (Func<string, SolidColorBrush>)((string name) => (SolidColorBrush)System.Windows.Application.Current.Resources[name]);
		string str = line.Split(": ")[0];
		if (str.ContainsF("FATAL"))
		{
			return GameLogLevel.Fatal;
		}
		if (str.ContainsF("ERROR"))
		{
			return GameLogLevel.Error;
		}
		if (str.ContainsF("WARN"))
		{
			return GameLogLevel.Warn;
		}
		if (str.ContainsF("INFO"))
		{
			return GameLogLevel.Info;
		}
		if (str.ContainsF("DEBUG"))
		{
			return GameLogLevel.Debug;
		}
		if (line.StartsWithF("Exception in thread \""))
		{
			return GameLogLevel.Error;
		}
		if ((line.ContainsF("Exception") || line.ContainsF("Realms authentication error with message ")) && lastLevel >= GameLogLevel.Warn)
		{
			return lastLevel;
		}
		if (line.StartsWithF("\tat ") && lastLevel >= GameLogLevel.Warn)
		{
			return lastLevel;
		}
		return GameLogLevel.Info;
	}

	private static SolidColorBrush GetColor(GameLogLevel level)
	{
		Func<string, SolidColorBrush> func = (string name) => (SolidColorBrush)System.Windows.Application.Current.Resources[name];
		switch (level)
		{
		case GameLogLevel.Debug:
			return func("ColorBrushDebug");
		case GameLogLevel.Info:
			func(ThemeManager.IsDarkMode ? "ColorBrushInfoDark" : "ColorBrushInfo");
			break;
		case GameLogLevel.Warn:
			return func("ColorBrushWarn");
		case GameLogLevel.Error:
			return func("ColorBrushError");
		case GameLogLevel.Fatal:
			return func("ColorBrushFatal");
		}
		return func(ThemeManager.IsDarkMode ? "ColorBrushInfoDark" : "ColorBrushInfo");
	}
}
