using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PCL.Core.App.IoC;
using PCL.Core.IO;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.Loaded)]
[LifecycleScope("rpc", "远程执行服务", true)]
public sealed class RpcService : ILifecycleService
{
	private NamedPipeServerStream? _pipe;

	public const string PipePrefix = "PCLCE_RPC";

	private static readonly string _EchoPipeName = $"{"PCLCE_RPC"}@{Basics.CurrentProcess.Id}";

	private static readonly string[] _RequestTypeArray = new string[3] { "GET", "SET", "REQ" };

	private static readonly HashSet<string> _RequestType;

	private static readonly Dictionary<string, RpcProperty> _PropertyMap;

	private static readonly Dictionary<string, RpcFunction> _FunctionMap;

	public string Identifier => "rpc";

	public string Name => "远程执行服务";

	public bool SupportAsync => true;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	}

	private static ILifecycleService Service => Context.ServiceInstance;

	[LifecycleStart]
	private void _Start()
	{
		_pipe = PipeComm.StartPipeServer("Echo", _EchoPipeName, _EchoPipeCallback);
	}

	[LifecycleStop]
	private async Task _StopAsync()
	{
		if (_pipe != null)
		{
			await _pipe.DisposeAsync();
		}
	}

	[LifecycleDependencyInjection("rpc-function", AttributeTargets.Method)]
	private static void _CollectRpcFunctionRegistry(ImmutableList<(RpcFunction func, string name)> items)
	{
		foreach (var item in items)
		{
			var (func, _) = item;
			AddFunction(item.name, func);
		}
	}

	[LifecycleDependencyInjection("rpc-property", AttributeTargets.Property)]
	private static void _CollectRpcPropertyRegistry(ImmutableList<(PropertyAccessor<string> prop, string name)> items)
	{
		foreach (var item in items)
		{
			PropertyAccessor<string> prop;
			(prop, _) = item;
			AddProperty(new RpcProperty(item.name, () => prop.Value, (string? value) =>
			{
				prop.Value = value ?? "";
			}, prop.CanSet));
		}
	}

	public static bool AddProperty(RpcProperty prop)
	{
		return _PropertyMap.TryAdd(prop.Name, prop);
	}

	public static bool RemoveProperty(string name)
	{
		return _PropertyMap.Remove(name);
	}

	public static bool RemoveProperty(RpcProperty prop)
	{
		string name = prop.Name;
		if (!_PropertyMap.TryGetValue(name, out RpcProperty value) || value != prop)
		{
			return false;
		}
		_PropertyMap.Remove(name);
		return true;
	}

	public static bool AddFunction(string name, RpcFunction func)
	{
		return _FunctionMap.TryAdd(name, func);
	}

	public static bool RemoveFunction(string name)
	{
		return _FunctionMap.Remove(name);
	}

	private static bool _EchoPipeCallback(StreamReader reader, StreamWriter writer, Process? client)
	{
		try
		{
			string text = reader.ReadLine();
			Context.Info("客户端请求: " + text);
			string[] array = text?.Split(new char[1] { ' ' }, 2) ?? Array.Empty<string>();
			if (array.Length < 2 || array[1].Length == 0)
			{
				throw new RpcException("请求参数过少");
			}
			string text2 = array[0].ToUpperInvariant();
			if (!_RequestType.Contains(text2))
			{
				throw new RpcException("请求类型必须为 " + string.Join("/", _RequestTypeArray) + " 其中之一");
			}
			string text3 = array[1];
			StringBuilder stringBuilder = new StringBuilder();
			for (int num = reader.Read(); num != 27; num = reader.Read())
			{
				stringBuilder.Append((char)num);
			}
			string text4 = ((stringBuilder.Length == 0) ? null : stringBuilder.ToString());
			switch (text2)
			{
			case "GET":
			case "SET":
			{
				text3 = text3.ToLowerInvariant();
				if (!_PropertyMap.TryGetValue(text3, out RpcProperty value2))
				{
					throw new RpcException("不存在属性 " + text3);
				}
				RpcResponse rpcResponse2;
				if (text2 == "GET")
				{
					try
					{
						string value3 = value2.Value;
						rpcResponse2 = new RpcResponse(RpcResponseStatus.Success, RpcResponseType.Text, value3, text3);
						Context.Trace("返回值: " + value3);
					}
					catch (RpcPropertyOperationFailedException)
					{
						rpcResponse2 = RpcResponse.EmptyFailure;
						Context.Debug("设置失败: 只写属性或请求被拒绝");
					}
				}
				else if (value2.Settable)
				{
					try
					{
						value2.Value = text4;
						rpcResponse2 = RpcResponse.EmptySuccess;
						Context.Trace("设置成功: " + text4);
					}
					catch (RpcPropertyOperationFailedException)
					{
						rpcResponse2 = RpcResponse.EmptyFailure;
						Context.Debug("设置失败: 请求被拒绝");
					}
				}
				else
				{
					rpcResponse2 = RpcResponse.EmptyFailure;
					Context.Debug("设置失败: 只读属性");
				}
				rpcResponse2.Response(writer);
				break;
			}
			case "REQ":
			{
				string[] array2 = text3.Split(new char[1] { ' ' }, 2);
				string text5 = array2[0].ToLowerInvariant();
				bool indent = false;
				if (text5.EndsWith('$'))
				{
					indent = true;
					string text6 = text5;
					text5 = text6.Substring(0, text6.Length - 1);
				}
				if (!_FunctionMap.TryGetValue(text5, out RpcFunction value))
				{
					throw new RpcException("不存在函数 " + text5);
				}
				string text7 = null;
				if (array2.Length > 1)
				{
					text7 = array2[1];
				}
				Context.Trace("正在调用函数 " + text5 + " " + text7);
				RpcResponse rpcResponse = value(text7, text4, indent);
				rpcResponse.Response(writer);
				Context.Trace($"函数已退出，返回状态 {rpcResponse.Status}");
				break;
			}
			}
		}
		catch (Exception ex3)
		{
			if (ex3 is RpcException ex4)
			{
				string reason = ex4.Reason;
				RpcResponse.Err(reason).Response(writer);
				Context.Info("出错: " + reason);
			}
			else
			{
				RpcResponse.Err(ex3.ToString(), "stacktrace").Response(writer);
				Context.Error("处理请求时发生异常", ex3);
			}
		}
		return true;
	}

	public RpcService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		await _StopAsync();
	}

	public async Task StartAsync()
	{
		_Start();
		if (DependencyGroups.InvokeInjection(new Action<ImmutableList<(RpcFunction, string)>>(_CollectRpcFunctionRegistry), "rpc-function", AttributeTargets.Method))
		{
			Context.Trace("Dependency injection success: rpc-function@64");
		}
		else
		{
			Context.Warn("Dependency injection failed: rpc-function@64");
		}
		if (DependencyGroups.InvokeInjection(new Action<ImmutableList<(PropertyAccessor<string>, string)>>(_CollectRpcPropertyRegistry), "rpc-property", AttributeTargets.Property))
		{
			Context.Trace("Dependency injection success: rpc-property@128");
		}
		else
		{
			Context.Warn("Dependency injection failed: rpc-property@128");
		}
	}

	static RpcService()
	{
		HashSet<string> hashSet = new HashSet<string>();
		string[] requestTypeArray = _RequestTypeArray;
		foreach (string item in requestTypeArray)
		{
			hashSet.Add(item);
		}
		_RequestType = hashSet;
		_PropertyMap = new Dictionary<string, RpcProperty>();
		_FunctionMap = new Dictionary<string, RpcFunction>
		{
			["ping"] = (string? _, string? _, bool _) => RpcResponse.EmptySuccess,
			["activate"] = (string? _, string? _, bool _) =>
			{
				if (Lifecycle.CurrentState >= LifecycleState.WindowCreated)
				{
					ActivateMainWindow();
				}
				else
				{
					Lifecycle.When(LifecycleState.WindowCreated, ActivateMainWindow);
				}
				return RpcResponse.EmptySuccess;
				static void ActivateMainWindow()
				{
					Application app = Lifecycle.CurrentApplication;
					((DispatcherObject)app).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
					{
						Window mainWindow = app.MainWindow;
						if (mainWindow.WindowState == WindowState.Minimized)
						{
							mainWindow.WindowState = WindowState.Normal;
						}
						if (!mainWindow.Topmost)
						{
							mainWindow.Topmost = true;
							mainWindow.Topmost = false;
						}
						mainWindow.Activate();
					}), Array.Empty<object>());
				}
			}
		};
		Context = null;
	}
}
