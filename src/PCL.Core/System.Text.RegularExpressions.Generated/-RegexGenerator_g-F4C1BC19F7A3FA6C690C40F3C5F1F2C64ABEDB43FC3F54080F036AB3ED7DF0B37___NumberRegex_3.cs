using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NumberRegex_3 : Regex
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
					int num2 = inputSpan.Slice(num).IndexOfNonAsciiOrAny_2D9E8074DDB4840DBEF3653AD14687473D327465C13ACF6531B3622B9518E6A3();
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
				int pos = 0;
				int num13 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				char c;
				if (!readOnlySpan.IsEmpty && (((c = readOnlySpan[0]) == '+') | (c == '-')))
				{
					readOnlySpan = readOnlySpan.Slice(1);
					num++;
				}
				num3 = num;
				num4 = num;
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[i]); i++)
				{
				}
				if (i != 0)
				{
					readOnlySpan = readOnlySpan.Slice(i);
					num += i;
					num5 = num;
					num4++;
					goto IL_00de;
				}
				goto IL_019a;
				IL_00de:
				num6 = num;
				if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '.')
				{
					readOnlySpan = readOnlySpan.Slice(1);
					num++;
				}
				num7 = num;
				goto IL_012f;
				IL_023f:
				num2 = 1;
				goto IL_025a;
				IL_012f:
				num8 = num;
				int j;
				for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[j]); j++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(j);
				num += j;
				num9 = num;
				goto IL_0193;
				IL_0193:
				num2 = 0;
				goto IL_025a;
				IL_019a:
				num = num3;
				readOnlySpan = inputSpan.Slice(num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num10 = num;
				int k;
				for (k = 0; (uint)k < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[k]); k++)
				{
				}
				if (k == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(k);
				num += k;
				num11 = num;
				num10++;
				goto IL_023f;
				IL_025a:
				while (true)
				{
					num13 = pos;
					num12 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
						num12++;
						if (!readOnlySpan.IsEmpty && (readOnlySpan[0] | 0x20) == 101)
						{
							if ((uint)readOnlySpan.Length > 1u && (((c = readOnlySpan[1]) == '+') | (c == '-')))
							{
								readOnlySpan = readOnlySpan.Slice(1);
								num++;
							}
							num++;
							readOnlySpan = inputSpan.Slice(num);
							int l;
							for (l = 0; (uint)l < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[l]); l++)
							{
							}
							if (l != 0)
							{
								readOnlySpan = readOnlySpan.Slice(l);
								num += l;
								if (num12 == 0)
								{
									continue;
								}
								goto IL_033b;
							}
						}
						if (--num12 < 0)
						{
							break;
						}
						num = runstack[--pos];
						readOnlySpan = inputSpan.Slice(num);
						goto IL_033b;
						IL_033b:
						pos = num13;
						runtextpos = num;
						Capture(0, start, num);
						return true;
					}
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 == 0)
					{
						break;
					}
					if (num2 != 1)
					{
						continue;
					}
					goto IL_0218;
				}
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num8 >= num9)
				{
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num6 >= num7)
					{
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num4 < num5)
						{
							num = --num5;
							readOnlySpan = inputSpan.Slice(num);
							goto IL_00de;
						}
						goto IL_019a;
					}
					num = --num7;
					readOnlySpan = inputSpan.Slice(num);
					goto IL_012f;
				}
				num = --num9;
				readOnlySpan = inputSpan.Slice(num);
				goto IL_0193;
				IL_0218:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num10 >= num11)
				{
					return false;
				}
				num = --num11;
				readOnlySpan = inputSpan.Slice(num);
				goto IL_023f;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NumberRegex_3 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NumberRegex_3();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NumberRegex_3()
	{
		pattern = "[-+]?(?:\\d+\\.?\\d*|\\.\\d+)(?:[eE][-+]?\\d+)?";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
