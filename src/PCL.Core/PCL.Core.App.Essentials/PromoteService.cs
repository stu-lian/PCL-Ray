using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.IoC;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.BeforeLoading, Priority = -10)]
[LifecycleScope("promote", "提权服务", false)]
public sealed class PromoteService : ILifecycleService
{
	private readonly record struct PromoteOperation(string Command, Action<string?>? Callback, bool DetailLog);

	private static Process? _promoteProcess;

	private static NamedPipeServerStream? _promotePipeServer;

	private static readonly ConcurrentQueue<PromoteOperation> _PendingOperations = new ConcurrentQueue<PromoteOperation>();

	private static readonly Dictionary<string, PromoteOperationFunction> _OperationFunctions = new Dictionary<string, PromoteOperationFunction>();

	private const string OperationErrNotFound = "ERR_OPERATION_NOT_FOUND";

	private const string OperationErrInvalidArgument = "ERR_ILLEGAL_ARGUMENT";

	private const string OperationErrExceptionThrown = "ERR_UNHANDLED_EXCEPTION";

	private const string OperationErrEmpty = "ERR_EMPTY";

	private static readonly AutoResetEvent _ActivateEvent = new AutoResetEvent(initialState: false);

	private static readonly Dictionary<string, Process> _RunningProcesses = new Dictionary<string, Process>();

	public static bool IsPromoteProcessRunning => _promoteProcess != null;

	public static bool IsCurrentProcessPromoted { get; private set; }

	public static Func<string, string?> Operate
	{
		private get
		{
			return (string command) =>
			{
				string[] array = command.Split(new char[1] { ' ' }, 2);
				_OperationFunctions.TryGetValue(array[0], out PromoteOperationFunction value);
				if (value == null)
				{
					return "ERR_OPERATION_NOT_FOUND";
				}
				try
				{
					return value((array.Length > 1) ? array[1] : null) ?? "ERR_EMPTY";
				}
				catch (Exception ex)
				{
					Context.Warn("操作出错", ex);
					return "ERR_UNHANDLED_EXCEPTION";
				}
			};
		}
		set;
	} = null;

	public string Identifier => "promote";

	public string Name => "提权服务";

