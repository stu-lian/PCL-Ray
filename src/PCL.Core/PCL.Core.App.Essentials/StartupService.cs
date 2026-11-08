using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PCL.Core.App.Cli;
using PCL.Core.App.IoC;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MaxValue)]
[LifecycleScope("startup", "基本信息", false)]
public sealed class StartupService : ILifecycleService
{
	private static readonly Dictionary<string, CommandLine> _UnhandledCommandMap = new Dictionary<string, CommandLine>();

	private static readonly ConcurrentDictionary<string, CommandHandler> _HandleCallbackMap = new ConcurrentDictionary<string, CommandHandler>();

	public static CommandLine CommandLine
	{
		get
		{
			return field ?? throw _GetUninitializedException();
		}
		private set;
	} = null;

	public static IReadOnlyDictionary<string, CommandLine> UnhandledCommands => _UnhandledCommandMap.AsReadOnly();

	public string Identifier => "startup";

	public string Name => "基本信息";

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

	private static Exception _GetUninitializedException()
	{
		return new InvalidOperationException("Not initialized");
	}

	public static bool TryHandleCommand(string command, CommandHandler? handler = null, bool registerCallback = false)
	{
		bool isCallback = false;
		if (handler == null)
		{
			_HandleCallbackMap.TryGetValue(command, out handler);
			if (handler == null)
			{
				return false;
			}
			isCallback = true;
		}
		else if (registerCallback)
		{
			_HandleCallbackMap.TryAdd(command, handler);
		}
		lock (_UnhandledCommandMap)
		{
			_UnhandledCommandMap.TryGetValue(command, out CommandLine value);
			if (value == null)
			{
				return false;
			}
			foreach (string item in _UnhandledCommandMap.Keys.Where((string x) => x.StartsWith(command)).ToList())
			{
				_UnhandledCommandMap.Remove(item);
			}
			try
			{
				handler(value, isCallback);
			}
			catch (Exception ex)
			{
				Context.Warn("Exception thrown while handle command: " + command, ex);
			}
			return true;
		}
	}

	[LifecycleStart]
	private static void _LogBasicInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("\n版本: ").Append(Basics.Metadata.Version).Append(" (")
			.Append(GetArchitectureName(RuntimeInformation.ProcessArchitecture))
			.Append(')');
		stringBuilder.Append("\n路径: ").Append(Basics.ExecutablePath);
		stringBuilder.Append("\n命令行参数:");
		if (Basics.CommandLineArguments.Length == 0)
		{
			stringBuilder.Append(" []");
		}
		else
		{
			string[] commandLineArguments = Basics.CommandLineArguments;
			foreach (string value in commandLineArguments)
			{
				stringBuilder.Append("\n - ").Append(value);
			}
		}
		stringBuilder.Append("\n系统版本: ").Append(Environment.OSVersion.Version).Append(" (")
			.Append(GetArchitectureName(RuntimeInformation.OSArchitecture))
			.Append(')');
		(ulong, ulong) physicalMemoryBytes = KernelInterop.GetPhysicalMemoryBytes();
		stringBuilder.Append("\n可用内存: ").Append(physicalMemoryBytes.Item2 / 1048576).Append('/')
			.Append(physicalMemoryBytes.Item1 / 1048576)
			.Append(" MiB");
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		Encoding encoding = Encoding.GetEncoding(0);
		stringBuilder.Append("\n默认代码页: ").Append(encoding.EncodingName).Append(" (")
			.Append(encoding.CodePage)
			.Append(')');
		stringBuilder.Append("\n管理员身份: ").Append(ProcessInterop.IsAdmin());
		stringBuilder.Append("\n识别码: ").Append(Identify.LauncherId);
		Context.Info(stringBuilder.ToString());
		static string GetArchitectureName(Architecture arch)
		{
			return arch switch
			{
				Architecture.X64 => "x64", 
				Architecture.Arm64 => "ARM64", 
				_ => arch.ToString(), 
			};
		}
	}

	[LifecycleStart]
	private static void _ParseCommandLineArgs()
	{
		IEnumerable<SubcommandDefinition> subcommands = new _003C_003Ez__ReadOnlyArray<SubcommandDefinition>(new SubcommandDefinition[3]
		{
			(commandText: "update", subcommands: new _003C_003Ez__ReadOnlyArray<SubcommandDefinition>(new SubcommandDefinition[3] { "execute", "success", "failed" })),
			(commandText: "activate", subcommands: Array.Empty<SubcommandDefinition>()),
			(commandText: "promote", subcommands: Array.Empty<SubcommandDefinition>())
		});
		Context.Debug("正在解析命令行参数...");
		CommandLine value = PCL.Core.App.Cli.CommandLine.Parse(Basics.FullCommandLineArguments, subcommands);
		StringBuilder stringBuilder = new StringBuilder();
		while (true)
		{
			_UnhandledCommandMap[stringBuilder.ToString()] = value;
			if (value.Subcommand == null)
			{
				break;
			}
			if (stringBuilder.Length != 0)
			{
				stringBuilder.Append('.');
			}
			stringBuilder.Append(value.Subcommand.CommandText);
			value = value.Subcommand;
		}
		_UnhandledCommandMap.Remove("", out value);
		CommandLine = value;
	}

	[RegisterRpc("cli")]
	public static RpcResponse OnRpcCommand(string? argument, string? content, bool indent)
	{
		if (content == null)
		{
			return RpcResponse.Err("Must provide valid JSON model");
		}
		try
		{
			Dictionary<string, CommandLine> models = JsonSerializer.Deserialize<Dictionary<string, CommandLine>>(content, JsonCompat.SerializerOptions);
			if (models == null)
			{
				return RpcResponse.Err("Invalid JSON: empty/null content");
			}
			Task.Run(() =>
			{
				foreach (var (text2, value) in models)
				{
					_UnhandledCommandMap[text2] = value;
					TryHandleCommand(text2);
				}
			});
		}
		catch (JsonException ex)
		{
			return RpcResponse.Err("Invalid JSON: " + ex.Message);
		}
		return RpcResponse.EmptySuccess;
	}

	public StartupService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
	}

	public async Task StartAsync()
	{
		_LogBasicInfo();
		_ParseCommandLineArgs();
		Context.DeclareStopped();
	}
}
