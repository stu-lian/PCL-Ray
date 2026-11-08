using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Minecraft;
using PCL.Core.UI;
using PCL.Core.UI.Controls;
using PCL.Core.UI.Theme;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Validate;
using PCL.Network;

namespace PCL;

public partial class FormMain : Window, IComponentConnector
{
	public enum PageType
	{
		Launch = 0,
		Download = 1,
		Tools = 3,
		Setup = 2,
		Multiplayer = 4,
		InstanceSelect = 5,
		TaskManager = 6,
		InstanceSetup = 7,
		CompDetail = 8,
		HelpDetail = 9,
		GameLog = 10,
		VersionSaves = 12
	}

	public enum PageSubType
	{
		Default = 0,
		DownloadInstall = 1,
		DownloadMod = 2,
		DownloadPack = 3,
		DownloadDataPack = 4,
		DownloadResourcePack = 5,
		DownloadShader = 6,
		DownloadWorld = 7,
		DownloadCompFavorites = 8,
		DownloadClient = 9,
		DownloadOptiFine = 10,
		DownloadForge = 11,
		DownloadNeoForge = 12,
		DownloadCleanroom = 13,
		DownloadFabric = 14,
		DownloadLiteLoader = 16,
		DownloadLabyMod = 17,
		DownloadLegacyFabric = 18,
		ToolsMapViewer = 19,
		ToolsModTranslate = 20,
		ToolsLogAnalyse = 21,
		ToolsColorCode = 22,
		ToolsHeadGenerator = 23,
		ToolsSummon = 24,
		ToolsShield = 25,
		ToolsFirework = 26,
		ToolsBeacon = 27,
		ToolsArmorTrim = 28,
		ToolsTitle = 29,
		ToolsPlayerData = 30,
		ToolsHardcoreReviver = 31,
		ToolsNbtEditor = 32,
		SetupLaunch = Default,
		SetupUI = DownloadInstall,
		SetupGameManage = DownloadMod,
		SetupLink = DownloadPack,
		SetupAbout = DownloadDataPack,
		SetupLog = DownloadResourcePack,
		SetupFeedback = DownloadShader,
		SetupGameLink = DownloadWorld,
		SetupUpdate = DownloadCompFavorites,
		SetupJava = DownloadClient,
		SetupLauncherMisc = DownloadOptiFine,
		SetupLauncherLanguage = DownloadForge,
		ToolsGameLink = DownloadInstall,
		ToolsTest = DownloadPack,
		VersionOverall = Default,
		VersionSetup = DownloadInstall,
		VersionExport = DownloadMod,
		VersionWorld = DownloadPack,
		VersionScreenshot = DownloadDataPack,
		VersionMod = DownloadResourcePack,
		VersionModDisabled = DownloadShader,
		VersionResourcePack = DownloadWorld,
		VersionShader = DownloadCompFavorites,
		VersionSchematic = DownloadClient,
		VersionInstall = DownloadOptiFine,
		VersionServer = DownloadForge,
		VersionModJarInJar = DownloadNeoForge,
		VersionSavesInfo = Default,
		VersionSavesDatapack = DownloadInstall
	}

	public class PageStackData
	{
		public (ModComp.CompProject CompProject, List<string> ExpandedTitles, string TargetVersion, ModComp.CompLoaderType TargetLoader, ModComp.CompType ResourceType, string SavePath)? additional;

		public PageHelpDetail? helpPage;

		public PageType page;

		public override bool Equals(object other)
		{
			if (other == null)
			{
				return false;
			}
			if (other is PageStackData)
			{
				PageStackData pageStackData = (PageStackData)other;
				if (page != pageStackData.page)
				{
					return false;
				}
				if (helpPage != null || pageStackData.helpPage != null)
				{
					return helpPage == pageStackData.helpPage;
				}
				(ModComp.CompProject, List<string>, string, ModComp.CompLoaderType, ModComp.CompType, string)? tuple = additional;
				if (!tuple.HasValue)
				{
					tuple = pageStackData.additional;
					return !tuple.HasValue;
				}
				tuple = pageStackData.additional;
				if (tuple.HasValue)
				{
					return additional.Equals(pageStackData.additional);
				}
				return false;
			}
			if (other is int num)
			{
				if (page == (PageType)num)
				{
					return false;
				}
				(ModComp.CompProject, List<string>, string, ModComp.CompLoaderType, ModComp.CompType, string)? tuple = additional;
				return !tuple.HasValue;
			}
			return false;
		}

		public static bool operator ==(PageStackData left, PageStackData right)
		{
			return EqualityComparer<PageStackData>.Default.Equals(left, right);
		}

		public static bool operator !=(PageStackData left, PageStackData right)
		{
			return !(left == right);
		}

		public static implicit operator PageStackData(PageType value)
		{
			return new PageStackData
			{
				page = value
			};
		}

		public static implicit operator PageType(PageStackData value)
		{
			return value.page;
		}
	}

	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static MsgBoxHandler _003C0_003E__MsgBoxWrapper_OnShow;

		public static HintHandler _003C1_003E__HintWrapper_OnShow;

