using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___Iso8601EndOfDay_6 : Regex
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
				if (num <= inputSpan.Length - 6)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_indexOfString_3FDDD2F495331D5070149F9D7128BE1B66022924EDBD0217D550055E9CBD1E9A);
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
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if ((uint)span.Length < 6u || !span.StartsWith("t24:00", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				num += 6;
				span = inputSpan.Slice(num);
				num2 = 0;
				int num5;
				while (true)
				{
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
					num2++;
					if (span.StartsWith(":00"))
					{
						num += 3;
						span = inputSpan.Slice(num);
						num3 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
							num3++;
							if (span.IsEmpty || span[0] != '.')
							{
								break;
							}
							int num4 = span.Slice(1).IndexOfAnyExcept('0');
							if (num4 < 0)
							{
								num4 = span.Length - 1;
							}
							if (num4 == 0)
							{
								break;
							}
							span = span.Slice(num4);
							num += num4;
							num++;
							span = inputSpan.Slice(num);
							if (num3 == 0)
							{
								continue;
							}
							goto IL_011a;
						}
						goto IL_00f7;
					}
					goto IL_0152;
					IL_011a:
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num3);
					if (num2 == 0)
					{
						continue;
					}
					goto IL_0189;
					IL_0189:
					num5 = num;
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					int num6 = num;
					char c;
					if (!span.IsEmpty && (c = span[0]) < '{' && ("\0\0⠀\0\0Ѐ\0Ѐ"[(int)c >> 4] & (1 << (c & 0xF))) != 0)
					{
						num++;
						span = inputSpan.Slice(num);
						break;
					}
					num = num6;
					span = inputSpan.Slice(num);
					if (num >= inputSpan.Length - 1 && ((uint)num >= (uint)inputSpan.Length || inputSpan[num] == '\n'))
					{
						break;
					}
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 != 0)
					{
						num3 = runstack[--pos];
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						goto IL_00f7;
					}
					return false;
					IL_0152:
					if (--num2 < 0)
					{
						return false;
					}
					num = runstack[--pos];
					span = inputSpan.Slice(num);
					goto IL_0189;
					IL_00f7:
					if (--num3 >= 0)
					{
						num = runstack[--pos];
						span = inputSpan.Slice(num);
						goto IL_011a;
					}
					goto IL_0152;
				}
				num = num5;
				span = inputSpan.Slice(num);
				runtextpos = num;
				Capture(0, start, num);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___Iso8601EndOfDay_6 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___Iso8601EndOfDay_6();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___Iso8601EndOfDay_6()
	{
		pattern = "[Tt]24:00(?::00(?:\\.0+)?)?(?=[Zz+\\-]|$)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
