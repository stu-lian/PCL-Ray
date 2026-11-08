using System;
using System.Globalization;
using PCL.Core.App.Localization;

namespace PCL.Core.Utils;

public static class TimeUtils
{
	public static string GetTimeNow()
	{
		return DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
	}

	public static long GetTimeTick()
	{
		return Environment.TickCount64;
	}

	public static long GetUnixTimestamp()
	{
		return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	}

	public static DateTimeOffset FromUnixTimestamp(long unixTimestamp)
	{
		if (unixTimestamp < 0)
		{
			throw new ArgumentOutOfRangeException("unixTimestamp", "Unix 时间戳不能为负数。");
		}
		try
		{
			return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).ToLocalTime();
		}
		catch (ArgumentOutOfRangeException ex)
		{
			throw new ArgumentOutOfRangeException("unixTimestamp", "Unix 时间戳超出有效范围。", ex.Message);
		}
	}

	public static DateTimeOffset ToLocalTime(DateTimeOffset utcDate)
	{
		return utcDate.ToLocalTime();
	}

	public static string FormatUnixTimestamp(long unixTimestamp)
	{
		return Lang.Date(FromUnixTimestamp(unixTimestamp), "g");
	}
}
