using System;

namespace PCL.Core.Minecraft.Saves.Exceptions;

public class SaveNotFoundException : Exception
{
	public string FolderPath { get; }

	public SaveNotFoundException(string folderPath)
		: base("未找到存档：'" + folderPath + "' 中缺少 level.dat")
	{
		FolderPath = folderPath;
	}

	public SaveNotFoundException(string folderPath, string message)
		: base(message)
	{
		FolderPath = folderPath;
	}

	public SaveNotFoundException(string folderPath, string message, Exception inner)
		: base(message, inner)
	{
		FolderPath = folderPath;
	}
}
