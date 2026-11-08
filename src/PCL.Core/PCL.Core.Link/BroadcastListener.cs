using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL.Core.Link;

public class BroadcastListener(bool receiveLocalOnly = true) : IDisposable
{
	private UdpClient? _client;

	private UdpClient? _clientV6;

	private CancellationTokenSource? _cts;

	private static readonly IPAddress _MulticastAddress = IPAddress.Parse("224.0.2.60");

	private static readonly IPAddress _MulticastAddressV6 = IPAddress.Parse("ff75:230::60");

	private Task? _listenTask;

	private Task? _listenTaskV6;

	private bool _isDisposed;

	public event Action<BroadcastRecord, IPEndPoint>? OnReceive;

	public void Start()
	{
		if (_client == null && _clientV6 == null)
		{
			_cts = new CancellationTokenSource();
			_client = new UdpClient();
			_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			_client.Client.Bind(new IPEndPoint(IPAddress.Any, 4445));
			_client.JoinMulticastGroup(_MulticastAddress);
			_clientV6 = new UdpClient(AddressFamily.InterNetworkV6);
			_clientV6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			_clientV6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, 4445));
			_clientV6.JoinMulticastGroup(_MulticastAddressV6);
			_listenTask = _ListenThreadAsync(_client);
			_listenTaskV6 = _ListenThreadAsync(_clientV6);
		}
	}

	private async Task _ListenThreadAsync(UdpClient? client)
	{
		while (_cts != null && client != null && !_cts.IsCancellationRequested)
		{
			try
			{
				UdpReceiveResult udpReceiveResult = await client.ReceiveAsync(_cts.Token);
				byte[] buffer = udpReceiveResult.Buffer;
				IPEndPoint remoteEndPoint = udpReceiveResult.RemoteEndPoint;
				if (_TryParseServerInfo(Encoding.UTF8.GetString(buffer), out BroadcastRecord serverInfo) && (object)serverInfo != null && (!receiveLocalOnly || _isAddressLocal(remoteEndPoint.Address)))
				{
					OnReceive?.Invoke(serverInfo, remoteEndPoint);
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex2)
			{
				Console.WriteLine("Error processing packet: " + ex2.Message);
			}
		}
	}

	private static bool _isAddressLocal(IPAddress address)
	{
		return NetworkUtils.GetAllLocalAddress().Contains(address, null);
	}

	private static bool _TryParseServerInfo(string rawMessage, out BroadcastRecord? serverInfo)
	{
		Match match = RegexPatterns.BroadcastMotd.Match(rawMessage);
		Match match2 = RegexPatterns.BroadcastAd.Match(rawMessage);
		if (!match2.Success || !int.TryParse(match2.Groups[1].Value.Trim(), out var result))
		{
			serverInfo = null;
			return false;
		}
		serverInfo = new BroadcastRecord(match.Success ? match.Groups[1].Value.Trim() : "missing no", new IPEndPoint(IPAddress.Loopback, result), DateTime.Now);
		return true;
	}

	public void Stop()
	{
		if (_client != null)
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
			_client?.Close();
			_client?.Dispose();
			_client = null;
			_clientV6?.Close();
			_clientV6?.Dispose();
			_clientV6 = null;
			_listenTask?.Wait(500);
			_listenTaskV6?.Wait(500);
		}
	}

	public void Dispose()
	{
		if (!_isDisposed)
		{
			_isDisposed = true;
			Stop();
		}
	}
}
