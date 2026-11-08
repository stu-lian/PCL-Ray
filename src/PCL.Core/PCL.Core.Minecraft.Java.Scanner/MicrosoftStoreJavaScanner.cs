using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Java.Scanner;

public class MicrosoftStoreJavaScanner : IJavaScanner
{
	private const string StorePackagePath = "Packages\\Microsoft.4297127D64EC6_8wekyb3d8bbwe\\LocalCache\\Local\\runtime";

	public void Scan(ICollection<string> results)
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages\\Microsoft.4297127D64EC6_8wekyb3d8bbwe\\LocalCache\\Local\\runtime");
			if (!Directory.Exists(path))
			{
				return;
			}
			foreach (string item in Directory.EnumerateDirectories(path))
			{
				if (!Path.GetFileName(item).StartsWith("java-runtime", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				foreach (string item2 in Directory.EnumerateDirectories(item))
				{
					foreach (string item3 in Directory.EnumerateDirectories(item2))
					{
						string text = Path.Combine(item3, "bin", "java.exe");
						if (File.Exists(text))
						{
							LogWrapper.Info("[Java] 检测到 Microsoft Store Java: " + text);
							results.Add(text);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Java", "Microsoft Store Java 扫描失败");
		}
	}
}
