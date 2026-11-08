using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftJsonVersion_34 : Regex
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
					int num2 = inputSpan.Slice(num).IndexOfAnyInRange('1', '9');
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
				int start2 = 0;
				int num7 = 0;
				int capturePosition = 0;
				int num8 = 0;
				int num9 = 0;
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int num10 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				int pos = 0;
				int num14 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num5 = num;
				num4 = num;
				num3 = Crawlpos();
				num6 = num;
				if ((uint)span.Length < 6u || !char.IsBetween(span[0], '1', '9') || !char.IsAsciiDigit(span[1]) || (span[2] | 0x20) != 119 || !char.IsAsciiDigit(span[3]) || !char.IsAsciiDigit(span[4]) || (uint)((span[5] | 0x20) - 97) > 6u)
				{
					goto IL_00f5;
				}
				num += 6;
				span = inputSpan.Slice(num);
				Capture(2, num6, num);
				num2 = 0;
				goto IL_08f2;
				IL_03c3:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num15;
				if (num10 != 0)
				{
					num15 = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (arg < arg2)
					{
						num = --arg2;
						span = inputSpan.Slice(num);
						goto IL_0337;
					}
					goto IL_0387;
				}
				goto IL_0225;
				IL_03dc:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
				num11++;
				int num16 = num;
				int arg7 = num;
				int arg8 = Crawlpos();
				int num17;
				int arg9;
				int arg10;
				if (!span.IsEmpty && span[0] == '-')
				{
					num++;
					span = inputSpan.Slice(num);
					num17 = num;
					arg9 = num;
					arg10 = Crawlpos();
					if ((uint)span.Length < 3u || !span.StartsWith("pre", StringComparison.OrdinalIgnoreCase))
					{
						goto IL_047f;
					}
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg9, arg10);
					num += 3;
					span = inputSpan.Slice(num);
					goto IL_0603;
				}
				goto IL_06db;
				IL_0266:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
				num10++;
				num15 = num;
				if (!span.IsEmpty && span[0] == '.')
				{
					num++;
					span = inputSpan.Slice(num);
					arg = num;
					int num18 = span.IndexOfAnyExceptInRange('0', '9');
					if (num18 < 0)
					{
						num18 = span.Length;
					}
					if (num18 != 0)
					{
						span = span.Slice(num18);
						num += num18;
						arg2 = num;
						arg++;
						goto IL_0337;
					}
				}
				goto IL_0387;
				IL_087b:
				if (--num11 < 0)
				{
					goto IL_03c3;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				span = inputSpan.Slice(num);
				goto IL_08cd;
				IL_0337:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
				Capture(5, num15, num);
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num15);
				if (num10 == 0)
				{
					goto IL_0266;
				}
				goto IL_03d9;
				IL_07c7:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num12);
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg7, arg8);
				goto IL_0845;
				IL_025b:
				capturePosition = Crawlpos();
				num10 = 0;
				goto IL_0266;
				IL_08f2:
				while (true)
				{
					Capture(1, num5, num);
					num14 = pos;
					num13 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num13++;
						int start3 = num;
						if ((uint)span.Length >= 13u && span.StartsWith("_unobfuscated", StringComparison.OrdinalIgnoreCase))
						{
							num += 13;
							span = inputSpan.Slice(num);
							Capture(9, start3, num);
							if (num13 == 0)
							{
								continue;
							}
						}
						else
						{
							if (--num13 < 0)
							{
								break;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
						}
						pos = num14;
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
					goto IL_08b7;
				}
				goto IL_00f5;
				IL_0845:
				Capture(6, num16, num);
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num16);
				if (num11 == 0)
				{
					goto IL_03dc;
				}
				goto IL_08cd;
				IL_08cd:
				Capture(3, start2, num);
				num2 = 1;
				goto IL_08f2;
				IL_078d:
				if (--num12 >= 0)
				{
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					span = inputSpan.Slice(num);
					goto IL_07c7;
				}
				goto IL_087b;
				IL_06ad:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg5, arg6, Crawlpos());
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 0, arg7, arg8);
				goto IL_0845;
				IL_04d7:
				num = arg9;
				span = inputSpan.Slice(num);
				UncaptureUntil(arg10);
				if ((uint)span.Length >= 8u && span.StartsWith("snapshot", StringComparison.OrdinalIgnoreCase))
				{
					num += 8;
					span = inputSpan.Slice(num);
					arg3 = num;
					if (!span.IsEmpty && span[0] == '-')
					{
						span = span.Slice(1);
						num++;
					}
					arg4 = num;
					goto IL_0597;
				}
				goto IL_06db;
				IL_0387:
				if (--num10 < 0)
				{
					goto IL_0225;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				span = inputSpan.Slice(num);
				goto IL_03d9;
				IL_00f5:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				start2 = num;
				num7 = num;
				if (span.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (span[0])
				{
				case '1':
					num++;
					span = inputSpan.Slice(num);
					break;
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					if ((uint)span.Length < 2u || !char.IsAsciiDigit(span[1]))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 2;
					span = inputSpan.Slice(num);
					break;
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(4, num7, num);
				if (span.IsEmpty || span[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num8 = num;
				int num19 = span.IndexOfAnyExceptInRange('0', '9');
				if (num19 < 0)
				{
					num19 = span.Length;
				}
				if (num19 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num19);
				num += num19;
				num9 = num;
				num8++;
				goto IL_025b;
				IL_0603:
				Capture(7, num17, num);
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num17);
				arg5 = num;
				int num20 = span.IndexOfAnyExceptInRange('1', '9');
				if (num20 < 0)
				{
					num20 = span.Length;
				}
				span = span.Slice(num20);
				num += num20;
				arg6 = num;
				goto IL_06ad;
				IL_0597:
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 2, arg9, arg10);
				goto IL_0603;
				IL_03d9:
				num11 = 0;
				goto IL_03dc;
				IL_047f:
				num = arg9;
				span = inputSpan.Slice(num);
				UncaptureUntil(arg10);
				if ((uint)span.Length < 2u || !span.StartsWith("rc", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_04d7;
				}
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, 1, arg9, arg10);
				num += 2;
				span = inputSpan.Slice(num);
				goto IL_0603;
				IL_054a:
				UncaptureUntil(runstack[--pos]);
				_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (arg3 < arg4)
				{
					num = --arg4;
					span = inputSpan.Slice(num);
					goto IL_0597;
				}
				goto IL_06db;
				IL_08b7:
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num11 != 0)
				{
					num16 = runstack[--pos];
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg8, out arg7);
					int num21 = runstack[--pos];
					if (num21 == 0)
					{
						UncaptureUntil(runstack[--pos]);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg6, out arg5);
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (arg5 >= arg6)
						{
							num17 = runstack[--pos];
							if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg10, out arg9);
							switch (runstack[--pos])
							{
							case 0:
								break;
							case 1:
								goto IL_04d7;
							case 2:
								goto IL_054a;
							default:
								goto IL_0603;
							}
							goto IL_047f;
						}
						num = --arg6;
						span = inputSpan.Slice(num);
						goto IL_06ad;
					}
					if (num21 == 1)
					{
						num12 = runstack[--pos];
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						goto IL_078d;
					}
					goto IL_0845;
				}
				goto IL_03c3;
				IL_06db:
				num = arg7;
				span = inputSpan.Slice(num);
				UncaptureUntil(arg8);
				if ((uint)span.Length >= 12u && span.StartsWith(" pre-release", StringComparison.OrdinalIgnoreCase))
				{
					num += 12;
					span = inputSpan.Slice(num);
					num12 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num12++;
						int start4 = num;
						if ((uint)span.Length < 2u || span[0] != ' ' || !char.IsBetween(span[1], '1', '9'))
						{
							break;
						}
						num += 2;
						span = inputSpan.Slice(num);
						Capture(8, start4, num);
						if (num12 == 0)
						{
							continue;
						}
						goto IL_07c7;
					}
					goto IL_078d;
				}
				goto IL_087b;
				IL_0225:
				UncaptureUntil(capturePosition);
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num8 >= num9)
				{
					UncaptureUntil(0);
					return false;
				}
				num = --num9;
				span = inputSpan.Slice(num);
				goto IL_025b;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num22)
				{
					while (Crawlpos() > num22)
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftJsonVersion_34 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftJsonVersion_34();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftJsonVersion_34()
	{
		pattern = "(([1-9][0-9]w[0-9]{2}[a-g])|((1|[2-9][0-9])\\.[0-9]+(\\.[0-9]+)?(-(pre|rc|snapshot-?)[1-9]*| Pre-Release( [1-9])?)?))(_unobfuscated)?";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 10;
	}
}
