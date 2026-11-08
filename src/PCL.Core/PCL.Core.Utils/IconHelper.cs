using System;
using System.Drawing;
using System.IO;
using PCL.Core.App;

namespace PCL.Core.Utils;

public static class IconHelper
{
	public static string GetIconPath()
	{
		string text = Path.Combine(Paths.Temp, "icon.png");
		if (!File.Exists(text))
		{
			CreateIcon();
		}
		return text;
	}

	private static void CreateIcon()
	{
		using Icon icon = Icon.ExtractAssociatedIcon(Basics.ExecutablePath) ?? throw new InvalidOperationException("无法提取程序图标。");
		using Bitmap bitmap = icon.ToBitmap();
		bitmap.Save(Path.Combine(Paths.Temp, "icon.png"));
	}
}
