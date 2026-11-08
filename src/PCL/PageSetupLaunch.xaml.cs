using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL;

public partial class PageSetupLaunch : MyPageRight, IComponentConnector
{
	private bool isLoad;

	private int ramTextLeft = 2;

	private int ramTextRight = 1;

	public PageSetupLaunch()
	{
		Loaded += PageSetupLaunch_Loaded;
		InitializeComponent();
	}

	private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		PanBack.ScrollToHome();
		RefreshRam(showAnim: false);
		if ((object)ModInstanceList.McMcInstanceSelected == null)
		{
			BtnSwitch.Visibility = Visibility.Collapsed;
		}
		else
		{
			BtnSwitch.Visibility = Visibility.Visible;
		}
		if (!isLoad)
		{
			isLoad = true;
			ModAnimation.AniControlEnabled++;
			Reload();
			ModAnimation.AniControlEnabled--;
			DispatcherTimer val = new DispatcherTimer
			{
				Interval = new TimeSpan(0, 0, 0, 1)
			};
			val.Tick += (object? _, EventArgs _) =>
			{
				RefreshRam();
			};
			val.Start();
			RectRamGame.SizeChanged += (object s, SizeChangedEventArgs e2) =>
			{
				RefreshRamText();
			};
		}
	}

	public void Reload()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected I4, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected I4, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected I4, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected I4, but got Unknown
		try
		{
			TextArgumentTitle.Text = Config.Launch.Title;
			TextArgumentInfo.Text = Config.Launch.TypeInfo;
			ComboArgumentIndieV2.SelectedIndex = Config.Launch.IndieSolutionV2;
			ComboArgumentVisibie.SelectedIndex = (int)Config.Launch.LauncherVisibility;
			ComboArgumentPriority.SelectedValue = ((int)Config.Launch.ProcessPriority).ToString();
			ComboArgumentWindowType.SelectedIndex = (int)Config.Launch.GameWindowMode;
			TextArgumentWindowWidth.Text = Config.Launch.GameWindowWidth.ToString();
			TextArgumentWindowHeight.Text = Config.Launch.GameWindowHeight.ToString();
			ComboMsAuthType.SelectedIndex = Config.Launch.LoginMsAuthType;
			ComboPreferredIpStack.SelectedIndex = (int)Config.Launch.PreferredIpStack;
			WindowTypeUIRefresh();
			((MyRadioBox)FindName("RadioRamType" + Config.Launch.MemoryAllocationMode)).Checked = true;
			SliderRamCustom.Value = Config.Launch.CustomMemorySize;
			SliderRamInitialCustom.Value = Config.Launch.CustomInitialMemorySize;
			RamType(Config.Launch.MemoryAllocationMode);
			ComboAdvanceRenderer.SelectedIndex = Config.Launch.Renderer;
			TextAdvanceJvm.Text = Config.Launch.JvmArgs;
			TextAdvanceGame.Text = Config.Launch.GameArgs;
			TextAdvanceRun.Text = Config.Launch.PreLaunchCommand;
			CheckAdvanceRunWait.Checked = Config.Launch.PreLaunchCommandWait;
			CheckAdvanceDisableLF.Checked = Config.Launch.DisableLF;
			CheckAdvanceGraphicCard.Checked = Config.Launch.SetGpuPreference;
			CheckAdvanceNoJavaw.Checked = Config.Launch.NoJavaw;
			CheckAdvanceDisableLwjglUnsafeAgent.Checked = Config.Launch.DisableLwjglUnsafeAgent;
			CheckAdvanceDisableCrashAnalysis.Checked = Config.Launch.DisableCrashAnalysis;
			CheckAdvanceLockMemory.Checked = Config.Launch.LockMemory;
			if (SystemInfo.IsArm64System)
			{
				CheckAdvanceDisableJLW.Checked = true;
				CheckAdvanceDisableJLW.IsEnabled = false;
				CheckAdvanceDisableJLW.ToolTip = Lang.Text("Setup.Launch.Advanced.DisableJlw.Arm64Notice");
			}
			else
			{
				CheckAdvanceDisableJLW.Checked = Config.Launch.DisableJlw;
			}
		}
		catch (NullReferenceException ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Launch.Error.ConfigReset"), ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Launch.Error.ConfigReset"));
			Reset();
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Setup.Launch.Error.LoadFailed"), ModBase.LogLevel.Feedback, null, Lang.Text("Setup.Launch.Error.LoadFailed"));
		}
	}

	public void Reset()
	{
		try
		{
			Config.Launch.Reset((object)null);
			ModBase.Log("[Setup] 已初始化启动设置");
			HintService.Hint(Lang.Text("Setup.Launch.Initialized"), HintType.Success, log: false);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Launch.Error.InitFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Launch.Error.InitFailed"));
		}
		Reload();
	}

	private void RadioBoxChange(object senderRaw, ModBase.RouteEventArgs e)
	{
		string[] array = ((MyRadioBox)senderRaw).Tag?.ToString()?.Split("/") ?? Array.Empty<string>();
		if (ModAnimation.AniControlEnabled == 0 && array.Length >= 2)
		{
			SetByTag(array[0], int.Parse(array[1]));
		}
	}

	private void TextBoxChange(object senderRaw, RoutedEventArgs e)
	{
		MyTextBox myTextBox = (MyTextBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myTextBox.Tag?.ToString(), myTextBox.Text);
		}
	}

	private void TextArgumentTitle_OnTextChanged(object senderRaw, TextChangedEventArgs e)
	{
		MyTextBox myTextBox = (MyTextBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myTextBox.Tag?.ToString(), myTextBox.Text);
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
			string? text = myComboBox.Tag?.ToString();
			SetByTag(text, (text == "LaunchArgumentPriority") ? Convert.ToInt32(myComboBox.SelectedValue) : myComboBox.SelectedIndex);
			if (text == "LaunchArgumentWindowType")
			{
				WindowTypeUIRefresh();
			}
		}
	}

	private void CheckBoxChange(object senderRaw, bool user)
	{
		MyCheckBox myCheckBox = (MyCheckBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myCheckBox.Tag?.ToString(), myCheckBox.Checked);
		}
		if (myCheckBox == CheckAdvanceLockMemory)
		{
			RamType(Config.Launch.MemoryAllocationMode);
		}
	}

	private static void SetByTag(string tag, object value)
	{
		ConfigService.TrySetValue(tag, value, (object)null);
	}

	private void BtnSwitch_Click(object sender, MouseButtonEventArgs e)
	{
		ModInstanceList.McMcInstanceSelected.Load();
		PageInstanceLeft.McInstance = ModInstanceList.McMcInstanceSelected;
		ModMain.frmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.DownloadInstall);
	}

	private void ComboAdvanceRenderer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ComboChange(sender, e);
		ComboAdvanceRenderer_SelectionChanged((MyComboBox)sender, e);
	}

	private void ComboArgumentIndie_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ComboChange(sender, e);
		ComboArgumentIndie_SelectionChanged(sender, e);
	}

	public void RamType(int type)
	{
		if (SliderRamCustom != null)
		{
			SliderRamCustom.IsEnabled = type == 1;
			SliderRamInitialCustom.IsEnabled = type == 1 && !Config.Launch.LockMemory;
		}
	}

	public void RefreshRam(bool showAnim)
	{
		if (LabRamGame != null && LabRamUsed != null && !(ModMain.frmMain.pageCurrent != FormMain.PageType.Setup) && ModMain.frmSetupLeft.pageID == FormMain.PageSubType.Default)
		{
			double num = Math.Round(GetRam(ModInstanceList.McMcInstanceSelected, useVersionJavaSetup: false), 5);
			(ulong, ulong) physicalMemoryBytes = KernelInterop.GetPhysicalMemoryBytes();
			double num2 = Math.Round((double)physicalMemoryBytes.Item1 / 1024.0 / 1024.0 / 1024.0, 1);
			double num3 = Math.Round((double)physicalMemoryBytes.Item2 / 1024.0 / 1024.0 / 1024.0, 1);
			double num4 = Math.Round(Math.Min(num, num3), 5);
			double num5 = Math.Round(num2 - num3, 5);
			double num6 = Math.Round(ModBase.MathClamp(num2 - num5 - num, 0.0, 1000.0), 1);
			if (num2 <= 1.5)
			{
				SliderRamCustom.MaxValue = (int)Math.Round(Math.Max(Math.Floor((num2 - 0.3) / 0.1), 1.0));
			}
			else if (num2 <= 8.0)
			{
				SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((num2 - 1.5) / 0.5) + 12.0);
			}
			else if (num2 <= 16.0)
			{
				SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((num2 - 8.0) / 1.0) + 25.0);
			}
			else
			{
				SliderRamCustom.MaxValue = (int)Math.Round(Math.Floor((num2 - 16.0) / 2.0) + 33.0);
			}
			double? initialRam = GetInitialRam(ModInstanceList.McMcInstanceSelected, useVersionJavaSetup: false);
			string text;
			if (initialRam.HasValue && num != num4)
			{
				text = Lang.Text("Setup.Launch.Memory.SuffixBoth", new object[2]
				{
					Lang.Number<double>(initialRam.Value, "N1"),
					Lang.Number<double>(num4, "N1")
				});
			}
			else if (initialRam.HasValue)
			{
				text = Lang.Text("Setup.Launch.Memory.SuffixInitial", new object[1] { Lang.Number<double>(initialRam.Value, "N1") });
			}
			else
			{
				text = ((num == num4) ? "" : Lang.Text("Setup.Launch.Memory.SuffixAvailable", new object[1] { Lang.Number<double>(num4, "N1") }));
			}
			LabRamGame.Text = Lang.Number<double>(num, "N1") + " GiB" + ((text.Length > 0) ? (" (" + text + ")") : "");
			LabRamUsed.Text = Lang.Number<double>(num5, "N1") + " GiB";
			LabRamTotal.Text = " / " + Lang.Number<double>(num2, "N1") + " GiB";
			LabRamWarn.Visibility = ((num != 1.0 || ModJava.IsGameSet64BitJava() || SystemInfo.Is32BitSystem || !ModJava.Javas.ExistAnyJava()) ? Visibility.Collapsed : Visibility.Visible);
			HintRamTooHigh.Visibility = ((!(num / num2 > 0.75)) ? Visibility.Collapsed : Visibility.Visible);
			if (showAnim)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaGridLengthWidth(ColumnRamUsed, num5 - ColumnRamUsed.Width.Value, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
					ModAnimation.AaGridLengthWidth(ColumnRamGame, num4 - ColumnRamGame.Width.Value, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
					ModAnimation.AaGridLengthWidth(ColumnRamEmpty, num6 - ColumnRamEmpty.Width.Value, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong))
				}, "SetupLaunch Ram Grid");
			}
			else
			{
				ColumnRamUsed.Width = new GridLength(num5, GridUnitType.Star);
				ColumnRamGame.Width = new GridLength(num4, GridUnitType.Star);
				ColumnRamEmpty.Width = new GridLength(num6, GridUnitType.Star);
			}
		}
	}

	private void RefreshRam()
	{
		RefreshRam(showAnim: true);
	}

	private void RefreshRamText()
	{
		double actualWidth = RectRamUsed.ActualWidth;
		double actualWidth2 = PanRamDisplay.ActualWidth;
		double actualWidth3 = LabRamGame.ActualWidth;
		double actualWidth4 = LabRamUsed.ActualWidth;
		double actualWidth5 = LabRamTotal.ActualWidth;
		double actualWidth6 = LabRamGameTitle.ActualWidth;
		double actualWidth7 = LabRamUsedTitle.ActualWidth;
		int num = ((!(actualWidth - 30.0 < actualWidth4) && !(actualWidth - 30.0 < actualWidth7)) ? ((actualWidth - 25.0 < actualWidth4 + actualWidth5) ? 1 : 2) : 0);
		if (ramTextLeft != num)
		{
			ramTextLeft = num;
			switch (num)
			{
			case 0:
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabRamUsed, 0.0 - LabRamUsed.Opacity, 100),
					ModAnimation.AaOpacity(LabRamTotal, 0.0 - LabRamTotal.Opacity, 100),
					ModAnimation.AaOpacity(LabRamUsedTitle, 0.0 - LabRamUsedTitle.Opacity, 100)
				}, "SetupLaunch Ram TextLeft");
				break;
			case 1:
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabRamUsed, 1.0 - LabRamUsed.Opacity, 100),
					ModAnimation.AaOpacity(LabRamTotal, 0.0 - LabRamTotal.Opacity, 100),
					ModAnimation.AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
				}, "SetupLaunch Ram TextLeft");
				break;
			case 2:
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabRamUsed, 1.0 - LabRamUsed.Opacity, 100),
					ModAnimation.AaOpacity(LabRamTotal, 1.0 - LabRamTotal.Opacity, 100),
					ModAnimation.AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
				}, "SetupLaunch Ram TextLeft");
				break;
			}
		}
		int num2 = ((!(actualWidth2 < actualWidth3 + 2.0 + actualWidth) && !(actualWidth2 < actualWidth6 + 2.0 + actualWidth)) ? 1 : 0);
		if (num2 == 0)
		{
			if (ModAnimation.AniControlEnabled == 0 && (ramTextRight != num2 || ModAnimation.AniIsRun("SetupLaunch Ram TextRight")))
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaX(LabRamGame, actualWidth2 - actualWidth3 - LabRamGame.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaX(LabRamGameTitle, actualWidth2 - actualWidth6 - LabRamGameTitle.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
				}, "SetupLaunch Ram TextRight");
			}
			else
			{
				ModAnimation.AniStop("SetupLaunch Ram TextRight");
				LabRamGame.Margin = new Thickness(actualWidth2 - actualWidth3, 3.0, 0.0, 0.0);
				LabRamGameTitle.Margin = new Thickness(actualWidth2 - actualWidth6, 0.0, 0.0, 5.0);
			}
		}
		else if (ModAnimation.AniControlEnabled == 0 && (ramTextRight != num2 || ModAnimation.AniIsRun("SetupLaunch Ram TextRight")))
		{
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaX(LabRamGame, 2.0 + actualWidth - LabRamGame.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaX(LabRamGameTitle, 2.0 + actualWidth - LabRamGameTitle.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
			}, "SetupLaunch Ram TextRight");
		}
		else
		{
			ModAnimation.AniStop("SetupLaunch Ram TextRight");
			LabRamGame.Margin = new Thickness(2.0 + actualWidth, 3.0, 0.0, 0.0);
			LabRamGameTitle.Margin = new Thickness(2.0 + actualWidth, 0.0, 0.0, 5.0);
		}
		ramTextRight = num2;
	}

	public static double GetRamFromTick(int value)
	{
		if (value <= 25)
		{
			if (value <= 12)
			{
				return (double)value * 0.1 + 0.3;
			}
			return (double)(value - 12) * 0.5 + 1.5;
		}
		if (value <= 33)
		{
			return value - 25 + 8;
		}
		return (value - 33) * 2 + 16;
	}

	public static bool HasCustomXms(McInstance version)
	{
		string text = (((object)version == null) ? "" : ((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)version.PathInstance]);
		string text2 = (string.IsNullOrEmpty(text) ? Config.Launch.JvmArgs : text);
		if (!string.IsNullOrEmpty(text2))
		{
			return text2.Contains("-Xms", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static double? GetInitialRam(McInstance version, bool useVersionJavaSetup, bool? is32BitJava = null)
	{
		if (Config.Launch.LockMemory)
		{
			return null;
		}
		if (HasCustomXms(version))
		{
			return null;
		}
		if (Config.Launch.MemoryAllocationMode != 1)
		{
			return null;
		}
		int customInitialMemorySize = Config.Launch.CustomInitialMemorySize;
		if (customInitialMemorySize <= 0)
		{
			return null;
		}
		return Math.Min(GetRamFromTick(customInitialMemorySize), GetRam(version, useVersionJavaSetup, is32BitJava));
	}

	public static double GetRam(McInstance version, bool useVersionJavaSetup, bool? is32BitJava = null)
	{
		double num = 0.0;
		if (Config.Launch.MemoryAllocationMode == 0)
		{
			double num2 = Math.Round((double)KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024.0 / 1024.0 / 1024.0 * 10.0) / 10.0;
			if ((object)version != null && !version.IsLoaded)
			{
				version.Load();
			}
			double val;
			double num4;
			double num5;
			double num6;
			if ((object)version != null && version.Modable)
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(version.PathIndie + "mods\\");
				int num3 = (directoryInfo.Exists ? directoryInfo.GetFiles().Length : 0);
				val = 0.5 + (double)num3 / 150.0;
				num4 = 1.5 + (double)num3 / 90.0;
				num5 = 2.7 + (double)num3 / 50.0;
				num6 = 4.5 + (double)num3 / 25.0;
			}
			else if ((object)version != null && version.Info.HasOptiFine)
			{
				val = 0.5;
				num4 = 1.5;
				num5 = 3.0;
				num6 = 5.0;
			}
			else
			{
				val = 0.5;
				num4 = 1.5;
				num5 = 2.5;
				num6 = 4.0;
			}
			(double, double)[] array = new (double, double)[4]
			{
				(num4, 1.0),
				(num5 - num4, 0.7),
				(num6 - num5, 0.4),
				(num6, 0.15)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(double, double) tuple = array[i];
				double item = tuple.Item1;
				double item2 = tuple.Item2;
				num += Math.Min(num2 * item2, item);
				num2 -= item / item2;
				if (num2 < 0.1)
				{
					break;
				}
			}
			num = Math.Round(Math.Max(num, val), 1);
		}
		else
		{
			num = GetRamFromTick(Config.Launch.CustomMemorySize);
		}
		if (is32BitJava ?? (!ModJava.IsGameSet64BitJava(useVersionJavaSetup ? version : null)))
		{
			num = Math.Min(1.0, num);
		}
		return num;
	}

	private void WindowTypeUIRefresh()
	{
		if (ComboArgumentWindowType != null)
		{
			if (ComboArgumentWindowType.SelectedIndex == 3 && LabArgumentWindowMiddle != null && LabArgumentWindowMiddle.Visibility == Visibility.Collapsed)
			{
				LabArgumentWindowMiddle.Visibility = Visibility.Visible;
				TextArgumentWindowHeight.Visibility = Visibility.Visible;
				TextArgumentWindowWidth.Visibility = Visibility.Visible;
			}
			else if (ComboArgumentWindowType.SelectedIndex != 3 && LabArgumentWindowMiddle != null && LabArgumentWindowMiddle.Visibility == Visibility.Visible)
			{
				LabArgumentWindowMiddle.Visibility = Visibility.Collapsed;
				TextArgumentWindowHeight.Visibility = Visibility.Collapsed;
				TextArgumentWindowWidth.Visibility = Visibility.Collapsed;
			}
		}
	}

	private void ComboArgumentVisibie_SelectionChanged(object sender, SelectionChangedEventArgs sizeChangedEventArgs)
	{
		ComboChange(sender, sizeChangedEventArgs);
		if (ModAnimation.AniControlEnabled == 0 && ComboArgumentVisibie.SelectedIndex == 0 && ModMain.MyMsgBox(Lang.Text("Setup.Launch.Options.Visibility.CloseImmediately.Warning.Message"), Lang.Text("Setup.Launch.Options.Visibility.CloseImmediately.Warning.Title"), Lang.Text("Setup.Launch.Options.Visibility.CloseImmediately.Warning.Continue"), Lang.Text("Common.Action.Cancel")) == 2)
		{
			ComboArgumentVisibie.SelectedItem = sizeChangedEventArgs.RemovedItems[0];
		}
	}

	private void ComboArgumentIndie_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			ModMain.MyMsgBox(Lang.Text("Setup.Launch.Options.InstanceIsolation.DefaultPolicyHint"));
		}
	}

	private void TextAdvanceRun_TextChanged(object sender, TextChangedEventArgs e)
	{
		CheckAdvanceRunWait.Visibility = (string.IsNullOrEmpty(TextAdvanceRun.Text) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void TextAdvanceJvm_TextChanged(object sender, TextChangedEventArgs e)
	{
		BtnAdvanceJvmReset.Visibility = ((TextAdvanceJvm.Text == Config.Launch.JvmArgsConfig.DefaultValue) ? Visibility.Hidden : Visibility.Visible);
	}

	private void BtnAdvanceJvmReset_Click(object sender, EventArgs e)
	{
		Config.Launch.JvmArgsConfig.Reset((object)null);
		Reload();
	}

	private void ComboAdvanceRenderer_SelectionChanged(MyComboBox sender, object e)
	{
		if (ModAnimation.AniControlEnabled != 0)
		{
			return;
		}
		if (!States.Hint.Renderer && ComboAdvanceRenderer.SelectedIndex != 0)
		{
			if (ModMain.MyMsgBox(Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Message"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				ComboAdvanceRenderer.SelectedItem = ((SelectionChangedEventArgs)e).RemovedItems[0];
				return;
			}
			Config.Launch.Renderer = sender.SelectedIndex;
			States.Hint.Renderer = true;
		}
		else
		{
			Config.Launch.Renderer = sender.SelectedIndex;
		}
	}

}
