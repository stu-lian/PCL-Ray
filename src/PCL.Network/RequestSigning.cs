using System;
using System.Net;
using System.Net.Http;
using PCL.Core.App;

namespace PCL.Network;

public static class RequestSigning
{
	internal static string SecretCdnSign(string urlWithMark)
	{
		if (!urlWithMark.EndsWithF("{CDN}"))
		{
			return urlWithMark;
		}
		return urlWithMark.Replace("{CDN}", "").Replace(" ", "%20");
	}

	internal static void SecretHeadersSign(string url, ref HttpRequestMessage client, bool useBrowserUserAgent = false, string customUserAgent = "")
	{
		client.Version = HttpVersion.Version20;
		client.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
		if (Uri.TryCreate(url, UriKind.Absolute, out Uri result) && (result.Host == "api.curseforge.com" || result.Host == "edge.forgecdn.net" || result.Host == "mediafilez.forgecdn.net"))
		{
			client.Headers.Add("x-api-key", Secrets.CurseForgeAPIKey);
		}
		string value;
		if (string.IsNullOrEmpty(customUserAgent))
		{
			value = (useBrowserUserAgent ? $"PCL2/{ModBase.upstreamVersion}.{"0"} PCLCE/{ModBase.versionStandardCode} Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0" : $"PCL2/{ModBase.upstreamVersion}.{"0"} PCLCE/{ModBase.versionStandardCode}");
		}
		else
		{
			value = customUserAgent;
		}
		client.Headers.Add("User-Agent", value);
	}
}
