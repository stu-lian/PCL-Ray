using System.Net.Sockets;

namespace PCL.Core.IO.Net;

public static class SocketExtensions
{
	public static void CloseGracefully(this Socket? socket)
	{
		if (socket == null)
		{
			return;
		}
		if (socket != null && !socket.IsBound && !socket.Connected)
		{
			socket.Dispose();
			return;
		}
		if (socket.Connected)
		{
			try
			{
				socket.Shutdown(SocketShutdown.Both);
			}
			catch
			{
			}
		}
		try
		{
			socket.Close();
		}
		catch
		{
		}
	}
}
