using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.App.Localization;
using PCL.Core.IO;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.Java.UserPreference;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Validate;
using FluentValidation;

namespace PCL;

public partial class PageInstanceSetup : MyPageRight, IComponentConnector
{
	private bool isLoaded;

	private int ramTextLeft = 2;

	private int ramTextRight = 1;

	private int comboServerLoginLast;

	private bool isReverting;

	public PageInstanceSetup()
	{
		Loaded += PageSetupSystem_Loaded;
		InitializeComponent();
		ComboArgumentIndieV2.SelectionChanged += ComboArgumentIndieV2_SelectionChanged;
		TextArgumentTitle.TextChanged += TextArgumentTitle_TextChanged;
		TextArgumentInfo.TextChanged += TextBoxChange;
		ComboArgumentJava.SelectionChanged += JavaSelectionUpdate;
		RadioRamType2.Check += RadioBoxChange;
		RadioRamType0.Check += RadioBoxChange;
		RadioRamType1.Check += RadioBoxChange;
		SliderRamCustom.Change += SliderChange;
		SliderRamInitialCustom.Change += SliderChange;
		ComboServerLoginRequire.SelectionChanged += ComboServerLogin_Changed;
		TextServerAuthServer.TextChanged += TextBoxChange;
		TextServerAuthServer.LostFocus += TextServerAuthServer_MouseLeave;
		TextServerAuthRegister.TextChanged += TextBoxChange;
		TextServerAuthName.TextChanged += TextBoxChange;
		TextServerEnter.TextChanged += TextBoxChange;
		BtnServerAuthLittle.Click += BtnServerAuthLittle_Click;
		BtnServerAuthLock.Click += BtnServerAuthLock_Click;
		BtnServerNewProfile.Click += BtnServerNewProfile_Click;
		ComboAdvanceRenderer.SelectionChanged += ComboAdvanceRenderer_SelectionChanged;
		TextAdvanceJvm.TextChanged += TextBoxChange;
		TextAdvanceGame.TextChanged += TextBoxChange;
		TextAdvanceClasspathHead.TextChanged += TextBoxChange;
		TextAdvanceRun.TextChanged += TextAdvanceRun_TextChanged;
		CheckAdvanceRunWait.Change += CheckBoxChange;
		CheckAdvanceJava.Change += CheckBoxChange;
		CheckAdvanceAssetsV2.Change += CheckBoxChange;
		CheckAdvanceUseProxyV2.Change += CheckBoxChange;
		CheckAdvanceDisableJLW.Change += CheckBoxChange;
		CheckAdvanceDisableLF.Change += CheckBoxChange;
		CheckUseDebugLog4j2Config.Change += CheckUseDebugLog4j2Config_CheckChanged;
		CheckAdvanceDisableLwjglUnsafeAgent.Change += CheckBoxChange;
		BtnSwitch.Click += BtnSwitch_Click;
		BtnCopyInstance.Click += BtnCopyInstance_Click;
		TextServerEnter.TextChanged += TextServerEnter_Change;
		ComboArgumentJava.DropDownOpened += ComboArgumentJava_DropDownOpened;
		CheckArgumentTitleEmpty.Change += CheckArgumentTitleEmpty_Change;
	}

