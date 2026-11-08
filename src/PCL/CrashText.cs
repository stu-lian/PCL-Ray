using System;

namespace PCL;

internal static class CrashText
{
	public static string BeforeFirst(string text, string marker, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(marker) ? (-1) : text.IndexOf(marker, _Comparison(ignoreCase)));
		if (num < 0)
		{
			return text;
		}
		return text.Substring(0, num);
	}

	public static string AfterFirst(string text, string marker, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(marker) ? (-1) : text.IndexOf(marker, _Comparison(ignoreCase)));
		if (num < 0)
		{
			return text;
		}
		return text.Substring(num + marker.Length);
	}

	public static string AfterLast(string text, string marker, bool ignoreCase = false)
	{
		int num = (string.IsNullOrEmpty(marker) ? (-1) : text.LastIndexOf(marker, _Comparison(ignoreCase)));
		if (num < 0)
		{
			return text;
		}
		return text.Substring(num + marker.Length);
	}

	public static string Between(string text, string after, string before, bool ignoreCase = false)
	{
		StringComparison comparisonType = _Comparison(ignoreCase);
		int num = (string.IsNullOrEmpty(after) ? (-1) : text.LastIndexOf(after, comparisonType));
		num = ((num >= 0) ? (num + after.Length) : 0);
		int num2 = (string.IsNullOrEmpty(before) ? (-1) : text.IndexOf(before, num, comparisonType));
		if (num2 >= 0)
		{
			int num3 = num;
			return text.Substring(num3, num2 - num3);
		}
		if (num <= 0)
		{
			return text;
		}
		return text.Substring(num);
	}

	private static StringComparison _Comparison(bool ignoreCase)
	{
		if (!ignoreCase)
		{
			return StringComparison.Ordinal;
		}
		return StringComparison.OrdinalIgnoreCase;
	}
}
