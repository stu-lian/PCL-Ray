using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.McPing.Model;

namespace PCL.Core.Link.McPing;

public interface IMcPingService : IDisposable
{
	IPEndPoint Endpoint { get; }

	string Host { get; }

	int Timeout { get; }

	Task<McPingResult?> PingAsync(CancellationToken cancellationToken = default(CancellationToken));
}
