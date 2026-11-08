using System;
using System.Collections.Generic;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.OS;

public static class EnvironmentInterop
{
	private const string LogModule = "Environment";

	private static readonly Dictionary<string, string?> SecretDictionary = new();

	public static bool ReadVariable<TValue>(string key, ref TValue target, bool detailLog = true)
	{
		string environmentVariable = Environment.GetEnvironmentVariable(key);
		if (environmentVariable == null)
		{
			return false;
		}
		string text = (detailLog ? (" = " + environmentVariable) : string.Empty);
		LogWrapper.Debug("Environment", "读取到环境变量 " + key + text);
		TValue val = environmentVariable.Convert<TValue>();
		if (val == null)
		{
			LogWrapper.Warn("Environment", "环境变量 " + key + " 类型转换失败");
			return false;
		}
		target = val;
		return true;
	}

	public static string? GetSecret(string key, bool readEnv = true, bool readEnvDebugOnly = false)
	{
		if (SecretDictionary.TryGetValue(key, out string? value))
		{
			return value;
		}
		if (!readEnv)
		{
			return null;
		}
		if (ReadVariable("PCL_" + key, ref value, detailLog: false))
		{
			SecretDictionary[key] = value;
		}
		return value;
	}

	public static string GetCurrentOsName()
	{
		if (OperatingSystem.IsWindows())
		{
			return "windows";
		}
		if (OperatingSystem.IsLinux())
		{
			return "linux";
		}
		if (!OperatingSystem.IsMacOS())
		{
			return "unknown";
		}
		return "osx";
	}
}
