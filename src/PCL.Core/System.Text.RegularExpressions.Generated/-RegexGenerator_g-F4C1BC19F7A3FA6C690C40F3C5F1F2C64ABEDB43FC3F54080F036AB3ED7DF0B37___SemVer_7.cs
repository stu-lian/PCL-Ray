using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___SemVer_7 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 5 && num == 0)
				{
					return true;
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
				int capturePosition = 0;
				int num8 = 0;
				int num9 = 0;
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int arg7 = 0;
				int arg8 = 0;
				int arg9 = 0;
				int arg10 = 0;
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				if (!span.IsEmpty && span[0] == 'v')
				{
					span = span.Slice(1);
					num++;
				}
				num5 = num;
				if (span.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (span[0])
				{
				case '0':
					num++;
					span = inputSpan.Slice(num);
					break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					int i;
					for (i = 1; (uint)i < (uint)span.Length && char.IsDigit(span[i]); i++)
					{
					}
					span = span.Slice(i);
					num += i;
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(1, num5, num);
				if (span.IsEmpty || span[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num6 = num;
				if (span.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (span[0])
				{
				case '0':
					num++;
					span = inputSpan.Slice(num);
					break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					int j;
					for (j = 1; (uint)j < (uint)span.Length && char.IsDigit(span[j]); j++)
					{
					}
					span = span.Slice(j);
					num += j;
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(2, num6, num);
				if (span.IsEmpty || span[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num7 = num;
				num4 = num;
				num3 = Crawlpos();
				if (span.IsEmpty || span[0] != '0')
				{
					goto IL_027d;
				}
				num2 = 0;
				num++;
				span = inputSpan.Slice(num);
				goto IL_035e;
				IL_035e:
				while (true)
				{
					Capture(3, num7, num);
					num10 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num10++;
						int num14;
						int arg11;
						int arg12;
						if (!span.IsEmpty && span[0] == '-')
						{
							num++;
							span = inputSpan.Slice(num);
							num14 = num;
							arg11 = num;
							arg12 = Crawlpos();
							if (span.IsEmpty || span[0] != '0')
							{
								goto IL_03f8;
							}
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg11, arg12);
							num++;
							span = inputSpan.Slice(num);
							goto IL_0676;
						}
						goto IL_0a35;
						IL_07c3:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg5, arg6, Crawlpos());
						int arg13;
						int arg14;
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg13, arg14);
						goto IL_0679;
						IL_08b9:
						UncaptureUntil(runstack[--pos]);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg8, out arg7);
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (arg7 < arg8)
						{
							num = --arg8;
							span = inputSpan.Slice(num);
							goto IL_0906;
						}
						goto IL_097a;
						IL_04f5:
						num = arg11;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg12);
						int k;
						for (k = 0; (uint)k < (uint)span.Length && char.IsDigit(span[k]); k++)
						{
						}
						span = span.Slice(k);
						num += k;
						char c;
						if (!span.IsEmpty && (c = span[0]) < '{' && ("\0\0\u2000\0\ufffe߿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0)
						{
							num++;
							span = inputSpan.Slice(num);
							arg3 = num;
							int num15 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDash);
							if (num15 < 0)
							{
								num15 = span.Length;
							}
							span = span.Slice(num15);
							num += num15;
							arg4 = num;
							goto IL_060a;
						}
						goto IL_0a35;
						IL_0635:
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg12, out arg11);
						switch (runstack[--pos])
						{
						case 0:
							break;
						case 1:
							goto IL_047d;
						case 2:
							goto IL_05bd;
						default:
							goto IL_0676;
						}
						goto IL_03f8;
						IL_0a35:
						if (--num10 < 0)
						{
							break;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_0a87;
						IL_060a:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 2, arg11, arg12);
						goto IL_0676;
						IL_0a87:
						num12 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
							num12++;
							int num16;
							if (!span.IsEmpty && span[0] == '+')
							{
								num++;
								span = inputSpan.Slice(num);
								num16 = num;
								arg9 = num;
								int num17 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDash);
								if (num17 < 0)
								{
									num17 = span.Length;
								}
								if (num17 != 0)
								{
									span = span.Slice(num17);
									num += num17;
									arg10 = num;
									arg9++;
									goto IL_0b5f;
								}
							}
							goto IL_0c91;
							IL_0b5f:
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg9, arg10, Crawlpos());
							num13 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
								num13++;
								if (span.IsEmpty || span[0] != '.')
								{
									break;
								}
								int num18 = span.Slice(1).IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDash);
								if (num18 < 0)
								{
									num18 = span.Length - 1;
								}
								if (num18 == 0)
								{
									break;
								}
								span = span.Slice(num18);
								num += num18;
								num++;
								span = inputSpan.Slice(num);
							}
							goto IL_0bf1;
							IL_0c91:
							if (--num12 < 0)
							{
								break;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							goto IL_0ce3;
							IL_0ce3:
							if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
							{
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num12 != 0)
								{
									num16 = runstack[--pos];
									num13 = runstack[--pos];
									if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									goto IL_0bf1;
								}
								break;
							}
							runtextpos = num;
							Capture(0, start, num);
							return true;
							IL_0bf1:
							if (--num13 < 0)
							{
								UncaptureUntil(runstack[--pos]);
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg10, out arg9);
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (arg9 < arg10)
								{
									num = --arg10;
									span = inputSpan.Slice(num);
									goto IL_0b5f;
								}
								goto IL_0c91;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num13);
							Capture(5, num16, num);
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num16);
							if (num12 == 0)
							{
								continue;
							}
							goto IL_0ce3;
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num10 != 0)
						{
							num14 = runstack[--pos];
							num11 = runstack[--pos];
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num11 != 0)
							{
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg14, out arg13);
								switch (runstack[--pos])
								{
								case 0:
									goto IL_06f3;
								case 1:
									goto IL_0779;
								case 2:
									goto IL_08b9;
								}
								goto IL_0679;
							}
							goto IL_0635;
						}
						break;
						IL_0779:
						UncaptureUntil(runstack[--pos]);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg6, out arg5);
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (arg5 < arg6)
						{
							num = --arg6;
							span = inputSpan.Slice(num);
							goto IL_07c3;
						}
						goto IL_07f1;
						IL_03f8:
						num = arg11;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg12);
						if (!span.IsEmpty && char.IsBetween(span[0], '1', '9'))
						{
							num++;
							span = inputSpan.Slice(num);
							arg = num;
							int l;
							for (l = 0; (uint)l < (uint)span.Length && char.IsDigit(span[l]); l++)
							{
							}
							span = span.Slice(l);
							num += l;
							arg2 = num;
							goto IL_04c7;
						}
						goto IL_04f5;
						IL_04c7:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg11, arg12);
						goto IL_0676;
						IL_05bd:
						UncaptureUntil(runstack[--pos]);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (arg3 < arg4)
						{
							num = --arg4;
							span = inputSpan.Slice(num);
							goto IL_060a;
						}
						goto IL_0a35;
						IL_0679:
						while (true)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
							num11++;
							if (span.IsEmpty || span[0] != '.')
							{
								break;
							}
							arg13 = num;
							arg14 = Crawlpos();
							if ((uint)span.Length >= 2u && span[1] == '0')
							{
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg13, arg14);
								num += 2;
								span = inputSpan.Slice(num);
								continue;
							}
							goto IL_06f3;
						}
						goto IL_097a;
						IL_0676:
						num11 = 0;
						goto IL_0679;
						IL_06f3:
						num = arg13;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg14);
						if ((uint)span.Length >= 2u && char.IsBetween(span[1], '1', '9'))
						{
							num += 2;
							span = inputSpan.Slice(num);
							arg5 = num;
							int m;
							for (m = 0; (uint)m < (uint)span.Length && char.IsDigit(span[m]); m++)
							{
							}
							span = span.Slice(m);
							num += m;
							arg6 = num;
							goto IL_07c3;
						}
						goto IL_07f1;
						IL_097a:
						if (--num11 < 0)
						{
							goto IL_0635;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num11);
						Capture(4, num14, num);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num14);
						if (num10 == 0)
						{
							continue;
						}
						goto IL_0a87;
						IL_0906:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg7, arg8, Crawlpos());
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 2, arg13, arg14);
						goto IL_0679;
						IL_047d:
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
							goto IL_04c7;
						}
						goto IL_04f5;
						IL_07f1:
						num = arg13;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg14);
						int n;
						for (n = 1; (uint)n < (uint)span.Length && char.IsDigit(span[n]); n++)
						{
						}
						span = span.Slice(n);
						num += n;
						if (!span.IsEmpty && (c = span[0]) < '{' && ("\0\0\u2000\0\ufffe߿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0)
						{
							num++;
							span = inputSpan.Slice(num);
							arg7 = num;
							int num19 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndDash);
							if (num19 < 0)
							{
								num19 = span.Length;
							}
							span = span.Slice(num19);
							num += num19;
							arg8 = num;
							goto IL_0906;
						}
						goto IL_097a;
					}
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 == 0)
					{
						break;
					}
					if (num2 != 1)
					{
						continue;
					}
					goto IL_0305;
				}
				goto IL_027d;
				IL_0305:
				UncaptureUntil(capturePosition);
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num8 >= num9)
				{
					UncaptureUntil(0);
					return false;
				}
				num = --num9;
				span = inputSpan.Slice(num);
				goto IL_033b;
				IL_033b:
				capturePosition = Crawlpos();
				num2 = 1;
				goto IL_035e;
				IL_027d:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (span.IsEmpty || !char.IsBetween(span[0], '1', '9'))
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num8 = num;
				int num20;
				for (num20 = 0; (uint)num20 < (uint)span.Length && char.IsDigit(span[num20]); num20++)
				{
				}
				span = span.Slice(num20);
				num += num20;
				num9 = num;
				goto IL_033b;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num21)
				{
					while (Crawlpos() > num21)
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___SemVer_7 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___SemVer_7();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___SemVer_7()
	{
		pattern = "^v?(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<build>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$";
		roptions = RegexOptions.ExplicitCapture;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "build", 5 },
			{ "major", 1 },
			{ "minor", 2 },
			{ "patch", 3 },
			{ "prerelease", 4 }
		};
		capslist = new string[6] { "0", "major", "minor", "patch", "prerelease", "build" };
		capsize = 6;
	}
}
