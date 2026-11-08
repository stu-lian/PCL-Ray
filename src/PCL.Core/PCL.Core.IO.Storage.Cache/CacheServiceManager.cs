using System;
using System.Threading.Tasks;
using PCL.Core.App.IoC;

namespace PCL.Core.IO.Storage.Cache;

[LifecycleScope("global_cache", "磁盘缓存服务", true)]
[LifecycleService(LifecycleState.Loading, Priority = 500)]
public class CacheServiceManager : ILifecycleService
{
	private static CacheService? _service;

	public static CacheService Current => _service ?? throw new InvalidOperationException("Cache service is not initialized yet.");

	public string Identifier => "global_cache";

	public string Name => "磁盘缓存服务";

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
	private static async Task _StartAsync()
	{
		Context.Debug("正在初始化缓存服务...");
		_service = new CacheService();
		await _service.InitializeAsync().ConfigureAwait(continueOnCapturedContext: false);
		Context.Info("缓存服务初始化成功");
	}

	[LifecycleStop]
	private static async Task _StopAsync()
	{
		Context.Debug("正在停止缓存服务...");
		if (_service != null)
		{
			await _service.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			_service = null;
		}
		Context.Info("缓存服务已停止");
	}

	public CacheServiceManager()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		await _StopAsync();
	}

	public async Task StartAsync()
	{
		await _StartAsync();
	}
}
