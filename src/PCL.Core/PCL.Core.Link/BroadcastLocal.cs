using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net;

namespace PCL.Core.Link;

public class BroadcastLocal(string description, int localPort) : IDisposable
{
	private Socket? _broadcastSocket;

	private CancellationTokenSource? _cts;

	private bool _isRunning;

	public void Start()
	{
		if (!_isRunning)
		{
			_cts = new CancellationTokenSource();
			_isRunning = true;
			Task.Run(() => _RunUdpBroadcastAsync(_cts.Token), _cts.Token);
			Console.WriteLine($"开始向本地 Minecraft 客户端广播，端口: {localPort}");
		}
	}

	public void Stop()
	{
		if (_isRunning)
		{
			_cts?.Cancel();
			_isRunning = false;
			_broadcastSocket?.CloseGracefully();
			Console.WriteLine("停止向本地 Minecraft 客户端广播");
		}
	}

	private async Task _RunUdpBroadcastAsync(CancellationToken cancellationToken)
	{
		_ = 2;
		try
		{
			_broadcastSocket = new Socket(SocketType.Dgram, ProtocolType.Udp)
			{
				DualMode = true
			};
			byte[] buffer = Encoding.UTF8.GetBytes($"[MOTD]{description}[/MOTD][AD]{localPort}[/AD]");
			IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Loopback, 4445);
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await _broadcastSocket.SendToAsync(new ArraySegment<byte>(buffer), SocketFlags.None, localEndpoint);
					await Task.Delay(1500, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex2)
				{
					Console.WriteLine("UDP 本地广播错误: " + ex2.Message);
					await Task.Delay(5000, cancellationToken);
				}
			}
		}
		catch (Exception ex3)
		{
			Console.WriteLine("UDP 本地广播任务发生错误: " + ex3.Message);
		}
	}

	public void Dispose()
	{
		Stop();
		_cts?.Dispose();
		_broadcastSocket.CloseGracefully();
		GC.SuppressFinalize(this);
	}
}
