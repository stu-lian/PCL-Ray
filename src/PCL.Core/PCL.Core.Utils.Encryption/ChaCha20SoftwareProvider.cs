using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PCL.Core.Utils.Encryption;

public class ChaCha20SoftwareProvider : IEncryptionProvider
{
	public static ChaCha20SoftwareProvider Instance { get; } = new ChaCha20SoftwareProvider();

	private static ReadOnlySpan<uint> Sigma => new uint[4] { 1634760805u, 857760878u, 2036477234u, 1797285236u };

	public static bool IsSupported => true;

	public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		byte[] array = new byte[data.Length + 12];
		Span<byte> span = array.AsSpan(0, 12);
		RandomNumberGenerator.Fill(span);
		_Process(data, array.AsSpan(12), key, span);
		return array;
	}

	public byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		if (data.Length < 12)
		{
			throw new ArgumentException("数据长度不足以包含 Nonce");
		}
		ReadOnlySpan<byte> nonce = data.Slice(0, 12);
		ReadOnlySpan<byte> input = data.Slice(12);
		byte[] array = new byte[input.Length];
		_Process(input, array, key, nonce);
		return array;
	}

	private static void _Process(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
	{
		if (key.Length != 32)
		{
			throw new ArgumentException("Key 必须为 32 字节");
		}
		if (nonce.Length != 12)
		{
			throw new ArgumentException("Nonce 必须为 12 字节");
		}
		Span<uint> span = stackalloc uint[16];
		Sigma.CopyTo(span.Slice(0, 4));
		for (int i = 0; i < 8; i++)
		{
			span[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));
		}
		span[12] = 0u;
		span[13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
		span[14] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
		span[15] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));
		Span<uint> span2 = stackalloc uint[16];
		int j = 0;
		for (int length = input.Length; j < length; j += 64)
		{
			_GenerateBlock(span2, span);
			span[12]++;
			int num = Math.Min(64, length - j);
			ReadOnlySpan<byte> readOnlySpan = input.Slice(j, num);
			Span<byte> span3 = output.Slice(j, num);
			Span<byte> span4 = MemoryMarshal.AsBytes(span2);
			for (int k = 0; k < num; k++)
			{
				span3[k] = (byte)(readOnlySpan[k] ^ span4[k]);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _GenerateBlock(Span<uint> x, ReadOnlySpan<uint> input)
	{
		input.CopyTo(x);
		for (int i = 0; i < 10; i++)
		{
			_QuarterRound(x, 0, 4, 8, 12);
			_QuarterRound(x, 1, 5, 9, 13);
			_QuarterRound(x, 2, 6, 10, 14);
			_QuarterRound(x, 3, 7, 11, 15);
			_QuarterRound(x, 0, 5, 10, 15);
			_QuarterRound(x, 1, 6, 11, 12);
			_QuarterRound(x, 2, 7, 8, 13);
			_QuarterRound(x, 3, 4, 9, 14);
		}
		for (int j = 0; j < 16; j++)
		{
			x[j] += input[j];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _QuarterRound(Span<uint> x, int a, int b, int c, int d)
	{
		x[a] += x[b];
		x[d] ^= x[a];
		x[d] = BitOperations.RotateLeft(x[d], 16);
		x[c] += x[d];
		x[b] ^= x[c];
		x[b] = BitOperations.RotateLeft(x[b], 12);
		x[a] += x[b];
		x[d] ^= x[a];
		x[d] = BitOperations.RotateLeft(x[d], 8);
		x[c] += x[d];
		x[b] ^= x[c];
		x[b] = BitOperations.RotateLeft(x[b], 7);
	}
}
