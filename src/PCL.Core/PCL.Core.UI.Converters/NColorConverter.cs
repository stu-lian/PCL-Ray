using System;
using System.ComponentModel;
using System.Globalization;

namespace PCL.Core.UI.Converters;

public class NColorConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(string)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string str)
		{
			return new NColor(str);
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (value is NColor nColor && destinationType == typeof(string))
		{
			return $"#{nColor.R:X2}{nColor.G:X2}{nColor.B:X2}{nColor.A:X2}";
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
