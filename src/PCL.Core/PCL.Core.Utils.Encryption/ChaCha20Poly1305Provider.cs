using System;
using System.Security.Cryptography;

namespace PCL.Core.Utils.Encryption;

public sealed class ChaCha20Poly1305Provider : IEncryptionProvider
{
	private const int NonceSize = 12;

	private const int TagSize = 16;

	private const int KeySize = 32;

	private const int SaltSize = 16;

	private static readonly byte[] _Info = "PCL.Core.Utils.Encryption.ChaCha20"u8.ToArray();

	public static ChaCha20Poly1305Provider Instance { get; } = new ChaCha20Poly1305Provider();

	public bool IsSupported => ChaCha20Poly1305.IsSupported;

	public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		byte[] array = new byte[16];
		byte[] array2 = new byte[12];
		byte[] array3 = new byte[16];
		RandomNumberGenerator.Fill(array);
		RandomNumberGenerator.Fill(array2);
		RandomNumberGenerator.Fill(array3);
		Span<byte> span = stackalloc byte[32];
		_DeriveKey(key, array, span);
		using ChaCha20Poly1305 chaCha20Poly = new ChaCha20Poly1305(span);
		byte[] array4 = new byte[data.Length];
		chaCha20Poly.Encrypt(array2, data, array4, array3);
		byte[] array5 = new byte[28 + array4.Length + 16];
		Span<byte> span2 = array5.AsSpan();
		array.CopyTo(span2.Slice(0, 16));
		array2.CopyTo(span2.Slice(16, 12));
		array3.CopyTo(span2.Slice(28, 16));
		array4.CopyTo(span2.Slice(44, array4.Length));
		return array5;
	}

	public byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		if (data.Length < 44)
		{
			throw new ArgumentException("Invalid encrypted data length");
		}
		ReadOnlySpan<byte> salt = data.Slice(0, 16);
		ReadOnlySpan<byte> nonce = data.Slice(16, 12);
		ReadOnlySpan<byte> tag = data.Slice(28, 16);
		ReadOnlySpan<byte> ciphertext = data.Slice(44);
		Span<byte> span = stackalloc byte[32];
		_DeriveKey(key, salt, span);
		using ChaCha20Poly1305 chaCha20Poly = new ChaCha20Poly1305(span);
		byte[] array = new byte[ciphertext.Length];
		chaCha20Poly.Decrypt(nonce, ciphertext, tag, array);
		return array;
	}

	private static void _DeriveKey(ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> outputKey)
	{
		HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, outputKey, salt, _Info.AsSpan());
	}
}
