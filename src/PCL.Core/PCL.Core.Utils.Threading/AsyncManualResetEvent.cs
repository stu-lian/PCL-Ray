using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

public sealed class AsyncManualResetEvent : IDisposable
{
	private readonly object _syncLock = new object();

	private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly ManualResetEventSlim _mre = new ManualResetEventSlim(initialState: false);

	private bool _disposed;

	public bool IsSet
	{
		get
		{
			lock (_syncLock)
			{
				return _tcs.Task.IsCompleted;
			}
		}
	}

	public AsyncManualResetEvent(bool initialState = false)
	{
		if (initialState)
		{
			_tcs.SetResult(result: true);
			_mre.Set();
		}
	}

	public Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		TaskCompletionSource<bool> tcs;
		lock (_syncLock)
		{
			tcs = _tcs;
		}
		if (!cancellationToken.CanBeCanceled || tcs.Task.IsCompleted)
		{
			return tcs.Task;
		}
		return _WaitWithCancellationAsync(tcs.Task, cancellationToken);
	}

	private static async Task _WaitWithCancellationAsync(Task waitTask, CancellationToken ct)
	{
		TaskCompletionSource<bool> cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		using (ct.Register((object? s) =>
		{
			((TaskCompletionSource<bool>)s).TrySetResult(result: true);
		}, cancelTcs))
		{
			if (await Task.WhenAny(waitTask, cancelTcs.Task).ConfigureAwait(continueOnCapturedContext: false) == cancelTcs.Task)
			{
				ct.ThrowIfCancellationRequested();
			}
			await waitTask.ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void Wait()
	{
		_mre.Wait();
	}

	public bool Wait(int millisecondsTimeout)
	{
		return _mre.Wait(millisecondsTimeout);
	}

	public bool Wait(TimeSpan timeout)
	{
		return _mre.Wait(timeout);
	}

	public void Wait(CancellationToken cancellationToken)
	{
		_mre.Wait(cancellationToken);
	}

	public void Set()
	{
		lock (_syncLock)
		{
			_tcs.TrySetResult(result: true);
			_mre.Set();
		}
	}

	public void Reset()
	{
		lock (_syncLock)
		{
			if (_tcs.Task.IsCompleted)
			{
				_tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				_mre.Reset();
			}
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_mre.Dispose();
			_disposed = true;
		}
	}
}
