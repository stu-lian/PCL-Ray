using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeLibVersion_30 : Regex
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
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_nonAscii_F9A6E3099A06C2F88B9C37A801C1898ECAA75390A63D9907AAD098D41F538907);
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
				span = inputSpan.Slice(num);
				int num6 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int num7 = 0;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '-')
				{
					return false;
				}
				num--;
				num4 = num;
				int i;
				for (i = 0; num > i; i++)
				{
					uint num8;
					if ((int)((uint)(-1074790400 << (int)(short)(num8 = (ushort)(inputSpan[num - i - 1] - 46))) & (num8 - 32)) >= 0)
					{
						break;
					}
				}
				if (i == 0)
				{
					return false;
				}
				num -= i;
				num5 = num;
				num4--;
				while (true)
				{
					if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != ':')
					{
						goto IL_00ae;
					}
					num--;
					num3 = num;
					if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
					{
						num--;
						if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 103)
						{
							num--;
							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
							{
								num--;
								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 111)
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 102)
									{
										num--;
										num2 = 0;
										goto IL_033c;
									}
								}
							}
						}
					}
					goto IL_01b2;
					IL_01b2:
					num = num3;
					span = inputSpan.Slice(num);
					if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
					{
						num--;
						if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
						{
							num--;
							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 100)
							{
								num--;
								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 97)
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 111)
									{
										num--;
										int num9 = 0;
										while (num9 < 2)
										{
											if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 108)
											{
												num--;
												num9++;
												continue;
											}
											goto IL_00ae;
										}
										if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 109)
										{
											num--;
											if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 102)
											{
												num--;
												num2 = 1;
												goto IL_033c;
											}
										}
									}
								}
							}
						}
					}
					goto IL_00ae;
					IL_033c:
					while (true)
					{
						if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == ':')
						{
							num--;
							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
							{
								num--;
								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 103)
								{
									num--;
									if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
									{
										num--;
										if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 111)
										{
											num--;
											if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 102)
											{
												num--;
												if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 116)
												{
													num--;
													if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 102)
													{
														num--;
														if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 97)
														{
															num--;
															if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 114)
															{
																num--;
																if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 99)
																{
																	num--;
																	if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
																	{
																		num--;
																		if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 110)
																		{
																			num--;
																			if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 105)
																			{
																				num--;
																				if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 109)
																				{
																					num--;
																					if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == '.')
																					{
																						num--;
																						if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 116)
																						{
																							num--;
																							if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 101)
																							{
																								num--;
																								if ((uint)(num - 1) < inputSpan.Length && (inputSpan[num - 1] | 0x20) == 110)
																								{
																									break;
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
						if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num2 != 0)
						{
							if (num2 != 1)
							{
								continue;
							}
							goto IL_00ae;
						}
						goto IL_01b2;
					}
					break;
					IL_00ae:
					if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num4 <= num5)
					{
						return false;
					}
					num = ++num5;
				}
				num--;
				num = num6;
				span = inputSpan.Slice(num);
				int num10 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_nonAscii_F9A6E3099A06C2F88B9C37A801C1898ECAA75390A63D9907AAD098D41F538907);
				if (num10 < 0)
				{
					num10 = span.Length;
				}
				if (num10 == 0)
				{
					return false;
				}
				span = span.Slice(num10);
				Capture(0, start, runtextpos = num + num10);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeLibVersion_30 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeLibVersion_30();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeLibVersion_30()
	{
		pattern = "(?<=net\\.minecraftforge:(?:forge|fmlloader):[0-9.]+-)[0-9a-zA-Z._+-]+";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
