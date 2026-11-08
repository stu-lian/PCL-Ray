using System;
using System.Collections.Generic;
using System.Management;
using PCL.Core.Logging;

namespace PCL.Core.Utils.OS;

public static class HardwareInfo
{
	public record struct GPUInfo
	{
		public string Name { get; init; }

		public string DriverVersion { get; init; }

		public long Memory { get; set; }
	}

	private static readonly object _Lock = new object();

	public static string CPUName = "Unknown";

	public static long SystemMemorySize = (long)KernelInterop.GetPhysicalMemoryBytes().Total / 1024L / 1024;

	public static IReadOnlyList<GPUInfo> GPUs { get; private set; } = Array.Empty<GPUInfo>();

	public static void GetHardwareInfo()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected Obj, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected Obj, but got Unknown
		string text = null;
		try
		{
			ManagementObjectSearcher val = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");
			try
			{
				var enumerator = val.Get().GetEnumerator();
				try
				{
					if (enumerator.MoveNext())
					{
						text = ((ManagementBaseObject)(ManagementObject)enumerator.Current)["Name"]?.ToString()?.Trim();
					}
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "获取 CPU 信息时出错");
		}
		List<GPUInfo> list = new List<GPUInfo>();
		try
		{
			ManagementObjectSearcher val2 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");
			try
			{
				var enumerator = val2.Get().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ManagementObject val3 = (ManagementObject)enumerator.Current;
						object obj = ((ManagementBaseObject)val3)["AdapterRAM"];
						GPUInfo gPUInfo = new GPUInfo
						{
							Name = (((ManagementBaseObject)val3)["Name"]?.ToString() ?? ""),
							DriverVersion = (((ManagementBaseObject)val3)["DriverVersion"]?.ToString() ?? ""),
							Memory = ((obj != null && !(obj is DBNull)) ? (Convert.ToInt64(((ManagementBaseObject)val3)["AdapterRAM"]) / 1048576) : 0)
						};
						list.Add(gPUInfo);
					}
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		catch (Exception ex2)
		{
			LogWrapper.Warn(ex2, "获取 GPU 信息时出错");
		}
		lock (_Lock)
		{
			if (text != null)
			{
				CPUName = text;
			}
			if (list.Count > 0)
			{
				GPUs = list;
			}
		}
		LogWrapper.Info("已获取系统硬件信息");
	}
}
