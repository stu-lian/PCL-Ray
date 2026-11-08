using System.Collections.Generic;
using PCL.Core.App.Localization;

namespace PCL;

public static class McVersionComparer
{
	public class VersionComparer : IComparer<string>
	{
		public int Compare(string x, string y)
		{
			return CompareVersion(x, y);
		}
	}

	public const string UNKNOWN_VERSION_KEY = "UnknownVersion";

	public static bool CompareVersionGe(string left, string right)
	{
		return CompareVersion(left, right) >= 0;
	}

	public static int CompareVersion(string left, string right)
	{
		if (left == Lang.Text("Minecraft.Version.Unknown") || right == Lang.Text("Minecraft.Version.Unknown"))
		{
			if (left == Lang.Text("Minecraft.Version.Unknown") && right != Lang.Text("Minecraft.Version.Unknown"))
			{
				return 1;
			}
			if (left == Lang.Text("Minecraft.Version.Unknown") && right == Lang.Text("Minecraft.Version.Unknown"))
			{
				return 0;
			}
			if (left != Lang.Text("Minecraft.Version.Unknown") && right == Lang.Text("Minecraft.Version.Unknown"))
			{
				return -1;
			}
		}
		left = left.ToLowerInvariant();
		right = right.ToLowerInvariant();
		List<string> list = left.Replace("快照", "snapshot").Replace("预览版", "pre").RegexSearch("[a-z]+|[0-9]+");
		List<string> list2 = right.Replace("快照", "snapshot").Replace("预览版", "pre").RegexSearch("[a-z]+|[0-9]+");
		int num = 0;
		while (true)
		{
			if (list.Count - 1 < num && list2.Count - 1 < num)
			{
				if (string.CompareOrdinal(left, right) > 0)
				{
					return 1;
				}
				if (string.CompareOrdinal(left, right) < 0)
				{
					return -1;
				}
				return 0;
			}
			string text = ((list.Count - 1 < num) ? "0" : list[num]);
			string text2 = ((list2.Count - 1 < num) ? "0" : list2[num]);
			if (!((text ?? "") == (text2 ?? "")))
			{
				if (text == "rc")
				{
					text = (-1).ToString();
				}
				if (text == "pre")
				{
					text = (-2).ToString();
				}
				if (text == "snapshot")
				{
					text = (-3).ToString();
				}
				if (text == "experimental")
				{
					text = (-4).ToString();
				}
				double num2 = ModBase.Val(text);
				if (text2 == "rc")
				{
					text2 = (-1).ToString();
				}
				if (text2 == "pre")
				{
					text2 = (-2).ToString();
				}
				if (text2 == "snapshot")
				{
					text2 = (-3).ToString();
				}
				if (text2 == "experimental")
				{
					text2 = (-4).ToString();
				}
				double num3 = ModBase.Val(text2);
				if (num2 == 0.0 && num3 == 0.0)
				{
					if (string.CompareOrdinal(text, text2) > 0)
					{
						return 1;
					}
					if (string.CompareOrdinal(text, text2) < 0)
					{
						return -1;
					}
				}
				else
				{
					if (num2 > num3)
					{
						return 1;
					}
					if (num2 < num3)
					{
						break;
					}
				}
			}
			num++;
		}
		return -1;
	}
}
