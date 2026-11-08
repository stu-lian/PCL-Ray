using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public partial class PageInstanceInstall : MyPageRight, IComponentConnector
{
	private enum InstallAction
	{
		Modify,
		Reset
	}

	private bool isLoad;

	private string? lastVersionName;

	private InstallAction _installAction;

	public bool isInSelectPage;

	private bool isFirstLoaded;

	private string? _vanillaName;

	private JsonObject? _vanillaData;

	private string? _vanillaIcon;

	private ModDownload.DlOptiFineListEntry? selectedOptiFine;

	private ModLoaderType? _selectedLoaderType;

	private string? selectedAPIName;

	private ModDownload.DlLiteLoaderListEntry? selectedLiteLoader;

	private ModDownload.DlForgeVersionEntry? selectedForge;

	private ModDownload.DlCleanroomListEntry? selectedCleanroom;

	private string? selectedCleanroomVersion;

	private ModDownload.DlNeoForgeListEntry? selectedNeoForge;

	private string? selectedNeoForgeVersion;

	private string? selectedFabric;

	private ModComp.CompFile? selectedFabricApi;

	private string? selectedLegacyFabric;

	private ModComp.CompFile? selectedLegacyFabricApi;

	private string? selectedLabyModChannel;

	private string? selectedLabyModCommitRef;

	private string? selectedLabyModVersion;

	private ModComp.CompFile? selectedOptiFabric;

	private bool _ReloadSelected_Ongoing;

	private ModComp.CompFile? _currentFabricApi;

	private string? _currentFabricApiPath;

	private ModComp.CompFile? _currentLegacyFabricApi;

	private string? _currentLegacyFabricApiPath;

	private ModComp.CompFile? _currentOptiFabric;

	private string? _currentOptiFabricPath;

	private string? currentInfo;

	public static string? mcVersionWaitingForSelect;

	private bool autoSelectedFabricApi;

	private bool autoSelectedLegacyFabricApi;

	private bool autoSelectedOptiFabric;

	private int VanillaDrop => McInstanceInfo.VersionToDrop(_vanillaName, allowSnapshot: true);

	public PageInstanceInstall()
	{
		Initialized += (object? a, EventArgs b) =>
		{
			LoaderInit();
		};
		Loaded += (object a, RoutedEventArgs b) =>
		{
			Init();
		};
		InitializeComponent();
		LoadMinecraft.Text = Lang.Text("Download.Version.LoadingList");
	}

	private void LoaderInit()
	{
		disabledPageAnimControls.Add(BtnSelectStart);
		PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, null, ModDownload.dlClientListLoader, (ModLoader.LoaderBase _) =>
		{
			GetCurrentInfo();
		});
		LoadOptiFine.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			OptiFine_Loaded();
			ReloadSelected();
		};
		LoadLiteLoader.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LiteLoader_Loaded();
			ReloadSelected();
		};
		LoadForge.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			Forge_Loaded();
			ReloadSelected();
		};
		LoadNeoForge.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			NeoForge_Loaded();
			ReloadSelected();
		};
		LoadCleanroom.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			Cleanroom_Loaded();
			ReloadSelected();
		};
		LoadFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			Fabric_Loaded();
			ReloadSelected();
		};
		LoadFabricApi.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			FabricApi_Loaded();
			ReloadSelected();
		};
		LoadLegacyFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LegacyFabric_Loaded();
			ReloadSelected();
		};
		LoadLegacyFabricApi.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LegacyFabricApi_Loaded();
			ReloadSelected();
		};
		LoadOptiFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			OptiFabric_Loaded();
			ReloadSelected();
		};
		LoadLabyMod.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LabyMod_Loaded();
			ReloadSelected();
		};
		PageExit += () =>
		{
			isInSelectPage = false;
		};
	}

	private void Init()
	{
		PanBack.ScrollToHome();
		GetCurrentInfo();
		bool isForceRestart = lastVersionName == null || (lastVersionName ?? "") != (_vanillaName ?? "");
		lastVersionName = _vanillaName;
		ModDownload.dlOptiFineListLoader.Start(null, isForceRestart);
		ModDownload.dlLiteLoaderListLoader.Start(null, isForceRestart);
		ModDownload.dlFabricListLoader.Start(null, isForceRestart);
		ModDownload.dlNeoForgeListLoader.Start(null, isForceRestart);
		ModDownload.dlCleanroomListLoader.Start(null, isForceRestart);
		ModDownload.dlLabyModListLoader.Start(null, isForceRestart);
		ModDownload.dlLegacyFabricListLoader.Start(null, isForceRestart);
		ModDownload.dlFabricApiLoader.Start(null, isForceRestart);
		ModDownload.dlLegacyFabricApiLoader.Start(null, isForceRestart);
		ModDownload.dlOptiFabricLoader.Start(null, isForceRestart);
		if (isLoad)
		{
			ReloadSelected();
			return;
		}
		isLoad = true;
		ModDownloadLib.McDownloadForgeRecommendedRefresh();
		LoadOptiFine.State = ModDownload.dlOptiFineListLoader;
		LoadLiteLoader.State = ModDownload.dlLiteLoaderListLoader;
		LoadFabric.State = ModDownload.dlFabricListLoader;
		LoadFabricApi.State = ModDownload.dlFabricApiLoader;
		LoadNeoForge.State = ModDownload.dlNeoForgeListLoader;
		LoadCleanroom.State = ModDownload.dlCleanroomListLoader;
		LoadOptiFabric.State = ModDownload.dlOptiFabricLoader;
		LoadLabyMod.State = ModDownload.dlLabyModListLoader;
		LoadLegacyFabric.State = ModDownload.dlLegacyFabricListLoader;
		LoadLegacyFabricApi.State = ModDownload.dlLegacyFabricApiLoader;
	}

	private void BtnSelectStart_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-loader-install"))
		{
			return;
		}
		if (PageInstanceLeft.McInstance.Info.HasQuilt)
		{
			HintService.Hint(Lang.Text("Instance.Overall.Reset.QuiltUnsupported"));
			return;
		}
		ModLoaderType? selectedLoaderType = _selectedLoaderType;
		if ((selectedLoaderType.HasValue && (Config.Launch.IndieSolutionV2 == 0 || Config.Launch.IndieSolutionV2 == 2) && ModMain.MyMsgBox(Lang.Text("Download.Install.InstanceIsolation.Warning.Message"), Lang.Text("Download.Install.InstanceIsolation.Warning.Title"), Lang.Text("Download.Install.InstanceIsolation.Warning.Cancel"), Lang.Text("Download.Install.InstanceIsolation.Warning.Continue")) == 1) || (_installAction == InstallAction.Reset && ModMain.MyMsgBox(Lang.Text("Instance.Install.Reset.Message"), Lang.Text("Instance.Install.Reset.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) == 2))
		{
			return;
		}
		if ((PageInstanceLeft.McInstance.PathIndie ?? "") != (PageInstanceLeft.McInstance.PathInstance ?? "") && PageInstanceLeft.McInstance.Info.HasLabyMod)
		{
			Directory.Delete(System.IO.Path.Combine(PageInstanceLeft.McInstance.PathIndie, "labymod-neo"), recursive: true);
		}
		ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".json", PageInstanceLeft.McInstance.PathInstance + "PCLInstallBackups\\" + PageInstanceLeft.McInstance.Name + ".json");
		if (File.Exists(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar"))
		{
			ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar", PageInstanceLeft.McInstance.PathInstance + "PCLInstallBackups\\" + PageInstanceLeft.McInstance.Name + ".jar");
		}
		ModComp.CompFile? compFile = selectedFabricApi;
		if (compFile != null && compFile.Equals(_currentFabricApi))
		{
			selectedFabricApi = null;
		}
		ModComp.CompFile? compFile2 = selectedLegacyFabricApi;
		if (compFile2 != null && compFile2.Equals(_currentLegacyFabricApi))
		{
			selectedLegacyFabricApi = null;
		}
		ModComp.CompFile? compFile3 = selectedOptiFabric;
		if (compFile3 != null && compFile3.Equals(_currentOptiFabric))
		{
			selectedOptiFabric = null;
		}
		ModDownloadLib.McInstallRequest request = new ModDownloadLib.McInstallRequest
		{
			targetInstanceName = PageInstanceLeft.McInstance.Name,
			targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + PageInstanceLeft.McInstance.Name + "\\",
			minecraftJson = _vanillaData?["url"].ToString(),
			minecraftName = _vanillaName,
			optiFineEntry = selectedOptiFine,
			forgeEntry = selectedForge,
			neoForgeEntry = selectedNeoForge,
			neoForgeVersion = selectedNeoForgeVersion,
			cleanroomEntry = selectedCleanroom,
			cleanroomVersion = selectedCleanroomVersion,
			fabricVersion = selectedFabric,
			fabricApi = selectedFabricApi,
			optiFabric = selectedOptiFabric,
			liteLoaderEntry = selectedLiteLoader,
			labyModChannel = selectedLabyModChannel,
			labyModCommitRef = selectedLabyModCommitRef,
			legacyFabricVersion = selectedLegacyFabric,
			legacyFabricApi = selectedLegacyFabricApi
		};
		BtnSelectStart.IsEnabled = false;
		if (ModDownloadLib.McInstall(request, (_installAction == InstallAction.Modify) ? Lang.Text("Instance.Install.Action.ModifyLabel") : Lang.Text("Common.Action.Reset")))
		{
			if (selectedFabricApi != null && _currentFabricApiPath != null)
			{
				File.Delete(_currentFabricApiPath);
			}
			if (selectedLegacyFabricApi != null && _currentLegacyFabricApiPath != null)
			{
				File.Delete(_currentLegacyFabricApiPath);
			}
			if (selectedOptiFabric != null && _currentOptiFabricPath != null)
			{
				File.Delete(_currentOptiFabricPath);
			}
			ModMain.frmMain.PageChange(new FormMain.PageStackData
			{
				page = FormMain.PageType.Launch
			});
		}
	}

	private string GetLoaderError(MyLoading loader)
	{
		if (loader == null || !loader.State.IsLoader)
		{
			return Lang.Text("Download.Install.State.Getting");
		}
		switch (loader.State.LoadingState)
		{
		case MyLoading.MyLoadingState.Run:
			return Lang.Text("Download.Install.State.Getting");
		case MyLoading.MyLoadingState.Error:
		{
			string message = ((ModLoader.LoaderBase)loader.State).Error.Message;
			if (!(message == Lang.Text("Download.Install.State.NoVersion")))
			{
				return Lang.Text("Download.Install.State.GetFailed", new object[1] { message });
			}
			return Lang.Text("Download.Install.State.NoVersion");
		}
		case MyLoading.MyLoadingState.Unloaded:
			return Lang.Text("Download.Install.State.UnknownUnloaded");
		default:
			return null;
		}
	}

	private void EnterSelectPage()
	{
		if (isInSelectPage)
		{
			return;
		}
		isInSelectPage = true;
		disabledPageAnimControls.Remove(BtnSelectStart);
		BtnSelectStart.Show = true;
		autoSelectedFabricApi = false;
		autoSelectedOptiFabric = false;
		PanSelect.Visibility = Visibility.Visible;
		PanSelect.IsHitTestVisible = true;
		PanMinecraft.IsHitTestVisible = false;
		PanBack.IsHitTestVisible = false;
		PanBack.ScrollToHome();
		CardMinecraft.IsSwapped = true;
		CardOptiFine.IsSwapped = true;
		CardLiteLoader.IsSwapped = true;
		CardForge.IsSwapped = true;
		CardNeoForge.IsSwapped = true;
		CardCleanroom.IsSwapped = true;
		CardFabric.IsSwapped = true;
		CardFabricApi.IsSwapped = true;
		CardOptiFabric.IsSwapped = true;
		CardLabyMod.IsSwapped = true;
		CardLegacyFabric.IsSwapped = true;
		CardLegacyFabricApi.IsSwapped = true;
		if (!States.Hint.InstallPageBack)
		{
			States.Hint.InstallPageBack = true;
			HintService.Hint(Lang.Text("Download.Install.Hint.MinecraftBack"));
		}
		foreach (FrameworkElement allAnimControl in GetAllAnimControls(PanSelect))
		{
			allAnimControl.Opacity = 1.0;
			allAnimControl.RenderTransform = new TranslateTransform();
		}
		if (McInstanceInfo.IsFormatFit(_vanillaName))
		{
			ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>> loaderTask = new ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>("DlForgeVersion " + _vanillaName, ModDownload.DlForgeVersionMain);
			LoadForge.State = loaderTask;
			loaderTask.Start(_vanillaName);
		}
		ModDownload.dlFabricApiLoader.Start();
		ModDownload.dlLegacyFabricApiLoader.Start();
		ModDownload.dlOptiFabricLoader.Start();
		ModAnimation.AniStart(new ModAnimation.AniData[4]
		{
			ModAnimation.AaOpacity(PanMinecraft, 0.0 - PanMinecraft.Opacity, 100, 10),
			ModAnimation.AaCode(() =>
			{
				PanBack.ScrollToHome();
				OptiFine_Loaded();
				LiteLoader_Loaded();
				Forge_Loaded();
				NeoForge_Loaded();
				Cleanroom_Loaded();
				Fabric_Loaded();
				LegacyFabric_Loaded();
				FabricApi_Loaded();
				LegacyFabricApi_Loaded();
				LabyMod_Loaded();
				OptiFabric_Loaded();
				ReloadSelected();
			}, 0, after: true),
			ModAnimation.AaOpacity(PanSelect, 1.0 - PanSelect.Opacity, 250, 150),
			ModAnimation.AaCode(() =>
			{
				PanMinecraft.Visibility = Visibility.Collapsed;
				PanBack.IsHitTestVisible = true;
				if (!isFirstLoaded)
				{
					isFirstLoaded = true;
					BtnOptiFineClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardOptiFine.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnLiteLoaderClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardLiteLoader.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnForgeClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardForge.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnLegacyFabricClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardLegacyFabric.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnNeoForgeClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardNeoForge.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnCleanroomClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardCleanroom.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnFabricClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardFabric.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnLegacyFabricApiClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardLegacyFabricApi.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnFabricApiClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardFabricApi.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnLabyModClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardLabyMod.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnOptiFabricClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardOptiFabric.MainTextBlock,
						Mode = BindingMode.OneWay
					});
				}
			}, 0, after: true)
		}, "FrmInstanceInstall SelectPageSwitch", refreshTime: true);
	}

	public void ExitSelectPage()
	{
		if (isInSelectPage)
		{
			isInSelectPage = false;
			LoadMinecraft_OnFinish();
			disabledPageAnimControls.Add(BtnSelectStart);
			BtnSelectStart.Show = false;
			ClearSelected();
			PanMinecraft.Visibility = Visibility.Visible;
			PanSelect.IsHitTestVisible = false;
			PanMinecraft.IsHitTestVisible = true;
			PanBack.IsHitTestVisible = false;
			PanBack.ScrollToHome();
			ModAnimation.AniStart(new ModAnimation.AniData[4]
			{
				ModAnimation.AaOpacity(PanSelect, 0.0 - PanSelect.Opacity, 90, 10),
				ModAnimation.AaCode(() =>
				{
					PanBack.ScrollToHome();
				}, 0, after: true),
				ModAnimation.AaOpacity(PanMinecraft, 1.0 - PanMinecraft.Opacity, 150, 100),
				ModAnimation.AaCode(() =>
				{
					PanSelect.Visibility = Visibility.Collapsed;
					PanBack.IsHitTestVisible = true;
				}, 0, after: true)
			}, "FrmInstanceInstall SelectPageSwitch");
		}
	}

	public void MinecraftSelected(MyListItem sender, MouseButtonEventArgs e)
	{
		_vanillaName = sender.Title;
		_vanillaData = (JsonObject)sender.Tag;
		_vanillaIcon = sender.Logo;
		EnterSelectPage();
	}

	private void CardMinecraft_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		ExitSelectPage();
		e.handled = true;
	}

	private void ReloadSelected()
	{
		if (_vanillaName == null || _ReloadSelected_Ongoing)
		{
			return;
		}
		_ReloadSelected_Ongoing = true;
		try
		{
			string selectInfo = GetSelectInfo();
			ItemSelect.Title = PageInstanceLeft.McInstance.Name;
			ItemSelect.Logo = GetSelectLogo();
			BtnSelectStart.IsEnabled = true;
			if ((selectInfo ?? "") == (currentInfo ?? ""))
			{
				ItemSelect.Info = selectInfo;
				BtnSelectStart.Text = Lang.Text("Instance.Install.Action.StartReset");
				_installAction = InstallAction.Reset;
				BtnSelectStart.SvgIcon = "lucide/rotate-ccw";
			}
			else
			{
				ItemSelect.Info = currentInfo + " → " + selectInfo;
				BtnSelectStart.Text = Lang.Text("Instance.Install.Action.StartModify");
				_installAction = InstallAction.Modify;
				BtnSelectStart.SvgIcon = "lucide/pencil";
			}
			ImgMinecraft.Source = new MyBitmap(_vanillaIcon);
			LabMinecraft.Text = _vanillaName;
			LabMinecraft.Foreground = ThemeManager.colorGray1;
			if (!McVersionComparer.CompareVersionGe(_vanillaName, "1.7.2"))
			{
				CardOptiFine.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardOptiFine.Visibility = Visibility.Visible;
				string text = LoadOptiFineGetError();
				CardOptiFine.MainSwap.Visibility = ((text != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text != null)
				{
					CardOptiFine.IsSwapped = true;
				}
				SetPanelVisibility(PanOptiFineInfo, CardOptiFine.IsSwapped);
				if (selectedOptiFine == null)
				{
					BtnOptiFineClear.Visibility = Visibility.Collapsed;
					ImgOptiFine.Visibility = Visibility.Collapsed;
					LabOptiFine.Text = text ?? Lang.Text("Download.Install.State.CanAdd");
					LabOptiFine.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnOptiFineClear.Visibility = Visibility.Visible;
					ImgOptiFine.Visibility = Visibility.Visible;
					LabOptiFine.Text = selectedOptiFine.DisplayName.Replace(_vanillaName + " ", "");
					LabOptiFine.Foreground = ThemeManager.colorGray1;
				}
			}
			if (!McInstanceInfo.IsFormatFit(_vanillaName) || !McVersionComparer.CompareVersionGe(_vanillaName, "1.5.2") || !McVersionComparer.CompareVersionGe("1.12.2", _vanillaName))
			{
				CardLiteLoader.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardLiteLoader.Visibility = Visibility.Visible;
				string text2 = LoadLiteLoaderGetError();
				CardLiteLoader.MainSwap.Visibility = ((text2 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text2 != null)
				{
					CardLiteLoader.IsSwapped = true;
				}
				SetPanelVisibility(PanLiteLoaderInfo, CardLiteLoader.IsSwapped);
				if (selectedLiteLoader == null)
				{
					BtnLiteLoaderClear.Visibility = Visibility.Collapsed;
					ImgLiteLoader.Visibility = Visibility.Collapsed;
					LabLiteLoader.Text = text2 ?? Lang.Text("Download.Install.State.CanAdd");
					LabLiteLoader.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnLiteLoaderClear.Visibility = Visibility.Visible;
					ImgLiteLoader.Visibility = Visibility.Visible;
					LabLiteLoader.Text = selectedLiteLoader.Inherit;
					LabLiteLoader.Foreground = ThemeManager.colorGray1;
				}
			}
			if (!McInstanceInfo.IsFormatFit(_vanillaName) || !McVersionComparer.CompareVersionGe(_vanillaName, "1.1"))
			{
				CardForge.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardForge.Visibility = Visibility.Visible;
				string text3 = LoadForgeGetError();
				CardForge.MainSwap.Visibility = ((text3 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text3 != null)
				{
					CardForge.IsSwapped = true;
				}
				SetPanelVisibility(PanForgeInfo, CardForge.IsSwapped);
				if (selectedForge == null)
				{
					BtnForgeClear.Visibility = Visibility.Collapsed;
					ImgForge.Visibility = Visibility.Collapsed;
					LabForge.Text = text3 ?? Lang.Text("Download.Install.State.CanAdd");
					LabForge.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnForgeClear.Visibility = Visibility.Visible;
					ImgForge.Visibility = Visibility.Visible;
					LabForge.Text = selectedForge.VersionName;
					LabForge.Foreground = ThemeManager.colorGray1;
				}
			}
			if (_vanillaName == "1.12.2")
			{
				CardCleanroom.Visibility = Visibility.Visible;
				string text4 = LoadCleanroomGetError();
				CardCleanroom.MainSwap.Visibility = ((text4 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text4 != null)
				{
					CardCleanroom.IsSwapped = true;
				}
				SetPanelVisibility(PanCleanroomInfo, CardCleanroom.IsSwapped);
				if (selectedCleanroom == null)
				{
					BtnCleanroomClear.Visibility = Visibility.Collapsed;
					ImgCleanroom.Visibility = Visibility.Collapsed;
					LabCleanroom.Text = text4 ?? Lang.Text("Download.Install.State.CanAdd");
					LabCleanroom.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnCleanroomClear.Visibility = Visibility.Visible;
					ImgCleanroom.Visibility = Visibility.Visible;
					LabCleanroom.Text = selectedCleanroom.VersionName;
					LabCleanroom.Foreground = ThemeManager.colorGray1;
				}
			}
			else
			{
				CardCleanroom.Visibility = Visibility.Collapsed;
			}
			if (!McVersionComparer.CompareVersionGe(_vanillaName, "1.20.1"))
			{
				CardNeoForge.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardNeoForge.Visibility = Visibility.Visible;
				string text5 = LoadNeoForgeGetError();
				CardNeoForge.MainSwap.Visibility = ((text5 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text5 != null)
				{
					CardNeoForge.IsSwapped = true;
				}
				SetPanelVisibility(PanNeoForgeInfo, CardNeoForge.IsSwapped);
				if (selectedNeoForge == null)
				{
					BtnNeoForgeClear.Visibility = Visibility.Collapsed;
					ImgNeoForge.Visibility = Visibility.Collapsed;
					LabNeoForge.Text = text5 ?? Lang.Text("Download.Install.State.CanAdd");
					LabNeoForge.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnNeoForgeClear.Visibility = Visibility.Visible;
					ImgNeoForge.Visibility = Visibility.Visible;
					LabNeoForge.Text = selectedNeoForge.VersionName;
					LabNeoForge.Foreground = ThemeManager.colorGray1;
				}
			}
			if (VanillaDrop < 130 || (VanillaDrop == 130 && !McVersionComparer.CompareVersionGe(_vanillaName, "18w43b")))
			{
				CardFabric.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardFabric.Visibility = Visibility.Visible;
				string text6 = LoadFabricGetError();
				CardFabric.MainSwap.Visibility = ((text6 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text6 != null)
				{
					CardFabric.IsSwapped = true;
				}
				SetPanelVisibility(PanFabricInfo, CardFabric.IsSwapped);
				if (selectedFabric == null)
				{
					BtnFabricClear.Visibility = Visibility.Collapsed;
					ImgFabric.Visibility = Visibility.Collapsed;
					LabFabric.Text = text6 ?? Lang.Text("Download.Install.State.CanAdd");
					LabFabric.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnFabricClear.Visibility = Visibility.Visible;
					ImgFabric.Visibility = Visibility.Visible;
					LabFabric.Text = selectedFabric.Replace("+build", "");
					LabFabric.Foreground = ThemeManager.colorGray1;
				}
			}
			if (selectedFabric == null)
			{
				CardFabricApi.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardFabricApi.Visibility = Visibility.Visible;
				string text7 = LoadFabricApiGetError();
				CardFabricApi.MainSwap.Visibility = ((text7 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text7 != null || selectedFabric == null)
				{
					CardFabricApi.IsSwapped = true;
				}
				SetPanelVisibility(PanFabricApiInfo, CardFabricApi.IsSwapped);
				if (selectedFabricApi == null)
				{
					BtnFabricApiClear.Visibility = Visibility.Collapsed;
					ImgFabricApi.Visibility = Visibility.Collapsed;
					LabFabricApi.Text = text7 ?? Lang.Text("Download.Install.State.CanAdd");
					LabFabricApi.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnFabricApiClear.Visibility = Visibility.Visible;
					ImgFabricApi.Visibility = Visibility.Visible;
					LabFabricApi.Text = selectedFabricApi.DisplayName.Split("]")[1].Replace("Fabric API ", "").Replace(" build ", ".").Split("+")
						.First()
						.Trim();
					LabFabricApi.Foreground = ThemeManager.colorGray1;
				}
			}
			if (VanillaDrop < 30 || VanillaDrop > 130)
			{
				CardLegacyFabric.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardLegacyFabric.Visibility = Visibility.Visible;
				string text8 = LoadLegacyFabricGetError();
				CardLegacyFabric.MainSwap.Visibility = ((text8 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text8 != null)
				{
					CardLegacyFabric.IsSwapped = true;
				}
				SetPanelVisibility(PanLegacyFabricInfo, CardLegacyFabric.IsSwapped);
				if (selectedLegacyFabric == null)
				{
					BtnLegacyFabricClear.Visibility = Visibility.Collapsed;
					ImgLegacyFabric.Visibility = Visibility.Collapsed;
					LabLegacyFabric.Text = text8 ?? Lang.Text("Download.Install.State.CanAdd");
					LabLegacyFabric.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnLegacyFabricClear.Visibility = Visibility.Visible;
					ImgLegacyFabric.Visibility = Visibility.Visible;
					LabLegacyFabric.Text = selectedLegacyFabric.Replace("+build", "");
					LabLegacyFabric.Foreground = ThemeManager.colorGray1;
				}
			}
			if (selectedLegacyFabric == null)
			{
				CardLegacyFabricApi.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardLegacyFabricApi.Visibility = Visibility.Visible;
				string text9 = LoadLegacyFabricApiGetError();
				CardLegacyFabricApi.MainSwap.Visibility = ((text9 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text9 != null || selectedLegacyFabric == null)
				{
					CardLegacyFabricApi.IsSwapped = true;
				}
				SetPanelVisibility(PanLegacyFabricApiInfo, CardLegacyFabricApi.IsSwapped);
				if (selectedLegacyFabricApi == null)
				{
					BtnLegacyFabricApiClear.Visibility = Visibility.Collapsed;
					ImgLegacyFabricApi.Visibility = Visibility.Collapsed;
					LabLegacyFabricApi.Text = text9 ?? Lang.Text("Download.Install.State.CanAdd");
					LabLegacyFabricApi.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnLegacyFabricApiClear.Visibility = Visibility.Visible;
					ImgLegacyFabricApi.Visibility = Visibility.Visible;
					LabLegacyFabricApi.Text = selectedLegacyFabricApi.DisplayName.Replace("Legacy Fabric API ", "");
					LabLegacyFabricApi.Foreground = ThemeManager.colorGray1;
				}
			}
			if (!McInstanceInfo.IsFormatFit(_vanillaName) || !McVersionComparer.CompareVersionGe(_vanillaName, "1.8.9"))
			{
				CardLabyMod.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardLabyMod.Visibility = Visibility.Visible;
				string text10 = LoadLabyModGetError();
				CardLabyMod.MainSwap.Visibility = ((text10 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text10 != null)
				{
					CardLabyMod.IsSwapped = true;
				}
				SetPanelVisibility(PanLabyModInfo, CardLabyMod.IsSwapped);
				if (selectedLabyModVersion == null)
				{
					BtnLabyModClear.Visibility = Visibility.Collapsed;
					ImgLabyMod.Visibility = Visibility.Collapsed;
					LabLabyMod.Text = text10 ?? Lang.Text("Download.Install.State.CanAdd");
					LabLabyMod.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnLabyModClear.Visibility = Visibility.Visible;
					ImgLabyMod.Visibility = Visibility.Visible;
					LabLabyMod.Text = selectedLabyModVersion;
					LabLabyMod.Foreground = ThemeManager.colorGray1;
				}
			}
			if (selectedFabric == null || selectedOptiFine == null)
			{
				CardOptiFabric.Visibility = Visibility.Collapsed;
			}
			else
			{
				CardOptiFabric.Visibility = Visibility.Visible;
				string text11 = LoadOptiFabricGetError();
				CardOptiFabric.MainSwap.Visibility = ((text11 != null) ? Visibility.Collapsed : Visibility.Visible);
				if (text11 != null || selectedFabric == null)
				{
					CardOptiFabric.IsSwapped = true;
				}
				SetPanelVisibility(PanOptiFabricInfo, CardOptiFabric.IsSwapped);
				if (selectedOptiFabric == null)
				{
					BtnOptiFabricClear.Visibility = Visibility.Collapsed;
					ImgOptiFabric.Visibility = Visibility.Collapsed;
					LabOptiFabric.Text = text11 ?? Lang.Text("Download.Install.State.CanAdd");
					LabOptiFabric.Foreground = ThemeManager.colorGray4;
				}
				else
				{
					BtnOptiFabricClear.Visibility = Visibility.Visible;
					ImgOptiFabric.Visibility = Visibility.Visible;
					LabOptiFabric.Text = selectedOptiFabric.DisplayName.ToLower().Replace("optifabric-", "").Replace(".jar", "")
						.Trim()
						.TrimStart('v');
					LabOptiFabric.Foreground = ThemeManager.colorGray1;
				}
			}
			if (selectedFabric != null && selectedFabricApi == null)
			{
				HintFabricAPI.Visibility = Visibility.Visible;
			}
			else
			{
				HintFabricAPI.Visibility = Visibility.Collapsed;
			}
			if (selectedLegacyFabric != null && selectedLegacyFabricApi == null)
			{
				HintLegacyFabricAPI.Visibility = Visibility.Visible;
			}
			else
			{
				HintLegacyFabricAPI.Visibility = Visibility.Collapsed;
			}
			if ((selectedFabric != null || selectedLegacyFabric != null) && selectedOptiFine != null && selectedOptiFabric == null)
			{
				if (VanillaDrop >= 140 && VanillaDrop <= 150)
				{
					HintOptiFabric.Visibility = Visibility.Collapsed;
					HintLegacyOptiFabric.Visibility = Visibility.Collapsed;
					HintOptiFabricOld.Visibility = Visibility.Visible;
				}
				else if (selectedLegacyFabric != null)
				{
					HintOptiFabric.Visibility = Visibility.Collapsed;
					HintLegacyOptiFabric.Visibility = Visibility.Visible;
					HintOptiFabricOld.Visibility = Visibility.Collapsed;
				}
				else
				{
					HintOptiFabric.Visibility = Visibility.Visible;
					HintOptiFabricOld.Visibility = Visibility.Collapsed;
					HintLegacyOptiFabric.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				HintOptiFabric.Visibility = Visibility.Collapsed;
				HintOptiFabricOld.Visibility = Visibility.Collapsed;
				HintLegacyOptiFabric.Visibility = Visibility.Collapsed;
			}
			if (VanillaDrop >= 160 && selectedOptiFine != null && (selectedForge != null || selectedFabric != null))
			{
				HintModOptiFine.Visibility = Visibility.Visible;
			}
			else
			{
				HintModOptiFine.Visibility = Visibility.Collapsed;
			}
		}
		finally
		{
			_ReloadSelected_Ongoing = false;
		}
	}

	private void ClearSelected()
	{
		_vanillaName = null;
		_vanillaData = null;
		_vanillaIcon = null;
		selectedOptiFine = null;
		selectedLiteLoader = null;
		_selectedLoaderType = null;
		selectedAPIName = null;
		selectedForge = null;
		selectedNeoForge = null;
		selectedNeoForgeVersion = null;
		selectedCleanroom = null;
		selectedCleanroomVersion = null;
		selectedFabric = null;
		selectedFabricApi = null;
		selectedOptiFabric = null;
		selectedLabyModCommitRef = null;
		selectedLabyModVersion = null;
		selectedLabyModChannel = null;
		selectedLegacyFabric = null;
		selectedLegacyFabricApi = null;
	}

	private void SelectModLoader(ModLoaderType loaderType)
	{
		if (loaderType != ModLoaderType.Forge)
		{
			selectedForge = null;
		}
		if (loaderType != ModLoaderType.NeoForge)
		{
			selectedNeoForge = null;
			selectedNeoForgeVersion = null;
		}
		if (loaderType != ModLoaderType.Cleanroom)
		{
			selectedCleanroom = null;
			selectedCleanroomVersion = null;
		}
		if (loaderType != ModLoaderType.Fabric)
		{
			selectedFabric = null;
			selectedFabricApi = null;
			selectedOptiFabric = null;
			autoSelectedFabricApi = false;
			autoSelectedOptiFabric = false;
		}
		if (loaderType != ModLoaderType.LegacyFabric)
		{
			selectedLegacyFabric = null;
			selectedLegacyFabricApi = null;
			autoSelectedLegacyFabricApi = false;
		}
		if (loaderType != ModLoaderType.LabyMod)
		{
			selectedLabyModChannel = null;
			selectedLabyModCommitRef = null;
			selectedLabyModVersion = null;
		}
		_selectedLoaderType = loaderType;
		selectedAPIName = null;
	}

	private void SetPanelVisibility(Grid panel, bool visible)
	{
		if (!object.Equals(panel.Tag, visible.ToString()))
		{
			panel.Tag = visible.ToString();
			if (visible)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaTranslateY(panel, 0.0 - ((TranslateTransform)panel.RenderTransform).Y, 150, 0, new ModAnimation.AniEaseOutFluent()),
					ModAnimation.AaOpacity(panel, 1.0 - panel.Opacity, 60)
				}, "PageDownloadInstall Visibility " + panel.Name);
			}
			else
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaTranslateY(panel, 6.0 - ((TranslateTransform)panel.RenderTransform).Y, 60),
					ModAnimation.AaOpacity(panel, 0.0 - panel.Opacity, 60)
				}, "PageDownloadInstall Visibility " + panel.Name);
			}
		}
	}

	private string GetSelectLogo()
	{
		if (selectedFabric != null)
		{
			return "pack://application:,,,/images/Blocks/Fabric.png";
		}
		if (selectedLegacyFabric != null)
		{
			return "pack://application:,,,/images/Blocks/Fabric.png";
		}
		if (selectedForge != null)
		{
			return "pack://application:,,,/images/Blocks/Anvil.png";
		}
		if (selectedNeoForge != null)
		{
			return "pack://application:,,,/images/Blocks/NeoForge.png";
		}
		if (selectedLiteLoader != null)
		{
			return "pack://application:,,,/images/Blocks/Egg.png";
		}
		if (selectedOptiFine != null)
		{
			return "pack://application:,,,/images/Blocks/GrassPath.png";
		}
		if (selectedCleanroom != null)
		{
			return "pack://application:,,,/images/Blocks/Cleanroom.png";
		}
		if (selectedLabyModVersion != null)
		{
			return "pack://application:,,,/images/Blocks/LabyMod.png";
		}
		return _vanillaIcon;
	}

	private string GetSelectInfo()
	{
		List<string> list = new List<string> { _vanillaName };
		(string, string)[] source = new (string, string)[7]
		{
			("Common.Installation.Fabric", selectedFabric?.Replace("+build", "")),
			("Common.Installation.LegacyFabric", selectedLegacyFabric),
			("Common.Installation.Forge", selectedForge?.VersionName),
			("Common.Installation.NeoForge", selectedNeoForge?.VersionName ?? VersionOrNull(selectedNeoForgeVersion)),
			("Common.Installation.Cleanroom", selectedCleanroom?.VersionName ?? VersionOrNull(selectedCleanroomVersion)),
			("Common.Installation.LabyMod", selectedLabyModVersion),
			("Common.Installation.OptiFine", selectedOptiFine?.DisplayName.Replace(_vanillaName + " ", ""))
		};
		list.AddRange(source.Where(((string NameKey, string Version) info) => !string.IsNullOrWhiteSpace(info.Version)).Select(((string NameKey, string Version) info) => Lang.Text(info.NameKey) + " " + info.Version));
		if (selectedLiteLoader != null)
		{
			list.Add(Lang.Text("Common.Installation.LiteLoader"));
		}
		if (list.Count == 1)
		{
			list.Add(Lang.Text("Instance.Install.NoExtraInstall"));
		}
		return string.Join("  |  ", list);
	}

	private static string? VersionOrNull<T>(T version)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		T x = version;
		T val = default;
		if (!equalityComparer.Equals(x, val))
		{
			ref T reference = ref version;
			val = default;
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					return null;
				}
			}
			return reference.ToString();
		}
		return null;
	}

	private object GetCurrentFabricApi()
	{
		List<ModComp.CompFile> output = ModDownload.dlFabricApiLoader.output;
		if (output == null)
		{
			return null;
		}
		ModLocalComp.LocalCompFile localComp = ModLocalComp.GetModLocalCompByKeywords(PageInstanceLeft.McInstance, new string[2] { "fabric-api", "fabric" }, "fabric", "api");
		if (localComp == null)
		{
			return null;
		}
		ModComp.CompFile compFile = output.FirstOrDefault((ModComp.CompFile comp) => (comp.Hash ?? "") == (localComp.ModrinthHash ?? ""));
		if (compFile != null)
		{
			_currentFabricApi = compFile;
			_currentFabricApiPath = localComp.path;
		}
		return compFile;
	}

	private object GetCurrentLegacyFabricApi()
	{
		List<ModComp.CompFile> output = ModDownload.dlLegacyFabricApiLoader.output;
		if (output == null)
		{
			return null;
		}
		ModLocalComp.LocalCompFile localComp = ModLocalComp.GetModLocalCompByKeywords(PageInstanceLeft.McInstance, new string[2] { "legacy-fabric-api", "legacy-fabric" }, "legacy-fabric", "api");
		if (localComp == null)
		{
			return null;
		}
		ModComp.CompFile compFile = output.FirstOrDefault((ModComp.CompFile comp) => (comp.Hash ?? "") == (localComp.ModrinthHash ?? ""));
		if (compFile != null)
		{
			_currentLegacyFabricApi = compFile;
			_currentLegacyFabricApiPath = localComp.path;
		}
		return compFile;
	}

	private object GetCurrentOptiFabric()
	{
		List<ModComp.CompFile> output = ModDownload.dlOptiFabricLoader.output;
		if (output == null)
		{
			return null;
		}
		ModLocalComp.LocalCompFile localComp = ModLocalComp.GetModLocalCompByKeywords(PageInstanceLeft.McInstance, "optifabric", "optifabric", "opti");
		if (localComp == null)
		{
			return null;
		}
		ModComp.CompFile compFile = output.FirstOrDefault((ModComp.CompFile comp) => (comp.Hash ?? "") == (localComp.ModrinthHash ?? ""));
		if (compFile != null)
		{
			_currentOptiFabric = compFile;
			_currentOptiFabricPath = localComp.path;
		}
		return compFile;
	}

	public void GetCurrentInfo()
	{
		ClearSelected();
		BtnSelectStart.IsEnabled = true;
		McInstanceInfo info = PageInstanceLeft.McInstance.Info;
		_vanillaName = info.VanillaName;
		if (info.HasLiteLoader)
		{
			selectedLiteLoader = new ModDownload.DlLiteLoaderListEntry
			{
				Inherit = info.VanillaName
			};
		}
		if (info.HasOptiFine)
		{
			selectedOptiFine = new ModDownload.DlOptiFineListEntry
			{
				DisplayName = info.VanillaName + " " + info.OptiFine.Replace("_", " "),
				IsPreview = info.OptiFine.ContainsF("pre"),
				Inherit = info.VanillaName,
				NameVersion = info.VanillaName + "-OptiFine_HD_U_" + info.OptiFine
			};
		}
		if (info.HasCleanroom)
		{
			_selectedLoaderType = ModLoaderType.Cleanroom;
			selectedAPIName = "Cleanroom";
			selectedCleanroomVersion = info.Cleanroom;
			selectedCleanroom = new ModDownload.DlCleanroomListEntry(selectedCleanroomVersion);
		}
		else if (info.HasForge)
		{
			_selectedLoaderType = ModLoaderType.Forge;
			selectedForge = new ModDownload.DlForgeVersionEntry(info.Forge, null, info.VanillaName)
			{
				Category = "installer",
				forgeType = ModDownload.DlForgelikeEntry.ForgelikeType.Forge,
				Inherit = info.VanillaName
			};
		}
		else if (info.HasLegacyFabric)
		{
			_selectedLoaderType = ModLoaderType.LegacyFabric;
			selectedLegacyFabric = info.LegacyFabric;
			selectedLegacyFabricApi = (ModComp.CompFile)GetCurrentLegacyFabricApi();
		}
		else if (info.HasFabric)
		{
			_selectedLoaderType = ModLoaderType.Fabric;
			selectedFabric = info.Fabric;
			selectedFabricApi = (ModComp.CompFile)GetCurrentFabricApi();
		}
		else if (info.HasLabyMod)
		{
			_selectedLoaderType = ModLoaderType.LabyMod;
			selectedLabyModVersion = info.LabyMod;
		}
		else if (info.HasNeoForge)
		{
			_selectedLoaderType = ModLoaderType.NeoForge;
			selectedNeoForgeVersion = info.NeoForge;
			selectedNeoForge = new ModDownload.DlNeoForgeListEntry(info.NeoForge)
			{
				VersionName = info.NeoForge,
				Inherit = info.VanillaName,
				forgeType = ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge
			};
		}
		if (info.HasFabric && info.HasOptiFine)
		{
			selectedOptiFabric = (ModComp.CompFile)GetCurrentOptiFabric();
		}
		_vanillaIcon = "pack://application:,,,/images/Blocks/Grass.png";
		currentInfo = GetSelectInfo();
		EnterSelectPage();
	}

	private static string GetVersionTypeTitle(string key)
	{
		return key switch
		{
			"正式版" => Lang.Text("Download.Version.Type.Release"), 
			"预览版" => Lang.Text("Download.Version.Type.Development"), 
			"远古版" => Lang.Text("Download.Version.Type.BeforeRelease"), 
			"愚人节版" => Lang.Text("Download.Version.Type.AprilFools"), 
			_ => key, 
		};
	}

	private void LoadMinecraft_OnFinish()
	{
		ExitSelectPage();
		try
		{
			Dictionary<string, List<JsonObject>> dictionary = new Dictionary<string, List<JsonObject>>
			{
				{
					"正式版",
					new List<JsonObject>()
				},
				{
					"预览版",
					new List<JsonObject>()
				},
				{
					"远古版",
					new List<JsonObject>()
				},
				{
					"愚人节版",
					new List<JsonObject>()
				}
			};
			JsonArray jsonArray = (JsonArray)ModDownload.dlClientListLoader.output.Value["versions"];
			foreach (JsonObject item in jsonArray)
			{
				string text = item["type"].ToString();
				string text2 = item["id"].ToString().ToLower();
				switch (text ?? "")
				{
				case "release":
					text = "正式版";
					break;
				case "snapshot":
				case "pending":
				{
					text = "预览版";
					if (text2.StartsWith("1.") && !text2.Contains("combat") && !text2.Contains("rc") && !text2.Contains("experimental") && !text2.Equals("1.2") && !text2.Contains("pre"))
					{
						text = "正式版";
						item["type"] = "release";
					}
					string text3 = item["id"].ToString().ToLower() ?? "";
					switch (text3)
					{
					case "2point0_blue":
					case "2point0_red":
					case "2point0_purple":
					case "2.0_blue":
					case "2.0_red":
					case "2.0_purple":
					case "2.0":
						text = "愚人节版";
						item["id"] = item["id"].ToString().Replace("point", ".");
						item["type"] = "special";
						item.Add("lore", McVersionClassifier.GetMcFoolName((string?)item["id"]));
						goto end_IL_00bd;
					case "20w14∞":
					case "20w14infinite":
						text = "愚人节版";
						item["id"] = "20w14∞";
						item["type"] = "special";
						item.Add("lore", McVersionClassifier.GetMcFoolName((string?)item["id"]));
						goto end_IL_00bd;
					default:
						switch (text3)
						{
						case "2.0":
						case "22w13oneblockatatime":
						case "23w13a_or_b":
						case "24w14potato":
						case "25w14craftmine":
						case "26w14a":
							break;
						default:
						{
							DateTime dateTime = McVersionClassifier.GetReleaseTime(item).ToUniversalTime().AddHours(2.0);
							if (dateTime.Month == 4 && dateTime.Day == 1)
							{
								text = "愚人节版";
								item["type"] = "special";
							}
							goto end_IL_00bd;
						}
						}
						break;
					case "15w14a":
					case "3d shareware v1.34":
					case "1.rv-pre1":
						break;
					}
					text = "愚人节版";
					item["type"] = "special";
					item.Add("lore", McVersionClassifier.GetMcFoolName((string?)item["id"]));
					break;
				}
				case "special":
					text = "愚人节版";
					break;
				default:
					{
						text = "远古版";
						break;
					}
					end_IL_00bd:
					break;
				}
				dictionary[text].Add(item);
			}
			foreach (KeyValuePair<string, List<JsonObject>> item2 in dictionary.ToList())
			{
				dictionary[item2.Key] = ((IEnumerable<JsonObject>)item2.Value).OrderByDescending((Func<JsonObject, DateTime>)McVersionClassifier.GetReleaseTime).ToList();
			}
			PanMinecraft.Children.Clear();
			MyCard myCard = new MyCard
			{
				Title = Lang.Text("Download.Version.Latest.Title"),
				Margin = new Thickness(0.0, 15.0, 0.0, 15.0)
			};
			List<JsonObject> list = new List<JsonObject>();
			JsonObject jsonObject2 = (JsonObject)dictionary["正式版"][0].DeepClone();
			jsonObject2["lore"] = Lang.Text("Download.Version.Latest.Release", new object[1] { Lang.Date(JsonCompat.ToObject<DateTime>(jsonObject2["releaseTime"]), "g") });
			list.Add(jsonObject2);
			if (JsonCompat.ToObject<DateTime>(dictionary["正式版"][0]["releaseTime"]) < JsonCompat.ToObject<DateTime>(dictionary["预览版"][0]["releaseTime"]))
			{
				JsonObject jsonObject3 = (JsonObject)dictionary["预览版"][0].DeepClone();
				jsonObject3["lore"] = Lang.Text("Download.Version.Latest.Development", new object[1] { Lang.Date(JsonCompat.ToObject<DateTime>(jsonObject3["releaseTime"]), "g") });
				list.Add(jsonObject3);
			}
			StackPanel stack = new StackPanel
			{
				Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
				VerticalAlignment = VerticalAlignment.Top,
				RenderTransform = new TranslateTransform(0.0, 0.0),
				Tag = list
			};
			MyCard.StackInstall(ref stack, StackInstall);
			myCard.Children.Add(stack);
			PanMinecraft.Children.Insert(0, myCard);
			foreach (KeyValuePair<string, List<JsonObject>> item3 in dictionary)
			{
				if (item3.Value.Any())
				{
					MyCard myCard2 = new MyCard
					{
						Title = GetVersionTypeTitle(item3.Key) + " (" + item3.Value.Count + ")",
						Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
					};
					StackPanel stackPanel = new StackPanel
					{
						Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
						VerticalAlignment = VerticalAlignment.Top,
						RenderTransform = new TranslateTransform(0.0, 0.0),
						Tag = item3.Value
					};
					myCard2.Children.Add(stackPanel);
					myCard2.SwapControl = stackPanel;
					myCard2.InstallMethod = StackInstall;
					myCard2.IsSwapped = true;
					PanMinecraft.Children.Add(myCard2);
				}
			}
			if (mcVersionWaitingForSelect == null)
			{
				return;
			}
			ModBase.Log("[Download] 自动选择 MC 版本：" + mcVersionWaitingForSelect);
			foreach (JsonObject item4 in jsonArray)
			{
				if (!((item4["id"].ToString() ?? "") != (mcVersionWaitingForSelect ?? "")))
				{
					MyListItem sender = ModDownloadLib.McDownloadListItem(item4, (object _, MouseButtonEventArgs _) =>
					{
					}, isSaveOnly: false);
					MinecraftSelected(sender, null);
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
		void StackInstall(StackPanel stackPanel2)
		{
			foreach (object item5 in (IEnumerable)stackPanel2.Tag)
			{
				stackPanel2.Children.Add(ModDownloadLib.McDownloadListItem((JsonObject)item5, (object obj, MouseButtonEventArgs e) =>
				{
					MinecraftSelected((MyListItem)obj, e);
				}, isSaveOnly: false));
			}
		}
	}

	private string LoadOptiFineGetError()
	{
		bool flag;
		switch (_selectedLoaderType)
		{
		case ModLoaderType.NeoForge:
		case ModLoaderType.Cleanroom:
		case ModLoaderType.LabyMod:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (LoadOptiFine == null || LoadOptiFine.State.LoadingState == MyLoading.MyLoadingState.Run)
		{
			return Lang.Text("Download.Install.State.Loading");
		}
		if (LoadOptiFine.State.LoadingState == MyLoading.MyLoadingState.Error)
		{
			return Lang.Text("Download.Install.State.GetVersionListFailed") + ((ModLoader.LoaderBase)LoadOptiFine.State).Error.Message;
		}
		if (_selectedLoaderType == ModLoaderType.Forge && McVersionComparer.CompareVersion(_vanillaName, "1.13") >= 0 && McVersionComparer.CompareVersion("1.14.3", _vanillaName) >= 0)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (selectedFabric != null && McVersionComparer.CompareVersion(_vanillaName, "1.20.4") > 0)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (GetLoaderError(LoadOptiFine) != null)
		{
			return GetLoaderError(LoadOptiFine);
		}
		bool flag2 = false;
		bool flag3 = false;
		foreach (ModDownload.DlOptiFineListEntry item in ModDownload.dlOptiFineListLoader.output.Value)
		{
			if (item.DisplayName.StartsWith(_vanillaName + " "))
			{
				flag2 = true;
				if (selectedForge == null)
				{
					return null;
				}
				if ((bool)IsOptiFineSuitForForge(item, selectedForge))
				{
					return null;
				}
				if (item.RequiredForgeVersion != null)
				{
					flag3 = true;
				}
			}
		}
		if (!flag2)
		{
			return Lang.Text("Download.Install.State.NoVersion");
		}
		if (flag3)
		{
			return Lang.Text("Download.Install.Compat.CompatForgeSpecificOnly");
		}
		return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
	}

	private object IsOptiFineSuitForForge(ModDownload.DlOptiFineListEntry optiFine, ModDownload.DlForgeVersionEntry forge)
	{
		if ((forge.Inherit ?? "") != (optiFine.Inherit ?? ""))
		{
			return false;
		}
		if (optiFine.RequiredForgeVersion == null)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(optiFine.RequiredForgeVersion))
		{
			return true;
		}
		if (optiFine.RequiredForgeVersion.Contains("."))
		{
			return McVersionComparer.CompareVersion(forge.version.ToString(), optiFine.RequiredForgeVersion) == 0;
		}
		return (double)forge.version.Revision == Convert.ToDouble(optiFine.RequiredForgeVersion);
	}

	private void CardOptiFine_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadOptiFineGetError() != null)
		{
			e.handled = true;
		}
	}

	private void OptiFine_Loaded()
	{
		try
		{
			if (ModDownload.dlOptiFineListLoader.State != ModBase.LoadState.Finished)
			{
				return;
			}
			List<ModDownload.DlOptiFineListEntry> list = new List<ModDownload.DlOptiFineListEntry>();
			foreach (ModDownload.DlOptiFineListEntry item in ModDownload.dlOptiFineListLoader.output.Value)
			{
				if ((selectedForge == null || (bool)IsOptiFineSuitForForge(item, selectedForge)) && item.DisplayName.StartsWith(_vanillaName + " "))
				{
					list.Add(item);
				}
			}
			if (!list.Any())
			{
				return;
			}
			list.Sort((ModDownload.DlOptiFineListEntry left, ModDownload.DlOptiFineListEntry right) =>
			{
				if (!left.IsPreview && right.IsPreview)
				{
					return true;
				}
				return (!left.IsPreview || right.IsPreview) && McVersionComparer.CompareVersion(left.DisplayName, right.DisplayName) != 0;
			});
			PanOptiFine.Children.Clear();
			foreach (ModDownload.DlOptiFineListEntry item2 in list)
			{
				PanOptiFine.Children.Add(ModDownloadLib.OptiFineDownloadListItem(item2, (object a, MouseButtonEventArgs b) =>
				{
					this.OptiFine_Selected((dynamic)a, b);
				}, isSaveOnly: false));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 OptiFine 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void OptiFine_Selected(MyListItem sender, EventArgs e)
	{
		selectedOptiFine = (ModDownload.DlOptiFineListEntry)sender.Tag;
		if (selectedForge != null && !(bool)IsOptiFineSuitForForge(selectedOptiFine, selectedForge))
		{
			selectedForge = null;
		}
		OptiFabric_Loaded();
		Forge_Loaded();
		NeoForge_Loaded();
		CardOptiFine.IsSwapped = true;
		ReloadSelected();
	}

	private void OptiFine_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedOptiFine = null;
		selectedOptiFabric = null;
		autoSelectedOptiFabric = false;
		CardOptiFine.IsSwapped = true;
		e.Handled = true;
		Forge_Loaded();
		NeoForge_Loaded();
		ReloadSelected();
	}

	private string LoadLiteLoaderGetError()
	{
		if (GetLoaderError(LoadLiteLoader) != null)
		{
			return GetLoaderError(LoadLiteLoader);
		}
		bool flag;
		switch (_selectedLoaderType)
		{
		case ModLoaderType.NeoForge:
		case ModLoaderType.Cleanroom:
		case ModLoaderType.LegacyFabric:
		case ModLoaderType.LabyMod:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (!ModDownload.dlLiteLoaderListLoader.output.Value.Any((ModDownload.DlLiteLoaderListEntry v) => (v.Inherit ?? "") == (_vanillaName ?? "")))
		{
			return Lang.Text("Download.Install.State.NoVersion");
		}
		return null;
	}

	private void CardLiteLoader_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadLiteLoaderGetError() != null)
		{
			e.handled = true;
		}
	}

	private void LiteLoader_Loaded()
	{
		try
		{
			if (ModDownload.dlLiteLoaderListLoader.State != ModBase.LoadState.Finished)
			{
				return;
			}
			List<ModDownload.DlLiteLoaderListEntry> list = new List<ModDownload.DlLiteLoaderListEntry>();
			foreach (ModDownload.DlLiteLoaderListEntry item in ModDownload.dlLiteLoaderListLoader.output.Value)
			{
				if ((item.Inherit ?? "") == (_vanillaName ?? ""))
				{
					list.Add(item);
				}
			}
			if (!list.Any())
			{
				return;
			}
			PanLiteLoader.Children.Clear();
			foreach (ModDownload.DlLiteLoaderListEntry item2 in list)
			{
				PanLiteLoader.Children.Add(ModDownloadLib.LiteLoaderDownloadListItem(item2, (object a, MouseButtonEventArgs b) =>
				{
					this.LiteLoader_Selected((dynamic)a, b);
				}, isSaveOnly: false));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 LiteLoader 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void LiteLoader_Selected(MyListItem sender, EventArgs e)
	{
		selectedLiteLoader = (ModDownload.DlLiteLoaderListEntry)sender.Tag;
		CardLiteLoader.IsSwapped = true;
		ReloadSelected();
	}

	private void LiteLoader_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedLiteLoader = null;
		CardLiteLoader.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

	private string LoadForgeGetError()
	{
		if (McVersionComparer.CompareVersionGe("1.5.1", _vanillaName) && McVersionComparer.CompareVersionGe(_vanillaName, "1.1"))
		{
			return Lang.Text("Download.Install.State.NoVersion");
		}
		ModLoaderType? selectedLoaderType = _selectedLoaderType;
		if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.Forge)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (GetLoaderError(LoadForge) != null)
		{
			return GetLoaderError(LoadForge);
		}
		ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>> loaderTask = (ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)LoadForge.State;
		if ((_vanillaName ?? "") != (loaderTask.input ?? ""))
		{
			return Lang.Text("Download.Install.State.Getting");
		}
		foreach (ModDownload.DlForgeVersionEntry item in loaderTask.output)
		{
			if (!(item.Category == "universal") && !(item.Category == "client"))
			{
				selectedLoaderType = _selectedLoaderType;
				if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.Forge)
				{
					return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
				}
				if (selectedOptiFine != null && McVersionComparer.CompareVersionGe(_vanillaName, "1.13") && McVersionComparer.CompareVersionGe("1.14.3", _vanillaName))
				{
					return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
				}
				if (selectedOptiFine == null || (bool)IsOptiFineSuitForForge(selectedOptiFine, item))
				{
					return null;
				}
			}
		}
		return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
	}

	private void CardForge_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadForgeGetError() != null)
		{
			e.handled = true;
		}
	}

	private void Forge_Loaded()
	{
		try
		{
			if (!LoadForge.State.IsLoader)
			{
				return;
			}
			ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>> loaderTask = (ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)LoadForge.State;
			if ((_vanillaName ?? "") != (loaderTask.input ?? "") || loaderTask.State != ModBase.LoadState.Finished)
			{
				return;
			}
			List<ModDownload.DlForgeVersionEntry> source = loaderTask.output.ToList();
			if (!loaderTask.output.Any())
			{
				return;
			}
			PanForge.Children.Clear();
			source = (from v in source.Where((ModDownload.DlForgeVersionEntry v) =>
				{
					if (v.Category == "universal" || v.Category == "client")
					{
						return false;
					}
					return (selectedOptiFine == null || (bool)IsOptiFineSuitForForge(selectedOptiFine, v)) ? true : false;
				})
				orderby v descending
				select v).ToList();
			ModDownloadLib.ForgeDownloadListItemPreload(PanForge, source, (object a, MouseButtonEventArgs b) =>
			{
				this.Forge_Selected((dynamic)a, b);
			}, isSaveOnly: false);
			foreach (ModDownload.DlForgeVersionEntry item in source)
			{
				PanForge.Children.Add(ModDownloadLib.ForgeDownloadListItem(item, (object a, MouseButtonEventArgs b) =>
				{
					this.Forge_Selected((dynamic)a, b);
				}, isSaveOnly: false));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Forge 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void Forge_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.Forge);
		selectedForge = (ModDownload.DlForgeVersionEntry)sender.Tag;
		CardForge.IsSwapped = true;
		if (selectedOptiFine != null && !(bool)IsOptiFineSuitForForge(selectedOptiFine, selectedForge))
		{
			selectedOptiFine = null;
		}
		OptiFine_Loaded();
		ReloadSelected();
	}

	private void Forge_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedForge = null;
		_selectedLoaderType = null;
		CardForge.IsSwapped = true;
		e.Handled = true;
		OptiFine_Loaded();
		ReloadSelected();
	}

	private string LoadNeoForgeGetError()
	{
		if (selectedOptiFine != null)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
		}
		ModLoaderType? selectedLoaderType = _selectedLoaderType;
		if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.NeoForge)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (GetLoaderError(LoadNeoForge) != null)
		{
			return GetLoaderError(LoadNeoForge);
		}
		if (!ModDownload.dlNeoForgeListLoader.output.Value.Any((ModDownload.DlNeoForgeListEntry v) => (v.Inherit ?? "") == (_vanillaName ?? "")))
		{
			return Lang.Text("Download.Install.State.NoVersion");
		}
		return null;
	}

	private void CardNeoForge_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadNeoForgeGetError() != null)
		{
			e.handled = true;
		}
	}

	private void NeoForge_Loaded()
	{
		try
		{
			if (ModDownload.dlNeoForgeListLoader.State != ModBase.LoadState.Finished)
			{
				return;
			}
			List<ModDownload.DlNeoForgeListEntry> list = ModDownload.dlNeoForgeListLoader.output.Value.Where((ModDownload.DlNeoForgeListEntry v) => (v.Inherit ?? "") == (_vanillaName ?? "")).ToList();
			if (!list.Any())
			{
				return;
			}
			PanNeoForge.Children.Clear();
			ModDownloadLib.NeoForgeDownloadListItemPreload(PanNeoForge, list, (object a, MouseButtonEventArgs b) =>
			{
				this.NeoForge_Selected((dynamic)a, b);
			}, isSaveOnly: false);
			foreach (ModDownload.DlNeoForgeListEntry item in list)
			{
				PanNeoForge.Children.Add(ModDownloadLib.NeoForgeDownloadListItem(item, (object a, MouseButtonEventArgs b) =>
				{
					this.NeoForge_Selected((dynamic)a, b);
				}, isSaveOnly: false));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 NeoForge 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void NeoForge_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.NeoForge);
		selectedNeoForge = (ModDownload.DlNeoForgeListEntry)sender.Tag;
		CardNeoForge.IsSwapped = true;
		OptiFine_Loaded();
		ReloadSelected();
	}

	private void NeoForge_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedNeoForge = null;
		_selectedLoaderType = null;
		CardNeoForge.IsSwapped = true;
		e.Handled = true;
		OptiFine_Loaded();
		ReloadSelected();
	}

	private string LoadCleanroomGetError()
	{
		if (!_vanillaName.StartsWith("1."))
		{
			return Lang.Text("Download.Install.State.NoAvailableVersion");
		}
		if (selectedOptiFine != null)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
		}
		ModLoaderType? selectedLoaderType = _selectedLoaderType;
		if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.Cleanroom)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (selectedLiteLoader != null)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLiteLoader");
		}
		if (GetLoaderError(LoadCleanroom) != null)
		{
			return GetLoaderError(LoadCleanroom);
		}
		if (!ModDownload.dlCleanroomListLoader.output.Value.Any((ModDownload.DlCleanroomListEntry v) => (v.Inherit ?? "") == (_vanillaName ?? "")))
		{
			return Lang.Text("Download.Install.State.NoVersion");
		}
		return null;
	}

	private void CardCleanroom_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadCleanroomGetError() != null)
		{
			e.handled = true;
		}
	}

	private void Cleanroom_Loaded()
	{
		try
		{
			if (ModDownload.dlCleanroomListLoader.State != ModBase.LoadState.Finished)
			{
				return;
			}
			List<ModDownload.DlCleanroomListEntry> list = ModDownload.dlCleanroomListLoader.output.Value.Where((ModDownload.DlCleanroomListEntry v) => (v.Inherit ?? "") == (_vanillaName ?? "")).ToList();
			if (!list.Any())
			{
				return;
			}
			PanCleanroom.Children.Clear();
			ModDownloadLib.CleanroomDownloadListItemPreload(PanCleanroom, list, (object a, MouseButtonEventArgs b) =>
			{
				this.Cleanroom_Selected((dynamic)a, b);
			}, isSaveOnly: false);
			foreach (ModDownload.DlCleanroomListEntry item in list)
			{
				PanCleanroom.Children.Add(ModDownloadLib.CleanroomDownloadListItem(item, (object a, MouseButtonEventArgs b) =>
				{
					this.Cleanroom_Selected((dynamic)a, b);
				}, isSaveOnly: false));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Cleanroom 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void Cleanroom_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.Cleanroom);
		selectedCleanroom = (ModDownload.DlCleanroomListEntry)sender.Tag;
		CardCleanroom.IsSwapped = true;
		OptiFine_Loaded();
		ReloadSelected();
	}

	private void Cleanroom_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedCleanroom = null;
		selectedCleanroomVersion = null;
		_selectedLoaderType = null;
		selectedAPIName = null;
		CardCleanroom.IsSwapped = true;
		e.Handled = true;
		OptiFine_Loaded();
		ReloadSelected();
	}

	private string LoadFabricGetError()
	{
		if (selectedOptiFine != null && McVersionComparer.CompareVersionGe(_vanillaName, "1.20.5"))
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
		}
		if (GetLoaderError(LoadFabric) != null)
		{
			return GetLoaderError(LoadFabric);
		}
		foreach (JsonObject item in ModDownload.dlFabricListLoader.output.Value["game"].AsArray())
		{
			if ((item["version"].ToString() ?? "") == (_vanillaName.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3") ?? ""))
			{
				ModLoaderType? selectedLoaderType = _selectedLoaderType;
				if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.Fabric)
				{
					return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
				}
				return null;
			}
		}
		return Lang.Text("Download.Install.State.NoVersion");
	}

	private void CardFabric_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadFabricGetError() != null)
		{
			e.handled = true;
		}
	}

	private void Fabric_Loaded()
	{
		try
		{
			if (ModDownload.dlFabricListLoader.State != ModBase.LoadState.Finished)
			{
				return;
			}
			JsonArray jsonArray = (JsonArray)ModDownload.dlFabricListLoader.output.Value["loader"];
			if (!jsonArray.Any())
			{
				return;
			}
			PanFabric.Children.Clear();
			PanFabric.Tag = jsonArray;
			CardFabric.SwapControl = PanFabric;
			CardFabric.InstallMethod = (StackPanel stack) =>
			{
				foreach (object item in (IEnumerable)stack.Tag)
				{
					stack.Children.Add(ModDownloadLib.FabricDownloadListItem((JsonObject)item, (object a, MouseButtonEventArgs b) =>
					{
						this.Fabric_Selected((dynamic)a, b);
					}));
				}
			};
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Fabric 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	public void Fabric_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.Fabric);
		selectedFabric = ((dynamic)sender.Tag)["version"].ToString();
		FabricApi_Loaded();
		OptiFabric_Loaded();
		CardFabric.IsSwapped = true;
		ReloadSelected();
	}

	private void Fabric_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedFabric = null;
		selectedFabricApi = null;
		autoSelectedFabricApi = false;
		selectedOptiFabric = null;
		autoSelectedOptiFabric = false;
		_selectedLoaderType = null;
		selectedAPIName = null;
		CardFabric.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

	public bool IsFabricApiCompatible(ModComp.CompFile fabricApi)
	{
		string text = fabricApi.DisplayName;
		try
		{
			if (text == null || _vanillaName == null)
			{
				return false;
			}
			text = text.ToLower();
			_vanillaName = _vanillaName.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3").ToLower();
			if (text.StartsWith("[" + _vanillaName + "]"))
			{
				return true;
			}
			if (!text.Contains("/") || !text.Contains("]"))
			{
				return false;
			}
			string[] array = text.BeforeFirst("]").TrimStart('[').Split("/");
			for (int i = 0; i < array.Length; i++)
			{
				if ((array[i] ?? "") == (_vanillaName ?? ""))
				{
					return true;
				}
			}
			List<string> list = text.BeforeFirst("]").RegexSearch("[a-z/]+|[0-9/]+");
			List<string> list2 = _vanillaName.BeforeFirst("]").RegexSearch("[a-z/]+|[0-9/]+");
			int num = 0;
			while (true)
			{
				if (list.Count - 1 < num && list2.Count - 1 < num)
				{
					return true;
				}
				string text2 = ((list.Count - 1 < num) ? "-1" : list[num]);
				string text3 = ((list2.Count - 1 < num) ? "-1" : list2[num]);
				if (!text2.Contains("/"))
				{
					if ((text2 ?? "") != (text3 ?? ""))
					{
						return false;
					}
				}
				else if (!text2.Contains(text3))
				{
					break;
				}
				num++;
			}
			return false;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "判断 Fabric API 版本适配性出错（" + text + ", " + _vanillaName + "）");
			return false;
		}
	}

	private string LoadFabricApiGetError()
	{
		if (GetLoaderError(LoadFabricApi) != null)
		{
			return GetLoaderError(LoadFabricApi);
		}
		if (ModDownload.dlFabricApiLoader.output == null)
		{
			if (selectedFabric != null)
			{
				return Lang.Text("Download.Install.State.Getting");
			}
			return Lang.Text("Download.Install.Compat.RequiresFabric");
		}
		if (ModDownload.dlFabricApiLoader.output.Any((ModComp.CompFile f) => IsFabricApiCompatible(f)))
		{
			if (selectedFabric != null)
			{
				return null;
			}
			return Lang.Text("Download.Install.Compat.RequiresFabric");
		}
		return Lang.Text("Download.Install.State.NoVersion");
	}

	private void CardFabricApi_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadFabricApiGetError() != null)
		{
			e.handled = true;
		}
	}

	private void FabricApi_Loaded()
	{
		try
		{
			if (ModDownload.dlFabricApiLoader.State != ModBase.LoadState.Finished || _vanillaName == null || selectedFabric == null)
			{
				return;
			}
			List<ModComp.CompFile> list = new List<ModComp.CompFile>();
			foreach (ModComp.CompFile item in ModDownload.dlFabricApiLoader.output)
			{
				if (IsFabricApiCompatible(item))
				{
					if (!item.DisplayName.StartsWith("["))
					{
						ModBase.Log("[Download] 已特判修改 Fabric API 显示名：" + item.DisplayName, ModBase.LogLevel.Debug);
						item.DisplayName = "[" + _vanillaName + "] " + item.DisplayName;
					}
					list.Add(item);
				}
			}
			if (!list.Any())
			{
				return;
			}
			list = list.OrderByDescending((ModComp.CompFile v) => v.ReleaseDate).ToList();
			PanFabricApi.Children.Clear();
			foreach (ModComp.CompFile item2 in list)
			{
				if (IsFabricApiCompatible(item2))
				{
					PanFabricApi.Children.Add(ModDownloadLib.FabricApiDownloadListItem(item2, (object a, MouseButtonEventArgs b) =>
					{
						this.FabricApi_Selected((dynamic)a, b);
					}));
				}
			}
			if (!autoSelectedFabricApi)
			{
				autoSelectedFabricApi = true;
				ModBase.Log("[Download] 已自动选择 Fabric API：" + ((MyListItem)PanFabricApi.Children[0]).Title);
				FabricApi_Selected((MyListItem)PanFabricApi.Children[0], null);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Fabric API 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void FabricApi_Selected(MyListItem sender, EventArgs e)
	{
		selectedFabricApi = (ModComp.CompFile)sender.Tag;
		selectedAPIName = "Fabric API";
		CardFabricApi.IsSwapped = true;
		ReloadSelected();
	}

	private void FabricApi_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedFabricApi = null;
		selectedAPIName = null;
		CardFabricApi.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

	private string LoadLegacyFabricGetError()
	{
		if (LoadLegacyFabric == null || LoadLegacyFabric.State.LoadingState == MyLoading.MyLoadingState.Run)
		{
			return Lang.Text("Download.Install.State.Loading");
		}
		if (LoadLegacyFabric.State.LoadingState == MyLoading.MyLoadingState.Error)
		{
			return Lang.Text("Download.Install.State.GetVersionListFailed", new object[1] { ((ModLoader.LoaderBase)LoadLegacyFabric.State).Error.Message });
		}
		foreach (JsonObject item in ModDownload.dlLegacyFabricListLoader.output.Value["game"].AsArray())
		{
			if ((item["version"].ToString() ?? "") == (_vanillaName ?? ""))
			{
				if (selectedLiteLoader != null)
				{
					return Lang.Text("Download.Install.Compat.IncompatibleWithLiteLoader");
				}
				ModLoaderType? selectedLoaderType = _selectedLoaderType;
				if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.LegacyFabric)
				{
					return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
				}
				return null;
			}
		}
		return Lang.Text("Download.Install.State.NoVersion");
	}

	private void CardLegacyFabric_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadLegacyFabricGetError() != null)
		{
			e.handled = true;
		}
	}

	private void LegacyFabric_Loaded()
	{
		try
		{
			if (ModDownload.dlLegacyFabricListLoader.State != ModBase.LoadState.Finished)
			{
				return;
			}
			JsonArray jsonArray = (JsonArray)ModDownload.dlLegacyFabricListLoader.output.Value["loader"];
			if (!jsonArray.Any())
			{
				return;
			}
			PanLegacyFabric.Children.Clear();
			PanLegacyFabric.Tag = jsonArray;
			CardLegacyFabric.SwapControl = PanLegacyFabric;
			CardLegacyFabric.InstallMethod = (StackPanel stack) =>
			{
				foreach (object item in (IEnumerable)stack.Tag)
				{
					stack.Children.Add(ModDownloadLib.LegacyFabricDownloadListItem((JsonObject)item, (object a, MouseButtonEventArgs b) =>
					{
						this.LegacyFabric_Selected((dynamic)a, b);
					}));
				}
			};
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 LegacyFabric 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	public void LegacyFabric_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.LegacyFabric);
		selectedLegacyFabric = ((dynamic)sender.Tag)["version"].ToString();
		LegacyFabricApi_Loaded();
		CardLegacyFabric.IsSwapped = true;
		ReloadSelected();
	}

	private void LegacyFabric_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedLegacyFabric = null;
		selectedLegacyFabricApi = null;
		autoSelectedLegacyFabricApi = false;
		_selectedLoaderType = null;
		selectedAPIName = null;
		CardLegacyFabric.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

	public static bool IsSuitableLegacyFabricApi(List<string> supportVersions, string minecraftVersion)
	{
		try
		{
			if (supportVersions.Contains(minecraftVersion))
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "判断 Legacy Fabric API 版本适配性出错（" + supportVersions?.ToString() + ", " + minecraftVersion + "）");
			return false;
		}
	}

	private string LoadLegacyFabricApiGetError()
	{
		if (LoadLegacyFabricApi == null || LoadLegacyFabricApi.State.LoadingState == MyLoading.MyLoadingState.Run)
		{
			return Lang.Text("Download.Install.State.Loading");
		}
		if (LoadLegacyFabricApi.State.LoadingState == MyLoading.MyLoadingState.Error)
		{
			return Lang.Text("Download.Install.State.GetVersionListFailed", new object[1] { ((ModLoader.LoaderBase)LoadLegacyFabricApi.State).Error.Message });
		}
		if (selectedAPIName != null && (object)selectedAPIName != "Legacy Fabric API")
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { selectedAPIName });
		}
		if (ModDownload.dlLegacyFabricApiLoader.output == null)
		{
			if (selectedLegacyFabric == null)
			{
				return Lang.Text("Download.Install.Compat.RequiresLegacyFabric");
			}
			return Lang.Text("Download.Install.State.Loading");
		}
		foreach (ModComp.CompFile item in ModDownload.dlLegacyFabricApiLoader.output)
		{
			if (IsSuitableLegacyFabricApi(item.GameVersions, _vanillaName))
			{
				if (selectedLegacyFabric == null)
				{
					return Lang.Text("Download.Install.Compat.RequiresLegacyFabric");
				}
				return null;
			}
		}
		return Lang.Text("Download.Install.State.NoVersion");
	}

	private void CardLegacyFabricApi_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadLegacyFabricApiGetError() != null)
		{
			e.handled = true;
		}
	}

	private void LegacyFabricApi_Loaded()
	{
		try
		{
			if (ModDownload.dlLegacyFabricApiLoader.State != ModBase.LoadState.Finished || _vanillaName == null || selectedLegacyFabric == null)
			{
				return;
			}
			List<ModComp.CompFile> list = new List<ModComp.CompFile>();
			foreach (ModComp.CompFile item in ModDownload.dlLegacyFabricApiLoader.output)
			{
				if (IsSuitableLegacyFabricApi(item.GameVersions, _vanillaName))
				{
					list.Add(item);
				}
			}
			if (!list.Any())
			{
				return;
			}
			list = list.OrderByDescending((ModComp.CompFile v) => v.ReleaseDate).ToList();
			PanLegacyFabricApi.Children.Clear();
			foreach (ModComp.CompFile item2 in list)
			{
				if (IsSuitableLegacyFabricApi(item2.GameVersions, _vanillaName))
				{
					PanLegacyFabricApi.Children.Add(ModDownloadLib.LegacyFabricApiDownloadListItem(item2, (object a, MouseButtonEventArgs b) =>
					{
						this.LegacyFabricApi_Selected((dynamic)a, b);
					}));
				}
			}
			if (!autoSelectedLegacyFabricApi)
			{
				autoSelectedLegacyFabricApi = true;
				ModBase.Log("[Download] 已自动选择 Legacy Fabric API：" + ((MyListItem)PanLegacyFabricApi.Children[0]).Title);
				LegacyFabricApi_Selected((MyListItem)PanLegacyFabricApi.Children[0], null);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Legacy Fabric API 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void LegacyFabricApi_Selected(MyListItem sender, EventArgs e)
	{
		selectedLegacyFabricApi = (ModComp.CompFile)sender.Tag;
		selectedAPIName = "Legacy Fabric API";
		CardLegacyFabricApi.IsSwapped = true;
		ReloadSelected();
	}

	private void LegacyFabricApi_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedLegacyFabricApi = null;
		selectedAPIName = null;
		CardLegacyFabricApi.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

	private bool IsOptiFabricCompatible(ModComp.CompFile modFile)
	{
		try
		{
			if (_vanillaName == null)
			{
				return false;
			}
			return modFile.GameVersions.Contains(_vanillaName);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "判断 OptiFabric 版本适配性出错（" + _vanillaName + "）");
			return false;
		}
	}

	private string LoadOptiFabricGetError()
	{
		if (VanillaDrop >= 140 && VanillaDrop <= 150)
		{
			return Lang.Text("Download.Install.Compat.OptiFabricOriginsRequired");
		}
		if (GetLoaderError(LoadOptiFabric) != null)
		{
			return GetLoaderError(LoadOptiFabric);
		}
		if (ModDownload.dlOptiFabricLoader.output == null)
		{
			if (selectedFabric == null && selectedOptiFine == null)
			{
				return Lang.Text("Download.Install.Compat.RequiresOptiFineAndFabric");
			}
			if (selectedFabric == null)
			{
				return Lang.Text("Download.Install.Compat.RequiresFabric");
			}
			if (selectedOptiFine == null)
			{
				return Lang.Text("Download.Install.Compat.RequiresOptiFine");
			}
			return Lang.Text("Download.Install.State.Getting");
		}
		foreach (ModComp.CompFile item in ModDownload.dlOptiFabricLoader.output)
		{
			if (IsOptiFabricCompatible(item))
			{
				if (selectedFabric == null && selectedOptiFine == null)
				{
					return Lang.Text("Download.Install.Compat.RequiresOptiFineAndFabric");
				}
				if (selectedFabric == null)
				{
					return Lang.Text("Download.Install.Compat.RequiresFabric");
				}
				if (selectedOptiFine == null)
				{
					return Lang.Text("Download.Install.Compat.RequiresOptiFine");
				}
				return null;
			}
		}
		return Lang.Text("Download.Install.State.NoVersion");
	}

	private void CardOptiFabric_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadOptiFabricGetError() != null)
		{
			e.handled = true;
		}
	}

	private void OptiFabric_Loaded()
	{
		try
		{
			if (ModDownload.dlOptiFabricLoader.State != ModBase.LoadState.Finished || _vanillaName == null || selectedFabric == null || selectedOptiFine == null)
			{
				return;
			}
			List<ModComp.CompFile> list = new List<ModComp.CompFile>();
			foreach (ModComp.CompFile item in ModDownload.dlOptiFabricLoader.output)
			{
				if (IsOptiFabricCompatible(item))
				{
					list.Add(item);
				}
			}
			if (!list.Any())
			{
				return;
			}
			list = list.OrderByDescending((ModComp.CompFile v) => v.ReleaseDate).ToList();
			PanOptiFabric.Children.Clear();
			foreach (ModComp.CompFile item2 in list)
			{
				if (IsOptiFabricCompatible(item2))
				{
					PanOptiFabric.Children.Add(ModDownloadLib.OptiFabricDownloadListItem(item2, (object a, MouseButtonEventArgs b) =>
					{
						this.OptiFabric_Selected((dynamic)a, b);
					}));
				}
			}
			if (!autoSelectedOptiFabric && (VanillaDrop < 140 || VanillaDrop > 150))
			{
				autoSelectedOptiFabric = true;
				ModBase.Log("[Download] 已自动选择 OptiFabric：" + ((MyListItem)PanOptiFabric.Children[0]).Title);
				OptiFabric_Selected((MyListItem)PanOptiFabric.Children[0], null);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 OptiFabric 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	private void OptiFabric_Selected(MyListItem sender, EventArgs e)
	{
		selectedOptiFabric = (ModComp.CompFile)sender.Tag;
		CardOptiFabric.IsSwapped = true;
		ReloadSelected();
	}

	private void OptiFabric_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedOptiFabric = null;
		CardOptiFabric.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

	private string LoadLabyModGetError()
	{
		if (LoadLabyMod == null || LoadLabyMod.State.LoadingState == MyLoading.MyLoadingState.Run)
		{
			return Lang.Text("Download.Install.State.Loading");
		}
		if (LoadLabyMod.State.LoadingState == MyLoading.MyLoadingState.Error)
		{
			return Lang.Text("Download.Install.State.GetVersionListFailed", new object[1] { ((ModLoader.LoaderBase)LoadLabyMod.State).Error.Message });
		}
		if (GetLoaderError(LoadLabyMod) != null)
		{
			return GetLoaderError(LoadLabyMod);
		}
		if (selectedOptiFine != null)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
		}
		ModLoaderType? selectedLoaderType = _selectedLoaderType;
		if (selectedLoaderType.HasValue && _selectedLoaderType != ModLoaderType.LabyMod)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", new object[1] { _selectedLoaderType });
		}
		if (selectedLiteLoader != null)
		{
			return Lang.Text("Download.Install.Compat.IncompatibleWithLiteLoader");
		}
		foreach (JsonObject item in ModDownload.dlLabyModListLoader.output.Value["production"]["minecraftVersions"].AsArray())
		{
			if ((item["version"].ToString() ?? "") == (_vanillaName ?? ""))
			{
				return null;
			}
		}
		foreach (JsonObject item2 in ModDownload.dlLabyModListLoader.output.Value["snapshot"]["minecraftVersions"].AsArray())
		{
			if ((item2["version"].ToString() ?? "") == (_vanillaName ?? ""))
			{
				return null;
			}
		}
		return Lang.Text("Download.Install.State.NoVersion");
	}

	private void CardLabyMod_PreviewSwap(object sender, ModBase.RouteEventArgs e)
	{
		if (LoadLabyModGetError() != null)
		{
			e.handled = true;
		}
	}

	private void LabyMod_Loaded()
	{
		try
		{
			if (LoadLabyMod.State.LoadingState == MyLoading.MyLoadingState.Run)
			{
				return;
			}
			JsonObject value = ModDownload.dlLabyModListLoader.output.Value;
			if (value == null || value["production"] == null || value["snapshot"] == null)
			{
				return;
			}
			JsonArray jsonArray = new JsonArray();
			foreach (JsonObject item in value["production"]["minecraftVersions"].AsArray())
			{
				if ((item["version"].ToString() ?? "") == (_vanillaName ?? ""))
				{
					JsonObject jsonObject = new JsonObject();
					jsonObject.Add("version", value["production"]["labyModVersion"].ToString());
					jsonObject.Add("channel", "production");
					jsonObject.Add("commitReference", value["production"]["commitReference"].ToString());
					jsonArray.Add(jsonObject);
				}
			}
			foreach (JsonObject item2 in value["snapshot"]["minecraftVersions"].AsArray())
			{
				if ((item2["version"].ToString() ?? "") == (_vanillaName ?? ""))
				{
					JsonObject jsonObject2 = new JsonObject();
					jsonObject2.Add("version", value["snapshot"]["labyModVersion"].ToString());
					jsonObject2.Add("channel", "snapshot");
					jsonObject2.Add("commitReference", value["snapshot"]["commitReference"].ToString());
					jsonArray.Add(jsonObject2);
				}
			}
			PanLabyMod.Children.Clear();
			PanLabyMod.Tag = jsonArray;
			CardLabyMod.SwapControl = PanLabyMod;
			CardLabyMod.InstallMethod = (StackPanel stack) =>
			{
				foreach (JsonObject item3 in (IEnumerable)stack.Tag)
				{
					stack.Children.Add(ModDownloadLib.LabyModDownloadListItem(item3, (object a, MouseButtonEventArgs b) =>
					{
						this.LabyMod_Selected((dynamic)a, b);
					}));
				}
			};
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 LabyMod 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Install.Error.OperationFailed"));
		}
	}

	public void LabyMod_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.LabyMod);
		selectedLabyModChannel = ((dynamic)sender.Tag)("channel").ToString();
		selectedLabyModCommitRef = ((dynamic)sender.Tag)("commitReference").ToString();
		selectedLabyModVersion = ((dynamic)sender.Tag)("version").ToString() + ((selectedLabyModChannel == "snapshot") ? (" " + Lang.Text("Download.Version.Type.Snapshot")) : (" " + Lang.Text("Download.Version.Type.Stable")));
		CardLabyMod.IsSwapped = true;
		ReloadSelected();
	}

	private void LabyMod_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedLabyModCommitRef = null;
		selectedLabyModVersion = null;
		selectedLabyModChannel = null;
		if (_selectedLoaderType == ModLoaderType.LabyMod)
		{
			_selectedLoaderType = null;
		}
		selectedAPIName = null;
		CardLabyMod.IsSwapped = true;
		e.Handled = true;
		ReloadSelected();
	}

}
