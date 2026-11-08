using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.Utils;

namespace PCL;

public partial class PageSetupUI : MyPageRight, IComponentConnector
{
	public bool isLoaded;

	public string[] ThemeColors
	{
		get
		{
			if (Basics.IsAprilFool)
			{
				return new string[4]
				{
					Lang.Text("Setup.Ui.Theme.Color.SkyBlue"),
					Lang.Text("Setup.Ui.Theme.Color.CatBlue"),
					Lang.Text("Setup.Ui.Theme.Color.CrashBlue"),
					Lang.Text("Setup.Ui.Theme.Color.Hmcl")
				};
			}
			return new string[3]
			{
				Lang.Text("Setup.Ui.Theme.Color.SkyBlue"),
				Lang.Text("Setup.Ui.Theme.Color.CatBlue"),
				Lang.Text("Setup.Ui.Theme.Color.CrashBlue")
			};
		}
	}

	public static bool HiddenForceShow
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			HiddenRefresh();
		}
	}

	public PageSetupUI()
	{
		InitializeComponent();
		Loaded += PageSetupUI_Loaded;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			HiddenRefresh();
		};
	}

	private void PageSetupUI_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		ModAnimation.AniControlEnabled++;
		Reload();
		ModAnimation.AniControlEnabled--;
		if (!isLoaded)
		{
			isLoaded = true;
			SliderLoad();
			PanLauncherHide.Visibility = Visibility.Visible;
		}
	}

	public void Reload()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected I4, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected I4, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected I4, but got Unknown
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Expected I4, but got Unknown
		try
		{
			SliderLauncherOpacity.Value = Config.Preference.Theme.WindowOpacity;
			CheckLauncherLogo.Checked = Config.Preference.ShowStartupLogo;
			ComboDarkMode.SelectedIndex = (int)Config.Preference.Theme.ColorMode;
			ComboDarkColor.SelectedIndex = (int)Config.Preference.Theme.DarkColor;
			ComboLightColor.SelectedIndex = (int)Config.Preference.Theme.LightColor;
			CheckShowLaunchingHint.Checked = Config.Preference.ShowLaunchingHint;
			ComboUiFont.SelectedFontTag = Config.Preference.Font;
			ComboUiMotdFont.SelectedFontTag = Config.Preference.MotdFont;
			CheckBlur.Checked = Config.Preference.Blur.IsEnabled;
			SliderBlurValue.Value = Config.Preference.Blur.Radius;
			SliderBlurSamplingRate.Value = Config.Preference.Blur.SamplingRate;
			ComboBlurType.SelectedIndex = Config.Preference.Blur.KernelType;
			PanBlurValue.Visibility = ((CheckBlur.Checked != true) ? Visibility.Collapsed : Visibility.Visible);
			CheckLockWindowSize.Checked = Config.Preference.LockWindowSize;
			SliderBackgroundOpacity.Value = Config.Preference.Background.WallpaperOpacity;
			SliderBackgroundBlur.Value = Config.Preference.Background.WallpaperBlurRadius;
			ComboBackgroundSuit.SelectedIndex = Config.Preference.Background.WallpaperSuitMode;
			CheckBackgroundColorful.Checked = Config.Preference.Background.BackgroundColorful;
			bool autoPauseVideo = Config.Preference.Background.AutoPauseVideo;
			CheckAutoPauseVideo.Checked = autoPauseVideo;
			if (ModVideoBack.IsGaming && autoPauseVideo)
			{
				BtnBackgroundRefresh.IsEnabled = false;
			}
			BackgroundRefresh(isHint: false, refresh: false);
			((MyRadioBox)FindName("RadioLogoType" + (int)Config.Preference.WindowTitleType)).Checked = true;
			CheckLogoLeft.Visibility = ((!RadioLogoType0.Checked) ? Visibility.Collapsed : Visibility.Visible);
			PanLogoText.Visibility = ((!RadioLogoType2.Checked) ? Visibility.Collapsed : Visibility.Visible);
			PanLogoChange.Visibility = ((!RadioLogoType3.Checked) ? Visibility.Collapsed : Visibility.Visible);
			TextLogoText.Text = Config.Preference.WindowTitleCustomText;
			CheckLogoLeft.Checked = Config.Preference.TopBarLeftAlign;
			CheckMusicRandom.Checked = Config.Preference.Music.ShufflePlayback;
			CheckMusicAuto.Checked = Config.Preference.Music.StartOnStartup;
			CheckMusicStop.Checked = Config.Preference.Music.StopInGame;
			CheckMusicStart.Checked = Config.Preference.Music.StartInGame;
			CheckMusicSMTC.Checked = Config.Preference.Music.EnableSMTC;
			SliderMusicVolume.Value = Config.Preference.Music.Volume;
			MusicRefreshUI();
			try
			{
				ComboCustomPreset.SelectedIndex = Config.Preference.Homepage.SelectedPreset;
			}
			catch
			{
				Config.Preference.Homepage.SelectedPresetConfig.Reset((object)null);
			}
			((MyRadioBox)FindName("RadioCustomType" + Config.Preference.Homepage.Type)).Checked = true;
			TextCustomNet.Text = Config.Preference.Homepage.CustomUrl;
			ModSetup.UiCustomType(Config.Preference.Homepage.Type);
			Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
			CheckHiddenPageDownload.Checked = hide.PageDownload;
			CheckHiddenPageSetup.Checked = hide.PageSetup;
			CheckHiddenPageTools.Checked = hide.PageTools;
			CheckHiddenSetupLaunch.Checked = hide.SetupLaunch;
			CheckHiddenSetupUI.Checked = hide.SetupUi;
			CheckHiddenSetupLauncherLanguage.Checked = hide.SetupLauncherLanguage;
			CheckHiddenSetupGameManage.Checked = hide.SetupGameManage;
			CheckHiddenSetupJava.Checked = hide.SetupJava;
			CheckHiddenLauncherMisc.Checked = hide.SetupLauncherMisc;
			CheckHiddenSetupUpdate.Checked = hide.SetupUpdate;
			CheckHiddenSetupGameLink.Checked = hide.SetupGameLink;
			CheckHiddenSetupAbout.Checked = hide.SetupAbout;
			CheckHiddenSetupFeedback.Checked = hide.SetupFeedback;
			CheckHiddenSetupLog.Checked = hide.SetupLog;
			CheckHiddenToolsGameLink.Checked = hide.ToolsGameLink;
			CheckHiddenToolsTest.Checked = hide.ToolsTest;
			CheckHiddenVersionEdit.Checked = hide.InstanceEdit;
			CheckHiddenVersionExport.Checked = hide.InstanceExport;
			CheckHiddenVersionSave.Checked = hide.InstanceSave;
			CheckHiddenVersionScreenshot.Checked = hide.InstanceScreenshot;
			CheckHiddenVersionMod.Checked = hide.InstanceMod;
			CheckHiddenVersionResourcePack.Checked = hide.InstanceResourcePack;
			CheckHiddenVersionShader.Checked = hide.InstanceShader;
			CheckHiddenVersionSchematic.Checked = hide.InstanceSchematic;
			CheckHiddenVersionServer.Checked = hide.InstanceServer;
			CheckHiddenFunctionSelect.Checked = hide.FunctionSelect;
			CheckHiddenFunctionModUpdate.Checked = hide.FunctionModUpdate;
			CheckHiddenFunctionHidden.Checked = hide.FunctionHidden;
		}
		catch (NullReferenceException ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Ui.Error.ConfigReset"), ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.ConfigReset"));
			Reset();
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Setup.Ui.Error.LoadFailed"), ModBase.LogLevel.Feedback, null, Lang.Text("Setup.Ui.Error.LoadFailed"));
		}
	}

	public void Reset()
	{
		try
		{
			Config.Preference.Reset((object)null);
			ModBase.Log("[Setup] 已初始化个性化设置！");
			HintService.Hint(Lang.Text("Setup.Ui.Initialized"), HintType.Success, log: false);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Ui.Error.InitFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.InitFailed"));
		}
		Reload();
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

	private void CheckBoxChange(object senderRaw, bool user)
	{
		MyCheckBox myCheckBox = (MyCheckBox)senderRaw;
		if (ModAnimation.AniControlEnabled == 0)
		{
			SetByTag(myCheckBox.Tag?.ToString(), myCheckBox.Checked);
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

	private void RadioBoxChange(object senderRaw, ModBase.RouteEventArgs e)
	{
		string[] array = ((MyRadioBox)senderRaw).Tag?.ToString()?.Split("/") ?? Array.Empty<string>();
		if (ModAnimation.AniControlEnabled == 0 && array.Length >= 2)
		{
			SetByTag(array[0], int.Parse(array[1]));
		}
	}

	private static void SetByTag(string tag, object value)
	{
		ConfigService.TrySetValue(tag, value, (object)null);
	}

	private void ComboFontChange(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			Config.Preference.Font = ComboUiFont.SelectedFontTag;
		}
	}

	private void ComboMotdFontChange(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			Config.Preference.MotdFont = ComboUiMotdFont.SelectedFontTag;
		}
	}

	private void BtnUIBgOpen_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenExplorer(ModBase.exePath + "PCL\\Pictures\\");
	}

	private void BtnBackgroundRefresh_Click(object sender, MouseButtonEventArgs e)
	{
		BackgroundRefresh(isHint: true, refresh: true);
	}

	public void BackgroundRefreshUI(bool show, int count)
	{
		if (PanBackgroundOpacity != null)
		{
			if (show)
			{
				PanBackgroundOpacity.Visibility = Visibility.Visible;
				PanBackgroundBlur.Visibility = Visibility.Visible;
				PanBackgroundSuit.Visibility = Visibility.Visible;
				BtnBackgroundClear.Visibility = Visibility.Visible;
				CheckAutoPauseVideo.Visibility = Visibility.Visible;
				CardBackground.Title = Lang.Text("Setup.Ui.Background.TitleWithCount", new object[1] { count });
			}
			else
			{
				PanBackgroundOpacity.Visibility = Visibility.Collapsed;
				PanBackgroundBlur.Visibility = Visibility.Collapsed;
				PanBackgroundSuit.Visibility = Visibility.Collapsed;
				BtnBackgroundClear.Visibility = Visibility.Collapsed;
				CheckAutoPauseVideo.Visibility = Visibility.Collapsed;
				CardBackground.Title = Lang.Text("Setup.Ui.Background.TitleDefault");
			}
			CardBackground.TriggerForceResize();
		}
	}

	private void BtnBackgroundClear_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModMain.MyMsgBox(Lang.Text("Setup.Ui.Background.Clear.Confirm.Message"), Lang.Text("Common.Dialog.Warning"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
		{
			ModBase.DeleteDirectory(ModBase.exePath + "PCL\\Pictures");
			BackgroundRefresh(isHint: false, refresh: true);
			HintService.Hint(Lang.Text("Setup.Ui.Background.Clear.Success"), HintType.Success);
		}
	}

	public static void BackgroundRefresh(bool isHint, bool refresh)
	{
		try
		{
			Directory.CreateDirectory(ModBase.exePath + "PCL\\Pictures\\");
			List<string> list = (from file in ModBase.EnumerateFiles(ModBase.exePath + "PCL\\Pictures\\")
				where !file.Extension.Equals(".ini", StringComparison.OrdinalIgnoreCase) && !file.Extension.Equals(".db", StringComparison.OrdinalIgnoreCase)
				select file.FullName).ToList();
			EventHandler<ExceptionRoutedEventArgs> value = (object? sender, ExceptionRoutedEventArgs e) =>
			{
				Exception errorException = e.ErrorException;
				string text2 = ModMain.frmMain.VideoBack.Source.ToString();
				if ((object)ModMain.frmMain.VideoBack.Source != null)
				{
					ModVideoBack.VideoStop();
					if (errorException.Message.Contains("0xC00D109B"))
					{
						ModBase.Log("刷新背景内容失败，该视频文件可能并非 H.264（AVC）格式。\n你可以尝试使用视频转码工具打开视频文件并设定目标格式为 H.264（AVC），然后转码该视频。\n文件：" + text2, ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.BackgroundVideoUnsupported"));
					}
					else
					{
						ModBase.Log(errorException, "刷新背景内容失败（" + text2 + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
					}
				}
			};
			ModMain.frmMain.VideoBack.MediaFailed -= value;
			ModVideoBack.GamingStateChanged -= ModVideoBack.OnGamingStateChanged;
			ModVideoBack.ForcePlayChanged -= ModVideoBack.OnForcePlayChanged;
			ModVideoBack.GamingStateChanged += ModVideoBack.OnGamingStateChanged;
			ModVideoBack.ForcePlayChanged += ModVideoBack.OnForcePlayChanged;
			if (!Config.Preference.Background.AutoPauseVideo)
			{
				ModVideoBack.ForcePlay = true;
			}
			if (list.Count == 0)
			{
				if (refresh)
				{
					if (ModMain.frmMain.ImgBack.Visibility == Visibility.Collapsed)
					{
						if (isHint)
						{
							HintService.Hint(Lang.Text("Setup.Ui.Background.NoAvailableContent"), HintType.Error);
						}
					}
					else
					{
						ModMain.frmMain.ImgBack.Visibility = Visibility.Collapsed;
						if (isHint)
						{
							HintService.Hint(Lang.Text("Setup.Ui.Background.Cleared"), HintType.Success);
						}
					}
				}
				if (ModMain.frmSetupUI != null)
				{
					ModMain.frmSetupUI.BackgroundRefreshUI(show: false, 0);
				}
				return;
			}
			if (refresh)
			{
				string text = RandomUtils.PickRandom<string>((ICollection<string>)list);
				try
				{
					ModMain.frmMain.ImgBack.Background = null;
					ModVideoBack.VideoStop();
					ModBase.Log("[UI] 加载背景内容：" + text);
					ModMain.frmMain.ImgBack.Background = (ImageBrush)new MyBitmap(text);
					_ = Config.Preference.Background.WallpaperSuitMode;
					ModMain.frmMain.ImgBack.Visibility = Visibility.Visible;
					if (isHint)
					{
						HintService.Hint(Lang.Text("Setup.Ui.Background.Refresh.Success", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success, log: false);
					}
				}
				catch (Exception ex)
				{
					try
					{
						ModMain.frmMain.VideoBack.MediaFailed += value;
						ModBase.Log(ex, "[UI] 加载背景图片失败" + text);
						if (ModBase.modeDebug)
						{
							HintService.Hint(Lang.Text("Setup.Ui.Background.ImageLoadFailed", new object[1] { text }));
						}
						ModMain.frmMain.ImgBack.Visibility = Visibility.Visible;
						ModMain.frmMain.VideoBack.Source = new Uri(text, UriKind.Absolute);
						ModVideoBack.VideoPlay();
						if (isHint)
						{
							HintService.Hint(Lang.Text("Setup.Ui.Background.Refresh.Success", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success, log: false);
						}
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "播放背景内容时出现未知错误：");
					}
				}
			}
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.BackgroundRefreshUI(show: true, list.Count);
			}
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, "刷新背景内容时出现未知错误", ModBase.LogLevel.Feedback, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
		}
	}

	private void BtnLogoChange_Click(object sender, MouseButtonEventArgs e)
	{
		string text = SystemDialogs.SelectFile(Lang.Text("Setup.Ui.ImageFile.Filter"), Lang.Text("Setup.Ui.ImageFile.SelectTitle"), (string)null);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			File.Delete(ModBase.exePath + "PCL\\Logo.png");
			ModBase.CopyFile(text, ModBase.exePath + "PCL\\Logo.png");
			ModMain.frmMain.ImageTitleLogo.Source = null;
			ModMain.frmMain.ImageTitleLogo.Source = ModBase.exePath + "PCL\\Logo.png";
		}
		catch (Exception ex)
		{
			if (ex.Message.Contains("参数无效"))
			{
				ModBase.Log("改变标题栏图片失败，该图片文件可能并非标准格式。\n你可以尝试使用画图打开该文件并重新保存，这会让图片变为标准格式。", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.TitleImageInvalidFormat"));
			}
			else
			{
				ModBase.Log(ex, "设置标题栏图片失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
			}
			ModMain.frmMain.ImageTitleLogo.Source = null;
		}
	}

	private void RadioLogoType3_Check(object sender, ModBase.RouteEventArgs e)
	{
		if (ModAnimation.AniControlEnabled != 0 || !e.raiseByMouse)
		{
			return;
		}
		while (true)
		{
			if (File.Exists(ModBase.exePath + "PCL\\Logo.png"))
			{
				try
				{
					ModMain.frmMain.ImageTitleLogo.Source = null;
					ModMain.frmMain.ImageTitleLogo.Source = ModBase.exePath + "PCL\\Logo.png";
					return;
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("参数无效"))
					{
						ModBase.Log("调整标题栏图片失败，该图片文件可能并非标准格式。\n你可以尝试使用画图打开该文件并重新保存，这会让图片变为标准格式。", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.TitleImageResizeInvalidFormat"));
					}
					else
					{
						ModBase.Log(ex, "调整标题栏图片失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
					}
					ModMain.frmMain.ImageTitleLogo.Source = null;
					e.handled = true;
					try
					{
						File.Delete(ModBase.exePath + "PCL\\Logo.png");
						return;
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "清理错误的标题栏图片失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
						return;
					}
				}
			}
			string text = SystemDialogs.SelectFile(Lang.Text("Setup.Ui.ImageFile.Filter"), Lang.Text("Setup.Ui.ImageFile.SelectTitle"), (string)null);
			if (string.IsNullOrEmpty(text))
			{
				break;
			}
			try
			{
				File.Delete(ModBase.exePath + "PCL\\Logo.png");
				ModBase.CopyFile(text, ModBase.exePath + "PCL\\Logo.png");
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "复制标题栏图片失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
				return;
			}
		}
		ModMain.frmMain.ImageTitleLogo.Source = null;
		e.handled = true;
	}

	private void BtnLogoDelete_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			File.Delete(ModBase.exePath + "PCL\\Logo.png");
			RadioLogoType1.SetChecked(value: true, user: true);
			HintService.Hint(Lang.Text("Setup.Ui.Logo.Clear.Success"), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "清空标题栏图片失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
		}
	}

	private void BtnMusicOpen_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenExplorer(ModBase.exePath + "PCL\\Musics\\");
	}

	private void BtnMusicRefresh_Click(object sender, MouseButtonEventArgs e)
	{
		ModMusic.MusicRefreshPlay(showHint: true);
	}

	public void MusicRefreshUI()
	{
		if (PanBackgroundOpacity != null)
		{
			if (ModMusic.musicAllList.Any())
			{
				PanMusicVolume.Visibility = Visibility.Visible;
				PanMusicDetail.Visibility = Visibility.Visible;
				BtnMusicClear.Visibility = Visibility.Visible;
				CardMusic.Title = Lang.Text("Setup.Ui.Music.TitleWithCount", new object[1] { ModBase.EnumerateFiles(ModBase.exePath + "PCL\\Musics\\").Count() });
			}
			else
			{
				PanMusicVolume.Visibility = Visibility.Collapsed;
				PanMusicDetail.Visibility = Visibility.Collapsed;
				BtnMusicClear.Visibility = Visibility.Collapsed;
				CardMusic.Title = Lang.Text("Setup.Ui.Music.Title");
			}
			CardMusic.TriggerForceResize();
		}
	}

	private void BtnMusicClear_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModMain.MyMsgBox(Lang.Text("Setup.Ui.Music.Clear.Confirm.Message"), Lang.Text("Common.Dialog.Warning"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
		{
			return;
		}
		ModBase.RunInThread(() =>
		{
			HintService.Hint(Lang.Text("Setup.Ui.Music.Deleting"));
			ModMusic.musicNAudio = null;
			ModMusic.musicWaitingList = new List<string>();
			ModMusic.musicAllList = new List<string>();
			Thread.Sleep(200);
			try
			{
				ModBase.DeleteDirectory(ModBase.exePath + "PCL\\Musics");
				HintService.Hint(Lang.Text("Setup.Ui.Music.Delete.Success"), HintType.Success);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "删除背景音乐失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
			}
			try
			{
				Directory.CreateDirectory(ModBase.exePath + "PCL\\Musics");
				ModBase.RunInUi(() =>
				{
					ModMusic.MusicRefreshPlay(showHint: false);
				});
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "重建背景音乐文件夹失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
			}
		});
	}

	private void CheckMusicStart_Change(object sender, bool user)
	{
		CheckBoxChange(sender, user);
		if (ModAnimation.AniControlEnabled == 0 && CheckMusicStart.Checked == true)
		{
			CheckMusicStop.Checked = false;
		}
	}

	private void CheckMusicStop_Change()
	{
		if (ModAnimation.AniControlEnabled == 0 && CheckMusicStop.Checked == true)
		{
			CheckMusicStart.Checked = false;
		}
	}

	private void BtnCustomRefresh_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.frmLaunchRight.ForceRefresh();
		HintService.Hint(Lang.Text("Setup.Ui.Homepage.Refresh.Success"), HintType.Success);
	}

	private void BtnCustomTutorial_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenWebsite("https://docs.pclc.cc/ce/customization/xaml-format");
	}

	private void ThemeColor_Change(object senderRaw, SelectionChangedEventArgs e)
	{
		MyComboBox myComboBox = (MyComboBox)senderRaw;
		SetByTag(myComboBox.Tag?.ToString(), myComboBox.SelectedIndex);
		ThemeManager.ThemeRefresh();
	}

	private void BtnLauncherDonate_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenWebsite("https://afdian.com/a/LTCat");
	}

	private void SliderLoad()
	{
		SliderMusicVolume.getHintText = (Func<object, object>)((object v) => Lang.Number<double>(Math.Ceiling(Convert.ToDouble(v) * 0.1) / 100.0, "P0"));
		SliderLauncherOpacity.getHintText = (Func<object, object>)((object v) => Lang.Number<double>(Math.Round(40.0 + Convert.ToDouble(v) * 0.1) / 100.0, "P0"));
		SliderBackgroundOpacity.getHintText = (Func<object, object>)((object v) => Lang.Number<double>(Math.Round(Convert.ToDouble(v) * 0.1) / 100.0, "P0"));
		SliderBackgroundBlur.getHintText = (Func<object, object>)((object v) => Lang.Text("Setup.Ui.Slider.Pixel", new object[1] { Lang.Number<double>(Convert.ToDouble(v), "N0") }));
		SliderBlurValue.getHintText = (Func<object, object>)((object v) => Lang.Text("Setup.Ui.Slider.Pixel", new object[1] { Lang.Number<double>(Convert.ToDouble(v), "N0") }));
		SliderBlurSamplingRate.getHintText = (Func<object, object>)((object v) => Lang.Number<double>(Convert.ToDouble(v) / 100.0, "P0"));
	}

	private void CheckMusicStart_OnChange(object sender, bool user)
	{
		CheckBoxChange(sender, user);
		CheckMusicStart_Change(sender, user);
	}

	private void CheckMusicStop_OnChange(object sender, bool user)
	{
		CheckBoxChange(sender, user);
		CheckMusicStop_Change();
	}

	public static void HiddenRefresh()
	{
		if (ModMain.frmMain.PanTitleSelect == null || !ModMain.frmMain.PanTitleSelect.IsLoaded)
		{
			return;
		}
		try
		{
			Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
			if (!HiddenForceShow && hide.PageDownload && hide.PageSetup && hide.PageTools && hide.ToolsGameLink)
			{
				ModMain.frmMain.PanTitleSelect.Visibility = Visibility.Collapsed;
			}
			else
			{
				ModMain.frmMain.PanTitleSelect.Visibility = Visibility.Visible;
				ModMain.frmMain.BtnTitleSelect1.Visibility = ((!HiddenForceShow && hide.PageDownload) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmMain.BtnTitleSelect2.Visibility = ((!HiddenForceShow && hide.PageSetup) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmMain.BtnTitleSelect3.Visibility = ((!HiddenForceShow && hide.PageTools) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmMain.BtnTitleSelect4.Visibility = ((!HiddenForceShow && hide.ToolsGameLink) ? Visibility.Collapsed : Visibility.Visible);
			}
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.CardSwitch.Visibility = ((!HiddenForceShow && hide.FunctionHidden) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupUI.CardSwitch.Title = (HiddenForceShow ? Lang.Text("Setup.Ui.FeatureHide.TitleTemporarilyDisabled") : Lang.Text("Setup.Ui.FeatureHide.Title"));
			}
			if (ModMain.frmSetupLeft != null)
			{
				ModMain.frmSetupLeft.ItemLaunch.Visibility = ((!HiddenForceShow && hide.SetupLaunch) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemUI.Visibility = ((!HiddenForceShow && hide.SetupUi) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemLauncherLanguage.Visibility = ((!HiddenForceShow && hide.SetupLauncherLanguage) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemGameManage.Visibility = ((!HiddenForceShow && hide.SetupGameManage) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemLauncherMisc.Visibility = ((!HiddenForceShow && hide.SetupLauncherMisc) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemJava.Visibility = ((!HiddenForceShow && hide.SetupJava) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemUpdate.Visibility = ((!HiddenForceShow && hide.SetupUpdate) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemAbout.Visibility = ((!HiddenForceShow && hide.SetupAbout) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemFeedback.Visibility = ((!HiddenForceShow && hide.SetupFeedback) ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupLeft.ItemLog.Visibility = ((!HiddenForceShow && hide.SetupLog) ? Visibility.Collapsed : Visibility.Visible);
				(TextBlock, bool)[] array = new (TextBlock, bool)[3]
				{
					(ModMain.frmSetupLeft.TextGameCategory, !hide.SetupLaunch || !hide.SetupJava || !hide.SetupGameManage),
					(ModMain.frmSetupLeft.TextLauncherCategory, !hide.SetupUi || !hide.SetupLauncherLanguage || !hide.SetupLauncherMisc),
					(ModMain.frmSetupLeft.TextAboutCategory, !hide.SetupAbout || !hide.SetupUpdate || !hide.SetupFeedback || !hide.SetupLog)
				};
				for (int i = 0; i < array.Length; i++)
				{
					(TextBlock, bool) tuple = array[i];
					bool flag = tuple.Item2 || HiddenForceShow;
					tuple.Item1.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
					if (flag)
					{
						tuple.Item1.Opacity = 0.6;
					}
				}
				int num = 0;
				if (!hide.SetupLaunch)
				{
					num++;
				}
				if (!hide.SetupUi)
				{
					num++;
				}
				if (!hide.SetupLauncherLanguage)
				{
					num++;
				}
				if (!hide.SetupGameManage)
				{
					num++;
				}
				if (!hide.SetupLauncherMisc)
				{
					num++;
				}
				if (!hide.SetupJava)
				{
					num++;
				}
				if (!hide.SetupUpdate)
				{
					num++;
				}
				if (!hide.SetupAbout)
				{
					num++;
				}
				if (!hide.SetupFeedback)
				{
					num++;
				}
				if (!hide.SetupLog)
				{
					num++;
				}
				ModMain.frmSetupLeft.PanItem.Visibility = ((num < 2 && !HiddenForceShow) ? Visibility.Collapsed : Visibility.Visible);
			}
			if (ModMain.frmToolsLeft != null)
			{
				ModMain.frmToolsLeft.ItemTest.Visibility = ((!HiddenForceShow && hide.ToolsTest) ? Visibility.Collapsed : Visibility.Visible);
				bool flag3 = (!HiddenForceShow && !hide.ToolsTest) || HiddenForceShow;
				ModMain.frmToolsLeft.TextToolsCategory.Visibility = ((!flag3) ? Visibility.Collapsed : Visibility.Visible);
				if (flag3)
				{
					ModMain.frmToolsLeft.TextToolsCategory.Opacity = 0.6;
				}
				int num2 = 0;
				if (!hide.ToolsTest)
				{
					num2++;
				}
				ModMain.frmToolsLeft.PanItem.Visibility = Visibility.Visible;
			}
			if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect)
			{
				ModMain.frmSelectRight.BtnEmptyDownload_Loaded();
			}
			if (ModMain.frmMain.pageCurrent == FormMain.PageType.Launch)
			{
				ModMain.frmLaunchLeft.RefreshButtonsUI();
			}
			if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup && ModMain.frmInstanceModDisabled != null)
			{
				ModMain.frmInstanceModDisabled.BtnDownload_Loaded();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新功能隐藏项目失败", ModBase.LogLevel.Feedback, null, Lang.Text("Setup.Ui.Error.OperationFailed"));
		}
	}

	private void HiddenSetupMain()
	{
		bool value = CheckHiddenPageSetup.Checked.Value;
		CheckHiddenSetupLaunch.Checked = value;
		CheckHiddenSetupUI.Checked = value;
		CheckHiddenSetupLauncherLanguage.Checked = value;
		CheckHiddenSetupGameManage.Checked = value;
		CheckHiddenLauncherMisc.Checked = value;
		CheckHiddenSetupJava.Checked = value;
		CheckHiddenSetupUpdate.Checked = value;
		CheckHiddenSetupGameLink.Checked = value;
		CheckHiddenSetupAbout.Checked = value;
		CheckHiddenSetupFeedback.Checked = value;
		CheckHiddenSetupLog.Checked = value;
	}

	private void HiddenSetupMain(object sender, bool user)
	{
		if (user)
		{
			bool value = CheckHiddenPageSetup.Checked.Value;
			CheckHiddenSetupLaunch.Checked = value;
			CheckHiddenSetupUI.Checked = value;
			CheckHiddenSetupLauncherLanguage.Checked = value;
			CheckHiddenSetupGameManage.Checked = value;
			CheckHiddenLauncherMisc.Checked = value;
			CheckHiddenSetupJava.Checked = value;
			CheckHiddenSetupUpdate.Checked = value;
			CheckHiddenSetupGameLink.Checked = value;
			CheckHiddenSetupAbout.Checked = value;
			CheckHiddenSetupFeedback.Checked = value;
			CheckHiddenSetupLog.Checked = value;
		}
	}

	private void HiddenSetupSub(object sender, bool user)
	{
		if (user)
		{
			Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
			bool value = hide.SetupLaunch && hide.SetupUi && hide.SetupLauncherLanguage && hide.SetupJava && hide.SetupUpdate && hide.SetupGameLink && hide.SetupAbout && hide.SetupFeedback && hide.SetupLog && hide.SetupLauncherMisc && hide.SetupGameManage;
			CheckHiddenPageSetup.Checked = value;
		}
	}

	private void HiddenToolsMain(object sender, bool user)
	{
		if (user)
		{
			bool value = CheckHiddenPageTools.Checked.Value;
			CheckHiddenToolsGameLink.Checked = value;
			CheckHiddenToolsTest.Checked = value;
		}
	}

	private void HiddenToolsSub(object sender, bool user)
	{
		if (user)
		{
			Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
			bool value = hide.ToolsGameLink && hide.ToolsTest;
			CheckHiddenPageTools.Checked = value;
		}
	}

	private void HiddenHint(object sender, bool user)
	{
		if (ModAnimation.AniControlEnabled == 0 && sender is MyCheckBox { Checked: var flag } && flag == true)
		{
			HintService.Hint(Lang.Text("Setup.Ui.FeatureHide.TemporaryHint"));
		}
	}

}