		public static Action _003C2_003E__TryClearTaskTemp;
	}

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action _003C_003E9__2_0;

		public static ColorModeChangedEvent _003C_003E9__5_0;

		public static ColorThemeChangedEvent _003C_003E9__5_1;

		public static ThreadStart _003C_003E9__6_0;

		public static Action _003C_003E9__6_5;

		public static Action _003C_003E9__14_0;

		public static Action _003C_003E9__34_1;

		public static Action _003C_003E9__34_5;

		public static Action _003C_003E9__34_6;

		public static Action _003C_003E9__34_7;

		public static Action _003C_003E9__34_8;

		public static Action _003C_003E9__71_0;

		internal void _003CShowUpdateLog_003Eb__2_0()
		{
			string text = ModBase.pathTemp + "CEUpdateLog.md";
			string caption = ((!File.Exists(text)) ? Lang.Text("Main.UpdateLog.Empty") : ModBase.ReadFile(text));
			if (ModMain.MyMsgBoxMarkdown(caption, Lang.Text("Main.UpdateLog.Title", new object[2]
			{
				"Publish",
				ModBase.versionBaseName
			}), Lang.Text("Common.Action.Confirm"), Lang.Text("Main.UpdateLog.FullChangelog")) == 2)
			{
				ModBase.OpenWebsite("https://github.com/stu-lian/PCL-Ray/releases");
			}
		}

		internal void _003C_002Ector_003Eb__5_0(bool _, ColorTheme _theme)
		{
			ThemeManager.ThemeRefresh();
		}

		internal void _003C_002Ector_003Eb__5_1(ColorTheme theme)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Expected I4, but got Unknown
			ThemeManager.ThemeRefresh((int)theme);
		}

		internal void _003CFormMain_Loaded_003Eb__6_0()
		{
			ModAnimation.AniControlEnabled--;
		}

		internal void _003CFormMain_Loaded_003Eb__6_5()
		{
			ModBase.OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW");
		}

		internal void _003CEndProgram_003Eb__14_0()
		{
			ModBase.Log("[System] 正在强行停止任务");
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				item.Abort();
			}
		}

		internal void _003CFileDrag_003Eb__34_1()
		{
			Config.Preference.Homepage.Type = 1;
			ModMain.frmLaunchRight.ForceRefresh();
			HintService.Hint(Lang.Text("Main.FileDrag.HomepageLoaded"), HintType.Success);
		}

		internal void _003CFileDrag_003Eb__34_5()
		{
			ModMain.frmInstanceSaves.Reload();
		}

		internal void _003CFileDrag_003Eb__34_6()
		{
			ModMain.frmInstanceResourcePack.ReloadCompFileList();
		}

		internal void _003CFileDrag_003Eb__34_7()
		{
			ModMain.frmInstanceShader.ReloadCompFileList();
		}

		internal void _003CFileDrag_003Eb__34_8()
		{
			ModMain.frmInstanceSchematic.ReloadCompFileList();
		}

		internal void _003CDragStop_003Eb__71_0()
		{
			if (ModMain.dragControl != null)
			{
				MySlider? dragControl = ModMain.dragControl;
				ModMain.dragControl = null;
				dragControl.DragStop();
			}
		}
	}

	public MouseEventArgs lastMouseArg;

	private bool isWindowLoadFinished;

	private readonly DragHelper _helper = new DragHelper();

	private bool canResize = true;

	private static bool isLogShown;

	public bool isSizeSaveable;

	private IDataObject _HandleDrag_PrevData;

	private DragDropEffects _HandleDrag_PrevEffects;

	public bool isSystemTimeChanged;

	public PageStackData pageCurrent = PageType.Launch;

	public PageStackData pageLast = PageType.Launch;

	public List<PageStackData> pageStack = new List<PageStackData>();

	public MyPageLeft pageLeft;

	public MyPageRight pageRight;

	private bool isChangingPage;


	public bool Hidden
	{
		get
		{
			return field;
		}
		set
		{
			if (field == value)
			{
				return;
			}
			field = value;
			if (value)
			{
				Left -= 10000.0;
				ShowInTaskbar = false;
				Visibility = Visibility.Hidden;
				ModBase.Log("[System] 窗口已隐藏，位置：(" + Left + "," + Top + ")");
			}
			else
			{
				if (Left < -2000.0)
				{
					Left += 10000.0;
				}
				ShowWindowToTop();
			}
		}
	}

	public PageSubType PageCurrentSub
	{
		get
		{
			switch (pageCurrent.page)
			{
			case PageType.Download:
				if (ModMain.frmDownloadLeft == null)
				{
					ModMain.frmDownloadLeft = new PageDownloadLeft();
				}
				return ModMain.frmDownloadLeft.pageID;
			case PageType.Setup:
				if (ModMain.frmSetupLeft == null)
				{
					ModMain.frmSetupLeft = new PageSetupLeft();
				}
				return ModMain.frmSetupLeft.pageID;
			case PageType.Multiplayer:
				return PageSubType.Default;
			case PageType.InstanceSetup:
				if (ModMain.frmInstanceLeft == null)
				{
					ModMain.frmInstanceLeft = new PageInstanceLeft();
				}
				return ModMain.frmInstanceLeft.pageID;
			default:
				return PageSubType.Default;
			}
		}
	}

	private void FormMain_MouseMove(object sender, MouseEventArgs e)
	{
		lastMouseArg = e;
	}

	private void ShowUpdateLog()
	{
		ModBase.RunInNewThread(() =>
		{
			string text = ModBase.pathTemp + "CEUpdateLog.md";
			string caption = ((!File.Exists(text)) ? Lang.Text("Main.UpdateLog.Empty") : ModBase.ReadFile(text));
			if (ModMain.MyMsgBoxMarkdown(caption, Lang.Text("Main.UpdateLog.Title", new object[2]
			{
				"Publish",
				ModBase.versionBaseName
			}), Lang.Text("Common.Action.Confirm"), Lang.Text("Main.UpdateLog.FullChangelog")) == 2)
			{
				ModBase.OpenWebsite("https://github.com/stu-lian/PCL-Ray/releases");
			}
		}, "UpdateLog Output");
	}

	public FormMain()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected Obj, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected Obj, but got Unknown
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected Obj, but got Unknown
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Expected Obj, but got Unknown
		ModBase.applicationStartTick = TimeUtils.GetTimeTick();
		ColorModeChangedEvent val = _003C_003Ec._003C_003E9__5_0;
		if (val == null)
		{
			ColorModeChangedEvent val2 = (bool _, ColorTheme _) =>
			{
				ThemeManager.ThemeRefresh();
			};
			_003C_003Ec._003C_003E9__5_0 = val2;
			val = val2;
		}
		ThemeService.ColorModeChanged += val;
		ColorThemeChangedEvent val3 = _003C_003Ec._003C_003E9__5_1;
		if (val3 == null)
		{
			ColorThemeChangedEvent val4 = (ColorTheme theme) =>
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Expected I4, but got Unknown
				ThemeManager.ThemeRefresh((int)theme);
			};
			_003C_003Ec._003C_003E9__5_1 = val4;
			val3 = val4;
		}
		ThemeService.ColorThemeChanged += val3;
		ModMain.frmMain = this;
		ModMain.frmLaunchLeft = new PageLaunchLeft();
		ModMain.frmLaunchRight = new PageLaunchRight();
		int lastVersion = States.System.LastVersion;
		if (lastVersion < ModBase.versionCode)
		{
			if (lastVersion <= 511 && !Config.System.TelemetryConfig.IsDefault((object)null) && Config.System.Telemetry)
			{
				Config.System.TelemetryConfig.Reset((object)null);
				ModBase.Log("[Start] 遥测策略变更：由旧版本升级到含新版遥测的版本，已重置遥测设置");
			}
			UpgradeSub(lastVersion);
		}
		else if (lastVersion > ModBase.versionCode)
		{
			DowngradeSub(lastVersion);
		}
		_ = Config.Preference.Theme.ThemeSelected;
		AddHandler(DragDrop.DragEnterEvent, new DragEventHandler(HandleDrag), handledEventsToo: true);
		AddHandler(DragDrop.DragOverEvent, new DragEventHandler(HandleDrag), handledEventsToo: true);
		MsgBoxHandler val5 = _003C_003EO._003C0_003E__MsgBoxWrapper_OnShow;
		if (val5 == null)
		{
			MsgBoxHandler val6 = ModMain.MsgBoxWrapper_OnShow;
			_003C_003EO._003C0_003E__MsgBoxWrapper_OnShow = val6;
			val5 = val6;
		}
		MsgBoxWrapper.OnShow += val5;
		HintHandler val7 = _003C_003EO._003C1_003E__HintWrapper_OnShow;
		if (val7 == null)
		{
			HintHandler val8 = HintService.HintWrapper_OnShow;
			_003C_003EO._003C1_003E__HintWrapper_OnShow = val8;
			val7 = val8;
		}
		HintWrapper.OnShow += val7;
		InitializeComponent();
		Opacity = 0.0;
		try
		{
			Height = States.UI.WindowHeight;
			Width = States.UI.WindowWidth;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "读取窗口默认大小失败", ModBase.LogLevel.Hint, null, Lang.Text("Main.Error.OperationFailed"));
			Height = MinHeight + 100.0;
			Width = MinWidth + 100.0;
		}
		if (ProcessInterop.IsAdmin())
		{
			ModBase.Log("[Start] PCL 当前正以管理员权限运行");
			SourceInitialized += (object? _, EventArgs _) =>
			{
				WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
				_helper.HwndSource = HwndSource.FromHwnd(windowInteropHelper.Handle);
				_helper.AddHook();
			};
			Closing += (object? _, CancelEventArgs _) =>
			{
				_helper.RemoveHook();
			};
			_helper.DragDrop += (object? _, EventArgs _) =>
			{
				FileDrag(_helper.DropFilePaths);
			};
		}
		if (ModMain.frmLaunchLeft.Parent != null)
		{
			((DependencyObject)ModMain.frmLaunchLeft).SetValue(ContentPresenter.ContentProperty, (object)null);
		}
		if (ModMain.frmLaunchRight.Parent != null)
		{
			((DependencyObject)ModMain.frmLaunchRight).SetValue(ContentPresenter.ContentProperty, (object)null);
		}
		PanMainLeft.Child = ModMain.frmLaunchLeft;
		pageLeft = ModMain.frmLaunchLeft;
		PanMainRight.Child = ModMain.frmLaunchRight;
		pageRight = ModMain.frmLaunchRight;
		ModMain.frmLaunchRight.PageState = MyPageRight.PageStates.ContentStay;
		if (ModBase.modeDebug)
		{
			HintService.Hint(Lang.Text("Main.DebugMode.Hint"));
		}
		ModFolder.mcFolderListLoader.Start(0);
		ModBase.Log("[Start] 第二阶段加载用时：" + (TimeUtils.GetTimeTick() - ModBase.applicationStartTick) + " ms");
		Lifecycle.When((LifecycleState)5, (Action)FormMain_Loaded);
	}

	private void FormMain_Loaded()
	{
		FormMain_SizeChanged();
		ModBase.applicationStartTick = TimeUtils.GetTimeTick();
		ModBase.frmHandle = new WindowInteropHelper(this).Handle;
		PageSetupUI.BackgroundRefresh(isHint: false, refresh: true);
		ModMusic.MusicRefreshPlay(showHint: false, isFirstLoad: true);
		BtnExtraUpdateRestart.showCheck = BtnExtraUpdateRestart_ShowCheck;
		BtnExtraDownload.showCheck = BtnExtraDownload_ShowCheck;
		BtnExtraBack.showCheck = BtnExtraBack_ShowCheck;
		BtnExtraApril.showCheck = BtnExtraApril_ShowCheck;
		BtnExtraShutdown.showCheck = BtnExtraShutdown_ShowCheck;
		BtnExtraLog.showCheck = BtnExtraLog_ShowCheck;
		BtnExtraApril.ShowRefresh();
		if (!Config.Preference.LockWindowSize)
		{
			AddResizer();
		}
		else
		{
			RemoveResizer();
		}
		if (RandomUtils.NextInt(1, 1000) == 233)
		{
			ShapeTitleLogo.Data = (Geometry)new GeometryConverter().ConvertFromString("M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M83,6.5 a10,11.5 180 1 0 0,18 M48,2.5 v24.5 h13.5");
		}
		ThemeManager.ThemeRefresh();
		ModSetup.ApplyAll();
		Lifecycle.CurrentApplication.Resources["BlurSamplingRate"] = (double)Config.Preference.Blur.SamplingRate * 0.01;
		Lifecycle.CurrentApplication.Resources["BlurType"] = Config.Preference.Blur.KernelType;
		if (Config.Preference.Blur.IsEnabled)
		{
			Lifecycle.CurrentApplication.Resources["BlurRadius"] = (double)Config.Preference.Blur.Radius * 1.0;
		}
		else
		{
			Lifecycle.CurrentApplication.Resources["BlurRadius"] = 0.0;
		}
		Topmost = false;
		if (ModMain.frmStart != null)
		{
			ModMain.frmStart.Close(new TimeSpan(0, 0, 0, 0, (int)Math.Round(400.0 / ModAnimation.aniSpeed)));
		}
		isSizeSaveable = true;
		ShowWindowToTop();
		((HwndSource)PresentationSource.FromVisual(this)).AddHook(WndProc);
		ModAnimation.AniStart(new ModAnimation.AniData[5]
		{
			ModAnimation.AaCode(() =>
			{
				ModAnimation.AniControlEnabled--;
			}, 50),
			ModAnimation.AaOpacity(this, (double)Config.Preference.Theme.WindowOpacity / 1000.0 + 0.4, 250, 100),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformPos.Y += (double)i;
			}, 0.0 - TransformPos.Y, 600, 100, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformRotate.Angle += (double)i;
			}, 0.0 - TransformRotate.Angle, 500, 100, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaCode(() =>
			{
				RenderTransform = null;
				isWindowLoadFinished = true;
				ModBase.Log($"[System] DPI：{ModBase.dpi}，系统版本：{Environment.OSVersion.VersionString}，PCL 位置：{Basics.ExecutablePath}");
			}, 0, after: true)
		}, "Form Show");
		ModAnimation.AniStart();
		ModMain.TimerMainStart();
		ModBase.RunInNewThread(() =>
		{
			try
			{
				if (!States.System.LauncherEula)
				{
					switch (ModMain.MyMsgBox(Lang.Text("Main.Eula.Message"), Lang.Text("Main.Eula.Title"), Lang.Text("Common.Action.Agree"), Lang.Text("Common.Action.Decline"), Lang.Text("Main.Eula.View"), isWarn: false, highLight: true, forceWait: false, null, null, () =>
					{
						ModBase.OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW");
					}))
					{
					case 1:
						States.System.LauncherEula = true;
						break;
					case 2:
						EndProgram(sendWarning: false);
						break;
					}
				}
				if (Config.System.TelemetryConfig.IsDefault((object)null))
				{
					int num = ModMain.MyMsgBox(Lang.Text("Main.Telemetry.Message"), Lang.Text("Main.Telemetry.Title"), Lang.Text("Common.Action.Agree"), Lang.Text("Common.Action.Decline"));
					Config.System.TelemetryConfig.SetValue(num == 1, (object)null, true, false);
				}
				try
				{
					ModDownload.dlClientListMojangLoader.Start(1);
					RunCountSub();
					UpdateManager.serverLoader.Start(1);
					ModBase.RunInNewThread(ModMain.TryClearTaskTemp, "TryClearTaskTemp", ThreadPriority.BelowNormal);
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "初始化加载池运行失败", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
				}
				HardwareInfo.GetHardwareInfo();
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "初始弹窗提示运行失败", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
			}
		}, "Start Loader", ThreadPriority.BelowNormal);
		ModBase.Log($"[Start] 第三阶段加载用时：{TimeUtils.GetTimeTick() - ModBase.applicationStartTick} ms");
	}

	private void RunCountSub()
	{
		States.SystemStatesGroup system = States.System;
		system.StartupCount += 1;
	}

	private void UpgradeSub(int lastVersionCode)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Invalid comparison between Unknown and I4
		ModBase.Log("[Start] 版本号从 " + lastVersionCode + " 升高到 " + ModBase.versionCode);
		States.System.LastVersion = ModBase.versionCode;
		int lastAlphaVersion = States.System.LastAlphaVersion;
		if (lastAlphaVersion < ModBase.versionCode)
		{
			States.System.LastAlphaVersion = ModBase.versionCode;
			ModBase.Log($"[Start] 最高版本号从 {lastAlphaVersion} 升高到 {ModBase.versionCode}");
		}
		if ((int)Config.Launch.GameWindowMode == 5)
		{
			Config.Launch.GameWindowMode = (GameWindowSizeMode)1;
		}
		UpdateManager.ShowCEAnnounce();
		if (lastVersionCode > 0 && lastAlphaVersion < ModBase.versionCode)
		{
			ShowUpdateLog();
			if (lastVersionCode < 521 && Config.Preference.Homepage.SelectedPreset >= 3)
			{
				Config.Preference.Homepage.SelectedPreset = 0;
				ModMain.MyMsgBox(Lang.Text("Main.HomepageReset.Content"), Lang.Text("Main.HomepageReset.Title"));
			}
		}
	}

	private void DowngradeSub(int lastVersionCode)
	{
		ModBase.Log("[Start] 版本号从 " + lastVersionCode + " 降低到 " + ModBase.versionCode);
		States.System.LastVersion = ModBase.versionCode;
	}

	private nint _SizeWndProc(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg != 132)
		{
			return IntPtr.Zero;
		}
		byte[] bytes = BitConverter.GetBytes(((IntPtr)lParam).ToInt64());
		short num = BitConverter.ToInt16(bytes, 0);
		short num2 = BitConverter.ToInt16(bytes, 2);
		(int, int, int, int) windowRectangle = WindowInterop.GetWindowRectangle(hWnd);
		(int, int, int, int) tuple = WindowInterop.ToWindowBounds(windowRectangle);
		if (num < windowRectangle.Item1 || num > windowRectangle.Item3 || num2 < windowRectangle.Item2 || num2 > windowRectangle.Item4)
		{
			return IntPtr.Zero;
		}
		if (!canResize)
		{
			return new IntPtr(1);
		}
		DpiScale dpi = VisualTreeHelper.GetDpi(this);
		double num3 = 6.0 * dpi.DpiScaleX;
		double num4 = 6.0 * dpi.DpiScaleY;
		double num5 = 5.0 * dpi.DpiScaleX;
		double num6 = 5.0 * dpi.DpiScaleY;
		int num7 = num - windowRectangle.Item1;
		int num8 = num2 - windowRectangle.Item2;
		int item = tuple.Item3;
		int item2 = tuple.Item4;
		bool flag = (double)num7 >= num3 && (double)num7 <= num3 + num5;
		bool flag2 = (double)num7 <= (double)item - num3 && (double)num7 >= (double)item - num3 - num5;
		bool flag3 = (double)num8 >= num4 && (double)num8 <= num4 + num6;
		bool flag4 = (double)num8 <= (double)item2 - num4 && (double)num8 >= (double)item2 - num4 - num6;
		handled = true;
		if (flag3 & flag)
		{
			return new IntPtr(13);
		}
		if (flag3 & flag2)
		{
			return new IntPtr(14);
		}
		if (flag4 & flag)
		{
			return new IntPtr(16);
		}
		if (flag4 & flag2)
		{
			return new IntPtr(17);
		}
		if (flag)
		{
			return new IntPtr(10);
		}
		if (flag2)
		{
			return new IntPtr(11);
		}
		if (flag3)
		{
			return new IntPtr(12);
		}
		if (flag4)
		{
			return new IntPtr(15);
		}
		return new IntPtr(1);
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		if (Config.System.DisableHardwareAcceleration && PresentationSource.FromVisual(this) is HwndSource hwndSource)
		{
			hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
		}
		base.OnSourceInitialized(e);
		nint handle = new WindowInteropHelper(this).Handle;
		HwndSource hwndSource2 = HwndSource.FromHwnd(handle);
		if (hwndSource2 != null)
		{
			hwndSource2.CompositionTarget.BackgroundColor = Colors.Transparent;
			hwndSource2.AddHook(_SizeWndProc);
		}
		try
		{
			WindowInterop.ExtendFrameIntoClientArea(handle, -1);
		}
		catch (Exception ex)
		{
			LogWrapper.Error("DWM 窗口框架应用失败: " + ex.Message);
		}
	}

	private void FormMain_Closing(object sender, CancelEventArgs e)
	{
		EndProgram(sendWarning: true);
		e.Cancel = true;
	}

	public void EndProgram(bool sendWarning, bool isUpdating = false)
	{
		if (sendWarning && ModNet.HasDownloadingTask())
		{
			if (ModMain.MyMsgBox(Lang.Text("Main.Exit.HasDownloadingTask"), Lang.Text("Common.Dialog.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 1)
			{
				return;
			}
			ModBase.RunInNewThread(() =>
			{
				ModBase.Log("[System] 正在强行停止任务");
				foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
				{
					item.Abort();
				}
			}, "强行停止下载任务");
		}
		ModBase.RunInUiWait(() =>
		{
			VideoBack.Stop();
			VideoBack.Source = null;
			VideoBack.Close();
			IsHitTestVisible = false;
			if (RenderTransform == null)
			{
				TranslateTransform transformPos = new TranslateTransform(0.0, 0.0);
				RotateTransform transformRotate = new RotateTransform(0.0);
				ScaleTransform transformScale = new ScaleTransform(1.0, 1.0);
				transformScale.CenterX = Width / 2.0;
				transformScale.CenterY = Height / 2.0;
				RenderTransform = new TransformGroup
				{
					Children = new TransformCollection(new _003C_003Ez__ReadOnlyArray<Transform>(new Transform[3] { transformRotate, transformPos, transformScale }))
				};
				ModAnimation.AniStart(new ModAnimation.AniData[6]
				{
					ModAnimation.AaOpacity(this, 0.0 - Opacity, 140, 40, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaDouble((object? i) =>
					{
						transformScale.ScaleX += (double)i;
						transformScale.ScaleY += (double)i;
					}, 0.88 - transformScale.ScaleX, 180),
					ModAnimation.AaDouble((object? i) =>
					{
						transformPos.Y += (double)i;
					}, 20.0 - transformPos.Y, 180, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaDouble((object? i) =>
					{
						transformRotate.Angle += (double)i;
					}, 0.6 - transformRotate.Angle, 180, 0, new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaCode(() =>
					{
						IsHitTestVisible = false;
						Visibility = Visibility.Collapsed;
						ShowInTaskbar = false;
					}, 210),
					ModAnimation.AaCode(() =>
					{
						EndProgramForce(ModBase.ProcessReturnValues.Success, force: false, isUpdating);
					}, 230)
				}, "Form Close");
			}
			else
			{
				EndProgramForce(ModBase.ProcessReturnValues.Success, force: false, isUpdating);
			}
			ModBase.Log("[System] 收到关闭指令");
		});
	}

	public static void EndProgramForce(ModBase.ProcessReturnValues returnCode = ModBase.ProcessReturnValues.Success, bool force = true, bool isUpdating = false)
	{
		ModBase.isProgramEnded = true;
		ModAnimation.AniControlEnabled++;
		if (UpdateManager.isUpdateWaitingRestart && !isUpdating)
		{
			UpdateManager.UpdateRestart(triggerRestartAndByEnd: false, triggerRestart: false);
		}
		if (returnCode == ModBase.ProcessReturnValues.Exception)
		{
			if (!isLogShown)
			{
				ModBase.FeedbackInfo();
				ModBase.Log("请在 https://github.com/stu-lian/PCL-Ray/issues 提交错误报告，以便于社区解决此问题！（这也有可能是原版 PCL 的问题）");
				isLogShown = true;
				ModBase.ShellOnly(LogWrapper.CurrentLogger.CurrentLogFiles.Last());
			}
			Thread.Sleep(500);
		}
		ModBase.Log("[System] 程序已退出，返回值：" + ModBase.GetStringFromEnum(returnCode));
		Lifecycle.Shutdown((int)returnCode, force);
	}

	private void BtnTitleClose_Click(object sender, EventArgs e)
	{
		EndProgram(sendWarning: true);
	}

	private void FormDragMove(object sender, MouseButtonEventArgs e)
	{
		if (((Grid)sender).IsMouseDirectlyOver)
		{
			DragMove();
		}
	}

	private void FormMain_SizeChanged(object? sender = null, EventArgs? e = null)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (isSizeSaveable)
		{
			States.UI.WindowHeight = Height;
			States.UI.WindowWidth = Width;
		}
		if (PanBack != null)
		{
			RectForm.Rect = new Rect(0.0, 0.0, PanBack.ActualWidth, PanBack.ActualHeight);
			double width = PanBack.ActualWidth + 0.001;
			double num = PanBack.ActualHeight + 0.001;
			PanForm.Width = width;
			PanForm.Height = num;
			PanMain.Width = width;
			if (PanTitle != null)
			{
				PanMain.Height = Math.Max(0.0, num - PanTitle.ActualHeight);
			}
			else
			{
				PanMain.Height = num;
			}
			VideoBack.Width = width;
			VideoBack.Height = num;
		}
		if (WindowState == WindowState.Maximized)
		{
			WindowState = WindowState.Normal;
		}
	}

	private void PanTitle_SizeChanged(object sender, EventArgs e)
	{
		if (PanTitleMain.ColumnDefinitions[0].ActualWidth - 30.0 <= 0.0)
		{
			PanTitleLeft.ColumnDefinitions[0].MaxWidth = 0.0;
		}
		else
		{
			PanTitleLeft.ColumnDefinitions[0].MaxWidth = PanTitleMain.ColumnDefinitions[0].ActualWidth - 30.0;
		}
	}

	private void BtnTitleMin_Click(object sender, EventArgs e)
	{
		WindowState = WindowState.Minimized;
	}

	private void BtnTitleHelp_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://www.bilibili.com/video/BV1uT4y1P7CX");
	}

	public void AddResizer()
	{
		canResize = true;
	}

	public void RemoveResizer()
	{
		canResize = false;
	}

	private void FormMain_KeyDown(object sender, KeyEventArgs e)
	{
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Invalid comparison between Unknown and I4
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Invalid comparison between Unknown and I4
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Invalid comparison between Unknown and I4
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Invalid comparison between Unknown and I4
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Invalid comparison between Unknown and I4
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Invalid comparison between Unknown and I4
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0458: Invalid comparison between Unknown and I4
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Invalid comparison between Unknown and I4
		if (e.IsRepeat)
		{
			return;
		}
		if (PanMsg.Children.Count > 0)
		{
			if ((int)e.Key == 6)
			{
				UIElement uIElement = PanMsg.Children[0];
				MyMsgInput myMsgInput = uIElement as MyMsgInput;
				Action action;
				if (myMsgInput == null)
				{
					MyMsgSelect myMsgSelect = uIElement as MyMsgSelect;
					if (myMsgSelect == null)
					{
						MyMsgText myMsgText = uIElement as MyMsgText;
						if (myMsgText == null)
						{
							MyMsgMarkdown myMsgMarkdown = uIElement as MyMsgMarkdown;
							if (myMsgMarkdown == null)
							{
								MyMsgLogin myMsgLogin = uIElement as MyMsgLogin;
								action = ((myMsgLogin == null) ? null : ((Action)(() =>
								{
									myMsgLogin.Btn1_Click(sender, null);
								})));
							}
							else
							{
								action = () =>
								{
									myMsgMarkdown.Btn1_Click(sender, null);
								};
							}
						}
						else
						{
							action = () =>
							{
								myMsgText.Btn1_Click(sender);
							};
						}
					}
					else
					{
						action = () =>
						{
							myMsgSelect.Btn1_Click(sender, null);
						};
					}
				}
				else
				{
					action = () =>
					{
						myMsgInput.Btn1_Click(sender, null);
					};
				}
				action?.Invoke();
				return;
			}
			if ((int)e.Key == 13)
			{
				UIElement uIElement2 = PanMsg.Children[0];
				MyMsgInput myMsgInput2 = uIElement2 as MyMsgInput;
				Action action;
				if (myMsgInput2 == null)
				{
					MyMsgSelect myMsgSelect2 = uIElement2 as MyMsgSelect;
					if (myMsgSelect2 == null)
					{
						MyMsgText myMsgText2 = uIElement2 as MyMsgText;
						if (myMsgText2 == null)
						{
							MyMsgMarkdown myMsgMarkdown2 = uIElement2 as MyMsgMarkdown;
							if (myMsgMarkdown2 == null)
							{
								MyMsgLogin myMsgLogin2 = uIElement2 as MyMsgLogin;
								if (myMsgLogin2 == null)
								{
									action = null;
								}
								else
								{
									action = ((myMsgLogin2.Btn3.Visibility == Visibility.Visible) ? ((Action)(() =>
									{
										myMsgLogin2.Btn3_Click(sender, null);
									})) : ((Action)(() =>
									{
										myMsgLogin2.Btn1_Click(sender, null);
									})));
								}
							}
							else if (myMsgMarkdown2.Btn3.Visibility != Visibility.Visible)
							{
								action = ((myMsgMarkdown2.Btn2.Visibility == Visibility.Visible) ? ((Action)(() =>
								{
									myMsgMarkdown2.Btn2_Click(sender, null);
								})) : ((Action)(() =>
								{
									myMsgMarkdown2.Btn1_Click(sender, null);
								})));
							}
							else
							{
								action = () =>
								{
									myMsgMarkdown2.Btn3_Click(sender, null);
								};
							}
						}
						else if (myMsgText2.Btn3.Visibility != Visibility.Visible)
						{
							action = ((myMsgText2.Btn2.Visibility == Visibility.Visible) ? ((Action)(() =>
							{
								myMsgText2.Btn2_Click(sender, null);
							})) : ((Action)(() =>
							{
								myMsgText2.Btn1_Click(sender);
							})));
						}
						else
						{
							action = () =>
							{
								myMsgText2.Btn3_Click(sender, null);
							};
						}
					}
					else
					{
						action = ((myMsgSelect2.Btn2.Visibility == Visibility.Visible) ? ((Action)(() =>
						{
							myMsgSelect2.Btn2_Click(sender, null);
						})) : ((Action)(() =>
						{
							myMsgSelect2.Btn1_Click(sender, null);
						})));
					}
				}
				else
				{
					action = ((myMsgInput2.Btn2.Visibility == Visibility.Visible) ? ((Action)(() =>
					{
						myMsgInput2.Btn2_Click(sender, null);
					})) : ((Action)(() =>
					{
						myMsgInput2.Btn1_Click(sender, null);
					})));
				}
				action?.Invoke();
				return;
			}
		}
		if ((int)e.Key == 13)
		{
			TriggerPageBack();
		}
		if ((int)e.Key == 100 && pageCurrent == PageType.InstanceSelect)
		{
			ModMain.frmSelectRight.showHidden = !ModMain.frmSelectRight.showHidden;
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
			return;
		}
		if ((int)e.Key == 101)
		{
			PageSetupUI.HiddenForceShow = !PageSetupUI.HiddenForceShow;
			if (PageSetupUI.HiddenForceShow)
			{
				HintService.Hint(Lang.Text("Main.HiddenFeature.Disabled"), HintType.Success);
			}
			else
			{
				HintService.Hint(Lang.Text("Main.HiddenFeature.Enabled"), HintType.Success);
			}
			PageSetupUI.HiddenRefresh();
			return;
		}
		if ((int)e.Key == 94)
		{
			if (pageLeft is IRefreshable)
			{
				((IRefreshable)pageLeft).Refresh();
			}
			if (pageRight is IRefreshable)
			{
				((IRefreshable)pageRight).Refresh();
			}
			return;
		}
		if ((int)e.Key == 6 && pageCurrent == PageType.Launch)
		{
			if (ModMain.isAprilEnabled && !ModMain.isAprilGiveup)
			{
				HintService.Hint(Lang.Text("Main.April.Nope"));
			}
			else
			{
				ModMain.frmLaunchLeft.LaunchButtonClick();
			}
		}
		if ((int)e.SystemKey == 120 || (int)e.SystemKey == 121)
		{
			e.Handled = true;
		}
	}

	private void FormMain_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (ModMain.frmMain.PanMsg.Children.Count <= 0 && !ModMain.WaitingMyMsgBox.Any() && (e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2))
		{
			TriggerPageBack();
		}
	}

	private void TriggerPageBack()
	{
		if (pageCurrent == PageType.Download && PageCurrentSub == PageSubType.DownloadInstall && ModMain.frmDownloadInstall.isInSelectPage)
		{
			ModMain.frmDownloadInstall.ExitSelectPage();
		}
		else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadOptiFine && ModMain.frmInstanceInstall.isInSelectPage)
		{
			ModMain.frmInstanceInstall.ExitSelectPage();
		}
		else
		{
			PageBack();
		}
	}

	private void FormMain_Activated(object sender, EventArgs e)
	{
		try
		{
			if (Config.Download.Comp.ReadClipboard)
			{
				ModComp.CompClipboard.GetClipboardResource();
			}
			if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadResourcePack)
			{
				ModMain.frmInstanceMod.ReloadCompFileList();
			}
			else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadWorld)
			{
				if (ModMain.frmInstanceResourcePack != null)
				{
					ModMain.frmInstanceResourcePack.ReloadCompFileList();
				}
			}
			else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadCompFavorites)
			{
				if (ModMain.frmInstanceShader != null)
				{
					ModMain.frmInstanceShader.ReloadCompFileList();
				}
			}
			else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadClient)
			{
				if (ModMain.frmInstanceSchematic != null)
				{
					ModMain.frmInstanceSchematic.ReloadCompFileList();
				}
			}
			else if (pageCurrent == PageType.InstanceSelect)
			{
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.RunOnUpdated, 1, "versions\\");
			}
			else if (ModMain.frmMain.pageRight is PageInstanceSavesDatapack && ModMain.frmInstanceSavesDatapack != null)
			{
				ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "切回窗口时出错", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	private void HandleDrag(object sender, DragEventArgs e)
	{
		try
		{
			if (e.Handled && e.Effects != DragDropEffects.None)
			{
				return;
			}
			e.Handled = true;
			if (e.Data == _HandleDrag_PrevData)
			{
				e.Effects = _HandleDrag_PrevEffects;
				return;
			}
			e.Effects = DragDropEffects.None;
			if (e.Data.GetDataPresent(DataFormats.Text))
			{
				string str = (string)e.Data.GetData(DataFormats.Text);
				if (str.StartsWithF("authlib-injector:yggdrasil-server:"))
				{
					e.Effects = DragDropEffects.Copy;
				}
				else if (str.StartsWithF("file:///"))
				{
					e.Effects = DragDropEffects.Copy;
				}
			}
			else if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (array != null && array.Length != 0)
				{
					e.Effects = DragDropEffects.Link;
				}
			}
			_HandleDrag_PrevData = e.Data;
			_HandleDrag_PrevEffects = e.Effects;
			ModBase.Log("[System] 设置拖放类型：" + ModBase.GetStringFromEnum(e.Effects));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "处理拖放时出错", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	private void FrmMain_Drop(object sender, DragEventArgs e)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected Obj, but got Unknown
		try
		{
			if (e.Data.GetDataPresent(DataFormats.Text))
			{
				try
				{
					string text = (string)e.Data.GetData(DataFormats.Text);
					ModBase.Log("[System] 接受文本拖拽：" + text);
					if (text.StartsWithF("authlib-injector:yggdrasil-server:"))
					{
						e.Handled = true;
						e.Effects = DragDropEffects.Copy;
						string authlibServer = WebUtility.UrlDecode(text.Substring("authlib-injector:yggdrasil-server:".Length));
						ModBase.Log("[System] Authlib 拖拽：" + authlibServer);
						if (!((AbstractValidator<string>)new HttpValidator()).Validate(authlibServer).IsValid)
						{
							HintService.Hint(Lang.Text("Main.FileDrag.AuthlibInvalid", new object[1] { authlibServer }), HintType.Error);
						}
						else if (ModMain.MyMsgBox(Lang.Text("Main.FileDrag.CreateAuthlibProfile", new object[1] { authlibServer }), Lang.Text("Main.FileDrag.CreateAuthlibProfileTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 2)
						{
							ModBase.RunInUi(() =>
							{
								PageLoginAuth.draggedAuthServer = authlibServer;
								ModMain.frmLaunchLeft.RefreshPage(anim: true, ModLaunch.McLoginType.Auth);
							});
							if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadInstall)
							{
								ModMain.frmInstanceSetup.Reload();
							}
						}
					}
					else if (text.StartsWithF("file:///"))
					{
						string item = WebUtility.UrlDecode(text).Substring("file:///".Length).Replace("/", "\\");
						e.Handled = true;
						e.Effects = DragDropEffects.Copy;
						FileDrag(new List<string> { item });
					}
					return;
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "无法接取文本拖拽事件", ModBase.LogLevel.Developer);
					return;
				}
			}
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				object data = e.Data.GetData(DataFormats.FileDrop);
				if (data == null)
				{
					HintService.Hint(Lang.Text("Main.FileDrag.ExtractFirst"), HintType.Error);
					return;
				}
				e.Handled = true;
				e.Effects = DragDropEffects.Link;
				FileDrag((IEnumerable<string>)data);
			}
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "接取拖拽事件失败", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	private void FileDrag(IEnumerable<string> filePathList)
	{
		ModBase.RunInNewThread(() =>
		{
			string text = filePathList.First();
			ModBase.Log("[System] 接受文件拖拽：" + text + (filePathList.Any() ? $" 等 {filePathList.Count()} 个文件" : ""), ModBase.LogLevel.Developer);
			if (Directory.Exists(filePathList.First()) && !File.Exists(filePathList.First()))
			{
				HintService.Hint(Lang.Text("Main.FileDrag.FileOnly"), HintType.Error);
			}
			else if (!File.Exists(filePathList.First()))
			{
				HintService.Hint(Lang.Text("Main.FileDrag.FileNotFound", new object[1] { filePathList.First() }), HintType.Error);
			}
			else
			{
				if (filePathList.Count() > 1)
				{
					string firstExtension = filePathList.First().AfterLast(".").ToLower();
					if (!filePathList.All((string f) => (f.AfterLast(".").ToLower() ?? "") == (firstExtension ?? "")) || !new ReadOnlySpan<string>(new string[8] { "jar", "litemod", "disabled", "old", "litematic", "nbt", "schematic", "schem" }).Contains(firstExtension))
					{
						HintService.Hint(Lang.Text("Main.FileDrag.SameTypeOnly"), HintType.Error);
						return;
					}
				}
				string extension = text.AfterLast(".").ToLower();
				if (extension == "xaml")
				{
					ModBase.Log("[System] 文件后缀为 XAML，作为主页加载");
					if (!File.Exists(ModBase.exePath + "PCL\\Custom.xaml") || ModMain.MyMsgBox(Lang.Text("Main.FileDrag.HomepageExists"), Lang.Text("Main.FileDrag.OverwriteTitle"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) != 2)
					{
						ModBase.CopyFile(text, ModBase.exePath + "PCL\\Custom.xaml");
						ModBase.RunInUi(() =>
						{
							Config.Preference.Homepage.Type = 1;
							ModMain.frmLaunchRight.ForceRefresh();
							HintService.Hint(Lang.Text("Main.FileDrag.HomepageLoaded"), HintType.Success);
						});
					}
				}
				else if (!PageInstanceCompResource.InstallMods(filePathList))
				{
					if (new ReadOnlySpan<string>(new string[4] { "litematic", "nbt", "schematic", "schem" }).Contains(extension))
					{
						ModBase.Log("[System] 文件为 " + extension + " 格式，尝试作为原理图安装");
						string targetFolderPath = null;
						if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.DownloadClient && ModMain.frmInstanceSchematic != null && ModMain.frmInstanceSchematic != null)
						{
							targetFolderPath = ModMain.frmInstanceSchematic.CurrentFolderPath;
						}
						PageInstanceCompResource.InstallCompFiles(filePathList, ModComp.CompType.Schematic, targetFolderPath);
					}
					else
					{
						if (pageCurrent == PageType.InstanceSetup && new string[1] { "zip" }.Any((string i) => (i ?? "") == (extension ?? "")))
						{
							switch (PageCurrentSub)
							{
							case PageSubType.DownloadPack:
							{
								string text4 = PageInstanceLeft.McInstance.PathIndie + "saves\\" + ModBase.GetFileNameWithoutExtentionFromPath(text);
								string path = Path.Combine(text4, "level.dat");
								if (Directory.Exists(text4))
								{
									HintService.Hint(Lang.Text("Main.FileDrag.SameFolderExists", new object[1] { text4 }), HintType.Error);
								}
								else
								{
									string text5 = Path.Combine(ModBase.pathTemp, "Cache", "WorldImport", ModBase.GetUuid().ToString());
									try
									{
										ModBase.ExtractFile(text, text5);
										string saveRootDirectory = SaveImportHelper.GetSaveRootDirectory(text5);
										if (saveRootDirectory == null)
										{
											HintService.Hint(Lang.Text("Main.FileDrag.SaveNotFound"), HintType.Error);
											return;
										}
										ModBase.CopyDirectory(saveRootDirectory, text4);
										if (!File.Exists(path))
										{
											if (Directory.Exists(text4))
											{
												ModBase.DeleteDirectory(text4, ignoreIssue: true);
											}
											HintService.Hint(Lang.Text("Main.FileDrag.SaveInvalid"), HintType.Error);
											return;
										}
									}
									catch (Exception ex)
									{
										if (Directory.Exists(text4))
										{
											ModBase.DeleteDirectory(text4, ignoreIssue: true);
										}
										ModBase.Log(ex, Lang.Text("Main.FileDrag.SaveImportFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Main.FileDrag.SaveImportFailed"));
										return;
									}
									finally
									{
										if (Directory.Exists(text5))
										{
											ModBase.DeleteDirectory(text5, ignoreIssue: true);
										}
									}
									HintService.Hint(Lang.Text("Main.FileDrag.Imported", new object[1] { ModBase.GetFileNameWithoutExtentionFromPath(text) }), HintType.Success);
									if (ModMain.frmInstanceSaves != null)
									{
										ModBase.RunInUi(() =>
										{
											ModMain.frmInstanceSaves.Reload();
										});
									}
								}
								return;
							}
							case PageSubType.DownloadWorld:
							{
								string text3 = PageInstanceLeft.McInstance.PathIndie + "resourcepacks\\" + ModBase.GetFileNameFromPath(text);
								if (File.Exists(text3))
								{
									HintService.Hint(Lang.Text("Main.FileDrag.SameFileExists", new object[1] { text3 }), HintType.Error);
								}
								else
								{
									ModBase.CopyFile(text, text3);
									HintService.Hint(Lang.Text("Main.FileDrag.Imported", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success);
									if (ModMain.frmInstanceResourcePack != null)
									{
										ModBase.RunInUi(() =>
										{
											ModMain.frmInstanceResourcePack.ReloadCompFileList();
										});
									}
								}
								return;
							}
							case PageSubType.DownloadCompFavorites:
							{
								string text2 = PageInstanceLeft.McInstance.PathIndie + "shaderpacks\\" + ModBase.GetFileNameFromPath(text);
								if (File.Exists(text2))
								{
									HintService.Hint(Lang.Text("Main.FileDrag.SameFileExists", new object[1] { text2 }), HintType.Error);
								}
								else
								{
									ModBase.CopyFile(text, text2);
									HintService.Hint(Lang.Text("Main.FileDrag.Imported", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success);
									if (ModMain.frmInstanceShader != null)
									{
										ModBase.RunInUi(() =>
										{
											ModMain.frmInstanceShader.ReloadCompFileList();
										});
									}
								}
								return;
							}
							}
						}
						if (pageCurrent == PageType.InstanceSetup && new ReadOnlySpan<string>(new string[4] { "litematic", "nbt", "schematic", "schem" }).Contains(extension) && PageCurrentSub == PageSubType.DownloadClient)
						{
							string text6 = PageInstanceLeft.McInstance.PathIndie + "schematics\\" + ModBase.GetFileNameFromPath(text);
							if (File.Exists(text6))
							{
								HintService.Hint(Lang.Text("Main.FileDrag.SameFileExists", new object[1] { text6 }), HintType.Error);
							}
							else
							{
								Directory.CreateDirectory(PageInstanceLeft.McInstance.PathIndie + "schematics\\");
								ModBase.CopyFile(text, text6);
								HintService.Hint(Lang.Text("Main.FileDrag.Imported", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success);
								if (ModMain.frmInstanceSchematic != null)
								{
									ModBase.RunInUi(() =>
									{
										ModMain.frmInstanceSchematic.ReloadCompFileList();
									});
								}
							}
						}
						else
						{
							if (new string[3] { "zip", "rar", "mrpack" }.Any((string t) => (t ?? "") == (extension ?? "")))
							{
								ModBase.Log("[System] 文件为压缩包，尝试作为整合包安装");
								try
								{
									ModModpack.ModpackInstall(text);
									return;
								}
								catch (ModBase.CancelledException)
								{
									return;
								}
								catch (Exception)
								{
								}
							}
							try
							{
								ModBase.Log("[System] 尝试进行错误报告分析");
								CrashAnalyzer crashAnalyzer = new CrashAnalyzer(ModBase.GetUuid());
								crashAnalyzer.Import(text);
								if (crashAnalyzer.Prepare())
								{
									crashAnalyzer.Analyze();
									crashAnalyzer.Output(isHandAnalyze: true, new List<string>());
									return;
								}
							}
							catch (Exception ex4)
							{
								ModBase.Log(ex4, "自主错误报告分析失败", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
							}
							HintService.Hint(Lang.Text("Main.FileDrag.UnknownOperation"));
						}
					}
				}
			}
		}, "文件拖拽");
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Invalid comparison between Unknown and I4
		switch (msg)
		{
		case 30:
		{
			DateTime now = DateTime.Now;
			if (now.Date == ModBase.applicationOpenTime.Date)
			{
				ModBase.Log("[System] 系统时间微调为：" + now.ToLongDateString() + " " + now.ToLongTimeString());
				isSystemTimeChanged = false;
			}
			else
			{
				ModBase.Log("[System] 系统时间修改为：" + now.ToLongDateString() + " " + now.ToLongTimeString());
				isSystemTimeChanged = true;
			}
			break;
		}
		case 6402:
			ModBase.Log("[System] 收到置顶信息：" + ((IntPtr)hwnd).ToInt64());
			if (!isWindowLoadFinished)
			{
				ModBase.Log("[System] 窗口尚未加载完成，忽略置顶请求");
				return IntPtr.Zero;
			}
			ShowWindowToTop();
			handled = true;
			break;
		case 26:
			if (Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet")
			{
				ModBase.Log($"[System] 系统主题更改，深色模式：{SystemTheme.IsSystemInDarkMode()}");
				if ((int)Config.Preference.Theme.ColorMode == 2 && ThemeManager.IsDarkMode != SystemTheme.IsSystemInDarkMode())
				{
					ThemeService.RefreshColorMode();
				}
			}
			break;
		}
		return IntPtr.Zero;
	}

	protected override void OnActivated(EventArgs e)
	{
		base.OnActivated(e);
		if (Hidden)
		{
			Hidden = false;
		}
	}

	public void ShowWindowToTop()
	{
		ModBase.RunInUi(() =>
		{
			Visibility = Visibility.Visible;
			ShowInTaskbar = true;
			WindowState = WindowState.Normal;
			Hidden = false;
			Topmost = true;
			Topmost = false;
			ModMain.SetForegroundWindow(ModBase.frmHandle);
			Focus();
			ModBase.Log($"[System] 窗口已置顶，位置：({Left}, {Top}), {Width} x {Height}");
		});
	}

	private void VideoEnded(object sender, RoutedEventArgs e)
	{
		VideoBack.Position = TimeSpan.Zero;
		VideoBack.Play();
	}

	private void WindowStateChanged(object sender, EventArgs e)
	{
		switch (WindowState)
		{
		case WindowState.Minimized:
			ModVideoBack.isMinimized = true;
			ModVideoBack.VideoPause();
			break;
		case WindowState.Normal:
			ModVideoBack.isMinimized = false;
			ModVideoBack.VideoPlay();
			break;
		}
	}

	private string PageNameGet(PageStackData stack)
	{
		return stack.page switch
		{
			PageType.InstanceSelect => Lang.Text("Main.Title.InstanceSelect"), 
			PageType.TaskManager => Lang.Text("Main.Title.TaskManager"), 
			PageType.GameLog => Lang.Text("Main.Title.GameLog"), 
			PageType.InstanceSetup => Lang.Text("Main.Title.InstanceSetup", new object[1] { ((object)PageInstanceLeft.McInstance == null) ? Lang.Text("Common.State.Unknown") : PageInstanceLeft.McInstance.Name }), 
			PageType.CompDetail => Lang.Text("Main.Title.ResourceDownload", new object[1] { stack.additional.Value.CompProject.TranslatedName }), 
			PageType.HelpDetail => stack.helpPage?.Title ?? "", 
			PageType.VersionSaves => Lang.Text("Main.Title.SaveManagement", new object[1] { ModBase.GetFolderNameFromPath(stack.additional.Value.SavePath) }), 
			_ => "", 
		};
	}

	public void PageNameRefresh(PageStackData type)
	{
		LabTitleInner.Text = PageNameGet(type);
	}

	public void PageNameRefresh()
	{
		PageNameRefresh(pageCurrent);
	}

	public void PageChange(PageStackData stack, PageSubType subType = PageSubType.Default)
	{
		if (string.IsNullOrEmpty(PageNameGet(stack)))
		{
			PageChangeExit();
			isChangingPage = true;
			MyRadioButton navButton = PanTitleSelect.Children.OfType<MyRadioButton>().FirstOrDefault(button => ModBase.Val(button.Tag) == (double)(int)stack.page);
			navButton?.SetChecked(value: true, raiseByMouse: true, string.IsNullOrEmpty(PageNameGet(pageCurrent)));
			isChangingPage = false;
			switch (stack.page)
			{
			case PageType.Download:
				if (ModMain.frmDownloadLeft == null)
				{
					ModMain.frmDownloadLeft = new PageDownloadLeft();
				}
				foreach (object child in ModMain.frmDownloadLeft.PanItem.Children)
				{
					if (child is MyListItem myListItem2 && ModBase.Val(myListItem2.Tag) == (double)subType)
					{
						myListItem2.SetChecked(value: true, user: true, stack == pageCurrent);
						break;
					}
				}
				break;
			case PageType.Setup:
				if (ModMain.frmSetupLeft == null)
				{
					ModMain.frmSetupLeft = new PageSetupLeft();
				}
				foreach (object child2 in ModMain.frmSetupLeft.PanItem.Children)
				{
					if (child2 is MyListItem myListItem && ModBase.Val(myListItem.Tag) == (double)subType)
					{
						myListItem.SetChecked(value: true, user: true, stack == pageCurrent);
						break;
					}
				}
				break;
			}
			PageChangeActual(stack, subType);
			return;
		}
		switch (stack.page)
		{
		case PageType.InstanceSetup:
			if (ModMain.frmInstanceLeft == null)
			{
				ModMain.frmInstanceLeft = new PageInstanceLeft();
			}
			foreach (object child3 in ModMain.frmInstanceLeft.PanItem.Children)
			{
				if (child3 is MyListItem myListItem4 && ModBase.Val(myListItem4.Tag) == (double)subType)
				{
					myListItem4.SetChecked(value: true, user: true, stack == pageCurrent);
					break;
				}
			}
			break;
		case PageType.VersionSaves:
			if (ModMain.frmInstanceSavesLeft == null)
			{
				ModMain.frmInstanceSavesLeft = new PageInstanceSavesLeft();
			}
			foreach (object child4 in ModMain.frmInstanceSavesLeft.PanItem.Children)
			{
				if (child4 is MyListItem myListItem3 && ModBase.Val(myListItem3.Tag) == (double)subType)
				{
					myListItem3.SetChecked(value: true, user: true, stack == pageCurrent);
					break;
				}
			}
			break;
		}
		PageChangeActual(stack, subType);
	}

	private void BtnTitleSelect_Click(MyRadioButton sender, bool raiseByMouse)
	{
		if (!isChangingPage)
		{
			PageType pageType = (PageType)int.Parse(sender.Tag.ToString());
			PageChangeActual(pageType, PageSubType.Default);
		}
	}

	private void BtnTitleInner_Click(object sender, EventArgs e)
	{
		PageBack();
	}

	public void PageBack()
	{
		if (pageStack.Any())
		{
			PageChangeActual(pageStack[0], PageSubType.Default);
		}
		else
		{
			PageChange(PageType.Launch);
		}
	}

	private void PageChangeActual(PageStackData stack, PageSubType subType)
	{
		if (pageCurrent == stack && (PageCurrentSub == subType || subType == (PageSubType)(-1)))
		{
			return;
		}
		ModAnimation.AniControlEnabled++;
		try
		{
			string pageName = PageNameGet(stack);
			if (string.IsNullOrEmpty(pageName))
			{
				PageChangeExit();
			}
			else if (pageStack.Any())
			{
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabTitleInner, 0.0 - LabTitleInner.Opacity, 130),
					ModAnimation.AaCode(() =>
					{
						LabTitleInner.Text = pageName;
					}, 0, after: true),
					ModAnimation.AaOpacity(LabTitleInner, 1.0, 150, 30)
				}, "FrmMain Titlebar SubLayer");
				if (pageStack.Contains(stack))
				{
					while (pageStack.Contains(stack))
					{
						pageStack.RemoveAt(0);
					}
				}
				else
				{
					pageStack.Insert(0, pageCurrent);
				}
			}
			else
			{
				PanTitleInner.Visibility = Visibility.Visible;
				PanTitleMain.IsHitTestVisible = false;
				PanTitleInner.IsHitTestVisible = true;
				PageNameRefresh(stack);
				ModAnimation.AniStart(new ModAnimation.AniData[5]
				{
					ModAnimation.AaOpacity(PanTitleMain, 0.0 - PanTitleMain.Opacity, 150),
					ModAnimation.AaX(PanTitleMain, 12.0 - PanTitleMain.Margin.Left, 150, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaOpacity(PanTitleInner, 1.0 - PanTitleInner.Opacity, 150, 200),
					ModAnimation.AaX(PanTitleInner, 0.0 - PanTitleInner.Margin.Left, 350, 200, new ModAnimation.AniEaseOutBack()),
					ModAnimation.AaCode(() =>
					{
						PanTitleMain.Visibility = Visibility.Collapsed;
					}, 0, after: true)
				}, "FrmMain Titlebar FirstLayer");
				pageStack.Insert(0, pageCurrent);
			}
			pageLast = pageCurrent;
			pageCurrent = stack;
			switch (stack.page)
			{
			case PageType.Launch:
				PageChangeAnim(ModMain.frmLaunchLeft, ModMain.frmLaunchRight);
				break;
			case PageType.Download:
				if (ModMain.frmDownloadLeft == null)
				{
					ModMain.frmDownloadLeft = new PageDownloadLeft();
				}
				if (subType != PageSubType.Default)
				{
					ModMain.frmDownloadLeft.pageID = subType;
				}
				else
				{
					subType = ModMain.frmDownloadLeft.pageID;
				}
				PageChangeAnim(ModMain.frmDownloadLeft, (FrameworkElement)ModMain.frmDownloadLeft.PageGet(subType));
				break;
			case PageType.Tools:
				if (ModMain.frmToolsLeft == null)
				{
					ModMain.frmToolsLeft = new PageToolsLeft();
				}
				subType = ModMain.frmToolsLeft.pageID;
				PageChangeAnim(ModMain.frmToolsLeft, (FrameworkElement)ModMain.frmToolsLeft.PageGet(subType));
				break;
			case PageType.Setup:
				if (ModMain.frmSetupLeft == null)
				{
					ModMain.frmSetupLeft = new PageSetupLeft();
				}
				subType = ModMain.frmSetupLeft.pageID;
				PageChangeAnim(ModMain.frmSetupLeft, (FrameworkElement)ModMain.frmSetupLeft.PageGet(subType));
				break;
			case PageType.Multiplayer:
				if (ModMain.frmToolsGameLink == null)
				{
					ModMain.frmToolsGameLink = new PageToolsGameLink();
				}
				ModMain.frmToolsGameLink.Reload();
				PageChangeAnim(new MyPageLeft(), ModMain.frmToolsGameLink);
				break;
			case PageType.GameLog:
				if (ModMain.frmLogLeft == null)
				{
					ModMain.frmLogLeft = new PageLogLeft();
				}
				if (ModMain.frmLogLeft == null)
				{
					ModMain.frmLogRight = new PageLogRight();
				}
				PageChangeAnim(ModMain.frmLogLeft, ModMain.frmLogRight);
				break;
			case PageType.InstanceSelect:
				if (ModMain.frmSelectLeft == null)
				{
					ModMain.frmSelectLeft = new PageSelectLeft();
				}
				if (ModMain.frmSelectRight == null)
				{
					ModMain.frmSelectRight = new PageSelectRight();
				}
				PageChangeAnim(ModMain.frmSelectLeft, ModMain.frmSelectRight);
				break;
			case PageType.TaskManager:
				if (ModMain.frmSpeedLeft == null)
				{
					ModMain.frmSpeedLeft = new PageSpeedLeft();
				}
				if (ModMain.frmSpeedRight == null)
				{
					ModMain.frmSpeedRight = new PageSpeedRight();
				}
				PageChangeAnim(ModMain.frmSpeedLeft, ModMain.frmSpeedRight);
				break;
			case PageType.InstanceSetup:
				if (ModMain.frmInstanceLeft == null)
				{
					ModMain.frmInstanceLeft = new PageInstanceLeft();
				}
				subType = ModMain.frmInstanceLeft.pageID;
				PageChangeAnim(ModMain.frmInstanceLeft, (FrameworkElement)ModMain.frmInstanceLeft.PageGet(subType));
				break;
			case PageType.CompDetail:
				if (ModMain.frmDownloadCompDetail == null)
				{
					ModMain.frmDownloadCompDetail = new PageDownloadCompDetail();
				}
				PageChangeAnim(new MyPageLeft(), ModMain.frmDownloadCompDetail);
				break;
			case PageType.HelpDetail:
				if (stack.helpPage == null)
				{
					throw new InvalidOperationException("帮助详情页面未初始化");
				}
				PageChangeAnim(new MyPageLeft(), stack.helpPage);
				break;
			case PageType.VersionSaves:
				if (ModMain.frmInstanceSavesLeft == null)
				{
					ModMain.frmInstanceSavesLeft = new PageInstanceSavesLeft();
				}
				PageInstanceSavesLeft.currentSave = stack.additional.Value.SavePath;
				subType = ModMain.frmInstanceSavesLeft.pageID;
				PageChangeAnim(ModMain.frmInstanceSavesLeft, (FrameworkElement)ModMain.frmInstanceSavesLeft.PageGet(subType));
				break;
			}
			BtnExtraDownload.ShowRefresh();
			BtnExtraApril.ShowRefresh();
			string stringFromEnum = ModBase.GetStringFromEnum((PageType)stack);
			int num = (int)subType;
			ModBase.Log("[Control] 切换主要页面：" + stringFromEnum + ", " + num);
		}
		catch (Exception ex)
		{
			int num = (int)pageCurrent.page;
			ModBase.Log(ex, "切换主要页面失败（ID " + num + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
		finally
		{
			ModAnimation.AniControlEnabled--;
		}
	}

	private void PageChangeAnim(FrameworkElement targetLeft, FrameworkElement targetRight)
	{
		ModAnimation.AniStop("FrmMain LeftChange");
		ModAnimation.AniStop("PageLeft PageChange");
		ModAnimation.AniControlEnabled++;
		if (targetLeft.Parent != null)
		{
			((DependencyObject)targetLeft).SetValue(ContentPresenter.ContentProperty, (object)null);
		}
		if (targetRight != null && targetRight.Parent != null)
		{
			((DependencyObject)targetRight).SetValue(ContentPresenter.ContentProperty, (object)null);
		}
		pageLeft = (MyPageLeft)targetLeft;
		pageRight = (MyPageRight)targetRight;
		((MyPageLeft)PanMainLeft.Child).TriggerHideAnimation();
		((MyPageRight)PanMainRight.Child).PageOnExit();
		ModAnimation.AniControlEnabled--;
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaCode(() =>
			{
				ModAnimation.AniControlEnabled++;
				PanMainLeft.Child = pageLeft;
				pageLeft.Opacity = 0.0;
				PanMainLeft.Background = null;
				ModAnimation.AniControlEnabled--;
				ModBase.RunInUi(() =>
				{
					PanMainLeft_Resize(PanMainLeft.ActualWidth);
				}, forceWaitUntilLoaded: true);
			}, 110),
			ModAnimation.AaCode(() =>
			{
				pageLeft.Opacity = 1.0;
				pageLeft.TriggerShowAnimation();
			}, 30, after: true)
		}, "FrmMain PageChangeLeft");
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaCode(() =>
			{
				ModAnimation.AniControlEnabled++;
				((MyPageRight)PanMainRight.Child).PageOnForceExit();
				PanMainRight.Child = pageRight;
				pageRight.Opacity = 0.0;
				PanMainRight.Background = null;
				ModAnimation.AniControlEnabled--;
				ModBase.RunInUi(() =>
				{
					BtnExtraBack.ShowRefresh();
				}, forceWaitUntilLoaded: true);
			}, 110),
			ModAnimation.AaCode(() =>
			{
				pageRight.Opacity = 1.0;
				pageRight.PageOnEnter();
			}, 30, after: true)
		}, "FrmMain PageChangeRight");
	}

	private void PageChangeExit()
	{
		if (pageStack.Any())
		{
			PanTitleMain.Visibility = Visibility.Visible;
			PanTitleMain.IsHitTestVisible = true;
			PanTitleInner.IsHitTestVisible = false;
			ModAnimation.AniStart(new ModAnimation.AniData[5]
			{
				ModAnimation.AaOpacity(PanTitleInner, 0.0 - PanTitleInner.Opacity, 150),
				ModAnimation.AaX(PanTitleInner, -18.0 - PanTitleInner.Margin.Left, 150, 0, new ModAnimation.AniEaseInFluent()),
				ModAnimation.AaOpacity(PanTitleMain, 1.0 - PanTitleMain.Opacity, 150, 200),
				ModAnimation.AaX(PanTitleMain, 0.0 - PanTitleMain.Margin.Left, 350, 200, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaCode(() =>
				{
					PanTitleInner.Visibility = Visibility.Collapsed;
				}, 0, after: true)
			}, "FrmMain Titlebar FirstLayer");
			pageStack.Clear();
		}
	}

	private void PanMainLeft_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (e.WidthChanged)
		{
			Size newSize = e.NewSize;
			PanMainLeft_Resize(newSize.Width);
		}
	}

	private void PanMainLeft_Resize(double newWidth)
	{
		if (Math.Abs(newWidth - RectLeftBackground.Width) > 0.1 && ModAnimation.AniControlEnabled == 0)
		{
			if (PanMain.Opacity < 0.1)
			{
				PanMainLeft.IsHitTestVisible = false;
			}
			if (newWidth > 0.0)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaWidth(RectLeftBackground, newWidth - RectLeftBackground.Width, 180, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
					ModAnimation.AaOpacity(RectLeftShadow, 1.0 - RectLeftShadow.Opacity, 180),
					ModAnimation.AaCode(() =>
					{
						PanMainLeft.IsHitTestVisible = true;
					}, 150)
				}, "FrmMain LeftChange", refreshTime: true);
			}
			else
			{
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaWidth(RectLeftBackground, 0.0 - RectLeftBackground.Width, 180, 0, new ModAnimation.AniEaseOutFluent()),
					ModAnimation.AaOpacity(RectLeftShadow, 0.0 - RectLeftShadow.Opacity, 180),
					ModAnimation.AaCode(() =>
					{
						PanMainLeft.IsHitTestVisible = true;
					}, 150)
				}, "FrmMain LeftChange", refreshTime: true);
			}
		}
		else
		{
			RectLeftBackground.Width = newWidth;
			PanMainLeft.IsHitTestVisible = true;
			ModAnimation.AniStop("FrmMain LeftChange");
		}
	}

	public void DragTick()
	{
		if (ModMain.dragControl != null && Mouse.LeftButton != MouseButtonState.Pressed)
		{
			DragStop();
		}
	}

	public void DragDoing()
	{
		if (ModMain.dragControl != null)
		{
			if (Mouse.LeftButton == MouseButtonState.Pressed)
			{
				ModMain.dragControl.DragDoing();
			}
			else
			{
				DragStop();
			}
		}
	}

	private void PanBack_MouseMove(object sender, EventArgs e)
	{
		DragDoing();
	}

	public void DragStop()
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.dragControl != null)
			{
				MySlider? dragControl = ModMain.dragControl;
				ModMain.dragControl = null;
				dragControl.DragStop();
			}
		});
	}

	private void BtnExtraUpdateRestart_Click(object sender, MouseButtonEventArgs e)
	{
		UpdateManager.UpdateRestart(triggerRestartAndByEnd: true);
	}

	private bool BtnExtraUpdateRestart_ShowCheck()
	{
		return UpdateManager.isUpdateWaitingRestart;
	}

	private void BtnExtraMusic_Click(object sender, MouseButtonEventArgs e)
	{
		ModMusic.MusicControlPause();
	}

	private void BtnExtraMusic_RightClick(object sender, MouseButtonEventArgs e)
	{
		ModMusic.MusicControlNext();
	}

	private void BtnExtraDownload_Click(object sender, MouseButtonEventArgs e)
	{
		PageChange(PageType.TaskManager);
	}

	private bool BtnExtraDownload_ShowCheck()
	{
		if (ModNet.HasDownloadingTask())
		{
			return !(pageCurrent == PageType.TaskManager);
		}
		return false;
	}

	public void AprilGiveup()
	{
		if (ModMain.isAprilEnabled && !ModMain.isAprilGiveup)
		{
			HintService.Hint("=D", HintType.Success);
			ModMain.isAprilGiveup = true;
			ModMain.frmLaunchLeft.AprilScaleTrans.ScaleX = 1.0;
			ModMain.frmLaunchLeft.AprilScaleTrans.ScaleY = 1.0;
			BtnExtraApril.ShowRefresh();
		}
	}

	private void BtnExtraApril_Click(object sender, MouseButtonEventArgs e)
	{
		AprilGiveup();
	}

	public bool BtnExtraApril_ShowCheck()
	{
		if (ModMain.isAprilEnabled && !ModMain.isAprilGiveup)
		{
			return pageCurrent == PageType.Launch;
		}
		return false;
	}

	private void BtnExtraShutdown_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			if (ModLaunch.mcLaunchLoaderReal != null)
			{
				ModLaunch.mcLaunchLoaderReal.Abort();
			}
			foreach (ModWatcher.Watcher mcWatcher in ModWatcher.mcWatcherList)
			{
				mcWatcher.Kill();
			}
			HintService.Hint(Lang.Text("Main.ShutdownMinecraft.Success"), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "强制关闭所有 Minecraft 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	public bool BtnExtraShutdown_ShowCheck()
	{
		return ModWatcher.hasRunningMinecraft;
	}

	private void BtnExtraLog_Click(object sender, MouseButtonEventArgs e)
	{
		PageChange(PageType.GameLog);
	}

	public bool BtnExtraLog_ShowCheck()
	{
		if (ModMain.frmLogLeft == null || ModMain.frmLogRight == null || pageCurrent == PageType.GameLog)
		{
			return false;
		}
		return ModMain.frmLogLeft.shownLogs.Count > 0;
	}

	public void BackToTop()
	{
		MyScrollViewer myScrollViewer = BtnExtraBack_GetRealChild();
		if (myScrollViewer != null)
		{
			myScrollViewer.PerformVerticalOffsetDelta(0.0 - myScrollViewer.VerticalOffset);
		}
		else
		{
			ModBase.Log("[UI] 无法返回顶部，未找到合适的 RealScroll", ModBase.LogLevel.Hint, null, Lang.Text("Main.Error.ScrollToTopFailed"));
		}
	}

	private void BtnExtraBack_Click(object sender, MouseButtonEventArgs e)
	{
		BackToTop();
	}

	private bool BtnExtraBack_ShowCheck()
	{
		MyScrollViewer myScrollViewer = BtnExtraBack_GetRealChild();
		if (myScrollViewer != null && myScrollViewer.Visibility == Visibility.Visible)
		{
			return myScrollViewer.VerticalOffset > Height + (double)((!BtnExtraBack.Show) ? 700 : 0);
		}
		return false;
	}

	private MyScrollViewer? BtnExtraBack_GetRealChild()
	{
		if (PanMainRight.Child == null || !(PanMainRight.Child is MyPageRight))
		{
			return null;
		}
		return ((MyPageRight)PanMainRight.Child).PanScroll;
	}

}
