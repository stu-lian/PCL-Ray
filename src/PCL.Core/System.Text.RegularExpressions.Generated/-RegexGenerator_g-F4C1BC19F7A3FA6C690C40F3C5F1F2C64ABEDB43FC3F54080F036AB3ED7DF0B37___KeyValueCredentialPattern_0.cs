using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___KeyValueCredentialPattern_0 : Regex
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
				if (num <= inputSpan.Length - 10)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_indexOfAnyStrings_OrdinalIgnoreCase_49ECD0753A256AA8ED2013964D4FC652AAF2522227A92945DA4473B2CF1F072E);
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num5 = num;
				if (!_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.IsPreWordCharBoundary(inputSpan, num))
				{
					UncaptureUntil(0);
					return false;
				}
				num4 = num;
				num3 = Crawlpos();
				if ((uint)span.Length >= 6u && span.StartsWith("access", StringComparison.OrdinalIgnoreCase))
				{
					char c;
					if ((uint)span.Length > 6u && (((c = span[6]) == '-') | (c == '_')))
					{
						span = span.Slice(1);
						num++;
					}
					if ((uint)span.Length >= 11u && span.Slice(6).StartsWith("to", StringComparison.OrdinalIgnoreCase) && !((((c = span[8]) | 0x20) != 107) & (c != 'K')) && span.Slice(9).StartsWith("en", StringComparison.OrdinalIgnoreCase))
					{
						num2 = 0;
						num += 11;
						span = inputSpan.Slice(num);
						goto IL_0353;
					}
				}
				goto IL_010a;
				IL_02d1:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if ((uint)span.Length < 13u || !span.StartsWith("authorization", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = 4;
				num += 13;
				span = inputSpan.Slice(num);
				goto IL_0353;
				IL_0289:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if ((uint)span.Length < 8u || !span.StartsWith("password", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_02d1;
				}
				num2 = 3;
				num += 8;
				span = inputSpan.Slice(num);
				goto IL_0353;
				IL_01ec:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if ((uint)span.Length >= 6u && span.StartsWith("client", StringComparison.OrdinalIgnoreCase))
				{
					char c;
					if ((uint)span.Length > 6u && (((c = span[6]) == '-') | (c == '_')))
					{
						span = span.Slice(1);
						num++;
					}
					if ((uint)span.Length >= 12u && span.Slice(6).StartsWith("secret", StringComparison.OrdinalIgnoreCase))
					{
						num2 = 2;
						num += 12;
						span = inputSpan.Slice(num);
						goto IL_0353;
					}
				}
				goto IL_0289;
				IL_0353:
				while (true)
				{
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.IsPostWordCharBoundary(inputSpan, num))
					{
						int i;
						for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
						{
						}
						span = span.Slice(i);
						num += i;
						char c;
						if (!span.IsEmpty && !(((c = span[0]) != ':') & (c != '=')))
						{
							int j;
							for (j = 1; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
							{
							}
							span = span.Slice(j);
							num += j;
							Capture(1, num5, num);
							int num6 = num;
							if (!span.IsEmpty && span[0] == '"')
							{
								int num7 = span.Slice(1).IndexOf('"');
								if (num7 < 0)
								{
									num7 = span.Length - 1;
								}
								span = span.Slice(num7);
								num += num7;
								if ((uint)span.Length >= 2u && span[1] == '"')
								{
									num += 2;
									span = inputSpan.Slice(num);
									goto IL_04b9;
								}
							}
							num = num6;
							span = inputSpan.Slice(num);
							int k;
							for (k = 0; (uint)k < (uint)span.Length && !char.IsWhiteSpace(span[k]); k++)
							{
							}
							if (k != 0)
							{
								span = span.Slice(k);
								num += k;
								goto IL_04b9;
							}
						}
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
						goto IL_01ec;
					case 2:
						goto IL_0289;
					case 3:
						goto IL_02d1;
					case 4:
						UncaptureUntil(0);
						return false;
					default:
						continue;
					}
					break;
					IL_04b9:
					runtextpos = num;
					Capture(0, start, num);
					return true;
				}
				goto IL_010a;
				IL_010a:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if ((uint)span.Length >= 7u && span.StartsWith("refresh", StringComparison.OrdinalIgnoreCase))
				{
					char c;
					if ((uint)span.Length > 7u && (((c = span[7]) == '-') | (c == '_')))
					{
						span = span.Slice(1);
						num++;
					}
					if ((uint)span.Length >= 12u && span.Slice(7).StartsWith("to", StringComparison.OrdinalIgnoreCase) && !((((c = span[9]) | 0x20) != 107) & (c != 'K')) && span.Slice(10).StartsWith("en", StringComparison.OrdinalIgnoreCase))
					{
						num2 = 1;
						num += 12;
						span = inputSpan.Slice(num);
						goto IL_0353;
					}
				}
				goto IL_01ec;
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___KeyValueCredentialPattern_0 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___KeyValueCredentialPattern_0();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___KeyValueCredentialPattern_0()
	{
		pattern = "(\\b(?:access[_-]?token|refresh[_-]?token|client[_-]?secret|password|authorization)\\b\\s*[:=]\\s*)(?:\"[^\"]*\"|\\S+)";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
