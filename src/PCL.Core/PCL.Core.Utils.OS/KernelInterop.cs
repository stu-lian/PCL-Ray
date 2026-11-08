using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils.OS;

public static class KernelInterop
{
	private enum LOGICAL_PROCESSOR_RELATIONSHIP : uint
	{
		RelationProcessorCore = 0u,
		RelationNumaNode = 1u,
		RelationCache = 2u,
		RelationProcessorPackage = 3u,
		RelationGroup = 4u,
		RelationAll = 65535u
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct MEMORYSTATUSEX
	{
		public uint dwLength;

		public uint dwMemoryLoad;

		public ulong ullTotalPhys;

		public ulong ullAvailPhys;

		public ulong ullTotalPageFile;

		public ulong ullAvailPageFile;

		public ulong ullTotalVirtual;

		public ulong ullAvailVirtual;

		public ulong ullAvailExtendedVirtual;
	}

	public sealed record ProcessorCore(byte EfficiencyClass, ulong Mask);

	private const int ERROR_INSUFFICIENT_BUFFER = 122;

	private const int ERROR_ACCESS_DENIED = 5;

	public static uint CurrentNativeThreadId => _GetCurrentThreadId();

	[SkipLocalsInit]
	private static uint _GetCurrentThreadId()
	{
		Marshal.SetLastSystemError(0);
		uint result = __PInvoke();
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
		static extern uint __PInvoke();
	}

