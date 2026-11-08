using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Http;

namespace PCL.Network;

public static class Requester
{
	public static void EnsureSuccess(HttpResponseMessage? response)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (response == null || !response.IsSuccessStatusCode)
		{
			throw new HttpResponseException(response);
		}
	}

	public static async Task<string> FetchStringAsync(string url, RequestParam param = default(RequestParam))
	{
		return await FetchAsync(url, new FetchParam
		{
			Method = "GET",
			Accept = param.Accept,
			FallbackUrl = param.FallbackUrl,
			UseBrowserUserAgent = param.UseBrowserUserAgent,
			Timeout = param.Timeout,
			Encoding = param.Encoding,
			MakeLog = true
		}, Math.Max(1, (param.Retries == 0) ? 1 : param.Retries)).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static string FetchString(string url, RequestParam param = default(RequestParam))
	{
		return FetchStringAsync(url, param).GetAwaiter().GetResult();
	}

	public static async Task<JsonNode> FetchJsonAsync(string url, RequestParam param = default(RequestParam))
	{
		return ModBase.GetJson(await FetchStringAsync(url, param).ConfigureAwait(continueOnCapturedContext: false));
	}

	public static async Task<T> FetchJsonAsync<T>(string url, RequestParam param = default(RequestParam)) where T : JsonNode
	{
		return (T)(await FetchJsonAsync(url, param).ConfigureAwait(continueOnCapturedContext: false));
	}

	public static JsonNode FetchJson(string url, RequestParam param = default(RequestParam))
	{
		return FetchJsonAsync(url, param).GetAwaiter().GetResult();
	}

	public static T FetchJson<T>(string url, RequestParam param = default(RequestParam)) where T : JsonNode
	{
		return FetchJsonAsync<T>(url, param).GetAwaiter().GetResult();
	}

	public static async Task<string> FetchAsync(string url, FetchParam param)
	{
		return await FetchAsync(url, param, 3).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static string Fetch(string url, FetchParam param)
	{
		return FetchAsync(url, param).GetAwaiter().GetResult();
	}

	public static string Fetch(string url)
	{
		return Fetch(url, new FetchParam
		{
			Method = "GET",
			Timeout = 30000,
			MakeLog = true
		});
	}

	private static async Task<string> FetchAsync(string url, FetchParam param, int retries)
	{
		List<string> list = new string[2] { url, param.FallbackUrl }.Where((string u) => !string.IsNullOrWhiteSpace(u)).Cast<string>().ToList();
		Exception ex = null;
		foreach (string currentUrl in list)
		{
			for (int attempt = 0; attempt < Math.Max(1, retries); attempt++)
			{
				try
				{
					return await FetchOnceAsync(currentUrl, param).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception ex2)
				{
					ex = ex2;
				}
			}
		}
		throw ex ?? new HttpRequestException("请求失败");
	}

	private static async Task<string> FetchOnceAsync(string url, FetchParam param)
	{
		HttpResponseMessage response = null;
		HttpRequestMessage request = new HttpRequestMessage(ParseMethod(param.Method), RequestSigning.SecretCdnSign(url));
		RequestSigning.SecretHeadersSign(url, ref request, param.UseBrowserUserAgent);
		try
		{
			if (!string.IsNullOrWhiteSpace(param.Accept))
			{
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(param.Accept));
			}
			if (param.Headers != null)
			{
				foreach (KeyValuePair<string, string> header in param.Headers)
				{
					request.Headers.TryAddWithoutValidation(header.Key, header.Value);
				}
			}
			if (SupportBody(request.Method) && param.Content != null)
			{
				if (param.Content is HttpContent content)
				{
					request.Content = content;
				}
				else
				{
					string content2 = ((param.Content is string text) ? text : (param.Content.ToString() ?? ""));
					request.Content = new StringContent(content2, param.Encoding ?? Encoding.UTF8, param.ContentType ?? "application/json");
				}
			}
			using CancellationTokenSource cts = new CancellationTokenSource();
			cts.CancelAfter((param.Timeout <= 0) ? 30000 : param.Timeout);
			response = await NetworkService.GetClient("default").SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			EnsureSuccess(response);
			return await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (!param.RequireContent)
			{
				response?.Dispose();
			}
			request.Dispose();
		}
	}

	public static HttpMethod ParseMethod(string? method)
	{
		return (method ?? "GET").ToUpperInvariant() switch
		{
			"POST" => HttpMethod.Post, 
			"PUT" => HttpMethod.Put, 
			"DELETE" => HttpMethod.Delete, 
			"PATCH" => HttpMethod.Patch, 
			"HEAD" => HttpMethod.Head, 
			_ => HttpMethod.Get, 
		};
	}

	public static bool SupportBody(HttpMethod method)
	{
		if (!(method == HttpMethod.Post) && !(method == HttpMethod.Put) && !(method == HttpMethod.Patch))
		{
			return method == HttpMethod.Delete;
		}
		return true;
	}

	public static int Ping(string ip)
	{
		try
		{
			PingReply pingReply = new Ping().Send(ip);
			return (int)((pingReply.Status == IPStatus.Success) ? pingReply.RoundtripTime : (-1));
		}
		catch
		{
			return -1;
		}
	}
}
