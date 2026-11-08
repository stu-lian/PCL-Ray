using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupGameManage : MyPageRight, IComponentConnector
{
	private bool isLoaded;

	public PageSetupGameManage()
	{
		InitializeComponent();
		Loaded += PageSetupSystem_Loaded;
	}

	private void PageSetupSystem_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		if (!isLoaded)
		{
			isLoaded = true;
			ModAnimation.AniControlEnabled++;
			Reload();
			SliderLoad();
			if (!Lang.IsChineseMainland)
			{
				TextFilenameFormat.Visibility = Visibility.Collapsed;
				ComboDownloadTranslateV2.Visibility = Visibility.Collapsed;
				TextModManageStyle.Visibility = Visibility.Collapsed;
				ComboModLocalNameStyle.Visibility = Visibility.Collapsed;
				RowFilenameFormat.Height = new GridLength(0.0);
				RowFilenameFormatGap.Height = new GridLength(0.0);
				RowModManageStyle.Height = new GridLength(0.0);
				RowModManageStyleGap.Height = new GridLength(0.0);
			}
			ModAnimation.AniControlEnabled--;
		}
	}

	public void Reload()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected I4, but got Unknown
		SliderDownloadThread.Value = Config.Download.ThreadLimit;
		SliderDownloadFileConnection.Value = Config.Download.FileConnectionLimit;
		SliderDownloadSpeed.Value = Config.Download.SpeedLimit;
		ComboDownloadHttpMode.SelectedIndex = (int)Config.Download.HttpMode;
		ComboDownloadSource.SelectedIndex = Config.Download.FileSource;
		ComboDownloadVersion.SelectedIndex = Config.Download.VersionListSource;
		CheckDownloadAutoSelectVersion.Checked = Config.Download.AutoSelectInstance;
		CheckFixAuthlib.Checked = Config.Download.FixAuthLib;
		ComboDownloadTranslateV2.SelectedIndex = Config.Download.Comp.NameFormatV2;
		ComboDownloadMod.SelectedIndex = Config.Download.Comp.CompSourceSolution;
		ComboModLocalNameStyle.SelectedIndex = Config.Download.Comp.UiCompNameSolution;
		ComboDownloadQuickBehavior.SelectedIndex = Config.Download.Comp.QuickDownloadBehavior;
		CheckDownloadIgnoreQuilt.Checked = Config.Download.Comp.IgnoreQuilt;
		CheckDownloadAutoInstallDependencies.Checked = Config.Download.Comp.AutoInstallDependencies;
		CheckDownloadAutoInstallOptionalDependencies.Checked = Config.Download.Comp.AutoInstallOptionalDependencies;
		CheckDownloadClipboard.Checked = Config.Download.Comp.ReadClipboard;
		CheckUpdateRelease.Checked = Config.Tool.ReleaseNotification;
		CheckUpdateSnapshot.Checked = Config.Tool.SnapshotNotification;
		CheckHelpLauncherLanguage.Checked = Config.Tool.AutoChangeLanguage;
	}

	public void Reset()
	{
		try
		{
			Config.Download.Reset((object)null);
			Config.Tool.Reset((object)null);
			ModBase.Log("[Setup] 已初始化其他页设置");
			HintService.Hint(Lang.Text("Setup.GameManage.Initialized"), HintType.Success, log: false);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.GameManage.Error.InitFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.GameManage.Error.InitFailed"));
		}
		Reload();
	}

	private void CheckBoxChange(object senderRaw, bool user)
	{
		MyCheckBox myCheckBox = (MyCheckBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myCheckBox.Tag?.ToString(), myCheckBox.Checked);
		}
	}

	private void SliderChange(object senderRaw, bool user)
	{
		MySlider mySlider = (MySlider)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(mySlider.Tag?.ToString(), mySlider.Value);
		}
	}

	private void ComboChange(object senderRaw, SelectionChangedEventArgs e)
	{
		MyComboBox myComboBox = (MyComboBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myComboBox.Tag?.ToString(), myComboBox.SelectedIndex);
		}
	}

	private static void SetByTag(string tag, object value)
	{
		ConfigService.TrySetValue(tag, value, (object)null);
	}

	private void SliderLoad()
	{
		SliderDownloadThread.getHintText = (Func<object, object>)((object v) => (int)v + 1);
		SliderDownloadFileConnection.getHintText = (Func<object, object>)((object v) => (int)v + 1);
		SliderDownloadSpeed.getHintText = (Func<object, object>)((object v) =>
		{
			int num = (int)v;
			if (num <= 31)
			{
				if (num <= 14)
				{
					return Lang.Number<double>((double)(num + 1) * 0.1, "N1") + " MiB/s";
				}
				return Lang.Number<double>((double)(num - 11) * 0.5, "N1") + " MiB/s";
			}
			return (num <= 41) ? (Lang.Number<int>(num - 21, "N0") + " MiB/s") : Lang.Text("Setup.GameManage.Download.Unlimited");
		});
	}

	private void SliderDownloadThread_PreviewChange(object sender, ModBase.RouteEventArgs e)
	{
		if (SliderDownloadThread.Value >= 64 && !States.Hint.LargeDownloadThread)
		{
			States.Hint.LargeDownloadThread = true;
			ModMain.MyMsgBox(Lang.Text("Setup.GameManage.Download.Threads.TooManyWarning.Message"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.GameManage.Download.Threads.TooManyWarning.Confirm"), "", "", isWarn: true);
		}
	}

}
