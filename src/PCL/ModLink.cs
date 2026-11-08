using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.McPing;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Utils.OS;

namespace PCL;

public static class ModLink
{
	public class PortFinder
	{
		public struct MIB_TCPROW_OWNER_PID
		{
			public int dwState;
			public int dwLocalAddr;
			public int dwLocalPort;
			public int dwRemoteAddr;
			public int dwRemotePort;
			public int dwOwningPid;
		}

		[DllImport("iphlpapi.dll", SetLastError = true)]
		public static extern int GetExtendedTcpTable(nint pTcpTable, ref int dwOutBufLen, bool bOrder, int ulAf, int tableClass, int reserved);

		public static List<int> GetProcessPort(int dwProcessId)
		{
			List<int> list = new List<int>();
			if (dwProcessId == 0)
			{
				return list;
			}
			int dwOutBufLen = 0;
			int result = GetExtendedTcpTable(IntPtr.Zero, ref dwOutBufLen, true, 2, 3, 0);
			if (result != 0 && result != 122)
			{
				return list;
			}
			nint buffer = Marshal.AllocHGlobal(dwOutBufLen);
			try
			{
				if (GetExtendedTcpTable(buffer, ref dwOutBufLen, true, 2, 3, 0) != 0)
				{
					return list;
				}
				nint current = buffer;
				int count = Marshal.ReadInt32(current);
				current = IntPtr.Add(current, 4);
				for (int i = 0; i < count; i++)
				{
					MIB_TCPROW_OWNER_PID row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(current);
					if (row.dwOwningPid == dwProcessId)
					{
						list.Add((row.dwLocalPort >> 8) | ((row.dwLocalPort & 0xFF) << 8));
					}
					current = IntPtr.Add(current, Marshal.SizeOf<MIB_TCPROW_OWNER_PID>());
				}
				return list;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}
	}

	public static async Task<List<Tuple<int, McPingResult, string>>> MCInstanceFindingAsync()
	{
		List<string> pids = new List<string>();
		foreach (string name in new[] { "java", "javaw" })
		{
			Process[] processes = Process.GetProcessesByName(name);
			foreach (Process process in processes)
			{
				pids.Add(process.Id.ToString());
			}
		}
		List<Tuple<int, McPingResult, string>> result = new List<Tuple<int, McPingResult, string>>();
		if (pids.Count == 0)
		{
			return result;
		}
		List<Tuple<int, int>> ports = new List<Tuple<int, int>>();
		foreach (string pid in pids)
		{
			foreach (int port in PortFinder.GetProcessPort(int.Parse(pid)))
			{
				ports.Add(new Tuple<int, int>(port, int.Parse(pid)));
			}
		}
		await Task.WhenAll(ports.Select(lookup => Task.Run(async () =>
		{
			IMcPingService service = McPingServiceFactory.CreateService("127.0.0.1", lookup.Item1, 3000);
			try
			{
				McPingResult ping = await service.PingAsync(default(CancellationToken));
				if (!string.IsNullOrWhiteSpace(ping?.Version.Name))
				{
					lock (result)
					{
						result.Add(new Tuple<int, McPingResult, string>(lookup.Item1, ping, GetLauncherBrand(lookup.Item2)));
					}
				}
			}
			catch
			{
			}
			finally
			{
				service.Dispose();
			}
		})).ToArray());
		return result;
	}

	public static string GetLauncherBrand(int pid)
	{
		try
		{
			string commandLine = ProcessInterop.GetCommandLine(pid);
			if (commandLine.Contains("-Dminecraft.launcher.brand="))
			{
				return commandLine.AfterFirst("-Dminecraft.launcher.brand=").BeforeFirst("-").TrimEnd('\'', ' ');
			}
			return commandLine.AfterFirst("--versionType ").BeforeFirst("-").TrimEnd('\'', ' ');
		}
		catch
		{
			return "";
		}
	}
}
