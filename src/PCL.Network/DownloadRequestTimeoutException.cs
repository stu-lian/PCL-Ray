using System;

namespace PCL.Network;

internal sealed class DownloadRequestTimeoutException : TimeoutException
{
	public DownloadRequestTimeoutException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
