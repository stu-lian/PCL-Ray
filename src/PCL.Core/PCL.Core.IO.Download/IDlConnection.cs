using System.Threading.Tasks;

namespace PCL.Core.IO.Download;

public interface IDlConnection
{
	Task<NDlConnectionInfo> StartAsync(long beginOffset);

	Task StopAsync();

	Task<byte[]> ReadAsync(int length);
}
