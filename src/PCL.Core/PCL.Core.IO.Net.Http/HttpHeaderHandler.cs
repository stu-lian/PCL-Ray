using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PCL.Core.IO.Net.Http;

public static class HttpHeaderHandler
{
	extension(HttpRequestMessage requestMessage)
	{
		public HttpRequestMessage WithHeader(string key, string value)
		{
			if (key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) && requestMessage.Content != null)
			{
				requestMessage.Content.Headers.TryAddWithoutValidation(key, value);
			}
			else
			{
				requestMessage.Headers.TryAddWithoutValidation(key, value);
			}
			return requestMessage;
		}

		public HttpRequestMessage WithHeaders(IDictionary<string, string> pairs)
		{
			ArgumentNullException.ThrowIfNull(pairs, "pairs");
			foreach (KeyValuePair<string, string> pair in pairs)
			{
				requestMessage.WithHeader(pair.Key, pair.Value);
			}
			return requestMessage;
		}

		public HttpRequestMessage WithHeader(KeyValuePair<string, string> pair)
		{
			return requestMessage.WithHeader(pair.Key, pair.Value);
		}

		public HttpRequestMessage WithAuthentication(string scheme, string token)
		{
			ArgumentException.ThrowIfNullOrEmpty(scheme, "scheme");
			ArgumentException.ThrowIfNullOrEmpty(token, "token");
			requestMessage.Headers.Authorization = new AuthenticationHeaderValue(scheme, token);
			return requestMessage;
		}

		public HttpRequestMessage WithBearerToken(string token)
		{
			return requestMessage.WithAuthentication("Bearer", token);
		}
	}
}
