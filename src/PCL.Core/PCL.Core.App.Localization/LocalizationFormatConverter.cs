using System;
using System.Globalization;
using System.Windows.Data;

namespace PCL.Core.App.Localization;

public sealed class LocalizationFormatConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return null;
		}
		if (!(parameter is string text) || string.IsNullOrWhiteSpace(text))
		{
			return value;
		}
		if (value is IFormattable formattable)
		{
			return formattable.ToString(text, Lang.Culture);
		}
		return string.Format(Lang.Culture, "{0}", value);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
