using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using PCL.Core.IO.Net.Http;
using PCL.Core.Logging;

namespace PCL.Core.IO.Net.Dns;

public class DnsQuery : IDisposable
{
	private const string ModuleName = "DoH query";

	private readonly DnsCachingClient _resolver;

	private readonly HttpClient[] _httpClients;

	public static DnsQuery Instance { get; } = new DnsQuery();

	private DnsQuery()
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected Obj, but got Unknown
		HttpClientHandler handler = new HttpClientHandler
		{
			Proxy = HttpProxyManager.Instance
		};
		_httpClients = new HttpClient[3]
		{
			new HttpClient(handler)
			{
				BaseAddress = new Uri("https://doh.pub/")
			},
			new HttpClient(handler)
			{
				BaseAddress = new Uri("https://doh.pysio.online/")
			},
			new HttpClient(handler)
			{
				BaseAddress = new Uri("https://cloudflare-dns.com/")
			}
		};
		_resolver = new DnsCachingClient((IDnsClient)(object)new WeightedDnsRacerClient(2, ((IEnumerable<IDnsClient>)((IEnumerable<HttpClient>)_httpClients).Select((Func<HttpClient, DnsHttpClient>)((HttpClient x) =>
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected Obj, but got Unknown
			return new DnsHttpClient(x);
		}))).ToArray()), (ObjectCache)new MemoryCache("DoH Query Cache"));
	}

	public async Task<DnsMessage?> QueryAsync(string host, DnsQueryType qType, CancellationToken cts = default(CancellationToken))
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return await _resolver.Query(DnsQueryFactory.CreateQuery(host, qType), cts);
		}
		catch (TaskCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			LogWrapper.Warn("DoH query", $"Failed to resolve DNS for {host}: {ex2.Message}, use system default DNS");
		}
		return null;
	}

	public async Task<IPAddress[]?> QueryForIpAsync(string host, CancellationToken cts = default(CancellationToken))
	{
		InlineArray2<Task<DnsMessage>> buffer = default;
		buffer[0] = QueryAsync(host, (DnsQueryType)1, cts);
		buffer[1] = QueryAsync(host, (DnsQueryType)28, cts);
		DnsMessage[] source = await Task.WhenAll<DnsMessage>(buffer);
		if (source.All((DnsMessage x) => x == null))
		{
			LogWrapper.Warn("DoH query", "Failed to query IP for host " + host + " using DoH, use system default DNS");
			return await System.Net.Dns.GetHostAddressesAsync(host, cts);
		}
		return (from x in source.Where((DnsMessage x) => x != null).SelectMany((DnsMessage x) => x.Answers).Where((DnsResourceRecord x) =>
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0009: Invalid comparison between Unknown and I4
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				//IL_000e: Invalid comparison between Unknown and I4
				DnsQueryType type = x.Type;
				return ((int)type == 1 || (int)type == 28) ? true : false;
			})
				.Select((DnsResourceRecord x) =>
				{
					IDnsResource resource = x.Resource;
					return (DnsIpAddressResource)(object)((resource is DnsIpAddressResource) ? resource : null);
				})
			where x != null
			select x.IPAddress).ToArray();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		HttpClient[] httpClients = _httpClients;
		for (int i = 0; i < httpClients.Length; i++)
		{
			httpClients[i].Dispose();
		}
		_resolver.Dispose();
	}
}
