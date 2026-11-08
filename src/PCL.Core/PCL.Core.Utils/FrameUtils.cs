using System;
using System.Diagnostics;

namespace PCL.Core.Utils;

public static class FrameUtils
{
	public static long Frequency { get; }

	static FrameUtils()
	{
		Frequency = Stopwatch.Frequency;
	}

	public static long NowStamp()
	{
		return Stopwatch.GetTimestamp();
	}

	public static long StampToFrameIndex(long startStamp, int fps)
	{
		long num = NowStamp() - startStamp;
		if (num <= 0)
		{
			return 0L;
		}
		return num * fps / Frequency;
	}

	public static long TimeSpanToFrameIndex(TimeSpan startTime, TimeSpan currentTime, int fps)
	{
		if (currentTime < startTime)
		{
			return 0L;
		}
		return (long)((currentTime - startTime).TotalSeconds * (double)fps);
	}
}
