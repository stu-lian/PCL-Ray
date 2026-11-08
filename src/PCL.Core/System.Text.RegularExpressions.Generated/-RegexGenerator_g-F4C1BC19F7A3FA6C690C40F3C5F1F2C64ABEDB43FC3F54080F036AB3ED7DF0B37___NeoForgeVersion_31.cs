using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NeoForgeVersion_31 : Regex
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
				if ((uint)num < (uint)inputSpan.Length)
				{
					int num2 = inputSpan.Slice(num).IndexOfAnyExcept('"');
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num3 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num4 = 0;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '"')
				{
					return false;
				}
				num--;
				num2 = num;
				while (true)
				{
					if ((uint)(num - 2) < inputSpan.Length)
					{
						int num5 = 0;
						while (true)
						{
							if (num5 < 2)
							{
								if (inputSpan[--num] != "\","[1 - num5])
								{
									break;
								}
								num5++;
								continue;
							}
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 115)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 118)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
							{
								break;
							}
							num--;
							if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
							{
								break;
							}
							num--;
							num = num3;
							span = inputSpan.Slice(num);
							int num6 = span.IndexOf('"');
							if (num6 < 0)
							{
								num6 = span.Length;
							}
							if (num6 == 0)
							{
								return false;
							}
							span = span.Slice(num6);
							num += num6;
							span = inputSpan.Slice(num);
							int num7 = num;
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (!span.StartsWith("\","))
							{
								return false;
							}
							num = num7;
							span = inputSpan.Slice(num);
							runtextpos = num;
							Capture(0, start, num);
							return true;
						}
					}
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num2;
					span = inputSpan.Slice(num);
					if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '"')
					{
						break;
					}
					num--;
					num2 = num;
				}
				return false;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NeoForgeVersion_31 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NeoForgeVersion_31();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NeoForgeVersion_31()
	{
		pattern = "(?<=orgeVersion\",[^\"]*?\")[^\"]+(?=\",)";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
