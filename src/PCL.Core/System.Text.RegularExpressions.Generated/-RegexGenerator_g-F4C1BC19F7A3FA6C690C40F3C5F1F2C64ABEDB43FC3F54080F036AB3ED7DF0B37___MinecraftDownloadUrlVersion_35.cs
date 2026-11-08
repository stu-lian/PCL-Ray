using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.504")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftDownloadUrlVersion_35 : Regex
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				span = inputSpan.Slice(num);
				int num2 = num;
				if (_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '/')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '/')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] != '/')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '\n')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 103)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 106)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 111)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 109)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || inputSpan[num - 1] == '\n')
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 114)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 101)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 104)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 99)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 110)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 117)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 97)
				{
					return false;
				}
				num--;
				if ((uint)(num - 1) >= inputSpan.Length || (inputSpan[num - 1] | 0x20) != 108)
				{
					return false;
				}
				num--;
				num = num2;
				span = inputSpan.Slice(num);
				int num3 = span.IndexOf('/');
				if (num3 < 0)
				{
					num3 = span.Length;
				}
				span = span.Slice(num3);
				Capture(0, start, runtextpos = num + num3);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftDownloadUrlVersion_35 Instance = new _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftDownloadUrlVersion_35();

	private _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftDownloadUrlVersion_35()
	{
		pattern = "(?<=launcher.mojang.com/mc/game/)[^/]*";
		roptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
