using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using PCL.Core.IO.Net.Dns;
using PCL.Core.Logging;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft;

public static class ServerAddressResolver
{
	public readonly record struct ResolvedServerAddress(string Host, string? Ip, int Port);

	private sealed record SrvRecord(int Priority, int Weight, int Port, string Target);

	private const int DefaultPort = 25565;

	private static readonly TimeSpan _HappyEyeballsStagger = TimeSpan.FromMilliseconds(250L);

	private static readonly TimeSpan _ConnectTimeout = TimeSpan.FromSeconds(2.5);

	private static readonly Regex _BracketedIpv6 = new Regex("^\\[(?<ip>.+?)\\](?::(?<port>\\d{1,5}))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex _TrailingPort = new Regex(":(?<port>\\d{1,5})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static async Task<ResolvedServerAddress> GetResolvedServerAddressAsync(string address, CancellationToken cancelToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(address))
		{
			throw new ArgumentException("服务器地址不能为空", "address");
		}
		address = _NormalizeInput(address);
		var (hostOrIp, num) = _ParseHostAndPort(address);
		if (num.HasValue)
		{
			int explicitPort = num.GetValueOrDefault();
			_ValidatePort(explicitPort);
			LogWrapper.Info($"使用显式端口，跳过 SRV：{hostOrIp}:{explicitPort}");
			(string, int)? tuple2 = await _ResolveReachableAsync(hostOrIp, explicitPort, cancelToken).ConfigureAwait(continueOnCapturedContext: false);
			if (tuple2.HasValue)
			{
				return new ResolvedServerAddress(hostOrIp, tuple2.Value.Item1, tuple2.Value.Item2);
			}
			return new ResolvedServerAddress(hostOrIp, await _ResolveFirstIpAsync(hostOrIp, cancelToken).ConfigureAwait(continueOnCapturedContext: false), explicitPort);
		}
		if (IPAddress.TryParse(hostOrIp, out IPAddress _))
		{
			(string, int)? tuple3 = await _ResolveReachableAsync(hostOrIp, 25565, cancelToken).ConfigureAwait(continueOnCapturedContext: false);
			if (tuple3.HasValue)
			{
				return new ResolvedServerAddress(hostOrIp, tuple3.Value.Item1, tuple3.Value.Item2);
			}
			return new ResolvedServerAddress(hostOrIp, hostOrIp, 25565);
		}
		string idnHost = _ToAsciiIdn(hostOrIp);
		List<SrvRecord> srvOrdered = await _QuerySrvOrderedAsync(idnHost, cancelToken).ConfigureAwait(continueOnCapturedContext: false);
		if (srvOrdered.Count > 0)
		{
			LogWrapper.Info($"SRV 记录可用（{srvOrdered.Count}）: _minecraft._tcp.{idnHost}");
			foreach (SrvRecord item in srvOrdered)
			{
				string targetHost = _TrimTrailingDot(item.Target);
				int port = item.Port;
				(string, int)? tuple4 = await _ResolveReachableAsync(targetHost, port, cancelToken).ConfigureAwait(continueOnCapturedContext: false);
				if (tuple4.HasValue)
				{
					LogWrapper.Info($"SRV 命中：{targetHost}:{port} -> {tuple4.Value.Item1}:{port}");
					return new ResolvedServerAddress(idnHost, tuple4.Value.Item1, tuple4.Value.Item2);
				}
			}
			SrvRecord first = srvOrdered[0];
			string text = await _ResolveFirstIpAsync(_TrimTrailingDot(first.Target), cancelToken).ConfigureAwait(continueOnCapturedContext: false);
			if (!string.IsNullOrEmpty(text))
			{
				return new ResolvedServerAddress(idnHost, text, first.Port);
			}
		}
		else
		{
			LogWrapper.Info($"无 SRV 记录或查询失败，回退默认端口：{idnHost}:{25565}");
		}
		return new ResolvedServerAddress(idnHost, await _ResolveFirstIpAsync(idnHost, cancelToken).ConfigureAwait(continueOnCapturedContext: false), 25565);
	}

	private static string _NormalizeInput(string input)
	{
		string text = input.Trim();
		int num = text.IndexOf("://", StringComparison.Ordinal);
		if (num > 0)
		{
			text = text.Substring(num + 3);
		}
		while (text.EndsWith("/", StringComparison.Ordinal))
		{
			string text2 = text;
			text = text2.Substring(0, text2.Length - 1);
		}
		return text;
	}

