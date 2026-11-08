using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

public sealed class AsyncCountResetEvent : IDisposable
{
	private readonly Queue<TaskCompletionSource<bool>> _waiters = new Queue<TaskCompletionSource<bool>>();

	private readonly object _lock = new object();

	private int _permits;

	private bool _disposed;

	~AsyncCountResetEvent()
	{
		Dispose();
	}

	public Task WaitAsync()
	{
		lock (_lock)
		{
			ObjectDisposedException.ThrowIf(_disposed, "AsyncCountResetEvent");
			if (_permits > 0)
			{
				_permits--;
				return Task.CompletedTask;
			}
			TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			_waiters.Enqueue(taskCompletionSource);
			return taskCompletionSource.Task;
		}
	}

	public void Set(int count = 1)
	{
		if (count <= 0)
		{
			return;
		}
		lock (_lock)
		{
			if (!_disposed)
			{
				while (count > 0 && _waiters.Count > 0)
				{
					_waiters.Dequeue().TrySetResult(result: true);
					count--;
				}
				_permits += count;
			}
		}
	}

	public void Dispose()
	{
		lock (_lock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			while (_waiters.Count > 0)
			{
				_waiters.Dequeue().TrySetException(new ObjectDisposedException("AsyncCountResetEvent"));
			}
		}
		GC.SuppressFinalize(this);
	}
}