	[DllImport("kernel32.dll", EntryPoint = "ExitProcess", ExactSpelling = true)]
	private static extern void _ExitProcess(uint statusCode);

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool _GetNamedPipeClientProcessId(nint pipeHandle, out uint clientProcessId)
	{
		clientProcessId = 0u;
		int num;
		int lastSystemError;
		fixed (uint* _clientProcessId_native = &clientProcessId)
		{
			Marshal.SetLastSystemError(0);
			num = __PInvoke(pipeHandle, _clientProcessId_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GetNamedPipeClientProcessId", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __pipeHandle_native, uint* __clientProcessId_native);
	}

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool _GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP relationshipType, nint buffer, ref uint returnLength)
	{
		int num;
		int lastSystemError;
		fixed (uint* _returnLength_native = &returnLength)
		{
			Marshal.SetLastSystemError(0);
			num = __PInvoke(relationshipType, buffer, _returnLength_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GetLogicalProcessorInformationEx", ExactSpelling = true)]
		unsafe static extern int __PInvoke(LOGICAL_PROCESSOR_RELATIONSHIP __relationshipType_native, nint __buffer_native, uint* __returnLength_native);
	}

	private static MEMORYSTATUSEX CreateStatus()
	{
		return new MEMORYSTATUSEX
		{
			dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
		};
	}

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer)
	{
		int num;
		int lastSystemError;
		fixed (MEMORYSTATUSEX* _lpBuffer_native = &lpBuffer)
		{
			Marshal.SetLastSystemError(0);
			num = __PInvoke(_lpBuffer_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx", ExactSpelling = true)]
		unsafe static extern int __PInvoke(MEMORYSTATUSEX* __lpBuffer_native);
	}

	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static bool _AllocConsole()
	{
		return __PInvoke() != 0;
		[DllImport("kernel32.dll", EntryPoint = "AllocConsole", ExactSpelling = true)]
		static extern int __PInvoke();
	}

	[DllImport("kernel32.dll", EntryPoint = "FreeConsole", ExactSpelling = true)]
	private static extern void _FreeConsole();

	[DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", ExactSpelling = true)]
	private static extern nint _GetConsoleWindow();

	[DllImport("kernel32.dll", ExactSpelling = true)]
	internal static extern uint GetACP();

	private static void _ThrowLastWin32Error(int? errorCode = null)
	{
		throw new Win32Exception(errorCode ?? Marshal.GetLastWin32Error());
	}

	public static void ExitProcess(int statusCode = 0)
	{
		_ExitProcess((uint)statusCode);
	}

	public static uint GetNamedPipeClientProcessId(nint pipeHandle)
	{
		if (!_GetNamedPipeClientProcessId(pipeHandle, out var clientProcessId))
		{
			_ThrowLastWin32Error();
		}
		return clientProcessId;
	}

	public static int GetPerformanceLogicalProcessorCount()
	{
		List<ProcessorCore> list = QueryProcessorCoreRelationships();
		if (list.Count == 0)
		{
			return Environment.ProcessorCount;
		}
		byte maxEff = list.Max((ProcessorCore c) => c.EfficiencyClass);
		return list.Where((ProcessorCore c) => c.EfficiencyClass == maxEff).Sum((ProcessorCore c) => CountSetBits(c.Mask));
		static int CountSetBits(ulong v)
		{
			int num = 0;
			while (v != 0L)
			{
				num += (int)(v & 1);
				v >>= 1;
			}
			return num;
		}
	}

	public static List<ProcessorCore> QueryProcessorCoreRelationships()
	{
		uint returnLength = 0u;
		if (!_GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, IntPtr.Zero, ref returnLength) && Marshal.GetLastWin32Error() != 122)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		List<ProcessorCore> list = new List<ProcessorCore>();
		nint num = Marshal.AllocHGlobal((int)returnLength);
		try
		{
			if (!_GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, num, ref returnLength))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			nint num2 = num;
			nint num3 = IntPtr.Add(num, (int)returnLength);
			int num4 = IntPtr.Size + 8;
			while (((IntPtr)num2).ToInt64() < ((IntPtr)num3).ToInt64())
			{
				int num5 = Marshal.ReadInt32(num2);
				uint offset = (uint)Marshal.ReadInt32(num2, 4);
				if (num5 == 0)
				{
					byte efficiencyClass = Marshal.ReadByte(num2, 9);
					ushort num6 = (ushort)Marshal.ReadInt16(num2, 30);
					nint pointer = IntPtr.Add(num2, 32);
					for (int i = 0; i < num6; i++)
					{
						nint ptr = IntPtr.Add(pointer, i * num4);
						ulong mask = (ulong)((IntPtr.Size == 8) ? Marshal.ReadInt64(ptr) : ((uint)Marshal.ReadInt32(ptr)));
						list.Add(new ProcessorCore(efficiencyClass, mask));
					}
				}
				num2 = IntPtr.Add(num2, (int)offset);
			}
			return list;
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}

	public static ulong GetAvailablePhysicalMemoryBytes()
	{
		MEMORYSTATUSEX lpBuffer = CreateStatus();
		if (!GlobalMemoryStatusEx(ref lpBuffer))
		{
			_ThrowLastWin32Error();
		}
		return lpBuffer.ullAvailPhys;
	}

	public static (ulong Total, ulong Available) GetPhysicalMemoryBytes()
	{
		MEMORYSTATUSEX lpBuffer = CreateStatus();
		if (!GlobalMemoryStatusEx(ref lpBuffer))
		{
			_ThrowLastWin32Error();
		}
		return (Total: lpBuffer.ullTotalPhys, Available: lpBuffer.ullAvailPhys);
	}

	public static double GetMemoryLoadPercent()
	{
		MEMORYSTATUSEX lpBuffer = CreateStatus();
		if (!GlobalMemoryStatusEx(ref lpBuffer))
		{
			_ThrowLastWin32Error();
		}
		return lpBuffer.dwMemoryLoad;
	}

	public static void AllocateConsole()
	{
		if (!_AllocConsole())
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error != 5)
			{
				_ThrowLastWin32Error(lastWin32Error);
			}
		}
	}

	public static void FreeConsole()
	{
		_FreeConsole();
	}

	public static nint GetConsoleWindow()
	{
		return _GetConsoleWindow();
	}
}
