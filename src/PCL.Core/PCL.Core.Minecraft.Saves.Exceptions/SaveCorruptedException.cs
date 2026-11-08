using System;

namespace PCL.Core.Minecraft.Saves.Exceptions;

public class SaveCorruptedException : Exception
{
	public string FolderPath { get; }

	public SaveCorruptedException(string folderPath)
		: base("存档损坏：无法解析 '" + folderPath + "' 中的 level.dat")
	{
		FolderPath = folderPath;
	}

	public SaveCorruptedException(string folderPath, string message)
		: base(message)
	{
		FolderPath = folderPath;
	}

	public SaveCorruptedException(string folderPath, string message, Exception inner)
		: base(message, inner)
	{
		FolderPath = folderPath;
	}
}
