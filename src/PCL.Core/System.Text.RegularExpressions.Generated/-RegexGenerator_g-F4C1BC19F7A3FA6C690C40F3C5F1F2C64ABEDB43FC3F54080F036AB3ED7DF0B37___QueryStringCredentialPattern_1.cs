using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QueryStringCredentialPattern_1 : Regex
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
				if (num <= inputSpan.Length - 8)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 7; num2++)
					{
						int num3 = readOnlySpan.Slice(num2).IndexOfAny('&', '?');
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						if ((uint)(num2 + 4) >= (uint)readOnlySpan.Length)
						{
							break;
						}
						char c;
						ulong num4;
						if ((((c = readOnlySpan[num2 + 3]) < '\u0080') ? (("\0\0\0\0\u0a48\b\u0a48\b"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\u0016\0CDFGIJKLSTcdfgijklstKÅ")) && (long)((ulong)(-9221683184846635008L << (int)(num4 = (uint)(readOnlySpan[num2 + 4] - 69))) & (num4 - 64)) < 0L)
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				char c;
				if (readOnlySpan.IsEmpty || (((c = readOnlySpan[0]) != '&') & (c != '?')))
				{
					UncaptureUntil(0);
					return false;
				}
				if ((uint)readOnlySpan.Length < 2u)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (readOnlySpan[1])
				{
				case 'A':
				case 'a':
					if ((uint)readOnlySpan.Length < 13u || !readOnlySpan.Slice(2).StartsWith("ccess_to", StringComparison.OrdinalIgnoreCase) || ((((c = readOnlySpan[10]) | 0x20) != 107) & (c != 'K')) || !readOnlySpan.Slice(11).StartsWith("en", StringComparison.OrdinalIgnoreCase))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 13;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'R':
				case 'r':
					if ((uint)readOnlySpan.Length < 14u || !readOnlySpan.Slice(2).StartsWith("efresh_to", StringComparison.OrdinalIgnoreCase) || ((((c = readOnlySpan[11]) | 0x20) != 107) & (c != 'K')) || !readOnlySpan.Slice(12).StartsWith("en", StringComparison.OrdinalIgnoreCase))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 14;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'T':
				case 't':
					if ((uint)readOnlySpan.Length < 6u || (readOnlySpan[2] | 0x20) != 111 || ((((c = readOnlySpan[3]) | 0x20) != 107) & (c != 'K')) || !readOnlySpan.Slice(4).StartsWith("en", StringComparison.OrdinalIgnoreCase))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 6;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'P':
				case 'p':
					if ((uint)readOnlySpan.Length < 9u || !readOnlySpan.Slice(2).StartsWith("assword", StringComparison.OrdinalIgnoreCase))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 9;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'C':
				case 'c':
					if ((uint)readOnlySpan.Length < 14u || !readOnlySpan.Slice(2).StartsWith("lient_secret", StringComparison.OrdinalIgnoreCase))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 14;
					readOnlySpan = inputSpan.Slice(num);
					break;
				default:
					UncaptureUntil(0);
					return false;
				}
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '=')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				Capture(1, num2, num);
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length; i++)
				{
					if (!(((c = readOnlySpan[i]) < '\u0080') ? (("쇿\uffffﾾ\uffff\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\u0002\u0001&'d")))
					{
						break;
					}
				}
				if (i == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(i);
				Capture(0, start, runtextpos = num + i);
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QueryStringCredentialPattern_1 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QueryStringCredentialPattern_1();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QueryStringCredentialPattern_1()
	{
		pattern = "([?&](?:access_token|refresh_token|token|password|client_secret)=)[^&\\s]+";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
