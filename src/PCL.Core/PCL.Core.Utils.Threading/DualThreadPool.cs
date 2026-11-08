using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

public class DualThreadPool
{
	private readonly TaskFactory _ioFactory;

	private readonly TaskFactory _cpuFactory;

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	public int MaxThread { get; }

	public DualThreadPool(int maxThread)
	{
		if (maxThread < 1)
		{
			throw new ArgumentOutOfRangeException("maxThread");
		}
		MaxThread = maxThread;
		LimitedConcurrencyLevelTaskScheduler scheduler = new LimitedConcurrencyLevelTaskScheduler(maxThread);
		LimitedConcurrencyLevelTaskScheduler scheduler2 = new LimitedConcurrencyLevelTaskScheduler(maxThread);
		CancellationToken token = _cts.Token;
		_ioFactory = new TaskFactory(token, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, scheduler);
		_cpuFactory = new TaskFactory(token, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, scheduler2);
	}

	public Task QueueIo(Action work)
	{
		return _ioFactory.StartNew(work);
	}

	public Task QueueIo(Func<Task> work)
	{
		return _ioFactory.StartNew(work).Unwrap();
	}

	public Task QueueCpu(Action work)
	{
		return _cpuFactory.StartNew(work);
	}

	public Task QueueCpu(Func<Task> work)
	{
		return _cpuFactory.StartNew(work).Unwrap();
	}

	public void CancelAll()
	{
		_cts.Cancel();
	}
}
