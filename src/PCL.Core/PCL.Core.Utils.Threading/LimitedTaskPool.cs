using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

public class LimitedTaskPool
{
	private readonly TaskFactory _factory;

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	public int MaxThread { get; }

	public LimitedTaskPool(int maxThread)
	{
		if (maxThread < 1)
		{
			throw new ArgumentOutOfRangeException("maxThread");
		}
		MaxThread = maxThread;
		LimitedConcurrencyLevelTaskScheduler scheduler = new LimitedConcurrencyLevelTaskScheduler(maxThread);
		CancellationToken token = _cts.Token;
		_factory = new TaskFactory(token, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, scheduler);
	}

	public Task Submit(Action work)
	{
		return _factory.StartNew(work);
	}

	public Task Submit(Func<Task> work)
	{
		return _factory.StartNew(work).Unwrap();
	}

	public void CancelAll()
	{
		_cts.Cancel();
	}
}
