namespace PCL.Core.App.Tasks;

public interface ITaskGroup : ITask
{
	event TaskGroupEvent AddTask;

	event TaskGroupEvent RemoveTask;
}
