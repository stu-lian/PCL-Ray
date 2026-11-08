using System.Buffers;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal static class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	private const int WordCategoriesMask = 262463;

	internal static readonly SearchValues<char> s_asciiLettersAndUnderscore = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

	internal static readonly SearchValues<char> s_ascii_FFFFFFFFEFFF00FC01000078010000F8 = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#%&'()*+,-./:;<=>?@[\\]^`{|}~\u007f");

	internal static readonly SearchValues<string> s_indexOfString_2A6F2F0517B49FBD69AC7922BAF9726B1EB94FC8F0F3D1041AC247E3E2904D60 = SearchValues.Create(new ReadOnlySpan<string>("<Setter"), StringComparison.Ordinal);

	internal static readonly SearchValues<string> s_indexOfString_52478F2B00FC8D3D8978DE01FEE97B62431A2B859ECA3D1005ECCEE9C01B2F27 = SearchValues.Create(new ReadOnlySpan<string>("<local:CustomEvent"), StringComparison.Ordinal);

	internal static readonly SearchValues<string> s_indexOfString_B533A93BE50ABB795514736D5E79D92D8E506951A2CF09F7DE9EB6F3792A5E87 = SearchValues.Create(new ReadOnlySpan<string>("<local:CustomEventService.EventType"), StringComparison.Ordinal);

	internal static readonly SearchValues<string> s_indexOfString_F2F2C7CB659A76719C92ED5AB6C67D5C3152B843BDAF8DEF0200ED232533E17A = SearchValues.Create(new ReadOnlySpan<string>("local:CustomEventService.EventType"), StringComparison.Ordinal);

	internal static readonly SearchValues<string> s_indexOfString_F6DA3C8354EE118ADB75C86D460C767946D3C1FB9040AD4C8A800EC7F79E0651 = SearchValues.Create(new ReadOnlySpan<string>("<local:CustomEvent.Type"), StringComparison.Ordinal);

	private static ReadOnlySpan<byte> WordCharBitmap => new byte[16]
	{
		0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
		255, 135, 254, 255, 255, 7
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfNonAsciiOrAny_CBE28BB3DFB3CF3EF41E993BE2E6352647549C825825AA683DE00E08AF5509AA(this ReadOnlySpan<char> span)
	{
		int num = span.IndexOfAnyExcept(s_ascii_FFFFFFFFEFFF00FC01000078010000F8);
		if ((uint)num < (uint)span.Length)
		{
			if (char.IsAscii(span[num]))
			{
				return num;
			}
			do
			{
				char c;
				if (((c = span[num]) < '\u0080') ? (("\0\0\u0010Ͽ\ufffe蟿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\u0002\n$%\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0"))
				{
					return num;
				}
				num++;
			}
			while ((uint)num < (uint)span.Length);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsBoundaryWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharBitmap = WordCharBitmap;
		int num = (int)ch >> 3;
		if ((uint)num < (uint)wordCharBitmap.Length)
		{
			return (wordCharBitmap[num] & (1 << (ch & 7))) != 0;
		}
		bool flag = (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		if (!flag)
		{
			bool flag2 = ((ch == '\u200c' || ch == '\u200d') ? true : false);
			flag = flag2;
		}
		return flag;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsPostWordCharBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		if ((uint)index < (uint)inputSpan.Length)
		{
			return !IsBoundaryWordChar(inputSpan[index]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsPreWordCharBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		if ((uint)num < (uint)inputSpan.Length)
		{
			return !IsBoundaryWordChar(inputSpan[num]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharBitmap = WordCharBitmap;
		int num = (int)ch >> 3;
		if ((uint)num >= (uint)wordCharBitmap.Length)
		{
			return (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		}
		return (wordCharBitmap[num] & (1 << (ch & 7))) != 0;
	}
}
