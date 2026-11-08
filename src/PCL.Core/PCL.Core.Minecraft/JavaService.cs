using System;
using System.Threading.Tasks;
using PCL.Core.App.IoC;
using PCL.Core.Minecraft.Java.Parser;
using PCL.Core.Minecraft.Java.Scanner;

namespace PCL.Core.Minecraft;

[LifecycleService(LifecycleState.Loaded)]
[LifecycleScope("java", "Java 管理", true)]
public sealed class JavaService : ILifecycleService
{
	private static JavaManager? _javaManager;

	public static JavaManager JavaManager => _javaManager;

	public string Identifier => "java";

	public string Name => "Java 管理";

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
		if (_javaManager == null)
		{
			Context.Info("Initializing Java Manager...");
			_javaManager = new JavaManager(new PeHeaderParser(), new RegistryJavaScanner(), new DefaultPathsScanner(), new PathEnvironmentScanner(), new MicrosoftStoreJavaScanner(), new WhereCommandScanner());
			_javaManager.ReadConfig();
			Context.Info("Lookup for local Java...");
			await _javaManager.ScanJavaAsync();
			string text = string.Join("\n\t", _javaManager.GetSortedJavaList());
			Context.Info("Finished to scan java: \n\t" + text);
		}
	}

	[LifecycleStop]
	private static void _Stop()
	{
		if (_javaManager != null)
		{
			_javaManager.SaveConfig();
			_javaManager = null;
		}
	}

	public JavaService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		_Stop();
	}

	public async Task StartAsync()
	{
		await _StartAsync();
	}
}
