using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils.OS;

public static class NtInterop
{
	[SkipLocalsInit]
	private unsafe static void RtlGetNtVersionNumbers(out int major, out int minor, out int build)
	{
		major = 0;
		minor = 0;
		build = 0;
		fixed (int* _build_native = &build)
		{
			fixed (int* _minor_native = &minor)
			{
				fixed (int* _major_native = &major)
				{
					__PInvoke(_major_native, _minor_native, _build_native);
				}
			}
		}
		[DllImport("ntdll.dll", EntryPoint = "RtlGetNtVersionNumbers", ExactSpelling = true)]
		unsafe static extern void __PInvoke(int* __major_native, int* __minor_native, int* __build_native);
	}

	private static void _ThrowLastWin32Error(int? errorCode = null)
	{
		throw new Win32Exception(errorCode ?? Marshal.GetLastWin32Error());
	}

	public static Version GetCurrentOsVersion()
	{
		RtlGetNtVersionNumbers(out var major, out var minor, out var build);
		build &= 0xFFFF;
		return new Version(major, minor, build);
	}
}
