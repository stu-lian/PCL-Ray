using System;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils.WinRT;

public class HStringHelper
{
	public unsafe static HString ToHString(string? value)
	{
		if (value == null)
		{
			return new HString(IntPtr.Zero);
		}
		nint value2 = default;
		fixed (char* sourceString = value)
		{
			Marshal.ThrowExceptionForHR(WinRTInterop.WindowsCreateString((ushort*)sourceString, value.Length, &value2));
		}
		return new HString(value2);
	}

	public unsafe static string ToManagedString(HString value)
	{
		if ((nint)value == IntPtr.Zero)
		{
			return "";
		}
		uint num = default;
		char* value2 = WinRTInterop.WindowsGetStringRawBuffer((nint)value, &num);
		if (num == 0)
		{
			return string.Empty;
		}
		return new string(value2, 0, (int)num);
	}

	public static void DeleteHString(HString value)
	{
		if (!value.IsNull)
		{
			WinRTInterop.WindowsDeleteString((nint)value);
		}
	}

	public unsafe static void WithReference(ReadOnlySpan<char> value, Action<HString> action)
	{
		fixed (char* sourceString = value)
		{
			WinRTInterop.HStringHeader hStringHeader = default;
			nint num = default;
			Marshal.ThrowExceptionForHR(WinRTInterop.WindowsCreateStringReference((ushort*)sourceString, value.Length, (nint*)(&hStringHeader), &num));
			action((HString)num);
		}
	}
}
