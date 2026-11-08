using System;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils.OS;

public static class SystemInfo
{
	public static readonly bool Is32BitSystem = !Environment.Is64BitOperatingSystem;

	public static readonly bool IsArm64System = RuntimeInformation.OSArchitecture == Architecture.Arm64;

	public static readonly bool IsGBKEncoding = KernelInterop.GetACP() == 936;

	public static readonly string OSInfo = $"{RuntimeInformation.OSDescription} {Environment.OSVersion.Version}";
}
