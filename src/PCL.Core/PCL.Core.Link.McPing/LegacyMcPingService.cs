using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Logging;

namespace PCL.Core.Link.McPing;

public class LegacyMcPingService : IMcPingService, IDisposable
{
	private readonly IPEndPoint _endpoint;

	private readonly string _host;

	private const int DefaultTimeout = 10000;

	private readonly int _timeout;

	private bool _disposed;

	public IPEndPoint Endpoint => _endpoint;

	public string Host => _host;

	public int Timeout => _timeout;

	public LegacyMcPingService(IPEndPoint endpoint, int timeout = 10000)
	{
		_endpoint = endpoint;
		_host = _endpoint.Address.ToString();
		_timeout = timeout;
	}

	public LegacyMcPingService(string ip, int port = 25565, int timeout = 10000)
	{
		_endpoint = (IPAddress.TryParse(ip, out IPAddress address) ? new IPEndPoint(address, port) : new IPEndPoint(Dns.GetHostAddresses(ip).First(), port));
		_host = ip;
		_timeout = timeout;
	}

	public async Task<McPingResult?> PingAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		Socket so = new Socket(SocketType.Stream, ProtocolType.Tcp);
		try
		{
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(_timeout);
			using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
			linkedCts.Token.Register(() =>
			{
				try
				{
					if (so.Connected)
					{
						so.Close();
					}
				}
				catch (ObjectDisposedException)
				{
				}
			});
			await so.ConnectAsync(_endpoint, linkedCts.Token);
			LogWrapper.Debug("LegacyMcPing", $"Connected to {_endpoint}");
			McPingResult result;
			await using (NetworkStream stream = new NetworkStream(so, ownsSocket: false))
			{
				byte[] array = new byte[2] { 254, 1 };
				await stream.WriteAsync(array.AsMemory(0, array.Length), linkedCts.Token);
				MemoryStream ms = new MemoryStream();
				await stream.CopyToAsync(ms, linkedCts.Token);
				so.Close();
				byte[] array2 = ms.ToArray();
				if (array2.Length < 21 || (array2.Length >= 21 && array2[0] != byte.MaxValue))
				{
					LogWrapper.Info("McPing", $"Unknown response from {_endpoint}, ignore");
					result = null;
				}
				else
				{
					string text = Encoding.UTF8.GetString(array2);
					try
					{
						string[] source = text.Split(new string[1] { "\0\0\0" }, StringSplitOptions.None);
						source = source.Select((string s) => new string(new ReadOnlySpan<char>(s.Where((char _, int index) => index % 2 == 0).ToArray()))).ToArray();
						result = ((source.Length >= 6) ? new McPingResult(new McPingVersionResult(source[2], int.Parse(source[1])), new McPingPlayerResult(int.Parse(source[5]), int.Parse(source[4]), new List<McPingPlayerSampleResult>()), source[3], string.Empty, 0L, new McPingModInfoResult(string.Empty, new List<McPingModInfoModResult>()), null) : null);
					}
					catch (Exception ex)
					{
						LogWrapper.Error(ex, "McPing", $"Unable to serialize response from {_endpoint}");
						result = null;
					}
				}
			}
			return result;
		}
		finally
		{
			if (so != null)
			{
				((IDisposable)so).Dispose();
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}
