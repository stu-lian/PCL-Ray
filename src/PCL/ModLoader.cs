using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shell;
using Microsoft.VisualBasic.CompilerServices;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

public static class ModLoader
{
	public enum LoaderFolderRunType
	{
		RunOnUpdated,
		ForceRun,
		UpdateOnly
	}

	public abstract class LoaderBase : ILoadingTrigger
	{
		public delegate void OnStateChangedThreadEventHandler(LoaderBase loader, ModBase.LoadState newState, ModBase.LoadState oldState);

		public delegate void OnStateChangedUiEventHandler(LoaderBase loader, ModBase.LoadState newState, ModBase.LoadState oldState);

		public delegate void PreviewFinishEventHandler(LoaderBase loader);

		public const string waitForExitTimeoutMessage = "等待加载器执行超时。";

		public readonly object lockState = new object();

		public bool block = true;

		public bool hasOnStateChangedThread;

		public bool isForceRestarting;

		public string name;

		public LoaderBase parent;

		public bool show = true;

		public int Uuid = ModBase.GetUuid();

		public LoaderBase RealParent
		{
			get
			{
				LoaderBase loaderBase = null;
				try
				{
					loaderBase = parent;
					while (loaderBase != null && loaderBase.parent != null)
					{
						loaderBase = loaderBase.parent;
					}
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "获取父加载器失败（" + name + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Application.Loader.Error.OperationFailed"));
					return null;
				}
				return loaderBase;
			}
		}

		public Action<LoaderBase> OnStateChanged
		{
			set
			{
				OnStateChangedUi += (LoaderBase loader, ModBase.LoadState newState, ModBase.LoadState oldState) =>
				{
					value(loader);
				};
			}
		}

		public ModBase.LoadState State
		{
			get
			{
				return field;
			}
			set
			{
				if (field == value)
				{
					return;
				}
				ModBase.LoadState oldState = field;
				if (value == ModBase.LoadState.Finished && Config.Debug.AddRandomDelay)
				{
					Thread.Sleep(RandomUtils.NextInt(100, 2000));
				}
				field = value;
				ModBase.Log("[Loader] 加载器 " + name + " 状态改变：" + ModBase.GetStringFromEnum(value));
				ModBase.RunInUi(() =>
				{
					switch (value)
					{
					case ModBase.LoadState.Loading:
						LoadingState = MyLoading.MyLoadingState.Run;
						break;
					case ModBase.LoadState.Failed:
						LoadingState = MyLoading.MyLoadingState.Error;
						break;
					default:
						LoadingState = MyLoading.MyLoadingState.Stop;
						break;
					}
					OnStateChangedUi?.Invoke(this, value, oldState);
				});
				if (hasOnStateChangedThread)
				{
					ModBase.RunInThread(() =>
					{
						OnStateChangedThread?.Invoke(this, value, oldState);
					});
				}
			}
		}

		public Exception Error { get; set; }

		public virtual double Progress
		{
			get
			{
				switch (State)
				{
				case ModBase.LoadState.Waiting:
					return 0.0;
				case ModBase.LoadState.Loading:
					if (field != -1.0)
					{
						return field;
					}
					return 0.02;
				default:
					return 1.0;
				}
			}
			set
			{
				if (field != value)
				{
					double oldProgress = field;
					field = value;
					ProgressChanged?.Invoke(value, oldProgress);
				}
			}
		} = -1.0;

		public double ProgressWeight { get; set; } = 1.0;

		public bool IsLoader { get; } = true;

		public MyLoading.MyLoadingState LoadingState
		{
			get
			{
				return field;
			}
			set
			{
				if (field != value)
				{
					MyLoading.MyLoadingState oldState = field;
					field = value;
					LoadingStateChanged?.Invoke(value, oldState);
				}
			}
		} = MyLoading.MyLoadingState.Stop;

		public event ILoadingTrigger.LoadingStateChangedEventHandler? LoadingStateChanged;

		public event ILoadingTrigger.ProgressChangedEventHandler? ProgressChanged;

		public event OnStateChangedThreadEventHandler? OnStateChangedThread;

		public event OnStateChangedUiEventHandler? OnStateChangedUi;

		public event PreviewFinishEventHandler? PreviewFinish;

