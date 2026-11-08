using System;
using System.Globalization;
using System.Windows.Media;

namespace PCL.Core.UI.Controls.SvgIcon;

internal static class SvgPaintParser
{
	private static readonly BrushConverter _BrushConverter = new BrushConverter();

	public static Brush? ParseBrush(string? value, Brush currentColorBrush)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		string text = value.Trim();
		if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		if (text.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
		{
			return currentColorBrush;
		}
		if (_TryParseRgbFunction(text, out Brush brush))
		{
			return brush;
		}
		try
		{
			return _BrushConverter.ConvertFromInvariantString(text) as Brush;
		}
		catch
		{
			return currentColorBrush;
		}
	}

	private static bool _TryParseRgbFunction(string value, out Brush brush)
	{
		brush = null;
		if (!value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int num = value.IndexOf('(');
		int num2 = value.LastIndexOf(')');
		if (num < 0 || num2 <= num)
		{
			return false;
		}
		int num3 = num + 1;
		string[] array = value.Substring(num3, num2 - num3).Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length < 3)
		{
			return false;
		}
		byte? b = _ParseColorComponent(array[0]);
		byte? b2 = _ParseColorComponent(array[1]);
		byte? b3 = _ParseColorComponent(array[2]);
		byte a = ((array.Length >= 4) ? _ParseAlpha(array[3]) : byte.MaxValue);
		if (!b.HasValue || !b2.HasValue || !b3.HasValue)
		{
			return false;
		}
		brush = new SolidColorBrush(Color.FromArgb(a, b.Value, b2.Value, b3.Value));
		return true;
	}

	private static byte? _ParseColorComponent(string value)
	{
		if (value.EndsWith('%'))
		{
			if (!double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return null;
			}
			return (byte)Math.Clamp(Math.Round(result / 100.0 * 255.0), 0.0, 255.0);
		}
		if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return null;
		}
		return (byte)Math.Clamp(Math.Round(result2), 0.0, 255.0);
	}

	private static byte _ParseAlpha(string value)
	{
		if (value.EndsWith('%'))
		{
			if (double.TryParse(value.Substring(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return (byte)Math.Clamp(Math.Round(result / 100.0 * 255.0), 0.0, 255.0);
			}
			return byte.MaxValue;
		}
		if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return byte.MaxValue;
		}
		return (byte)Math.Clamp(Math.Round((result2 <= 1.0) ? (result2 * 255.0) : result2), 0.0, 255.0);
	}
}
