using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Network;

public static class ModNet
{
	public const string NetDownloadEnd = ".PCLDownloading";

	public const int NetTaskConnectionLimitMax = 256;

	public const int NetTaskSingleFileConnectionLimitMax = 8;

	public const int NetTaskConnectionsPerHostLimit = 256;

	public const long NetTaskBufferBudgetBytes = 536870912L;

	public static int NetTaskConnectionLimit { get; set; } = 64;

	public static int NetTaskSingleFileConnectionLimit { get; set; } = 8;

	[Obsolete("请使用 NetTaskConnectionLimit。")]
	public static int NetTaskThreadLimit
	{
		get
		{
			return NetTaskConnectionLimit;
		}
		set
		{
			NetTaskConnectionLimit = value;
		}
	}

	public static long NetTaskSpeedLimitLow { get; set; } = 262144L;

	public static long NetTaskSpeedLimitHigh { get; set; } = -1L;

	public static long NetTaskSpeedLimitLeft { get; set; } = -1L;

	public static int NetTaskThreadCount { get; set; }

	public static NetManager NetManager => PCL.Network.NetManager.Instance;

	public static object NetGetCodeByRequestRetry(string url, Encoding? encode = null, string accept = "", bool isJson = false, string? backupUrl = null, bool useBrowserUserAgent = false)
	{
		RequestParam param = new RequestParam
		{
			Encoding = encode,
			Accept = accept,
			FallbackUrl = backupUrl,
			UseBrowserUserAgent = useBrowserUserAgent,
			Timeout = 30000,
			Retries = 3
		};
		string text = Requester.FetchString(url, param);
		if (!isJson)
		{
			return text;
		}
		return ModBase.GetJson(text);
	}

	public static object NetGetCodeByRequestOnce(string url, Encoding? encode = null, int timeout = 30000, bool isJson = false, string accept = "", bool useBrowserUserAgent = false)
	{
		RequestParam param = new RequestParam
		{
			Encoding = encode,
			Accept = accept,
			UseBrowserUserAgent = useBrowserUserAgent,
			Timeout = timeout,
			Retries = 1
		};
		string text = Requester.FetchString(url, param);
		if (!isJson)
		{
			return text;
		}
		return ModBase.GetJson(text);
	}

	public static string NetGetCodeByLoader(string url, int timeout = 45000, bool isJson = false, bool useBrowserUserAgent = false)
	{
		return NetGetCodeByLoader(new string[1] { url }, timeout, isJson, useBrowserUserAgent);
	}

	public static string NetGetCodeByLoader(IEnumerable<string> urls, int timeout = 45000, bool isJson = false, bool useBrowserUserAgent = false)
	{
		Exception innerException = null;
		foreach (string url in urls)
		{
			try
			{
				string text = Requester.Fetch(url, new FetchParam
				{
					Method = "GET",
					Timeout = timeout,
					UseBrowserUserAgent = useBrowserUserAgent
				});
				return isJson ? ModBase.GetJson(text).ToString() : text;
			}
			catch (Exception ex)
			{
				innerException = ex;
				ModBase.Log(ex, "[Fetch] 获取文件内容失败，尝试下一个源：" + url);
			}
		}
		throw new Exception("无法获取文件内容", innerException);
	}

	public static string NetRequestRetry(string url, string method, string data = "", string? contentType = null, Encoding? encoding = null, string? accept = null, bool useBrowserUserAgent = false)
	{
		return Requester.Fetch(url, new FetchParam
		{
			Method = method,
			Content = data,
			ContentType = contentType,
			Encoding = encoding,
			Accept = accept,
			UseBrowserUserAgent = useBrowserUserAgent,
			Timeout = 30000
		});
	}

	public static string NetRequestOnce(string url, string method, string data = "", string? contentType = null, Encoding? encoding = null, string? accept = null, bool useBrowserUserAgent = false)
	{
		return NetRequestRetry(url, method, data, contentType, encoding, accept, useBrowserUserAgent);
	}

	public static Task NetDownloadByClient(string url, string localFile, bool useBrowserUserAgent = false)
	{
		return FileDownloader.DownloadAsync(url, localFile, useBrowserUserAgent);
	}

	public static void NetDownloadByLoader(string url, string localFile, ModLoader.LoaderBase? loaderToSyncProgress = null, ModBase.FileChecker? check = null, bool useBrowserUserAgent = false)
	{
		FileDownloader.DownloadAsync(url, localFile, useBrowserUserAgent).GetAwaiter().GetResult();
	}

	public static void NetDownloadByLoader(IEnumerable<string> urls, string localFile, ModLoader.LoaderBase? loaderToSyncProgress = null, ModBase.FileChecker? check = null, bool useBrowserUserAgent = false)
	{
		FileDownloader.DownloadAsync(urls, localFile, useBrowserUserAgent).GetAwaiter().GetResult();
	}

	public static bool HasDownloadingTask(bool ignoreCustomDownload = false)
	{
		foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
		{
			if (item.show && item.State == ModBase.LoadState.Loading && (!ignoreCustomDownload || !item.name.Contains("自定义下载")))
			{
				return true;
			}
		}
		return false;
	}
}
