using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___UncPath_24 : Regex
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
				if (num <= inputSpan.Length - 5 && num == 0)
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
				int arg = 0;
				int arg2 = 0;
				int num5 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				if (!span.StartsWith("\\\\"))
				{
					UncaptureUntil(0);
					return false;
				}
				int num6 = span.Slice(2).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_ascii_48400D40000001000000010);
				if (num6 < 0)
				{
					num6 = span.Length - 2;
				}
				if (num6 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num6);
				num += num6;
				if ((uint)span.Length < 3u || span[2] != '\\')
				{
					UncaptureUntil(0);
					return false;
				}
				num += 3;
				span = inputSpan.Slice(num);
				num3 = num;
				int num7 = span.IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_ascii_48400D40000001000000010);
				if (num7 < 0)
				{
					num7 = span.Length;
				}
				if (num7 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num7);
				num += num7;
				num4 = num;
				num3++;
				while (true)
				{
					num2 = Crawlpos();
					num5 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num5++;
						int num8 = num;
						if (!span.IsEmpty && span[0] == '\\')
						{
							num++;
							span = inputSpan.Slice(num);
							arg = num;
							int num9 = span.IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_ascii_48400D40000001000000010);
							if (num9 < 0)
							{
								num9 = span.Length;
							}
							if (num9 != 0)
							{
								span = span.Slice(num9);
								num += num9;
								arg2 = num;
								arg++;
								goto IL_0211;
							}
						}
						while (--num5 >= 0)
						{
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							if (!span.IsEmpty && span[0] == '\\')
							{
								span = span.Slice(1);
								num++;
							}
							if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
							{
								if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num5 != 0)
								{
									num8 = runstack[--pos];
									UncaptureUntil(runstack[--pos]);
									_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
									if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (arg >= arg2)
									{
										continue;
									}
									goto IL_01ff;
								}
								break;
							}
							runtextpos = num;
							Capture(0, start, num);
							return true;
						}
						break;
						IL_01ff:
						num = --arg2;
						span = inputSpan.Slice(num);
						goto IL_0211;
						IL_0211:
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
						Capture(1, num8, num);
						_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.StackPush(ref runstack, ref pos, num8);
					}
					UncaptureUntil(num2);
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num3 >= num4)
					{
						break;
					}
					num = --num4;
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___UncPath_24 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___UncPath_24();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___UncPath_24()
	{
		pattern = "^\\\\\\\\[^\\\\/:*?\"<>|]+\\\\[^\\\\/:*?\"<>|]+(\\\\[^\\\\/:*?\"<>|]+)*\\\\?$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
