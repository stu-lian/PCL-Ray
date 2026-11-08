using System.Buffers;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal static class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	private const int WordCategoriesMask = 262463;

	internal static readonly SearchValues<char> s_asciiDigitsAndDot = SearchValues.Create(".0123456789");

	internal static readonly SearchValues<char> s_asciiExceptDigits = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*+,-./:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f");

	internal static readonly SearchValues<char> s_asciiLetters = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

	internal static readonly SearchValues<char> s_asciiLettersAndDigitsAndDash = SearchValues.Create("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

	internal static readonly SearchValues<char> s_asciiLettersAndDigitsAndDashDotUnderscoreKelvinSign = SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyzK");

	internal static readonly SearchValues<char> s_asciiLettersAndDigitsAndDashUnderscoreKelvinSign = SearchValues.Create("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyzK");

	internal static readonly SearchValues<char> s_asciiLettersAndDigitsAndKelvinSign = SearchValues.Create("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzK");

	internal static readonly SearchValues<char> s_ascii_100000000000000FEFFFF07 = SearchValues.Create(" abcdefghijklmnopqrstuvwxyz");

	internal static readonly SearchValues<char> s_ascii_40FF077E0000007E000000 = SearchValues.Create(".0123456789:ABCDEFabcdef");

	internal static readonly SearchValues<char> s_ascii_48400D40000001000000010 = SearchValues.Create("\"*/:<>?\\|");

	internal static readonly SearchValues<char> s_ascii_48FF030000000000000000 = SearchValues.Create("+.0123456789");

	internal static readonly SearchValues<char> s_ascii_FFFFFFFFFF9700FCFFFFFFFFFFFFFFFF = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*,/:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f");

	internal static readonly SearchValues<string> s_indexOfAnyStrings_OrdinalIgnoreCase_49ECD0753A256AA8ED2013964D4FC652AAF2522227A92945DA4473B2CF1F072E;

	internal static readonly SearchValues<string> s_indexOfString_1C17126F8C84A9000B9ABA4E9B40C4E0511DF17323E9917370F4CBDA2CD09602;

	internal static readonly SearchValues<string> s_indexOfString_1CCE0727369099930FE05CB0BBF57386A8EBA8A63F463FF5080C4AF5416F69DD;

	internal static readonly SearchValues<string> s_indexOfString_3FDDD2F495331D5070149F9D7128BE1B66022924EDBD0217D550055E9CBD1E9A;

	internal static readonly SearchValues<string> s_indexOfString_5D61FA9348748C1296FEB9F2A6F36DAB8AEC23B3F4B6625D9F2B92845A9925A2;

	internal static readonly SearchValues<string> s_indexOfString_bearer_OrdinalIgnoreCase;

	internal static readonly SearchValues<string> s_indexOfString_lwjgl_OrdinalIgnoreCase;

	internal static readonly SearchValues<char> s_nonAscii_10E4EA2219CF5AA8C94D1BDCF39F541399EFD76AB9C6C906C8EB478A2A307567;

	internal static readonly SearchValues<char> s_nonAscii_F9A6E3099A06C2F88B9C37A801C1898ECAA75390A63D9907AAD098D41F538907;

	private static ReadOnlySpan<byte> WordCharBitmap => new byte[16]
	{
		0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
		255, 135, 254, 255, 255, 7
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfNonAsciiOrAny_2D817A11655641E09039B9CEF8FF696999614A6D1CD036F8C96490A245B60E9F(this ReadOnlySpan<char> span)
	{
		int num = span.IndexOfAnyExcept(s_asciiExceptDigits);
		if ((uint)num < (uint)span.Length)
		{
			if (char.IsAscii(span[num]))
			{
				return num;
			}
			do
			{
				char c;
				if (((c = span[num]) < '\u0080') ? char.IsAsciiDigit(c) : RegexRunner.CharInClass(c, "\0\u0002\u000112\t"))
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
	internal static int IndexOfNonAsciiOrAny_2D9E8074DDB4840DBEF3653AD14687473D327465C13ACF6531B3622B9518E6A3(this ReadOnlySpan<char> span)
	{
		int num = span.IndexOfAnyExcept(s_ascii_FFFFFFFFFF9700FCFFFFFFFFFFFFFFFF);
		if ((uint)num < (uint)span.Length)
		{
			if (char.IsAscii(span[num]))
			{
				return num;
			}
			do
			{
				char c;
				if (((c = span[num]) < '\u0080') ? (("\0\0栀Ͽ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\u0004\u0001+,-/\t"))
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
	internal static void StackPop(int[] stack, ref int pos, out int arg0, out int arg1)
	{
		arg0 = stack[--pos];
		arg1 = stack[--pos];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)num < (uint)array.Length)
		{
			array[num] = arg0;
			pos++;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg1)
		{
			Array.Resize(ref reference, reference2 * 2);
			StackPush(ref reference, ref reference2, arg1);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 1) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			pos += 2;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg2, int arg3)
		{
			Array.Resize(ref reference, (reference2 + 1) * 2);
			StackPush(ref reference, ref reference2, arg2, arg3);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 2) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			array[num + 2] = arg2;
			pos += 3;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1, arg2);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg3, int arg4, int arg5)
		{
			Array.Resize(ref reference, (reference2 + 2) * 2);
			StackPush(ref reference, ref reference2, arg3, arg4, arg5);
		}
	}

	static _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities()
	{
		InlineArray5<string> buffer = default;
		buffer[0] = "access";
		buffer[1] = "refresh";
		buffer[2] = "client";
		buffer[3] = "password";
		buffer[4] = "authoriz";
		s_indexOfAnyStrings_OrdinalIgnoreCase_49ECD0753A256AA8ED2013964D4FC652AAF2522227A92945DA4473B2CF1F072E = SearchValues.Create(buffer, StringComparison.OrdinalIgnoreCase);
		s_indexOfString_1C17126F8C84A9000B9ABA4E9B40C4E0511DF17323E9917370F4CBDA2CD09602 = SearchValues.Create(new ReadOnlySpan<string>("[MOTD]"), StringComparison.Ordinal);
		s_indexOfString_1CCE0727369099930FE05CB0BBF57386A8EBA8A63F463FF5080C4AF5416F69DD = SearchValues.Create(new ReadOnlySpan<string>("[AD]"), StringComparison.Ordinal);
		s_indexOfString_3FDDD2F495331D5070149F9D7128BE1B66022924EDBD0217D550055E9CBD1E9A = SearchValues.Create(new ReadOnlySpan<string>("t24:00"), StringComparison.OrdinalIgnoreCase);
		s_indexOfString_5D61FA9348748C1296FEB9F2A6F36DAB8AEC23B3F4B6625D9F2B92845A9925A2 = SearchValues.Create(new ReadOnlySpan<string>("\\lastpending"), StringComparison.OrdinalIgnoreCase);
		s_indexOfString_bearer_OrdinalIgnoreCase = SearchValues.Create(new ReadOnlySpan<string>("bearer"), StringComparison.OrdinalIgnoreCase);
		s_indexOfString_lwjgl_OrdinalIgnoreCase = SearchValues.Create(new ReadOnlySpan<string>("lwjgl"), StringComparison.OrdinalIgnoreCase);
		s_nonAscii_10E4EA2219CF5AA8C94D1BDCF39F541399EFD76AB9C6C906C8EB478A2A307567 = SearchValues.Create("+-./0123456789=ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~K");
		s_nonAscii_F9A6E3099A06C2F88B9C37A801C1898ECAA75390A63D9907AAD098D41F538907 = SearchValues.Create("+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyzK");
	}
}
