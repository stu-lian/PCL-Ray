using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FullHttpUri_9 : Regex
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
				if (num <= inputSpan.Length - 10 && num == 0)
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
				int capturePosition = 0;
				int capturePosition2 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int num11 = 0;
				int capturePosition3 = 0;
				int capturePosition4 = 0;
				int capturePosition5 = 0;
				int num12 = 0;
				int num13 = 0;
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int num14 = 0;
				int num15 = 0;
				int num16 = 0;
				int num17 = 0;
				int arg7 = 0;
				int arg8 = 0;
				int arg9 = 0;
				int arg10 = 0;
				int arg11 = 0;
				int arg12 = 0;
				int num18 = 0;
				int num19 = 0;
				int num20 = 0;
				int num21 = 0;
				int num22 = 0;
				int num23 = 0;
				int num24 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num9 = num;
				if ((uint)span.Length < 4u || !span.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				if ((uint)span.Length > 4u && (span[4] | 0x20) == 115)
				{
					span = span.Slice(1);
					num++;
				}
				num += 4;
				span = inputSpan.Slice(num);
				Capture(1, num9, num);
				if (!span.StartsWith("://"))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 3;
				span = inputSpan.Slice(num);
				num10 = num;
				num6 = num;
				num5 = Crawlpos();
				if ((uint)span.Length < 9u || !span.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0170;
				}
				num2 = 0;
				num += 9;
				span = inputSpan.Slice(num);
				goto IL_0be2;
				IL_042a:
				int arg13;
				num = arg13;
				span = inputSpan.Slice(num);
				int arg14;
				UncaptureUntil(arg14);
				num++;
				span = inputSpan.Slice(num);
				arg = num;
				if (!span.IsEmpty && span[0] == '1')
				{
					span = span.Slice(1);
					num++;
				}
				arg2 = num;
				goto IL_04c6;
				IL_05ed:
				if (--num18 >= 0)
				{
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					if (num18 != 0)
					{
						if (num18 < 3)
						{
							goto IL_05a6;
						}
						goto IL_0651;
					}
				}
				goto IL_0304;
				IL_0246:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				num12 = num;
				if (!span.IsEmpty && span[0] == '1')
				{
					span = span.Slice(1);
					num++;
				}
				num13 = num;
				goto IL_02b7;
				IL_0638:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num18 == 0)
				{
					goto IL_0304;
				}
				goto IL_05a6;
				IL_02b7:
				capturePosition3 = Crawlpos();
				int i;
				for (i = 0; i < 2 && (uint)i < (uint)span.Length && char.IsDigit(span[i]); i++)
				{
				}
				if (i == 0)
				{
					goto IL_0287;
				}
				span = span.Slice(i);
				num += i;
				num3 = 1;
				goto IL_0320;
				IL_0320:
				num18 = 0;
				goto IL_0323;
				IL_0987:
				if (--num19 < 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				span = inputSpan.Slice(num);
				if (num19 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num8 = num;
				capturePosition2 = Crawlpos();
				num14 = num;
				int j;
				char c;
				for (j = 0; j < 63 && (uint)j < (uint)span.Length; j++)
				{
					if (!(((c = span[j]) < '\u0080') ? char.IsAsciiLetter(c) : RegexRunner.CharInClass(c, "\0\u0006\0A[a{KÅ")))
					{
						break;
					}
				}
				if (j >= 2)
				{
					span = span.Slice(j);
					num += j;
					num15 = num;
					num14 += 2;
					goto IL_0a90;
				}
				goto IL_0aa0;
				IL_0170:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				num7 = num;
				capturePosition = Crawlpos();
				if (!span.IsEmpty && span[0] == '2' && (uint)span.Length >= 2u)
				{
					switch (span[1])
					{
					case '5':
						if ((uint)span.Length < 3u || !char.IsBetween(span[2], '0', '5'))
						{
							break;
						}
						num += 3;
						span = inputSpan.Slice(num);
						goto IL_023e;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
						{
							if ((uint)span.Length < 3u || !char.IsDigit(span[2]))
							{
								break;
							}
							num += 3;
							span = inputSpan.Slice(num);
							goto IL_023e;
						}
						IL_023e:
						num3 = 0;
						goto IL_0320;
					}
				}
				goto IL_0246;
				IL_092e:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num20);
				if (!span.IsEmpty && span[0] == '.')
				{
					num++;
					span = inputSpan.Slice(num);
					goto IL_0714;
				}
				goto IL_093f;
				IL_0be2:
				int num28;
				while (true)
				{
					Capture(2, num10, num);
					num21 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num21++;
						int num25;
						int arg15;
						int arg16;
						int arg17;
						int arg18;
						int arg19;
						int arg20;
						if (!span.IsEmpty && span[0] == ':')
						{
							num++;
							span = inputSpan.Slice(num);
							num25 = num;
							arg15 = num;
							arg16 = Crawlpos();
							if (!span.IsEmpty && span[0] == '6')
							{
								arg17 = num;
								arg18 = Crawlpos();
								if ((uint)span.Length >= 2u && span[1] == '5')
								{
									arg19 = num;
									arg20 = Crawlpos();
									if ((uint)span.Length >= 3u && span[2] == '5' && (uint)span.Length >= 4u)
									{
										switch (span[3])
										{
										case '3':
											if ((uint)span.Length < 5u || !char.IsBetween(span[4], '0', '5'))
											{
												break;
											}
											num += 5;
											span = inputSpan.Slice(num);
											goto IL_0d37;
										case '0':
										case '1':
										case '2':
											{
												if ((uint)span.Length < 5u || !char.IsDigit(span[4]))
												{
													break;
												}
												num += 5;
												span = inputSpan.Slice(num);
												goto IL_0d37;
											}
											IL_0d37:
											_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg19, arg20);
											goto IL_0e06;
										}
									}
									goto IL_0d4e;
								}
								goto IL_0e1d;
							}
							goto IL_0eff;
						}
						goto IL_1110;
						IL_1162:
						num22 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
							num22++;
							int num26 = num;
							if (!span.IsEmpty && span[0] == '/')
							{
								num++;
								span = inputSpan.Slice(num);
								arg9 = num;
								int k;
								for (k = 0; (uint)k < (uint)span.Length; k++)
								{
									if (!(((c = span[k]) < '\u0080') ? (("쇿\uffff\ufff6翿\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\u0004\u0001#$?@d")))
									{
										break;
									}
								}
								span = span.Slice(k);
								num += k;
								arg10 = num;
								goto IL_1260;
							}
							goto IL_12b0;
							IL_1302:
							num23 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
								num23++;
								int num27;
								if (!span.IsEmpty && span[0] == '?')
								{
									num++;
									span = inputSpan.Slice(num);
									num27 = num;
									arg11 = num;
									int l;
									for (l = 0; (uint)l < (uint)span.Length; l++)
									{
										if (!(((c = span[l]) < '\u0080') ? (("쇿\uffff\ufff6\uffff\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\u0002\u0001#$d")))
										{
											break;
										}
									}
									span = span.Slice(l);
									num += l;
									arg12 = num;
									goto IL_1400;
								}
								goto IL_1450;
								IL_14a2:
								num24 = 0;
								while (true)
								{
									_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
									num24++;
									if (span.IsEmpty || span[0] != '#')
									{
										goto IL_1530;
									}
									num++;
									span = inputSpan.Slice(num);
									int start2 = num;
									int m;
									for (m = 0; (uint)m < (uint)span.Length && !char.IsWhiteSpace(span[m]); m++)
									{
									}
									span = span.Slice(m);
									num += m;
									Capture(7, start2, num);
									if (num24 == 0)
									{
										continue;
									}
									goto IL_156a;
									IL_1530:
									if (--num24 < 0)
									{
										break;
									}
									num = runstack[--pos];
									UncaptureUntil(runstack[--pos]);
									span = inputSpan.Slice(num);
									goto IL_156a;
									IL_156a:
									if (num >= inputSpan.Length - 1 && ((uint)num >= (uint)inputSpan.Length || inputSpan[num] == '\n'))
									{
										runtextpos = num;
										Capture(0, start, num);
										return true;
									}
									goto IL_1530;
								}
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num23 != 0)
								{
									num27 = runstack[--pos];
									UncaptureUntil(runstack[--pos]);
									_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg12, out arg11);
									if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (arg11 < arg12)
									{
										num = --arg12;
										span = inputSpan.Slice(num);
										goto IL_1400;
									}
									goto IL_1450;
								}
								break;
								IL_1450:
								if (--num23 < 0)
								{
									break;
								}
								num = runstack[--pos];
								UncaptureUntil(runstack[--pos]);
								span = inputSpan.Slice(num);
								goto IL_14a2;
								IL_1400:
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg11, arg12, Crawlpos());
								Capture(6, num27, num);
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num27);
								if (num23 == 0)
								{
									continue;
								}
								goto IL_14a2;
							}
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num22 != 0)
							{
								num26 = runstack[--pos];
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
									goto IL_1260;
								}
								goto IL_12b0;
							}
							break;
							IL_12b0:
							if (--num22 < 0)
							{
								break;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							goto IL_1302;
							IL_1260:
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg9, arg10, Crawlpos());
							Capture(5, num26, num);
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num26);
							if (num22 == 0)
							{
								continue;
							}
							goto IL_1302;
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num21 == 0)
						{
							break;
						}
						num25 = runstack[--pos];
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg16, out arg15);
						switch (runstack[--pos])
						{
						case 0:
							break;
						case 1:
							goto IL_0f97;
						case 2:
							goto IL_1021;
						default:
							goto IL_10da;
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg18, out arg17);
						num28 = runstack[--pos];
						if (num28 == 0)
						{
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg20, out arg19);
							num28 = runstack[--pos];
							if (num28 == 0)
							{
								goto IL_0d4e;
							}
							if (num28 != 1)
							{
								goto IL_0e06;
							}
							goto IL_0e1d;
						}
						if (num28 != 1)
						{
							goto IL_0ee8;
						}
						goto IL_0eff;
						IL_0e06:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg17, arg18);
						goto IL_0ee8;
						IL_0ee8:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg15, arg16);
						goto IL_10da;
						IL_10da:
						Capture(4, num25, num);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num25);
						if (num21 == 0)
						{
							continue;
						}
						goto IL_1162;
						IL_106e:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg7, arg8, Crawlpos());
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 2, arg15, arg16);
						goto IL_10da;
						IL_0f97:
						num = arg15;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg16);
						if (!span.IsEmpty && char.IsBetween(span[0], '1', '9'))
						{
							num++;
							span = inputSpan.Slice(num);
							arg7 = num;
							int n;
							for (n = 0; n < 3 && (uint)n < (uint)span.Length && char.IsDigit(span[n]); n++)
							{
							}
							span = span.Slice(n);
							num += n;
							arg8 = num;
							goto IL_106e;
						}
						goto IL_1110;
						IL_0eff:
						num = arg15;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg16);
						if ((uint)span.Length < 5u || !char.IsBetween(span[0], '1', '5') || !char.IsDigit(span[1]) || !char.IsDigit(span[2]) || !char.IsDigit(span[3]) || !char.IsDigit(span[4]))
						{
							goto IL_0f97;
						}
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg15, arg16);
						num += 5;
						span = inputSpan.Slice(num);
						goto IL_10da;
						IL_0d4e:
						num = arg19;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg20);
						if ((uint)span.Length >= 5u && char.IsBetween(span[2], '0', '4') && char.IsDigit(span[3]) && char.IsDigit(span[4]))
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg19, arg20);
							num += 5;
							span = inputSpan.Slice(num);
							goto IL_0e06;
						}
						goto IL_0e1d;
						IL_1021:
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
							goto IL_106e;
						}
						goto IL_1110;
						IL_1110:
						if (--num21 < 0)
						{
							break;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_1162;
						IL_0e1d:
						num = arg17;
						span = inputSpan.Slice(num);
						UncaptureUntil(arg18);
						if ((uint)span.Length >= 5u && char.IsBetween(span[1], '0', '4') && char.IsDigit(span[2]) && char.IsDigit(span[3]) && char.IsDigit(span[4]))
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg17, arg18);
							num += 5;
							span = inputSpan.Slice(num);
							goto IL_0ee8;
						}
						goto IL_0eff;
					}
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					switch (num2)
					{
					case 0:
						break;
					case 1:
						goto IL_0638;
					case 2:
						goto IL_06fc;
					case 3:
						goto IL_0ba2;
					default:
						continue;
					}
					break;
				}
				goto IL_0170;
				IL_0aa0:
				num = num8;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition2);
				if ((uint)span.Length >= 4u && span.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
				{
					num += 4;
					span = inputSpan.Slice(num);
					num16 = num;
					int num29;
					for (num29 = 0; num29 < 59 && (uint)num29 < (uint)span.Length; num29++)
					{
						if (!(((c = span[num29]) < '\u0080') ? (("\0\0\u2000Ͽ\ufffe߿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\n\0-.0:A[a{KÅ")))
						{
							break;
						}
					}
					if (num29 >= 2)
					{
						span = span.Slice(num29);
						num += num29;
						num17 = num;
						num16 += 2;
						goto IL_0b95;
					}
				}
				goto IL_09d4;
				IL_093f:
				num20 = runstack[--pos];
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num20 != 0)
				{
					goto IL_0805;
				}
				goto IL_0987;
				IL_0479:
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
					goto IL_04c6;
				}
				goto IL_05ed;
				IL_0bbb:
				num2 = 3;
				goto IL_0be2;
				IL_057b:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg13, arg14);
				goto IL_05e3;
				IL_0805:
				UncaptureUntil(runstack[--pos]);
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg6, out arg5);
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (arg5 < arg6 && (arg6 = inputSpan.Slice(arg5, arg6 - arg5).LastIndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_asciiLettersAndDigitsAndKelvinSign)) >= 0)
				{
					arg6 += arg5;
					num = arg6;
					span = inputSpan.Slice(num);
					goto IL_0872;
				}
				if (--num20 >= 0)
				{
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					goto IL_092e;
				}
				goto IL_0987;
				IL_0ba2:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num4 == 0)
				{
					UncaptureUntil(capturePosition4);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num14 < num15)
					{
						num = --num15;
						span = inputSpan.Slice(num);
						goto IL_0a90;
					}
					goto IL_0aa0;
				}
				if (num4 == 1)
				{
					UncaptureUntil(capturePosition5);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num16 >= num17)
					{
						goto IL_09d4;
					}
					num = --num17;
					span = inputSpan.Slice(num);
					goto IL_0b95;
				}
				goto IL_0bbb;
				IL_06fc:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				num19 = 0;
				goto IL_0714;
				IL_0651:
				num2 = 1;
				goto IL_0be2;
				IL_05e3:
				if (num18 < 3)
				{
					goto IL_0323;
				}
				goto IL_0651;
				IL_0287:
				UncaptureUntil(capturePosition3);
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num12 < num13)
				{
					num = --num13;
					span = inputSpan.Slice(num);
					goto IL_02b7;
				}
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if (!span.IsEmpty && span[0] == '[')
				{
					num++;
					span = inputSpan.Slice(num);
					num11 = num;
					int num30 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_ascii_40FF077E0000007E000000);
					if (num30 < 0)
					{
						num30 = span.Length;
					}
					if (num30 != 0)
					{
						span = span.Slice(num30);
						num += num30;
						Capture(3, num11, num);
						if (!span.IsEmpty && span[0] == ']')
						{
							num2 = 2;
							num++;
							span = inputSpan.Slice(num);
							goto IL_0be2;
						}
					}
				}
				goto IL_06fc;
				IL_09d4:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num19 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				goto IL_093f;
				IL_0b95:
				capturePosition5 = Crawlpos();
				num4 = 1;
				goto IL_0bbb;
				IL_0323:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
				num18++;
				if (!span.IsEmpty && span[0] == '.')
				{
					arg13 = num;
					arg14 = Crawlpos();
					if ((uint)span.Length >= 2u && span[1] == '2' && (uint)span.Length >= 3u)
					{
						switch (span[2])
						{
						case '5':
							if ((uint)span.Length < 4u || !char.IsBetween(span[3], '0', '5'))
							{
								break;
							}
							num += 4;
							span = inputSpan.Slice(num);
							goto IL_0413;
						case '0':
						case '1':
						case '2':
						case '3':
						case '4':
							{
								if ((uint)span.Length < 4u || !char.IsDigit(span[3]))
								{
									break;
								}
								num += 4;
								span = inputSpan.Slice(num);
								goto IL_0413;
							}
							IL_0413:
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg13, arg14);
							goto IL_05e3;
						}
					}
					goto IL_042a;
				}
				goto IL_05ed;
				IL_0304:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num3 == 0)
				{
					goto IL_0246;
				}
				if (num3 == 1)
				{
					goto IL_0287;
				}
				goto IL_0320;
				IL_05a6:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg14, out arg13);
				num28 = runstack[--pos];
				if (num28 == 0)
				{
					goto IL_042a;
				}
				if (num28 == 1)
				{
					UncaptureUntil(runstack[--pos]);
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (arg3 >= arg4)
					{
						goto IL_0479;
					}
					num = --arg4;
					span = inputSpan.Slice(num);
					goto IL_057b;
				}
				goto IL_05e3;
				IL_077b:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
				num20++;
				arg5 = num;
				int num31;
				for (num31 = 0; num31 < 61 && (uint)num31 < (uint)span.Length; num31++)
				{
					if (!(((c = span[num31]) < '\u0080') ? (("\0\0\u2000Ͽ\ufffe߿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\n\0-.0:A[a{KÅ")))
					{
						break;
					}
				}
				span = span.Slice(num31);
				num += num31;
				arg6 = num;
				goto IL_0872;
				IL_04c6:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
				arg3 = num;
				int num32;
				for (num32 = 0; num32 < 2 && (uint)num32 < (uint)span.Length && char.IsDigit(span[num32]); num32++)
				{
				}
				if (num32 == 0)
				{
					goto IL_0479;
				}
				span = span.Slice(num32);
				num += num32;
				arg4 = num;
				arg3++;
				goto IL_057b;
				IL_0714:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
				num19++;
				if (!span.IsEmpty && !(((c = span[0]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))))
				{
					num++;
					span = inputSpan.Slice(num);
					num20 = 0;
					goto IL_077b;
				}
				goto IL_0987;
				IL_0a90:
				capturePosition4 = Crawlpos();
				num4 = 0;
				goto IL_0bbb;
				IL_0872:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg5, arg6, Crawlpos());
				if (span.IsEmpty || (((c = span[0]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))))
				{
					goto IL_0805;
				}
				num++;
				span = inputSpan.Slice(num);
				if (num20 == 0)
				{
					goto IL_077b;
				}
				goto IL_092e;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num33)
				{
					while (Crawlpos() > num33)
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FullHttpUri_9 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FullHttpUri_9();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FullHttpUri_9()
	{
		pattern = "^(?<scheme>https?)://(?<host>localhost|(?:(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)(?:\\.(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)){3})|\\[(?<ipv6>[0-9A-Fa-f:.]+)\\]|(?:(?:[A-Za-z0-9](?:[A-Za-z0-9\\-]{0,61}[A-Za-z0-9])?\\.)+(?:[A-Za-z]{2,63}|xn--[A-Za-z0-9\\-]{2,59})))(?::(?<port>6553[0-5]|655[0-2]\\d|65[0-4]\\d{2}|6[0-4]\\d{3}|[1-5]\\d{4}|[1-9]\\d{0,3}))?(?<path>/[^\\s?#]*)?(?:\\?(?<query>[^\\s#]*))?(?:#(?<fragment>[^\\s]*))?$";
		roptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "fragment", 7 },
			{ "host", 2 },
			{ "ipv6", 3 },
			{ "path", 5 },
			{ "port", 4 },
			{ "query", 6 },
			{ "scheme", 1 }
		};
		capslist = new string[8] { "0", "scheme", "host", "ipv6", "port", "path", "query", "fragment" };
		capsize = 8;
	}
}
