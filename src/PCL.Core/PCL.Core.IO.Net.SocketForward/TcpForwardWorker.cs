using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.IO.Net.SocketForward;

public sealed class TcpForwardWorker : IDisposable
{
	private readonly TcpForwardConfig _cfg;

	private volatile CancellationTokenSource _cts;

	private readonly SemaphoreSlim _connectionSemaphore;

	private Task? _workerTask;

	private readonly ConcurrentDictionary<Guid, Task> _subWorkerTask = new ConcurrentDictionary<Guid, Task>();

	private const string ModuleName = "TcpForward";

	private readonly Lock _operationLock = new Lock();

	private bool _disposed;

	public IPEndPoint? LocalEndPoint { get; private set; }

	public int ActiveConnectionCount => _cfg.MaxConnection - _connectionSemaphore.CurrentCount;

	internal TcpForwardWorker(TcpForwardConfig cfg)
	{
		_cfg = cfg;
		_cts = new CancellationTokenSource();
		_connectionSemaphore = new SemaphoreSlim(_cfg.MaxConnection, _cfg.MaxConnection);
	}

	public void Start()
	{
		using (_operationLock.EnterScope())
		{
			Task workerTask = _workerTask;
			if (workerTask != null && !workerTask.IsCompleted)
			{
				return;
			}
			_cts = new CancellationTokenSource();
			_workerTask = _WorkerFunc();
			_workerTask.ContinueWith((Task x) =>
			{
				if (x.IsFaulted)
				{
					LogWrapper.Error(x.Exception, "TcpForward", "工作线程出现错误");
				}
			});
		}
	}

	public void Stop()
	{
		using (_operationLock.EnterScope())
		{
			Task workerTask = _workerTask;
			if (workerTask == null || workerTask.IsCompleted)
			{
				return;
			}
			_cts.Cancel();
			CancellationTokenSource oldCts = _cts;
			workerTask = _workerTask;
			ICollection<Task> values = _subWorkerTask.Values;
			int num = 0;
			Task[] array = new Task[1 + values.Count];
			array[num] = workerTask;
			num++;
			foreach (Task item in values)
			{
				array[num] = item;
				num++;
			}
			Task.WhenAll(new ReadOnlySpan<Task>(array)).ContinueWith((Task x) =>
			{
				oldCts.Dispose();
				if (x.IsFaulted)
				{
					LogWrapper.Error(x.Exception, "TcpForward", "后台关闭工作线程时遇到错误抛出");
				}
			});
			LogWrapper.Info("TcpForward", "TCP 端口转发已停止，转发线程将在后台陆续关闭");
			_subWorkerTask.Clear();
		}
	}

	private async Task _WorkerFunc()
	{
		using Socket listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
		listener.NoDelay = true;
		listener.ReceiveBufferSize = (int)_cfg.BufferSize;
		listener.SendBufferSize = (int)_cfg.BufferSize;
		if (!IPAddress.TryParse(_cfg.LocalHost, out IPAddress address))
		{
			throw new InvalidOperationException("出现意料之外的本地监听地址");
		}
		listener.Bind(new IPEndPoint(address, _cfg.LocalPort));
		listener.Listen();
		if (!(listener.LocalEndPoint is IPEndPoint iPEndPoint))
		{
			throw new InvalidCastException("出现了意外的转换操作");
		}
		LocalEndPoint = iPEndPoint;
		LogWrapper.Info("TcpForward", $"TCP 端口转发已启动，监听 {iPEndPoint}，目标 tcp://{_cfg.RemoteHost}:{_cfg.RemotePort}");
		while (!_cts.IsCancellationRequested)
		{
			try
			{
				Socket clientSocket = await listener.AcceptAsync(_cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (await _connectionSemaphore.WaitAsync(0).ConfigureAwait(continueOnCapturedContext: false))
				{
					Guid taskGuid = Guid.NewGuid();
					_subWorkerTask.TryAdd(taskGuid, _HandleConnectionAsync(clientSocket, _cts.Token).ContinueWith((Task x) =>
					{
						if (x.IsFaulted)
						{
							LogWrapper.Error(x.Exception, "TcpForward", "连接处理线程出现错误");
						}
						try
						{
							_subWorkerTask.TryRemove(taskGuid, out Task _);
							_connectionSemaphore.Release();
						}
						catch (ObjectDisposedException)
						{
						}
					}));
				}
				else
				{
					clientSocket.CloseGracefully();
					LogWrapper.Warn("TcpForward", $"已达到最大连接数限制({_cfg.MaxConnection})，拒绝新连接");
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex2)
			{
				LogWrapper.Error(ex2, "TcpForward", "接受连接时发生错误");
				await Task.Delay(500).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private async Task _HandleConnectionAsync(Socket clientSocket, CancellationToken cancellationToken)
	{
		Guid connectionId = Guid.NewGuid();
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		Socket targetSocket = null;
		try
		{
			LogWrapper.Info("TcpForward", $"接受来自 {clientSocket.RemoteEndPoint} 的连接");
			targetSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
			{
				NoDelay = true,
				ReceiveBufferSize = (int)_cfg.BufferSize,
				SendBufferSize = (int)_cfg.BufferSize
			};
			await targetSocket.ConnectAsync(_cfg.RemoteHost, _cfg.RemotePort, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			LogWrapper.Info("TcpForward", $"开始 TCP 转发 {clientSocket.RemoteEndPoint} <-> {targetSocket.RemoteEndPoint}({connectionId})");
			Task task = _ForwardDataAsync(clientSocket, targetSocket, _cfg.BufferSize, cts.Token);
			Task task2 = _ForwardDataAsync(targetSocket, clientSocket, _cfg.BufferSize, cts.Token);
			await Task.WhenAny(task, task2).ConfigureAwait(continueOnCapturedContext: false);
			await cts.CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
			LogWrapper.Debug("TcpForward", $"TCP 转发 {connectionId} 已结束");
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			LogWrapper.Error(ex2, "TcpForward", $"处理连接 {connectionId} 时发生错误");
		}
		finally
		{
			clientSocket.CloseGracefully();
			targetSocket?.CloseGracefully();
		}
	}

	private static async Task _ForwardDataAsync(Socket source, Socket destination, uint bufferSize, CancellationToken cancellationToken)
	{
		using IMemoryOwner<byte> bufferOwner = MemoryPool<byte>.Shared.Rent((int)bufferSize);
		Memory<byte> buffer = bufferOwner.Memory;
		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				int bytesRead = await source.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (bytesRead == 0)
				{
					break;
				}
				int bytesSend;
				int num2;
				for (bytesSend = 0; bytesRead > bytesSend; bytesSend += num2)
				{
					int num = bytesSend;
					num2 = await destination.SendAsync(buffer.Slice(num, bytesRead - num), SocketFlags.None, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					if (num2 == 0)
					{
						break;
					}
				}
				if (bytesRead != bytesSend)
				{
					break;
				}
			}
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		_Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void _Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			Stop();
			_connectionSemaphore.Dispose();
			_disposed = true;
		}
	}

	~TcpForwardWorker()
	{
		_Dispose(disposing: false);
	}
}
