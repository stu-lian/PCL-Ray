using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Windows;
using System.Windows.Interop;

namespace PCL.Core.Utils.OS;

public class DragHelper
{
	private struct DragPoint
	{
		public int X;

		public int Y;
	}

	private struct CHANGEFILTERSTRUCT
	{
		public uint cbSize;

		public uint ExtStatus;
	}

	private const uint WM_COPYGLOBALDATA = 73u;

	private const uint WM_COPYDATA = 74u;

	private const uint WM_DROPFILES = 563u;

	private const uint MSGFLT_ALLOW = 1u;

	private const uint MSGFLT_ADD = 1u;

	public string[]? DropFilePaths { get; private set; }

	public Point DropDragPoint
	{
		[CompilerGenerated]
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return field;
		}
		[CompilerGenerated]
		private set
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			field = value;
		}
	}

	public HwndSource? HwndSource { get; set; }

	public event EventHandler? DragDrop;

	public void AddHook()
	{
		if (HwndSource == null)
		{
			throw new InvalidOperationException("HwndSource 未设置");
		}
		RemoveHook();
		HwndSource.AddHook(WndProc);
		nint handle = HwndSource.Handle;
		if (IsUserAnAdmin())
		{
			RevokeDragDrop(handle);
		}
		DragAcceptFiles(handle, accept: true);
		ChangeMessageFilter(handle);
	}

	public void RemoveHook()
	{
		if (HwndSource != null)
		{
			HwndSource.RemoveHook(WndProc);
			DragAcceptFiles(HwndSource.Handle, accept: false);
		}
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (TryGetDropInfo(msg, wParam, out string[] filePaths, out DragPoint dropPoint))
		{
			DropFilePaths = filePaths;
			DropDragPoint = new Point((double)dropPoint.X, (double)dropPoint.Y);
			DragDrop?.Invoke(this, EventArgs.Empty);
			handled = true;
		}
		return IntPtr.Zero;
	}

	private unsafe static void ChangeMessageFilter(nint hwnd)
	{
		Version version = Environment.OSVersion.Version;
		if (version < new Version(6, 0))
		{
			return;
		}
		bool flag = version >= new Version(6, 1);
		CHANGEFILTERSTRUCT filter = new CHANGEFILTERSTRUCT
		{
			cbSize = (uint)sizeof(CHANGEFILTERSTRUCT)
		};
		uint[] array = new uint[3] { 563u, 73u, 74u };
		foreach (uint msg in array)
		{
			if (!(flag ? ChangeWindowMessageFilterEx(hwnd, msg, 1u, ref filter) : ChangeWindowMessageFilter(msg, 1u)))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
	}

	private static bool TryGetDropInfo(int msg, nint hDrop, out string[]? filePaths, out DragPoint dropPoint)
	{
		filePaths = null;
		dropPoint = default;
		if ((long)msg != 563)
		{
			return false;
		}
		uint num = DragQueryFile(hDrop, uint.MaxValue, IntPtr.Zero, 0u);
		filePaths = new string[num];
		Span<char> span = stackalloc char[1024];
		for (uint num2 = 0u; num2 < num; num2++)
		{
			uint num3 = DragQueryFile(hDrop, num2, IntPtr.Zero, 0u) + 1;
			if (num3 > 32768)
			{
				num3 = 32768u;
			}
			Span<char> lpszFile = ((num3 <= 1024) ? span.Slice(0, (int)num3) : ((Span<char>)new char[num3]));
			DragQueryFile(hDrop, num2, lpszFile, num3);
			filePaths[num2] = new string(lpszFile.Slice(0, (int)(num3 - 1)));
		}
		DragFinish(hDrop);
		return true;
	}

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool ChangeWindowMessageFilter(uint msg, uint flags)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(msg, flags);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "ChangeWindowMessageFilter", ExactSpelling = true)]
		static extern int __PInvoke(uint __msg_native, uint __flags_native);
	}

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool ChangeWindowMessageFilterEx(nint hwnd, uint msg, uint action, ref CHANGEFILTERSTRUCT filter)
	{
		int num;
		int lastSystemError;
		fixed (CHANGEFILTERSTRUCT* _filter_native = &filter)
		{
			Marshal.SetLastSystemError(0);
			num = __PInvoke(hwnd, msg, action, _filter_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "ChangeWindowMessageFilterEx", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hwnd_native, uint __msg_native, uint __action_native, CHANGEFILTERSTRUCT* __filter_native);
	}

	[SkipLocalsInit]
	private static void DragAcceptFiles(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool accept)
	{
		int _accept_native = (accept ? 1 : 0);
		__PInvoke(hwnd, _accept_native);
		[DllImport("shell32.dll", EntryPoint = "DragAcceptFiles", ExactSpelling = true)]
		static extern void __PInvoke(nint __hwnd_native, int __accept_native);
	}

	[SkipLocalsInit]
	private unsafe static uint DragQueryFile(nint hDrop, uint iFile, Span<char> lpszFile, uint cch)
	{
		uint result;
		fixed (char* ptr = &SpanMarshaller<char, char>.ManagedToUnmanagedIn.GetPinnableReference(lpszFile))
		{
			void* _lpszFile_native = ptr;
			result = __PInvoke(hDrop, iFile, (char*)_lpszFile_native, cch);
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "DragQueryFileW", ExactSpelling = true)]
		unsafe static extern uint __PInvoke(nint __hDrop_native, uint __iFile_native, char* __lpszFile_native, uint __cch_native);
	}

	[DllImport("shell32.dll", EntryPoint = "DragQueryFileW", ExactSpelling = true)]
	private static extern uint DragQueryFile(nint hDrop, uint iFile, nint lpszFile, uint cch);

	[DllImport("shell32.dll", ExactSpelling = true)]
	private static extern void DragFinish(nint hDrop);

	[DllImport("ole32.dll", ExactSpelling = true)]
	private static extern int RevokeDragDrop(nint hwnd);

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool IsUserAnAdmin()
	{
		return __PInvoke() != 0;
		[DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin", ExactSpelling = true)]
		static extern int __PInvoke();
	}
}
