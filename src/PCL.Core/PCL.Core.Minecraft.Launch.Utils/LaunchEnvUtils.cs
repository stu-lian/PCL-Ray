using System;
using System.IO;
using PCL.Core.App;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Launch.Utils;

public static class LaunchEnvUtils
{
	private const string DebugLegacyLog4J2ConfigResource = "Resources/log4j2-legacy-debug.xml";

	private const string DebugLog4J2ConfigResource = "Resources/log4j2-debug.xml";

	private static readonly object _ExtractLegacyDebugLog4J2ConfigLock = new object();

	private static readonly object _ExtractDebugLog4J2ConfigLock = new object();

	public static string ExtractLegacyDebugLog4j2Config()
	{
		return _ExtractFile("Resources/log4j2-legacy-debug.xml", "log4j2-legacy-debug.xml", _ExtractLegacyDebugLog4J2ConfigLock);
	}

	public static string ExtractDebugLog4j2Config()
	{
		return _ExtractFile("Resources/log4j2-debug.xml", "log4j2-debug.xml", _ExtractDebugLog4J2ConfigLock);
	}

	private static string _ExtractFile(string resourceName, string fileName, object lockObj)
	{
		string text = Path.Combine(Paths.Temp, fileName);
		LogWrapper.Info(resourceName, "选定路径：" + text);
		lock (lockObj)
		{
			try
			{
				_WriteResourceToFile(resourceName, text);
			}
			catch (Exception ex)
			{
				if (!File.Exists(text))
				{
					throw new FileNotFoundException("释放 " + resourceName + " 失败", ex);
				}
				LogWrapper.Warn(ex, resourceName + " 文件释放失败，尝试删除后重试");
				File.Delete(text);
				try
				{
					_WriteResourceToFile(resourceName, text);
				}
				catch (Exception ex2)
				{
					string text2 = Path.Combine(Paths.Temp, Path.GetFileNameWithoutExtension(fileName) + "2" + Path.GetExtension(fileName));
					LogWrapper.Warn(ex2, resourceName + " 重试失败，尝试新路径：" + text2);
					_WriteResourceToFile(resourceName, text2);
					text = text2;
				}
			}
		}
		return text;
	}

	private static void _WriteResourceToFile(string resourceName, string path)
	{
		using Stream stream = Basics.GetResourceStream(resourceName);
		if (stream == null)
		{
			throw new FileNotFoundException("资源 " + resourceName + " 未找到。");
		}
		using FileStream destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096);
		stream.CopyTo(destination);
	}
}
