using System.Threading;

namespace PCL.Network;

internal sealed class HostQuotaEntry(string host)
{
	private readonly Lock _lock = new Lock();

	private int _referenceCount;

	private bool _retired;

	public string Host { get; } = host;

	public AsyncQuota Quota { get; } = new AsyncQuota();

	public bool TryAddReference()
	{
		using (_lock.EnterScope())
		{
			if (_retired)
			{
				return false;
			}
			_referenceCount++;
			return true;
		}
	}

	public bool ReleaseReference()
	{
		using (_lock.EnterScope())
		{
			if (--_referenceCount != 0)
			{
				return false;
			}
			_retired = true;
			return true;
		}
	}
}
