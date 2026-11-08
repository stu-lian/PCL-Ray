using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Essentials;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI.Controls;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL;

public partial class Application : System.Windows.Application
{
	public class BindingErrorTraceListener : TraceListener
	{
		public override void Write(string message)
		{
			ModBase.Log("警告，检测到 Binding 失败：" + message);
		}

		public override void WriteLine(string message)
		{
			ModBase.Log("警告，检测到 Binding 失败：" + message);
		}
	}

	public Application()
	{
		Lifecycle.When((LifecycleState)3, (Action)_ApplicationStartup);
		Lifecycle.When((LifecycleState)5, (Action)_ShowEnvironmentWarning);
		SessionEnding += _ApplicationSessionEnding;
	}

	private static void _ApplicationStartup()
	{
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Expected Obj, but got Unknown
		try
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorTraceListener());
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			StartupValidation.EnsureWpfFont();
			string[] commandLineArguments = Basics.CommandLineArguments;
			if (commandLineArguments.Length != 0 && commandLineArguments[0] == "--gpu")
			{
				try
				{
					ModMain.SetGPUPreference(commandLineArguments[1].Trim('"'));
					Environment.Exit(5);
				}
				catch (Exception)
				{
					Environment.Exit(1);
				}
			}
			Directory.CreateDirectory(ModBase.exePath + "PCL\\Pictures");
			Directory.CreateDirectory(ModBase.exePath + "PCL\\Musics");
			Directory.CreateDirectory(Path.Combine(ModBase.pathTemp, "Cache"));
			Directory.CreateDirectory(Path.Combine(ModBase.pathTemp, "Download"));
			Directory.CreateDirectory(ModBase.pathAppdata);
			ToolTipService.InitialShowDelayProperty.OverrideMetadata(typeof(DependencyObject), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)100));
			Tooltip.Enable();
			if (Config.Preference.ShowStartupLogo)
			{
				ModMain.frmStart = new SplashScreen("Images\\splash.png");
				ModMain.frmStart.Show(false, true);
			}
			_ = Config.Debug.Enabled;
			_ = Config.Debug.AnimationSpeed;
			_ = Config.Network.HttpProxy.CustomAddress;
			_ = Config.Network.HttpProxy.CustomUsername;
			_ = Config.Network.HttpProxy.Type;
			_ = Config.Download.ThreadLimit;
			_ = Config.Download.SpeedLimit;
			_ = Config.Preference.Font;
			ConfigItem<PCL.Core.App.UpdateChannel> updateChannelConfig = Config.Update.UpdateChannelConfig;
			if (updateChannelConfig.IsDefault((object)null))
			{
				// PCL Ray only ships the stable (Release) channel.
				updateChannelConfig.SetValue((PCL.Core.App.UpdateChannel)0, (object)null, false, false);
			}
			for (int i = 1; i <= 5; i++)
			{
				string path = $"{ModBase.exePath}PCL\\Log-CE{i}.log";
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			ModBase.Log("[Start] 第一阶段加载用时：" + (TimeUtils.GetTimeTick() - ModBase.applicationStartTick) + " ms");
			ModBase.applicationStartTick = TimeUtils.GetTimeTick();
			ModAnimation.AniControlEnabled++;
		}
		catch (Exception ex2)
		{
			string executablePath = Basics.ExecutablePath;
			MessageBox.Show(ExceptionDetails.Compose(Lang.Text("Application.InitializationError.Path", new object[1] { string.IsNullOrEmpty(executablePath) ? Lang.Text("Application.InitializationError.PathUnavailable") : executablePath }), ex2), Lang.Text("SystemDialog.Startup.InitializationTitle"), MessageBoxButton.OK, MessageBoxImage.Hand);
			FormMain.EndProgramForce(ModBase.ProcessReturnValues.Exception);
		}
	}

	private static void _ShowEnvironmentWarning()
	{
		List<string> list = new List<string>();
		if (NtInterop.GetCurrentOsVersion().Build < 17763)
		{
			list.Add(Lang.Text("Application.EnvironmentWarning.WindowsVersion"));
		}
		if (SystemInfo.Is32BitSystem)
		{
			list.Add(Lang.Text("Application.EnvironmentWarning.System32Bit"));
		}
		if (ModBase.exePath.Contains(Path.GetTempPath()) || ModBase.exePath.Contains("AppData\\Local\\Temp\\"))
		{
			list.Add(Lang.Text("Application.EnvironmentWarning.TempFolder"));
		}
		if (ModBase.exePath.ContainsF("wechat_files", ignoreCase: true) || ModBase.exePath.ContainsF("WeChat Files", ignoreCase: true) || ModBase.exePath.ContainsF("Tencent Files", ignoreCase: true))
		{
			list.Add(Lang.Text("Application.EnvironmentWarning.SocialSoftwareFolder"));
		}
		if (list.Count != 0)
		{
			ModMain.MyMsgBox(Lang.Text("Application.EnvironmentWarning.Message", new object[1] { list.Join("\r\n") }), Lang.Text("Application.EnvironmentWarning.Title"), Lang.Text("Application.EnvironmentWarning.IKnow"), "", "", isWarn: true);
		}
	}

	private static void _ApplicationSessionEnding(object sender, SessionEndingCancelEventArgs e)
	{
		ModMain.frmMain.EndProgram(sendWarning: false);
	}

	private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		try
		{
			e.Handled = true;
			if (!ModBase.isProgramEnded)
			{
				ModBase.FeedbackInfo();
				string text = e.Exception.ToString();
				if (text.Contains("System.Windows.Threading.Dispatcher.Invoke") || text.Contains("MS.Internal.AppModel.ITaskbarList.HrInit") || text.Contains("未能加载文件或程序集"))
				{
					ModBase.OpenWebsite("https://get.dot.net/10");
					LogWrapper.Error(e.Exception, Lang.Text("SystemDialog.Startup.DotNetRuntimeOutdated.Message"));
				}
				else
				{
					LogWrapper.Error(e.Exception, Lang.Text("SystemDialog.Error.Unexpected.Message"));
				}
			}
		}
		catch
		{
		}
	}

	[DllImport("kernel32", CharSet = CharSet.Ansi, EntryPoint = "SetDllDirectoryA")]
	private static extern bool _SetDllDirectory(string lpPathName);

	private void _MyIconButtonClick(object sender, EventArgs e)
	{
	}
}
