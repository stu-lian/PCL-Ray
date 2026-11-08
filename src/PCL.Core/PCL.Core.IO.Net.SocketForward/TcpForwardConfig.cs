namespace PCL.Core.IO.Net.SocketForward;

public sealed class TcpForwardConfig
{
	public const uint MaxBufferSize = 32768u;

	public const uint MinBufferSize = 1024u;

	public string? LocalHost { get; set; }

	public ushort LocalPort { get; set; }

	public string? RemoteHost { get; set; }

	public ushort RemotePort { get; set; }

	public ushort MaxConnection { get; set; } = 10;

	public uint BufferSize { get; set; } = 8192u;
}
