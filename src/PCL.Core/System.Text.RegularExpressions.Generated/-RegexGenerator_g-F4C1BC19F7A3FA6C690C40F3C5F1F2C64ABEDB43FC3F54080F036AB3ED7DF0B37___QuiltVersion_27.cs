using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QuiltVersion_27 : Regex
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
				if (num <= inputSpan.Length - 8)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiDigitsAndDot);
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
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num10 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				num2 = num;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != ':')
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 100)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 108)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '-')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 116)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 117)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 113)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != ':')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 116)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 117)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 113)
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
				Capture(1, num2, num);
				num = num10;
				span = inputSpan.Slice(num);
				num7 = num;
				int num11 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiDigitsAndDot);
				if (num11 < 0)
				{
					num11 = span.Length;
				}
				if (num11 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num11);
				num += num11;
				num8 = num;
				num7++;
				while (true)
				{
					num6 = Crawlpos();
					num9 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num9++;
						int start2 = num;
						if ((uint)span.Length >= 7u && span[0] == '+' && span.Slice(1).StartsWith("build", StringComparison.OrdinalIgnoreCase) && span[6] != '\n')
						{
							int num12 = span.Slice(7).IndexOfAnyExceptInRange('0', '9');
							if (num12 < 0)
							{
								num12 = span.Length - 7;
							}
							if (num12 != 0)
							{
								span = span.Slice(num12);
								num += num12;
								num += 7;
								span = inputSpan.Slice(num);
								Capture(2, start2, num);
								if (num9 == 0)
								{
									continue;
								}
								goto IL_062f;
							}
						}
						goto IL_05f5;
						IL_05f5:
						if (--num9 < 0)
						{
							break;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_062f;
						IL_062f:
						num3 = num;
						num4 = num;
						if ((uint)span.Length >= 6u && span.StartsWith("-beta", StringComparison.OrdinalIgnoreCase) && span[5] != '\n')
						{
							num += 6;
							span = inputSpan.Slice(num);
							Capture(4, num4, num);
							if (!span.IsEmpty && char.IsAsciiDigit(span[0]))
							{
								num++;
								span = inputSpan.Slice(num);
								num5 = num;
								if (!span.IsEmpty && char.IsAsciiDigit(span[0]))
								{
									span = span.Slice(1);
									num++;
								}
								Capture(5, num5, num);
								Capture(3, num3, num);
								runtextpos = num;
								Capture(0, start, num);
								return true;
							}
						}
						goto IL_05f5;
					}
					UncaptureUntil(num6);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num7 >= num8)
					{
						break;
					}
					num = --num8;
					span = inputSpan.Slice(num);
				}
				UncaptureUntil(0);
				return false;
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QuiltVersion_27 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QuiltVersion_27();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QuiltVersion_27()
	{
		pattern = "(?<=(org.quiltmc:quilt-loader:))[0-9\\.]+(\\+build.[0-9]+)?((-beta.)[0-9]([0-9]?))";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 6;
	}
}
