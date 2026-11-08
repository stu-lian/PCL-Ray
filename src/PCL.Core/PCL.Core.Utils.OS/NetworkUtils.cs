using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PCL.Core.Utils.OS;

public static class NetworkUtils
{
	private static readonly IPAddress[] _LocalIpAddresses = (from ua in (from x in NetworkInterface.GetAllNetworkInterfaces()
			where x != null && x.OperationalStatus == OperationalStatus.Up
			select x).SelectMany((NetworkInterface x) => x.GetIPProperties().UnicastAddresses).Where((UnicastIPAddressInformation ua) =>
		{
			IPAddress address = ua.Address;
			bool flag;
			if (address != null)
			{
				AddressFamily addressFamily = address.AddressFamily;
				if (addressFamily == AddressFamily.InterNetwork || addressFamily == AddressFamily.InterNetworkV6)
				{
					flag = true;
					goto IL_0020;
				}
			}
			flag = false;
			goto IL_0020;
			IL_0020:
			if (flag && !ua.Address.Equals(IPAddress.Any) && !ua.Address.Equals(IPAddress.IPv6Any) && !ua.Address.Equals(IPAddress.Loopback))
			{
				return !ua.Address.Equals(IPAddress.IPv6Loopback);
			}
			return false;
		})
		select ua.Address).ToArray();

	public static IPAddress[] GetAllLocalAddress()
	{
		return _LocalIpAddresses;
	}
}
