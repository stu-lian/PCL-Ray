using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils;

public static class VarIntHelper
{
	public static byte[] Encode(ulong value)
	{
		using MemoryStream memoryStream = new MemoryStream();
		do
		{
			byte b = (byte)(value & 0x7F);
			value >>= 7;
			if (value != 0L)
			{
				b |= 0x80;
			}
			memoryStream.WriteByte(b);
		}
		while (value != 0L);
		return memoryStream.ToArray();
	}

	public static byte[] Encode(uint value)
	{
		return Encode((ulong)value);
	}

	public static ulong Decode(byte[] bytes, out int readLength)
	{
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		ulong num = 0uL;
		int num2 = 0;
		int num3 = 0;
		foreach (byte b in bytes)
		{
			if (num3 >= 10)
			{
				throw new FormatException("VarInt exceeds maximum length");
			}
			num |= (ulong)((long)(b & 0x7F) << num2);
			num3++;
			if ((b & 0x80) == 0)
			{
				readLength = num3;
				return num;
			}
			num2 += 7;
		}
		throw new FormatException("Incomplete VarInt encoding");
	}

	public static uint DecodeUInt(byte[] bytes, out int readLength)
	{
		ulong num = Decode(bytes, out readLength);
		if (num > uint.MaxValue)
		{
			throw new OverflowException("Decoded value exceeds UInt32 range");
		}
		return (uint)num;
	}

	public static async Task<ulong> ReadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
	{
		ulong result = 0uL;
		int shift = 0;
		int bytesRead = 0;
		byte[] buffer = new byte[1];
		while (true)
		{
			if (await stream.ReadAsync(buffer, 0, 1, cancellationToken) == 0)
			{
				throw new EndOfStreamException();
			}
			byte b = buffer[0];
			bytesRead++;
			if (bytesRead > 10)
			{
				throw new FormatException("VarInt exceeds maximum length");
			}
			result |= (ulong)((long)(b & 0x7F) << shift);
			if ((b & 0x80) == 0)
			{
				break;
			}
			shift += 7;
		}
		return result;
	}

	public static async Task<uint> ReadUIntFromStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
	{
		ulong num = await ReadFromStreamAsync(stream, cancellationToken);
		if (num > uint.MaxValue)
		{
			throw new OverflowException("Decoded value exceeds UInt32 range");
		}
		return (uint)num;
	}

	public static long DecodeZigZag(ulong value)
	{
		return (long)((value >> 1) ^ (0L - (value & 1)));
	}

	public static ulong EncodeZigZag(ulong value)
	{
		return (value << 1) ^ (value >> 63);
	}
}