	private void PageSetupSystem_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		PanBack.ScrollToHome();
		RefreshRam(showAnim: false);
		ModAnimation.AniControlEnabled++;
		Reload();
		ModAnimation.AniControlEnabled--;
		if (!isLoaded)
		{
			isLoaded = true;
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
		try
		{
			TextArgumentTitle.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.Title)[(object)PageInstanceLeft.McInstance.PathInstance];
			CheckArgumentTitleEmpty.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.UseGlobalTitle)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextArgumentInfo.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.TypeInfo)[(object)PageInstanceLeft.McInstance.PathInstance];
			_ = PageInstanceLeft.McInstance.PathIndie;
			ComboArgumentIndieV2.SelectedIndex = ((!((ParameterizedProperty<object, bool>)(object)Config.Instance.IndieV2)[(object)PageInstanceLeft.McInstance.PathInstance]) ? 1 : 0);
			CheckArgumentTitleEmpty.Visibility = ((TextArgumentTitle.Text.Length > 0) ? Visibility.Collapsed : Visibility.Visible);
			TextArgumentTitle.HintText = ((CheckArgumentTitleEmpty.Checked == true) ? Lang.Text("Common.Option.Default") : Lang.Text("Instance.Setup.FollowGlobal"));
			RefreshJavaComboBox();
			int type = ((ParameterizedProperty<object, int>)(object)Config.Instance.MemorySolution)[(object)PageInstanceLeft.McInstance.PathInstance];
			((MyRadioBox)FindName("RadioRamType" + type)).Checked = true;
			SliderRamCustom.Value = ((ParameterizedProperty<object, int>)(object)Config.Instance.CustomMemorySize)[(object)PageInstanceLeft.McInstance.PathInstance];
			SliderRamInitialCustom.Value = ((ParameterizedProperty<object, int>)(object)Config.Instance.CustomInitialMemorySize)[(object)PageInstanceLeft.McInstance.PathInstance];
			RamType(type);
			TextServerEnter.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.ServerToEnter)[(object)PageInstanceLeft.McInstance.PathInstance];
			ComboServerLoginRequire.SelectedIndex = ((ParameterizedProperty<object, int>)(object)Config.InstanceAuth.LoginRequirementSolution)[(object)PageInstanceLeft.McInstance.PathInstance];
			comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
			TextServerAuthServer.Text = ((ParameterizedProperty<object, string>)(object)Config.InstanceAuth.AuthServerAddress)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextServerAuthName.Text = ((ParameterizedProperty<object, string>)(object)Config.InstanceAuth.AuthServerDisplayName)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextServerAuthRegister.Text = ((ParameterizedProperty<object, string>)(object)Config.InstanceAuth.AuthRegisterAddress)[(object)PageInstanceLeft.McInstance.PathInstance];
			ServerLogin(ComboServerLoginRequire.SelectedIndex);
			ComboAdvanceRenderer.SelectedIndex = ((ParameterizedProperty<object, int>)(object)Config.Instance.Renderer)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextAdvanceClasspathHead.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.ClasspathHead)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextAdvanceJvm.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextAdvanceGame.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.GameArgs)[(object)PageInstanceLeft.McInstance.PathInstance];
			TextAdvanceRun.Text = ((ParameterizedProperty<object, string>)(object)Config.Instance.PreLaunchCommand)[(object)PageInstanceLeft.McInstance.PathInstance];
			CheckAdvanceRunWait.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.PreLaunchCommandWait)[(object)PageInstanceLeft.McInstance.PathInstance];
			CheckAdvanceDisableLwjglUnsafeAgent.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableLwjglUnsafeAgent)[(object)PageInstanceLeft.McInstance.PathInstance];
			if (((ParameterizedProperty<object, int>)(object)Config.Instance.AssetVerifySolutionV1)[(object)PageInstanceLeft.McInstance.PathInstance] == 2)
			{
				ModBase.Log("[Setup] 已迁移老版本的关闭文件校验设置");
				Config.Instance.AssetVerifySolutionV1Config.Reset((object)PageInstanceLeft.McInstance.PathInstance);
				((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableAssetVerifyV2)[(object)PageInstanceLeft.McInstance.PathInstance] = true;
			}
			CheckAdvanceAssetsV2.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableAssetVerifyV2)[(object)PageInstanceLeft.McInstance.PathInstance];
			CheckAdvanceUseProxyV2.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.UseProxy)[(object)PageInstanceLeft.McInstance.PathInstance];
			CheckAdvanceJava.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.IgnoreJavaCompatibility)[(object)PageInstanceLeft.McInstance.PathInstance];
			if (SystemInfo.IsArm64System)
			{
				CheckAdvanceDisableJLW.Checked = true;
				CheckAdvanceDisableJLW.IsEnabled = false;
				CheckAdvanceDisableJLW.ToolTip = Lang.Text("Setup.Launch.Advanced.DisableJlw.Arm64ToolTip");
			}
			else
			{
				CheckAdvanceDisableJLW.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableJlw)[(object)PageInstanceLeft.McInstance.PathInstance];
			}
			CheckUseDebugLog4j2Config.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.UseDebugLof4j2Config)[(object)PageInstanceLeft.McInstance.PathInstance];
			CheckAdvanceDisableLF.Checked = ((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableLF)[(object)PageInstanceLeft.McInstance.PathInstance];
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "重载实例独立设置时出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Setup.Error.OperationFailed"));
		}
	}

	public void Reset()
	{
		try
		{
			if (!((ParameterizedProperty<object, bool>)(object)Config.InstanceAuth.AuthLocked)[(object)PageInstanceLeft.McInstance.PathInstance])
			{
				Config.InstanceAuth.Reset((object)PageInstanceLeft.McInstance.PathInstance);
			}
			Config.Instance.Reset((object)PageInstanceLeft.McInstance.PathInstance);
			ModBase.Log("[Setup] 已初始化实例独立设置");
			HintService.Hint(Lang.Text("Instance.Setup.Initialize.Success"), HintType.Success, log: false);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "初始化实例独立设置失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Setup.Error.OperationFailed"));
		}
		Reload();
	}

	private static void SetByTag(string tag, object value)
	{
		ConfigService.TrySetValue(tag, value, (object)PageInstanceLeft.McInstance.PathInstance);
	}

	private void RadioBoxChange(object o, ModBase.RouteEventArgs routeEventArgs)
	{
		if (ModAnimation.AniControlEnabled == 0 && o is MyRadioBox { Tag: string tag })
		{
			int num = tag.IndexOf('/');
			if (num >= 0)
			{
				SetByTag(tag.Substring(0, num), int.Parse(tag.Substring(num + 1)));
			}
		}
	}

	private void TextBoxChange(object o, TextChangedEventArgs textChangedEventArgs)
	{
		if (ModAnimation.AniControlEnabled == 0 && o is MyTextBox { Tag: var tag } myTextBox)
		{
			SetByTag(tag?.ToString(), myTextBox.Text);
		}
	}

	private void SliderChange(object o, bool user)
	{
		if (ModAnimation.AniControlEnabled == 0 && o is MySlider { Tag: var tag } mySlider)
		{
			SetByTag(tag?.ToString(), mySlider.Value);
		}
	}

	private void ComboChange(MyComboBox sender, object e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
		}
	}

	private void CheckBoxChange(object sender, bool user)
	{
		if (ModAnimation.AniControlEnabled == 0 && sender is MyCheckBox { Tag: var tag } myCheckBox)
		{
			SetByTag(tag?.ToString(), myCheckBox.Checked == true);
		}
	}

	private void BtnSwitch_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Setup);
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
		if (LabRamGame != null && LabRamUsed != null && !(ModMain.frmMain.pageCurrent != FormMain.PageType.InstanceSetup) && ModMain.frmInstanceLeft.pageID == FormMain.PageSubType.DownloadInstall)
		{
			double num = Math.Round(GetRam(PageInstanceLeft.McInstance), 5);
			(ulong, ulong) physicalMemoryBytes = KernelInterop.GetPhysicalMemoryBytes();
			double num2 = Math.Round((double)(physicalMemoryBytes.Item1 / 1024 / 1024 / 1024), 1);
			double num3 = Math.Round((double)(physicalMemoryBytes.Item2 / 1024 / 1024 / 1024), 1);
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
			double? initialRam = GetInitialRam(PageInstanceLeft.McInstance);
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
			LabRamWarn.Visibility = ((num != 1.0 || ModJava.IsGameSet64BitJava(PageInstanceLeft.McInstance) || SystemInfo.Is32BitSystem || !ModJava.Javas.ExistAnyJava()) ? Visibility.Collapsed : Visibility.Visible);
			HintRamTooHigh.Visibility = ((!(num / num2 > 0.75)) ? Visibility.Collapsed : Visibility.Visible);
			if (showAnim)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaGridLengthWidth(ColumnRamUsed, num5 - ColumnRamUsed.Width.Value, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
					ModAnimation.AaGridLengthWidth(ColumnRamGame, num4 - ColumnRamGame.Width.Value, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
					ModAnimation.AaGridLengthWidth(ColumnRamEmpty, num6 - ColumnRamEmpty.Width.Value, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong))
				}, "VersionSetup Ram Grid");
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
				}, "VersionSetup Ram TextLeft");
				break;
			case 1:
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabRamUsed, 1.0 - LabRamUsed.Opacity, 100),
					ModAnimation.AaOpacity(LabRamTotal, 0.0 - LabRamTotal.Opacity, 100),
					ModAnimation.AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
				}, "VersionSetup Ram TextLeft");
				break;
			case 2:
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabRamUsed, 1.0 - LabRamUsed.Opacity, 100),
					ModAnimation.AaOpacity(LabRamTotal, 1.0 - LabRamTotal.Opacity, 100),
					ModAnimation.AaOpacity(LabRamUsedTitle, 0.7 - LabRamUsedTitle.Opacity, 100)
				}, "VersionSetup Ram TextLeft");
				break;
			}
		}
		int num2 = ((!(actualWidth2 < actualWidth3 + 2.0 + actualWidth) && !(actualWidth2 < actualWidth6 + 2.0 + actualWidth)) ? 1 : 0);
		if (num2 == 0)
		{
			if (ModAnimation.AniControlEnabled == 0 && (ramTextRight != num2 || ModAnimation.AniIsRun("VersionSetup Ram TextRight")))
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaX(LabRamGame, actualWidth2 - actualWidth3 - LabRamGame.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaX(LabRamGameTitle, actualWidth2 - actualWidth6 - LabRamGameTitle.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
				}, "VersionSetup Ram TextRight");
			}
			else
			{
				LabRamGame.Margin = new Thickness(actualWidth2 - actualWidth3, 3.0, 0.0, 0.0);
				LabRamGameTitle.Margin = new Thickness(actualWidth2 - actualWidth6, 0.0, 0.0, 5.0);
			}
		}
		else if (ModAnimation.AniControlEnabled == 0 && (ramTextRight != num2 || ModAnimation.AniIsRun("VersionSetup Ram TextRight")))
		{
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaX(LabRamGame, 2.0 + actualWidth - LabRamGame.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaX(LabRamGameTitle, 2.0 + actualWidth - LabRamGameTitle.Margin.Left, 100, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
			}, "VersionSetup Ram TextRight");
		}
		else
		{
			LabRamGame.Margin = new Thickness(2.0 + actualWidth, 3.0, 0.0, 0.0);
			LabRamGameTitle.Margin = new Thickness(2.0 + actualWidth, 0.0, 0.0, 5.0);
		}
		ramTextRight = num2;
	}

	public static double GetRam(McInstance version, bool? is32BitJava = null)
	{
		string text = version?.PathInstance;
		if (((ParameterizedProperty<object, int>)(object)Config.Instance.MemorySolution)[(object)text] == 2)
		{
			return PageSetupLaunch.GetRam(version, useVersionJavaSetup: true, is32BitJava);
		}
		double num = 0.0;
		if (((ParameterizedProperty<object, int>)(object)Config.Instance.MemorySolution)[(object)text] == 0)
		{
			double num2 = Math.Round((double)(KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024 / 1024 / 1024 * 10)) / 10.0;
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
			double num7 = num4;
			num += Math.Min(num2, num7);
			num2 -= num7;
			if (num2 >= 0.1)
			{
				num7 = num5 - num4;
				num += Math.Min(num2 * 0.7, num7);
				num2 -= num7 / 0.7;
				if (num2 >= 0.1)
				{
					num7 = num6 - num5;
					num += Math.Min(num2 * 0.4, num7);
					num2 -= num7 / 0.4;
					if (num2 >= 0.1)
					{
						num7 = num6;
						num += Math.Min(num2 * 0.15, num7);
						num2 -= num7 / 0.15;
					}
				}
			}
			num = Math.Round(Math.Max(num, val), 1);
		}
		else
		{
			num = PageSetupLaunch.GetRamFromTick(((ParameterizedProperty<object, int>)(object)Config.Instance.CustomMemorySize)[(object)text]);
		}
		if (is32BitJava ?? (!ModJava.IsGameSet64BitJava(PageInstanceLeft.McInstance)))
		{
			num = Math.Min(1.0, num);
		}
		return num;
	}

	public static double? GetInitialRam(McInstance version, bool? is32BitJava = null)
	{
		if (Config.Launch.LockMemory)
		{
			return null;
		}
		if (PageSetupLaunch.HasCustomXms(version))
		{
			return null;
		}
		string text = version?.PathInstance;
		switch (((ParameterizedProperty<object, int>)(object)Config.Instance.MemorySolution)[(object)text])
		{
		case 2:
			return PageSetupLaunch.GetInitialRam(version, useVersionJavaSetup: true, is32BitJava);
		case 1:
		{
			int num = ((ParameterizedProperty<object, int>)(object)Config.Instance.CustomInitialMemorySize)[(object)text];
			if (num <= 0)
			{
				return null;
			}
			return Math.Min(PageSetupLaunch.GetRamFromTick(num), GetRam(version, is32BitJava));
		}
		default:
			return null;
		}
	}

	private void ComboServerLogin_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			ServerLogin(ComboServerLoginRequire.SelectedIndex);
			if (TextServerAuthServer.IsValidated)
			{
				BtnServerAuthLock.IsEnabled = true;
			}
			else
			{
				BtnServerAuthLock.IsEnabled = false;
			}
			if (((ComboServerLoginRequire.SelectedIndex != 2 && ComboServerLoginRequire.SelectedIndex != 3) || TextServerAuthServer.IsValidated) && comboServerLoginLast != ComboServerLoginRequire.SelectedIndex)
			{
				comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
				((ParameterizedProperty<object, int>)(object)Config.InstanceAuth.LoginRequirementSolution)[(object)PageInstanceLeft.McInstance.PathInstance] = ComboServerLoginRequire.SelectedIndex;
			}
		}
	}

	private void TextServerAuthServer_MouseLeave(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(TextServerAuthServer.Text))
		{
			return;
		}
		if (!TextServerAuthServer.Text.EndsWithF("/api/yggdrasil/") && !TextServerAuthServer.Text.EndsWithF("/api/yggdrasil"))
		{
			if (TextServerAuthServer.Text.EndsWithF("/"))
			{
				TextServerAuthServer.Text += "api/yggdrasil";
				HintService.Hint(Lang.Text("Instance.Setup.Server.AuthServer.AutoFormatted"));
			}
			else
			{
				TextServerAuthServer.Text += "/api/yggdrasil";
				HintService.Hint(Lang.Text("Instance.Setup.Server.AuthServer.AutoFormatted"));
			}
		}
		if (TextServerAuthServer.Text.EndsWithF("/api/yggdrasil/"))
		{
			TextServerAuthServer.Text = TextServerAuthServer.Text.BeforeLast("/");
			HintService.Hint(Lang.Text("Instance.Setup.Server.AuthServer.AutoFormatted"));
		}
		comboServerLoginLast = ComboServerLoginRequire.SelectedIndex;
		ComboChange(ComboServerLoginRequire, null);
	}

	public void ServerLogin(int type)
	{
		LabServerAuthName.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		TextServerAuthName.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		LabServerAuthRegister.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		TextServerAuthRegister.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		LabServerAuthServer.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		TextServerAuthServer.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		BtnServerAuthLittle.Visibility = ((type != 2 && type != 3) ? Visibility.Collapsed : Visibility.Visible);
		BtnServerNewProfile.Visibility = (((type != 2 && type != 3) || !ProfileUi.CanCreateOtherProfile()) ? Visibility.Collapsed : Visibility.Visible);
		if (type == 0 || type == 1)
		{
			BtnServerAuthLock.Visibility = Visibility.Collapsed;
		}
		else
		{
			BtnServerAuthLock.Visibility = Visibility.Visible;
		}
		if (((ParameterizedProperty<object, bool>)(object)Config.InstanceAuth.AuthLocked)[(object)PageInstanceLeft.McInstance.PathInstance])
		{
			HintServerLoginLock.Visibility = Visibility.Visible;
			ComboServerLoginRequire.IsEnabled = false;
			TextServerAuthServer.IsEnabled = false;
			TextServerAuthName.IsEnabled = false;
			TextServerAuthRegister.IsEnabled = false;
			BtnServerAuthLittle.IsEnabled = false;
		}
		else
		{
			HintServerLoginLock.Visibility = Visibility.Collapsed;
			ComboServerLoginRequire.IsEnabled = true;
			TextServerAuthServer.IsEnabled = true;
			TextServerAuthName.IsEnabled = true;
			TextServerAuthRegister.IsEnabled = true;
			BtnServerAuthLittle.IsEnabled = true;
		}
		CardServer.TriggerForceResize();
		if (type != 2 && type != 3)
		{
			LabServerAuthServerSecurity.Visibility = Visibility.Collapsed;
			LabServerAuthServerSecurityVerify.Visibility = Visibility.Collapsed;
		}
		else if (TextServerAuthServer.Text.StartsWithF("https://"))
		{
			LabServerAuthServerSecurity.Visibility = Visibility.Collapsed;
			LabServerAuthServerSecurityVerify.Visibility = Visibility.Visible;
		}
		else if (TextServerAuthServer.Text.StartsWithF("http://"))
		{
			LabServerAuthServerSecurity.Visibility = Visibility.Visible;
			LabServerAuthServerSecurityVerify.Visibility = Visibility.Collapsed;
		}
		else
		{
			LabServerAuthServerSecurity.Visibility = Visibility.Collapsed;
			LabServerAuthServerSecurityVerify.Visibility = Visibility.Collapsed;
		}
	}

	private void BtnServerAuthLittle_Click(object sender, MouseButtonEventArgs e)
	{
		if (string.IsNullOrEmpty(TextServerAuthServer.Text) || !(TextServerAuthServer.Text != "https://littleskin.cn/api/yggdrasil") || ModMain.MyMsgBox(Lang.Text("Instance.Setup.Server.LittleSkin.Override.Message"), Lang.Text("Instance.Setup.Server.LittleSkin.Override.Title"), Lang.Text("Instance.Setup.Server.LittleSkin.Override.Continue"), Lang.Text("Common.Action.Cancel")) != 2)
		{
			TextServerAuthServer.Text = "https://littleskin.cn/api/yggdrasil";
			TextServerAuthRegister.Text = "https://littleskin.cn/auth/register";
			TextServerAuthName.Text = Lang.Text("Instance.Setup.Server.LittleSkin.Name");
		}
	}

	private void BtnServerAuthLock_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModMain.MyMsgBox(Lang.Text("Instance.Setup.Server.LockLoginMethod.Message"), Lang.Text("Instance.Setup.Server.LockLoginMethod.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
		{
			((ParameterizedProperty<object, bool>)(object)Config.InstanceAuth.AuthLocked)[(object)PageInstanceLeft.McInstance.PathInstance] = true;
			Reload();
		}
	}

	private void BtnServerNewProfile_Click(object sender, MouseButtonEventArgs e)
	{
		if (!ProfileUi.CanCreateOtherProfile())
		{
			return;
		}
		ModMain.frmMain.PageChange(new FormMain.PageStackData
		{
			page = FormMain.PageType.Launch
		});
		PageLoginAuth.draggedAuthServer = TextServerAuthServer.Text;
		ModBase.RunInNewThread(() =>
		{
			Thread.Sleep(150);
			ModBase.RunInUi(() =>
			{
				ModMain.frmLaunchLeft.RefreshPage(anim: true, ModLaunch.McLoginType.Auth);
			});
		});
	}

	private static void TextServerEnter_Change(object sender, TextChangedEventArgs e)
	{
		if (sender is MyTextBox myTextBox)
		{
			myTextBox.Text = myTextBox.Text.Replace("：", ":");
		}
	}

	public void RefreshJavaComboBox()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected Obj, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected Obj, but got Unknown
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Expected Obj, but got Unknown
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected Obj, but got Unknown
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Expected Obj, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected Obj, but got Unknown
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Expected Obj, but got Unknown
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		if (ComboArgumentJava == null)
		{
			return;
		}
		JavaPreference instanceJavaPreference = ModJava.GetInstanceJavaPreference(PageInstanceLeft.McInstance);
		ComboArgumentJava.Items.Clear();
		ComboArgumentJava.Items.Add(new MyComboBoxItem
		{
			Content = Lang.Text("Instance.Setup.FollowGlobal"),
			Tag = (object)new UseGlobalPreference()
		});
		ComboArgumentJava.Items.Add(new MyComboBoxItem
		{
			Content = Lang.Text("Instance.Setup.Options.Java.AutoSelect"),
			Tag = (object)new AutoSelect()
		});
		MyComboBoxItem newItem;
		if (instanceJavaPreference is UseRelativePath)
		{
			UseRelativePath val = (UseRelativePath)instanceJavaPreference;
			string fullPath = Path.GetFullPath(Path.Combine(Basics.ExecutableDirectory, val.RelativePath));
			JavaEntry val2 = ModJava.Javas.Get(fullPath);
			if (Files.IsPathWithinDirectory(fullPath, Basics.ExecutableDirectory) && val2 != null && val2.IsEnabled)
			{
				MyComboBoxItem myComboBoxItem = new MyComboBoxItem();
				myComboBoxItem.Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative.WithJava", new object[1] { ((object)val2).ToString() });
				myComboBoxItem.Tag = (object)new UseRelativePath(val.RelativePath);
				myComboBoxItem.ToolTip = Lang.Text("Instance.Setup.Options.Java.RelativePathToolTip", new object[2] { val.RelativePath, fullPath });
				newItem = myComboBoxItem;
			}
			else
			{
				MyComboBoxItem myComboBoxItem = new MyComboBoxItem();
				myComboBoxItem.Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative.Invalid");
				myComboBoxItem.Tag = (object)new UseRelativePath(val.RelativePath);
				myComboBoxItem.ToolTip = Lang.Text("Instance.Setup.Options.Java.InvalidPathToolTip", new object[1] { fullPath });
				newItem = myComboBoxItem;
			}
		}
		else
		{
			newItem = new MyComboBoxItem
			{
				Content = Lang.Text("Instance.Setup.Options.Java.SelectRelative"),
				Tag = (object)new UseRelativePath("jre\\bin\\java.exe"),
				ToolTip = Lang.Text("Instance.Setup.Options.Java.SelectRelativeToolTip")
			};
		}
		ComboArgumentJava.Items.Add(newItem);
		MyComboBoxItem myComboBoxItem2 = null;
		try
		{
			foreach (JavaEntry sortedJava in ModJava.Javas.GetSortedJavaList())
			{
				MyComboBoxItem myComboBoxItem = new MyComboBoxItem();
				myComboBoxItem.Content = ((object)sortedJava).ToString();
				myComboBoxItem.ToolTip = Lang.Text("Instance.Setup.Options.Java.Details.ToolTip", new object[3]
				{
					sortedJava.Installation.JavaExePath,
					sortedJava.Installation.Version,
					sortedJava.Source
				});
				myComboBoxItem.Tag = sortedJava;
				MyComboBoxItem myComboBoxItem3 = myComboBoxItem;
				ToolTipService.SetInitialShowDelay((DependencyObject)(object)myComboBoxItem3, 300);
				ToolTipService.SetBetweenShowDelay((DependencyObject)(object)myComboBoxItem3, 100);
				ComboArgumentJava.Items.Add(myComboBoxItem3);
			}
		}
		catch (Exception ex)
		{
			((ParameterizedProperty<object, string>)(object)Config.Instance.SelectedJava)[(object)PageInstanceLeft.McInstance.PathInstance] = "使用全局设置";
			ModBase.Log(ex, "更新实例设置 Java 下拉框失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Setup.Error.OperationFailed"));
			ComboArgumentJava.Items.Clear();
			ComboArgumentJava.Items.Add(new MyComboBoxItem
			{
				Content = Lang.Text("Instance.Setup.Options.Java.LoadFailed"),
				IsEnabled = false
			});
			ComboArgumentJava.SelectedIndex = 0;
			RefreshRam(showAnim: true);
			return;
		}
		if (instanceJavaPreference == null)
		{
			myComboBoxItem2 = ComboArgumentJava.Items[1] as MyComboBoxItem;
		}
		else if (instanceJavaPreference is UseGlobalPreference)
		{
			myComboBoxItem2 = ComboArgumentJava.Items[0] as MyComboBoxItem;
		}
		else if (instanceJavaPreference is UseRelativePath)
		{
			myComboBoxItem2 = ComboArgumentJava.Items[2] as MyComboBoxItem;
		}
		else if (instanceJavaPreference is ExistingJava)
		{
			ExistingJava val3 = (ExistingJava)instanceJavaPreference;
			int i = 3;
			for (int num = ComboArgumentJava.Items.Count - 1; i <= num; i++)
			{
				if (ComboArgumentJava.Items[i] is MyComboBoxItem myComboBoxItem4 && myComboBoxItem4.Tag is JavaEntry && string.Equals(((JavaEntry)myComboBoxItem4.Tag).Installation.JavaExePath, val3.JavaExePath, StringComparison.OrdinalIgnoreCase))
				{
					myComboBoxItem2 = myComboBoxItem4;
					break;
				}
			}
		}
		if (myComboBoxItem2 == null && ComboArgumentJava.Items.Count > 1)
		{
			myComboBoxItem2 = ComboArgumentJava.Items[1] as MyComboBoxItem;
		}
		if (myComboBoxItem2 != null)
		{
			ComboArgumentJava.SelectedItem = myComboBoxItem2;
		}
		if (!ModJava.Javas.ExistAnyJava() && ComboArgumentJava.Items.Count <= 3)
		{
			ComboArgumentJava.Items.Clear();
			MyComboBoxItem myComboBoxItem5 = new MyComboBoxItem
			{
				Content = Lang.Text("Instance.Setup.Options.Java.NoRuntime"),
				ToolTip = Lang.Text("Instance.Setup.Options.Java.NoRuntime.ToolTip"),
				IsEnabled = false
			};
			ComboArgumentJava.Items.Add(myComboBoxItem5);
			ComboArgumentJava.SelectedItem = myComboBoxItem5;
		}
		RefreshRam(showAnim: true);
	}

	private void ComboArgumentJava_DropDownOpened(object? sender, EventArgs e)
	{
		if (ComboArgumentJava.SelectedItem == null)
		{
			ComboArgumentJava.IsDropDownOpen = false;
		}
		else if (ComboArgumentJava.Items[0] is MyComboBoxItem myComboBoxItem && ((string)myComboBoxItem.Content == Lang.Text("Instance.Setup.Options.Java.NoRuntime") || (string)myComboBoxItem.Content == Lang.Text("Instance.Setup.Options.Java.LoadFailed")))
		{
			ComboArgumentJava.IsDropDownOpen = false;
		}
	}

	private void JavaSelectionUpdate(object sender, SelectionChangedEventArgs e)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected Obj, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected Obj, but got Unknown
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected Obj, but got Unknown
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected Obj, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected Obj, but got Unknown
		if (ModAnimation.AniControlEnabled != 0 || ComboArgumentJava.SelectedItem == null || !(ComboArgumentJava.SelectedItem is MyComboBoxItem myComboBoxItem) || (myComboBoxItem.Tag == null && (string)myComboBoxItem.Content != Lang.Text("Instance.Setup.Options.Java.AutoSelect")))
		{
			return;
		}
		JavaPreference value = null;
		string text = "";
		object tag = myComboBoxItem.Tag;
		if ((tag == null || tag is AutoSelect) ? true : false)
		{
			value = (JavaPreference)new AutoSelect();
			text = "[Java] 修改实例 Java 选择设置：自动选择";
		}
		else if (myComboBoxItem.Tag is UseGlobalPreference)
		{
			value = (JavaPreference)new UseGlobalPreference();
			text = "[Java] 修改实例 Java 选择设置：跟随全局设置";
		}
		else if (myComboBoxItem.Tag is UseRelativePath)
		{
			string text2 = SystemDialogs.SelectFile(Lang.Text("Setup.Java.SelectFile.Filter"), Lang.Text("Setup.Java.SelectFile.Title"), Basics.ExecutableDirectory);
			if (string.IsNullOrWhiteSpace(text2))
			{
				return;
			}
			text2 = Path.GetFullPath(text2);
			string relativePath = Path.GetRelativePath(Basics.ExecutableDirectory, text2);
			if (!Files.IsPathWithinDirectory(relativePath, Basics.ExecutableDirectory))
			{
				HintService.Hint(Lang.Text("Instance.Setup.Options.Java.PathOutOfRange"), HintType.Error);
				return;
			}
			value = (JavaPreference)new UseRelativePath(relativePath);
			text = "[Java] 修改实例 Java 选择设置：相对路径 | " + relativePath;
		}
		else if (myComboBoxItem.Tag is JavaEntry)
		{
			JavaEntry val = (JavaEntry)myComboBoxItem.Tag;
			value = (JavaPreference)new ExistingJava(val.Installation.JavaExePath);
			text = $"[Java] 修改实例 Java 选择设置：{val}";
		}
		string text3 = JsonSerializer.Serialize<JavaPreference>(value, JsonCompat.SerializerOptions);
		((ParameterizedProperty<object, string>)(object)Config.Instance.SelectedJava)[(object)PageInstanceLeft.McInstance.PathInstance] = text3;
		ModBase.Log(text);
		RefreshRam(showAnim: true);
	}

	private void ComboArgumentIndieV2_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0 && !isReverting)
		{
			if (ModMain.MyMsgBox(Lang.Text("Instance.Setup.Options.InstanceIsolation.Message"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				isReverting = true;
				ComboArgumentIndieV2.SelectedItem = e.RemovedItems[0];
				isReverting = false;
			}
			else
			{
				bool flag = ComboArgumentIndieV2.SelectedIndex == 0;
				((ParameterizedProperty<object, bool>)(object)Config.Instance.IndieV2)[(object)PageInstanceLeft.McInstance.PathInstance] = flag;
			}
		}
	}

	private void CheckArgumentTitleEmpty_Change(object sender, bool e)
	{
		TextArgumentTitle.HintText = ((CheckArgumentTitleEmpty.Checked == true) ? Lang.Text("Common.Option.Default") : Lang.Text("Instance.Setup.FollowGlobal"));
		CheckBoxChange(sender, e);
	}

	private void TextArgumentTitle_TextChanged(object sender, TextChangedEventArgs e)
	{
		CheckArgumentTitleEmpty.Visibility = ((TextArgumentTitle.Text.Length > 0) ? Visibility.Collapsed : Visibility.Visible);
		TextBoxChange(sender, e);
	}

	private void TextAdvanceRun_TextChanged(object sender, TextChangedEventArgs e)
	{
		CheckAdvanceRunWait.Visibility = (string.IsNullOrEmpty(TextAdvanceRun.Text) ? Visibility.Collapsed : Visibility.Visible);
		TextBoxChange(sender, e);
	}

	private void ComboAdvanceRenderer_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled != 0)
		{
			return;
		}
		if (!States.Hint.Renderer && ComboAdvanceRenderer.SelectedIndex != 0)
		{
			if (ModMain.MyMsgBox(Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Message"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				ComboAdvanceRenderer.SelectedItem = e.RemovedItems[0];
				return;
			}
			ComboChange(ComboAdvanceRenderer, e);
			States.Hint.Renderer = true;
		}
		else
		{
			ComboChange(ComboAdvanceRenderer, e);
		}
	}

	private void CheckUseDebugLog4j2Config_CheckChanged(object sender, bool e)
	{
		if (ModAnimation.AniControlEnabled != 0 || !(sender is MyCheckBox { Checked: var flag } myCheckBox))
		{
			return;
		}
		if (flag == true && !States.Hint.DebugLog4j2Config)
		{
			if (ModMain.MyMsgBox(Lang.Text("Instance.Setup.Advanced.UseDebugLog4j.Message"), Lang.Text("Common.Dialog.Warning"), Lang.Text("Setup.Launch.Advanced.Renderer.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				myCheckBox.Checked = false;
				return;
			}
			CheckBoxChange(sender, e);
			States.Hint.DebugLog4j2Config = true;
		}
		else
		{
			CheckBoxChange(sender, e);
		}
	}


	private void BtnCopyInstance_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			McInstance instance = PageInstanceLeft.McInstance;
			string sourcePath = instance.PathInstance;
			string sourceName = instance.Name;
			FolderNameValidator validator = new FolderNameValidator(Path.Combine(ModFolder.mcFolderSelected, "versions"), true, false, false);
			string newName = ModMain.MyMsgBoxInput(Lang.Text("Instance.Setup.CopyInstance.InputTitle"), Lang.Text("Instance.Setup.CopyInstance.InputMessage"), sourceName + " " + Lang.Text("Instance.Setup.CopyInstance.DefaultSuffix"), new Collection<IValidator<string>> { (IValidator<string>)(object)validator }, Lang.Text("Instance.Setup.CopyInstance.InputHint"));
			if (string.IsNullOrWhiteSpace(newName))
			{
				return;
			}
			if (string.Equals(newName, sourceName, StringComparison.OrdinalIgnoreCase))
			{
				HintService.Hint(Lang.Text("Instance.Setup.CopyInstance.SameName"), HintType.Error);
				return;
			}
			string targetPath = Path.Combine(ModFolder.mcFolderSelected, "versions", newName) + "\\";
			if (Directory.Exists(targetPath))
			{
				HintService.Hint(Lang.Text("Instance.Setup.CopyInstance.Exists", new object[1] { newName }), HintType.Error);
				return;
			}
			string taskName = Lang.Text("Instance.Setup.CopyInstance.TaskName", new object[1] { newName });
			List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>();
			loaders.Add(new ModLoader.LoaderTask<string, string>(taskName, (ModLoader.LoaderTask<string, string> task) =>
			{
				// 配置写入是异步的，先落盘再复制，避免副本丢失最近的设置修改。
				((DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance)).InvalidateCache(sourcePath);
				Directory.CreateDirectory(targetPath);
				ModBase.CopyDirectory(sourcePath, targetPath, (double delta) =>
				{
					task.Progress += delta;
				});
				ModInstanceHelper.RenameInstanceFiles(targetPath, sourceName, newName, sourcePath);
				((DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance)).InvalidateCache(targetPath);
				ModBase.IniClearCache(Path.Combine(targetPath, "PCL", "Setup.ini"));
			}));
			ModLoader.LoaderCombo<string> combo = new ModLoader.LoaderCombo<string>(taskName, loaders);
			combo.OnStateChanged = (ModLoader.LoaderBase loader) =>
			{
				switch (loader.State)
				{
				case ModBase.LoadState.Finished:
					HintService.Hint(Lang.Text("Instance.Setup.CopyInstance.Success", new object[1] { newName }), HintType.Success);
					ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
					ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
					break;
				case ModBase.LoadState.Failed:
					ModBase.Log(loader.Error, "复制实例 " + sourceName + " 失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Setup.CopyInstance.Failed"));
					try
					{
						((DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance)).InvalidateCache(targetPath);
						ModBase.DeleteDirectory(targetPath);
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "清理复制失败的实例文件夹失败");
					}
					break;
				case ModBase.LoadState.Aborted:
					HintService.Hint(Lang.Text("Instance.Setup.CopyInstance.Cancelled", new object[1] { newName }));
					break;
				}
			};
			combo.Start(targetPath);
			ModLoader.LoaderTaskbarAdd(combo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "复制实例 " + PageInstanceLeft.McInstance.Name + " 失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Setup.CopyInstance.Failed"));
		}
	}
}
