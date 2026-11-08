using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LabyModVersion_33 : Regex
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
				if (num <= inputSpan.Length - 3)
				{
					int num2 = inputSpan.Slice(num).IndexOf('1');
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num2 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '=')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 115)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 118)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '-')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					return false;
				}
				num--;
				for (int i = 0; i < 2; i++)
				{
					if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
					{
						return false;
					}
					num--;
				}
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 117)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '\n')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 100)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 121)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 98)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 108)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '\n')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 116)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 100)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '-')
				{
					return false;
				}
				num--;
				num = num2;
				readOnlySpan = inputSpan.Slice(num);
				if ((uint)readOnlySpan.Length < 2u || readOnlySpan[0] != '1' || readOnlySpan[1] == '\n')
				{
					return false;
				}
				int num3 = readOnlySpan.Slice(2).IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_ascii_48FF030000000000000000);
				if (num3 < 0)
				{
					num3 = readOnlySpan.Length - 2;
				}
				if (num3 == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(num3);
				num += num3;
				Capture(0, start, runtextpos = num + 2);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LabyModVersion_33 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LabyModVersion_33();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LabyModVersion_33()
	{
		pattern = "(?<=-Dnet.labymod.running-version=)1.[0-9+.]+";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
