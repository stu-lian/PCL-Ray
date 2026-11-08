using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace PCL.Core.UI.Controls.SvgIcon;

internal static class SvgNumberParser
{
	private static Regex _NumberRegex()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NumberRegex_3.Instance;
	}

	public static double TryParse(string? value, double fallback = 0.0)
	{
		return TryParseNullable(value) ?? fallback;
	}

	public static double? TryParseNullable(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		Match match = _NumberRegex().Match(value.Trim());
		if (!match.Success)
		{
			return null;
		}
		if (!double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return null;
		}
		return result;
	}

	public static double[] ParseNumberList(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Array.Empty<double>();
		}
		MatchCollection matchCollection = _NumberRegex().Matches(value);
		double[] array = new double[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			array[i] = double.Parse(matchCollection[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
		}
		return array;
	}
}
