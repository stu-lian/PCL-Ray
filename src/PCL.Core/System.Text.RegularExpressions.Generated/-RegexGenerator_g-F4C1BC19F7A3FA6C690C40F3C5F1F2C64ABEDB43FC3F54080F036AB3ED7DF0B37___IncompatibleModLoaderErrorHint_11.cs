using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___IncompatibleModLoaderErrorHint_11 : Regex
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
				if (num <= inputSpan.Length - 33)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 32; num2++)
					{
						int num3 = readOnlySpan.Slice(num2 + 5).IndexOfAny('D', 'p');
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						if ((uint)(num2 + 8) >= (uint)readOnlySpan.Length)
						{
							break;
						}
						char c;
						if ((((c = readOnlySpan[num2 + 4]) == 'I') | (c == 'm')) && (((c = readOnlySpan[num2 + 8]) == '\'') | (c == 'i')))
						{
							runtextpos = num + num2;
							return true;
						}
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
				int pos = 0;
				int num8 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num2 = num;
				int num9 = pos;
				if (span.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (span[0])
				{
				case 'i':
				{
					if (!span.Slice(1).StartsWith("ncompatible"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 12;
					span = inputSpan.Slice(num);
					num5 = num;
					int num11 = inputSpan.Length - num;
					if (num11 == 0)
					{
						UncaptureUntil(0);
						return false;
					}
					span = span.Slice(num11);
					num += num11;
					num6 = num;
					num5++;
					while (true)
					{
						num4 = Crawlpos();
						if (span.StartsWith("'Fabric Loader' (fabricloader)"))
						{
							break;
						}
						UncaptureUntil(num4);
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num5 >= num6 || (num6 = inputSpan.Slice(num5, Math.Min(inputSpan.Length, num6 + 29) - num5).LastIndexOf("'Fabric Loader' (fabricloader)")) < 0)
						{
							UncaptureUntil(0);
							return false;
						}
						num6 += num5;
						num = num6;
						span = inputSpan.Slice(num);
					}
					num += 30;
					span = inputSpan.Slice(num);
					break;
				}
				case 'M':
				{
					if (!span.Slice(1).StartsWith("od ID: '"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 9;
					span = inputSpan.Slice(num);
					num8 = pos;
					num7 = 0;
					do
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num7++;
						if (span.StartsWith("neo"))
						{
							num += 3;
							span = inputSpan.Slice(num);
							continue;
						}
						if (--num7 < 0)
						{
							UncaptureUntil(0);
							return false;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						break;
					}
					while (num7 == 0);
					pos = num8;
					if (!span.StartsWith("forge', Requested by '"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 22;
					span = inputSpan.Slice(num);
					num3 = num;
					int num10 = span.IndexOf('\'');
					if (num10 < 0)
					{
						num10 = span.Length;
					}
					if (num10 == 0)
					{
						UncaptureUntil(0);
						return false;
					}
					span = span.Slice(num10);
					num += num10;
					Capture(2, num3, num);
					if (span.IsEmpty || span[0] != '\'')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					span = inputSpan.Slice(num);
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
				pos = num9;
				Capture(1, num2, num);
				runtextpos = num;
				Capture(0, start, num);
				return true;
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___IncompatibleModLoaderErrorHint_11 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___IncompatibleModLoaderErrorHint_11();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___IncompatibleModLoaderErrorHint_11()
	{
		pattern = "(incompatible[\\s\\S]+'Fabric Loader' \\(fabricloader\\)|Mod ID: '(?:neo)?forge', Requested by '([^']+)')";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}
