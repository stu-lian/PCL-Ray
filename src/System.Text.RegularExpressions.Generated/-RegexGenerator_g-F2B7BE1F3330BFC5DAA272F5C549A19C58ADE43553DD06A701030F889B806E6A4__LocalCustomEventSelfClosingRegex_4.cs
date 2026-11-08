using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal sealed class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventSelfClosingRegex_4 : Regex
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
				if (num <= inputSpan.Length - 29)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_indexOfString_52478F2B00FC8D3D8978DE01FEE97B62431A2B859ECA3D1005ECCEE9C01B2F27);
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (!span.StartsWith("<local:CustomEvent"))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 18;
				span = inputSpan.Slice(num);
				num5 = num;
				int i;
				for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				if (i == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(i);
				num += i;
				num6 = num;
				num5++;
				while (true)
				{
					num3 = Crawlpos();
					num10 = num;
					while (true)
					{
						num9 = Crawlpos();
						if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.IsPreWordCharBoundary(inputSpan, num) && span.StartsWith("Type"))
						{
							int j;
							for (j = 4; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
							{
							}
							span = span.Slice(j);
							num += j;
							if (!span.IsEmpty && span[0] == '=')
							{
								int k;
								for (k = 1; (uint)k < (uint)span.Length && char.IsWhiteSpace(span[k]); k++)
								{
								}
								span = span.Slice(k);
								num += k;
								if (!span.IsEmpty && span[0] == '"')
								{
									num++;
									span = inputSpan.Slice(num);
									num2 = num;
									int num11 = span.IndexOf('"');
									if (num11 < 0)
									{
										num11 = span.Length;
									}
									if (num11 != 0)
									{
										span = span.Slice(num11);
										num += num11;
										Capture(1, num2, num);
										if (!span.IsEmpty && span[0] == '"')
										{
											num++;
											span = inputSpan.Slice(num);
											num7 = num;
											int num12 = span.IndexOf('>');
											if (num12 < 0)
											{
												num12 = span.Length;
											}
											span = span.Slice(num12);
											num += num12;
											num8 = num;
											while (true)
											{
												num4 = Crawlpos();
												if (!span.IsEmpty && span[0] == '/')
												{
													int l;
													for (l = 1; (uint)l < (uint)span.Length && char.IsWhiteSpace(span[l]); l++)
													{
													}
													span = span.Slice(l);
													num += l;
													if (!span.IsEmpty && span[0] == '>')
													{
														Capture(0, start, runtextpos = num + 1);
														return true;
													}
												}
												UncaptureUntil(num4);
												if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
												{
													CheckTimeout();
												}
												if (num7 >= num8 || (num8 = inputSpan.Slice(num7, num8 - num7).LastIndexOf('/')) < 0)
												{
													break;
												}
												num8 += num7;
												num = num8;
												span = inputSpan.Slice(num);
											}
										}
									}
								}
							}
						}
						UncaptureUntil(num9);
						if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						num = num10;
						span = inputSpan.Slice(num);
						if (span.IsEmpty || span[0] == '>')
						{
							break;
						}
						num++;
						span = inputSpan.Slice(num);
						num10 = num;
					}
					UncaptureUntil(num3);
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num5 >= num6)
					{
						break;
					}
					num = --num6;
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

	internal static readonly _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventSelfClosingRegex_4 Instance = new _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventSelfClosingRegex_4();

	private _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventSelfClosingRegex_4()
	{
		pattern = "<local:CustomEvent\\s+[^>]*?\\bType\\s*=\\s*\"([^\"]+)\"[^>]*/\\s*>";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
