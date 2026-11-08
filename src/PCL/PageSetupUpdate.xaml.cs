using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL;

public partial class PageSetupUpdate : MyPageRight, IComponentConnector
{
	private enum UpdateStatus
	{
		Checking,
		Available,
		Error,
		Latest
	}

	public VersionDataModel updateInfo;

	public PageSetupUpdate()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
	}

	private void Init()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected I4, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected I4, but got Unknown
		ModAnimation.AniControlEnabled++;
		TextMirrorCDK.Password = Config.Update.MirrorChyanKey;
		// PCL Ray only ships the stable channel.
		ComboSystemUpdateChannel.SelectedIndex = 0;
		ComboSystemUpdateMode.SelectedIndex = (int)Config.Update.UpdateMode;
		TextCurrentVersion.Text = "PCL Ray " + VersionNameFormat(ModBase.versionBaseName);
		ModAnimation.AniControlEnabled--;
		CheckUpdate();
	}

	private async Task<UpdateStatus> IsLatestAsync()
	{
		try
		{
			if (await UpdateManager.remoteServer.IsLatestAsync(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64, SemVer.Parse(ModBase.versionBaseName), ModBase.versionCode))
			{
				ModBase.Log("[Update] 已是最新版本");
				return UpdateStatus.Latest;
			}
			ModBase.Log("[Update] 有可用的新版本");
			return UpdateStatus.Available;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Update.Error.NetworkFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Setup.Update.Error.NetworkFailed"));
			return UpdateStatus.Error;
		}
	}

	public async void CheckUpdate()
	{
		ModBase.Log("[Update] 开始检查更新");
		CardUpdate.Visibility = Visibility.Collapsed;
		CardCheck.Visibility = Visibility.Visible;
		TextCurrentDesc.Text = Lang.Text("Setup.Update.Checking");
		BtnCheckAgain.IsEnabled = false;
		switch (await IsLatestAsync())
		{
		case UpdateStatus.Available:
		{
			Exception ex = null;
			try
			{
				updateInfo = UpdateManager.remoteServer.GetLatestVersion(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64);
				TextUpdateName.Text = "PCL Ray " + VersionNameFormat(updateInfo.VersionName);
				string text = updateInfo.Changelog.Between("<summary>", "</summary>");
				if (!updateInfo.Changelog.Contains("<summary>") || string.IsNullOrWhiteSpace(text.Trim()))
				{
					TextChangelog.Text = Lang.Text("Setup.Update.Changelog.Empty");
				}
				else
				{
					TextChangelog.Text = text;
				}
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
			BtnCheckAgain.IsEnabled = true;
			if (updateInfo == null)
			{
				TextCurrentDesc.Text = Lang.Text("Setup.Update.CheckFailed");
				if (ex != null)
				{
					ModBase.Log(ex, "[Update] 检查更新失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Update.Check.Failed"));
				}
				else
				{
					ModBase.Log("[Update] 检查更新失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Update.Check.Failed"));
				}
				break;
			}
			if (UpdateManager.updateLoader != null && UpdateManager.updateLoader.State == ModBase.LoadState.Loading)
			{
				BtnUpdate_Timer();
				BtnUpdate.IsEnabled = false;
			}
			else if (UpdateManager.isUpdateWaitingRestart)
			{
				BtnUpdate.Text = Lang.Text("Setup.Update.RestartInstall");
				BtnUpdate.IsEnabled = true;
			}
			else
			{
				BtnUpdate.Text = Lang.Text("Setup.Update.Install");
				BtnUpdate.IsEnabled = true;
			}
			CardUpdate.Visibility = Visibility.Visible;
			CardCheck.Visibility = Visibility.Collapsed;
			break;
		}
		case UpdateStatus.Latest:
			CardUpdate.Visibility = Visibility.Collapsed;
			CardCheck.Visibility = Visibility.Visible;
			BtnCheckAgain.IsEnabled = true;
			TextCurrentDesc.Text = Lang.Text("Setup.Update.Latest");
			break;
		case UpdateStatus.Error:
			CardUpdate.Visibility = Visibility.Collapsed;
			CardCheck.Visibility = Visibility.Visible;
			BtnCheckAgain.IsEnabled = true;
			TextCurrentDesc.Text = Lang.Text("Setup.Update.CheckFailed");
			break;
		}
	}

	public void BtnUpdate_Timer()
	{
		while (UpdateManager.updateLoader != null && UpdateManager.updateLoader.State == ModBase.LoadState.Loading)
		{
			ModBase.RunInUi(() =>
			{
				BtnUpdate.Text = Lang.Number<double>(UpdateManager.updateLoader.Progress, "P2");
			});
			Thread.Sleep(200);
		}
	}

	private void BtnUpdate_Click(object sender, MouseButtonEventArgs e)
	{
		if (UpdateManager.isUpdateWaitingRestart)
		{
			UpdateManager.UpdateRestart(triggerRestartAndByEnd: true);
		}
		UpdateManager.UpdateStart(UpdateEnums.UpdateType.UpdateNow);
	}

	private void BtnChangelogDetail_Click(object sender, EventArgs e)
	{
		if (updateInfo == null)
		{
			ModMain.MyMsgBox(Lang.Text("Setup.Update.Changelog.Unavailable"), Lang.Text("Setup.Update.Changelog.Title"));
		}
		else
		{
			ModMain.MyMsgBoxMarkdown(updateInfo.Changelog, Lang.Text("Setup.Update.Changelog.Title"));
		}
	}

	private void ComboSystemUpdateMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			Config.Update.UpdateMode = (LauncherAutoUpdateBehavior)ComboSystemUpdateMode.SelectedIndex;
		}
	}

	private void ComboSystemUpdateBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled != 0)
		{
			return;
		}
		bool flag = false;
		switch (ComboSystemUpdateChannel.SelectedIndex)
		{
		case 1:
			if (ModMain.MyMsgBox(Lang.Text("Setup.Update.Channel.Beta.Warning.Message"), Lang.Text("Setup.Update.Channel.Common.Warning.Title"), Lang.Text("Setup.Update.Channel.Common.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				flag = true;
			}
			else
			{
				CheckUpdate();
			}
			break;
		case 2:
		{
			if (ModMain.MyMsgBox(Lang.Text("Setup.Update.Channel.Dev.Warning.Message"), Lang.Text("Setup.Update.Channel.Common.Warning.Title"), Lang.Text("Setup.Update.Channel.Common.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				flag = true;
				break;
			}
			string text = Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.ExpectedInput");
			if (ModMain.MyMsgBoxInput(Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.Title"), Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.Message", new object[1] { text }), "", null, "", Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.Submit"), Lang.Text("Common.Action.Cancel"), isWarn: true) == text)
			{
				CheckUpdate();
				break;
			}
			HintService.Hint(Lang.Text("Setup.Update.Channel.Dev.FinalConfirm.WrongInput"));
			flag = true;
			break;
		}
		}
		if (flag)
		{
			ModAnimation.AniControlEnabled++;
			ComboSystemUpdateChannel.SelectedItem = e.RemovedItems[0];
			ModAnimation.AniControlEnabled--;
		}
		else
		{
			Config.Update.UpdateChannel = PCL.Core.App.UpdateChannel.Release;
		}
	}

	private void TextMirrorCDK_PasswordChanged(object sender, EventArgs e)
	{
		Config.Update.MirrorChyanKey = TextMirrorCDK.Password;
	}

	private void BtnGetMirrorCDK_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenWebsite("https://mirrorchyan.com/");
	}

	private void BtnChangelog_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenWebsite("https://github.com/stu-lian/PCL-Ray/releases/tag/v" + ModBase.versionBaseName);
	}

	public string VersionNameFormat(string str)
	{
		str = str.Replace("v", "");
		if (!str.Contains("-"))
		{
			return str;
		}
		string text = str.AfterLast("-");
		str = str.BeforeLast("-");
		return str + " " + text.Replace(".", " ").Replace("beta", "Beta").Replace("rc", "RC");
	}

	private void BtnCheckAgain_OnClick(object sender, MouseButtonEventArgs e)
	{
		CheckUpdate();
	}

}
