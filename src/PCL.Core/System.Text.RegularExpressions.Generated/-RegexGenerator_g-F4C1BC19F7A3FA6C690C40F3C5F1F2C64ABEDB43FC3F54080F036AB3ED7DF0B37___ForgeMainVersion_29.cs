using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeMainVersion_29 : Regex
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
				int num6 = 0;
				int num7 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num8 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num9 = pos;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '-')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				num7 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num7++;
					int num10 = num;
					num3 = num;
					int i;
					for (i = 0; num > i && char.IsAsciiDigit(inputSpan[num - i - 1]); i++)
					{
					}
					num -= i;
					num4 = num;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num3, num4, Crawlpos());
						if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
						{
							num--;
							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
							{
								num--;
								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 112)
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == '_')
									{
										num--;
										Capture(1, num10, num);
										_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num10);
										if (num7 == 0)
										{
											break;
										}
										goto IL_024b;
									}
								}
							}
						}
						goto IL_00bf;
						IL_00bf:
						UncaptureUntil(runstack[--pos]);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out num4, out num3);
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num3 > num4)
						{
							num = ++num4;
							continue;
						}
						if (--num7 < 0)
						{
							UncaptureUntil(0);
							return false;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_024b;
						IL_024b:
						num5 = num;
						int j;
						for (j = 0; num > j; j++)
						{
							uint num11;
							if ((int)((uint)(-1074790400 << (int)(short)(num11 = (ushort)(inputSpan[num - j - 1] - 46))) & (num11 - 32)) >= 0)
							{
								break;
							}
						}
						if (j != 0)
						{
							num -= j;
							num6 = num;
							num5--;
							while (true)
							{
								num2 = Crawlpos();
								if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == ':')
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
									{
										num--;
										if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 103)
										{
											num--;
											if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
											{
												num--;
												if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 111)
												{
													num--;
													if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 102)
													{
														num--;
														pos = num9;
														num = num8;
														span = inputSpan.Slice(num);
														int num12 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiDigitsAndDot);
														if (num12 < 0)
														{
															num12 = span.Length;
														}
														if (num12 == 0)
														{
															UncaptureUntil(0);
															return false;
														}
														span = span.Slice(num12);
														Capture(0, start, runtextpos = num + num12);
														return true;
													}
												}
											}
										}
									}
								}
								UncaptureUntil(num2);
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num5 <= num6)
								{
									break;
								}
								num = ++num6;
							}
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num7 != 0)
						{
							num10 = runstack[--pos];
							goto IL_00bf;
						}
						UncaptureUntil(0);
						return false;
					}
				}
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeMainVersion_29 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeMainVersion_29();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeMainVersion_29()
	{
		pattern = "(?<=forge:[0-9\\.]+(_pre[0-9]*)?\\-)[0-9\\.]+";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
