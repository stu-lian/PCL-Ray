using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PCL.Core.Logging;

namespace PCL.Core.App.IoC;

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MaxValue)]
public sealed class Lifecycle : ILifecycleService
{
	private class SystemLifecycleService : ILifecycleService
	{
		public string Name => "系统";

		public string Identifier => "system";

		public bool SupportAsync => false;

		public Task StartAsync()
		{
			return Task.CompletedTask;
		}

		public Task StopAsync()
		{
			return Task.CompletedTask;
		}
	}

	private static LifecycleContext? _context;

	private static readonly ILifecycleService _SystemService = new SystemLifecycleService();

	private static readonly LifecycleServiceInfo _SystemServiceInfo = new LifecycleServiceInfo(_SystemService, LifecycleState.BeforeLoading);

	internal static readonly LifecycleContext SystemContext = GetContext(_SystemService);

	private static DateTime? _countRunningStart;

	private static bool _isApplicationStarted = false;

	private static bool _isLoadingStarted = false;

	private static bool _isWindowCreated = false;

	private static bool _hasRequestedStopLoading = false;

	private static bool _hasRequestedRestart = false;

	private static string? _requestRestartArguments;

	private static ILifecycleService? _requestRestartService;

	private static readonly object _ExitLock = new object();

	private static ILifecycleLogService? _logService;

	private static readonly List<LifecycleLogItem> _PendingLogs = new List<LifecycleLogItem>();

	private static readonly ConcurrentDictionary<string, LifecycleServiceInfo> _RunningServiceInfoMap = new ConcurrentDictionary<string, LifecycleServiceInfo>();

	private static readonly ConcurrentStack<ILifecycleService> _StartedServiceStack = new ConcurrentStack<ILifecycleService>();

	private static readonly Dictionary<string, ILifecycleService> _ManualServiceMap = new Dictionary<string, ILifecycleService>();

	private static readonly HashSet<ILifecycleService> _DeclaredStoppedServices = new HashSet<ILifecycleService>();

	private static readonly ConcurrentDictionary<string, Exception> _ServiceLastExceptionMap = new ConcurrentDictionary<string, Exception>();

	private static readonly ConcurrentBag<Task> _StoppingServiceTasks = new ConcurrentBag<Task>();

	public string Identifier => "lifecycle";

	public string Name => "生命周期";

	public bool SupportAsync => false;

	private static LifecycleContext Context => _context ?? SystemContext;

	public static Application CurrentApplication { get; set; } = null;

	public static bool HasShutdownStarted { get; private set; } = false;

	public static bool IsForceShutdown { get; private set; } = false;

	public static string PendingLogDirectory { get; set; } = "PCL\\Log";

	public static string PendingLogFileName { get; set; } = "LastPending.log";

	public static bool IsLogServiceStarted => _logService != null;

	public static ICollection<string> RunningServices => _RunningServiceInfoMap.Keys;

	public static LifecycleState CurrentState
	{
		get;
		private set
		{
			Context.Debug($"状态改变: {value}");
			field = value;
			try
			{
				StateChanged?.Invoke(value);
			}
			catch (Exception ex)
			{
				Context.Warn("状态更改事件出错", ex);
			}
		}
	} = LifecycleState.BeforeLoading;

	public static event Action<string>? ServiceStarting;

	public static event Action<string>? ServiceStarted;

	public static event Action<string>? ServiceStopping;

	public static event Action<string>? ServiceStopped;

	public static event Action<string>? ServiceDeclaredStopped;

	public static event Action<string, Exception>? ServiceUnhandledException;

	public static event Action<LifecycleState>? StateChanged;

	private Lifecycle()
	{
		_context = GetContext(this);
	}

	public Task StartAsync()
	{
		return Task.CompletedTask;
	}

	public Task StopAsync()
	{
		_context = null;
		return Task.CompletedTask;
	}

