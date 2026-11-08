using System;
using System.IO;
using Microsoft.Win32;
using PCL.Core.App.Localization;
using PCL.Core.Logging;

namespace PCL.Core.UI;

public static class SystemDialogs
{
	public static string SelectSaveFile(string? title, string fileName, string? fileFilter = null, string? initialDirectory = null)
	{
		string text = title ?? Lang.Text("SystemDialog.File.SelectTitle");
		string filter = fileFilter ?? Lang.Text("SystemDialog.File.AllFilesFilter");
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			AddExtension = true,
			Title = text,
			FileName = fileName,
			Filter = filter,
			InitialDirectory = ((!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)) ? initialDirectory : null)
		};
		LogWrapper.Info("Dialog", "打开保存文件对话框：" + text);
		if (saveFileDialog.ShowDialog() != true)
		{
			LogWrapper.Info("Dialog", "选择文件被取消");
			return "";
		}
		string fileName2 = saveFileDialog.FileName;
		LogWrapper.Info("Dialog", "选择文件返回：" + fileName2);
		if (!string.IsNullOrEmpty(fileName2))
		{
			return Path.GetFullPath(fileName2);
		}
		return "";
	}

	public static string SelectFile(string? fileFilter = null, string? title = null, string? initialDirectory = null)
	{
		string[] array = SelectFiles(fileFilter, title, initialDirectory, allowMultiSelect: false);
		if (array.Length != 0)
		{
			return array[0];
		}
		return "";
	}

	public static string[] SelectFiles(string? fileFilter = null, string? title = null, string? initialDirectory = null, bool allowMultiSelect = true)
	{
		string text = title ?? Lang.Text("SystemDialog.File.SelectTitle");
		string filter = fileFilter ?? Lang.Text("SystemDialog.File.AllFilesFilter");
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			AddExtension = true,
			CheckFileExists = true,
			Filter = filter,
			Multiselect = allowMultiSelect,
			Title = text,
			ValidateNames = true,
			InitialDirectory = ((!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)) ? initialDirectory : null)
		};
		string text2 = (allowMultiSelect ? "多" : "单");
		LogWrapper.Info("Dialog", "打开选择" + text2 + "个文件对话框: " + text);
		if (openFileDialog.ShowDialog() != true)
		{
			LogWrapper.Info("Dialog", "选择文件被取消");
			return Array.Empty<string>();
		}
		string[] fileNames = openFileDialog.FileNames;
		LogWrapper.Info("Dialog", "选择" + text2 + "个文件返回: " + string.Join(",", fileNames));
		if (fileNames.Length != 0)
		{
			return Array.ConvertAll(fileNames, Path.GetFullPath);
		}
		return Array.Empty<string>();
	}

	public static string SelectFolder(string? title = null, string? initialDirectory = null)
	{
		string text = title ?? Lang.Text("SystemDialog.Folder.SelectTitle");
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = text,
			InitialDirectory = (initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
			Multiselect = false
		};
		LogWrapper.Info("Dialog", "打开选择文件夹对话框: " + text);
		if (openFolderDialog.ShowDialog() != true)
		{
			LogWrapper.Info("Dialog", "选择文件夹被取消");
			return "";
		}
		string folderName = openFolderDialog.FolderName;
		if (string.IsNullOrEmpty(folderName))
		{
			LogWrapper.Info("Dialog", "选择文件夹返回: 空");
			return "";
		}
		string text2 = Path.GetFullPath(folderName).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		LogWrapper.Info("Dialog", "选择文件夹返回: " + text2);
		return text2;
	}
}
