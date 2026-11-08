using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Clock;

public class StopwatchClock(int fps = 60) : IClock, IDisposable
{
	private CancellationTokenSource? _cts;

	private long _lastStamp;

	private long _lastFrame;

	public int Fps { get; set; } = fps;

	public bool IsRunning
	{
		get
		{
			if (_cts != null)
			{
				return !_cts.IsCancellationRequested;
			}
			return false;
		}
	}

	public event EventHandler<long>? Tick;

	~StopwatchClock()
	{
		Dispose();
	}

	public void Start()
	{
		if (IsRunning)
		{
			return;
		}
		_cts = new CancellationTokenSource();
		Task.Run(() =>
		{
			_lastStamp = FrameUtils.NowStamp();
			while (!_cts.IsCancellationRequested)
			{
				if (Fps == int.MaxValue)
				{
					_lastFrame++;
					Tick?.Invoke(this, _lastFrame);
				}
				else
				{
					long num = FrameUtils.StampToFrameIndex(_lastStamp, Fps);
					if (num != _lastFrame)
					{
						_lastFrame = num;
						Tick?.Invoke(this, num);
					}
				}
			}
		}, _cts.Token);
	}

	public void Stop()
	{
		if (_cts != null)
		{
			_cts.Cancel();
			_cts = null;
		}
	}

	public void Dispose()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
	}
}
