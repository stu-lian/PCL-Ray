using System;
using System.Security.Cryptography;

namespace PCL.Core.Utils.Encryption;

public class AesGcmProvider : IEncryptionProvider
{
	private const int NonceSize = 12;

	private const int TagSize = 16;

	public static AesGcmProvider Instance { get; } = new AesGcmProvider();

	public bool IsSupported => AesGcm.IsSupported;

	public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		byte[] array = new byte[12];
		RandomNumberGenerator.Fill(array);
		byte[] array2 = new byte[16];
		byte[] array3 = new byte[data.Length];
		using (AesGcm aesGcm = new AesGcm(key, 16))
		{
			aesGcm.Encrypt(array, data, array3, array2);
		}
		byte[] array4 = new byte[28 + array3.Length];
		Buffer.BlockCopy(array, 0, array4, 0, 12);
		Buffer.BlockCopy(array2, 0, array4, 12, 16);
		Buffer.BlockCopy(array3, 0, array4, 28, array3.Length);
		return array4;
	}

	public byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		if (data.Length < 28)
		{
			throw new ArgumentException("加密数据长度不足。");
		}
		ReadOnlySpan<byte> nonce = data.Slice(0, 12);
		ReadOnlySpan<byte> tag = data.Slice(12, 16);
		ReadOnlySpan<byte> ciphertext = data.Slice(28);
		byte[] array = new byte[ciphertext.Length];
		using AesGcm aesGcm = new AesGcm(key, 16);
		aesGcm.Decrypt(nonce, ciphertext, tag, array);
		return array;
	}
}
