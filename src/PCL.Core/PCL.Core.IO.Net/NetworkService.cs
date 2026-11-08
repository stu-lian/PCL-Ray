using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PCL.Core.App;
using PCL.Core.App.IoC;
using PCL.Core.IO.Net.Http;
using PCL.Core.IO.Net.Http.Cache;
using PCL.Core.IO.Storage.Cache;
using PCL.Core.Logging;
using Polly;

namespace PCL.Core.IO.Net;

[LifecycleService(LifecycleState.Loading)]
[LifecycleScope("network", "网络服务", true)]
public class NetworkService : ILifecycleService
{
	private const int LifeTime = 15;

	private const string MicrosoftEntraIdServer = "https://login.microsoftonline.com/";

	private const string MojangPistonMetaServer = "https://piston-meta.mojang.com/";

	private const string MojangSessionServer = "https://sessionserver.mojang.com/";

	private const string CurseForgeApiServer = "https://api.curseforge.com/v1/";

	private const string ModrinthApiServer = "https://api.modrinth.com/v2/";

	private const string MinecraftServiceServer = "https://api.minecraftservices.com/";

	public const string Default = "default";

	public const string MicrosoftEntraId = "microsoft_id";

	public const string MinecraftService = "minecraft_service";

	public const string Cache = "cache";

	public const string MojangPistonMeta = "mojang_piston";

	public const string MojangSession = "mojang_session";

	public const string CurseForgeApi = "curseforge_api";

	public const string ModrinthApi = "modrinth_api";

	private static ServiceProvider? _provider;

	private static IHttpClientFactory? _factory;

	private const int BaseRetryDelayMs = 1000;

	private const int MaxRetryDelayMs = 30000;

	public string Identifier => "network";

	public string Name => "网络服务";

	public bool SupportAsync => true;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	}

	private static ILifecycleService Service => Context.ServiceInstance;

	[LifecycleStart]
	private static void _Start()
	{
		ServiceCollection services = new ServiceCollection();
		services.ConfigureHttpClientDefaults((IHttpClientBuilder b) =>
		{
			b.ConfigurePrimaryHttpMessageHandler(_GetSocketsHttpHandler).ConfigureHttpClient((HttpClient c) =>
			{
				c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PCL-CE", Basics.VersionName));
			}).SetHandlerLifetime(TimeSpan.FromMinutes(15L));
		});
		services.AddHttpClient("default");
		services.AddHttpClient("curseforge_api").ConfigureHttpClient((HttpClient c) =>
		{
			c.DefaultRequestHeaders.Add("x-api-key", Secrets.CurseForgeAPIKey);
			c.BaseAddress = new Uri("https://api.curseforge.com/v1/");
		});
		services.AddHttpClient("modrinth_api").ConfigureHttpClient((HttpClient c) =>
		{
			c.BaseAddress = new Uri("https://api.modrinth.com/v2/");
		});
		services.AddHttpClient("microsoft_id").ConfigureHttpClient((HttpClient c) =>
		{
			c.BaseAddress = new Uri("https://login.microsoftonline.com/");
		});
		services.AddHttpClient("minecraft_service").ConfigureHttpClient((HttpClient c) =>
		{
			c.BaseAddress = new Uri("https://api.minecraftservices.com/");
		});
		services.AddHttpClient("mojang_piston").ConfigureHttpClient((HttpClient c) =>
		{
			c.BaseAddress = new Uri("https://piston-meta.mojang.com/");
		});
		services.AddHttpClient("mojang_session").ConfigureHttpClient((HttpClient c) =>
		{
			c.BaseAddress = new Uri("https://sessionserver.mojang.com/");
		});
		services.AddHttpClient("cache").ConfigurePrimaryHttpMessageHandler(() => new HttpCacheHandler(_GetSocketsHttpHandler(), CacheServiceManager.Current)).SetHandlerLifetime(TimeSpan.FromMinutes(15L));
		_provider?.Dispose();
		_provider = services.BuildServiceProvider();
		_factory = _provider.GetRequiredService<IHttpClientFactory>();
	}

	[LifecycleStop]
	private static void _Stop()
	{
		_provider?.Dispose();
	}

	private static SocketsHttpHandler _GetSocketsHttpHandler()
	{
		return new SocketsHttpHandler
		{
			UseProxy = true,
			AutomaticDecompression = DecompressionMethods.All,
			Proxy = HttpProxyManager.Instance,
			AllowAutoRedirect = true,
			MaxAutomaticRedirections = 20,
			UseCookies = false,
			EnableMultipleHttp2Connections = true,
			ConnectCallback = (Config.Network.EnableDoH ? new Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>(HostConnectionHandler.Instance.GetConnectionAsync) : null)
		};
	}

	public static HttpClient GetClient(string wantClientType = "default")
	{
		return _factory?.CreateClient(wantClientType) ?? throw new InvalidOperationException("在初始化完成前的意外调用");
	}

	private static TimeSpan _DefaultSleepDurationProvider(int attempt)
	{
		return TimeSpan.FromMilliseconds(Math.Min(Math.Pow(2.0, attempt - 1) * 1000.0, 30000.0));
	}

	public static AsyncPolicy GetRetryPolicy(int retry = 3, Func<int, TimeSpan>? retryPolicy = null)
	{
		if (retryPolicy == null)
		{
			retryPolicy = _DefaultSleepDurationProvider;
		}
		return (AsyncPolicy)(object)AsyncRetrySyntax.WaitAndRetryAsync(Policy.Handle<HttpRequestException>(), retry, (Func<int, TimeSpan>)((int attempt) => retryPolicy(attempt)), (Action<Exception, TimeSpan, int, Context>)((Exception exception, TimeSpan timeSpan, int retryAttempt, Context _) =>
		{
			LogWrapper.Debug(exception, "Network", $"HTTP 请求失败，正在进行第 {retryAttempt} 次重试，等待 {timeSpan.TotalMilliseconds} 毫秒。");
		}));
	}

	public NetworkService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		_Stop();
	}

	public async Task StartAsync()
	{
		_Start();
	}
}
