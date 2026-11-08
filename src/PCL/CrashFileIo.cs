using System;
using System.IO;
using System.Text;
using System.Threading;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils.Codecs;

namespace PCL;

internal static class CrashFileIo
{
	public static byte[] ReadBytes(string filePath)
	{
		try
		{
			using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using MemoryStream memoryStream = new MemoryStream();
			fileStream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash IO", "读取与游戏崩溃关联的日志文件时出错，");
			return Files.ReadAllBytesOrEmptyAsync(filePath, default(CancellationToken)).GetAwaiter().GetResult();
		}
	}

	public static string ReadText(string filePath, Encoding? encoding = null)
	{
		byte[] array = ReadBytes(filePath);
		if (encoding != null)
		{
			return encoding.GetString(array);
		}
		return EncodingUtils.DecodeBytes(array);
	}

	public static void WriteText(string filePath, string text, Encoding? encoding = null)
	{
		Files.WriteFileAsync(filePath, text, false, encoding, default(CancellationToken)).GetAwaiter().GetResult();
	}

	public static void CopyFile(string fromPath, string toPath)
	{
		Files.CopyFileAsync(fromPath, toPath, default(CancellationToken)).GetAwaiter().GetResult();
	}

	public static void DeleteDirectory(string directoryPath)
	{
		Directories.DeleteDirectoryAsync(directoryPath, true, default(CancellationToken)).GetAwaiter().GetResult();
	}

	public static bool CanExtractArchive(string archivePath)
	{
		string fileName = Path.GetFileName(archivePath);
		if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
		{
			return fileName.EndsWith(".bz2", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static void ExtractFile(string archivePath, string destinationDirectory)
	{
		if (!CanExtractArchive(archivePath))
		{
			throw new NotSupportedException("崩溃日志导入不支持该压缩格式。支持 zip、gz、bz2、tar、tgz 与 tar.gz。");
		}
		Files.ExtractFileAsync(archivePath, destinationDirectory, (Action<double>)null, default(CancellationToken)).GetAwaiter().GetResult();
	}
}
