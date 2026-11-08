using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils.OS;

public static class WindowInterop
{
	private struct MARGINS
	{
		public int leftWidth;

		public int rightWidth;

		public int topHeight;

		public int bottomHeight;
	}

	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private enum MONITOR_DPI_TYPE
	{
		MDT_EFFECTIVE_DPI = 0,
		MDT_ANGULAR_DPI = 1,
		MDT_RAW_DPI = 2,
		MDT_DEFAULT = MDT_EFFECTIVE_DPI
	}

	private const int MONITOR_DEFAULTTOPRIMARY = 1;

	[SkipLocalsInit]
	private unsafe static int DwmExtendFrameIntoClientArea(nint hWnd, ref MARGINS pMarInset)
	{
		int result;
		fixed (MARGINS* _pMarInset_native = &pMarInset)
		{
			result = __PInvoke(hWnd, _pMarInset_native);
		}
		return result;
		[DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hWnd_native, MARGINS* __pMarInset_native);
	}

	[SkipLocalsInit]
	private unsafe static int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool pfEnabled)
	{
		pfEnabled = false;
		Unsafe.SkipInit(out int num);
		int result = __PInvoke(&num);
		pfEnabled = num != 0;
		return result;
		[DllImport("dwmapi.dll", EntryPoint = "DwmIsCompositionEnabled", ExactSpelling = true)]
		unsafe static extern int __PInvoke(int* __pfEnabled_native);
	}

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool GetWindowRect(nint hWnd, out RECT lpRect)
	{
		lpRect = default;
		int num;
		fixed (RECT* _lpRect_native = &lpRect)
		{
			num = __PInvoke(hWnd, _lpRect_native);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "GetWindowRect", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hWnd_native, RECT* __lpRect_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	private static extern nint MonitorFromWindow(nint hWnd, uint dwFlags);

	[SkipLocalsInit]
	private unsafe static int GetDpiForMonitor(nint hMonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY)
	{
		dpiX = 0u;
		dpiY = 0u;
		int result;
		fixed (uint* _dpiY_native = &dpiY)
		{
			fixed (uint* _dpiX_native = &dpiX)
			{
				result = __PInvoke(hMonitor, dpiType, _dpiX_native, _dpiY_native);
			}
		}
		return result;
		[DllImport("shcore.dll", EntryPoint = "GetDpiForMonitor", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hMonitor_native, MONITOR_DPI_TYPE __dpiType_native, uint* __dpiX_native, uint* __dpiY_native);
	}

	public static bool IsCompositionEnabled()
	{
		int num = DwmIsCompositionEnabled(out var pfEnabled);
		if (num == 0)
		{
			return pfEnabled;
		}
		throw new Win32Exception(num, "Failed to check DWM status");
	}

	public static void ExtendFrameIntoClientArea(nint hWnd, int marginLeft, int marginTop, int marginRight, int marginBottom)
	{
		MARGINS pMarInset = new MARGINS
		{
			leftWidth = marginLeft,
			rightWidth = marginRight,
			topHeight = marginTop,
			bottomHeight = marginBottom
		};
		if (IsCompositionEnabled())
		{
			int num = DwmExtendFrameIntoClientArea(hWnd, ref pMarInset);
			if (num != 0)
			{
				throw new Win32Exception(num, "Failed to extend frame into client area");
			}
		}
	}

	public static void ExtendFrameIntoClientArea(nint hWnd, int margin)
	{
		ExtendFrameIntoClientArea(hWnd, margin, margin, margin, margin);
	}

	public static (int Left, int Top, int Right, int Bottom) GetWindowRectangle(nint hWnd)
	{
		if (!GetWindowRect(hWnd, out var lpRect))
		{
			throw new Win32Exception("Failed to get window rectangle");
		}
		return (Left: lpRect.left, Top: lpRect.top, Right: lpRect.right, Bottom: lpRect.bottom);
	}

	public static (int X, int Y, int Width, int Height) ToWindowBounds(this (int Left, int Top, int Right, int Bottom) rect)
	{
		(int Left, int Top, int Right, int Bottom) tuple = rect;
		int item = tuple.Left;
		int item2 = tuple.Top;
		int item3 = tuple.Right;
		int item4 = tuple.Bottom;
		int item5 = item;
		int item6 = item2;
		int item7 = item3 - item;
		int item8 = item4 - item2;
		return (X: item5, Y: item6, Width: item7, Height: item8);
	}

	public static int GetSystemDpi(nint hWnd = 0)
	{
		if (GetDpiForMonitor(MonitorFromWindow(hWnd, 1u), MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var _) == 0)
		{
			return (int)dpiX;
		}
		return 96;
	}
}
