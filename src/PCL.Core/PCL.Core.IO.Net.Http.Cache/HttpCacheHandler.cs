using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Storage;
using PCL.Core.IO.Storage.Cache;

namespace PCL.Core.IO.Net.Http.Cache;

public class HttpCacheHandler : DelegatingHandler
{
	private ICacheService _cacheService;

	public HttpCacheHandler(HttpMessageHandler invoker, ICacheService cacheService)
	{
		InnerHandler = invoker;
		_cacheService = cacheService;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		string uri = request.RequestUri?.ToString();
		if (string.IsNullOrEmpty(uri))
		{
			return await base.SendAsync(request, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		string cacheKey = CacheKeys.ApiResponse("http", uri);
		CacheResult<byte[]> cacheResult = await _cacheService.GetAsync<byte[]>(cacheKey, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (cacheResult.Found)
		{
			HttpResponseMessage httpResponseMessage = _DeserializeResponse(cacheResult.Value);
			httpResponseMessage.Headers.Add("X-Cache-Hit", "HIT");
			httpResponseMessage.RequestMessage = request;
			return httpResponseMessage;
		}
		string metaKey = CacheKeys.ApiResponseMeta("http", uri);
		CacheResult<HttpCacheMetadata> cacheResult2 = await _cacheService.GetAsync<HttpCacheMetadata>(metaKey, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (cacheResult2.Found)
		{
			if (cacheResult2.Value.ETag != null)
			{
				request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cacheResult2.Value.ETag));
			}
			if (cacheResult2.Value.LastModified != null)
			{
				request.Headers.IfModifiedSince = DateTimeOffset.Parse(cacheResult2.Value.LastModified);
			}
		}
		HttpResponseMessage response = await base.SendAsync(request, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (response.Headers.CacheControl?.NoStore ?? false)
		{
			return response;
		}
		if (response.StatusCode == HttpStatusCode.NotModified)
		{
			CacheResult<byte[]> cacheResult3 = await _cacheService.GetAsync<byte[]>(cacheKey, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (cacheResult3.Found)
			{
				HttpResponseMessage httpResponseMessage2 = _DeserializeResponse(cacheResult3.Value);
				httpResponseMessage2.Headers.Add("X-Cache-Hit", "HIT");
				httpResponseMessage2.RequestMessage = request;
				return httpResponseMessage2;
			}
		}
		byte[] value = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		TimeSpan? absoluteExpiration = _ComputeTtl(response);
		await _cacheService.SetAsync(cacheKey, value, new CachePolicy
		{
			AbsoluteExpiration = absoluteExpiration,
			Group = "http",
			Tags = "http-response"
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		await _cacheService.SetAsync(metaKey, new HttpCacheMetadata
		{
			ETag = response.Headers.ETag?.Tag,
			LastModified = response.Content.Headers.LastModified?.ToString("O"),
			StatusCode = (int)response.StatusCode
		}, CachePolicy.NeverExpire, ct).ConfigureAwait(continueOnCapturedContext: false);
		response.Headers.Add("X-Cache-Hit", "MISS");
		return response;
	}

	private static TimeSpan? _ComputeTtl(HttpResponseMessage response)
	{
		CacheControlHeaderValue cacheControl = response.Headers.CacheControl;
		if ((cacheControl?.MaxAge).HasValue)
		{
			return cacheControl.MaxAge;
		}
		if ((cacheControl?.SharedMaxAge).HasValue)
		{
			return cacheControl.SharedMaxAge;
		}
		return TimeSpan.FromMinutes(5L);
	}

	private static HttpResponseMessage _DeserializeResponse(byte[] data)
	{
		return new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new ByteArrayContent(data)
		};
	}
}
