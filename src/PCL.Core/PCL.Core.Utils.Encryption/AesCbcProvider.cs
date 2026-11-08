using System;
using System.IO;
using System.Security.Cryptography;

namespace PCL.Core.Utils.Encryption;

[Obsolete("Do not use this AES mode for Encryption")]
public sealed class AesCbcProvider : IEncryptionProvider
{
	private const int SaltSize = 32;

	private const int IvSize = 16;

	public static AesCbcProvider Instance { get; } = new AesCbcProvider();

	public bool IsSupported => false;

	public byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		using Aes aes = Aes.Create();
		aes.KeySize = 256;
		aes.BlockSize = 128;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		ReadOnlySpan<byte> readOnlySpan = data.Slice(0, 32);
		ReadOnlySpan<byte> readOnlySpan2 = data.Slice(32, 16);
		aes.IV = readOnlySpan2.ToArray();
		if (data.Length < readOnlySpan.Length + readOnlySpan2.Length)
		{
			throw new ArgumentException("AES-CBC: Can not decrypt data, the encrypted data is broken");
		}
		using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(key.ToArray(), readOnlySpan.ToArray(), 1000))
		{
			aes.Key = rfc2898DeriveBytes.GetBytes(aes.KeySize / 8);
		}
		using MemoryStream memoryStream = new MemoryStream();
		using MemoryStream stream = new MemoryStream(data.Slice(48).ToArray());
		using CryptoStream cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
		cryptoStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
	{
		throw new NotSupportedException("You should no longer use AES-CBC as your encryption method");
	}
}
