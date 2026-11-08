using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Utils.OS;

namespace PCL.Core.IO;

public static class PipeComm
{
	public static readonly Encoding PipeEncoding = Encoding.UTF8;

	public const char PipeEndingChar = '\u001b';

	private static void _PipeLog(string message)
	{
		LogWrapper.Trace("Pipe", message);
	}

	private static void _PipeLogDebug(string message)
	{
		LogWrapper.Debug("Pipe", message);
	}

	public static NamedPipeServerStream StartPipeServer(string identifier, string pipeName, Func<StreamReader, StreamWriter, Process?, bool> loopCallback, Action? stopCallback = null, bool stopWhenException = false, int[]? allowedProcessId = null)
	{
		NamedPipeServerStream pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024, 1024);
		string threadName = "PipeServer/" + identifier;
		Basics.RunInNewThread(() =>
		{
			LogWrapper.Debug("Pipe", $"{identifier}: {pipeName} 服务端已在 '{threadName}' 工作线程启动");
			bool flag = true;
			bool flag2 = false;
			while (flag)
			{
				try
				{
					flag = false;
					pipe.WaitForConnection();
					Process arg = null;
					int clientProcessId = 0;
					try
					{
						uint namedPipeClientProcessId = KernelInterop.GetNamedPipeClientProcessId(pipe.SafePipeHandle.DangerousGetHandle());
						clientProcessId = (int)namedPipeClientProcessId;
						if (allowedProcessId == null || !allowedProcessId.All((int id) => id != clientProcessId))
						{
							arg = Process.GetProcessById(clientProcessId);
							goto end_IL_008d;
						}
						flag = true;
						pipe.Disconnect();
						_PipeLog($"[Pipe] {identifier}: 已拒绝 {clientProcessId}");
						continue;
						end_IL_008d:;
					}
					catch (Exception)
					{
						if (allowedProcessId != null)
						{
							flag = true;
							throw;
						}
					}
					flag2 = true;
					LogWrapper.Debug("Pipe", $"{identifier}: {clientProcessId} 已连接");
					StreamReader streamReader = new StreamReader(pipe, PipeEncoding, detectEncodingFromByteOrderMarks: false, 1024, leaveOpen: true);
					StreamWriter streamWriter = new StreamWriter(pipe, PipeEncoding, 1024, leaveOpen: true);
					flag = loopCallback(streamReader, streamWriter, arg);
					streamWriter.Write('\u001b');
					streamWriter.Flush();
					streamReader.Read();
					goto IL_024f;
				}
				catch (Exception ex2)
				{
					if ((!pipe.IsConnected & flag2) && ex2 is IOException)
					{
						_PipeLogDebug(identifier + ": 客户端连接已丢失");
						flag = true;
					}
					else
					{
						LogWrapper.Warn(ex2, "Pipe", identifier + ": 服务端出错");
						if (stopWhenException)
						{
							flag = false;
						}
					}
					goto IL_024f;
				}
				continue;
				IL_024f:
				try
				{
					pipe.Disconnect();
				}
				catch (InvalidOperationException)
				{
				}
				flag2 = false;
				_PipeLogDebug(identifier + ": 已断开连接");
			}
			pipe.Dispose();
			_PipeLogDebug(identifier + ": 服务端已停止");
			stopCallback?.Invoke();
		}, threadName);
		return pipe;
	}
}
