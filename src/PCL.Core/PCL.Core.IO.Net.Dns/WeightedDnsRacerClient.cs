using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ae.Dns.Protocol;
using PCL.Core.Utils.Exts;

namespace PCL.Core.IO.Net.Dns;

public sealed class WeightedDnsRacerClient : IDnsClient, IDisposable
{
	private readonly (IDnsClient Client, byte Weight)[] _clients;

	private readonly int _concurrentCount;

	private bool _updateWeigh = true;

	private readonly object _lock = new object();

	public WeightedDnsRacerClient(int concurrentCount, params IDnsClient[] clients)
	{
		if (clients == null || clients.Length < concurrentCount)
		{
			throw new ArgumentException("Not enough clients.");
		}
		_concurrentCount = concurrentCount;
		_clients = clients.Select((IDnsClient c) => ((IDnsClient Client, byte Weight))(Client: c, Weight: 0)).ToArray();
	}

	public async Task<DnsMessage> Query(DnsMessage query, CancellationToken ct = default(CancellationToken))
	{
		(IDnsClient Client, byte Weight)[] candidates;
		lock (_lock)
		{
			candidates = _clients.OrderByDescending(((IDnsClient Client, byte Weight) x) => x.Weight).Take(_concurrentCount).ToArray();
		}
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		Task<DnsMessage>[] tasks = new Task<DnsMessage>[candidates.Length];
		for (int num = 0; num < candidates.Length; num++)
		{
			Task<DnsMessage> task = candidates[num].Client.Query(query, cts.Token);
			task.Forget();
			tasks[num] = task;
		}
		Task<DnsMessage> task2 = await tasks.WhenAnySuccessAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (task2 == null)
		{
			throw new InvalidOperationException("All queries failed.");
		}
		IDnsClient item = candidates[Array.IndexOf(tasks, task2)].Client;
		if (_updateWeigh)
		{
			lock (_lock)
			{
				for (int num2 = 0; num2 < _clients.Length; num2++)
				{
					if (_clients[num2].Client == item)
					{
						if (_clients[num2].Weight < byte.MaxValue)
						{
							_clients[num2] = (Client: _clients[num2].Client, Weight: (byte)(_clients[num2].Weight + 1));
						}
						else
						{
							_updateWeigh = false;
						}
						break;
					}
				}
			}
		}
		cts.Cancel();
		return await task2.ConfigureAwait(continueOnCapturedContext: false);
	}

	public void Dispose()
	{
		(IDnsClient, byte)[] clients = _clients;
		for (int i = 0; i < clients.Length; i++)
		{
			((IDisposable)clients[i].Item1)?.Dispose();
		}
	}
}
