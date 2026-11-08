using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HttpUri_8 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 8 && num == 0)
				{
					return true;
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
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				if ((uint)span.Length < 4u || !span.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				if ((uint)span.Length > 4u && (span[4] | 0x20) == 115)
				{
					span = span.Slice(1);
					num++;
				}
				if (!span.Slice(4).StartsWith("://"))
				{
					return false;
				}
				num3 = num;
				if ((uint)span.Length >= 8u && span[7] == '[')
				{
					num += 8;
					span = inputSpan.Slice(num);
					int i;
					for (i = 0; (uint)i < (uint)span.Length; i++)
					{
						char c;
						if (!(((c = span[i]) < '\u0080') ? (("쇿\uffff\ufffe\uffff\uffff\udfff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\u0002\u0001]^d")))
						{
							break;
						}
					}
					if (i != 0)
					{
						span = span.Slice(i);
						num += i;
						if (!span.IsEmpty && span[0] == ']')
						{
							num2 = 0;
							num++;
							span = inputSpan.Slice(num);
							goto IL_0239;
						}
					}
				}
				goto IL_0166;
				IL_0166:
				num = num3;
				span = inputSpan.Slice(num);
				num += 7;
				span = inputSpan.Slice(num);
				num4 = num;
				int j;
				for (j = 0; (uint)j < (uint)span.Length; j++)
				{
					char c;
					if (!(((c = span[j]) < '\u0080') ? (("쇿\uffff翶篿\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\b\u0001#$/0:;?@d")))
					{
						break;
					}
				}
				if (j == 0)
				{
					return false;
				}
				span = span.Slice(j);
				num += j;
				num5 = num;
				num4++;
				goto IL_021e;
				IL_021e:
				num2 = 1;
				goto IL_0239;
				IL_0239:
				while (true)
				{
					num6 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
						num6++;
						if (!span.IsEmpty && span[0] == ':')
						{
							num++;
							span = inputSpan.Slice(num);
							arg = num;
							int k;
							for (k = 0; k < 5 && (uint)k < (uint)span.Length && char.IsDigit(span[k]); k++)
							{
							}
							if (k != 0)
							{
								span = span.Slice(k);
								num += k;
								arg2 = num;
								arg++;
								goto IL_02fe;
							}
						}
						goto IL_0318;
						IL_02fe:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2);
						if (num6 == 0)
						{
							continue;
						}
						goto IL_0359;
						IL_0359:
						num7 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
							num7++;
							if (!span.IsEmpty && span[0] == '/')
							{
								num++;
								span = inputSpan.Slice(num);
								arg3 = num;
								int l;
								for (l = 0; (uint)l < (uint)span.Length; l++)
								{
									char c;
									if (!(((c = span[l]) < '\u0080') ? (("쇿\uffff\ufff6翿\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\u0004\u0001#$?@d")))
									{
										break;
									}
								}
								span = span.Slice(l);
								num += l;
								arg4 = num;
								goto IL_043a;
							}
							goto IL_0454;
							IL_0495:
							num8 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
								num8++;
								if (!span.IsEmpty && span[0] == '?')
								{
									num++;
									span = inputSpan.Slice(num);
									arg5 = num;
									int m;
									for (m = 0; (uint)m < (uint)span.Length; m++)
									{
										char c;
										if (!(((c = span[m]) < '\u0080') ? (("쇿\uffff\ufff6\uffff\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\u0002\u0001#$d")))
										{
											break;
										}
									}
									span = span.Slice(m);
									num += m;
									arg6 = num;
									goto IL_0576;
								}
								goto IL_0590;
								IL_05d1:
								num9 = 0;
								while (true)
								{
									_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num);
									num9++;
									if (span.IsEmpty || span[0] != '#')
									{
										goto IL_063b;
									}
									int n;
									for (n = 1; (uint)n < (uint)span.Length && !char.IsWhiteSpace(span[n]); n++)
									{
									}
									span = span.Slice(n);
									num += n;
									if (num9 == 0)
									{
										continue;
									}
									goto IL_0661;
									IL_063b:
									if (--num9 < 0)
									{
										break;
									}
									num = runstack[--pos];
									span = inputSpan.Slice(num);
									goto IL_0661;
									IL_0661:
									if (num >= inputSpan.Length - 1 && ((uint)num >= (uint)inputSpan.Length || inputSpan[num] == '\n'))
									{
										runtextpos = num;
										Capture(0, start, num);
										return true;
									}
									goto IL_063b;
								}
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num8 != 0)
								{
									_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg6, out arg5);
									if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (arg5 < arg6)
									{
										num = --arg6;
										span = inputSpan.Slice(num);
										goto IL_0576;
									}
									goto IL_0590;
								}
								break;
								IL_0590:
								if (--num8 < 0)
								{
									break;
								}
								num = runstack[--pos];
								span = inputSpan.Slice(num);
								goto IL_05d1;
								IL_0576:
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg5, arg6);
								if (num8 == 0)
								{
									continue;
								}
								goto IL_05d1;
							}
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num7 != 0)
							{
								_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (arg3 < arg4)
								{
									num = --arg4;
									span = inputSpan.Slice(num);
									goto IL_043a;
								}
								goto IL_0454;
							}
							break;
							IL_0454:
							if (--num7 < 0)
							{
								break;
							}
							num = runstack[--pos];
							span = inputSpan.Slice(num);
							goto IL_0495;
							IL_043a:
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg3, arg4);
							if (num7 == 0)
							{
								continue;
							}
							goto IL_0495;
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num6 != 0)
						{
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (arg < arg2)
							{
								num = --arg2;
								span = inputSpan.Slice(num);
								goto IL_02fe;
							}
							goto IL_0318;
						}
						break;
						IL_0318:
						if (--num6 < 0)
						{
							break;
						}
						num = runstack[--pos];
						span = inputSpan.Slice(num);
						goto IL_0359;
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
					goto IL_01f7;
				}
				goto IL_0166;
				IL_01f7:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num4 >= num5)
				{
					return false;
				}
				num = --num5;
				span = inputSpan.Slice(num);
				goto IL_021e;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HttpUri_8 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HttpUri_8();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HttpUri_8()
	{
		pattern = "^https?://(?:\\[[^\\]\\s]+\\]|[^/\\s?#:]+)(?::\\d{1,5})?(?:/[^\\s?#]*)?(?:\\?[^\\s#]*)?(?:#\\S*)?$";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
