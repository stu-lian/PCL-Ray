using System;
using System.Threading;
using System.Windows.Media;
using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Clock;

public class WpfCompositionTargetRenderingClock(int fps = 60) : IUIClock, IClock, IDisposable
{
	private CancellationTokenSource? _cts;

	private TimeSpan _lastTime = TimeSpan.Zero;

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

	~WpfCompositionTargetRenderingClock()
	{
		Dispose();
	}

	public void Start()
	{
		if (!IsRunning)
		{
			_cts = new CancellationTokenSource();
			CompositionTarget.Rendering += _OnCompositionTargetOnRendering;
		}
	}

	private void _OnCompositionTargetOnRendering(object? _, EventArgs args)
	{
		if (!(args is RenderingEventArgs e))
		{
			return;
		}
		if (_cts.IsCancellationRequested)
		{
			CompositionTarget.Rendering -= _OnCompositionTargetOnRendering;
			return;
		}
		if (Fps == int.MaxValue)
		{
			_lastFrame++;
			Tick?.Invoke(this, _lastFrame);
			return;
		}
		if (_lastTime == TimeSpan.Zero)
		{
			_lastTime = e.RenderingTime;
		}
		long num = FrameUtils.TimeSpanToFrameIndex(_lastTime, e.RenderingTime, Fps);
		if (num != _lastFrame)
		{
			_lastFrame = num;
			_lastTime = e.RenderingTime;
			Tick?.Invoke(this, num);
		}
	}

	public void Stop()
	{
		CompositionTarget.Rendering -= _OnCompositionTargetOnRendering;
		if (_cts != null)
		{
			_cts.Cancel();
			_cts = null;
		}
	}

	public void Dispose()
	{
		CompositionTarget.Rendering -= _OnCompositionTargetOnRendering;
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
	}
}
