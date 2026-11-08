using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;

public interface ITask
{
	string Title { get; }

	event TaskStateEvent StateChanged;

	Task ExecuteAsync(CancellationToken cancelToken = default(CancellationToken));
}
