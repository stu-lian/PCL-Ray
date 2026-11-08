using System;

namespace PCL.Core.Utils.Encryption;

public interface IEncryptionProvider
{
	static bool IsSupported { get; }

	byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key);

	byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key);
}
