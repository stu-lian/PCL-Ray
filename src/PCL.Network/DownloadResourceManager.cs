using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Network;

internal static class DownloadResourceManager
{
	private static readonly AsyncQuota ConnectionQuota = new AsyncQuota();

	private static readonly AsyncQuota BufferQuota = new AsyncQuota();

	private static readonly ConcurrentDictionary<string, HostQuotaEntry> HostConnectionQuotas = new ConcurrentDictionary<string, HostQuotaEntry>(StringComparer.OrdinalIgnoreCase);

	private static readonly Lock SpeedLock = new Lock();

	private static int _activeConnectionCount;

	private static long _speedBytes;

	private static long _speedSnapshotTick = Stopwatch.GetTimestamp();

	private static long _speed;

	private const long ThrottleBurstBytes = 131072L;

	private const int MaxThrottleDelayMilliseconds = 2000;

	private static readonly Lock ThrottleScheduleLock = new Lock();

	private static double _throttleTokens;

	private static long _throttleLastRefillTikck;

	public static int ActiveConnectionCount => Volatile.Read(in _activeConnectionCount);

	public static long DownloadSpeed
	{
		get
		{
			using (SpeedLock.EnterScope())
			{
				long timestamp = Stopwatch.GetTimestamp();
				long num = timestamp - _speedSnapshotTick;
				if (num >= Stopwatch.Frequency / 4)
				{
					long num2 = Interlocked.Exchange(ref _speedBytes, 0L);
					_speed = ((num > 0) ? Math.Max(0L, (long)((double)num2 * (double)Stopwatch.Frequency / (double)num)) : 0);
					_speedSnapshotTick = timestamp;
				}
				return _speed;
			}
		}
	}

	internal static void RecordDownloadedBytes(long bytes)
	{
		if (bytes > 0)
		{
			Interlocked.Add(ref _speedBytes, bytes);
		}
	}

	public static async ValueTask<DownloadConnectionLease> AcquireConnectionAsync(string url, CancellationToken cancellationToken)
	{
		string host = (Uri.TryCreate(url, UriKind.Absolute, out Uri result) ? result.Host : url);
		HostQuotaEntry hostEntry = AcquireHostQuotaEntry(host);
		DownloadQuotaLease hostLease = null;
		try
		{
			hostLease = await hostEntry.Quota.AcquireAsync(1L, () => 256L, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			DownloadQuotaLease globalLease = await ConnectionQuota.AcquireAsync(1L, () => Math.Clamp(ModNet.NetTaskConnectionLimit, 1, 256), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			Interlocked.Increment(ref _activeConnectionCount);
			return new DownloadConnectionLease(globalLease, hostLease, hostEntry);
		}
		catch
		{
			hostLease?.Dispose();
			ReleaseHostQuotaEntry(hostEntry);
			throw;
		}
	}

	public static async ValueTask<DownloadBufferLease> RentBufferAsync(int minBytes, CancellationToken cancellationToken)
	{
		return new DownloadBufferLease(await BufferQuota.AcquireAsync(minBytes, () => 536870912L, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), ArrayPool<byte>.Shared.Rent(minBytes));
	}

	internal static void ReleaseConnection()
	{
		Interlocked.Decrement(ref _activeConnectionCount);
	}

	public static void OnConnectionLimitChanged()
	{
		ConnectionQuota.NotifyCapacityChanged();
	}

	public static async Task ThrottleAsync(int bytes, CancellationToken ct)
	{
		long limitHigh = ModNet.NetTaskSpeedLimitHigh;
		if (limitHigh <= 0 || bytes <= 0)
		{
			return;
		}
		long num3;
		using (ThrottleScheduleLock.EnterScope())
		{
			long timestamp = Stopwatch.GetTimestamp();
			if (_throttleLastRefillTikck == 0L)
			{
				_throttleLastRefillTikck = timestamp;
				_throttleTokens = 131072.0;
			}
			else
			{
				long num = timestamp - _throttleLastRefillTikck;
				if (num > 0)
				{
					double num2 = (double)num * (double)limitHigh / (double)Stopwatch.Frequency;
					_throttleTokens = Math.Min(131072.0, _throttleTokens + num2);
					_throttleLastRefillTikck = timestamp;
				}
			}
			_throttleTokens -= bytes;
			if (_throttleTokens >= 0.0)
			{
				return;
			}
			num3 = (long)Math.Ceiling((0.0 - _throttleTokens) * (double)Stopwatch.Frequency / (double)limitHigh);
		}
		if (num3 <= 0)
		{
			return;
		}
		long deadlineTick = Stopwatch.GetTimestamp() + num3;
		long maxChunkTicks = (long)Math.Ceiling(2000.0 * (double)Stopwatch.Frequency / 1000.0);
		try
		{
			while (true)
			{
				long netTaskSpeedLimitHigh = ModNet.NetTaskSpeedLimitHigh;
				if (netTaskSpeedLimitHigh <= 0)
				{
					break;
				}
				if (netTaskSpeedLimitHigh != limitHigh)
				{
					using (ThrottleScheduleLock.EnterScope())
					{
						limitHigh = ModNet.NetTaskSpeedLimitHigh;
						if (limitHigh <= 0 || !(_throttleTokens < 0.0))
						{
							break;
						}
						long timestamp2 = Stopwatch.GetTimestamp();
						long num4 = (long)Math.Ceiling((0.0 - _throttleTokens) * (double)Stopwatch.Frequency / (double)limitHigh);
						deadlineTick = timestamp2 + num4;
					}
				}
				long num5 = deadlineTick - Stopwatch.GetTimestamp();
				if (num5 > 0)
				{
					long num6 = (long)Math.Ceiling((double)Math.Min(num5, maxChunkTicks) * 1000.0 / (double)Stopwatch.Frequency);
					if (num6 <= 0)
					{
						num6 = 1L;
					}
					await Task.Delay(TimeSpan.FromMilliseconds(num6), ct).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				break;
			}
		}
		catch (OperationCanceledException)
		{
			using (ThrottleScheduleLock.EnterScope())
			{
				_throttleTokens = Math.Min(131072.0, _throttleTokens + (double)bytes);
			}
			throw;
		}
	}

	private static HostQuotaEntry AcquireHostQuotaEntry(string host)
	{
		HostQuotaEntry orAdd;
		while (true)
		{
			orAdd = HostConnectionQuotas.GetOrAdd(host, (string key) => new HostQuotaEntry(key));
			HostQuotaEntry value2;
			if (orAdd.TryAddReference())
			{
				if (HostConnectionQuotas.TryGetValue(host, out HostQuotaEntry value) && orAdd == value)
				{
					break;
				}
				ReleaseHostQuotaEntry(orAdd);
			}
			else if (HostConnectionQuotas.TryGetValue(host, out value2) && orAdd == value2)
			{
				((ICollection<KeyValuePair<string, HostQuotaEntry>>)HostConnectionQuotas).Remove(new KeyValuePair<string, HostQuotaEntry>(host, orAdd));
			}
		}
		return orAdd;
	}

	internal static void ReleaseHostQuotaEntry(HostQuotaEntry entry)
	{
		if (entry.ReleaseReference())
		{
			((ICollection<KeyValuePair<string, HostQuotaEntry>>)HostConnectionQuotas).Remove(new KeyValuePair<string, HostQuotaEntry>(entry.Host, entry));
		}
	}
}
