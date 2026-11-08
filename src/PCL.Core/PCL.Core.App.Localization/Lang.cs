using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using Humanizer;
using PCL.Core.App.IoC;
using PCL.Core.IO;

namespace PCL.Core.App.Localization;

public static class Lang
{
	private static readonly HashSet<string> _UnrestrictedTimeZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "China Standard Time" };

	private static readonly HashSet<string> _UnrestrictedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zh-CN" };

	public static CultureInfo Culture { get; private set; } = CultureInfo.CurrentCulture;

	public static bool IsFeaturesUnrestricted
	{
		get
		{
			if (!Config.Debug.AllowRestrictedFeature)
			{
				if (_UnrestrictedTimeZones.Contains(TimeZoneInfo.Local.Id))
				{
					if (!_UnrestrictedCultures.Contains(CultureInfo.CurrentCulture.Name))
					{
						return _UnrestrictedCultures.Contains(CultureInfo.CurrentUICulture.Name);
					}
					return true;
				}
				return false;
			}
			return true;
		}
	}

	public static bool IsChineseMainland => Culture.Name == "zh-CN";

	internal static void SyncCulture(CultureInfo culture)
	{
		Culture = culture;
	}

	public static string Text(string key)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(key, "key");
		if (Application.Current?.TryFindResource(key) is string result)
		{
			return result;
		}
		if (_LifecycleSafeFindResource(key) is string result2)
		{
			return result2;
		}
		return key;
	}

	public static string Text(string key, params object?[] args)
	{
		return string.Format(Culture, Text(key), args);
	}

	public static string Date(DateTime value, string format = "G")
	{
		return value.ToString(format, Culture);
	}

	public static string Date(DateTimeOffset value, string format = "G")
	{
		return value.ToString(format, Culture);
	}

	public static string TimeSpan(TimeSpan span, int precision = 2, bool addAffixes = true, TimeUnit maxUnit = (TimeUnit)7, TimeUnit minUnit = (TimeUnit)3)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		bool flag = span.TotalMilliseconds < 0.0;
		if (flag)
		{
			span = span.Negate();
		}
		string text = TimeSpanHumanizeExtensions.Humanize(span, precision, Culture, maxUnit, minUnit, ", ", false);
		if (!addAffixes)
		{
			return text;
		}
		return Text(flag ? "Common.Format.TimeSpan.Past" : "Common.Format.TimeSpan.Future", text);
	}

	public static string Number<T>(T value, string? format = null) where T : IFormattable
	{
		return value.ToString(format, Culture);
	}

	public static string CompactNumber(long value)
	{
		double num = Math.Abs((double)value);
		int num2 = ((value >= 0) ? 1 : (-1));
		if (_IsEastAsianCulture(Culture.Name))
		{
			if (!(num > 1000000000000.0))
			{
				if (!(num > 100000000.0))
				{
					if (num > 100000.0)
					{
						return Text("Common.Format.Number.Digit1", Number((double)num2 * Math.Round(num / 10000.0), "N0"));
					}
					return Number(value, "N0");
				}
				return Text("Common.Format.Number.Digit2", Number((double)num2 * num / 100000000.0, "N2"));
			}
			return Text("Common.Format.Number.Digit3", Number((double)num2 * num / 1000000000000.0, "N2"));
		}
		if (!(num > 1000000000.0))
		{
			if (!(num > 1000000.0))
			{
				if (num > 10000.0)
				{
					return Text("Common.Format.Number.Digit1", Number((double)num2 * Math.Round(num / 1000.0), "N0"));
				}
				return Number(value, "N0");
			}
			return Text("Common.Format.Number.Digit2", Number((double)num2 * num / 1000000.0, "N2"));
		}
		return Text("Common.Format.Number.Digit3", Number((double)num2 * num / 1000000000.0, "N2"));
	}

	public static string FileSize(long length, int startUnit = 0)
	{
		return ByteStream.GetReadableLength(length, startUnit, Culture);
	}

	private static bool _IsEastAsianCulture(string cultureName)
	{
		bool flag = cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
		if (!flag)
		{
			bool flag2;
			switch (cultureName)
			{
			case "ja-JP":
			case "ko-KR":
			case "lzh":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		return flag;
	}

	private static object? _LifecycleSafeFindResource(string key)
	{
		try
		{
			return Lifecycle.CurrentApplication.TryFindResource(key);
		}
		catch (InvalidOperationException)
		{
			return null;
		}
		catch (NullReferenceException)
		{
			return null;
		}
	}
}
