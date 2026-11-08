using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PCL.Core.IO.Net;

public static class NetworkHelper
{
	public static int NewTcpPort()
	{
		using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		return (socket.LocalEndPoint != null) ? ((IPEndPoint)socket.LocalEndPoint).Port : 0;
	}

	public static bool IsNetworkAvailable()
	{
		return NetworkInterface.GetIsNetworkAvailable();
	}
}
