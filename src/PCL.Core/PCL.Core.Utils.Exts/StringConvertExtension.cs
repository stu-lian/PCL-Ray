using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace PCL.Core.Utils.Exts;

public static class StringConvertExtension
{
	public static object? Convert(string? value, Type targetType)
	{
		ArgumentNullException.ThrowIfNull(targetType, "targetType");
		if (targetType == typeof(string))
		{
			return value;
		}
		if (value == null)
		{
			if (!targetType.IsValueType || (object)Nullable.GetUnderlyingType(targetType) != null)
			{
				return null;
			}
			return Activator.CreateInstance(targetType);
		}
		TypeConverter converter = TypeDescriptor.GetConverter(targetType);
		if (converter.CanConvertFrom(typeof(string)))
		{
			return converter.ConvertFromInvariantString(value);
		}
		if (typeof(IConvertible).IsAssignableFrom(targetType))
		{
			return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
		}
		if (targetType.IsEnum)
		{
			return Enum.Parse(targetType, value, ignoreCase: true);
		}
		MethodInfo method = targetType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(string) }, null);
		if ((object)method != null)
		{
			return method.Invoke(null, new object[1] { value });
		}
		throw new NotSupportedException("无法将字符串转换为类型 " + targetType.FullName);
	}

	public static T? Convert<T>(this string? value)
	{
		object obj = Convert(value, typeof(T));
		if (obj == null)
		{
			return default;
		}
		return (T)obj;
	}
}
