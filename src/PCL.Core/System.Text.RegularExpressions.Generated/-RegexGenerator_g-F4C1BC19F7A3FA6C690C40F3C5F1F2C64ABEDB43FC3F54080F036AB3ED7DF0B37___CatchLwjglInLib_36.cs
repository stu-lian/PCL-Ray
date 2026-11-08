using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CatchLwjglInLib_36 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 6)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_indexOfString_lwjgl_OrdinalIgnoreCase);
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int pos = 0;
				int num7 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num8 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != ':')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 108)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 106)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 119)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 108)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '\n')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				num = num8;
				span = inputSpan.Slice(num);
				if ((uint)span.Length < 5u || !span.StartsWith("lwjgl", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 5;
				span = inputSpan.Slice(num);
				num2 = num;
				if (span.IsEmpty || span[0] != '-')
				{
					UncaptureUntil(0);
					return false;
				}
				int num9 = span.Slice(1).IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDashDotUnderscoreKelvinSign);
				if (num9 < 0)
				{
					num9 = span.Length - 1;
				}
				span = span.Slice(num9);
				num += num9;
				num++;
				span = inputSpan.Slice(num);
				Capture(1, num2, num);
				span = inputSpan.Slice(num);
				int num10 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num11 = pos;
				num3 = num;
				if ((uint)span.Length < 6u || span[0] != ':' || !char.IsAsciiDigit(span[1]) || span[2] == '\n' || !char.IsAsciiDigit(span[3]) || span[4] == '\n' || !char.IsAsciiDigit(span[5]))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 6;
				span = inputSpan.Slice(num);
				num4 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num4++;
					int start2 = num;
					if (span.IsEmpty || span[0] != '-')
					{
						goto IL_03cd;
					}
					int num12 = span.Slice(1).IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDashDotUnderscoreKelvinSign);
					if (num12 < 0)
					{
						num12 = span.Length - 1;
					}
					span = span.Slice(num12);
					num += num12;
					num++;
					span = inputSpan.Slice(num);
					Capture(3, start2, num);
					if (num4 == 0)
					{
						continue;
					}
					goto IL_040d;
					IL_040d:
					if (!span.IsEmpty && span[0] == ':')
					{
						break;
					}
					goto IL_03cd;
					IL_03cd:
					if (--num4 < 0)
					{
						UncaptureUntil(0);
						return false;
					}
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					goto IL_040d;
				}
				num++;
				span = inputSpan.Slice(num);
				num7 = pos;
				num6 = num;
				num5 = 0;
				do
				{
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num6, num);
					num6 = num;
					num5++;
					int start3 = num;
					int num13 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDashDotUnderscoreKelvinSign);
					if (num13 < 0)
					{
						num13 = span.Length;
					}
					span = span.Slice(num13);
					num += num13;
					Capture(4, start3, num);
				}
				while (num != num6 && num5 == 0);
				pos = num7;
				Capture(2, num3, num);
				pos = num11;
				num = num10;
				span = inputSpan.Slice(num);
				runtextpos = num;
				Capture(0, start, num);
				return true;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CatchLwjglInLib_36 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CatchLwjglInLib_36();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CatchLwjglInLib_36()
	{
		pattern = "(?<=org.lwjgl:)lwjgl(-[a-z._.\\-.0-9]*)(?=(:[0-9].[0-9].[0-9](-[a-z.0-9._.\\-]*)?:([a-z._.\\-.0-9]*)?))";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 5;
	}
}
