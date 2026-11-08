using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CleanroomVersion_28 : Regex
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
				int arg = 0;
				int arg2 = 0;
				int num6 = 0;
				int num7 = 0;
				int pos = 0;
				int num8 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num9 = num;
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				for (int i = 0; i < 2; i++)
				{
					if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
					{
						UncaptureUntil(0);
						return false;
					}
					num--;
				}
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
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
				for (int j = 0; j < 2; j++)
				{
					if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
					{
						UncaptureUntil(0);
						return false;
					}
					num--;
				}
				for (int k = 0; k < 2; k++)
				{
					if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
					{
						UncaptureUntil(0);
						return false;
					}
					num--;
				}
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
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
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				Capture(1, num2, num);
				num = num9;
				span = inputSpan.Slice(num);
				num4 = num;
				int num10 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiDigitsAndDot);
				if (num10 < 0)
				{
					num10 = span.Length;
				}
				if (num10 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num10);
				num += num10;
				num5 = num;
				num4++;
				while (true)
				{
					num3 = Crawlpos();
					num6 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num6++;
						int num11 = num;
						if ((uint)span.Length >= 7u && span[0] == '+' && span.Slice(1).StartsWith("build", StringComparison.OrdinalIgnoreCase) && span[6] != '\n')
						{
							num += 7;
							span = inputSpan.Slice(num);
							arg = num;
							int num12 = span.IndexOfAnyExceptInRange('0', '9');
							if (num12 < 0)
							{
								num12 = span.Length;
							}
							if (num12 != 0)
							{
								span = span.Slice(num12);
								num += num12;
								arg2 = num;
								arg++;
								goto IL_0618;
							}
						}
						goto IL_0668;
						IL_06ba:
						num8 = pos;
						num7 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
							num7++;
							int start2 = num;
							if ((uint)span.Length >= 6u && span.StartsWith("-alpha", StringComparison.OrdinalIgnoreCase))
							{
								num += 6;
								span = inputSpan.Slice(num);
								Capture(3, start2, num);
								if (num7 == 0)
								{
									continue;
								}
							}
							else
							{
								if (--num7 < 0)
								{
									break;
								}
								num = runstack[--pos];
								UncaptureUntil(runstack[--pos]);
								span = inputSpan.Slice(num);
							}
							pos = num8;
							runtextpos = num;
							Capture(0, start, num);
							return true;
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num6 != 0)
						{
							num11 = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (arg < arg2)
							{
								num = --arg2;
								span = inputSpan.Slice(num);
								goto IL_0618;
							}
							goto IL_0668;
						}
						break;
						IL_0618:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
						Capture(2, num11, num);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num11);
						if (num6 == 0)
						{
							continue;
						}
						goto IL_06ba;
						IL_0668:
						if (--num6 < 0)
						{
							break;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_06ba;
					}
					UncaptureUntil(num3);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num4 >= num5)
					{
						break;
					}
					num = --num5;
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CleanroomVersion_28 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CleanroomVersion_28();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CleanroomVersion_28()
	{
		pattern = "(?<=(com.cleanroommc:cleanroom:))[0-9\\.]+(\\+build.[0-9]+)?(-alpha)?";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 4;
	}
}
