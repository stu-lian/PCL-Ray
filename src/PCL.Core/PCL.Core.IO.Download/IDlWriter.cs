using System.IO;
using System.Threading.Tasks;

namespace PCL.Core.IO.Download;

public interface IDlWriter
{
	bool IsSupportParallel { get; }

	Task<Stream> CreateStreamAsync();

	Task StopAsync();

	Task FinishAsync();
}
