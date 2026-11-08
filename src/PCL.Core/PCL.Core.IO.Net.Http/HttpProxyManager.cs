using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.OS;

namespace PCL.Core.IO.Net.Http;

public class HttpProxyManager : IWebProxy, IDisposable
{
	public enum ProxyMode
	{
		NoProxy,
		SystemProxy,
		CustomProxy
	}

	private enum ProxyProtocol
	{
		Http,
		Socks
	}

	private sealed record ProxyItem(ProxyProtocol Protocol, string Address);

	public static readonly HttpProxyManager Instance = new HttpProxyManager();

	private readonly object _lock = new object();

	private ProxyMode _mode = ProxyMode.SystemProxy;

	private readonly WebProxy _customWebProxy = new WebProxy
	{
		BypassProxyOnLocal = true
	};

	private readonly WebProxy _systemWebProxy = new WebProxy
	{
		BypassProxyOnLocal = true
	};

	private const string ProxyRegPathFull = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";

	private const string ProxyRegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";

	private readonly RegistryChangeMonitor _proxyMonitor = new RegistryChangeMonitor("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");

	public ProxyMode Mode
	{
		get
		{
			lock (_lock)
			{
				return _mode;
			}
		}
		set
		{
			lock (_lock)
			{
				_mode = value;
			}
		}
	}

	public Uri? CustomProxyAddress
	{
		get
		{
			lock (_lock)
			{
				return _customWebProxy.Address;
			}
		}
		set
		{
			lock (_lock)
			{
				_customWebProxy.Address = value;
			}
		}
	}

	public ICredentials? CustomProxyCredentials
	{
		get
		{
			lock (_lock)
			{
				return _customWebProxy.Credentials;
			}
		}
		set
		{
			lock (_lock)
			{
				_customWebProxy.Credentials = value;
			}
		}
	}

	public bool BypassOnLocal
	{
		get
		{
			lock (_lock)
			{
				return field;
			}
		}
		set
		{
			lock (_lock)
			{
				field = value;
				_systemWebProxy.BypassProxyOnLocal = value;
			}
		}
	} = true;

	public ICredentials? Credentials
	{
		get
		{
			lock (_lock)
			{
				return (_mode == ProxyMode.CustomProxy) ? _customWebProxy.Credentials : null;
			}
		}
		set
		{
			lock (_lock)
			{
				_customWebProxy.Credentials = value;
			}
		}
	}

	private HttpProxyManager()
	{
		RefreshSystemProxy();
		_proxyMonitor.Changed += _OnSystemProxyChanged;
	}

	private void _OnSystemProxyChanged(object? sender, EventArgs e)
	{
		RefreshSystemProxy();
	}

	private static ProxyItem[] _GetProxyFromString(string? proxyString)
	{
		if (proxyString.IsNullOrWhiteSpace())
		{
			return Array.Empty<ProxyItem>();
		}
		if (proxyString.Contains('='))
		{
			List<ProxyItem> list = new List<ProxyItem>();
			string[] array = proxyString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (int i = 0; i < array.Length; i++)
			{
				ProxyItem proxyItem = _ParseKeyValueSegment(array[i]);
				if ((object)proxyItem != null)
				{
					list.Add(proxyItem);
				}
			}
			return list.ToArray();
		}
		ProxyItem proxyItem2 = _ParseAddress(proxyString.Trim());
		if ((object)proxyItem2 != null)
		{
			return new ProxyItem[1] { proxyItem2 };
		}
		return Array.Empty<ProxyItem>();
	}

	private static ProxyItem? _ParseKeyValueSegment(string segment)
	{
		int num = segment.IndexOf('=');
		if (num <= 0 || num >= segment.Length - 1)
		{
			return null;
		}
		return _ParseAddress(segment.Substring(num + 1).Trim(), _ParseProtocol(segment.Substring(0, num).Trim()));
	}

	private static ProxyItem? _ParseAddress(string address, ProxyProtocol? protocol = null)
	{
		if (address.IsNullOrWhiteSpace())
		{
			return null;
		}
		if (Uri.TryCreate(address, default(UriCreationOptions), out Uri result) && !result.Host.IsNullOrEmpty())
		{
			string address2 = ((result.Port > 0) ? $"{result.Host}:{result.Port}" : result.Host);
			return new ProxyItem(protocol ?? _ParseProtocol(result.Scheme), address2);
		}
		return new ProxyItem(protocol.GetValueOrDefault(), address);
	}

	private static ProxyProtocol _ParseProtocol(string scheme)
	{
		switch (scheme.ToLowerInvariant())
		{
		case "socks":
		case "socks4":
		case "socks5":
			return ProxyProtocol.Socks;
		default:
			return ProxyProtocol.Http;
		}
	}

	public void RefreshSystemProxy()
	{
		lock (_lock)
		{
			try
			{
				int num = (int)(Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyEnable", 0) ?? ((object)0));
				string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "ProxyServer", string.Empty) as string;
				ProxyItem proxyItem = _GetProxyFromString(text).FirstOrDefault((ProxyItem x) => x.Protocol == ProxyProtocol.Http);
				_systemWebProxy.Address = (((object)proxyItem == null || proxyItem.Address.IsNullOrEmpty() || num == 0) ? null : new Uri("http://" + proxyItem.Address));
				LogWrapper.Info("Proxy", $"已从操作系统更新代理设置，系统代理状态：{num}|{text}");
			}
			catch (Exception ex)
			{
				LogWrapper.Error(ex, "Proxy", "获取系统代理时出现异常");
			}
		}
	}

	public Uri? GetProxy(Uri destination)
	{
		lock (_lock)
		{
			return _mode switch
			{
				ProxyMode.NoProxy => null, 
				ProxyMode.SystemProxy => _systemWebProxy.GetProxy(destination), 
				ProxyMode.CustomProxy => _customWebProxy.GetProxy(destination), 
				_ => null, 
			};
		}
	}

	public bool IsBypassed(Uri host)
	{
		lock (_lock)
		{
			return _mode switch
			{
				ProxyMode.NoProxy => true, 
				ProxyMode.SystemProxy => _systemWebProxy.IsBypassed(host), 
				ProxyMode.CustomProxy => _customWebProxy.IsBypassed(host), 
				_ => true, 
			};
		}
	}

	public void Dispose()
	{
		_proxyMonitor.Dispose();
		GC.SuppressFinalize(this);
	}
}