	public static LifecycleContext GetContext(ILifecycleService self)
	{
		return new LifecycleContext(self, (LifecycleLogItem item) =>
		{
			lock (_PendingLogs)
			{
				if (_logService == null)
				{
					_PendingLogs.Add(item);
				}
				else
				{
					_PushLog(item, _logService);
				}
			}
			if (item.ActionLevel == ActionLevel.MsgBoxFatal)
			{
				_FatalExit();
			}
		}, (int statusCode) =>
		{
			if (CurrentState != LifecycleState.BeforeLoading)
			{
				throw new InvalidOperationException("只能在 BeforeLoading 时请求退出");
			}
			Context.Info(_ServiceName(self) + " 已请求退出程序");
			_Exit(statusCode);
		}, (string? args) =>
		{
			_hasRequestedRestart = true;
			_requestRestartService = self;
			_requestRestartArguments = args;
		}, () =>
		{
			string identifier = self.Identifier;
			if (GetServiceInfo(identifier)?.Identifier == identifier)
			{
				throw new InvalidOperationException("只能在服务启动阶段调用");
			}
			_DeclaredStoppedServices.Add(self);
		}, () =>
		{
			if (CurrentState != LifecycleState.BeforeLoading)
			{
				throw new InvalidOperationException("只能在 BeforeLoading 时请求停止加载");
			}
			Context.Info(_ServiceName(self) + " 已请求停止继续加载");
			_hasRequestedStopLoading = true;
		});
	}

	public static void OnException(object ex)
	{
		Context.Fatal("未捕获的异常", ex as Exception);
	}

	public static void OnInitialize()
	{
		if (_isApplicationStarted)
		{
			return;
		}
		_isApplicationStarted = true;
		Thread.CurrentThread.Name = "STA";
		AppDomain.CurrentDomain.UnhandledException += (object _, UnhandledExceptionEventArgs e) =>
		{
			OnException(e.ExceptionObject);
		};
		AppDomain.CurrentDomain.ProcessExit += (object? _, EventArgs _) =>
		{
			_Exit();
		};
		TaskScheduler.UnobservedTaskException += (object? _, UnobservedTaskExceptionEventArgs e) =>
		{
			Context.Error("未观测到的异步任务异常", e.Exception);
			e.SetObserved();
		};
		_RunningServiceInfoMap["system"] = _SystemServiceInfo;
		Type[] array = _GetServiceTypes(LifecycleState.Manual);
		for (int num = 0; num < array.Length; num++)
		{
			ILifecycleService lifecycleService = _CreateService(array[num]);
			string identifier = lifecycleService.Identifier;
			if (!_ManualServiceMap.TryAdd(identifier, lifecycleService))
			{
				Context.Warn(_ServiceName(lifecycleService, LifecycleState.Manual) + " 标识符重复，已跳过");
			}
		}
		_StartStateFlow(LifecycleState.BeforeLoading);
		if (!_hasRequestedStopLoading)
		{
			int statusCode = CurrentApplication.Run();
			if (!HasShutdownStarted)
			{
				_Exit(statusCode);
			}
		}
	}

	public static void OnLoading()
	{
		if (!_isLoadingStarted)
		{
			_isLoadingStarted = true;
			_StartStateFlow(LifecycleState.Loading, LifecycleState.WindowCreating);
			CurrentApplication.MainWindow.Show();
		}
	}

	public static void OnWindowCreated()
	{
		if (!_isWindowCreated)
		{
			_isWindowCreated = true;
			_StartStateFlow(LifecycleState.WindowCreated);
			_countRunningStart = DateTime.Now;
			_StartWorker(LifecycleState.Running, LifecycleState.Exiting, count: false);
		}
	}

	private static void _FatalExit()
	{
		ForceShutdown(-1);
	}

	private static void _Exit(int statusCode = 0)
	{
		lock (_ExitLock)
		{
			if (HasShutdownStarted)
			{
				return;
			}
			HasShutdownStarted = true;
		}
		DateTime? countRunningStart = _countRunningStart;
		if (countRunningStart.HasValue)
		{
			DateTime valueOrDefault = countRunningStart.GetValueOrDefault();
			_LogStateCount(DateTime.Now - valueOrDefault, LifecycleState.Running);
		}
		_StartStateFlow(LifecycleState.Exiting, null, count: false);
		Context.Debug("正在停止运行中的服务");
		ILifecycleLogService lifecycleLogService = null;
		ILifecycleService result;
		while (_StartedServiceStack.TryPop(out result))
		{
			if (!_RunningServiceInfoMap.TryGetValue(result.Identifier, out LifecycleServiceInfo value) || !value.IsStopped)
			{
				if (result is ILifecycleLogService lifecycleLogService2)
				{
					Context.Trace("已跳过日志服务: " + _ServiceName(lifecycleLogService2));
					lifecycleLogService = lifecycleLogService2;
				}
				else
				{
					_StopService(result, result.SupportAsync);
				}
			}
		}
		_WaitStoppingServiceTasks();
		if (lifecycleLogService != null)
		{
			Context.Trace("退出过程已结束，正在停止日志服务");
			lifecycleLogService.StopAsync().ConfigureAwait(continueOnCapturedContext: false).GetAwaiter()
				.GetResult();
			Console.WriteLine("[Lifecycle] Log service stopped");
		}
		_SavePendingLogs();
		if (_hasRequestedRestart)
		{
			ILifecycleService requestRestartService = _requestRestartService;
			if (requestRestartService != null)
			{
				Console.WriteLine("[Lifecycle] Requested by '" + requestRestartService.Identifier + "', restarting the program...");
				_RunCurrentExecutable(_requestRestartArguments);
			}
		}
		Console.WriteLine($"[Lifecycle] Exiting program with status: {statusCode}");
		if (statusCode == -1)
		{
			Basics.CurrentProcess.Kill();
		}
		else
		{
			Environment.Exit(statusCode);
		}
		Console.WriteLine("[Lifecycle] Warning! Abnormal behaviour, try to kill process 1s later.");
		Thread.Sleep(1000);
		_KillCurrentProcess();
	}

