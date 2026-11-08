using System;
using System.Globalization;
using System.IO;

namespace PCL.Core.IO;

public class ByteStream(Stream stream)
{
	private static readonly string[] _Units = new string[9] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" };

	public long Length => stream.Length;

	public string GetReadableLength()
	{
		return GetReadableLength(Length);
	}

	public static string GetReadableLength(long length, int startUnit = 0, IFormatProvider? provider = null)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startUnit, _Units.Length, "startUnit");
		ArgumentOutOfRangeException.ThrowIfLessThan(startUnit, 0, "startUnit");
		bool flag = length < 0;
		decimal num = (flag ? (-length) : length);
		if (num == 0m)
		{
			return "0 B";
		}
		int num2 = startUnit;
		decimal num3 = num;
		while (num3 >= 1024m && num2 < _Units.Length - 1)
		{
			num3 /= 1024m;
			num2++;
		}
		if (num2 >= _Units.Length)
		{
			throw new ArgumentOutOfRangeException("length", "Value too large for predefined units.");
		}
		return (flag ? "-" : "") + num3.ToString("0.##", provider ?? CultureInfo.InvariantCulture) + " " + _Units[num2];
	}
}
