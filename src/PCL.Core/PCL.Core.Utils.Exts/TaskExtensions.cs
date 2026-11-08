using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Exts;

public static class TaskExtensions
{
	extension(Task task)
	{
		public void Forget()
		{
			task.ContinueWith((Task t) => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
		}
	}

	public static async Task<Task<T>> WhenAnySuccessAsync<T>(this IEnumerable<Task<T>> tasks)
	{
		ArgumentNullException.ThrowIfNull(tasks, "tasks");
		List<Task<T>> taskList = tasks.ToList();
		if (taskList.Count == 0)
		{
			throw new ArgumentException("Task collection is empty.", "tasks");
		}
		HashSet<Task<T>> remaining = new HashSet<Task<T>>(taskList);
		while (remaining.Count > 0)
		{
			Task<T> task = await Task.WhenAny(remaining);
			remaining.Remove(task);
			if (task.IsCompletedSuccessfully)
			{
				return task;
			}
		}
		List<Exception> list = taskList.Where((Task<T> t) => t.IsFaulted).SelectMany((Task<T> t) =>
		{
			IEnumerable<Exception> enumerable = t.Exception?.Flatten().InnerExceptions;
			return enumerable ?? Enumerable.Empty<Exception>();
		}).ToList();
		if (list.Count > 0)
		{
			throw new AggregateException("All connection attempts failed.", list);
		}
		if (taskList.Any((Task<T> t) => t.IsCanceled))
		{
			throw new OperationCanceledException("All connection attempts were canceled.");
		}
		throw new InvalidOperationException("All tasks completed but none succeeded, failed, or canceled.");
	}
}
