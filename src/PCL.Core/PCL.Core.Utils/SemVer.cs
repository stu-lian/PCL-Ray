using System;
using System.Text.RegularExpressions;

namespace PCL.Core.Utils;

[Serializable]
public class SemVer(int major, int minor, int patch, string? prerelease = null, string? buildMetadata = null) : IComparable<SemVer>, IEquatable<SemVer>
{
	public int Major => major;

	public int Minor => minor;

	public int Patch => patch;

	public string Prerelease => prerelease ?? string.Empty;

	public string BuildMetadata => buildMetadata ?? string.Empty;

	public static SemVer Parse(string version)
	{
		if (!TryParse(version, out SemVer result))
		{
			throw new ArgumentException("Invalid semantic version format");
		}
		return result;
	}

	public static bool TryParse(string version, out SemVer? result)
	{
		if (string.IsNullOrWhiteSpace(version))
		{
			result = null;
			return false;
		}
		Match match = RegexPatterns.SemVer.Match(version);
		if (!match.Success)
		{
			// 兼容 2.01.1 这类带前导零的版本号：仅归一化主版本段，预发布与构建元数据保持原样。
			string normalized = NormalizeLeadingZeros(version);
			if (!string.Equals(normalized, version, StringComparison.Ordinal))
			{
				match = RegexPatterns.SemVer.Match(normalized);
			}
		}
		if (!match.Success)
		{
			result = null;
			return false;
		}
		result = _CreateFromMatch(match);
		return true;
	}

	/// <summary>去掉 major/minor/patch 的前导零，例如 2.01.1 归一化为 2.1.1。</summary>
	private static string NormalizeLeadingZeros(string version)
	{
		int cut = version.Length;
		int dash = version.IndexOf('-');
		int plus = version.IndexOf('+');
		if (dash >= 0) cut = dash;
		if (plus >= 0 && plus < cut) cut = plus;
		string core = version.Substring(0, cut);
		string rest = version.Substring(cut);
		string prefix = core.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? core.Substring(0, 1) : "";
		string[] parts = core.Substring(prefix.Length).Split('.');
		for (int i = 0; i < parts.Length; i++)
		{
			if (parts[i].Length > 1)
			{
				parts[i] = parts[i].TrimStart('0');
				if (parts[i].Length == 0) parts[i] = "0";
			}
		}
		return prefix + string.Join(".", parts) + rest;
	}

	private static SemVer _CreateFromMatch(Match match)
	{
		int num = int.Parse(match.Groups["major"].Value);
		int num2 = int.Parse(match.Groups["minor"].Value);
		int num3 = int.Parse(match.Groups["patch"].Value);
		string value = match.Groups["prerelease"].Value;
		string value2 = match.Groups["build"].Value;
		return new SemVer(num, num2, num3, value, value2);
	}

	public int CompareTo(SemVer? other)
	{
		if ((object)other == null)
		{
			return 1;
		}
		int num = Major.CompareTo(other.Major);
		if (num != 0)
		{
			return num;
		}
		num = Minor.CompareTo(other.Minor);
		if (num != 0)
		{
			return num;
		}
		num = Patch.CompareTo(other.Patch);
		if (num != 0)
		{
			return num;
		}
		return _ComparePrerelease(Prerelease, other.Prerelease);
	}

	private static int _ComparePrerelease(string a, string b)
	{
		if (string.Equals(a, b, StringComparison.Ordinal))
		{
			return 0;
		}
		if (string.IsNullOrEmpty(a))
		{
			return 1;
		}
		if (string.IsNullOrEmpty(b))
		{
			return -1;
		}
		string[] array = a.Split('.');
		string[] array2 = b.Split('.');
		int num = Math.Min(array.Length, array2.Length);
		for (int i = 0; i < num; i++)
		{
			string text = array[i];
			string text2 = array2[i];
			bool flag = int.TryParse(text, out var result);
			bool flag2 = int.TryParse(text2, out var result2);
			int num2;
			if (flag & flag2)
			{
				num2 = result.CompareTo(result2);
			}
			else
			{
				num2 = ((!(flag | flag2)) ? string.Compare(text, text2, StringComparison.Ordinal) : ((!flag) ? 1 : (-1)));
			}
			if (num2 != 0)
			{
				return num2;
			}
		}
		return array.Length.CompareTo(array2.Length);
	}

	public override string ToString()
	{
		string text = $"{Major}.{Minor}.{Patch}";
		if (!string.IsNullOrEmpty(Prerelease))
		{
			text = text + "-" + Prerelease;
		}
		if (!string.IsNullOrEmpty(BuildMetadata))
		{
			text = text + "+" + BuildMetadata;
		}
		return text;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as SemVer);
	}

	public bool Equals(SemVer? other)
	{
		if ((object)other != null && Major == other.Major && Minor == other.Minor && Patch == other.Patch && string.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal))
		{
			return string.Equals(BuildMetadata, other.BuildMetadata, StringComparison.Ordinal);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((17 * 23 + Major.GetHashCode()) * 23 + Minor.GetHashCode()) * 23 + Patch.GetHashCode()) * 23 + Prerelease.GetHashCode()) * 23 + BuildMetadata.GetHashCode();
	}

	public static bool operator ==(SemVer? left, SemVer? right)
	{
		if ((object)left == right)
		{
			return true;
		}
		if ((object)left == null || (object)right == null)
		{
			return false;
		}
		return left.Equals(right);
	}

	public static bool operator !=(SemVer? left, SemVer? right)
	{
		return !(left == right);
	}

	public static bool operator <(SemVer? left, SemVer? right)
	{
		if ((object)left != null)
		{
			return left.CompareTo(right) < 0;
		}
		return (object)right != null;
	}

	public static bool operator >(SemVer? left, SemVer? right)
	{
		if ((object)left != null)
		{
			return left.CompareTo(right) > 0;
		}
		return false;
	}

	public static bool operator <=(SemVer? left, SemVer? right)
	{
		if ((object)left != null)
		{
			return left.CompareTo(right) <= 0;
		}
		return true;
	}

	public static bool operator >=(SemVer? left, SemVer? right)
	{
		if ((object)left != null)
		{
			return left.CompareTo(right) >= 0;
		}
		return (object)right == null;
	}
}
