using System.Threading.Tasks;

namespace PCL.Core.App.IoC;

public abstract class GeneralService : ILifecycleService
{
	public string Identifier { get; }

	public string Name { get; }

	public bool SupportAsync { get; }

	protected LifecycleContext ServiceContext { get; }

	protected GeneralService(string identifier, string name, bool asyncStart = true)
	{
		Identifier = identifier;
		Name = name;
		SupportAsync = asyncStart;
		ServiceContext = Lifecycle.GetContext(this);
	}

	public virtual void Start()
	{
	}

	public virtual void Stop()
	{
	}

	public Task StartAsync()
	{
		Start();
		return Task.CompletedTask;
	}

	public Task StopAsync()
	{
		Stop();
		return Task.CompletedTask;
	}
}
