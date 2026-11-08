using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PCL.Core.UI.Animation.Clock;

public sealed class WinMMClock(int fps = 60) : IClock, IDisposable
{
	private delegate void TimeProc(uint id, uint msg, nint user, nint dw1, nint dw2);

	private uint _timerId;

	private long _frameIndex;

	private TimeProc? _callback;

	private const uint TimePeriodic = 1u;

	private const uint TimeCallbackFunction = 0u;

	public int Fps { get; set; } = fps;

	public bool IsRunning { get; private set; }

	public event EventHandler<long>? Tick;

	~WinMMClock()
	{
		Dispose();
	}

	public void Start()
	{
		if (!IsRunning)
		{
			IsRunning = true;
			_frameIndex = 0L;
			uint uDelay = (uint)Math.Max(1.0, 1000.0 / (double)Fps);
			_callback = (uint _, uint _, nint _, nint _, nint _) =>
			{
				_frameIndex++;
				Tick?.Invoke(this, _frameIndex);
			};
			_timerId = _TimeSetEvent(uDelay, 0u, _callback, IntPtr.Zero, 1u);
		}
	}

	public void Stop()
	{
		if (IsRunning)
		{
			IsRunning = false;
			if (_timerId != 0)
			{
				_TimeKillEvent(_timerId);
				_timerId = 0u;
			}
			_callback = null;
		}
	}

	public void Dispose()
	{
		Stop();
		GC.SuppressFinalize(this);
	}

	[SkipLocalsInit]
	private static uint _TimeSetEvent(uint uDelay, uint uResolution, TimeProc lpTimeProc, nint dwUser, uint fuEvent)
	{
		nint _lpTimeProc_native = ((lpTimeProc != null) ? Marshal.GetFunctionPointerForDelegate(lpTimeProc) : 0);
		Marshal.SetLastSystemError(0);
		uint result = __PInvoke(uDelay, uResolution, _lpTimeProc_native, dwUser, fuEvent);
		int lastSystemError = Marshal.GetLastSystemError();
		GC.KeepAlive(lpTimeProc);
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("winmm.dll", EntryPoint = "timeSetEvent", ExactSpelling = true)]
		static extern uint __PInvoke(uint __uDelay_native, uint __uResolution_native, nint __lpTimeProc_native, nint __dwUser_native, uint __fuEvent_native);
	}

	[SkipLocalsInit]
	private static void _TimeKillEvent(uint uTimerId)
	{
		Marshal.SetLastSystemError(0);
		__PInvoke(uTimerId);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		[DllImport("winmm.dll", EntryPoint = "timeKillEvent", ExactSpelling = true)]
		static extern void __PInvoke(uint __uTimerId_native);
	}
}
