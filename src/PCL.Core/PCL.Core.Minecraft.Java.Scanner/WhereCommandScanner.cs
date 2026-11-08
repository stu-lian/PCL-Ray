using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Java.Scanner;

public class WhereCommandScanner : IJavaScanner
{
	public void Scan(ICollection<string> results)
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "where",
				Arguments = "java",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return;
			}
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			if (process.ExitCode != 0)
			{
				return;
			}
			foreach (string item in from p in text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				select p.Trim() into p
				where File.Exists(p)
				select p)
			{
				results.Add(item);
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Java", "where 命令扫描失败");
		}
	}
}
