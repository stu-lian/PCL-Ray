using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal sealed class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventOpenTagRegex_5 : Regex
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
				if (num <= inputSpan.Length - 28)
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (!span.StartsWith("<local:CustomEvent"))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 18;
				span = inputSpan.Slice(num);
				num4 = num;
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
				num5 = num;
				num4++;
				while (true)
				{
					num3 = Crawlpos();
					num7 = num;
					while (true)
					{
						num6 = Crawlpos();
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
									int num8 = span.IndexOf('"');
									if (num8 < 0)
									{
										num8 = span.Length;
									}
									if (num8 != 0)
									{
										span = span.Slice(num8);
										num += num8;
										Capture(1, num2, num);
										if (!span.IsEmpty && span[0] == '"')
										{
											int num9 = span.Slice(1).IndexOf('>');
											if (num9 < 0)
											{
												num9 = span.Length - 1;
											}
											span = span.Slice(num9);
											num += num9;
											if ((uint)span.Length >= 2u && span[1] == '>')
											{
												Capture(0, start, runtextpos = num + 2);
												return true;
											}
										}
									}
								}
							}
						}
						UncaptureUntil(num6);
						if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						num = num7;
						span = inputSpan.Slice(num);
						if (span.IsEmpty || span[0] == '>')
						{
							break;
						}
						num++;
						span = inputSpan.Slice(num);
						num7 = num;
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

	internal static readonly _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventOpenTagRegex_5 Instance = new _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventOpenTagRegex_5();

	private _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventOpenTagRegex_5()
	{
		pattern = "<local:CustomEvent\\s+[^>]*?\\bType\\s*=\\s*\"([^\"]+)\"[^>]*>";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
