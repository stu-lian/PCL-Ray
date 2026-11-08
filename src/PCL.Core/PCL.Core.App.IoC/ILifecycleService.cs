using System.Threading.Tasks;

namespace PCL.Core.App.IoC;

public interface ILifecycleService
{
	string Identifier { get; }

	string Name { get; }

	bool SupportAsync { get; }

	Task StartAsync();

	Task StopAsync();
}