		public LoaderBase()
		{
			name = "未命名任务 " + Uuid + "#";
		}

		public virtual void InitParent(LoaderBase parent)
		{
			this.parent = parent;
		}

		protected void RaisePreviewFinish()
		{
			PreviewFinish?.Invoke(this);
		}

		public abstract void Start(object? input = null, bool isForceRestart = false);

		public abstract void Abort();

		public void WaitForExit(object input = null, LoaderBase loaderToSyncProgress = null, bool isForceRestart = false)
		{
			Start(input, isForceRestart);
			while (State == ModBase.LoadState.Loading)
			{
				if (loaderToSyncProgress != null)
				{
					loaderToSyncProgress.Progress = Progress;
				}
				Thread.Sleep(10);
			}
			if (State != ModBase.LoadState.Finished)
			{
				if (State == ModBase.LoadState.Aborted)
				{
					throw new ThreadInterruptedException("加载器执行已中断。");
				}
				if (Error == null)
				{
					throw new InvalidOperationException("未知错误！");
				}
				throw Error;
			}
		}

		public void WaitForExitTime(int timeout, object input = null, string timeoutMessage = "等待加载器执行超时。", object loaderToSyncProgress = null, bool isForceRestart = false)
		{
			Start(input, isForceRestart);
			while (State == ModBase.LoadState.Loading)
			{
				if (loaderToSyncProgress != null)
				{
					((dynamic)loaderToSyncProgress).Progress = Progress;
				}
				Thread.Sleep(10);
				timeout -= 10;
				if (timeout < 0)
				{
					throw new TimeoutException(timeoutMessage);
				}
			}
			if (State != ModBase.LoadState.Finished)
			{
				if (State == ModBase.LoadState.Aborted)
				{
					throw new ThreadInterruptedException("加载器执行已中断。");
				}
				if (Error == null)
				{
					throw new InvalidOperationException("未知错误！");
				}
				throw Error;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is LoaderBase loaderBase)
			{
				return Uuid == loaderBase.Uuid;
			}
			return false;
		}
	}

	public abstract class LoaderTask : LoaderBase
	{
		public long lastFinishedTime;

		public Task? lastRunningTask;

		public int reloadTimeout = -1;

		public bool IsAborted => IsAbortedWithThread(Task.CurrentId ?? (-1));

		public bool IsAbortedWithThread(int compareTaskId)
		{
			if (lastRunningTask != null && compareTaskId == lastRunningTask.Id)
			{
				return State == ModBase.LoadState.Aborted;
			}
			return true;
		}

		public abstract bool ShouldStart(ref object? input, bool isForceRestart = false, bool ignoreReloadTimeout = false);

		public abstract object? StartGetInputNoType(object? input = null, Func<object>? inputDelegate = null);
	}

	public class LoaderTask<InputType, OutputType> : LoaderTask
	{
		public InputType input;

		protected internal Func<InputType?>? inputDelegate;

		protected internal Action<LoaderTask<InputType, OutputType>> loadDelegate;

		public OutputType output;

		private CancellationTokenSource? cancelToken;

		protected internal ThreadPriority threadPriority;

		public CancellationToken AbortedToken => cancelToken?.Token ?? new CancellationToken(canceled: true);

		public LoaderTask(string name, Action<LoaderTask<InputType, OutputType>> loadDelegate, Func<InputType?>? inputDelegate = null, ThreadPriority priority = ThreadPriority.Normal)
		{
			base.name = name;
			this.loadDelegate = loadDelegate;
			this.inputDelegate = inputDelegate;
		}

		public InputType? StartGetInput(InputType? input = default(InputType?), Func<InputType?>? inputDelegate = null)
		{
			if (inputDelegate == null)
			{
				inputDelegate = this.inputDelegate;
			}
			if (EqualityComparer<InputType>.Default.Equals(input, default) && inputDelegate != null)
			{
				ModBase.RunInUiWait(() => input = inputDelegate());
			}
			return input;
		}

		public override object? StartGetInputNoType(object? input = null, Func<object?>? inputDelegate = null)
		{
			return StartGetInput((input == null) ? default(InputType) : ((InputType)input), (inputDelegate == null) ? null : ((Func<InputType>)(() => (InputType)inputDelegate())));
		}

