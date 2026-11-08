using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Exts;

public static class HttpRequestExtension
{
	public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		HttpRequestMessage clone = new HttpRequestMessage(request.Method, request.RequestUri)
		{
			Version = request.Version,
			VersionPolicy = request.VersionPolicy
		};
		if (request.Content != null)
		{
			HttpRequestMessage httpRequestMessage = clone;
			httpRequestMessage.Content = await request.Content._DeepCloneAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}
		foreach (KeyValuePair<string, object> item in (IEnumerable<KeyValuePair<string, object>>)request.Options)
		{
			clone.Options.TryAdd(item.Key, item.Value);
		}
		return clone;
	}

	private static async Task<HttpContent?> _DeepCloneAsync(this HttpContent content)
	{
		MemoryStream ms = new MemoryStream();
		await content.CopyToAsync(ms).ConfigureAwait(continueOnCapturedContext: false);
		ms.Position = 0L;
		StreamContent streamContent = new StreamContent(ms);
		foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
		{
			streamContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}
		return streamContent;
	}
}