	private static void _ValidatePort(int port)
	{
		if ((port < 1 || port > 65535) ? true : false)
		{
			throw new FormatException($"无效的端口：{port}");
		}
	}

	private static string _ToAsciiIdn(string host)
	{
		try
		{
			IdnMapping idnMapping = new IdnMapping();
			string unicode = _TrimTrailingDot(host);
			return idnMapping.GetAscii(unicode) + (host.EndsWith(".", StringComparison.Ordinal) ? "." : "");
		}
		catch
		{
			return host;
		}
	}

	private static string _TrimTrailingDot(string host)
	{
		if (!host.EndsWith(".", StringComparison.Ordinal))
		{
			return host;
		}
		return host.Substring(0, host.Length - 1);
	}

	private static (string HostOrIp, int? Port) _ParseHostAndPort(string input)
	{
		Match match = _BracketedIpv6.Match(input);
		IPAddress address;
		if (match.Success)
		{
			string value = match.Groups["ip"].Value;
			if (!IPAddress.TryParse(value, out address))
			{
				throw new FormatException("无效的 IPv6 地址格式");
			}
			Group obj = match.Groups["port"];
			if (obj.Success)
			{
				int num = int.Parse(obj.Value, CultureInfo.InvariantCulture);
				_ValidatePort(num);
				return (HostOrIp: value, Port: num);
			}
			return (HostOrIp: value, Port: null);
		}
		if (IPAddress.TryParse(input, out address))
		{
			return (HostOrIp: input, Port: null);
		}
		Match match2 = _TrailingPort.Match(input);
		if (match2.Success && input.Count((char c) => c == ':') == 1)
		{
			int num2 = int.Parse(match2.Groups["port"].Value, CultureInfo.InvariantCulture);
			_ValidatePort(num2);
			int length = match2.Value.Length;
			string text = input.Substring(0, input.Length - length);
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new FormatException("无效的主机名");
			}
			return (HostOrIp: text, Port: num2);
		}
		return (HostOrIp: input, Port: null);
	}

	private static async Task<List<SrvRecord>> _QuerySrvOrderedAsync(string domain, CancellationToken ct)
	{
		try
		{
			string text = "_minecraft._tcp." + _TrimTrailingDot(domain);
			LogWrapper.Info("尝试 SRV 查询：" + text);
			DnsMessage val = await DnsQuery.Instance.QueryAsync(text, (DnsQueryType)33, ct);
			if (val == null || val.Answers.Count == 0)
			{
				return new List<SrvRecord>();
			}
			List<SrvRecord> list = new List<SrvRecord>();
			foreach (DnsResourceRecord answer in val.Answers)
			{
				IDnsResource resource = answer.Resource;
				DnsUnknownResource val2 = (DnsUnknownResource)(object)((resource is DnsUnknownResource) ? resource : null);
				if (val2 == null)
				{
					return new List<SrvRecord>();
				}
				DnsSrvResource dnsSrvResource = new DnsSrvResource();
				int offset = 0;
				dnsSrvResource.ReadBytes(val2.Raw, ref offset, val2.Raw.Length);
				list.Add(new SrvRecord(dnsSrvResource.Priority, dnsSrvResource.Weight, dnsSrvResource.Port, dnsSrvResource.Target));
			}
			list.RemoveAll((SrvRecord p) => p.Target == ".");
			if (list.Count == 0)
			{
				return new List<SrvRecord>();
			}
			List<SrvRecord> list2 = new List<SrvRecord>(list.Count);
			foreach (IGrouping<int, SrvRecord> item2 in from p in list
				group p by p.Priority into g
				orderby g.Key
				select g)
			{
				List<SrvRecord> list3 = item2.ToList();
				while (list3.Count > 0)
				{
					SrvRecord item = _PopByWeight(list3);
					list2.Add(item);
				}
			}
			return list2;
		}
		catch (SocketException ex)
		{
			LogWrapper.Warn(ex, "SRV 查询失败（网络错误）");
			return new List<SrvRecord>();
		}
		catch (Exception ex2)
		{
			LogWrapper.Warn(ex2, "SRV 查询异常");
			return new List<SrvRecord>();
		}
	}

	private static SrvRecord _PopByWeight(List<SrvRecord> pool)
	{
		int num = pool.Sum((SrvRecord p) => p.Weight);
		if (num <= 0)
		{
			int index = RandomUtils.NextInt(0, pool.Count - 1);
			SrvRecord result = pool[index];
			pool.RemoveAt(index);
			return result;
		}
		int num2 = RandomUtils.NextInt(1, num);
		int num3 = 0;
		for (int num4 = 0; num4 < pool.Count; num4++)
		{
			num3 += pool[num4].Weight;
			if (num3 >= num2)
			{
				SrvRecord result2 = pool[num4];
				pool.RemoveAt(num4);
				return result2;
			}
		}
		SrvRecord result3 = pool[pool.Count - 1];
		pool.RemoveAt(pool.Count - 1);
		return result3;
	}

	private static async Task<(string Ip, int Port)?> _ResolveReachableAsync(string hostOrIp, int port, CancellationToken ct)
	{
		_ = 3;
		try
		{
			if (IPAddress.TryParse(hostOrIp, out IPAddress ipLiteral))
			{
				if ((await _ConnectOneAsync(ipLiteral, port, ct).ConfigureAwait(continueOnCapturedContext: false)).Item1)
				{
					return (ipLiteral.ToString(), port);
				}
				return null;
			}
			IPAddress[] array = await Dns.GetHostAddressesAsync(_TrimTrailingDot(hostOrIp), ct).ConfigureAwait(continueOnCapturedContext: false);
			if (array.Length == 0)
			{
				return null;
			}
			IPAddress[] addrs = array.Where((IPAddress a) => a.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
			IPAddress[] v4 = array.Where((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
			string text = await _ConnectAnyAsync(addrs, port, TimeSpan.Zero, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text != null)
			{
				return (text, port);
			}
			text = await _ConnectAnyAsync(v4, port, _HappyEyeballsStagger, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text != null)
			{
				return (text, port);
			}
			return null;
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			LogWrapper.Warn(ex, $"解析或连接失败：{hostOrIp}:{port}");
			return null;
		}
	}

	private static async Task<string?> _ResolveFirstIpAsync(string hostOrIp, CancellationToken ct)
	{
		try
		{
			if (IPAddress.TryParse(hostOrIp, out IPAddress address))
			{
				return address.ToString();
			}
			IPAddress[] source = await Dns.GetHostAddressesAsync(_TrimTrailingDot(hostOrIp), ct).ConfigureAwait(continueOnCapturedContext: false);
			return (source.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetworkV6) ?? source.FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork))?.ToString();
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			LogWrapper.Warn(ex, "DNS 解析失败：" + hostOrIp);
			return null;
		}
	}

	private static async Task<string?> _ConnectAnyAsync(IReadOnlyList<IPAddress> addrs, int port, TimeSpan delay, CancellationToken ct)
	{
		if (addrs.Count == 0)
		{
			return null;
		}
		if (delay > TimeSpan.Zero)
		{
			await Task.Delay(delay, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		try
		{
			List<Task<(bool ok, string ip)>> tasks = new List<Task<(bool, string)>>(addrs.Count);
			tasks.AddRange(addrs.Select((IPAddress ip2) => _ConnectOneAsync(ip2, port, cts.Token)));
			while (tasks.Count > 0)
			{
				Task<(bool, string)> task = await Task.WhenAny(tasks).ConfigureAwait(continueOnCapturedContext: false);
				tasks.Remove(task);
				var (flag, ip) = await task.ConfigureAwait(continueOnCapturedContext: false);
				if (flag)
				{
					try
					{
						await cts.CancelAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					catch
					{
					}
					return ip;
				}
			}
			return null;
		}
		finally
		{
			if (cts != null)
			{
				((IDisposable)cts).Dispose();
			}
		}
	}

	private static async Task<(bool ok, string ip)> _ConnectOneAsync(IPAddress ip, int port, CancellationToken ct)
	{
		try
		{
			using Socket sock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			sock.NoDelay = true;
			using CancellationTokenSource timeoutCts = new CancellationTokenSource(_ConnectTimeout);
			using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
			await sock.ConnectAsync(new IPEndPoint(ip, port), linked.Token).ConfigureAwait(continueOnCapturedContext: false);
			return (ok: true, ip: ip.ToString());
		}
		catch
		{
			return (ok: false, ip: ip.ToString());
		}
	}
}
