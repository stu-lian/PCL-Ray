using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___EnglishSpacedKeywords_22 : Regex
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
				if ((uint)num < (uint)inputSpan.Length)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLetters);
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
				int capturePosition = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int num11 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num5 = num;
				num4 = num;
				num3 = Crawlpos();
				num7 = num;
				int num12 = span.IndexOfAnyExceptInRange('A', 'Z');
				if (num12 < 0)
				{
					num12 = span.Length;
				}
				if (num12 != 0)
				{
					span = span.Slice(num12);
					num += num12;
					num8 = num;
					num7++;
					goto IL_00a9;
				}
				goto IL_00b5;
				IL_0125:
				while (true)
				{
					Capture(1, num5, num);
					int num13 = num;
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					int num14 = num11;
					int num15 = span.IndexOfAnyExceptInRange('A', 'Z');
					if (num15 < 0)
					{
						num15 = span.Length;
					}
					if (num15 != 0)
					{
						span = span.Slice(num15);
						num += num15;
						num9 = num;
						int num16 = span.IndexOfAnyExceptInRange('a', 'z');
						if (num16 < 0)
						{
							num16 = span.Length;
						}
						if (num16 != 0)
						{
							span = span.Slice(num16);
							num += num16;
							num10 = num;
							num9++;
							num6 = Crawlpos();
							int num17 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_ascii_100000000000000FEFFFF07);
							if (num17 < 0)
							{
								num17 = span.Length;
							}
							span = span.Slice(num17);
							num += num17;
							num11 = num14;
							num = num13;
							span = inputSpan.Slice(num);
							runtextpos = num;
							Capture(0, start, num);
							return true;
						}
					}
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 == 0)
					{
						break;
					}
					if (num2 == 1)
					{
						UncaptureUntil(0);
						return false;
					}
				}
				UncaptureUntil(capturePosition);
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num7 < num8)
				{
					num = --num8;
					span = inputSpan.Slice(num);
					goto IL_00a9;
				}
				goto IL_00b5;
				IL_00a9:
				capturePosition = Crawlpos();
				num2 = 0;
				goto IL_0125;
				IL_00b5:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				int num18 = span.IndexOfAnyExceptInRange('a', 'z');
				if (num18 < 0)
				{
					num18 = span.Length;
				}
				if (num18 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num18);
				num += num18;
				num2 = 1;
				goto IL_0125;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num19)
				{
					while (Crawlpos() > num19)
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___EnglishSpacedKeywords_22 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___EnglishSpacedKeywords_22();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___EnglishSpacedKeywords_22()
	{
		pattern = "([A-Z]+|[a-z]+?)(?=[A-Z]+[a-z]+[a-z ]*)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
