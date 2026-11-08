using System;

namespace PCL.Core.Utils.Exts;

public static class ByteExtension
{
	extension(ReadOnlySpan<byte> bytes)
	{
		public string ToHexString()
		{
			return Convert.ToHexString(bytes).ToLower();
		}

		public string FromByteToB64()
		{
			return Convert.ToBase64String(bytes);
		}

		public string FromBytesToB64UrlSafe()
		{
			return bytes.FromByteToB64().FromB64ToB64UrlSafe();
		}
	}
}
