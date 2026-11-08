using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___DlNeoForgeVersion_37 : Regex
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
				if (num <= inputSpan.Length - 5)
				{
					int num2 = inputSpan.Slice(num).IndexOfNonAsciiOrAny_2D817A11655641E09039B9CEF8FF696999614A6D1CD036F8C96490A245B60E9F();
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
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num11 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '"')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				num = num11;
				span = inputSpan.Slice(num);
				num5 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num5++;
					int start2 = num;
					if (!span.StartsWith("1.20.1-"))
					{
						goto IL_00e3;
					}
					num += 7;
					span = inputSpan.Slice(num);
					Capture(1, start2, num);
					if (num5 == 0)
					{
						continue;
					}
					goto IL_0123;
					IL_0123:
					int i;
					for (i = 0; (uint)i < (uint)span.Length && char.IsDigit(span[i]); i++)
					{
					}
					if (i != 0)
					{
						span = span.Slice(i);
						num += i;
						if (!span.IsEmpty && span[0] == '.')
						{
							int num12 = span.Slice(1).IndexOf('.');
							if (num12 < 0)
							{
								num12 = span.Length - 1;
							}
							if (num12 != 0)
							{
								span = span.Slice(num12);
								num += num12;
								if ((uint)span.Length >= 2u && span[1] == '.')
								{
									num += 2;
									span = inputSpan.Slice(num);
									num3 = num;
									int j;
									for (j = 0; (uint)j < (uint)span.Length && char.IsDigit(span[j]); j++)
									{
									}
									if (j != 0)
									{
										span = span.Slice(j);
										num += j;
										num4 = num;
										num3++;
										while (true)
										{
											num2 = Crawlpos();
											num6 = 0;
											while (true)
											{
												_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
												num6++;
												int num13 = num;
												if (!span.IsEmpty && span[0] == '.')
												{
													num++;
													span = inputSpan.Slice(num);
													arg = num;
													int k;
													for (k = 0; (uint)k < (uint)span.Length && char.IsDigit(span[k]); k++)
													{
													}
													if (k != 0)
													{
														span = span.Slice(k);
														num += k;
														arg2 = num;
														arg++;
														goto IL_033b;
													}
												}
												goto IL_038b;
												IL_033b:
												_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
												Capture(2, num13, num);
												_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num13);
												if (num6 == 0)
												{
													continue;
												}
												goto IL_03dd;
												IL_03dd:
												num7 = 0;
												while (true)
												{
													_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
													num7++;
													int num14 = num;
													int start3;
													if (!span.IsEmpty && span[0] == '-')
													{
														num++;
														span = inputSpan.Slice(num);
														start3 = num;
														if (!span.IsEmpty)
														{
															char c = span[0];
															if ((uint)c <= 66u)
															{
																if (c == 'A')
																{
																	goto IL_04a1;
																}
																if (c == 'B')
																{
																	goto IL_0467;
																}
															}
															else
															{
																if (c == 'a')
																{
																	goto IL_04a1;
																}
																if (c == 'b')
																{
																	goto IL_0467;
																}
															}
														}
													}
													goto IL_06c8;
													IL_04e6:
													_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
													num8++;
													int num15 = num;
													if (!span.IsEmpty && span[0] == '.')
													{
														num++;
														span = inputSpan.Slice(num);
														arg3 = num;
														int l;
														for (l = 0; (uint)l < (uint)span.Length && char.IsDigit(span[l]); l++)
														{
														}
														if (l != 0)
														{
															span = span.Slice(l);
															num += l;
															arg4 = num;
															arg3++;
															goto IL_05c3;
														}
													}
													goto IL_0613;
													IL_04a1:
													if ((uint)span.Length >= 5u && span.Slice(1).StartsWith("lpha", StringComparison.OrdinalIgnoreCase))
													{
														num += 5;
														span = inputSpan.Slice(num);
														goto IL_04d9;
													}
													goto IL_06c8;
													IL_0662:
													_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num8);
													Capture(3, num14, num);
													_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num14);
													if (num7 == 0)
													{
														continue;
													}
													goto IL_071a;
													IL_0613:
													if (--num8 >= 0)
													{
														num = runstack[--pos];
														UncaptureUntil(runstack[--pos]);
														span = inputSpan.Slice(num);
														goto IL_0662;
													}
													goto IL_06c8;
													IL_06c8:
													if (--num7 < 0)
													{
														break;
													}
													num = runstack[--pos];
													UncaptureUntil(runstack[--pos]);
													span = inputSpan.Slice(num);
													goto IL_071a;
													IL_04d9:
													Capture(4, start3, num);
													num8 = 0;
													goto IL_04e6;
													IL_05c3:
													_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
													Capture(5, num15, num);
													_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num15);
													if (num8 == 0)
													{
														goto IL_04e6;
													}
													goto IL_0662;
													IL_0467:
													if ((uint)span.Length >= 4u && span.Slice(1).StartsWith("eta", StringComparison.OrdinalIgnoreCase))
													{
														num += 4;
														span = inputSpan.Slice(num);
														goto IL_04d9;
													}
													goto IL_06c8;
													IL_071a:
													num9 = 0;
													while (true)
													{
														_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
														num9++;
														int num16 = num;
														if ((uint)span.Length >= 10u && span[0] == '+' && span.Slice(1).StartsWith("snapshot-", StringComparison.OrdinalIgnoreCase))
														{
															num += 10;
															span = inputSpan.Slice(num);
															arg5 = num;
															int m;
															for (m = 0; (uint)m < (uint)span.Length && char.IsDigit(span[m]); m++)
															{
															}
															if (m != 0)
															{
																span = span.Slice(m);
																num += m;
																arg6 = num;
																arg5++;
																goto IL_081a;
															}
														}
														goto IL_086a;
														IL_086a:
														if (--num9 < 0)
														{
															break;
														}
														num = runstack[--pos];
														UncaptureUntil(runstack[--pos]);
														span = inputSpan.Slice(num);
														goto IL_08bc;
														IL_081a:
														_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg5, arg6, Crawlpos());
														Capture(6, num16, num);
														_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num16);
														if (num9 == 0)
														{
															continue;
														}
														goto IL_08bc;
														IL_08bc:
														num10 = 0;
														while (true)
														{
															_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
															num10++;
															int start4 = num;
															if ((uint)span.Length >= 5u && span[0] == '+' && span.Slice(1).StartsWith("pre-", StringComparison.OrdinalIgnoreCase))
															{
																num += 5;
																span = inputSpan.Slice(num);
																int n;
																for (n = 0; (uint)n < (uint)span.Length && char.IsDigit(span[n]); n++)
																{
																}
																if (n != 0)
																{
																	span = span.Slice(n);
																	num += n;
																	Capture(7, start4, num);
																	if (num10 == 0)
																	{
																		continue;
																	}
																	goto IL_09a6;
																}
															}
															goto IL_096c;
															IL_096c:
															if (--num10 < 0)
															{
																break;
															}
															num = runstack[--pos];
															UncaptureUntil(runstack[--pos]);
															span = inputSpan.Slice(num);
															goto IL_09a6;
															IL_09a6:
															span = inputSpan.Slice(num);
															int num17 = num;
															if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
															{
																CheckTimeout();
															}
															if (!span.IsEmpty && span[0] == '"')
															{
																num = num17;
																span = inputSpan.Slice(num);
																runtextpos = num;
																Capture(0, start, num);
																return true;
															}
															goto IL_096c;
														}
														if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
														{
															CheckTimeout();
														}
														if (num9 != 0)
														{
															num16 = runstack[--pos];
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
																goto IL_081a;
															}
															goto IL_086a;
														}
														break;
													}
													if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
													{
														CheckTimeout();
													}
													if (num7 == 0)
													{
														break;
													}
													num14 = runstack[--pos];
													num8 = runstack[--pos];
													if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
													{
														CheckTimeout();
													}
													if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
													{
														CheckTimeout();
													}
													if (num8 != 0)
													{
														num15 = runstack[--pos];
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
															goto IL_05c3;
														}
														goto IL_0613;
													}
													goto IL_06c8;
												}
												if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
												{
													CheckTimeout();
												}
												if (num6 != 0)
												{
													num13 = runstack[--pos];
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
														goto IL_033b;
													}
													goto IL_038b;
												}
												break;
												IL_038b:
												if (--num6 < 0)
												{
													break;
												}
												num = runstack[--pos];
												UncaptureUntil(runstack[--pos]);
												span = inputSpan.Slice(num);
												goto IL_03dd;
											}
											UncaptureUntil(num2);
											if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											if (num3 >= num4)
											{
												break;
											}
											num = --num4;
											span = inputSpan.Slice(num);
										}
									}
								}
							}
						}
					}
					goto IL_00e3;
					IL_00e3:
					if (--num5 < 0)
					{
						break;
					}
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					goto IL_0123;
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___DlNeoForgeVersion_37 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___DlNeoForgeVersion_37();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___DlNeoForgeVersion_37()
	{
		pattern = "(?<=\")(1\\.20\\.1-)?\\d+\\.[^\\.]+\\.\\d+(\\.\\d+)?(-(beta|alpha)(\\.\\d+)?)?(\\+snapshot-\\d+)?(\\+pre-\\d+)?(?=\")";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 8;
	}
}
