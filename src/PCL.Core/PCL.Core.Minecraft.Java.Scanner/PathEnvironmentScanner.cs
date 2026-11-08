using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Java.Scanner;

public class PathEnvironmentScanner : IJavaScanner
{
	public void Scan(ICollection<string> results)
	{
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable("PATH");
			if (string.IsNullOrEmpty(environmentVariable))
			{
				return;
			}
			string[] array = environmentVariable.Split(';', StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				if (Directory.Exists(text))
				{
					string text2 = Path.Combine(text, "java.exe");
					if (File.Exists(text2))
					{
						results.Add(text2);
					}
				}
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Java", "PATH环境变量扫描失败");
		}
	}
}
