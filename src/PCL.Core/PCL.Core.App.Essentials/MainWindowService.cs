using System;
using System.Windows;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.WindowCreating, Priority = int.MaxValue)]
public sealed class MainWindowService : GeneralService
{
	private static LifecycleContext? _context;

	public static Func<Window>? Loading { private get; set; }

	private static LifecycleContext Context => _context;

	private MainWindowService()
		: base("window", "主窗体", asyncStart: false)
	{
		_context = ServiceContext;
	}

	public override void Start()
	{
		Context.Debug("正在初始化 WPF 窗体");
		Window window = Loading();
		window.Loaded += (object _, RoutedEventArgs _) =>
		{
			Lifecycle.OnWindowCreated();
		};
		Lifecycle.CurrentApplication.MainWindow = window;
		Context.Trace("窗体创建完毕");
	}
}
