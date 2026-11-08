using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.IO.Net.Http;

public abstract class HttpServer : IDisposable
{
	private readonly HttpListener _server = new HttpListener();

	public readonly ushort Port;

	public readonly string[] Host;

	private Task? _handleLoop;

	private CancellationTokenSource? _cancellationTokenSource;

	private readonly Dictionary<(HttpMethod method, string path), Func<HttpListenerRequest, Task<HttpRouteResponse>>> _handlers = new Dictionary<(HttpMethod, string), Func<HttpListenerRequest, Task<HttpRouteResponse>>>();

	private bool _initialized;

	protected HttpServer(IPAddress[] listenAddr, ushort port = 0)
	{
		ArgumentNullException.ThrowIfNull(listenAddr, "listenAddr");
		if (port == 0)
		{
			port = (ushort)NetworkHelper.NewTcpPort();
		}
		Port = port;
		if (listenAddr.Length == 0)
		{
			listenAddr = new IPAddress[2]
			{
				IPAddress.Loopback,
				IPAddress.IPv6Loopback
			};
		}
		List<string> list = new List<string>();
		IPAddress[] array = listenAddr;
		foreach (IPAddress iPAddress in array)
		{
			_server.Prefixes.Add($"http://{iPAddress}:{port}/");
			list.Add(iPAddress.ToString());
		}
		Host = list.ToArray();
	}

	protected abstract void Init();

	protected void Register(HttpMethod method, string path, Func<HttpListenerRequest, Task<HttpRouteResponse>> handler)
	{
		ArgumentNullException.ThrowIfNull(method, "method");
		ArgumentNullException.ThrowIfNull(path, "path");
		ArgumentNullException.ThrowIfNull(handler, "handler");
		_handlers[(method, path)] = handler;
	}

	public void Start()
	{
		if (!_initialized && _handlers.Count == 0)
		{
			Init();
			_initialized = true;
		}
		_cancellationTokenSource = new CancellationTokenSource();
		_server.Start();
		_handleLoop = _HandleRequestAsync();
	}

	private async Task _HandleRequestAsync()
	{
		CancellationToken cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				HttpListenerContext context = await _server.GetContextAsync();
				Task.Run(async () =>
				{
					await _ProcessRequestAsync(context);
				}, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (HttpListenerException)
			{
				break;
			}
		}
	}

	private async Task _ProcessRequestAsync(HttpListenerContext context)
	{
		_ = 1;
		try
		{
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			string item = request.Url?.AbsolutePath ?? string.Empty;
			HttpMethod item2 = new HttpMethod(request.HttpMethod);
			Func<HttpListenerRequest, Task<HttpRouteResponse>> value2;
			if (_handlers.TryGetValue((item2, item), out Func<HttpListenerRequest, Task<HttpRouteResponse>> value))
			{
				await _ExecuteHandlerAsync(value, request, response);
			}
			else if (_handlers.TryGetValue((item2, "*"), out value2))
			{
				await _ExecuteHandlerAsync(value2, request, response);
			}
			else
			{
				response.StatusCode = 404;
			}
		}
		finally
		{
			try
			{
				context.Response.Close();
			}
			catch
			{
			}
		}
	}

	private static async Task _ExecuteHandlerAsync(Func<HttpListenerRequest, Task<HttpRouteResponse>> handler, HttpListenerRequest request, HttpListenerResponse response)
	{
		try
		{
			(await handler(request)).Pour(response);
		}
		catch (Exception value)
		{
			response.StatusCode = 500;
			response.ContentEncoding = Encoding.UTF8;
			response.ContentType = "text/plain";
			HttpRouteResponse.Text($"Internal Server Error:\n{value}", "text/plain", Encoding.UTF8).Pour(response);
		}
	}

	public void Stop()
	{
		_cancellationTokenSource?.Cancel();
		_server.Stop();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Stop();
		_server.Close();
		_cancellationTokenSource?.Dispose();
	}
}
