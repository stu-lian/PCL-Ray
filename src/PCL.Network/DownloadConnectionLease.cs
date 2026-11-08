using System;
using System.Threading;

namespace PCL.Network;

internal sealed class DownloadConnectionLease(DownloadQuotaLease globalLease, DownloadQuotaLease hostLease, HostQuotaEntry hostEntry) : IDisposable
{
	private DownloadQuotaLease? _globalLease = globalLease;

	private DownloadQuotaLease? _hostLease = hostLease;

	private HostQuotaEntry? _hostEntry = hostEntry;

	public void Dispose()
	{
		DownloadQuotaLease downloadQuotaLease = Interlocked.Exchange(ref _globalLease, null);
		if (downloadQuotaLease != null)
		{
			DownloadResourceManager.ReleaseConnection();
			downloadQuotaLease.Dispose();
			Interlocked.Exchange(ref _hostLease, null)?.Dispose();
			HostQuotaEntry hostQuotaEntry = Interlocked.Exchange(ref _hostEntry, null);
			if (hostQuotaEntry != null)
			{
				DownloadResourceManager.ReleaseHostQuotaEntry(hostQuotaEntry);
			}
		}
	}
}
