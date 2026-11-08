using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.Utils.OS;

namespace PCL;

public partial class PageSetupLauncherMisc : MyPageRight, IComponentConnector
{
	private bool isFirstLoad = true;

	private bool isLoaded;

	public PageSetupLauncherMisc()
	{
		InitializeComponent();
		Loaded += PageSetupLink_Loaded;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Reload();
		};
	}

	private void PageSetupLink_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		if (!isLoaded)
		{
			isLoaded = true;
			ModAnimation.AniControlEnabled++;
			SliderLoad();
			Reload();
			ModAnimation.AniControlEnabled--;
		}
	}

	public void Reload()
	{
		ComboSystemActivity.SelectedIndex = States.System.AnnounceSolution;
		CheckSystemDisableHardwareAcceleration.Checked = Config.System.DisableHardwareAcceleration;
		SliderAniFPS.Value = Config.System.AnimationFpsLimit;
		SliderMaxLog.Value = Config.System.MaxGameLog;
		CheckSystemTelemetry.Checked = Config.System.Telemetry;
		TextSystemHttpProxy.Text = Config.Network.HttpProxy.CustomAddress;
		TextSystemHttpProxyCustomUsername.Text = Config.Network.HttpProxy.CustomUsername;
		TextSystemHttpProxyCustomPassword.Text = Config.Network.HttpProxy.CustomPassword;
		((MyRadioBox)FindName($"RadioHttpProxyType{Config.Network.HttpProxy.Type}")).SetChecked(value: true, user: false);
		CheckNetDohEnable.Checked = Config.Network.EnableDoH;
		SliderDebugAnim.Value = Config.Debug.AnimationSpeed;
		CheckDebugSkipCopy.Checked = Config.Debug.DontCopy;
		CheckDebugMode.Checked = Config.Debug.Enabled;
		CheckDebugDelay.Checked = Config.Debug.AddRandomDelay;
	}

	public void Reset()
	{
		try
		{
			Config.Network.Reset((object)null);
			Config.Debug.Reset((object)null);
			Config.System.Reset((object)null);
			ModBase.Log("[Setup] 已初始化启动器-杂项页设置");
			HintService.Hint(Lang.Text("Setup.Misc.Initialized"), HintType.Success, log: false);
			Reload();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Misc.Error.InitFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Misc.Error.InitFailed"));
		}
		Reload();
	}

	private void ComboChange(object senderRaw, SelectionChangedEventArgs e)
	{
		MyComboBox myComboBox = (MyComboBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myComboBox.Tag?.ToString(), myComboBox.SelectedIndex);
		}
	}

	private void RadioBoxChange(object senderRaw, ModBase.RouteEventArgs e)
	{
		string[] array = ((MyRadioBox)senderRaw).Tag?.ToString()?.Split("/") ?? Array.Empty<string>();
		if (ModAnimation.AniControlEnabled == 0 && array.Length >= 2)
		{
			SetByTag(array[0], int.Parse(array[1]));
		}
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

	private static void SetByTag(string tag, object value)
	{
		ConfigService.TrySetValue(tag, value, (object)null);
	}

	private void ApplyHttpProxyBtn_OnClicked(object sender, MouseButtonEventArgs e)
	{
		Config.Network.HttpProxy.CustomAddress = TextSystemHttpProxy.Text;
		Config.Network.HttpProxy.CustomUsername = TextSystemHttpProxyCustomUsername.Text;
		Config.Network.HttpProxy.CustomPassword = TextSystemHttpProxyCustomPassword.Text;
	}

	private void SliderLoad()
	{
		SliderDebugAnim.getHintText = (Func<object, object>)((object v) => ((int)v <= 29) ? (Lang.Number<double>(Math.Round(Convert.ToDouble(v) / 10.0 + 0.1, 1), "N1") + "x") : Lang.Text("Common.Action.Close"));
		SliderAniFPS.getHintText = (Func<object, string>)((object v) => Lang.Number<int>(Convert.ToInt32(v) + 1, "N0") + " FPS");
		SliderMaxLog.getHintText = (Func<object, object>)((object v) =>
		{
			int num = Convert.ToInt32(v);
			if (num <= 13)
			{
				if (num <= 5)
				{
					return num * 10 + 50;
				}
				return num * 50 - 150;
			}
			return (num <= 28) ? ((object)(num * 100 - 800)) : Lang.Text("Setup.Misc.System.MaxLogLines.Unlimited");
		});
	}

	private void Check_DisableHardwareAcceleration(object _, bool __)
	{
		HintService.Hint(Lang.Text("Setup.Misc.HardwareAcceleration.RestartNotice"));
	}

	private void CheckDebugMode_Change(object _, bool __)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			HintService.Hint(Lang.Text("Setup.Misc.Debug.Mode.Hint"), HintType.Info, log: false);
		}
	}

	private void ComboSystemActivity_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0 && ComboSystemActivity.SelectedIndex == 2 && ModMain.MyMsgBox(Lang.Text("Setup.Misc.System.Announcement.Disabled.Warning.Message"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Misc.System.Announcement.Disabled.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
		{
			ComboSystemActivity.SelectedItem = e.RemovedItems[0];
		}
	}

	private void CheckDebugMode_OnChange(object sender, bool user)
	{
		CheckBoxChange(sender, user);
		CheckDebugMode_Change(sender, user);
	}

	private void CheckSystemDisableHardwareAcceleration_OnChange(object sender, bool user)
	{
		CheckBoxChange(sender, user);
		Check_DisableHardwareAcceleration(sender, user);
	}

	private void ComboSystemActivity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ComboChange(sender, e);
		ComboSystemActivity_SelectionChanged(sender, e);
	}

	private void BtnSystemSettingExp_Click(object sender, MouseButtonEventArgs e)
	{
		string text = SystemDialogs.SelectSaveFile(Lang.Text("Setup.Misc.System.ExportSettings.SaveTitle"), "PCL 全局配置.json", Lang.Text("Setup.Misc.System.ExportSettings.Filter"), ModBase.exePath);
		if (!string.IsNullOrWhiteSpace(text))
		{
			File.Copy(ConfigService.SharedConfigPath, text, overwrite: true);
			HintService.Hint(Lang.Text("Setup.Misc.System.ExportSettings.Success"), HintType.Success);
			ModBase.OpenExplorer(text);
		}
	}

	private void BtnSystemSettingImp_Click(object sender, MouseButtonEventArgs e)
	{
		string text = SystemDialogs.SelectFile(Lang.Text("Setup.Misc.System.ExportSettings.Filter"), Lang.Text("Setup.Misc.System.ImportSettings.SelectTitle"), (string)null);
		if (!string.IsNullOrWhiteSpace(text))
		{
			File.Copy(text, ConfigService.SharedConfigPath, overwrite: true);
			ModMain.MyMsgBox(Lang.Text("Setup.Misc.System.ImportSettings.Success.Message"), null, Lang.Text("Setup.Misc.System.ImportSettings.Success.Restart"), "", "", isWarn: false, highLight: true, forceWait: true);
			Process.Start(new ProcessStartInfo(Basics.ExecutablePath));
			FormMain.EndProgramForce();
		}
	}

	private void BtnSystemStopUsingPclCe_Click(object sender, MouseButtonEventArgs e)
	{
		int num = ModMain.MyMsgBox(Lang.Text("Setup.Misc.System.StopUsingPclCe.Message"), Lang.Text("Setup.Misc.System.StopUsingPclCe.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Setup.Misc.System.StopUsingPclCe.ContinueAndRemove"), Lang.Text("Common.Action.Cancel"), isWarn: true);
		if (num < 3 && ModMain.MyMsgBox(Lang.Text("Setup.Misc.System.StopUsingPclCe.Message.Final"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
		{
			StopUsingPClCeCore(num == 2);
		}
	}

	private void StopUsingPClCeCore(bool removeMcResources)
	{
		int i;
		if (removeMcResources && States.Game.Folders != "")
		{
			string[] array = States.Game.Folders.Split('|');
			for (i = 0; i < array.Length; i++)
			{
				string path = array[i].Split('>')[1];
				Delete(new _003C_003Ez__ReadOnlySingleElementList<string>(Path.Combine(path, "PCL.ini")));
				string path2 = Path.Combine(path, "versions");
				if (Directory.Exists(path2))
				{
					Delete(from p in Directory.EnumerateDirectories(path2)
						select Path.Combine(p, "PCL", "config.v1.yml"));
				}
			}
		}
		i = 5;
		List<string> list = new List<string>(i);
		CollectionsMarshal.SetCount(list, i);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		span[0] = Paths.Data;
		span[1] = Paths.OldSharedData;
		span[2] = Paths.SharedData;
		span[3] = Paths.SharedLocalData;
		span[4] = Paths.Temp;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("/c timeout /t 5 /nobreak >nul & ");
		foreach (string item in list)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
			handler.AppendLiteral("rmdir /s /q \"");
			handler.AppendFormatted(item);
			handler.AppendLiteral("\" & ");
			stringBuilder2.Append(ref handler);
		}
		Process.Start(new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = stringBuilder.ToString(),
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden,
			UseShellExecute = false
		});
		KernelInterop.ExitProcess(0);
		static void Delete(IEnumerable<string> paths)
		{
			foreach (string path3 in paths)
			{
				try
				{
					if (File.Exists(path3))
					{
						File.Delete(path3);
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}

}
