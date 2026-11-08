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
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Utils;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageLaunchLeft : MyPageLeft, IComponentConnector
{
	public interface ILoginPage
	{
		void Reload();
	}

	private enum LaunchButtonAction
	{
		Loading,
		Launch,
		Download,
		Disabled
	}

	private enum PageType
	{
		None,
		Auth,
		Ms,
		Profile,
		ProfileSkin,
		Offline
	}

	private double actualUsedHeight;

	private double actualUsedWidth;

	private int btnLaunchState;

	private string _btnLaunchLanguage;

	private McInstance btnLaunchVersion;

	private bool isHeightAnimating;

	private LaunchButtonAction _launchButtonAction;

	private bool isLoad;

	private bool isLoadFinished;

	private bool isWidthAnimating;

	private double showProgress;

	private PageType pageCurrent;

	public static ModLoader.LoaderTask<ModBase.EqualableList<string>, string> skinMs = new ModLoader.LoaderTask<ModBase.EqualableList<string>, string>("Loader Skin Ms", SkinMsLoad, SkinMsInput, ThreadPriority.AboveNormal);

	public static ModLoader.LoaderTask<ModBase.EqualableList<string>, string> skinLegacy = new ModLoader.LoaderTask<ModBase.EqualableList<string>, string>("Loader Skin Legacy", SkinLegacyLoad, SkinLegacyInput, ThreadPriority.AboveNormal);

	public static ModLoader.LoaderTask<ModBase.EqualableList<string>, string> skinAuth = new ModLoader.LoaderTask<ModBase.EqualableList<string>, string>("Loader Skin Auth", SkinAuthLoad, SkinAuthInput, ThreadPriority.AboveNormal);

	public static List<ModLoader.LoaderTask<ModBase.EqualableList<string>, string>> skinLoaders = new List<ModLoader.LoaderTask<ModBase.EqualableList<string>, string>> { skinMs, skinLegacy, skinAuth };

	private static string StageWaitWindow => Lang.Text("Minecraft.Launch.Stage.WaitWindow");

	private static string StageEnd => Lang.Text("Minecraft.Launch.Stage.End");

	private static string StageRoot => Lang.Text("Minecraft.Launch.Stage.Root");

	public PageLaunchLeft()
	{
		InitializeComponent();
		Loaded += PageLaunchLeft_Loaded;
		WeakLanguageChanged.Add<PageLaunchLeft>(this, (Action<PageLaunchLeft>)OnLanguageChanged);
		BtnInstance.Click += BtnInstance_Click;
		BtnLaunch.Click += BtnLaunch_Click;
		BtnLaunch.Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshButtonsUI();
		};
		BtnCancel.Click += BtnCancel_Click;
		BtnMore.Click += BtnMore_Click;
		PanLaunchingInfo.SizeChanged += PanLaunchingInfo_SizeChangedW;
		PanLaunchingInfo.SizeChanged += PanLaunchingInfo_SizeChangedH;
	}

	private static void OnLanguageChanged(PageLaunchLeft page)
	{
		ModBase.RunInUi(page.RefreshButtonsUI);
	}

	public void PageLaunchLeft_Loaded(object sender, RoutedEventArgs e)
	{
		if (isLoad)
		{
			RefreshPage(anim: false);
		}
		AprilPosTrans.X = 0.0;
		AprilPosTrans.Y = 0.0;
		if (isLoad)
		{
			return;
		}
		isLoad = true;
		ModAnimation.AniControlEnabled++;
		ModInstanceList.mcInstanceListLoader.LoadingStateChanged += (MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			RefreshButtonsUI();
		};
		ModFolder.mcFolderListLoader.LoadingStateChanged += (MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			RefreshButtonsUI();
		};
		RefreshButtonsUI();
		ProfileService.Load();
		if (ProfileService.LastUsedProfile >= 0 && ProfileService.LastUsedProfile < ProfileService.Profiles.Count)
		{
			ProfileService.SelectAt(ProfileService.LastUsedProfile);
		}
		ModBase.RunInNewThread(() =>
		{
			string text = null;
			if (File.Exists(Path.Combine(ModBase.exePath, "modpack.zip")))
			{
				text = Path.Combine(ModBase.exePath, "modpack.zip");
			}
			if (File.Exists(Path.Combine(ModBase.exePath, "modpack.mrpack")))
			{
				text = Path.Combine(ModBase.exePath, "modpack.mrpack");
			}
			if (text != null && !RestrictedFeaturePolicy.Check("auto-modpack-install"))
			{
				text = null;
			}
			if (text != null)
			{
				ModBase.Log("[Launch] 需自动安装整合包：" + text, ModBase.LogLevel.Debug);
				States.Game.SelectedFolder = "$.minecraft\\";
				if (!Directory.Exists(ModBase.exePath + ".minecraft\\"))
				{
					Directory.CreateDirectory(ModBase.exePath + ".minecraft\\");
					Directory.CreateDirectory(ModBase.exePath + ".minecraft\\versions\\");
					ModFolder.McFolderLauncherProfilesJsonCreate(ModBase.exePath + ".minecraft\\");
				}
				PageSelectLeft.AddFolder(ModBase.exePath + ".minecraft\\", ModBase.GetFolderNameFromPath(ModBase.exePath), showHint: false);
				ModFolder.mcFolderListLoader.WaitForExit();
			}
			ModFolder.mcFolderSelected = States.Game.SelectedFolder.ToString().Replace("$", ModBase.exePath);
			if (string.IsNullOrEmpty(ModFolder.mcFolderSelected) || !Directory.Exists(ModFolder.mcFolderSelected))
			{
				if (string.IsNullOrEmpty(ModFolder.mcFolderSelected))
				{
					ModBase.Log("[Launch] 没有已储存的 Minecraft 文件夹");
				}
				else
				{
					ModBase.Log("[Launch] Minecraft 文件夹无效，该文件夹已不存在：" + ModFolder.mcFolderSelected, ModBase.LogLevel.Debug);
				}
				ModFolder.mcFolderListLoader.WaitForExit(null, null, isForceRestart: true);
				States.Game.SelectedFolder = ModFolder.mcFolderList[0].Location.Replace(ModBase.exePath, "$");
			}
			ModBase.Log("[Launch] Minecraft 文件夹：" + ModFolder.mcFolderSelected);
			if (Config.Debug.AddRandomDelay)
			{
				Thread.Sleep(RandomUtils.NextInt(500, 3000));
			}
			if (text != null)
			{
				try
				{
					ModLoader.LoaderCombo<string> loaderCombo = ModModpack.ModpackInstall(text);
					ModBase.Log("[Launch] 自动安装整合包已开始：" + text);
					loaderCombo.WaitForExit();
					if (loaderCombo.State == ModBase.LoadState.Finished)
					{
						ModBase.Log("[Launch] 自动安装整合包成功，清理安装包：" + text);
						if (File.Exists(text))
						{
							File.Delete(text);
						}
					}
				}
				catch (ModBase.CancelledException ex)
				{
					ModBase.Log(ex, "自动安装整合包被用户取消：" + text);
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, Lang.Text("Select.Folder.Error.InstallPack", new object[1] { text }), ModBase.LogLevel.Msgbox, null, Lang.Text("Select.Folder.Error.InstallPack", new object[1] { text }));
				}
			}
			string selectedInstance = States.Game.SelectedInstance;
			McInstance instance = ((selectedInstance == "") ? null : new McInstance(selectedInstance));
			if ((object)instance == null || !instance.PathInstance.StartsWithF(ModFolder.mcFolderSelected) || !instance.Check())
			{
				ModBase.Log("[Launch] 当前选择的 Minecraft 实例无效：" + (((object)instance == null) ? "null" : instance.PathInstance), ((object)instance != null) ? ModBase.LogLevel.Debug : ModBase.LogLevel.Normal);
				if (ModInstanceList.mcInstanceListLoader.State != ModBase.LoadState.Finished)
				{
					ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\", waitForExit: true);
				}
				if (ModInstanceList.mcInstanceList.Count == 0 || ModInstanceList.mcInstanceList.First().Value[0].Logo.Contains("RedstoneBlock"))
				{
					instance = null;
					States.Game.SelectedInstance = "";
					ModBase.Log("[Launch] 无可用 Minecraft 实例");
				}
				else
				{
					instance = ModInstanceList.mcInstanceList.First().Value[0];
					States.Game.SelectedInstance = instance.Name;
					ModBase.Log("[Launch] 自动选择 Minecraft 实例：" + instance.PathInstance);
				}
			}
			ModBase.RunInUi(() =>
			{
				ModInstanceList.McMcInstanceSelected = instance;
				isLoadFinished = true;
				RefreshButtonsUI();
				RefreshPage(anim: false);
			});
		}, "Instance Check", ThreadPriority.AboveNormal);
		RefreshPage(anim: false);
		ModAnimation.AniControlEnabled--;
	}

	private void BtnInstance_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModLaunch.mcLaunchLoader.State != ModBase.LoadState.Loading)
		{
			ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
		}
	}

	public void LaunchButtonClick()
	{
		if (ModLaunch.mcLaunchLoader.State == ModBase.LoadState.Loading || !BtnLaunch.IsEnabled || (ModMain.frmMain.pageRight != null && ModMain.frmMain.pageRight.PageState != MyPageRight.PageStates.ContentStay && ModMain.frmMain.pageRight.PageState != MyPageRight.PageStates.ContentEnter))
		{
			return;
		}
		if (ModMain.isAprilEnabled && !ModMain.isAprilGiveup)
		{
			ModMain.isAprilGiveup = true;
			ModMain.frmLaunchLeft.AprilScaleTrans.ScaleX = 1.0;
			ModMain.frmLaunchLeft.AprilScaleTrans.ScaleY = 1.0;
			ModMain.frmLaunchLeft.AprilPosTrans.X = 0.0;
			ModMain.frmLaunchLeft.AprilPosTrans.Y = 0.0;
			ModMain.frmMain.BtnExtraApril.ShowRefresh();
		}
		switch (_launchButtonAction)
		{
		case LaunchButtonAction.Launch:
			if (File.Exists(ModInstanceList.McMcInstanceSelected.PathInstance + ".pclignore"))
			{
				HintService.Hint(Lang.Text("Launch.Home.Instance.InstallingCannotLaunch"), HintType.Error);
			}
			else
			{
				ModLaunch.McLaunchStart();
			}
			break;
		case LaunchButtonAction.Download:
			ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall);
			break;
		}
	}

	public void RefreshButtonsUI()
	{
		if (!BtnLaunch.IsLoaded)
		{
			return;
		}
		int num = ((isLoadFinished && ModInstanceList.mcInstanceListLoader.State != ModBase.LoadState.Loading && ModFolder.mcFolderListLoader.State != ModBase.LoadState.Loading) ? (((object)ModInstanceList.McMcInstanceSelected != null) ? 3 : ((Config.Preference.Hide.PageDownload && !PageSetupUI.HiddenForceShow) ? 1 : 2)) : 0);
		string code = LocalizationService.CurrentLanguage.Code;
		if (num != btnLaunchState || !(code == _btnLaunchLanguage) || !(((((object)ModInstanceList.McMcInstanceSelected == null) ? "" : ModInstanceList.McMcInstanceSelected.PathInstance) ?? "") == ((((object)btnLaunchVersion == null) ? "" : btnLaunchVersion.PathInstance) ?? "")))
		{
			_btnLaunchLanguage = code;
			btnLaunchVersion = ModInstanceList.McMcInstanceSelected;
			btnLaunchState = num;
			switch (num)
			{
			case 0:
				_launchButtonAction = LaunchButtonAction.Loading;
				ModBase.Log("[Minecraft] 启动按钮：正在加载 Minecraft 实例");
				ModMain.frmLaunchLeft.BtnLaunch.Text = Lang.Text("Launch.Home.Button.Loading");
				ModMain.frmLaunchLeft.BtnLaunch.IsEnabled = false;
				ModMain.frmLaunchLeft.LabVersion.Text = Lang.Text("Launch.Home.Instance.Loading");
				ModMain.frmLaunchLeft.BtnInstance.IsEnabled = false;
				ModMain.frmLaunchLeft.BtnMore.Visibility = Visibility.Collapsed;
				break;
			case 1:
				_launchButtonAction = LaunchButtonAction.Disabled;
				ModBase.Log("[Minecraft] 启动按钮：无 Minecraft 实例，下载已禁用");
				ModMain.frmLaunchLeft.BtnLaunch.Text = Lang.Text("Launch.Home.Button.Launch");
				ModMain.frmLaunchLeft.BtnLaunch.IsEnabled = false;
				ModMain.frmLaunchLeft.LabVersion.Text = Lang.Text("Launch.Home.Instance.NotFound");
				ModMain.frmLaunchLeft.BtnInstance.IsEnabled = true;
				ModMain.frmLaunchLeft.BtnMore.Visibility = Visibility.Collapsed;
				break;
			case 2:
				_launchButtonAction = LaunchButtonAction.Download;
				ModBase.Log("[Minecraft] 启动按钮：无 Minecraft 实例，要求下载");
				ModMain.frmLaunchLeft.BtnLaunch.Text = Lang.Text("Launch.Home.Button.Download");
				ModMain.frmLaunchLeft.BtnLaunch.IsEnabled = true;
				ModMain.frmLaunchLeft.LabVersion.Text = Lang.Text("Launch.Home.Instance.NotFound");
				ModMain.frmLaunchLeft.BtnInstance.IsEnabled = true;
				ModMain.frmLaunchLeft.BtnMore.Visibility = Visibility.Collapsed;
				break;
			case 3:
				_launchButtonAction = LaunchButtonAction.Launch;
				ModBase.Log("[Minecraft] 启动按钮：Minecraft 实例：" + ModInstanceList.McMcInstanceSelected.PathInstance);
				ModMain.frmLaunchLeft.BtnLaunch.Text = Lang.Text("Launch.Home.Button.Launch");
				ModMain.frmLaunchLeft.BtnInstance.IsEnabled = true;
				if (ProfileService.Current != null)
				{
					BtnLaunch.IsEnabled = true;
				}
				else
				{
					BtnLaunch.IsEnabled = false;
				}
				ModMain.frmLaunchLeft.LabVersion.Text = ModInstanceList.McMcInstanceSelected.Name;
				break;
			}
		}
		ModMain.frmLaunchLeft.BtnInstance.Visibility = ((!PageSetupUI.HiddenForceShow && Config.Preference.Hide.FunctionSelect) ? Visibility.Collapsed : Visibility.Visible);
		if (num == 3)
		{
			ModMain.frmLaunchLeft.BtnMore.Visibility = ModMain.frmLaunchLeft.BtnInstance.Visibility;
		}
	}

	private void BtnCancel_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModLaunch.mcLaunchLoaderReal == null)
		{
			return;
		}
		ModLaunch.mcLaunchLoaderReal.Abort();
		ModLaunch.McLaunchLog("已取消启动");
		try
		{
			if (ModLaunch.mcLaunchWatcher != null)
			{
				ModLaunch.mcLaunchWatcher.Kill();
			}
			else if (ModLaunch.mcLaunchProcess != null && !ModLaunch.mcLaunchProcess.HasExited)
			{
				ModLaunch.mcLaunchProcess.Kill();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Minecraft.Launch.Error.CancelProcess"), ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.CancelProcess"));
		}
	}

	private void BtnMore_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModLaunch.mcLaunchLoader.State != ModBase.LoadState.Loading)
		{
			ModInstanceList.McMcInstanceSelected.Load();
			PageInstanceLeft.McInstance = ModInstanceList.McMcInstanceSelected;
			if (File.Exists(ModInstanceList.McMcInstanceSelected.PathInstance + ".pclignore"))
			{
				HintService.Hint(Lang.Text("Launch.Home.Instance.InstallingCannotSetup"), HintType.Error);
			}
			else
			{
				ModMain.frmMain.PageChange(FormMain.PageType.InstanceSetup);
			}
		}
	}

	public void LaunchingRefresh()
	{
		try
		{
			if (ModLaunch.mcLaunchLoaderReal.State == ModBase.LoadState.Aborted)
			{
				return;
			}
			bool flag = false;
			try
			{
				bool flag2 = false;
				foreach (ModLoader.LoaderBase loader in ModLaunch.mcLaunchLoaderReal.GetLoaderList(requireShow: false))
				{
					if (loader.State == ModBase.LoadState.Loading || loader.State == ModBase.LoadState.Waiting)
					{
						LabLaunchingStage.Text = loader.name;
						flag = loader.name == StageWaitWindow || loader.name == StageEnd;
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					LabLaunchingStage.Text = Lang.Text("Launch.Status.Completed");
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "获取是否启动完成失败，可能是由于启动状态改变导致集合已修改");
				return;
			}
			if (ModAnimation.AniIsRun("Launch State Page"))
			{
				flag = false;
			}
			double progress = ModLaunch.mcLaunchLoaderReal.Progress;
			if (progress >= showProgress)
			{
				showProgress += (progress - showProgress) * 0.2 + 0.005;
			}
			if (progress <= showProgress)
			{
				showProgress = progress;
			}
			if (flag)
			{
				showProgress = 1.0;
			}
			TextBlock labLaunchingTitle = LabLaunchingTitle;
			string text;
			if (flag)
			{
				text = Lang.Text("Launch.Status.Title.Launched");
			}
			else
			{
				text = ((ModLaunch.currentLaunchOptions.SaveBatch == null) ? Lang.Text("Launch.Status.Title.Launching") : Lang.Text("Launch.Status.Title.ExportingScript"));
			}
			labLaunchingTitle.Text = text;
			LabLaunchingProgress.Text = Lang.Number<double>(showProgress, "P2");
			bool hasLaunchDownloader = false;
			try
			{
				foreach (LoaderDownload task in ModNet.NetManager.Tasks)
				{
					if (task.RealParent != null && task.RealParent.name == StageRoot && task.State == ModBase.LoadState.Loading)
					{
						hasLaunchDownloader = true;
					}
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "获取 Minecraft 启动下载器失败，可能是因为启动被取消");
				hasLaunchDownloader = false;
			}
			LabLaunchingDownload.Text = ModBase.GetString(ModNet.NetManager.Speed) + "/s";
			bool showLaunchingHint = Config.Preference.ShowLaunchingHint;
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData>
			{
				ModAnimation.AaGridLengthWidth(ProgressLaunchingFinished, showProgress - ProgressLaunchingFinished.Width.Value, 260, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaGridLengthWidth(ProgressLaunchingUnfinished, 1.0 - showProgress - ProgressLaunchingUnfinished.Width.Value, 260, 0, new ModAnimation.AniEaseOutFluent())
			};
			if (hasLaunchDownloader == (LabLaunchingDownload.Visibility == Visibility.Collapsed))
			{
				LabLaunchingDownload.Visibility = Visibility.Visible;
				LabLaunchingDownloadLeft.Visibility = Visibility.Visible;
				list.AddRange(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabLaunchingDownload, (double)(hasLaunchDownloader ? 1 : 0) - LabLaunchingDownload.Opacity, 100),
					ModAnimation.AaOpacity(LabLaunchingDownloadLeft, (hasLaunchDownloader ? 0.5 : 0.0) - LabLaunchingDownloadLeft.Opacity, 100),
					ModAnimation.AaCode(() =>
					{
						if (!hasLaunchDownloader)
						{
							LabLaunchingDownload.Visibility = Visibility.Collapsed;
							LabLaunchingDownloadLeft.Visibility = Visibility.Collapsed;
						}
					}, 110)
				});
			}
			if (!flag == (LabLaunchingProgress.Visibility == Visibility.Collapsed))
			{
				LabLaunchingProgress.Visibility = Visibility.Visible;
				LabLaunchingProgressLeft.Visibility = Visibility.Visible;
				if (flag & showLaunchingHint)
				{
					PanLaunchingHint.Visibility = Visibility.Visible;
				}
				list.AddRange(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(LabLaunchingProgress, (double)((!flag) ? 1 : 0) - LabLaunchingProgress.Opacity, 100),
					ModAnimation.AaOpacity(LabLaunchingProgressLeft, ((!flag) ? 0.5 : 0.0) - LabLaunchingProgressLeft.Opacity, 100),
					ModAnimation.AaOpacity(PanLaunchingHint, (double)((flag & showLaunchingHint) ? 1 : 0) - PanLaunchingHint.Opacity, 100)
				});
			}
			ModAnimation.AniStart(list, "Launching Progress");
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, Lang.Text("Minecraft.Launch.Error.RefreshInfo"), ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Error.RefreshInfo"));
		}
	}

	private void PanLaunchingInfo_SizeChangedW(object sender, SizeChangedEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		Size val = e.NewSize;
		double width = val.Width;
		val = e.PreviousSize;
		double value = width - val.Width;
		val = e.PreviousSize;
		if (val.Width != 0.0 && !isWidthAnimating && !(Math.Abs(value) < 1.0) && PanLaunchingInfo.ActualWidth != 0.0)
		{
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaWidth(PanLaunchingInfo, value, 180, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaCode(() =>
				{
					isWidthAnimating = false;
					PanLaunchingInfo.Width = actualUsedWidth;
				}, 0, after: true)
			}, "Launching Info Width");
			isWidthAnimating = true;
			actualUsedWidth = PanLaunchingInfo.Width;
			Grid panLaunchingInfo = PanLaunchingInfo;
			val = e.PreviousSize;
			panLaunchingInfo.Width = val.Width;
		}
	}

	private void PanLaunchingInfo_SizeChangedH(object sender, SizeChangedEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		Size val = e.NewSize;
		double height = val.Height;
		val = e.PreviousSize;
		double value = height - val.Height;
		val = e.PreviousSize;
		if (val.Height != 0.0 && !isHeightAnimating && !(Math.Abs(value) < 1.0) && PanLaunchingInfo.ActualHeight != 0.0)
		{
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaHeight(PanLaunchingInfo, value, 180, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaCode(() =>
				{
					isHeightAnimating = false;
					PanLaunchingInfo.Height = actualUsedHeight;
				}, 0, after: true)
			}, "Launching Info Height");
			isHeightAnimating = true;
			actualUsedHeight = PanLaunchingInfo.Height;
			Grid panLaunchingInfo = PanLaunchingInfo;
			val = e.PreviousSize;
			panLaunchingInfo.Height = val.Height;
		}
	}

	private void BtnLaunch_Click(object sender, MouseButtonEventArgs e)
	{
		LaunchButtonClick();
	}

	public void PageChangeToLaunching()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected I4, but got Unknown
		McProfile current = ProfileService.Current;
		switch ((current != null) ? ((SafeProfile)current).ProfileType : (ProfileType?)null)
		{
		case (ProfileType)1L:
			LabLaunchingMethod.Text = Lang.Text("Launch.Account.Type.Offline");
			break;
		case (ProfileType)0L:
			LabLaunchingMethod.Text = Lang.Text("Launch.Account.Type.Microsoft");
			break;
		case (ProfileType)2L:
		case (ProfileType)3L:
		{
			TextBlock labLaunchingMethod = LabLaunchingMethod;
			string text = Lang.Text("Launch.Account.Type.ThirdParty");
			McProfile current2 = ProfileService.Current;
			labLaunchingMethod.Text = text + ((!string.IsNullOrEmpty((current2 != null) ? current2.ServerName : null)) ? (" / " + ProfileService.Current.ServerName) : "");
			break;
		}
		}
		LabLaunchingName.Text = ModInstanceList.McMcInstanceSelected.Name;
		LabLaunchingStage.Text = Lang.Text("Common.Action.Initialize");
		LabLaunchingTitle.Text = ((ModLaunch.currentLaunchOptions?.SaveBatch == null) ? Lang.Text("Launch.Status.Title.Launching") : Lang.Text("Launch.Status.Title.ExportingScript"));
		LabLaunchingProgress.Text = Lang.Number<double>(0.0, "P2");
		LabLaunchingProgress.Opacity = 1.0;
		LabLaunchingDownload.Visibility = Visibility.Visible;
		LabLaunchingProgressLeft.Opacity = 0.6;
		LabLaunchingDownload.Visibility = Visibility.Visible;
		LabLaunchingDownload.Text = ModBase.GetString(0L) + "/s";
		LabLaunchingDownload.Opacity = 0.0;
		LabLaunchingDownload.Visibility = Visibility.Collapsed;
		LabLaunchingDownloadLeft.Opacity = 0.0;
		LabLaunchingDownloadLeft.Visibility = Visibility.Collapsed;
		ProgressLaunchingFinished.Width = new GridLength(0.0, GridUnitType.Star);
		ProgressLaunchingUnfinished.Width = new GridLength(1.0, GridUnitType.Star);
		PanLaunchingHint.Opacity = 0.0;
		PanLaunchingHint.Visibility = Visibility.Collapsed;
		PanLaunchingInfo.Width = double.NaN;
		ModLaunch.mcLaunchProcess = null;
		ModLaunch.mcLaunchWatcher = null;
		if (Config.Preference.ShowLaunchingHint)
		{
			LabLaunchingHint.Text = PageLaunchRight.GetRandomHint(enableLengthLimit: true, raw: true);
		}
		else
		{
			LabLaunchingHint.Text = "";
		}
		PanInput.IsHitTestVisible = false;
		PanLaunching.IsHitTestVisible = false;
		LoadLaunching.State.LoadingState = MyLoading.MyLoadingState.Run;
		PanLaunching.Visibility = Visibility.Visible;
		ModAnimation.AniStart(new ModAnimation.AniData[6]
		{
			ModAnimation.AaOpacity(PanInput, 0.0, 50),
			ModAnimation.AaOpacity(PanInput, 0.0 - PanInput.Opacity, 110, 0, new ModAnimation.AniEaseInFluent(), after: true),
			ModAnimation.AaScaleTransform(PanInput, 1.2 - ((ScaleTransform)PanInput.RenderTransform).ScaleX, 160),
			ModAnimation.AaOpacity(PanLaunching, 1.0 - PanLaunching.Opacity, 150, 100),
			ModAnimation.AaScaleTransform(PanLaunching, 1.0 - ((ScaleTransform)PanLaunching.RenderTransform).ScaleX, 500, 100, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaCode(() =>
			{
				PanLaunching.IsHitTestVisible = true;
			}, 150)
		}, "Launch State Page");
	}

	public void PageChangeToLogin()
	{
		if (PageGet(pageCurrent) is ILoginPage loginPage)
		{
			loginPage.Reload();
		}
		PanInput.IsHitTestVisible = false;
		PanLaunching.IsHitTestVisible = false;
		LoadLaunching.State.LoadingState = MyLoading.MyLoadingState.Stop;
		PanInput.Visibility = Visibility.Visible;
		ModAnimation.AniStart(new ModAnimation.AniData[5]
		{
			ModAnimation.AaOpacity(PanLaunching, 0.0 - PanLaunching.Opacity, 150),
			ModAnimation.AaScaleTransform(PanLaunching, 0.8 - ((ScaleTransform)PanLaunching.RenderTransform).ScaleX, 150, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaOpacity(PanInput, 1.0 - PanInput.Opacity, 250, 50),
			ModAnimation.AaScaleTransform(PanInput, 1.0 - ((ScaleTransform)PanInput.RenderTransform).ScaleX, 300, 50, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaCode(() =>
			{
				PanInput.IsHitTestVisible = true;
			}, 200)
		}, "Launch State Page", refreshTime: true);
	}

	private object PageGet(PageType type)
	{
		switch (type)
		{
		case PageType.Auth:
			if (ModMain.frmLoginAuth == null)
			{
				ModMain.frmLoginAuth = new PageLoginAuth();
			}
			return ModMain.frmLoginAuth;
		case PageType.Ms:
			if (ModMain.frmLoginMs == null)
			{
				ModMain.frmLoginMs = new PageLoginMs();
			}
			return ModMain.frmLoginMs;
		case PageType.Profile:
			if (ModMain.frmLoginProfile == null)
			{
				ModMain.frmLoginProfile = new PageLoginProfile();
			}
			return ModMain.frmLoginProfile;
		case PageType.ProfileSkin:
			if (ModMain.frmLoginProfileSkin == null)
			{
				ModMain.frmLoginProfileSkin = new PageLoginProfileSkin();
			}
			return ModMain.frmLoginProfileSkin;
		case PageType.Offline:
			if (ModMain.frmLoginOffline == null)
			{
				ModMain.frmLoginOffline = new PageLoginOffline();
			}
			return ModMain.frmLoginOffline;
		default:
			throw new ArgumentOutOfRangeException("Type", "即将切换的登录分页编号越界");
		}
	}

	private object PageChange(PageType type, bool anim)
	{
		object pageNew = ModMain.frmLoginMs;
		try
		{
			if (pageCurrent == type)
			{
				return pageNew;
			}
			pageNew = PageGet(type);
			ModAnimation.AniStop("FrmLogin PageChange");
			if (pageNew is FrameworkElement { Parent: not null } frameworkElement)
			{
				((DependencyObject)frameworkElement).SetValue(ContentPresenter.ContentProperty, (object)null);
			}
			if (anim)
			{
				((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
				{
					ModAnimation.AniStart(new ModAnimation.AniData[3]
					{
						ModAnimation.AaOpacity(PanLogin, 0.0 - PanLogin.Opacity, 100, 0, new ModAnimation.AniEaseOutFluent()),
						ModAnimation.AaCode(() =>
						{
							ModAnimation.AniControlEnabled++;
							PanLogin.Children.Clear();
							PanLogin.Children.Add((UIElement)pageNew);
							ModAnimation.AniControlEnabled--;
						}, 100),
						ModAnimation.AaOpacity(PanLogin, 1.0, 100, 120, new ModAnimation.AniEaseInFluent())
					}, "FrmLogin PageChange");
				}), (DispatcherPriority)7);
			}
			else
			{
				ModAnimation.AniControlEnabled++;
				PanLogin.Children.Clear();
				PanLogin.Children.Add((UIElement)pageNew);
				ModAnimation.AniControlEnabled--;
			}
			pageCurrent = type;
			return pageNew;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Launch.Account.Error.SwitchPage", new object[1] { ModBase.GetStringFromEnum(type) }), ModBase.LogLevel.Feedback, null, Lang.Text("Launch.Account.Error.SwitchPage", new object[1] { ModBase.GetStringFromEnum(type) }));
			return pageNew;
		}
	}

	public void RefreshPage(bool anim, ModLaunch.McLoginType targetLoginType = (ModLaunch.McLoginType)0)
	{
		PageType pageType = PageType.None;
		if (targetLoginType != 0)
		{
			if (targetLoginType == ModLaunch.McLoginType.Ms)
			{
				pageType = PageType.Ms;
			}
			if (targetLoginType == ModLaunch.McLoginType.Auth)
			{
				pageType = PageType.Auth;
			}
			if (targetLoginType == ModLaunch.McLoginType.Legacy)
			{
				pageType = PageType.Offline;
			}
		}
		else if (ProfileService.Current != null)
		{
			pageType = PageType.ProfileSkin;
			BtnLaunch.IsEnabled = true;
		}
		else
		{
			pageType = PageType.Profile;
			if (_launchButtonAction != LaunchButtonAction.Download)
			{
				BtnLaunch.IsEnabled = false;
			}
		}
		if (pageCurrent != pageType)
		{
			PageChange(pageType, anim);
		}
	}

	private static ModBase.EqualableList<string> SkinMsInput()
	{
		ModBase.EqualableList<string> equalableList = new ModBase.EqualableList<string>();
		McProfile current = ProfileService.Current;
		equalableList.Add(((current != null) ? ((SafeProfile)current).UserName : null) ?? "");
		McProfile current2 = ProfileService.Current;
		equalableList.Add(((current2 != null) ? ((SafeProfile)current2).Uuid : null) ?? "");
		return equalableList;
	}

	private static void SkinMsLoad(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> data)
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.frmLoginProfileSkin != null && ModMain.frmLoginProfileSkin.Skin != null)
			{
				ModMain.frmLoginProfileSkin.Skin.Clear();
			}
		});
		string text = data.input[0];
		string text2 = data.input[1];
		if (ProfileService.Current != null)
		{
			text = ((SafeProfile)ProfileService.Current).UserName;
			text2 = ((SafeProfile)ProfileService.Current).Uuid;
		}
		if (string.IsNullOrEmpty(text))
		{
			data.output = ModBase.pathImage + "Skins/" + ModSkin.McSkinSex(ProfileUi.GetOfflineUuid(text)) + ".png";
			ModBase.Log("[Minecraft] 获取微软正版皮肤失败，ID 为空");
		}
		else
		{
			try
			{
				string address = ModSkin.McSkinGetAddress(text2, "Ms");
				if (data.IsAborted)
				{
					throw new ThreadInterruptedException("当前任务已取消：" + text);
				}
				address = ModSkin.McSkinDownload(address);
				if (data.IsAborted)
				{
					throw new ThreadInterruptedException("当前任务已取消：" + text);
				}
				data.output = address;
			}
			catch (Exception ex)
			{
				if (ex is ThreadInterruptedException)
				{
					data.output = "";
					ModBase.Log("[Minecraft] 已取消皮肤获取：" + text);
					return;
				}
				if (ex.ToString().Contains("429"))
				{
					data.output = ModBase.pathImage + "Skins/" + ModSkin.McSkinSex(ProfileUi.GetOfflineUuid(text)) + ".png";
					ModBase.Log(Lang.Text("Launch.Skin.Error.MsRateLimited", new object[1] { text }), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Error.MsRateLimited", new object[1] { text }));
				}
				else if (ex.ToString().Contains("未设置自定义皮肤"))
				{
					data.output = ModBase.pathImage + "Skins/" + ModSkin.McSkinSex(ProfileUi.GetOfflineUuid(text)) + ".png";
					ModBase.Log("[Minecraft] 用户未设置自定义皮肤，跳过皮肤加载");
				}
				else
				{
					data.output = ModBase.pathImage + "Skins/" + ModSkin.McSkinSex(ProfileUi.GetOfflineUuid(text)) + ".png";
					ModBase.Log(ex, Lang.Text("Launch.Skin.Error.MsGet", new object[1] { text }), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Error.MsGet", new object[1] { text }));
				}
			}
		}
		if (ModMain.frmLoginProfileSkin != null && ModMain.frmLoginProfileSkin.Skin.loader == data)
		{
			ModBase.RunInUi(ModMain.frmLoginProfileSkin.Skin.Load);
		}
		else if (!data.IsAborted)
		{
			data.input = null;
		}
	}

	private static ModBase.EqualableList<string> SkinLegacyInput()
	{
		ModBase.EqualableList<string> equalableList = new ModBase.EqualableList<string>();
		McProfile current = ProfileService.Current;
		equalableList.Add(((current != null) ? ((SafeProfile)current).UserName : null) ?? "");
		McProfile current2 = ProfileService.Current;
		equalableList.Add(((current2 != null) ? ((SafeProfile)current2).Uuid : null) ?? "");
		return equalableList;
	}

	private static void SkinLegacyLoad(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> data)
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.frmLoginProfileSkin != null && ModMain.frmLoginProfileSkin.Skin != null)
			{
				ModMain.frmLoginProfileSkin.Skin.Clear();
			}
		});
		data.output = ModBase.pathImage + "Skins/" + ModSkin.McSkinSex(data.input[1]) + ".png";
		if (ModMain.frmLoginProfileSkin != null && ModMain.frmLoginProfileSkin.Skin.loader == data)
		{
			ModBase.RunInUi(() =>
			{
				ModMain.frmLoginProfileSkin.Skin.Load();
			});
		}
		else if (!data.IsAborted)
		{
			data.input = null;
		}
	}

	private static ModBase.EqualableList<string> SkinAuthInput()
	{
		ModBase.EqualableList<string> equalableList = new ModBase.EqualableList<string>();
		McProfile current = ProfileService.Current;
		equalableList.Add(((current != null) ? ((SafeProfile)current).UserName : null) ?? "");
		McProfile current2 = ProfileService.Current;
		equalableList.Add(((current2 != null) ? ((SafeProfile)current2).Uuid : null) ?? "");
		return equalableList;
	}

	private static void SkinAuthLoad(ModLoader.LoaderTask<ModBase.EqualableList<string>, string> data)
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.frmLoginProfileSkin != null && ModMain.frmLoginProfileSkin.Skin != null)
			{
				ModMain.frmLoginProfileSkin.Skin.Clear();
			}
		});
		string text = data.input[0];
		string text2 = data.input[1];
		if (string.IsNullOrEmpty(text))
		{
			data.output = ModBase.pathImage + "Skins/Steve.png";
			ModBase.Log("[Minecraft] 获取 Authlib-Injector 皮肤失败，ID 为空");
		}
		else
		{
			try
			{
				string address = ModSkin.McSkinGetAddress(text2, "Auth");
				if (data.IsAborted)
				{
					throw new ThreadInterruptedException("当前任务已取消：" + text);
				}
				address = ModSkin.McSkinDownload(address);
				if (data.IsAborted)
				{
					throw new ThreadInterruptedException("当前任务已取消：" + text);
				}
				data.output = address;
			}
			catch (Exception ex)
			{
				if (ex is ThreadInterruptedException)
				{
					data.output = "";
					return;
				}
				if (ex.ToString().Contains("429"))
				{
					data.output = ModBase.pathImage + "Skins/Steve.png";
					ModBase.Log("[Minecraft] 获取 Authlib-Injector 皮肤失败（" + text + "）：获取皮肤太过频繁，请 5 分钟后再试！", ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Error.AuthlibRateLimited"));
				}
				else if (ex.ToString().Contains("未设置自定义皮肤"))
				{
					data.output = ModBase.pathImage + "Skins/Steve.png";
					ModBase.Log("[Minecraft] 用户未设置自定义皮肤，跳过皮肤加载");
				}
				else
				{
					data.output = ModBase.pathImage + "Skins/Steve.png";
					ModBase.Log(ex, Lang.Text("Launch.Skin.Error.AuthGet", new object[1] { text }), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.Error.AuthGet", new object[1] { text }));
				}
			}
		}
		if (ModMain.frmLoginProfileSkin != null && ModMain.frmLoginProfileSkin.Skin.loader == data)
		{
			ModBase.RunInUi(ModMain.frmLoginProfileSkin.Skin.Load);
		}
		else if (!data.IsAborted)
		{
			data.input = null;
		}
	}

}
