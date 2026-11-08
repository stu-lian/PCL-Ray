using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PCL.Core.Utils;

namespace PCL.Core.IO.Net.Http;

[Serializable]
public class HttpRouteResponse
{
	public HttpStatusCode? StatusCode;

	public Encoding? ContentEncoding;

	public string? ContentType;

	public string? RedirectLocation;

	public bool? SendChunked;

	public CookieCollection? Cookies;

	public Stream? InputStream;

	public static readonly HttpRouteResponse NoContent = Empty(HttpStatusCode.NoContent);

	public static readonly HttpRouteResponse BadRequest = Empty(HttpStatusCode.BadRequest);

	public static readonly HttpRouteResponse Forbidden = Empty(HttpStatusCode.Forbidden);

	public static readonly HttpRouteResponse NotFound = Empty(HttpStatusCode.NotFound);

	public static readonly HttpRouteResponse InternalServerError = Empty(HttpStatusCode.InternalServerError);

	public static readonly HttpRouteResponse BadGateway = Empty(HttpStatusCode.BadGateway);

	public void Pour(HttpListenerResponse target)
	{
		target.StatusCode = (int)(StatusCode ?? HttpStatusCode.OK);
		string contentType = ContentType;
		if (contentType != null)
		{
			target.ContentType = contentType;
		}
		Encoding contentEncoding = ContentEncoding;
		if (contentEncoding != null)
		{
			target.ContentEncoding = contentEncoding;
		}
		string redirectLocation = RedirectLocation;
		if (redirectLocation != null)
		{
			target.RedirectLocation = redirectLocation;
		}
		bool? sendChunked = SendChunked;
		if (sendChunked.HasValue)
		{
			bool valueOrDefault = sendChunked == true;
			target.SendChunked = valueOrDefault;
		}
		CookieCollection cookies = Cookies;
		if (cookies != null)
		{
			target.Cookies = cookies;
		}
		InputStream?.CopyTo(target.OutputStream);
	}

	public Task<HttpRouteResponse> AsTask()
	{
		return Task.FromResult(this);
	}

	public static HttpRouteResponse Empty(HttpStatusCode statusCode)
	{
		return new HttpRouteResponse
		{
			StatusCode = statusCode
		};
	}

	public static HttpRouteResponse Input(Stream stream, string contentType = "application/octet-stream", Encoding? encoding = null)
	{
		return new HttpRouteResponse
		{
			InputStream = stream,
			ContentType = contentType,
			ContentEncoding = (encoding ?? Encoding.UTF8)
		};
	}

	public static HttpRouteResponse Text(string text, string contentType = "text/plain", Encoding? encoding = null)
	{
		return Input(new StringStream(text, encoding), contentType, encoding);
	}

	public static HttpRouteResponse Redirect(string location, HttpStatusCode statusCode = HttpStatusCode.Found)
	{
		return new HttpRouteResponse
		{
			StatusCode = statusCode,
			RedirectLocation = location
		};
	}

	public static HttpRouteResponse Json(object obj, JsonSerializerOptions? options)
	{
		MemoryStream memoryStream = new MemoryStream();
		JsonSerializer.Serialize(memoryStream, obj, options ?? JsonCompat.SerializerOptions);
		memoryStream.Position = 0L;
		return new HttpRouteResponse
		{
			ContentEncoding = Encoding.UTF8,
			ContentType = "application/json, charset=utf-8",
			InputStream = memoryStream
		};
	}

	public static HttpRouteResponse Json(object obj)
	{
		return Json(obj, JsonCompat.SerializerOptions);
	}
}
