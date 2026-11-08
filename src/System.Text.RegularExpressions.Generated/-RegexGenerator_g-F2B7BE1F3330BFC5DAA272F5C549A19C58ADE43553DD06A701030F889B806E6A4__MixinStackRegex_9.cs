using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal sealed class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__MixinStackRegex_9 : Regex
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
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
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
					int num2 = inputSpan.Slice(num).IndexOfNonAsciiOrAny_CBE28BB3DFB3CF3EF41E993BE2E6352647549C825825AA683DE00E08AF5509AA();
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num8 = num;
				if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num9 = 0;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '$')
				{
					return false;
				}
				num--;
				num2 = num;
				int i;
				for (i = 0; num > i && _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsWordChar(inputSpan[num - i - 1]); i++)
				{
				}
				if (i == 0)
				{
					return false;
				}
				num -= i;
				num3 = num;
				num2--;
				while (true)
				{
					if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == '$')
					{
						num--;
						num4 = num;
						int j;
						for (j = 0; num > j && _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsWordChar(inputSpan[num - j - 1]); j++)
						{
						}
						if (j != 0)
						{
							num -= j;
							num5 = num;
							num4--;
							while (true)
							{
								if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == '.')
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] != '(')
									{
										num--;
										num6 = num;
										while (true)
										{
											if ((uint)(num - 3) < inputSpan.Length)
											{
												int num10 = 0;
												while (true)
												{
													if (num10 < 3)
													{
														if (inputSpan[--num] != "at "[2 - num10])
														{
															break;
														}
														num10++;
														continue;
													}
													num = num8;
													readOnlySpan = inputSpan.Slice(num);
													char c;
													if (readOnlySpan.IsEmpty || (((c = readOnlySpan[0]) < '\u0080') ? (("\0\0\u0010Ͽ\ufffe蟿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\0\u0002\n$%\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0"))))
													{
														return false;
													}
													num++;
													readOnlySpan = inputSpan.Slice(num);
													num7 = num;
													int num11;
													while (true)
													{
														readOnlySpan = inputSpan.Slice(num);
														num11 = num;
														if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
														{
															CheckTimeout();
														}
														if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '$')
														{
															num++;
															readOnlySpan = inputSpan.Slice(num);
															int k;
															for (k = 0; (uint)k < (uint)readOnlySpan.Length && _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsWordChar(readOnlySpan[k]); k++)
															{
															}
															if (k != 0)
															{
																readOnlySpan = readOnlySpan.Slice(k);
																num += k;
																if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '(')
																{
																	break;
																}
															}
														}
														if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
														{
															CheckTimeout();
														}
														num = num7;
														readOnlySpan = inputSpan.Slice(num);
														if (readOnlySpan.IsEmpty || (((c = readOnlySpan[0]) < '\u0080') ? (("\0\0\u0010Ͽ\ufffe蟿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\0\u0002\n$%\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0"))))
														{
															return false;
														}
														num++;
														readOnlySpan = inputSpan.Slice(num);
														num7 = num;
													}
													num = num11;
													readOnlySpan = inputSpan.Slice(num);
													runtextpos = num;
													Capture(0, start, num);
													return true;
												}
											}
											if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											num = num6;
											readOnlySpan = inputSpan.Slice(num);
											if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '(')
											{
												break;
											}
											num--;
											num6 = num;
										}
									}
								}
								if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num4 <= num5)
								{
									break;
								}
								num = ++num5;
							}
						}
					}
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 <= num3)
					{
						break;
					}
					num = ++num3;
				}
				return false;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__MixinStackRegex_9 Instance = new _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__MixinStackRegex_9();

	private _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__MixinStackRegex_9()
	{
		pattern = "(?<=at [^(]+?\\.\\w+\\$\\w+\\$)[\\w\\$]+?(?=\\$\\w+\\()";
		roptions = RegexOptions.Compiled;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
