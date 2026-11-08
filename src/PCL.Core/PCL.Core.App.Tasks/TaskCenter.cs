using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.App.Tasks;

public static class TaskCenter
{
	private static readonly ConditionalWeakTable<ITask, TaskModel> _ModelMap = new ConditionalWeakTable<ITask, TaskModel>();

	public static ObservableCollection<TaskModel> Tasks { get; } = new ObservableCollection<TaskModel>();

	private static TaskModel _InitModel(ITask instance)
	{
		ITaskCancelable cancelable = instance as ITaskCancelable;
		ITaskPausable pausable = instance as ITaskPausable;
		ITaskProgressive taskProgressive = instance as ITaskProgressive;
		TaskModel model = new TaskModel
		{
			Title = instance.Title,
			SupportProgress = (taskProgressive != null),
			OnCancel = ((cancelable == null) ? null : ((Action)(() =>
			{
				cancelable.Cancel();
			}))),
			OnPause = ((pausable == null) ? null : ((Action)(() =>
			{
				pausable.Pause();
			})))
		};
		instance.StateChanged += (TaskState state, string message) =>
		{
			LogWrapper.Trace("TaskCenter", $"{instance.Title}: state changed ({state}): {message}");
			model.State = state;
			model.StateMessage = message;
		};
		if (taskProgressive != null)
		{
			taskProgressive.ProgressChanged += (double progress) =>
			{
				model.Progress = Math.Clamp(progress, 0.0, 1.0);
			};
		}
		if (instance is ITaskGroup taskGroup)
		{
			taskGroup.AddTask += (ITask task) =>
			{
				TaskModel taskModel = _InitModel(task);
				_ModelMap.Add(task, taskModel);
				model.Children.Add(taskModel);
			};
			taskGroup.RemoveTask += (ITask task) =>
			{
				if (_ModelMap.TryGetValue(task, out TaskModel value))
				{
					model.Children.Remove(value);
				}
			};
		}
		return model;
	}

	public static void Register(ITask instance, bool start = true)
	{
		TaskModel model = _InitModel(instance);
		Tasks.Add(model);
		if (!start)
		{
			return;
		}
		Task.Run(async () =>
		{
			try
			{
				await instance.ExecuteAsync();
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				LogWrapper.Warn(ex2, "TaskCenter", instance.Title + ": exception thrown");
				model.State = TaskState.Failed;
				model.StateMessage = ex2.Message;
			}
		});
	}

	public static void RemoveFinished()
	{
		foreach (TaskModel item in Tasks.Where((TaskModel x) => x.State > TaskState.Running).ToList())
		{
			Tasks.Remove(item);
		}
	}
}
