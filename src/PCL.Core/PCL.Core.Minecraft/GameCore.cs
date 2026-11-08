using System.IO;
using System.IO.Compression;

namespace PCL.Core.Minecraft;

public class GameCore
{
	private readonly string _corePath;

	public GameCore(string corePath)
	{
		if (!File.Exists(corePath))
		{
			throw new FileNotFoundException("未找到指定文件：" + corePath);
		}
		_corePath = corePath;
	}

	public void AddToCore(string jarPath)
	{
		if (!File.Exists(jarPath))
		{
			throw new FileNotFoundException("未找到指定文件：" + jarPath);
		}
		using FileStream stream = new FileStream(_corePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 16384, useAsync: true);
		using FileStream stream2 = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.Read, 16384, useAsync: true);
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Update);
		using ZipArchive zipArchive2 = new ZipArchive(stream2);
		string value = (jarPath.EndsWith(".jar") ? "" : "MINECRAFT-JAR");
		foreach (ZipArchiveEntry entry in zipArchive2.Entries)
		{
			if (!entry.FullName.Contains(value))
			{
				continue;
			}
			using Stream destination = zipArchive.CreateEntry(entry.FullName).Open();
			using Stream stream3 = zipArchive2.GetEntry(entry.FullName)?.Open();
			stream3?.CopyTo(destination);
		}
		zipArchive.GetEntry("META-INF")?.Delete();
	}
}