	public static void Shutdown(int statusCode = 0, bool force = false)
	{
		if (CurrentState == LifecycleState.BeforeLoading)
		{
			throw new InvalidOperationException();
		}
		if (HasShutdownStarted)
		{
			return;
		}
		Context.Info(force ? "开始强制关闭程序" : "正在关闭程序");
		IsForceShutdown = force;
		if (force)
		{
			_Exit(statusCode);
			return;
		}
		Thread thread = new Thread(() =>
		{
			_Exit(statusCode);
		});
		thread.Name = "Lifecycle/Shutdown";
		thread.Start();
	}

	public static void ForceShutdown(int statusCode = 0)
	{
		Shutdown(statusCode, force: true);
	}

	private static void _PushLog(LifecycleLogItem item, ILifecycleLogService service)
	{
		service.OnLog(item);
	}

	private static void _SavePendingLogs()
	{
		if (_PendingLogs.Count == 0)
		{
			Console.WriteLine("[Lifecycle] No pending logs");
			return;
		}
		try
		{
			string text = Path.Combine(PendingLogDirectory, PendingLogFileName);
			if (!Path.IsPathRooted(text))
			{
				text = Path.Combine(Basics.ExecutableDirectory, text);
			}
			Directory.CreateDirectory(Basics.GetParentPathOrDefault(text));
			using FileStream stream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.Read);
			using StreamWriter streamWriter = new StreamWriter(stream, Encoding.UTF8);
			foreach (LifecycleLogItem pendingLog in _PendingLogs)
			{
				streamWriter.WriteLine(pendingLog.ComposeMessage());
			}
			Console.WriteLine("[Lifecycle] Pending logs saved to " + text);
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			Console.WriteLine("[Lifecycle] Error saving pending logs, writing to stdout...");
			foreach (LifecycleLogItem pendingLog2 in _PendingLogs)
			{
				Console.WriteLine(pendingLog2.ComposeMessage());
			}
		}
	}

	private static void _RunCurrentExecutable(string? arguments)
	{
		string processPath = Environment.ProcessPath;
		if (arguments == null)
		{
			Process.Start(processPath);
		}
		else
		{
			Process.Start(processPath, arguments);
		}
	}

	private static void _KillCurrentProcess()
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = "taskkill.exe",
			Arguments = $"/f /t /pid {Environment.ProcessId}",
			UseShellExecute = false,
			CreateNoWindow = true
		});
	}

	private static string _ServiceName(ILifecycleService service, LifecycleState? state = null)
	{
		return service.Name;
	}

	private static Task _StartServiceTask(ILifecycleService service, bool manual = false)
	{
		ILifecycleLogService logService = null;
		if (service is ILifecycleLogService lifecycleLogService)
		{
			if (_logService != null)
			{
				throw new InvalidOperationException("日志服务只能有一个");
			}
			logService = lifecycleLogService;
		}
		LifecycleState state = ((!manual) ? CurrentState : LifecycleState.Manual);
		string name = _ServiceName(service, state);
		lock (_ManualServiceMap)
		{
			if (_ManualServiceMap.ContainsKey(service.Identifier) && IsServiceRunning(service.Identifier))
			{
				Context.Warn(name + " 标识符重复，已跳过");
				return Task.CompletedTask;
			}
			_RunningServiceInfoMap[service.Identifier] = _SystemServiceInfo;
		}
		if (!service.SupportAsync)
		{
			return AsyncCall();
		}
		return Task.Run((Func<Task?>)AsyncCall);
		async Task AsyncCall()
		{
			try
			{
				Context.Trace("正在启动 " + name);
				ServiceStarting?.Invoke(service.Identifier);
				await service.StartAsync().ConfigureAwait(!service.SupportAsync);
				LifecycleServiceInfo value = new LifecycleServiceInfo(service, state);
				Context.Debug(name + " 启动成功");
				if (_DeclaredStoppedServices.Contains(service))
				{
					_DeclaredStoppedServices.Remove(service);
					ServiceDeclaredStopped?.Invoke(service.Identifier);
					Context.Trace(name + " 已中止");
				}
				else
				{
					_StartedServiceStack.Push(service);
					_RunningServiceInfoMap[service.Identifier] = value;
					ServiceStarted?.Invoke(service.Identifier);
				}
			}
			catch (Exception ex)
			{
				Context.Warn(name + " 启动失败，尝试停止", ex);
				_UpdateLastException(service, ex);
				_StopService(service, async: false);
			}
			if (logService == null)
			{
				return;
			}
			lock (_PendingLogs)
			{
				foreach (LifecycleLogItem pendingLog in _PendingLogs)
				{
					_PushLog(pendingLog, logService);
				}
				_PendingLogs.Clear();
				_logService = logService;
			}
		}
	}

	private static Type[] _GetServiceTypes(LifecycleState state)
	{
		return LifecycleServiceTypes.GetServiceTypes(state);
	}

	private static ILifecycleService _CreateService(Type type)
	{
		string fullName = type.FullName;
		try
		{
			SystemContext.Trace("正在实例化 " + fullName);
			ILifecycleService lifecycleService = (ILifecycleService)Activator.CreateInstance(type, nonPublic: true);
			string value = (lifecycleService.SupportAsync ? "异步" : "同步");
			SystemContext.Trace($"实例化完成: {lifecycleService.Name} ({lifecycleService.Identifier}), 启动方式: {value}");
			return lifecycleService;
		}
		catch (Exception ex)
		{
			SystemContext.Fatal("注册服务项实例化失败: " + fullName, ex);
			throw;
		}
	}

	private static void _LogStateCount(TimeSpan count, LifecycleState state)
	{
		Context.Debug($"状态 {state} 共用时 {Math.Round(count.TotalMilliseconds)} ms");
	}

	private static void _InitializeAndStartStateServices(LifecycleState state)
	{
		Type[] array = _GetServiceTypes(state);
		if (array.Length == 0)
		{
			return;
		}
		List<ILifecycleService> list = new List<ILifecycleService>();
		Type[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			ILifecycleService lifecycleService = _CreateService(array2[i]);
			if (lifecycleService.SupportAsync)
			{
				list.Add(lifecycleService);
			}
			else
			{
				_StartServiceTask(lifecycleService).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
			}
			if (_hasRequestedStopLoading)
			{
				return;
			}
		}
		Task.WaitAll(list.Select((ILifecycleService instance) => _StartServiceTask(instance)).ToArray());
	}

	private static void _StartStateFlow(LifecycleState start, LifecycleState? end = null, bool count = true)
	{
		int i = (int)start;
		for (int num = ((!end.HasValue) ? i : ((int)end.Value)); i <= num; i++)
		{
			DateTime? dateTime = (count ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			LifecycleState lifecycleState = (LifecycleState)i;
			_NextState(lifecycleState);
			_InitializeAndStartStateServices(lifecycleState);
			if (dateTime.HasValue)
			{
				DateTime valueOrDefault = dateTime.GetValueOrDefault();
				_LogStateCount(DateTime.Now - valueOrDefault, lifecycleState);
			}
		}
	}

	private static void _StartWorker(LifecycleState state, LifecycleState? wait = null, bool count = true)
	{
		Thread thread = new Thread(() =>
		{
			LifecycleState start = state;
			bool count2 = count;
			_StartStateFlow(start, null, count2);
			if (wait.HasValue)
			{
				LifecycleState valueOrDefault = wait.GetValueOrDefault();
				WaitForState(valueOrDefault);
			}
		});
		thread.IsBackground = true;
		thread.Name = $"Lifecycle/{state}";
		thread.Start();
	}

	private static void _RemoveRunningInstance(ILifecycleService service)
	{
		_RunningServiceInfoMap.TryRemove(service.Identifier, out LifecycleServiceInfo value);
		value?.MarkAsStopped();
	}

	private static void _WaitStoppingServiceTasks()
	{
		Task.WaitAll(_StoppingServiceTasks.ToArray());
	}

	private static void _StopService(ILifecycleService service, bool async, bool manual = false)
	{
		string name = _ServiceName(service, (!manual) ? CurrentState : LifecycleState.Manual);
		if (async)
		{
			_StoppingServiceTasks.Add(Task.Run((Func<Task?>)Stop));
		}
		else
		{
			Stop().ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
		}
		async Task Stop()
		{
			try
			{
				Context.Trace("正在停止 " + name);
				ServiceStopping?.Invoke(service.Identifier);
				await service.StopAsync().ConfigureAwait(!async);
				ServiceStopped?.Invoke(service.Identifier);
				Context.Debug(name + " 已停止");
			}
			catch (Exception ex)
			{
				Context.Warn("停止 " + name + " 时出错，已跳过", ex);
				_UpdateLastException(service, ex);
			}
			_RemoveRunningInstance(service);
		}
	}

	private static void _UpdateLastException(ILifecycleService service, Exception ex)
	{
		_ServiceLastExceptionMap[service.Identifier] = ex;
		ServiceUnhandledException?.Invoke(service.Identifier, ex);
	}

	public static bool IsServiceRunning(string identifier)
	{
		return _RunningServiceInfoMap.ContainsKey(identifier);
	}

	public static LifecycleServiceInfo? GetServiceInfo(string? identifier)
	{
		if (identifier == null)
		{
			return null;
		}
		_RunningServiceInfoMap.TryGetValue(identifier, out LifecycleServiceInfo value);
		return value;
	}

	public static Exception? GetServiceLastException(string identifier)
	{
		if (!_ServiceLastExceptionMap.TryGetValue(identifier, out Exception value))
		{
			return null;
		}
		return value;
	}

	public static bool StartService(string identifier, bool? async = null)
	{
		_ManualServiceMap.TryGetValue(identifier, out ILifecycleService service);
		if (service == null || IsServiceRunning(identifier))
		{
			return false;
		}
		bool valueOrDefault = async == true;
		if (!async.HasValue)
		{
			valueOrDefault = service.SupportAsync;
			async = valueOrDefault;
		}
		if (async == true)
		{
			Task.Run(() => _StartServiceTask(service, manual: true));
		}
		else
		{
			_StartServiceTask(service, manual: true);
		}
		return true;
	}

	public static bool StopService(string identifier, bool async = true)
	{
		_ManualServiceMap.TryGetValue(identifier, out ILifecycleService value);
		if (value == null || !IsServiceRunning(identifier))
		{
			return false;
		}
		_StopService(value, async, manual: true);
		return true;
	}

	public static bool StartCustomService(ILifecycleService service)
	{
		if (IsServiceRunning(service.Identifier) || _ManualServiceMap.ContainsKey(service.Identifier))
		{
			return false;
		}
		_StartServiceTask(service);
		return true;
	}

	private static void _NextState(LifecycleState? enforce = null)
	{
		if (enforce.HasValue)
		{
			LifecycleState valueOrDefault = enforce.GetValueOrDefault();
			CurrentState = valueOrDefault;
		}
		else
		{
			CurrentState++;
		}
	}

	public static bool WaitForState(LifecycleState state)
	{
		if (CurrentState >= state)
		{
			return false;
		}
		ManualResetEventSlim mre = new ManualResetEventSlim(initialState: false);
		try
		{
			StateChanged += TempHandler;
			try
			{
				mre.Wait();
			}
			finally
			{
				StateChanged -= TempHandler;
			}
			return true;
		}
		finally
		{
			if (mre != null)
			{
				((IDisposable)mre).Dispose();
			}
		}
		void TempHandler(LifecycleState s)
		{
			if (s == state)
			{
				mre.Set();
			}
		}
	}

	public static Task<bool> WaitForStateAsync(LifecycleState state)
	{
		if (CurrentState >= state)
		{
			return Task.FromResult(result: false);
		}
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		StateChanged += TempHandler;
		return tcs.Task;
		void TempHandler(LifecycleState s)
		{
			if (s == state)
			{
				StateChanged -= TempHandler;
				tcs.TrySetResult(result: true);
			}
		}
	}

	public static void When(LifecycleState when, Action action)
	{
		if (CurrentState < when)
		{
			StateChanged += TempHandler;
		}
		void TempHandler(LifecycleState state)
		{
			if (state == when)
			{
				action();
				StateChanged -= TempHandler;
			}
		}
	}
}
