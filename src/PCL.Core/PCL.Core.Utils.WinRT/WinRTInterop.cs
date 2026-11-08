using System;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils.WinRT;

public static class WinRTInterop
{
	internal struct HStringHeader
	{
		private unsafe fixed byte _data[24];
	}

	[DllImport("combase.dll", ExactSpelling = true)]
	private unsafe static extern int RoGetActivationFactory(nint activatableClassId, Guid* iid, nint* factory);

	[DllImport("combase.dll", ExactSpelling = true)]
	private unsafe static extern int RoActivateInstance(nint activatableClassId, nint* instance);

	public unsafe static nint ActivateInstance(ReadOnlySpan<char> activatableClassId)
	{
		nint zero = IntPtr.Zero;
		fixed (char* sourceString = activatableClassId)
		{
			HStringHeader hStringHeader = default;
			nint activatableClassId2 = default;
			Marshal.ThrowExceptionForHR(WindowsCreateStringReference((ushort*)sourceString, activatableClassId.Length, (nint*)(&hStringHeader), &activatableClassId2));
			Marshal.ThrowExceptionForHR(RoActivateInstance(activatableClassId2, &zero));
		}
		return zero;
	}

	public unsafe static nint GetActivationFactory(ReadOnlySpan<char> activatableClassId, Guid iid)
	{
		nint zero = IntPtr.Zero;
		fixed (char* sourceString = activatableClassId)
		{
			HStringHeader hStringHeader = default;
			nint activatableClassId2 = default;
			Marshal.ThrowExceptionForHR(WindowsCreateStringReference((ushort*)sourceString, activatableClassId.Length, (nint*)(&hStringHeader), &activatableClassId2));
			Marshal.ThrowExceptionForHR(RoGetActivationFactory(activatableClassId2, &iid, &zero));
		}
		return zero;
	}

	[DllImport("combase.dll", ExactSpelling = true)]
	internal unsafe static extern int WindowsCreateString(ushort* sourceString, int length, nint* hstring);

	[DllImport("combase.dll", ExactSpelling = true)]
	internal unsafe static extern int WindowsCreateStringReference(ushort* sourceString, int length, nint* hstringHeader, nint* hstring);

	[DllImport("combase.dll", ExactSpelling = true)]
	internal static extern int WindowsDeleteString(nint hstring);

	[DllImport("combase.dll", ExactSpelling = true)]
	internal unsafe static extern char* WindowsGetStringRawBuffer(nint hstring, uint* length);
}
