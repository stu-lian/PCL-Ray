using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;

namespace PCL.Core.Utils.OS;

public class RegistryChangeMonitor : IDisposable
{
	private const int REG_NOTIFY_CHANGE_LAST_SET = 4;

	private const int KEY_NOTIFY = 16;

	private const int KEY_QUERY_VALUE = 1;

	private const int KEY_READ = 17;

	private const nuint HKEY_CURRENT_USER = 2147483649u;

	private readonly nint _hKey;

	private readonly ManualResetEvent _stopEvent = new ManualResetEvent(initialState: false);

	private readonly ManualResetEvent _registryEvent = new ManualResetEvent(initialState: false);

	private readonly Thread _monitorThread;

	public event EventHandler? Changed;

	[SkipLocalsInit]
	private unsafe static int _RegOpenKeyEx(nuint hKey, string subKey, uint options, int samDesired, out nint phkResult)
	{
		phkResult = 0;
		int result;
		int lastSystemError;
		fixed (nint* _phkResult_native = &phkResult)
		{
			fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(subKey))
			{
				void* _subKey_native = ptr;
				Marshal.SetLastSystemError(0);
				result = __PInvoke(hKey, (ushort*)_subKey_native, options, samDesired, _phkResult_native);
				lastSystemError = Marshal.GetLastSystemError();
			}
		}
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("advapi32.dll", EntryPoint = "RegOpenKeyExW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nuint __hKey_native, ushort* __subKey_native, uint __options_native, int __samDesired_native, nint* __phkResult_native);
	}

	[SkipLocalsInit]
	private static int _RegNotifyChangeKeyValue(nint hKey, [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree, int dwNotifyFilter, nint hEvent, [MarshalAs(UnmanagedType.Bool)] bool fAsynchronous)
	{
		int _fAsynchronous_native = (fAsynchronous ? 1 : 0);
		int _bWatchSubtree_native = (bWatchSubtree ? 1 : 0);
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(hKey, _bWatchSubtree_native, dwNotifyFilter, hEvent, _fAsynchronous_native);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("advapi32.dll", EntryPoint = "RegNotifyChangeKeyValue", ExactSpelling = true)]
		static extern int __PInvoke(nint __hKey_native, int __bWatchSubtree_native, int __dwNotifyFilter_native, nint __hEvent_native, int __fAsynchronous_native);
	}

	[SkipLocalsInit]
	private static int _RegCloseKey(nint hKey)
	{
		Marshal.SetLastSystemError(0);
		int result = __PInvoke(hKey);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("advapi32.dll", EntryPoint = "RegCloseKey", ExactSpelling = true)]
		static extern int __PInvoke(nint __hKey_native);
	}

	public RegistryChangeMonitor(string keyPath)
	{
		int num = _RegOpenKeyEx(2147483649u, keyPath, 0u, 17, out _hKey);
		if (num != 0)
		{
			throw new Win32Exception(num);
		}
		_monitorThread = new Thread(_MonitorThread)
		{
			IsBackground = true
		};
		_monitorThread.Start();
	}

	private void _MonitorThread()
	{
		try
		{
			_RegisterForNotification();
			while (!_stopEvent.WaitOne(0))
			{
				switch (WaitHandle.WaitAny(new WaitHandle[2] { _registryEvent, _stopEvent }, TimeSpan.FromSeconds(1L)))
				{
				case 1:
					return;
				case 0:
					_registryEvent.Reset();
					Changed?.Invoke(this, EventArgs.Empty);
					_RegisterForNotification();
					break;
				}
			}
		}
		finally
		{
			_registryEvent.Dispose();
		}
	}

	private void _RegisterForNotification()
	{
		int num = _RegNotifyChangeKeyValue(_hKey, bWatchSubtree: true, 4, _registryEvent.SafeWaitHandle.DangerousGetHandle(), fAsynchronous: true);
		if (num != 0)
		{
			_stopEvent.Set();
			throw new Win32Exception(num);
		}
	}

	public void Dispose()
	{
		_stopEvent.Set();
		Thread monitorThread = _monitorThread;
		if (monitorThread != null && monitorThread.IsAlive)
		{
			_monitorThread.Join(1000);
		}
		if (_hKey != IntPtr.Zero)
		{
			_RegCloseKey(_hKey);
		}
		_stopEvent.Dispose();
		GC.SuppressFinalize(this);
	}
}
