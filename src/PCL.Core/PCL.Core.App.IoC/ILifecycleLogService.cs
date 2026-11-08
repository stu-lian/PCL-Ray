namespace PCL.Core.App.IoC;

public interface ILifecycleLogService : ILifecycleService
{
	void OnLog(LifecycleLogItem item);
}
