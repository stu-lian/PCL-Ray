using System;
using System.Net;
using PCL.Core.Utils.Exts;

namespace PCL.Core.IO.Net.SocketForward;

public class TcpForwardBuilder
{
	private readonly TcpForwardConfig _cfg = new TcpForwardConfig();

	public TcpForwardWorker Build()
	{
		ArgumentNullException.ThrowIfNull(_cfg.RemoteHost, "_cfg.RemoteHost");
		ArgumentOutOfRangeException.ThrowIfZero(_cfg.RemotePort, "_cfg.RemotePort");
		if (_cfg.LocalHost.IsNullOrWhiteSpace())
		{
			_cfg.LocalHost = IPAddress.Loopback.ToString();
		}
		return new TcpForwardWorker(_cfg);
	}

	public TcpForwardBuilder BindLocalRandom()
	{
		_cfg.LocalHost = IPAddress.Loopback.ToString();
		_cfg.LocalPort = 0;
		return this;
	}

	public TcpForwardBuilder BindLocal(ushort port)
	{
		ArgumentOutOfRangeException.ThrowIfZero(port, "port");
		_cfg.LocalHost = IPAddress.Loopback.ToString();
		_cfg.LocalPort = port;
		return this;
	}

	public TcpForwardBuilder SetRemote(string host, ushort port)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(host, "host");
		ArgumentOutOfRangeException.ThrowIfZero(port, "port");
		_cfg.RemoteHost = host;
		_cfg.RemotePort = port;
		return this;
	}

	public TcpForwardBuilder SetRemote(IPEndPoint remote)
	{
		ArgumentNullException.ThrowIfNull(remote, "remote");
		_cfg.RemoteHost = remote.Address.ToString();
		_cfg.RemotePort = (ushort)remote.Port;
		return this;
	}

	public TcpForwardBuilder SetRemote(IPAddress host, ushort port)
	{
		ArgumentNullException.ThrowIfNull(host, "host");
		ArgumentOutOfRangeException.ThrowIfZero(port, "port");
		_cfg.RemoteHost = host.ToString();
		_cfg.RemotePort = port;
		return this;
	}

	public TcpForwardBuilder SetBufferSize(uint size)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 32768u, "size");
		ArgumentOutOfRangeException.ThrowIfLessThan(size, 1024u, "size");
		_cfg.BufferSize = size;
		return this;
	}

	public TcpForwardBuilder SetMaxAllowedActiveConnection(ushort maxConnectionCount)
	{
		ArgumentOutOfRangeException.ThrowIfZero(maxConnectionCount, "maxConnectionCount");
		_cfg.MaxConnection = maxConnectionCount;
		return this;
	}
}