		public override bool ShouldStart(ref object? input, bool isForceRestart = false, bool ignoreReloadTimeout = false)
		{
			try
			{
				input = StartGetInput(Conversions.ToGenericParameter<InputType>(input));
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "加载输入获取失败（" + name + "）", ModBase.LogLevel.Hint, null, Lang.Text("Application.Loader.Error.OperationFailed"));
				Error = ex;
				lock (lockState)
				{
					State = ModBase.LoadState.Failed;
				}
			}
			if (isForceRestart)
			{
				return true;
			}
			if (input == null != (this.input == null) || (input != null && !input.Equals(this.input)))
			{
				return true;
			}
			if ((State == ModBase.LoadState.Loading || State == ModBase.LoadState.Finished) && (ignoreReloadTimeout || reloadTimeout == -1 || lastFinishedTime == 0L || TimeUtils.GetTimeTick() - lastFinishedTime < reloadTimeout))
			{
				return false;
			}
			return true;
		}

		public override void Start(object input = null, bool isForceRestart = false)
		{
			if (!ShouldStart(ref input, isForceRestart))
			{
				return;
			}
			if (State == ModBase.LoadState.Loading)
			{
				TriggerThreadAbort();
			}
			this.input = Conversions.ToGenericParameter<InputType>(input);
			lock (lockState)
			{
				State = ModBase.LoadState.Loading;
				Progress = -1.0;
			}
			lastRunningTask = Task.Run(() =>
			{
				try
				{
					isForceRestarting = isForceRestart;
					if (ModBase.modeDebug)
					{
						ModBase.Log($"[Loader] 加载线程 {name} ({Task.CurrentId}) 已{(isForceRestarting ? "强制" : "")}启动");
					}
					loadDelegate(this);
					if (IsAborted)
					{
						ModBase.Log($"[Loader] 加载线程 {name} ({Task.CurrentId}) 已中断但线程正常运行至结束，输出被弃用（最新线程：{((lastRunningTask == null) ? (-1) : lastRunningTask.Id)}）", ModBase.LogLevel.Developer);
					}
					else
					{
						if (ModBase.modeDebug)
						{
							ModBase.Log($"[Loader] 加载线程 {name} ({Task.CurrentId}) 已完成");
						}
						RaisePreviewFinish();
						State = ModBase.LoadState.Finished;
						lastFinishedTime = TimeUtils.GetTimeTick();
					}
				}
				catch (ModBase.CancelledException ex)
				{
					if (ModBase.modeDebug)
					{
						ModBase.Log(ex, $"加载线程 {name} ({Task.CurrentId}) 已触发取消中断，已完成 {Math.Round(Progress * 100.0)}%");
					}
					if (!IsAborted)
					{
						State = ModBase.LoadState.Aborted;
					}
				}
				catch (ThreadInterruptedException ex2)
				{
					if (ModBase.modeDebug)
					{
						ModBase.Log(ex2, $"加载线程 {name} ({Task.CurrentId}) 已触发线程中断，已完成 {Math.Round(Progress * 100.0)}%");
					}
					if (!IsAborted)
					{
						State = ModBase.LoadState.Aborted;
					}
				}
				catch (Exception ex3)
				{
					if (!IsAborted)
					{
						ModBase.Log(ex3, $"加载线程 {name} ({Task.CurrentId}) 出错，已完成 {Math.Round(Progress * 100.0)}%", ModBase.LogLevel.Developer);
						Error = ex3;
						State = ModBase.LoadState.Failed;
					}
				}
			}, (cancelToken ?? (cancelToken = new CancellationTokenSource())).Token);
		}

		public override void Abort()
		{
			if (State == ModBase.LoadState.Loading)
			{
				lock (lockState)
				{
					State = ModBase.LoadState.Aborted;
				}
				TriggerThreadAbort();
			}
		}

