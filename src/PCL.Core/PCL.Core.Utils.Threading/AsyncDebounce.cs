using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

public class AsyncDebounce(CancellationToken cancelToken = default(CancellationToken)) : IDisposable
{
	private Task? _currentTask;

	private Task? _worker;

	private CancellationTokenSource? _currentDelayCts;

	private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

	private readonly object _resetLock = new object();

	public required TimeSpan Delay { get; init; }

	public required Func<Task> ScheduledTask { get; init; }

	public bool IsCurrentTaskCompleted { get; private set; }

	public bool IsCurrentTaskRunning => _currentTask != null;

	public async Task ResetAsync()
	{
		IsCurrentTaskCompleted = false;
		Task currentTask;
		lock (_resetLock)
		{
			try
			{
				_currentDelayCts?.Cancel();
			}
			catch (ObjectDisposedException)
			{
			}
			_currentDelayCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
			CancellationTokenSource capturedCts = _currentDelayCts;
			currentTask = _currentTask;
			_worker = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(Delay, capturedCts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (OperationCanceledException)
				{
					return;
				}
				finally
				{
					capturedCts.Dispose();
				}
				if (_currentDelayCts != capturedCts || capturedCts.IsCancellationRequested || _cts.IsCancellationRequested)
				{
					return;
				}
				if (_currentTask != null)
				{
					await _currentTask.ConfigureAwait(continueOnCapturedContext: false);
				}
				Task task = ScheduledTask();
				lock (_resetLock)
				{
					_currentTask = task;
				}
				try
				{
					await task.ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					lock (_resetLock)
					{
						_currentTask = null;
						IsCurrentTaskCompleted = true;
					}
				}
			}, _cts.Token);
		}
		if (currentTask != null)
		{
			await currentTask.ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void Dispose()
	{
		_cts.Cancel();
		try
		{
			_worker?.Wait();
		}
		catch
		{
		}
		_currentDelayCts?.Dispose();
		_cts.Dispose();
		GC.SuppressFinalize(this);
	}
}
