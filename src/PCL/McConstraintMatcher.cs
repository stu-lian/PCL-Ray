using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL;

public static class McConstraintMatcher
{
	public enum MatchKind
	{
		None,
		Incompatible,
		NoConstraint,
		Range,
		Exact
	}

	private static bool IsForgeLike(string loaderType)
	{
		if (!string.Equals(loaderType, "Forge", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(loaderType, "NeoForge", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static MatchKind Match(string fileName, string version, string constraint, string loaderType, string instanceMc)
	{
		if (string.IsNullOrWhiteSpace(instanceMc))
		{
			return MatchKind.None;
		}
		if (ContainsVersionToken(fileName, instanceMc) || ContainsVersionToken(version, instanceMc))
		{
			return MatchKind.Exact;
		}
		if (string.IsNullOrWhiteSpace(constraint))
		{
			return MatchKind.NoConstraint;
		}
		if (!Satisfies(constraint, loaderType, instanceMc))
		{
			return MatchKind.Incompatible;
		}
		return MatchKind.Range;
	}

	public static bool Satisfies(string constraint, string loaderType, string version)
	{
		if (string.IsNullOrWhiteSpace(constraint))
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(version))
		{
			return false;
		}
		try
		{
			string text = constraint.TrimStart();
			bool flag = text.Length > 0 && (text[0] == '[' || text[0] == '(');
			bool flag2 = constraint.IndexOfAny(new char[5] { '>', '<', '~', '^', '*' }) >= 0 || constraint.Contains("||");
			if (flag && !flag2)
			{
				return SatisfiesMaven(constraint, version);
			}
			if (flag2 && !flag)
			{
				return SatisfiesSemVer(constraint, version);
			}
			if (IsForgeLike(loaderType))
			{
				return SatisfiesMaven(constraint, version);
			}
			if (loaderType == null)
			{
				return SatisfiesSemVer(constraint, version) || SatisfiesMaven(constraint, version);
			}
			return SatisfiesSemVer(constraint, version);
		}
		catch
		{
			return false;
		}
	}

	private static string StripBuild(string s)
	{
		int num = s.IndexOf('+');
		if (num >= 0)
		{
			return s.Substring(0, num);
		}
		return s;
	}

	public static string StripV(string s)
	{
		if (s == null || s.Length <= 1 || (s[0] != 'v' && s[0] != 'V') || !char.IsDigit(s[1]))
		{
			return s;
		}
		return s.Substring(1);
	}

	private static int Cmp(string a, string b)
	{
		return McVersionComparer.CompareVersion(StripV(a), StripV(b));
	}

	public static int CompareMavenVersions(string left, string right)
	{
		left = StripV(left?.Trim() ?? "");
		right = StripV(right?.Trim() ?? "");
		try
		{
			return MavenComparableVersion.Compare(left, right);
		}
		catch (ArgumentException)
		{
			return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
		}
	}

	public static int CompareFabricVersions(string left, string right)
	{
		left = StripBuild(left?.Trim() ?? "");
		right = StripBuild(right?.Trim() ?? "");
		if (!IsKnown(left) || !IsKnown(right))
		{
			return string.CompareOrdinal(left, right);
		}
		return CompareSemVer(left, right);
	}

	public static bool HasComparableBound(string constraint)
	{
		if (string.IsNullOrWhiteSpace(constraint))
		{
			return false;
		}
		foreach (string item in SplitTopLevel(constraint))
		{
			string text = item.Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (text[0] != '[' && text[0] != '(')
			{
				if (IsKnown(text.TrimStart(new char[7] { '>', '<', '=', '~', '^', ' ', '"' })))
				{
					return true;
				}
			}
			else
			{
				if (text.Length < 2)
				{
					continue;
				}
				if (text[text.Length - 1] != ']')
				{
					if (text[text.Length - 1] != ')')
					{
						continue;
					}
				}
				string text2 = text.Substring(1, text.Length - 2);
				int num = text2.IndexOf(',');
				if (num < 0)
				{
					if (IsKnown(text2.Trim()))
					{
						return true;
					}
					continue;
				}
				string s = text2.Substring(0, num).Trim();
				string s2 = text2.Substring(num + 1).Trim();
				if (IsKnown(s) || IsKnown(s2))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsKnown(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return false;
		}
		if (s.EndsWith("-"))
		{
			s = s.Substring(0, s.Length - 1);
		}
		s = StripV(s);
		if (s.Length > 0)
		{
			return char.IsDigit(s[0]);
		}
		return false;
	}

	private static bool SatisfiesMaven(string constraint, string mc)
	{
		foreach (string item in SplitTopLevel(constraint))
		{
			string text = item.Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (text[0] != '[' && text[0] != '(')
			{
				if (text.Length > 0 && CompareMavenVersions(mc, text) >= 0)
				{
					return true;
				}
			}
			else
			{
				if (text.Length < 2)
				{
					continue;
				}
				if (text[text.Length - 1] != ']')
				{
					if (text[text.Length - 1] != ')')
					{
						continue;
					}
				}
				bool flag = text[0] == '[';
				bool flag2 = text[text.Length - 1] == ']';
				string text2 = text.Substring(1, text.Length - 2);
				int num = text2.IndexOf(',');
				if (num < 0)
				{
					string text3 = text2.Trim();
					if (text3.Length > 0 && CompareMavenVersions(mc, text3) == 0)
					{
						return true;
					}
					continue;
				}
				string text4 = text2.Substring(0, num).Trim();
				string text5 = text2.Substring(num + 1).Trim();
				bool flag3 = true;
				if (text4.Length > 0)
				{
					int num2 = CompareMavenVersions(mc, text4);
					flag3 = (flag ? (num2 >= 0) : (num2 > 0));
				}
				if (flag3 && text5.Length > 0)
				{
					int num3 = CompareMavenVersions(mc, text5);
					flag3 = (flag2 ? (num3 <= 0) : (num3 < 0));
				}
				if (flag3)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static List<string> SplitTopLevel(string s)
	{
		List<string> list = new List<string>();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < s.Length; i++)
		{
			switch (s[i])
			{
			case '(':
			case '[':
				num++;
				break;
			case ')':
			case ']':
				num--;
				break;
			case ',':
				if (num == 0)
				{
					list.Add(s.Substring(num2, i - num2));
					num2 = i + 1;
				}
				break;
			}
		}
		list.Add(s.Substring(num2));
		return list;
	}

	private static int CompareSemVer(string a, string b)
	{
		a = StripBuild(a);
		b = StripBuild(b);
		int num = a.IndexOf('-');
		int num2 = b.IndexOf('-');
		string left = ((num < 0) ? a : a.Substring(0, num));
		string right = ((num2 < 0) ? b : b.Substring(0, num2));
		int num3 = _CompareSemVerCore(left, right);
		if (num3 != 0)
		{
			return num3;
		}
		if (num < 0 && num2 < 0)
		{
			return 0;
		}
		if (num < 0)
		{
			return 1;
		}
		if (num2 < 0)
		{
			return -1;
		}
		return ComparePrerelease(a.Substring(num + 1), b.Substring(num2 + 1));
	}

	private static int _CompareSemVerCore(string left, string right)
	{
		string[] array = left.Split('.');
		string[] array2 = right.Split('.');
		if (array.All(IsAllDigits) && array2.All(IsAllDigits))
		{
			int num = Math.Max(array.Length, array2.Length);
			for (int i = 0; i < num; i++)
			{
				int num2 = CompareNumericId((i < array.Length) ? array[i] : "0", (i < array2.Length) ? array2[i] : "0");
				if (num2 != 0)
				{
					return num2;
				}
			}
			return 0;
		}
		return Cmp(left, right);
	}

	private static int ComparePrerelease(string a, string b)
	{
		string[] array = a.Split('.');
		string[] array2 = b.Split('.');
		int num = Math.Min(array.Length, array2.Length);
		for (int i = 0; i < num; i++)
		{
			bool flag = IsAllDigits(array[i]);
			bool flag2 = IsAllDigits(array2[i]);
			int num2;
			if (flag & flag2)
			{
				num2 = CompareNumericId(array[i], array2[i]);
			}
			else
			{
				num2 = (flag ? (-1) : (flag2 ? 1 : string.CompareOrdinal(array[i], array2[i])));
			}
			if (num2 != 0)
			{
				return num2;
			}
		}
		return array.Length.CompareTo(array2.Length);
	}

	private static bool IsAllDigits(string s)
	{
		if (s.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (!char.IsDigit(s[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static int CompareNumericId(string x, string y)
	{
		x = x.TrimStart('0');
		y = y.TrimStart('0');
		if (x.Length == y.Length)
		{
			return string.CompareOrdinal(x, y);
		}
		return x.Length - y.Length;
	}

	private static bool SatisfiesSemVer(string constraint, string mc)
	{
		string[] array = constraint.Split(new string[1] { "||" }, StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0)
			{
				continue;
			}
			bool flag = true;
			string[] array2 = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			for (int j = 0; j < array2.Length; j++)
			{
				if (!TermMatches(array2[j], mc))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	private static bool TermMatches(string term, string mc)
	{
		if (term == "*" || string.Equals(term, "any", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		int i;
		for (i = 0; i < term.Length && "><=".IndexOf(term[i]) >= 0; i++)
		{
		}
		string text = term.Substring(0, i);
		string text2 = term.Substring(i).Trim();
		bool flag = text2.EndsWith("-");
		if (flag)
		{
			text2 = text2.Substring(0, text2.Length - 1);
		}
		if (text2.Length == 0)
		{
			return false;
		}
		if (text.Length == 0 && (text2[0] == '~' || text2[0] == '^'))
		{
			bool flag2 = text2[0] == '~';
			string text3 = text2.Substring(1).Trim();
			if (!IsKnown(text3))
			{
				return false;
			}
			if (CompareSemVer(mc, text3) < 0)
			{
				return false;
			}
			List<int> list = new List<int>();
			int num = -1;
			string text4 = text3;
			foreach (char c in text4)
			{
				if (char.IsDigit(c))
				{
					int num2 = c - 48;
					if (num > (int.MaxValue - num2) / 10)
					{
						return false;
					}
					num = ((num >= 0) ? (num * 10) : 0) + num2;
				}
				else if (num >= 0)
				{
					list.Add(num);
					num = -1;
				}
			}
			if (num >= 0)
			{
				list.Add(num);
			}
			if (list.Count == 0)
			{
				return false;
			}
			string b;
			if (flag2)
			{
				b = ((list.Count >= 2) ? (list[0] + "." + (list[1] + 1)) : (list[0] + 1).ToString());
			}
			else
			{
				int k;
				for (k = 0; k < list.Count && list[k] == 0; k++)
				{
				}
				if (k >= list.Count)
				{
					k = list.Count - 1;
				}
				b = string.Join(".", list.Take(k).Concat(new int[1] { list[k] + 1 }));
			}
			return CompareSemVer(mc, b) < 0;
		}
		if (text.Length == 0 && (text2.EndsWith(".x") || text2.EndsWith(".X") || text2.EndsWith(".*")))
		{
			string text5 = text2.Substring(0, text2.Length - 2);
			if (!(mc == text5))
			{
				return mc.StartsWith(text5 + ".", StringComparison.Ordinal);
			}
			return true;
		}
		if (!IsKnown(text2))
		{
			if (((text != null && text.Length == 0) || text == "=" || text == "==") ? true : false)
			{
				return string.Equals(mc, text2, StringComparison.Ordinal);
			}
			return false;
		}
		bool flag3 = flag;
		if (flag3)
		{
			bool flag4 = (((text != null && text.Length == 0) || text == ">=" || text == ">") ? true : false);
			flag3 = flag4;
		}
		if (flag3 && string.Equals(mc.Split('-')[0], text2, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		int num3 = CompareSemVer(mc, text2);
		switch (text)
		{
		case "=":
		case "==":
		case "":
			return num3 == 0;
		case ">=":
			return num3 >= 0;
		case ">":
			return num3 > 0;
		case "<=":
			return num3 <= 0;
		case "<":
			return num3 < 0;
		default:
			return false;
		}
	}

	public static bool ContainsVersionToken(string haystack, string version)
	{
		if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(version))
		{
			return false;
		}
		for (int num = haystack.IndexOf(version, StringComparison.Ordinal); num >= 0; num = haystack.IndexOf(version, num + 1, StringComparison.Ordinal))
		{
			char c = ((num > 0) ? haystack[num - 1] : ' ');
			int num2 = num + version.Length;
			char c2 = ((num2 < haystack.Length) ? haystack[num2] : ' ');
			bool num3 = c != '.' && !char.IsDigit(c);
			bool flag = !char.IsDigit(c2) && (c2 != '.' || !char.IsDigit((num2 + 1 < haystack.Length) ? haystack[num2 + 1] : ' '));
			if (num3 & flag)
			{
				return true;
			}
		}
		return false;
	}
}
