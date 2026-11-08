using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils.Validate;

namespace PCL;

public partial class PageDownloadInstall : MyPageRight, IComponentConnector
{
	private bool isLoad;

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

	private ModDownload.DlNeoForgeListEntry? selectedNeoForge;

	private string? selectedFabric;

	private ModComp.CompFile? selectedFabricApi;

	private string? selectedLegacyFabric;

	private ModComp.CompFile? selectedLegacyFabricApi;

	private string? selectedLabyModChannel;

	private string? selectedLabyModCommitRef;

	private string? selectedLabyModVersion;

	private string? selectedLabyModBaseVersion;

	private ModComp.CompFile? selectedOptiFabric;

	private bool _ReloadSelected_Ongoing;

	private bool isSelectNameEdited;

	private bool isSelectNameChanging;

	public static string? mcVersionWaitingForSelect;

	private bool autoSelectedFabricApi;

	private bool autoSelectedLegacyFabricApi;

	private bool autoSelectedOptiFabric;

	private int VanillaDrop => McInstanceInfo.VersionToDrop(_vanillaName, allowSnapshot: true);

	public PageDownloadInstall()
	{
		Initialized += (object? _, EventArgs _) =>
		{
			LoaderInit();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
		InitializeComponent();
		PanScroll = PanBack;
		LoadMinecraft.Text = Lang.Text("Download.Version.LoadingList");
		WeakLanguageChanged.Add<PageDownloadInstall>(this, (Action<PageDownloadInstall>)OnLanguageChanged);
		BtnBack.Click += (object _, EventArgs _) =>
		{
			ExitSelectPage();
		};
		CardOptiFine.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadOptiFine.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardForge.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadForge.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardNeoForge.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadNeoForge.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardFabric.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardFabricApi.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadFabricApi.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardOptiFabric.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadOptiFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardLiteLoader.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadLiteLoader.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		LoadCleanroom.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardCleanroom.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		LoadLabyMod.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			ReloadSelected();
		};
		CardLabyMod.Swap += (object _, ModBase.RouteEventArgs _) =>
		{
			ReloadSelected();
		};
		TextSelectName.TextChanged += TextSelectName_TextChanged;
		TextSelectName.ValidateChanged += TextSelectName_ValidateChanged;
		CardOptiFine.PreviewSwap += CardOptiFine_PreviewSwap;
		LoadOptiFine.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			OptiFine_Loaded();
		};
		BtnOptiFineClear.MouseLeftButtonUp += OptiFine_Clear;
		CardLiteLoader.PreviewSwap += CardLiteLoader_PreviewSwap;
		LoadLiteLoader.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LiteLoader_Loaded();
		};
		BtnLiteLoaderClear.MouseLeftButtonUp += LiteLoader_Clear;
		CardForge.PreviewSwap += CardForge_PreviewSwap;
		LoadForge.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			Forge_Loaded();
		};
		BtnForgeClear.MouseLeftButtonUp += Forge_Clear;
		CardNeoForge.PreviewSwap += CardNeoForge_PreviewSwap;
		LoadNeoForge.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			NeoForge_Loaded();
		};
		BtnNeoForgeClear.MouseLeftButtonUp += NeoForge_Clear;
		CardCleanroom.PreviewSwap += CardCleanroom_PreviewSwap;
		LoadCleanroom.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			Cleanroom_Loaded();
		};
		BtnCleanroomClear.MouseLeftButtonUp += Cleanroom_Clear;
		CardFabric.PreviewSwap += CardFabric_PreviewSwap;
		LoadFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			Fabric_Loaded();
		};
		BtnFabricClear.MouseLeftButtonUp += Fabric_Clear;
		CardFabricApi.PreviewSwap += CardFabricApi_PreviewSwap;
		LoadFabricApi.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			FabricApi_Loaded();
		};
		BtnFabricApiClear.MouseLeftButtonUp += FabricApi_Clear;
		CardLegacyFabric.PreviewSwap += CardLegacyFabric_PreviewSwap;
		LoadLegacyFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LegacyFabric_Loaded();
		};
		BtnLegacyFabricClear.MouseLeftButtonUp += LegacyFabric_Clear;
		CardLegacyFabricApi.PreviewSwap += CardLegacyFabricApi_PreviewSwap;
		LoadLegacyFabricApi.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LegacyFabricApi_Loaded();
		};
		BtnLegacyFabricApiClear.MouseLeftButtonUp += LegacyFabricApi_Clear;
		CardOptiFabric.PreviewSwap += CardOptiFabric_PreviewSwap;
		LoadOptiFabric.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			OptiFabric_Loaded();
		};
		BtnOptiFabricClear.MouseLeftButtonUp += OptiFabric_Clear;
		CardLabyMod.PreviewSwap += CardLabyMod_PreviewSwap;
		LoadLabyMod.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			LabyMod_Loaded();
		};
		BtnLabyModClear.MouseLeftButtonUp += LabyMod_Clear;
		TextSelectName.KeyDown += TextSelectName_KeyDown;
		BtnStart.Click += (object _, MouseButtonEventArgs _) =>
		{
			BtnStart_Click();
		};
	}

	private static void OnLanguageChanged(PageDownloadInstall page)
	{
		page._OnLanguageChanged();
	}

	private void LoaderInit()
	{
		disabledPageAnimControls.Add(BtnStart);
		PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, null, ModDownload.dlClientListLoader, (ModLoader.LoaderBase _) =>
		{
			LoadMinecraft_OnFinish();
		});
	}

	private void Init()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected Obj, but got Unknown
		PanBack.ScrollToHome();
		ModDownload.dlOptiFineListLoader.Start();
		ModDownload.dlLiteLoaderListLoader.Start();
		ModDownload.dlFabricListLoader.Start();
		ModDownload.dlNeoForgeListLoader.Start();
		ModDownload.dlCleanroomListLoader.Start();
		ModDownload.dlLabyModListLoader.Start();
		ModDownload.dlLegacyFabricListLoader.Start();
		TextSelectName.ValidateRules = new Collection<IValidator<string>> { (IValidator<string>)new FolderNameValidator(ModFolder.mcFolderSelected + "versions", true, true, false) };
		TextSelectName.Validate();
		ReloadSelected();
		if (!isLoad)
		{
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

	private void BtnBack_Click(object sender, EventArgs e)
	{
		ExitSelectPage();
	}

	private void EnterSelectPage()
	{
		if (isInSelectPage)
		{
			return;
		}
		isInSelectPage = true;
		PanInner.Margin = new Thickness(25.0, 10.0, 25.0, 40.0);
		autoSelectedFabricApi = false;
		autoSelectedOptiFabric = false;
		isSelectNameEdited = false;
		PanSelect.Visibility = Visibility.Visible;
		PanSelect.IsHitTestVisible = true;
		PanMinecraft.IsHitTestVisible = false;
		PanBack.IsHitTestVisible = false;
		PanBack.ScrollToHome();
		disabledPageAnimControls.Remove(BtnStart);
		BtnStart.Show = true;
		CardOptiFine.IsSwapped = true;
		CardLiteLoader.IsSwapped = true;
		CardForge.IsSwapped = true;
		CardNeoForge.IsSwapped = true;
		CardCleanroom.IsSwapped = true;
		CardFabric.IsSwapped = true;
		CardFabricApi.IsSwapped = true;
		CardOptiFabric.IsSwapped = true;
		CardLabyMod.IsSwapped = true;
		if (!States.Hint.InstallPageBack)
		{
			States.Hint.InstallPageBack = true;
			HintService.Hint(Lang.Text("Download.Install.Hint.MinecraftBack"));
		}
		foreach (FrameworkElement allAnimControl in GetAllAnimControls(PanSelect))
		{
			allAnimControl.Opacity = 1.0;
			if (allAnimControl.RenderTransform == null || allAnimControl.RenderTransform is TranslateTransform)
			{
				allAnimControl.RenderTransform = new TranslateTransform();
			}
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
		ModDownload.dlLabyModListLoader.Start();
		ModAnimation.AniStart(new ModAnimation.AniData[6]
		{
			ModAnimation.AaOpacity(PanMinecraft, 0.0 - PanMinecraft.Opacity, 70, 10),
			ModAnimation.AaTranslateX(PanMinecraft, -50.0 - ((TranslateTransform)PanMinecraft.RenderTransform).X, 90, 10),
			ModAnimation.AaCode(() =>
			{
				PanBack.ScrollToHome();
				TextSelectName.Validate();
				OptiFine_Loaded();
				LiteLoader_Loaded();
				Forge_Loaded();
				NeoForge_Loaded();
				Cleanroom_Loaded();
				Fabric_Loaded();
				LegacyFabric_Loaded();
				FabricApi_Loaded();
				LegacyFabricApi_Loaded();
				OptiFabric_Loaded();
				LabyMod_Loaded();
				ReloadSelected();
				PanMinecraft.Visibility = Visibility.Collapsed;
			}, 0, after: true),
			ModAnimation.AaOpacity(PanSelect, 1.0 - PanSelect.Opacity, 70, 100),
			ModAnimation.AaTranslateX(PanSelect, 0.0 - ((TranslateTransform)PanSelect.RenderTransform).X, 160, 100, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
			ModAnimation.AaCode(() =>
			{
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
					BtnLegacyFabricClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardLegacyFabric.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnFabricApiClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardFabricApi.MainTextBlock,
						Mode = BindingMode.OneWay
					});
					BtnLegacyFabricApiClearInner.SetBinding(Shape.FillProperty, new Binding("Foreground")
					{
						Source = CardLegacyFabricApi.MainTextBlock,
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
		}, "FrmDownloadInstall SelectPageSwitch", refreshTime: true);
	}

	public void ExitSelectPage()
	{
		if (isInSelectPage)
		{
			isInSelectPage = false;
			PanInner.Margin = new Thickness(25.0, 10.0, 25.0, 25.0);
			disabledPageAnimControls.Add(BtnStart);
			BtnStart.Show = false;
			ClearSelected();
			PanMinecraft.Visibility = Visibility.Visible;
			PanSelect.IsHitTestVisible = false;
			PanMinecraft.IsHitTestVisible = true;
			PanBack.IsHitTestVisible = false;
			PanBack.ScrollToHome();
			ModAnimation.AniStart(new ModAnimation.AniData[6]
			{
				ModAnimation.AaOpacity(PanSelect, 0.0 - PanSelect.Opacity, 70, 10),
				ModAnimation.AaTranslateX(PanSelect, 50.0 - ((TranslateTransform)PanSelect.RenderTransform).X, 90, 10),
				ModAnimation.AaCode(() =>
				{
					PanBack.ScrollToHome();
				}, 0, after: true),
				ModAnimation.AaOpacity(PanMinecraft, 1.0 - PanMinecraft.Opacity, 70, 100),
				ModAnimation.AaTranslateX(PanMinecraft, 0.0 - ((TranslateTransform)PanMinecraft.RenderTransform).X, 160, 100, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
				ModAnimation.AaCode(() =>
				{
					PanSelect.Visibility = Visibility.Collapsed;
					PanBack.IsHitTestVisible = true;
				}, 0, after: true)
			}, "FrmDownloadInstall SelectPageSwitch");
		}
	}

	public void MinecraftSelected(MyListItem sender, MouseButtonEventArgs e)
	{
		_vanillaName = sender.Title;
		_vanillaData = (JsonObject)(dynamic)sender.Tag;
		_vanillaIcon = sender.Logo;
		EnterSelectPage();
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
			SelectNameUpdate();
			ImgLogo.Source = GetSelectLogo();
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
					LabFabricApi.Text = selectedFabricApi.DisplayName.Split("]")[1].Replace("Fabric API ", "").Replace(" build ", ".").Trim();
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
		selectedCleanroom = null;
		selectedFabric = null;
		selectedFabricApi = null;
		selectedOptiFabric = null;
		selectedLabyModCommitRef = null;
		selectedLabyModVersion = null;
		selectedLabyModBaseVersion = null;
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
		}
		if (loaderType != ModLoaderType.Cleanroom)
		{
			selectedCleanroom = null;
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
			selectedLabyModBaseVersion = null;
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

	private string GetSelectName()
	{
		string text = _vanillaName;
		if (selectedFabric != null)
		{
			text = text + "-Fabric_" + selectedFabric.Replace("+build", "");
		}
		if (selectedLegacyFabric != null)
		{
			text = text + "-LegacyFabric_" + selectedLegacyFabric;
		}
		if (selectedLabyModVersion != null)
		{
			text = text + "-LabyMod_" + selectedLabyModBaseVersion + ((selectedLabyModChannel == "snapshot") ? "_Snapshot" : "_Production");
		}
		if (selectedForge != null)
		{
			text = text + "-Forge_" + selectedForge.VersionName;
		}
		if (selectedNeoForge != null)
		{
			text = text + "-NeoForge_" + selectedNeoForge.VersionName;
		}
		if (selectedCleanroom != null)
		{
			text = text + "-Cleanroom_" + selectedCleanroom.VersionName;
		}
		if (selectedLiteLoader != null)
		{
			text += "-LiteLoader";
		}
		if (selectedOptiFine != null)
		{
			text = text + "-OptiFine_" + selectedOptiFine.DisplayName.Replace(_vanillaName + " ", "").Replace(" ", "_");
		}
		return text;
	}

	private void SelectNameUpdate()
	{
		if (!isSelectNameEdited && !isSelectNameChanging)
		{
			isSelectNameChanging = true;
			TextSelectName.Text = GetSelectName();
			isSelectNameChanging = false;
		}
	}

	private void TextSelectName_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!isSelectNameChanging)
		{
			isSelectNameEdited = true;
			ReloadSelected();
		}
	}

	private void TextSelectName_ValidateChanged(object sender, EventArgs e)
	{
		BtnStart.IsEnabled = TextSelectName.IsValidated;
	}

	private void LoadMinecraft_OnFinish()
	{
		ExitSelectPage();
		try
		{
			if (!(ModDownload.dlClientListLoader.output.Value["versions"] is JsonArray jsonArray))
			{
				return;
			}
			_RebuildVersionCards(jsonArray);
			if (mcVersionWaitingForSelect == null)
			{
				return;
			}
			ModBase.Log("[Download] 自动选择 MC 版本：" + mcVersionWaitingForSelect);
			foreach (JsonObject item in jsonArray)
			{
				if (!((((string?)item["id"]) ?? "") != mcVersionWaitingForSelect))
				{
					MyListItem sender = ModDownloadLib.McDownloadListItem(item, (object _, MouseButtonEventArgs _) =>
					{
					}, isSaveOnly: false);
					MinecraftSelected(sender, null);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void _RebuildVersionCards(JsonArray versions)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		McVersionCategory[] array = new McVersionCategory[4] { McVersionCategory.Release, McVersionCategory.Snapshot, McVersionCategory.BeforeRelease, McVersionCategory.AprilFools };
		
		McVersionCategory[] array2 = array;
		Dictionary<McVersionCategory, List<JsonObject>> dictionary = array2.ToDictionary((McVersionCategory category) =>
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			return category;
		}, (McVersionCategory _) => new List<JsonObject>());
		foreach (JsonObject version in versions)
		{
			McVersionCategory key = McVersionClassifier.ClassifyVersion(version);
			dictionary[key].Add(version);
		}
		McVersionCategory[] array3 = array2;
		foreach (McVersionCategory key2 in array3)
		{
			dictionary[key2] = ((IEnumerable<JsonObject>)dictionary[key2]).OrderByDescending((Func<JsonObject, DateTime>)McVersionClassifier.GetReleaseTime).ToList();
		}
		PanMinecraft.Children.Clear();
		_AddLatestVersionCard(dictionary);
		_AddCategoryCards(dictionary, array2);
	}

	private void _OnLanguageChanged()
	{
		ModBase.RunInUi(() =>
		{
			LoadMinecraft.Text = Lang.Text("Download.Version.LoadingList");
			if (ModDownload.dlClientListLoader.output.Value?["versions"] is JsonArray versions)
			{
				_RebuildVersionCards(versions);
			}
		});
	}

	private void _AddLatestVersionCard(Dictionary<McVersionCategory, List<JsonObject>> dict)
	{
		JsonObject jsonObject = dict[(McVersionCategory)0].FirstOrDefault();
		JsonObject jsonObject2 = dict[(McVersionCategory)1].FirstOrDefault();
		List<JsonObject> list = new List<JsonObject>();
		if (jsonObject != null)
		{
			JsonObject jsonObject3 = (JsonObject)jsonObject.DeepClone();
			jsonObject3["lore"] = Lang.Text("Download.Version.Latest.Release", new object[1] { Lang.Date(McVersionClassifier.GetReleaseTime(jsonObject3), "g") });
			list.Add(jsonObject3);
		}
		if (jsonObject2 != null && (jsonObject == null || McVersionClassifier.GetReleaseTime(jsonObject) < McVersionClassifier.GetReleaseTime(jsonObject2)))
		{
			JsonObject jsonObject4 = (JsonObject)jsonObject2.DeepClone();
			jsonObject4["lore"] = Lang.Text("Download.Version.Latest.Development", new object[1] { Lang.Date(McVersionClassifier.GetReleaseTime(jsonObject4), "g") });
			list.Add(jsonObject4);
		}
		if (list.Count != 0)
		{
			MyCard myCard = new MyCard
			{
				Title = Lang.Text("Download.Version.Latest.Title"),
				Margin = new Thickness(0.0, 15.0, 0.0, 15.0)
			};
			StackPanel stack = _CreateVersionStack(list);
			MyCard.StackInstall(ref stack, _StackInstall);
			myCard.Children.Add(stack);
			PanMinecraft.Children.Insert(0, myCard);
		}
	}

	private void _AddCategoryCards(Dictionary<McVersionCategory, List<JsonObject>> dict, IEnumerable<McVersionCategory> categoryOrder)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		foreach (McVersionCategory item in categoryOrder)
		{
			List<JsonObject> list = dict[item];
			if (list.Count != 0)
			{
				MyCard myCard = new MyCard
				{
					Title = $"{McVersionClassifier.GetCategoryDisplayName(item)} ({list.Count})",
					Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
				};
				StackPanel stackPanel = _CreateVersionStack(list);
				myCard.Children.Add(stackPanel);
				myCard.SwapControl = stackPanel;
				myCard.InstallMethod = _StackInstall;
				myCard.IsSwapped = true;
				PanMinecraft.Children.Add(myCard);
			}
		}
	}

	private static StackPanel _CreateVersionStack(IEnumerable<JsonObject> versions)
	{
		return new StackPanel
		{
			Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransform = new TranslateTransform(0.0, 0.0),
			Tag = versions
		};
	}

	private static void _StackInstall(StackPanel stack)
	{
		foreach (object item in (IEnumerable)stack.Tag)
		{
			stack.Children.Add(ModDownloadLib.McDownloadListItem((JsonObject)item, (object sender, MouseButtonEventArgs e) =>
			{
				ModMain.frmDownloadInstall.MinecraftSelected((MyListItem)sender, e);
			}, isSaveOnly: false));
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
				return (!left.IsPreview || right.IsPreview) && McVersionComparer.CompareVersionGe(left.DisplayName, right.DisplayName);
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
			ModBase.Log(ex, "可视化 OptiFine 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void OptiFine_Selected(MyListItem sender, EventArgs e)
	{
		selectedOptiFine = (ModDownload.DlOptiFineListEntry)(dynamic)sender.Tag;
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
			ModBase.Log(ex, "可视化 LiteLoader 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void LiteLoader_Selected(MyListItem sender, EventArgs e)
	{
		selectedLiteLoader = (ModDownload.DlLiteLoaderListEntry)(dynamic)sender.Tag;
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
				if (selectedNeoForge != null || selectedFabric != null)
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
			ModBase.Log(ex, "可视化 Forge 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void Forge_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.Forge);
		selectedForge = (ModDownload.DlForgeVersionEntry)(dynamic)sender.Tag;
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
			ModBase.Log(ex, "可视化 NeoForge 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void NeoForge_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.NeoForge);
		selectedNeoForge = (ModDownload.DlNeoForgeListEntry)(dynamic)sender.Tag;
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

	private string? LoadCleanroomGetError()
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
			ModBase.Log(ex, "可视化 Cleanroom 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void Cleanroom_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.Cleanroom);
		selectedCleanroom = (ModDownload.DlCleanroomListEntry)(dynamic)sender.Tag;
		CardCleanroom.IsSwapped = true;
		OptiFine_Loaded();
		ReloadSelected();
	}

	private void Cleanroom_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedCleanroom = null;
		_selectedLoaderType = null;
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
			ModBase.Log(ex, "可视化 Fabric 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	public void Fabric_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.Fabric);
		ModBase.Log(((dynamic)sender.Tag).ToString());
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
		string displayName = fabricApi.DisplayName;
		try
		{
			if (displayName == null || _vanillaName == null)
			{
				return false;
			}
			string targetName = _vanillaName.Replace("∞", "infinite").Replace("Combat Test 7c", "1.16_combat-3").ToLower();
			return fabricApi.RawGameVersions.Any((string f) => f == targetName);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "判断 Fabric API 版本适配性出错（" + displayName + ", " + _vanillaName + "）");
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
			ModBase.Log(ex, "可视化 Fabric API 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void FabricApi_Selected(MyListItem sender, EventArgs e)
	{
		selectedFabricApi = (ModComp.CompFile)(dynamic)sender.Tag;
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
			return Lang.Text("Download.Install.State.GetVersionListFailed") + ((ModLoader.LoaderBase)LoadLegacyFabric.State).Error.Message;
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
			ModBase.Log(ex, "可视化 LegacyFabric 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
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
			return Lang.Text("Download.Install.State.GetVersionListFailed") + ((ModLoader.LoaderBase)LoadLegacyFabricApi.State).Error.Message;
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
			ModBase.Log(ex, "可视化 Legacy Fabric API 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void LegacyFabricApi_Selected(MyListItem sender, EventArgs e)
	{
		selectedLegacyFabricApi = (ModComp.CompFile)(dynamic)sender.Tag;
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
			ModBase.Log(ex, "可视化 OptiFabric 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	private void OptiFabric_Selected(MyListItem sender, EventArgs e)
	{
		selectedOptiFabric = (ModComp.CompFile)(dynamic)sender.Tag;
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
			return Lang.Text("Download.Install.State.GetVersionListFailed") + ((ModLoader.LoaderBase)LoadLabyMod.State).Error.Message;
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
			ModBase.Log(ex, "可视化 LabyMod 安装版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Install.Error.OperationFailed"));
		}
	}

	public void LabyMod_Selected(MyListItem sender, EventArgs e)
	{
		SelectModLoader(ModLoaderType.LabyMod);
		selectedLabyModChannel = ((dynamic)sender.Tag)["channel"].ToString();
		selectedLabyModCommitRef = ((dynamic)sender.Tag)["commitReference"].ToString();
		selectedLabyModBaseVersion = ((dynamic)sender.Tag)["version"].ToString();
		selectedLabyModVersion = selectedLabyModBaseVersion + " " + ((selectedLabyModChannel == "snapshot") ? Lang.Text("Download.Version.Type.Snapshot") : Lang.Text("Download.Version.Type.Stable"));
		CardLabyMod.IsSwapped = true;
		ReloadSelected();
	}

	private void LabyMod_Clear(object sender, MouseButtonEventArgs e)
	{
		selectedLabyModCommitRef = null;
		selectedLabyModVersion = null;
		selectedLabyModBaseVersion = null;
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

	private void TextSelectName_KeyDown(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Key == 6 && BtnStart.IsEnabled)
		{
			BtnStart_Click();
		}
	}

	private void BtnStart_Click()
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-install"))
		{
			return;
		}
		ModLoaderType? selectedLoaderType = _selectedLoaderType;
		if (!selectedLoaderType.HasValue || (Config.Launch.IndieSolutionV2 != 0 && Config.Launch.IndieSolutionV2 != 2) || ModMain.MyMsgBox(Lang.Text("Download.Install.InstanceIsolation.Warning.Message"), Lang.Text("Download.Install.InstanceIsolation.Warning.Title"), Lang.Text("Download.Install.InstanceIsolation.Warning.Cancel"), Lang.Text("Download.Install.InstanceIsolation.Warning.Continue")) != 1)
		{
			string text = TextSelectName.Text;
			if (ModDownloadLib.McInstall(new ModDownloadLib.McInstallRequest
			{
				targetInstanceName = text,
				targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + text + "\\",
				minecraftJson = _vanillaData?["url"].ToString(),
				minecraftName = _vanillaName,
				optiFineEntry = selectedOptiFine,
				forgeEntry = selectedForge,
				neoForgeEntry = selectedNeoForge,
				cleanroomEntry = selectedCleanroom,
				fabricVersion = selectedFabric,
				fabricApi = selectedFabricApi,
				optiFabric = selectedOptiFabric,
				liteLoaderEntry = selectedLiteLoader,
				labyModChannel = selectedLabyModChannel,
				labyModCommitRef = selectedLabyModCommitRef,
				legacyFabricVersion = selectedLegacyFabric,
				legacyFabricApi = selectedLegacyFabricApi
			}))
			{
				ExitSelectPage();
			}
		}
	}

}
