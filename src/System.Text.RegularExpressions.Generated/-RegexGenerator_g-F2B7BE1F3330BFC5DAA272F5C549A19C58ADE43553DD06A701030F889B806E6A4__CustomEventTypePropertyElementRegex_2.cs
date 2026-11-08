using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
internal sealed class _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__CustomEventTypePropertyElementRegex_2 : Regex
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
				if (num <= inputSpan.Length - 50)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_indexOfString_F6DA3C8354EE118ADB75C86D460C767946D3C1FB9040AD4C8A800EC7F79E0651);
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
				num2 = num;
				if (!span.StartsWith("<local:CustomEvent.Type"))
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 23; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				if (span.IsEmpty || span[0] != '>')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num6 = num;
				int j;
				for (j = 0; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
				{
				}
				span = span.Slice(j);
				num += j;
				num7 = num;
				while (true)
				{
					num5 = Crawlpos();
					Capture(1, num2, num);
					num3 = num;
					if (!span.IsEmpty && span[0] != '<')
					{
						num++;
						span = inputSpan.Slice(num);
						num9 = num;
						while (true)
						{
							num8 = Crawlpos();
							Capture(2, num3, num);
							num4 = num;
							int k;
							for (k = 0; (uint)k < (uint)span.Length && char.IsWhiteSpace(span[k]); k++)
							{
							}
							span = span.Slice(k);
							num += k;
							if (span.StartsWith("</local:CustomEvent.Type"))
							{
								int l;
								for (l = 24; (uint)l < (uint)span.Length && char.IsWhiteSpace(span[l]); l++)
								{
								}
								span = span.Slice(l);
								num += l;
								if (!span.IsEmpty && span[0] == '>')
								{
									num++;
									span = inputSpan.Slice(num);
									Capture(3, num4, num);
									runtextpos = num;
									Capture(0, start, num);
									return true;
								}
							}
							UncaptureUntil(num8);
							if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							num = num9;
							span = inputSpan.Slice(num);
							if (span.IsEmpty || span[0] == '<')
							{
								break;
							}
							num++;
							span = inputSpan.Slice(num);
							num9 = num;
						}
					}
					UncaptureUntil(num5);
					if (_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num6 >= num7 || (num7 = inputSpan.Slice(num6, num7 - num6).LastIndexOfAnyExcept('<')) < 0)
					{
						break;
					}
					num7 += num6;
					num = num7;
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

	internal static readonly _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__CustomEventTypePropertyElementRegex_2 Instance = new _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__CustomEventTypePropertyElementRegex_2();

	private _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__CustomEventTypePropertyElementRegex_2()
	{
		pattern = "(<local:CustomEvent\\.Type\\s*>\\s*)([^<]+?)(\\s*</local:CustomEvent\\.Type\\s*>)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 4;
	}
}
