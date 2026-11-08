using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Storage.Cache.Model;
using PCL.Core.Logging;

namespace PCL.Core.IO.Storage.Cache;

internal class CacheEvictionService(SqliteCacheStorage db, FileCacheStorage files, CacheOptions options)
{
	private CancellationTokenSource? _cts;

	private Task? _loop;

	private readonly object _startLock = new object();

	public void Start()
	{
		lock (_startLock)
		{
			if (_loop == null)
			{
				_cts = new CancellationTokenSource();
				_loop = Task.Run(() => _EvictionLoopAsync(_cts.Token));
			}
		}
	}

	public void Stop()
	{
		lock (_startLock)
		{
			if (_cts != null)
			{
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
			_loop = null;
		}
	}

	public void CheckThreshold()
	{
	}

	private async Task _EvictionLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(options.EvictionInterval, ct).ConfigureAwait(continueOnCapturedContext: false);
				await db.DeleteExpiredAsync(DateTime.UtcNow, ct).ConfigureAwait(continueOnCapturedContext: false);
				long num = (await db.GetStatsAsync(ct).ConfigureAwait(continueOnCapturedContext: false)).TotalSizeBytes - (options.MaxCacheSize - options.ReserveBytes);
				if (num > 0)
				{
					await _EvictAsync(num, ct).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex2)
			{
				LogWrapper.Warn(ex2, "CacheEviction", "An error occurred while evicting cache entries:");
			}
		}
	}

	private async Task _EvictAsync(long targetBytes, CancellationToken ct)
	{
		long freed = 0L;
		while (freed < targetBytes && !ct.IsCancellationRequested)
		{
			List<EvictionCandidate> list = await db.GetEvictionCandidatesAsync(50, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (list.Count == 0)
			{
				break;
			}
			foreach (EvictionCandidate can in list)
			{
				if (ct.IsCancellationRequested)
				{
					return;
				}
				await db.DeleteAsync(can.CacheKey, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (can.EntryType == EntryType.FileRef && can.FileHash != null)
				{
					await files.ForceDeleteAsync(can.FileHash).ConfigureAwait(continueOnCapturedContext: false);
				}
				freed += can.DataSize;
			}
		}
	}
}
