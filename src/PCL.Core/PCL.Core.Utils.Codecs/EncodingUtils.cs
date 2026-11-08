using System;
using System.Text;

namespace PCL.Core.Utils.Codecs;

public static class EncodingUtils
{
	public static bool IsDefaultEncodingUtf8()
	{
		return Encoding.Default.CodePage == 65001;
	}

	public static bool IsDefaultEncodingGbk()
	{
		return Encoding.Default.CodePage == 936;
	}

	public static string DecodeBytes(byte[] bytes)
	{
		if (bytes.Length == 0)
		{
			return "";
		}
		Encoding encoding = EncodingDetector.DetectEncoding(bytes);
		if (!encoding.Equals(Encoding.Default))
		{
			ReadOnlySpan<byte> bytes2 = bytes.AsSpan();
			if (encoding.Equals(Encoding.UTF8) && bytes2.Length >= 3 && bytes2[0] == 239 && bytes2[1] == 187 && bytes2[2] == 191)
			{
				return encoding.GetString(bytes2.Slice(3));
			}
			if (encoding.Equals(Encoding.BigEndianUnicode) && bytes2.Length >= 2 && bytes2[0] == 254 && bytes2[1] == byte.MaxValue)
			{
				return encoding.GetString(bytes2.Slice(2));
			}
			if (encoding.Equals(Encoding.Unicode) && bytes2.Length >= 2 && bytes2[0] == byte.MaxValue && bytes2[1] == 254)
			{
				return encoding.GetString(bytes2.Slice(2));
			}
			if (encoding.Equals(Encoding.UTF32) && bytes2.Length >= 4 && bytes2[0] == byte.MaxValue && bytes2[1] == 254 && bytes2[2] == 0 && bytes2[3] == 0)
			{
				return encoding.GetString(bytes2.Slice(4));
			}
			if (encoding.CodePage == Encoding.GetEncoding("utf-32BE").CodePage && bytes2.Length >= 4 && bytes2[0] == 0 && bytes2[1] == 0 && bytes2[2] == 254 && bytes2[3] == byte.MaxValue)
			{
				return encoding.GetString(bytes2.Slice(4));
			}
			return encoding.GetString(bytes2);
		}
		try
		{
			string text = Encoding.UTF8.GetString(bytes);
			return text.Contains('\ufffd') ? Encodings.GB18030.GetString(bytes) : text;
		}
		catch (DecoderFallbackException)
		{
			return Encodings.GB18030.GetString(bytes);
		}
	}
}
