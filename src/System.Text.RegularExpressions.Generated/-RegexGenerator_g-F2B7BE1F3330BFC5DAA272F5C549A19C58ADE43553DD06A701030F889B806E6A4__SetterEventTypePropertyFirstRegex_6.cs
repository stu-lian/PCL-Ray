using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal sealed class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__SetterEventTypePropertyFirstRegex_6 : Regex
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
				if (num <= inputSpan.Length - 63)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_indexOfString_2A6F2F0517B49FBD69AC7922BAF9726B1EB94FC8F0F3D1041AC247E3E2904D60);
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (!span.StartsWith("<Setter"))
				{
					UncaptureUntil(0);
					return false;
				}
				if (!_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsPostWordCharBoundary(inputSpan, num + 7))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 7;
				span = inputSpan.Slice(num);
				num8 = num;
				while (true)
				{
					num6 = Crawlpos();
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsPreWordCharBoundary(inputSpan, num) && span.StartsWith("Property"))
					{
						int i;
						for (i = 8; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
						{
						}
						span = span.Slice(i);
						num += i;
						if (!span.IsEmpty && span[0] == '=')
						{
							int j;
							for (j = 1; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
							{
							}
							span = span.Slice(j);
							num += j;
							if (span.StartsWith("\"local:CustomEventService.EventType\""))
							{
								num += 36;
								span = inputSpan.Slice(num);
								num9 = num;
								while (true)
								{
									num7 = Crawlpos();
									if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsPreWordCharBoundary(inputSpan, num) && span.StartsWith("Value"))
									{
										int k;
										for (k = 5; (uint)k < (uint)span.Length && char.IsWhiteSpace(span[k]); k++)
										{
										}
										span = span.Slice(k);
										num += k;
										if (!span.IsEmpty && span[0] == '=')
										{
											int l;
											for (l = 1; (uint)l < (uint)span.Length && char.IsWhiteSpace(span[l]); l++)
											{
											}
											span = span.Slice(l);
											num += l;
											if (!span.IsEmpty && span[0] == '"')
											{
												num++;
												span = inputSpan.Slice(num);
												num2 = num;
												int num10 = span.IndexOf('"');
												if (num10 < 0)
												{
													num10 = span.Length;
												}
												if (num10 != 0)
												{
													span = span.Slice(num10);
													num += num10;
													Capture(1, num2, num);
													if (!span.IsEmpty && span[0] == '"')
													{
														num++;
														span = inputSpan.Slice(num);
														num4 = num;
														int num11 = span.IndexOf('>');
														if (num11 < 0)
														{
															num11 = span.Length;
														}
														span = span.Slice(num11);
														num += num11;
														num5 = num;
														while (true)
														{
															num3 = Crawlpos();
															if (!span.IsEmpty)
															{
																char c = span[0];
																if (c != '/')
																{
																	if (c == '>')
																	{
																		int m;
																		for (m = 1; (uint)m < (uint)span.Length && char.IsWhiteSpace(span[m]); m++)
																		{
																		}
																		span = span.Slice(m);
																		num += m;
																		if (span.StartsWith("</Setter"))
																		{
																			int n;
																			for (n = 8; (uint)n < (uint)span.Length && char.IsWhiteSpace(span[n]); n++)
																			{
																			}
																			span = span.Slice(n);
																			num += n;
																			if (!span.IsEmpty && span[0] == '>')
																			{
																				num++;
																				span = inputSpan.Slice(num);
																				goto IL_04bf;
																			}
																		}
																	}
																}
																else
																{
																	int num12;
																	for (num12 = 1; (uint)num12 < (uint)span.Length && char.IsWhiteSpace(span[num12]); num12++)
																	{
																	}
																	span = span.Slice(num12);
																	num += num12;
																	if (!span.IsEmpty && span[0] == '>')
																	{
																		num++;
																		span = inputSpan.Slice(num);
																		goto IL_04bf;
																	}
																}
															}
															UncaptureUntil(num3);
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
															continue;
															IL_04bf:
															runtextpos = num;
															Capture(0, start, num);
															return true;
														}
													}
												}
											}
										}
									}
									UncaptureUntil(num7);
									if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									num = num9;
									span = inputSpan.Slice(num);
									if (span.IsEmpty || span[0] == '>')
									{
										break;
									}
									num++;
									span = inputSpan.Slice(num);
									num9 = num;
								}
							}
						}
					}
					UncaptureUntil(num6);
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num8;
					span = inputSpan.Slice(num);
					if (span.IsEmpty || span[0] == '>')
					{
						break;
					}
					num++;
					span = inputSpan.Slice(num);
					num8 = num;
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

	internal static readonly _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__SetterEventTypePropertyFirstRegex_6 Instance = new _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__SetterEventTypePropertyFirstRegex_6();

	private _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__SetterEventTypePropertyFirstRegex_6()
	{
		pattern = "<Setter\\b[^>]*?\\bProperty\\s*=\\s*\"local:CustomEventService\\.EventType\"[^>]*?\\bValue\\s*=\\s*\"([^\"]+)\"[^>]*(?:/\\s*>|>\\s*</Setter\\s*>)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
