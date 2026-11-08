using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

public sealed class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
	[ThreadStatic]
	private static bool _currentThreadIsProcessingItems;

	private readonly LinkedList<Task> _tasks = new LinkedList<Task>();

	private int _delegatesQueuedOrRunning;

	private readonly int _maxDegreeOfParallelism;

	public override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

	public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
	{
		if (maxDegreeOfParallelism < 1)
		{
			throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
		}
		_maxDegreeOfParallelism = maxDegreeOfParallelism;
	}

	protected override IEnumerable<Task> GetScheduledTasks()
	{
		bool lockTaken = false;
		try
		{
			Monitor.TryEnter(_tasks, ref lockTaken);
			if (lockTaken)
			{
				return _tasks.ToArray();
			}
			throw new NotSupportedException();
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(_tasks);
			}
		}
	}

	protected override void QueueTask(Task task)
	{
		lock (_tasks)
		{
			_tasks.AddLast(task);
			if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
			{
				_delegatesQueuedOrRunning++;
				ThreadPool.UnsafeQueueUserWorkItem((object? _) =>
				{
					_ProcessTasks();
				}, null);
			}
		}
	}

	private void _ProcessTasks()
	{
		_currentThreadIsProcessingItems = true;
		try
		{
			while (true)
			{
				Task value;
				lock (_tasks)
				{
					if (_tasks.Count == 0)
					{
						_delegatesQueuedOrRunning--;
						break;
					}
					value = _tasks.First.Value;
					_tasks.RemoveFirst();
				}
				TryExecuteTask(value);
			}
		}
		finally
		{
			_currentThreadIsProcessingItems = false;
		}
	}

	protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
	{
		if (!_currentThreadIsProcessingItems)
		{
			return false;
		}
		if (taskWasPreviouslyQueued)
		{
			if (TryDequeue(task))
			{
				return TryExecuteTask(task);
			}
			return false;
		}
		return TryExecuteTask(task);
	}

	protected override bool TryDequeue(Task task)
	{
		lock (_tasks)
		{
			return _tasks.Remove(task);
		}
	}
}
