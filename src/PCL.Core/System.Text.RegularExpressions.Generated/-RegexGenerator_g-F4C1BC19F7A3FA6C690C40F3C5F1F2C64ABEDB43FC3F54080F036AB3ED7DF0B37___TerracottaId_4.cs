using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___TerracottaId_4 : Regex
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
				if (num <= inputSpan.Length - 29)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 28; num2++)
					{
						int num3 = readOnlySpan.Slice(num2 + 5).IndexOf('-');
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						char c;
						if ((((c = readOnlySpan[num2]) < '\u0080') ? char.IsAsciiLetterOrDigit(c) : RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ")) && (((c = readOnlySpan[num2 + 1]) < '\u0080') ? char.IsAsciiLetterOrDigit(c) : RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ")))
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				char c;
				for (int i = 0; i < 4; i++)
				{
					int start2 = num;
					if ((uint)readOnlySpan.Length < 6u || (((c = readOnlySpan[0]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[1]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[2]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[3]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[4]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || readOnlySpan[5] != '-')
					{
						UncaptureUntil(0);
						return false;
					}
					num += 6;
					readOnlySpan = inputSpan.Slice(num);
					Capture(1, start2, num);
				}
				if ((uint)readOnlySpan.Length < 5u || (((c = readOnlySpan[0]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[1]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[2]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[3]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))) || (((c = readOnlySpan[4]) < '\u0080') ? (!char.IsAsciiLetterOrDigit(c)) : (!RegexRunner.CharInClass(c, "\0\b\00:A[a{KÅ"))))
				{
					UncaptureUntil(0);
					return false;
				}
				Capture(0, start, runtextpos = num + 5);
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

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___TerracottaId_4 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___TerracottaId_4();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___TerracottaId_4()
	{
		pattern = "([0-9A-Z]{5}-){4}[0-9A-Z]{5}";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
