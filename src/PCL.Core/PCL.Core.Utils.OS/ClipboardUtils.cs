using System;
using System.Windows;

namespace PCL.Core.Utils.OS;

public static class ClipboardUtils
{
	public static void SetClipboardFiles(string[] paths)
	{
		if (paths == null || paths.Length == 0)
		{
			throw new ArgumentException("Paths cannot be null or empty.", "paths");
		}
		DataObject dataObject = new DataObject();
		dataObject.SetData(DataFormats.FileDrop, paths);
		Clipboard.SetDataObject(dataObject);
	}
}
