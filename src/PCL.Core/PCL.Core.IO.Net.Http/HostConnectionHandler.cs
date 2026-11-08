using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Dns;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.IO.Net.Http;

public class HostConnectionHandler
{
	private const string ModuleName = "HostConnectionHandler";

	private readonly DnsQuery _dnsQuery = DnsQuery.Instance;

	private static readonly TimeSpan _CacheDuration = TimeSpan.FromMinutes(10L);

	private const int WaitTasks = 2;

	private readonly ConcurrentDictionary<(string host, int port), ConcurrentDictionary<IPAddress, DateTime>> _connectionCache = new ConcurrentDictionary<(string, int), ConcurrentDictionary<IPAddress, DateTime>>();

	public static HostConnectionHandler Instance { get; } = new HostConnectionHandler();

	public async ValueTask<Stream> GetConnectionAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
	{
		string host = context.DnsEndPoint.Host;
		int port = context.DnsEndPoint.Port;
		DateTime now = DateTime.UtcNow;
		if (IPAddress.TryParse(host, out IPAddress address))
		{
			return await _ConnectToAddressAsync(address, port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		IPAddress[] array = await _dnsQuery.QueryForIpAsync(host, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (array == null || array.Length == 0)
		{
			throw new HttpRequestException("DNS resolution failed for " + host);
		}
		IPAddress[] array2 = _SortAddresses(host, port, array, now);
		List<Task<NetworkStream>> connectionTasks = new List<Task<NetworkStream>>(2);
		CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		CancellationToken token = cancellationSource.Token;
		try
		{
			for (int i = 0; i < array2.Length; i++)
			{
				IPAddress iPAddress = array2[i];
				int delay = i * (iPAddress.AddressFamily.Equals(AddressFamily.InterNetwork) ? 150 : 80);
				connectionTasks.Add(_DelayedConnectAsync(iPAddress, port, delay, token));
			}
			Task<NetworkStream> winner = await connectionTasks.ToArray().WhenAnySuccessAsync().ConfigureAwait(continueOnCapturedContext: false);
			NetworkStream networkStream = await winner.ConfigureAwait(continueOnCapturedContext: false);
			IPAddress address2 = ((IPEndPoint)networkStream.Socket.RemoteEndPoint).Address;
			_UpdateConnectionCache(host, port, address2, now);
			LogWrapper.Debug("HostConnectionHandler", $"Connected to {host} via {address2}");
			cancellationSource.Cancel();
			_CleanupUnusedConnectionsAsync(connectionTasks, winner).ConfigureAwait(continueOnCapturedContext: false);
			return networkStream;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex2)
		{
			cancellationSource.Cancel();
			_CleanupUnusedConnectionsAsync(connectionTasks, null).ConfigureAwait(continueOnCapturedContext: false);
			LogWrapper.Error(ex2, "HostConnectionHandler", "All connection attempts failed for " + host);
			throw new HttpRequestException("Connection failed for " + host, ex2);
		}
		finally
		{
			cancellationSource.Dispose();
		}
	}

	private IPAddress[] _SortAddresses(string host, int port, IPAddress[] addresses, DateTime now)
	{
		ConcurrentDictionary<IPAddress, DateTime> addressCache = CollectionExtensions.GetValueOrDefault(key: (host, port), dictionary: _connectionCache);
		List<IPAddress> list = new List<IPAddress>();
		List<IPAddress> list2 = new List<IPAddress>();
		IPAddress[] array = addresses;
		foreach (IPAddress iPAddress in array)
		{
			if (addressCache != null && addressCache.TryGetValue(iPAddress, out var value) && now - value <= _CacheDuration)
			{
				list.Add(iPAddress);
			}
			else
			{
				list2.Add(iPAddress);
			}
		}
		list.Sort((IPAddress a, IPAddress b) => addressCache[b].CompareTo(addressCache[a]));
		List<IPAddress> list3 = (from ip in list2
			where ip.AddressFamily == AddressFamily.InterNetworkV6
			orderby Array.IndexOf(addresses, ip)
			select ip).ToList();
		List<IPAddress> list4 = (from ip in list2
			where ip.AddressFamily == AddressFamily.InterNetwork
			orderby Array.IndexOf(addresses, ip)
			select ip).ToList();
		List<IPAddress> list5 = new List<IPAddress>();
		int num = 0;
		int num2 = 0;
		while (num < list3.Count || num2 < list4.Count)
		{
			if (num < list3.Count)
			{
				list5.Add(list3[num++]);
			}
			if (num2 < list4.Count)
			{
				list5.Add(list4[num2++]);
			}
		}
		return list.Concat(list5).Take(2).ToArray();
	}

	private void _UpdateConnectionCache(string host, int port, IPAddress ip, DateTime now)
	{
		(string, int) key = (host, port);
		ConcurrentDictionary<IPAddress, DateTime> orAdd = _connectionCache.GetOrAdd(key, ((string host, int port) _) => new ConcurrentDictionary<IPAddress, DateTime>());
		foreach (KeyValuePair<IPAddress, DateTime> item in orAdd)
		{
			if (now - item.Value > _CacheDuration)
			{
				orAdd.TryRemove(item.Key, out var _);
			}
		}
		orAdd[ip] = now;
	}

	private static async Task<NetworkStream> _DelayedConnectAsync(IPAddress ip, int port, int delay, CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(delay, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		return await _ConnectToAddressAsync(ip, port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task<NetworkStream> _ConnectToAddressAsync(IPAddress ip, int port, CancellationToken cancellationToken)
	{
		Socket socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
		{
			NoDelay = true
		};
		try
		{
			await socket.ConnectAsync(ip, port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new NetworkStream(socket, ownsSocket: true);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			socket.Dispose();
			throw new HttpRequestException($"Connection to {ip}:{port} failed", ex);
		}
	}

	private static async Task _CleanupUnusedConnectionsAsync(List<Task<NetworkStream>> allTasks, Task<NetworkStream>? winnerTask)
	{
		foreach (Task<NetworkStream> item in allTasks.Where((Task<NetworkStream> t) => t != winnerTask))
		{
			try
			{
				if (item.IsCompletedSuccessfully)
				{
					await (await item.ConfigureAwait(continueOnCapturedContext: false)).DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				if (item.IsCompleted)
				{
					_ = item.Exception;
					continue;
				}
				item.ContinueWith((Task<NetworkStream> t) =>
				{
					if (t.IsCompletedSuccessfully)
					{
						t.Result.Dispose();
					}
					else
					{
						_ = t.Exception;
					}
				}, TaskScheduler.Default);
			}
			catch (Exception ex)
			{
				LogWrapper.Warn(ex, "HostConnectionHandler", "Dispose connection with an error");
			}
		}
	}
}
