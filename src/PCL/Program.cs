using System;
using System.Windows.Input;
using PCL.Core.App;
using PCL.Core.App.Essentials;
using PCL.Core.App.IoC;
using PCL.Core.Utils.OS;

namespace PCL;

internal static class Program
{
	[STAThread]
	public static void Main()
	{
		if (Basics.CommandLineArguments.Contains("--console"))
		{
			KernelInterop.AllocateConsole();
		}
		Console.WriteLine("Welcome to Plain Craft Launcher 2 Ray Edition!");
		ApplicationService.Loading = () =>
		{
			Application application = new Application();
			application.InitializeComponent();
			return application;
		};
		MainWindowService.Loading = () => new FormMain();
		_ = Tablet.TabletDevices;
		Lifecycle.OnInitialize();
	}
}
