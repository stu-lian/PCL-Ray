using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Hash;

public class MurmurHash2Provider : IHashProvider
{
	public static MurmurHash2Provider Instance { get; } = new MurmurHash2Provider();

	public int Length => 8;

	public byte[] ComputeHash(ReadOnlySpan<byte> data)
	{
		List<byte> list = new List<byte>(data.Length);
		ReadOnlySpan<byte> readOnlySpan = data;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			if (b != 9 && b != 10 && b != 13 && b != 32)
			{
				list.Add(b);
			}
		}
		return _ComputeHash(CollectionsMarshal.AsSpan(list));
	}

	public byte[] ComputeHash(string input, Encoding? encoding = null)
	{
		if (encoding == null)
		{
			encoding = Encoding.UTF8;
		}
		return ComputeHash(encoding.GetBytes(input));
	}

	public byte[] ComputeHash(Stream input)
	{
		using MemoryStream memoryStream = new MemoryStream();
		input.CopyTo(memoryStream);
		return ComputeHash(memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length));
	}

	public async ValueTask<byte[]> ComputeHashAsync(Stream input, CancellationToken cancellationToken = default(CancellationToken))
	{
		using MemoryStream ms = new MemoryStream();
		await input.CopyToAsync(ms, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return ComputeHash(ms.GetBuffer().AsSpan(0, (int)ms.Length));
	}

	private static byte[] _ComputeHash(ReadOnlySpan<byte> data)
	{
		uint num = (uint)(1 ^ data.Length);
		int i = 0;
		for (int num2 = data.Length - 4; i <= num2; i += 4)
		{
			uint num3 = (uint)(data[i] | (data[i + 1] << 8) | (data[i + 2] << 16) | (data[i + 3] << 24));
			num3 *= 1540483477;
			num3 ^= num3 >> 24;
			num3 *= 1540483477;
			num *= 1540483477;
			num ^= num3;
		}
		switch (data.Length - i)
		{
		case 3:
			num ^= (uint)(data[i] | (data[i + 1] << 8));
			num ^= (uint)(data[i + 2] << 16);
			num *= 1540483477;
			break;
		case 2:
			num ^= (uint)(data[i] | (data[i + 1] << 8));
			num *= 1540483477;
			break;
		case 1:
			num ^= data[i];
			num *= 1540483477;
			break;
		}
		num ^= num >> 13;
		num *= 1540483477;
		num ^= num >> 15;
		return BitConverter.GetBytes(num);
	}
}
