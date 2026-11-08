using System;
using System.IO;
using System.Text;

namespace PCL.Core.Utils.Codecs;

public static class EncodingDetector
{
	public static Encoding DetectEncoding(Stream stream, bool readFromBegin = false)
	{
		if (!stream.CanRead)
		{
			throw new ArgumentException("流必须支持读操作");
		}
		if (!stream.CanSeek)
		{
			throw new ArgumentException("流必须支持 Seek 操作");
		}
		long position = stream.Position;
		if (readFromBegin)
		{
			stream.Seek(0L, SeekOrigin.Begin);
		}
		try
		{
			return _DetectByBom(stream, position) ?? _DetectWithoutBOM(stream, position) ?? Encoding.Default;
		}
		finally
		{
			stream.Position = position;
		}
	}

	public static Encoding DetectEncoding(byte[] bytes)
	{
		return DetectEncoding(new MemoryStream(bytes), readFromBegin: true);
	}

	private static Encoding? _DetectByBom(Stream stream, long originalPosition)
	{
		stream.Position = originalPosition;
		long num = Math.Min(stream.Length - stream.Position, 4L);
		byte[] array = new byte[num];
		if (stream.Read(array, 0, array.Length) != num)
		{
			throw new Exception("无法获取样本长度");
		}
		if (num >= 3 && array != null && array.Length == 3 && array[0] == 239 && array[1] == 187 && array[2] == 191)
		{
			return Encoding.UTF8;
		}
		if (num >= 2)
		{
			if (array != null && array.Length == 2 && array[0] == 254 && array[1] == byte.MaxValue)
			{
				return Encoding.BigEndianUnicode;
			}
			if (array != null && array.Length == 2 && array[0] == byte.MaxValue && array[1] == 254)
			{
				if (num >= 4 && array != null && array.Length == 4 && array[2] == 0 && array[3] == 0)
				{
					return Encoding.UTF32;
				}
				return Encoding.Unicode;
			}
		}
		if (num >= 4)
		{
			if (array != null && array.Length == 4 && array[0] == 0 && array[1] == 0 && array[2] == 254 && array[3] == byte.MaxValue)
			{
				return Encoding.GetEncoding("utf-32BE");
			}
			if (array != null && array.Length == 4 && array[0] == byte.MaxValue && array[1] == 254 && array[2] == 0 && array[3] == 0)
			{
				return Encoding.UTF32;
			}
		}
		return null;
	}

	private static Encoding? _DetectWithoutBOM(Stream stream, long originalPosition)
	{
		if (!_IsValidUtf8(stream, originalPosition))
		{
			return null;
		}
		return Encoding.UTF8;
	}

	private static bool _IsValidUtf8(Stream stream, long originalPosition)
	{
		byte[] array = new byte[1024];
		stream.Position = originalPosition;
		try
		{
			string s = Encoding.UTF8.GetString(array);
			return Encoding.UTF8.GetBytes(s).SequenceEqual(array);
		}
		catch
		{
			return false;
		}
	}
}
