using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Java.Scanner;

public class RegistryJavaScanner : IJavaScanner
{
	private static readonly string[] _RegistryPaths = new string[4] { "SOFTWARE\\JavaSoft\\Java Development Kit", "SOFTWARE\\JavaSoft\\Java Runtime Environment", "SOFTWARE\\WOW6432Node\\JavaSoft\\Java Development Kit", "SOFTWARE\\WOW6432Node\\JavaSoft\\Java Runtime Environment" };

	private static readonly string[] _BrandRegistryPaths = new string[2] { "SOFTWARE\\Azul Systems\\Zulu", "SOFTWARE\\BellSoft\\Liberica" };

	public void Scan(ICollection<string> results)
	{
		try
		{
			_ScanJavaSoftRegistry(results);
			_ScanBrandRegistry(results);
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Java", "注册表扫描失败");
		}
	}

	private static void _ScanJavaSoftRegistry(ICollection<string> results)
	{
		string[] registryPaths = _RegistryPaths;
		foreach (string name in registryPaths)
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
			if (registryKey == null)
			{
				continue;
			}
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string name2 in subKeyNames)
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey(name2);
				string javaHome = registryKey2?.GetValue("JavaHome") as string;
				if (!string.IsNullOrEmpty(javaHome) && !Path.GetInvalidPathChars().Any((char c) => javaHome.Contains(c)))
				{
					string text = Path.Combine(javaHome, "bin", "java.exe");
					if (File.Exists(text))
					{
						results.Add(text);
					}
				}
			}
		}
	}

	private static void _ScanBrandRegistry(ICollection<string> results)
	{
		string[] brandRegistryPaths = _BrandRegistryPaths;
		foreach (string name in brandRegistryPaths)
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
			if (registryKey == null)
			{
				continue;
			}
			string[] subKeyNames = registryKey.GetSubKeyNames();
			foreach (string name2 in subKeyNames)
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey(name2);
				string installPath = registryKey2?.GetValue("InstallationPath") as string;
				if (!string.IsNullOrEmpty(installPath) && !Path.GetInvalidPathChars().Any((char c) => installPath.Contains(c)))
				{
					string text = Path.Combine(installPath, "bin", "java.exe");
					if (File.Exists(text))
					{
						results.Add(text);
					}
				}
			}
		}
	}
}
