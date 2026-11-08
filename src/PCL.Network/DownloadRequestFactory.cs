using System;
using System.Collections.Generic;
using System.Net.Http;
using Downloader;

namespace PCL.Network;

internal static class DownloadRequestFactory
{
	internal static RequestConfiguration Create(string url, bool useBrowserUserAgent, string customUserAgent = "")
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected Obj, but got Unknown
		HttpRequestMessage client = new HttpRequestMessage(HttpMethod.Get, url);
		RequestSigning.SecretHeadersSign(url, ref client, useBrowserUserAgent, customUserAgent);
		try
		{
			RequestConfiguration val = new RequestConfiguration();
			if (client.Headers.UserAgent.Count > 0)
			{
				val.UserAgent = client.Headers.UserAgent.ToString();
			}
			foreach (KeyValuePair<string, IEnumerable<string>> header in client.Headers)
			{
				if (!header.Key.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
				{
					val.Headers.Add(header.Key + ": " + string.Join(", ", header.Value));
				}
			}
			return val;
		}
		finally
		{
			client.Dispose();
		}
	}
}
