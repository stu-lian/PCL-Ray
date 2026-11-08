using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Security.Authentication;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL;

public partial class PageLoginMs : StackPanel, IComponentConnector
{
	public PageLoginMs()
	{
		InitializeComponent();
		BtnBack.Click += BtnBack_Click;
		BtnLogin.Click += BtnLogin_Click;
	}

	private void BtnBack_Click(object sender, EventArgs e)
	{
		ProfileService.IsCreatingProfile = false;
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: true);
		});
	}

	private void BtnLogin_Click(object sender, EventArgs e)
	{
		BtnLogin.IsEnabled = false;
		BtnBack.Visibility = Visibility.Collapsed;
		BtnLogin.Text = Lang.Number<double>(0.0, "P0");
		ModBase.RunInNewThread(() =>
		{
			McProfile current = ProfileService.Current;
			bool flag = false;
			try
			{
				ProfileService.Select((McProfile)null);
				ModLaunch.mcLoginMsLoader.Start(ProfileUi.GetLoginData(ModLaunch.McLoginType.Ms), isForceRestart: true);
				while (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Loading)
				{
					ModBase.RunInUi(() =>
					{
						BtnLogin.Text = Lang.Number<double>(ModLaunch.mcLoginMsLoader.Progress, "P0");
					});
					Thread.Sleep(50);
				}
				if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Finished)
				{
					flag = true;
					ModBase.RunInUi(() =>
					{
						ModMain.frmLaunchLeft.RefreshPage(anim: true);
					});
					return;
				}
				if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Aborted)
				{
					throw new ThreadInterruptedException();
				}
				if (ModLaunch.mcLoginMsLoader.Error == null)
				{
					throw new InvalidOperationException(Lang.Text("Launch.Account.Microsoft.Error.Unknown"));
				}
				throw ModLaunch.mcLoginMsLoader.Error;
			}
			catch (ThreadInterruptedException)
			{
				if (!flag && current != null)
				{
					ProfileService.Select(current);
				}
				HintService.Hint(Lang.Text("Launch.Account.LoginCancelled"));
			}
			catch (Exception ex2)
			{
				if (!flag && current != null)
				{
					ProfileService.Select(current);
				}
				if (!(ex2.Message == "$$"))
				{
					if (ex2.Message.StartsWith("$"))
					{
						HintService.Hint(Lang.Text("Launch.Account.Microsoft.LoginFailed.WithDetail", new object[1] { ex2.Message.TrimStart('$') }), HintType.Error);
					}
					else if (ex2 is AuthenticationException && ex2.Message.ContainsF("SSL/TLS"))
					{
						ModBase.Log(ex2, Lang.Text("Launch.Account.Microsoft.LoginFailed.Message") + "\r\n" + ex2.Message, ModBase.LogLevel.Msgbox, null, Lang.Text("Launch.Account.Microsoft.Error.OperationFailed"));
					}
					else
					{
						ModBase.Log(ex2, Lang.Text("Launch.Account.Microsoft.LoginFailed.Title"), ModBase.LogLevel.Msgbox, null, Lang.Text("Launch.Account.Microsoft.LoginFailed.Title"));
					}
				}
			}
			finally
			{
				ProfileService.IsCreatingProfile = false;
				ModBase.RunInUi(() =>
				{
					BtnLogin.IsEnabled = true;
					BtnBack.Visibility = Visibility.Visible;
					BtnLogin.Text = Lang.Text("Launch.Account.Login");
				});
			}
		}, "Ms Login");
	}

}
