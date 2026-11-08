using System;
using System.Threading;

namespace PCL.Network;

internal sealed class DownloadQuotaLease : IDisposable
{
	private AsyncQuota? _quota;

	private readonly long _amount;

	internal DownloadQuotaLease(AsyncQuota quota, long amount)
	{
		_quota = quota;
		_amount = amount;
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _quota, null)?.Release(_amount);
	}
}
