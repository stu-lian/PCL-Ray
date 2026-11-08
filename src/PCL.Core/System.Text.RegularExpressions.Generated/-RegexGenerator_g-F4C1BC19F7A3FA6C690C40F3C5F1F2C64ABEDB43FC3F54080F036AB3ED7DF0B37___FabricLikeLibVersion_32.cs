using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FabricLikeLibVersion_32 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (!TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
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
				span = inputSpan.Slice(num);
				int num6 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != ':')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 121)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 100)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 116)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != ':')
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				num2 = num;
				int num7 = num;
				int capturePosition = Crawlpos();
				num3 = num;
				if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 99)
				{
					num--;
					if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 109)
					{
						num--;
						if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 99)
						{
							num--;
							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 105)
							{
								num--;
								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 98)
									{
										num--;
										if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 97)
										{
											num--;
											if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 102)
											{
												num--;
												Capture(2, num3, num);
												goto IL_0775;
											}
										}
									}
								}
							}
						}
					}
				}
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				num4 = num;
				if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 99)
				{
					num--;
					if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 109)
					{
						num--;
						if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 116)
						{
							num--;
							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 108)
							{
								num--;
								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 105)
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 117)
									{
										num--;
										if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 113)
										{
											num--;
											Capture(3, num4, num);
											goto IL_0775;
										}
									}
								}
							}
						}
					}
				}
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				num5 = num;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 105)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 98)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 102)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 121)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 108)
				{
					UncaptureUntil(0);
					return false;
				}
				num--;
				Capture(4, num5, num);
				goto IL_0775;
				IL_0775:
				Capture(1, num2, num);
				num = num6;
				span = inputSpan.Slice(num);
				int num8 = span.IndexOf('"');
				if (num8 < 0)
				{
					num8 = span.Length;
				}
				span = span.Slice(num8);
				Capture(0, start, runtextpos = num + num8);
				return true;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num9)
				{
					while (Crawlpos() > num9)
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FabricLikeLibVersion_32 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FabricLikeLibVersion_32();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FabricLikeLibVersion_32()
	{
		pattern = "(?<=((fabricmc)|(quiltmc)|(legacyfabric)):intermediary:)[^\"]*";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 5;
	}
}
