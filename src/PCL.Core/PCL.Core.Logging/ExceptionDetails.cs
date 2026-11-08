using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using PCL.Core.App.Localization;

namespace PCL.Core.Logging;

public static class ExceptionDetails
{
	private const int MaxSingleMessageLength = 4096;

	private const int MaxCombinedMessageLength = 8192;

	public static string GetUserReason(Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception, "exception");
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		Exception ex = exception;
		int num = 0;
		while (ex != null && num < 32)
		{
			string text = ex.Message?.Trim();
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "$$", StringComparison.Ordinal))
			{
				text = _TruncateForUser(_RedactSensitiveText(text));
				if (hashSet.Add(text))
				{
					list.Add(text);
				}
			}
			num++;
			ex = ex.InnerException;
		}
		return _TruncateForUser(string.Join(Environment.NewLine, list), 8192);
	}

	private static string _TruncateForUser(string text, int? maxLength = null)
	{
		int num = maxLength ?? 4096;
		if (text.Length > num)
		{
			return text.Substring(0, num) + Environment.NewLine + "…";
		}
		return text;
	}

	private static Regex _KeyValueCredentialPattern()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___KeyValueCredentialPattern_0.Instance;
	}

	private static Regex _QueryStringCredentialPattern()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QueryStringCredentialPattern_1.Instance;
	}

	private static Regex _BearerTokenPattern()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___BearerTokenPattern_2.Instance;
	}

	private static string _RedactSensitiveText(string text)
	{
		text = _KeyValueCredentialPattern().Replace(text, "$1[redacted]");
		text = _QueryStringCredentialPattern().Replace(text, "$1[redacted]");
		text = _BearerTokenPattern().Replace(text, "Bearer [redacted]");
		return text;
	}

	public static string GetDebugDetails(Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception, "exception");
		return exception.ToString();
	}

	public static string Compose(string summary, Exception? exception = null)
	{
		return Compose(summary, (exception == null) ? null : GetDebugDetails(exception));
	}

	public static string Compose(string summary, string? details)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(summary, "summary");
		if (!string.IsNullOrWhiteSpace(details))
		{
			return Lang.Text("SystemDialog.Error.Detail.WithException", summary.TrimEnd(), details);
		}
		return summary;
	}
}
