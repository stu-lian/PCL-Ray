using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MinValue)]
[LifecycleScope("application", "应用程序", false)]
public sealed class ApplicationService : ILifecycleService
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static DispatcherUnhandledExceptionEventHandler _003C_003E9__4_0;

		public static StartupEventHandler _003C_003E9__4_1;

		internal void _003C_Start_003Eb__4_0(object _, DispatcherUnhandledExceptionEventArgs e)
		{
			Lifecycle.OnException(e.Exception);
		}

		internal void _003C_Start_003Eb__4_1(object _, StartupEventArgs eventArgs)
		{
			Lifecycle.OnLoading();
		}
	}

	public static Func<Application>? Loading { private get; set; }

	public string Identifier => "application";

	public string Name => "应用程序";

	public bool SupportAsync => false;

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
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected Obj, but got Unknown
		Context.Debug("正在初始化 WPF 应用程序容器");
		Application application = Loading();
		DispatcherUnhandledExceptionEventHandler val = _003C_003Ec._003C_003E9__4_0;
		if (val == null)
		{
			DispatcherUnhandledExceptionEventHandler val2 = (object _, DispatcherUnhandledExceptionEventArgs e) =>
			{
				Lifecycle.OnException(e.Exception);
			};
			_003C_003Ec._003C_003E9__4_0 = val2;
			val = val2;
		}
		application.DispatcherUnhandledException += val;
		application.Startup += (object _, StartupEventArgs _) =>
		{
			Lifecycle.OnLoading();
		};
		Lifecycle.CurrentApplication = application;
		Loading = null;
		Context.Trace("应用程序容器初始化完毕");
	}

	[LifecycleStop]
	private static void _Stop()
	{
		Application app = Lifecycle.CurrentApplication;
		Dispatcher dispatcher = ((DispatcherObject)app).Dispatcher;
		ManualResetEventSlim exited;
		if (Lifecycle.IsForceShutdown)
		{
			Context.Warn("已指定强制关闭，跳过 WPF 标准关闭流程");
		}
		else
		{
			if (dispatcher == null || dispatcher.HasShutdownFinished)
			{
				return;
			}
			exited = new ManualResetEventSlim();
			try
			{
				dispatcher.BeginInvoke((DispatcherPriority)10, (Delegate)(Action)(() =>
				{
					app.Exit += Exited;
					if (!dispatcher.HasShutdownStarted)
					{
						Context.Debug("发起 WPF 退出流程");
						app.Shutdown();
					}
				}));
				try
				{
					Context.Debug("正在等待应用程序容器退出");
					if (exited.Wait(5000))
					{
						Context.Trace("应用程序容器已退出");
					}
					else
					{
						Context.Warn("应用程序容器退出超时，停止等待");
					}
				}
				finally
				{
					dispatcher.BeginInvoke((DispatcherPriority)10, (Delegate)(Action)(() =>
					{
						app.Exit -= Exited;
					}));
				}
			}
			finally
			{
				if (exited != null)
				{
					((IDisposable)exited).Dispose();
				}
			}
		}
		void Exited(object? sender, EventArgs e)
		{
			exited.Set();
		}
	}

	public ApplicationService()
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