	public bool SupportAsync => false;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	} = null;

	private static ILifecycleService Service => Context.ServiceInstance;

	private static string _GetPromotePipeName(int processId)
	{
		return $"PCLCE_PM@{processId}";
	}

	public static bool AddOperationFunction(string name, PromoteOperationFunction operation)
	{
		if (IsCurrentProcessPromoted)
		{
			return _OperationFunctions.TryAdd(name, operation);
		}
		return false;
	}

	public static bool AddJsonOperationFunction<TValue>(string name, Func<TValue?, string?> operation)
	{
		return AddOperationFunction(name, (string? arg) =>
		{
			if (arg == null)
			{
				return "ERR_EMPTY";
			}
			TValue arg2 = JsonSerializer.Deserialize<TValue>(arg, JsonCompat.SerializerOptions);
			return operation(arg2);
		});
	}

	private static string _ShortenString(string str)
	{
		if (str.Length <= 15)
		{
			return str;
		}
		return str.Substring(0, 15) + "...";
	}

	private static void _PerformAsPromoteProcess(string pid)
	{
		Context.Info("正在连接提权通信管道");
		Process processById = Process.GetProcessById(int.Parse(pid));
		if (!string.Equals(Path.GetFullPath(processById.MainModule.FileName), Basics.ExecutablePath, StringComparison.OrdinalIgnoreCase))
		{
			Context.Error("来源验证失败，正在退出");
			return;
		}
		string pipeName = _GetPromotePipeName(processById.Id);
		NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
		namedPipeClientStream.Connect(10000);
		Context.Info("已连接，开始通信");
		StreamReader streamReader = new StreamReader(namedPipeClientStream);
		StreamWriter streamWriter = new StreamWriter(namedPipeClientStream);
		while (true)
		{
			string text = streamReader.ReadLine();
			if (string.IsNullOrEmpty(text))
			{
				break;
			}
			Context.Debug("正在执行: " + _ShortenString(text));
			string text2 = Operate(text) ?? "ERR_EMPTY";
			Context.Trace("返回结果: " + _ShortenString(text2));
			streamWriter.WriteLine(text2.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' '));
			streamWriter.Flush();
			Context.Trace("返回成功");
		}
		Context.Info("管道已关闭，正在退出");
	}

	private static bool _PromotePipeCallback(StreamReader reader, StreamWriter writer, Process? client)
	{
		while (IsPromoteProcessRunning)
		{
			if (!_PendingOperations.TryDequeue(out var result))
			{
				_ActivateEvent.WaitOne();
				continue;
			}
			string text = result.Command.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
			string text2 = (result.DetailLog ? text : _ShortenString(text));
			Context.Debug("正在执行: " + text2);
			writer.WriteLine(text);
			writer.Flush();
			string text3 = reader.ReadLine();
			if (text3 == null)
			{
				Context.Warn("管道输入流已结束");
				break;
			}
			string text4 = (result.DetailLog ? text3 : _ShortenString(text3));
			Context.Trace("执行结果: " + text4);
			if (text3 == "ERR_EMPTY")
			{
				text3 = null;
			}
			result.Callback?.Invoke(text3);
		}
		return false;
	}

	private static bool _StartPromoteProcess()
	{
		_promoteProcess = ProcessInterop.Start(Basics.ExecutablePath, $"promote {Basics.CurrentProcessId}", runAsAdmin: true);
		if (_promoteProcess == null)
		{
			Context.Warn("提权进程启动失败");
			return false;
		}
		_promoteProcess.Exited += (object? _, EventArgs _) =>
		{
			_promoteProcess = null;
		};
		if (_promotePipeServer == null)
		{
			_promotePipeServer = PipeComm.StartPipeServer("Promote", _GetPromotePipeName(Basics.CurrentProcessId), _PromotePipeCallback, () =>
			{
				_promotePipeServer = null;
			}, stopWhenException: true, new int[1] { _promoteProcess.Id });
		}
		return true;
	}

	public static void Append(string command, Action<string?>? callback = null, bool detailLog = true)
	{
		_PendingOperations.Enqueue(new PromoteOperation(command, callback, detailLog));
	}

	public static bool Activate()
	{
		if (!IsPromoteProcessRunning && !_StartPromoteProcess())
		{
			_PendingOperations.Clear();
			return false;
		}
		_ActivateEvent.Set();
		return true;
	}

	[PromoteOperation("kill")]
	public static string? KillProcess(string? arg)
	{
		if (arg == null)
		{
			return "ERR_ILLEGAL_ARGUMENT";
		}
		string[] array = arg.Split(' ');
		if (!_RunningProcesses.TryGetValue(array[0], out Process value))
		{
			return null;
		}
		value.Kill();
		if (array.Length > 1)
		{
			int.TryParse(array[1], out var result);
			return value.WaitForExit(result).ToString();
		}
		value.WaitForExit();
		return true.ToString();
	}

	[PromoteOperation("start")]
	public static string? StartProcess(string? arg)
	{
		if (arg == null)
		{
			return "ERR_ILLEGAL_ARGUMENT";
		}
		string[] array = arg.Split(new string[1] { " ; " }, 2, StringSplitOptions.RemoveEmptyEntries);
		bool flag = false;
		if (array[0].EndsWith('.'))
		{
			string text = array[0];
			array[0] = text.Substring(0, text.Length - 1);
			flag = true;
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(array[0]);
		if (flag)
		{
			processStartInfo.CreateNoWindow = true;
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardInput = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
		}
		if (array.Length > 1)
		{
			processStartInfo.Arguments = array[1];
		}
		return _StartProcessWithInfo(processStartInfo);
	}

	private static string? _StartProcessWithInfo(ProcessStartInfo? info)
	{
		if (info == null)
		{
			return "ERR_ILLEGAL_ARGUMENT";
		}
		Process process = Process.Start(info);
		if (process == null)
		{
			return null;
		}
		string id = process.Id.ToString();
		process.Exited += (object? _, EventArgs _) =>
		{
			_RunningProcesses.Remove(id);
		};
		_RunningProcesses[id] = process;
		return id;
	}

	[DependencyInjectionPoint("promote", false)]
	private static void _CollectOperationFunction(PromoteOperationFunction operation, string name)
	{
		AddOperationFunction(name, operation);
	}

	[LifecycleStart]
	private static void _Start()
	{
		string[] args = Basics.CommandLineArguments;
		if (args != null && args.Length == 2 && args[0] == "promote")
		{
			Context.Info("当前进程为提权进程");
			IsCurrentProcessPromoted = true;
			Context.Info("正在加载提权操作");
			_CollectOperationFunction_InvokeInjection_Promote();
			AddJsonOperationFunction<ProcessStartInfo>("start-json", _StartProcessWithInfo);
			Lifecycle.PendingLogFileName = "LastPending_Promote.log";
			LogWrapper.OnLog += (LogLevel level, string msg, string? module, Exception? ex) =>
			{
				Context.CustomLog("[" + module + "] " + msg, ex, level);
			};
			Context.Info("已接管通用日志");
			Context.Info("正在启动服务线程");
			Thread thread = new Thread(() =>
			{
				_PerformAsPromoteProcess(args[1]);
			});
			thread.Name = "Promote";
			thread.Start();
			Context.RequestStopLoading();
			Context.DeclareStopped();
		}
		else
		{
			Context.Info("当前进程为主进程");
			IsCurrentProcessPromoted = false;
		}
	}

	[LifecycleStop]
	private static void _Stop()
	{
		if (_promotePipeServer != null)
		{
			Context.Debug("正在结束提权管道服务");
			_promotePipeServer.Dispose();
		}
		if (_promoteProcess != null && !_promoteProcess.WaitForExit(3000))
		{
			Context.Debug("正在结束提权进程");
			ProcessInterop.Kill(_promoteProcess, 0, force: true);
		}
	}

	[GeneratedCode("PCL.Core.SourceGenerators.DependencyCollectorGenerator", "1.0.0.0")]
	[ExcludeFromCodeCoverage]
	private static void _CollectOperationFunction_InvokeInjection_Promote()
	{
		_CollectOperationFunction(KillProcess, "kill");
		_CollectOperationFunction(StartProcess, "start");
	}

	public PromoteService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		_Stop();
	}

	public async Task StartAsync()
	{
		_Start();
	}
}
