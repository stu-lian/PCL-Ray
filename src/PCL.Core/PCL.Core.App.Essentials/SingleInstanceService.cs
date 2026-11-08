using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PCL.Core.App.IoC;
using PCL.Core.IO;
using PCL.Core.Utils;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.BeforeLoading, Priority = -2134567890)]
[LifecycleScope("single-instance", "单例", false)]
public sealed class SingleInstanceService : ILifecycleService
{
	private static FileStream? _lockStream;

	private static readonly string _LockFilePath = Path.Combine(Paths.SharedLocalData, "instance.lock");

	public string Identifier => "single-instance";

	public string Name => "单例";

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

	private static void _TryRpc(string processId, string content)
	{
		string pipeName = "PCLCE_RPC@" + processId;
		using NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
		namedPipeClientStream.Connect(1000);
		using StreamWriter streamWriter = new StreamWriter(namedPipeClientStream, PipeComm.PipeEncoding);
		streamWriter.WriteLine(content);
		streamWriter.Write('\u001b');
		streamWriter.Flush();
	}

	[LifecycleStart]
	private static void _Start()
	{
		try
		{
			FileStream fileStream = File.Open(_LockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
			Context.Debug("未发现重复实例，正在向单例锁写入信息");
			using StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.ASCII, 8, leaveOpen: true);
			streamWriter.Write(Basics.CurrentProcessId);
			streamWriter.Flush();
			_lockStream = fileStream;
		}
		catch (Exception)
		{
			try
			{
				using FileStream stream = File.Open(_LockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using StreamReader streamReader = new StreamReader(stream);
				string text = streamReader.ReadToEnd();
				Context.Info("发现重复实例 " + text + "，尝试传递参数并拉起主窗口");
				try
				{
					_TryRpc(text, "REQ cli\n" + JsonSerializer.Serialize(StartupService.UnhandledCommands, JsonCompat.SerializerOptions));
					_TryRpc(text, "REQ activate");
				}
				catch (Exception ex2)
				{
					Context.Warn("RPC 通信失败", ex2);
				}
			}
			catch (Exception ex3)
			{
				Context.Error("读取单例锁出错", ex3);
			}
			finally
			{
				Context.RequestExit(1);
			}
		}
	}

	[LifecycleStop]
	private static void _Stop()
	{
		if (_lockStream != null)
		{
			Context.Debug("正在删除单例锁");
			_lockStream.Dispose();
			File.Delete(_LockFilePath);
		}
	}

	public SingleInstanceService()
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
