using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal sealed class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__PackageRegex_8 : Regex
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
				if (num <= inputSpan.Length - 5)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_asciiLettersAndUnderscore);
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
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num13 = num;
				if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num14 = num12;
				num2 = num;
				int i;
				for (i = 0; num > i && inputSpan[num - i - 1] != '{'; i++)
				{
				}
				if (i == 0)
				{
					return false;
				}
				num -= i;
				num3 = num;
				num2--;
				while ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '\n')
				{
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 <= num3)
					{
						return false;
					}
					num = ++num3;
				}
				num--;
				num12 = num14;
				num = num13;
				span = inputSpan.Slice(num);
				num4 = num;
				int num15 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_asciiLettersAndUnderscore);
				if (num15 < 0)
				{
					num15 = span.Length;
				}
				if (num15 == 0)
				{
					return false;
				}
				span = span.Slice(num15);
				num += num15;
				num5 = num;
				num4++;
				while (true)
				{
					int j;
					for (j = 0; (uint)j < (uint)span.Length && _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsWordChar(span[j]); j++)
					{
					}
					if (j != 0)
					{
						span = span.Slice(j);
						num += j;
						if (!span.IsEmpty && span[0] == '.')
						{
							num++;
							span = inputSpan.Slice(num);
							num6 = num;
							int num16 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_asciiLettersAndUnderscore);
							if (num16 < 0)
							{
								num16 = span.Length;
							}
							if (num16 != 0)
							{
								span = span.Slice(num16);
								num += num16;
								num7 = num;
								num6++;
								while (true)
								{
									num8 = num;
									int k;
									for (k = 0; (uint)k < (uint)span.Length; k++)
									{
										char c;
										if (!(((c = span[k]) < '\u0080') ? (("\0\0䀀Ͽ\ufffe蟿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\u0002\n./\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0")))
										{
											break;
										}
									}
									if (k != 0)
									{
										span = span.Slice(k);
										num += k;
										num9 = num;
										num8++;
										while (true)
										{
											span = inputSpan.Slice(num);
											int num17 = num;
											if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											int num18 = num12;
											if (!span.IsEmpty && span[0] == '.')
											{
												num++;
												span = inputSpan.Slice(num);
												num10 = num;
												int l;
												for (l = 0; (uint)l < (uint)span.Length; l++)
												{
													char c;
													if (!(((c = span[l]) < '\u0080') ? (("\0\0䀐Ͽ\ufffe蟿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\u0004\n$%./\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0")))
													{
														break;
													}
												}
												if (l != 0)
												{
													span = span.Slice(l);
													num += l;
													num11 = num;
													num10++;
													while (true)
													{
														if (span.IsEmpty || span[0] != '.')
														{
															if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
															{
																CheckTimeout();
															}
															if (num10 >= num11 || (num11 = inputSpan.Slice(num10, num11 - num10).LastIndexOf('.')) < 0)
															{
																break;
															}
															num11 += num10;
															num = num11;
															span = inputSpan.Slice(num);
															continue;
														}
														num12 = num18;
														num = num17;
														span = inputSpan.Slice(num);
														runtextpos = num;
														Capture(0, start, num);
														return true;
													}
												}
											}
											if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											if (num8 >= num9 || (num9 = inputSpan.Slice(num8, num9 - num8).LastIndexOf('.')) < 0)
											{
												break;
											}
											num9 += num8;
											num = num9;
											span = inputSpan.Slice(num);
										}
									}
									if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (num6 >= num7)
									{
										break;
									}
									num = --num7;
									span = inputSpan.Slice(num);
								}
							}
						}
					}
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
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
				return false;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__PackageRegex_8 Instance = new _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__PackageRegex_8();

	private _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__PackageRegex_8()
	{
		pattern = "(?<=\\n[^{]+)[a-zA-Z_]+\\w+\\.[a-zA-Z_]+[\\w\\.]+(?=\\.[\\w\\.$]+\\.)";
		roptions = RegexOptions.Compiled;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
