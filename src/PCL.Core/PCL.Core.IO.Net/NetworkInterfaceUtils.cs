using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PCL.Core.IO.Net;

public static class NetworkInterfaceUtils
{
	public enum IPv6Status
	{
		Unknown,
		Public,
		RFC4193,
		Unavailable
	}

	public static List<NetworkInterface> GetAvailableInterface()
	{
		return (from iface in NetworkInterface.GetAllNetworkInterfaces()
			where !_IsVirtualInterface(iface)
			select iface).ToList();
	}

	public static IPv6Status GetIPv6Status()
	{
		foreach (NetworkInterface item in GetAvailableInterface())
		{
			IPAddress[] array = (from addr in item.GetIPProperties().UnicastAddresses
				where addr.Address.AddressFamily == AddressFamily.InterNetworkV6
				select addr.Address).ToArray();
			if (array.Length == 0)
			{
				return IPv6Status.Unavailable;
			}
			IPAddress[] array2 = array;
			foreach (IPAddress ip in array2)
			{
				if (_IsPublicIPv6(ip))
				{
					return IPv6Status.Public;
				}
				if (_IsUniqueLocalIPv6(ip))
				{
					return IPv6Status.RFC4193;
				}
			}
		}
		return IPv6Status.Unknown;
	}

	private static bool _IsVirtualInterface(NetworkInterface iface)
	{
		NetworkInterfaceType[] source = new NetworkInterfaceType[3]
		{
			NetworkInterfaceType.Loopback,
			NetworkInterfaceType.Tunnel,
			NetworkInterfaceType.Ppp
		};
		string[] source2 = new string[11]
		{
			"virtual", "pseudo", "loopback", "tunnel", "vpn", "ppp", "veth", "docker", "hyper-v", "vmware",
			"virtualbox"
		};
		if (!Enumerable.Contains(source, iface.NetworkInterfaceType))
		{
			return source2.Any((string keyword) => iface.Description.ToLower().Contains(keyword));
		}
		return true;
	}

	private static bool _IsPublicIPv6(IPAddress ip)
	{
		byte[] addressBytes = ip.GetAddressBytes();
		if (addressBytes[0] >= 32)
		{
			return addressBytes[0] <= 63;
		}
		return false;
	}

	private static bool _IsUniqueLocalIPv6(IPAddress ip)
	{
		byte[] addressBytes = ip.GetAddressBytes();
		if (addressBytes[0] != 252)
		{
			return addressBytes[0] == 253;
		}
		return true;
	}
}
