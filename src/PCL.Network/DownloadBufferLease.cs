using System;
using System.Buffers;

namespace PCL.Network;

internal sealed class DownloadBufferLease(DownloadQuotaLease lase, byte[] buffer) : IDisposable
{
	public byte[] Buffer { get; } = buffer;

	public int Length => Buffer.Length;

	public void Dispose()
	{
		ArrayPool<byte>.Shared.Return(Buffer);
		lase.Dispose();
	}
}