		private void TriggerThreadAbort()
		{
			if (lastRunningTask != null)
			{
				if (ModBase.modeDebug)
				{
					ModBase.Log($"[Loader] 加载线程 {name} ({lastRunningTask.Id}) 已中断");
				}
				if (!lastRunningTask.IsCompleted)
				{
					cancelToken?.Cancel();
				}
				lastRunningTask = null;
				cancelToken = null;
			}
		}
	}

	public class LoaderCombo : LoaderBase
	{
		public object? input;

		public List<LoaderBase> loaders = new List<LoaderBase>();

		public override double Progress
		{
			get
			{
				switch (State)
				{
				case ModBase.LoadState.Waiting:
					return 0.0;
				case ModBase.LoadState.Loading:
				{
					double num = 0.0;
					double num2 = 0.0;
					foreach (LoaderBase loader in loaders)
					{
						num += loader.ProgressWeight;
						num2 += loader.ProgressWeight * loader.Progress;
					}
					if (num == 0.0)
					{
						return 0.0;
					}
					return num2 / num;
				}
				default:
					return 1.0;
				}
			}
			set
			{
				throw new Exception("多重加载器不支持设置进度");
			}
		}

		public LoaderCombo(string name, IEnumerable<LoaderBase> loaders)
		{
			this.loaders.Clear();
			foreach (LoaderBase loader in loaders)
			{
				if (loader != null)
				{
					this.loaders.Add(loader);
					loader.OnStateChangedThread += SubTaskStateChanged;
					loader.hasOnStateChangedThread = true;
				}
			}
			InitParent(null);
			base.name = name;
		}

		public override void InitParent(LoaderBase parent)
		{
			base.parent = parent;
			foreach (LoaderBase loader in loaders)
			{
				loader.InitParent(this);
			}
		}

		public override void Start(object input = null, bool isForceRestart = false)
		{
			isForceRestarting = isForceRestart;
			lock (lockState)
			{
				if (State == ModBase.LoadState.Loading)
				{
					return;
				}
				State = ModBase.LoadState.Loading;
			}
			this.input = input;
			if (isForceRestart)
			{
				foreach (LoaderBase loader in loaders)
				{
					loader.State = ModBase.LoadState.Waiting;
				}
			}
			ModBase.RunInThread(Update);
		}

		public override void Abort()
		{
			lock (lockState)
			{
				if (State != ModBase.LoadState.Loading && State != ModBase.LoadState.Waiting)
				{
					return;
				}
			}
			foreach (LoaderBase loader in loaders)
			{
				loader.Abort();
			}
			lock (lockState)
			{
				if (State == ModBase.LoadState.Loading || State == ModBase.LoadState.Waiting)
				{
					State = ModBase.LoadState.Aborted;
				}
			}
		}

		private void SubTaskStateChanged(LoaderBase loader, ModBase.LoadState newState, ModBase.LoadState oldState)
		{
			switch (newState)
			{
			case ModBase.LoadState.Finished:
				Update();
				return;
			case ModBase.LoadState.Aborted:
				Abort();
				return;
			case ModBase.LoadState.Waiting:
			case ModBase.LoadState.Loading:
				return;
			}
			lock (lockState)
			{
				if (State >= ModBase.LoadState.Finished)
				{
					return;
				}
				Error = new Exception(loader.name + "失败", loader.Error);
				State = loader.State;
			}
			foreach (LoaderBase loader2 in loaders)
			{
				loader = loader2;
				loader.Abort();
			}
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		}

		private void Update()
		{
			if (State == ModBase.LoadState.Finished || State == ModBase.LoadState.Failed || State == ModBase.LoadState.Aborted)
			{
				return;
			}
			bool flag = true;
			bool flag2 = false;
			object obj = input;
			foreach (LoaderBase loader in loaders)
			{
				switch (loader.State)
				{
				case ModBase.LoadState.Finished:
					if (loader is LoaderTask loaderTask2)
					{
						Type type2 = loaderTask2.GetType().GenericTypeArguments.FirstOrDefault();
						object obj3 = ((obj != null && type2 == obj.GetType()) ? obj : null);
						if (loaderTask2.ShouldStart(ref obj3, isForceRestart: false, ignoreReloadTimeout: true))
						{
							ModBase.Log("[Loader] 由于输入条件变更，重启已完成的加载器 " + loader.name);
							break;
						}
						obj = ((dynamic)loader).output;
					}
					if (loader.block && !flag)
					{
						flag2 = true;
					}
					continue;
				case ModBase.LoadState.Loading:
					if (loader is LoaderTask loaderTask)
					{
						Type type = loader.GetType().GenericTypeArguments.FirstOrDefault();
						object obj2 = ((obj != null && type == obj.GetType()) ? obj : null);
						if (loaderTask.ShouldStart(ref obj2, isForceRestart: false, ignoreReloadTimeout: true))
						{
							ModBase.Log("[Loader] 由于输入条件变更，重启进行中的加载器 " + loader.name, ModBase.LogLevel.Developer);
							break;
						}
					}
					flag = false;
					flag2 = true;
					continue;
				}
				flag = false;
				if (flag2)
				{
					continue;
				}
				if (obj != null)
				{
					if (!(loader is LoaderTask) && !(loader is LoaderCombo))
					{
						if (loader == null)
						{
							throw new Exception($"未知的加载器类型（{loader?.GetType()}）");
						}
						loader.Start((obj is List<DownloadFile>) ? obj : null, isForceRestarting);
					}
					else
					{
						Type type3 = loader.GetType().GenericTypeArguments.FirstOrDefault();
						loader.Start((type3 == obj.GetType()) ? obj : null, isForceRestarting);
					}
				}
				else
				{
					loader.Start(null, isForceRestarting);
				}
				if (loader.block)
				{
					flag2 = true;
				}
			}
			if (flag)
			{
				RaisePreviewFinish();
				State = ModBase.LoadState.Finished;
				ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			}
		}

		public static void GetLoaderList(LoaderCombo loader, ref List<LoaderBase> list, bool requireShow = true)
		{
			foreach (LoaderBase loader3 in loader.loaders)
			{
				if (loader3.show || !requireShow)
				{
					list.Add(loader3);
				}
				if (loader3 is LoaderCombo loader2)
				{
					GetLoaderList(loader2, ref list);
				}
			}
		}

		public void GetLoaderList(ref List<LoaderBase> list, bool requireShow = true)
		{
			GetLoaderList(this, ref list, requireShow);
		}

		public List<LoaderBase> GetLoaderList(bool requireShow = true)
		{
			List<LoaderBase> list = new List<LoaderBase>();
			GetLoaderList(ref list, requireShow);
			return list;
		}
	}

	public class LoaderCombo<InputType> : LoaderCombo
	{
		public InputType typedInput;

		public LoaderCombo(string name, IEnumerable<LoaderBase> loaders)
			: base(name, loaders)
		{
		}

		public override void Start(object input = null, bool isForceRestart = false)
		{
			typedInput = Conversions.ToGenericParameter<InputType>(input);
			base.Start(typedInput, isForceRestart);
		}
	}

	private struct LoaderFolderDictionaryEntry
	{
		public DateTime? lastCheckTime;

		public string folderPath;

		public override bool Equals(object obj)
		{
			if (!(obj is LoaderFolderDictionaryEntry loaderFolderDictionaryEntry))
			{
				return false;
			}
			if (EqualityComparer<DateTime?>.Default.Equals(lastCheckTime, loaderFolderDictionaryEntry.lastCheckTime))
			{
				return (folderPath ?? "") == (loaderFolderDictionaryEntry.folderPath ?? "");
			}
			return false;
		}
	}

	public static ModBase.SafeList<LoaderBase> loaderTaskbar = new ModBase.SafeList<LoaderBase>();

	public static double loaderTaskbarProgress;

	private static TaskbarItemProgressState loaderTaskbarProgressLast = TaskbarItemProgressState.None;

	private static readonly Dictionary<LoaderBase, LoaderFolderDictionaryEntry> loaderFolderDictionary = new Dictionary<LoaderBase, LoaderFolderDictionaryEntry>();

	public static void LoaderTaskbarAdd<T>(LoaderCombo<T> loader)
	{
		if (ModMain.frmSpeedLeft != null)
		{
			ModMain.frmSpeedLeft.TaskRemove(loader);
		}
		loaderTaskbar.Add(loader);
		ModBase.Log("[Taskbar] " + loader.name + " 已加入任务列表");
	}

	public static void LoaderTaskbarProgressRefresh()
	{
		try
		{
			double num = LoaderTaskbarProgressGet();
			foreach (LoaderBase item in loaderTaskbar)
			{
				if (loaderTaskbar.All((LoaderBase l) => l.State != ModBase.LoadState.Loading) || item.State == ModBase.LoadState.Waiting || item.State == ModBase.LoadState.Aborted)
				{
					ModMain.frmSpeedLeft?.TaskRefresh(item);
					loaderTaskbar.Remove(item);
					ModBase.Log("[Taskbar] " + item.name + " 已移出任务列表");
				}
			}
			if (num <= 0.0 || num >= 1.0 || loaderTaskbarProgress > num)
			{
				loaderTaskbarProgress = num;
			}
			else
			{
				loaderTaskbarProgress = loaderTaskbarProgress * 0.9 + num * 0.1;
			}
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.BtnExtraDownload.Progress = loaderTaskbarProgress;
			});
			TaskbarItemProgressState taskbarItemProgressState;
			if (!loaderTaskbar.Any() || loaderTaskbarProgress == 1.0)
			{
				taskbarItemProgressState = TaskbarItemProgressState.None;
			}
			else if (loaderTaskbarProgress < 0.015)
			{
				taskbarItemProgressState = TaskbarItemProgressState.Indeterminate;
			}
			else
			{
				taskbarItemProgressState = TaskbarItemProgressState.Normal;
				ModMain.frmMain.TaskbarItemInfo.ProgressValue = loaderTaskbarProgress;
			}
			if (loaderTaskbarProgressLast != taskbarItemProgressState)
			{
				loaderTaskbarProgressLast = taskbarItemProgressState;
				ModMain.frmMain.TaskbarItemInfo.ProgressState = taskbarItemProgressState;
				ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新任务栏进度显示失败", ModBase.LogLevel.Feedback, null, Lang.Text("Application.Loader.Error.OperationFailed"));
		}
	}

	public static double LoaderTaskbarProgressGet()
	{
		try
		{
			if (!loaderTaskbar.Any())
			{
				return 1.0;
			}
			return ModBase.MathClamp(loaderTaskbar.Select((LoaderBase l) => l.Progress).Average(), 0.0, 1.0);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取任务栏进度出错", ModBase.LogLevel.Feedback, null, Lang.Text("Application.Loader.Error.OperationFailed"));
			return 0.5;
		}
	}

	public static bool LoaderFolderRun(LoaderBase loader, string folderPath, LoaderFolderRunType type, int maxDepth = 0, string extraPath = "", bool waitForExit = false, object loaderInput = null)
	{
		LoaderFolderDictionaryEntry value = new LoaderFolderDictionaryEntry
		{
			folderPath = folderPath + extraPath,
			lastCheckTime = null
		};
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(folderPath + extraPath);
			value.lastCheckTime = (directoryInfo.Exists ? new DateTime?(GetActualLastWriteTimeUtc(directoryInfo, maxDepth)) : ((DateTime?)null));
			if (type == LoaderFolderRunType.RunOnUpdated && loaderFolderDictionary.ContainsKey(loader))
			{
				if (directoryInfo.Exists)
				{
					DateTime? lastCheckTime = loaderFolderDictionary[loader].lastCheckTime;
					if (lastCheckTime.HasValue && value.Equals(loaderFolderDictionary[loader]))
					{
						return false;
					}
				}
				else
				{
					DateTime? lastCheckTime = loaderFolderDictionary[loader].lastCheckTime;
					if (!lastCheckTime.HasValue)
					{
						return false;
					}
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "文件夹加载器启动检测出错");
		}
		loaderFolderDictionary[loader] = value;
		if (type == LoaderFolderRunType.UpdateOnly)
		{
			return false;
		}
		if (waitForExit)
		{
			loader.WaitForExit(loaderInput ?? folderPath, null, isForceRestart: true);
		}
		else
		{
			loader.Start(loaderInput ?? folderPath, isForceRestart: true);
		}
		return true;
	}

	private static DateTime GetActualLastWriteTimeUtc(DirectoryInfo folderInfo, int maxDepth)
	{
		DateTime dateTime = folderInfo.LastWriteTimeUtc;
		if (maxDepth > 0)
		{
			foreach (DirectoryInfo item in folderInfo.EnumerateDirectories())
			{
				DateTime actualLastWriteTimeUtc = GetActualLastWriteTimeUtc(item, maxDepth - 1);
				if (actualLastWriteTimeUtc > dateTime)
				{
					dateTime = actualLastWriteTimeUtc;
				}
			}
		}
		return dateTime;
	}
}
