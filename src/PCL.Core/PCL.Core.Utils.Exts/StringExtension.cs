using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace PCL.Core.Utils.Exts;

public static class StringExtension
{
	extension(string input)
	{
		public string FromB10ToB36()
		{
			BigInteger bigInteger = BigInteger.Parse(input);
			List<char> list = new List<char>();
			for (; bigInteger > 0L; bigInteger /= (BigInteger)36)
			{
				byte b = (bigInteger % 36).ToByteArray()[0];
				list.Add(_B36Map[b]);
			}
			list.Reverse();
			return string.Join("", list);
		}

		public string FromB36ToB10()
		{
			return input.Select((char c) => (c < '0' || c > '9') ? (c - 65 + 10) : (c - 48)).ToArray().Aggregate(new BigInteger(0), (BigInteger n, int i) => n * 36 + i)
				.ToString();
		}

		public string FromB10ToB32()
		{
			BigInteger bigInteger = BigInteger.Parse(input);
			List<char> list = new List<char>();
			for (; bigInteger > 0L; bigInteger /= (BigInteger)32)
			{
				byte b = (bigInteger % 32).ToByteArray()[0];
				list.Add(_B32Map[b]);
			}
			list.Reverse();
			return string.Join("", list);
		}

		public string FromB32ToB10()
		{
			return input.Select(Parse).ToArray().Aggregate(new BigInteger(0), (BigInteger n, int i) => n * 32 + i)
				.ToString();
			static int Parse(char c)
			{
				switch (c)
				{
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					return c - 50;
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'G':
				case 'H':
					return c - 65 + 8;
				case 'J':
				case 'K':
				case 'L':
				case 'M':
				case 'N':
					return c - 74 + 16;
				case 'P':
				case 'Q':
				case 'R':
				case 'S':
				case 'T':
				case 'U':
				case 'V':
				case 'W':
				case 'X':
				case 'Y':
				case 'Z':
					return c - 80 + 21;
				default:
					throw new ArgumentOutOfRangeException("input", $"Character '{c}' out of Base32 range");
				}
			}
		}

		public string FromB64ToB64UrlSafe()
		{
			return input.Replace("+", "-").Replace("/", "_");
		}

		public string FromB64UrlSafeToB64()
		{
			return input.Replace("-", "+").Replace("_", "/");
		}

		public byte[] FromB64ToBytes()
		{
			switch (input.Length % 4)
			{
			case 3:
				input += "===";
				break;
			case 2:
				input += "==";
				break;
			case 1:
				input += "=";
				break;
			}
			return Convert.FromBase64String(input);
		}

		public byte[] FromB64UrlSafeToBytes()
		{
			return input.FromB64UrlSafeToB64().FromB64ToBytes();
		}

		public T ParseToEnum<T>() where T : struct, Enum
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return (T)(object)0;
			}
			if (int.TryParse(input, out var result))
			{
				return (T)(object)result;
			}
			return Enum.Parse<T>(input, ignoreCase: true);
		}
	}

	extension([NotNullWhen(false)] string? value)
	{
		public bool IsNullOrEmpty()
		{
			return string.IsNullOrEmpty(value);
		}

		public bool IsNullOrWhiteSpace()
		{
			return string.IsNullOrWhiteSpace(value);
		}
	}

	extension(string? input)
	{
		public string ReplaceNullOrEmpty(string? replacement = null)
		{
			if (!string.IsNullOrEmpty(input))
			{
				return input;
			}
			return replacement ?? string.Empty;
		}

		public string ReplaceLineBreak(string replacement = " ")
		{
			return input?.Replace(RegexPatterns.NewLine, replacement) ?? string.Empty;
		}

		[return: NotNullIfNotNull("input")]
		public string? Replace(Regex regex, string replacement)
		{
			if (input != null)
			{
				return regex.Replace(input, replacement);
			}
			return null;
		}

		public bool IsMatch(Regex regex)
		{
			if (input != null)
			{
				return regex.IsMatch(input);
			}
			return false;
		}
	}

	extension(string str)
	{
		public List<string> RegexSearch(Regex regex)
		{
			List<string> list = new List<string>();
			MatchCollection matchCollection = regex.Matches(str);
			if (matchCollection.Count == 0)
			{
				return list;
			}
			list.AddRange(from Match item in matchCollection
				select item.Value);
			return list;
		}

		public bool IsASCII()
		{
			return str.All((char c) => c < '\u0080');
		}

		public bool StartsWithF(string prefix, bool ignoreCase = false)
		{
			return str.StartsWith(prefix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		public bool EndsWithF(string suffix, bool ignoreCase = false)
		{
			return str.EndsWith(suffix, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		public bool ContainsF(string subStr, bool ignoreCase = false)
		{
			return str.Contains(subStr, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		public int IndexOfF(string subStr, bool ignoreCase = false)
		{
			return str.IndexOf(subStr, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		public int IndexOfF(string subStr, int startIndex, bool ignoreCase = false)
		{
			return str.IndexOf(subStr, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		public int LastIndexOfF(string subStr, bool ignoreCase = false)
		{
			return str.LastIndexOf(subStr, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		public int LastIndexOfF(string subStr, int startIndex, bool ignoreCase = false)
		{
			return str.LastIndexOf(subStr, startIndex, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}
	}

	extension(string hex)
	{
		public byte[] HexToBytes()
		{
			return Convert.FromHexString(hex);
		}
	}

	private static readonly char[] _B36Map = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

	private static readonly char[] _B32Map = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

	public static string? ConvertToString(object? obj)
	{
		if (obj == null)
		{
			return null;
		}
		if (obj is string result)
		{
			return result;
		}
		TypeConverter converter = TypeDescriptor.GetConverter(obj.GetType());
		if (converter.CanConvertTo(typeof(string)))
		{
			return converter.ConvertToInvariantString(obj) as string;
		}
		if (obj is IFormattable formattable)
		{
			return formattable.ToString(null, CultureInfo.InvariantCulture);
		}
		return obj.ToString();
	}

	public static string? ConvertToString<T>(this T? value)
	{
		return ConvertToString((object?)value);
	}
}
