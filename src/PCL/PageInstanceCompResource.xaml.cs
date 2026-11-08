using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.UI.Theme;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageInstanceCompResource : MyPageRight, IRefreshable, IComponentConnector
{
	public enum FilterType
	{
		All,
		Enabled,
		Disabled,
		CanUpdate,
		Unavailable,
		Duplicate
	}

	private enum SortMethod
	{
		FileName,
		CompName,
		TagNums,
		CreateTime,
		ModFileSize
	}

	private readonly Dictionary<string, (DateTime CreationTime, long Length)> modFileInfoCache = new Dictionary<string, (DateTime, long)>();

	private readonly ModComp.CompType currentCompType;

	private readonly MyLocalCompItem.SwipeSelect currentSwipSelect;

	private bool isLoad;

	public Dictionary<string, MyLocalCompItem> modItems = new Dictionary<string, MyLocalCompItem>();

	private int bottomBarShownCount;

	public HashSet<string> selectedMods = new HashSet<string>();

	private SortMethod currentSortMethod = SortMethod.CompName;

	private readonly object sortLock = new object();

	public static List<string> updatingVersions = new List<string>();

	private List<ModLocalComp.LocalCompFile> searchResult;

	private CancellationTokenSource _cancelToken;

	public string CurrentFolderPath { get; set; } = "";

	public FilterType Filter
	{
		get
		{
			return field;
		}
		set
		{
			if (field != value)
			{
				field = value;
				switch (value)
				{
				case FilterType.All:
					BtnFilterAll.Checked = true;
					break;
				case FilterType.Enabled:
					BtnFilterEnabled.Checked = true;
					break;
				case FilterType.Disabled:
					BtnFilterDisabled.Checked = true;
					break;
				case FilterType.CanUpdate:
					BtnFilterCanUpdate.Checked = true;
					break;
				case FilterType.Duplicate:
					BtnFilterDuplicate.Checked = true;
					break;
				default:
					BtnFilterError.Checked = true;
					break;
				}
				RefreshUI();
			}
		}
	}

	public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);

	public PageInstanceCompResource()
	{
		InitializeComponent();
		Unloaded += Page_Unloaded;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			PageOther_Loaded();
		};
		Initialized += (object? _, EventArgs _) =>
		{
			LoaderInit();
		};
		PageExit += UnselectedAllWithAnimation;
		Load.Click += Load_Click;
		BtnManageBack.Click += BtnManageBack_Click;
		BtnHintBack.Click += BtnHintBack_Click;
		BtnManageOpen.Click += BtnManageOpen_Click;
		BtnHintOpen.Click += BtnManageOpen_Click;
		BtnManageSelectAll.Click += BtnManageSelectAll_Click;
		BtnManageInstall.Click += BtnManageInstall_Click;
		BtnHintInstall.Click += BtnManageInstall_Click;
		BtnManageInfoExport.Click += BtnManageInfoExport_Click;
		BtnManageDownload.Click += BtnManageDownload_Click;
		BtnHintDownload.Click += BtnManageDownload_Click;
		BtnSchematicDownloadMod.Click += BtnSchematicDownloadMod_Click;
		BtnSchematicVersionSelect.Click += BtnSchematicVersionSelect_Click;
		Load.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			UnselectedAllWithAnimation();
		};
		SearchBox.PreviewKeyDown += SearchBox_PreviewKeyDown;
		BtnFilterAll.Check += ChangeFilter;
		BtnFilterCanUpdate.Check += ChangeFilter;
		BtnFilterDisabled.Check += ChangeFilter;
		BtnFilterEnabled.Check += ChangeFilter;
		BtnFilterError.Check += ChangeFilter;
		BtnFilterDuplicate.Check += ChangeFilter;
		BtnSort.Click += BtnSortClick;
		BtnSelectEnable.Click += BtnSelectED_Click;
		BtnSelectDisable.Click += BtnSelectED_Click;
		BtnSelectUpdate.Click += BtnSelectUpdate_Click;
		BtnSelectDelete.Click += BtnSelectDelete_Click;
		BtnSelectCancel.Click += BtnSelectCancel_Click;
		BtnSelectFavorites.Click += BtnSelectFavorites_Click;
		BtnSelectShare.Click += BtnSelectShare_Click;
		SearchBox.TextChanged += SearchRun;
	}

	private (DateTime CreationTime, long Length) GetModFileInfo(string path)
	{
		if (modFileInfoCache.TryGetValue(path, out (DateTime, long) value))
		{
			return value;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			(DateTime, long) tuple = (fileInfo.CreationTime, fileInfo.Length);
			if (!modFileInfoCache.ContainsKey(path))
			{
				modFileInfoCache.Add(path, tuple);
			}
			return tuple;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取模组信息失败: " + path);
			return (CreationTime: DateTime.MinValue, Length: 0L);
		}
	}

	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		modFileInfoCache.Clear();
	}

	public PageInstanceCompResource(ModComp.CompType loadCompType)
	{
		currentCompType = loadCompType;
		CurrentFolderPath = "";
		currentSwipSelect = new MyLocalCompItem.SwipeSelect
		{
			TargetFrm = this
		};
		InitializeComponent();
		if (new ModComp.CompType[3]
		{
			ModComp.CompType.Shader,
			ModComp.CompType.ResourcePack,
			ModComp.CompType.Schematic
		}.Contains(currentCompType, null))
		{
			BtnSelectEnable.Visibility = Visibility.Collapsed;
			BtnSelectDisable.Visibility = Visibility.Collapsed;
		}
		if (currentCompType == ModComp.CompType.Schematic)
		{
			BtnManageDownload.Visibility = Visibility.Collapsed;
			BtnHintDownload.Visibility = Visibility.Collapsed;
		}
		if (currentCompType == ModComp.CompType.Mod)
		{
			BtnManageJarInJar.Click += (object _, MouseButtonEventArgs _) =>
			{
				ModMain.frmInstanceLeft?.PageChange(FormMain.PageSubType.DownloadNeoForge);
			};
		}
		else
		{
			BtnManageJarInJar.Visibility = Visibility.Collapsed;
		}
		Unloaded += Page_Unloaded;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			PageOther_Loaded();
		};
		LoaderInit();
		PageExit += UnselectedAllWithAnimation;
		Load.Click += Load_Click;
		BtnManageBack.Click += BtnManageBack_Click;
		BtnHintBack.Click += BtnHintBack_Click;
		BtnManageOpen.Click += BtnManageOpen_Click;
		BtnHintOpen.Click += BtnManageOpen_Click;
		BtnManageSelectAll.Click += BtnManageSelectAll_Click;
		BtnManageInstall.Click += BtnManageInstall_Click;
		BtnHintInstall.Click += BtnManageInstall_Click;
		BtnManageDownload.Click += BtnManageDownload_Click;
		BtnHintDownload.Click += BtnManageDownload_Click;
		BtnManageInfoExport.Click += BtnManageInfoExport_Click;
		BtnSchematicDownloadMod.Click += BtnSchematicDownloadMod_Click;
		BtnSchematicVersionSelect.Click += BtnSchematicVersionSelect_Click;
		Load.StateChanged += (object _, MyLoading.MyLoadingState _, MyLoading.MyLoadingState _) =>
		{
			UnselectedAllWithAnimation();
		};
		SearchBox.PreviewKeyDown += SearchBox_PreviewKeyDown;
		BtnFilterAll.Check += ChangeFilter;
		BtnFilterCanUpdate.Check += ChangeFilter;
		BtnFilterDisabled.Check += ChangeFilter;
		BtnFilterEnabled.Check += ChangeFilter;
		BtnFilterError.Check += ChangeFilter;
		BtnFilterDuplicate.Check += ChangeFilter;
		BtnSort.Click += BtnSortClick;
		BtnSelectEnable.Click += BtnSelectED_Click;
		BtnSelectDisable.Click += BtnSelectED_Click;
		BtnSelectUpdate.Click += BtnSelectUpdate_Click;
		BtnSelectDelete.Click += BtnSelectDelete_Click;
		BtnSelectCancel.Click += BtnSelectCancel_Click;
		BtnSelectFavorites.Click += BtnSelectFavorites_Click;
		BtnSelectShare.Click += BtnSelectShare_Click;
		SearchBox.TextChanged += SearchRun;
	}

	private ModLocalComp.CompLocalLoaderData GetRequireLoaderData()
	{
		ModLocalComp.CompLocalLoaderData compLocalLoaderData = new ModLocalComp.CompLocalLoaderData();
		compLocalLoaderData.gameVersion = PageInstanceLeft.McInstance;
		compLocalLoaderData.frm = this;
		List<ModComp.CompLoaderType> loaders = new List<ModComp.CompLoaderType>();
		switch (currentCompType)
		{
		case ModComp.CompType.Mod:
			loaders = ModLocalComp.GetCurrentVersionModLoader();
			break;
		case ModComp.CompType.ResourcePack:
			loaders = new ModComp.CompLoaderType[1] { ModComp.CompLoaderType.Minecraft }.ToList();
			break;
		case ModComp.CompType.Shader:
			loaders = new ModComp.CompLoaderType[4]
			{
				ModComp.CompLoaderType.OptiFine,
				ModComp.CompLoaderType.Iris,
				ModComp.CompLoaderType.Vanilla,
				ModComp.CompLoaderType.Canvas
			}.ToList();
			break;
		case ModComp.CompType.Schematic:
			loaders = new ModComp.CompLoaderType[1] { ModComp.CompLoaderType.Minecraft }.ToList();
			break;
		}
		compLocalLoaderData.loaders = loaders;
		compLocalLoaderData.compPath = PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\";
		compLocalLoaderData.compType = currentCompType;
		return compLocalLoaderData;
	}

	public void PageOther_Loaded()
	{
		CurrentFolderPath = string.Empty;
		if (ModMain.frmMain.pageLast.page != FormMain.PageType.CompDetail)
		{
			PanBack.ScrollToHome();
		}
		ModAnimation.AniControlEnabled++;
		selectedMods.Clear();
		ReloadCompFileList();
		ChangeAllSelected(value: false);
		ModAnimation.AniControlEnabled--;
		if (isLoad)
		{
			return;
		}
		isLoad = true;
		if (currentCompType == ModComp.CompType.Schematic && !States.Hint.SchematicFirstTime)
		{
			ModBase.RunInUi(() =>
			{
				ModMain.MyMsgBox(Lang.Text("Instance.Saves.Folder.DoubleClickHint.Message"), Lang.Text("Instance.Saves.Folder.DoubleClickHint.Title"), Lang.Text("Common.Action.GotIt"));
				States.Hint.SchematicFirstTime = true;
			}, forceWaitUntilLoaded: true);
		}
		ModMain.frmMain.KeyDown += FrmMain_KeyDown;
		foreach (MyRadioButton child in PanFilter.Children)
		{
			child.LabText.Margin = new Thickness(-2.0, 0.0, 8.0, 0.0);
		}
	}

	public void ReloadCompFileList(bool forceReload = false)
	{
		if (LoaderRun(forceReload ? ModLoader.LoaderFolderRunType.ForceRun : ModLoader.LoaderFolderRunType.RunOnUpdated))
		{
			ModBase.Log($"[System] 已刷新 {currentCompType} 列表");
			modFileInfoCache.Clear();
			ModBase.RunInUi(() =>
			{
				Filter = FilterType.All;
				PanBack.ScrollToHome();
				SearchBox.Text = "";
			});
		}
	}

	private void RefreshSelf()
	{
		Refresh(currentCompType);
	}

	void IRefreshable.Refresh()
	{
		RefreshSelf();
	}

	public static void Refresh(ModComp.CompType whichPage)
	{
		try
		{
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			File.Delete(ModBase.pathTemp + "Cache\\LocalComp.json");
			ModBase.Log("[CompResource] 由于点击刷新按钮，清理本地工程信息缓存");
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "强制刷新时清理本地工程信息缓存失败");
		}
		switch (whichPage)
		{
		case ModComp.CompType.Mod:
			if (ModMain.frmInstanceMod != null)
			{
				ModMain.frmInstanceMod.ReloadCompFileList(forceReload: true);
			}
			ModMain.frmInstanceLeft.ItemMod.Checked = true;
			break;
		case ModComp.CompType.ResourcePack:
			if (ModMain.frmInstanceResourcePack != null)
			{
				ModMain.frmInstanceResourcePack.ReloadCompFileList(forceReload: true);
			}
			ModMain.frmInstanceLeft.ItemResourcePack.Checked = true;
			break;
		case ModComp.CompType.Shader:
			if (ModMain.frmInstanceShader != null)
			{
				ModMain.frmInstanceShader.ReloadCompFileList(forceReload: true);
			}
			ModMain.frmInstanceLeft.ItemShader.Checked = true;
			break;
		case ModComp.CompType.Schematic:
			if (ModMain.frmInstanceSchematic != null)
			{
				ModMain.frmInstanceSchematic.ReloadCompFileList(forceReload: true);
			}
			ModMain.frmInstanceLeft.ItemSchematic.Checked = true;
			break;
		}
		HintService.Hint(Lang.Text("Instance.Left.Refreshing"), HintType.Info, log: false);
	}

	private void LoaderInit()
	{
		PageLoaderInit(Load, PanLoad, PanAllBack, null, ModLocalComp.compResourceListLoader, (ModLoader.LoaderBase _) =>
		{
			LoadUIFromLoaderOutput();
		}, () => currentCompType, autoRun: false);
	}

	private void Load_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModLocalComp.compResourceListLoader.State == ModBase.LoadState.Failed)
		{
			LoaderRun(ModLoader.LoaderFolderRunType.ForceRun);
		}
	}

	public bool LoaderRun(ModLoader.LoaderFolderRunType type)
	{
		return ModLoader.LoaderFolderRun(folderPath: (!string.IsNullOrEmpty(CurrentFolderPath)) ? CurrentFolderPath : (PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\"), loader: ModLocalComp.compResourceListLoader, type: type, maxDepth: 0, extraPath: "", waitForExit: false, loaderInput: GetRequireLoaderData());
	}

	private void EnterFolder(string folderPath)
	{
		try
		{
			if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
			{
				HintService.Hint(Lang.Text("Instance.Saves.Folder.NotFound"), HintType.Error);
				return;
			}
			CurrentFolderPath = folderPath;
			ModBase.Log("[原理图] 进入文件夹：" + folderPath);
			ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, folderPath, ModLoader.LoaderFolderRunType.ForceRun, 0, "", waitForExit: false, GetRequireLoaderData());
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "进入文件夹失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	private void EnterFolderWithCheck(string folderPath)
	{
		try
		{
			EnterFolder(folderPath);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "进入文件夹失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	private void GoBackToParentFolder()
	{
		if (string.IsNullOrEmpty(CurrentFolderPath))
		{
			return;
		}
		try
		{
			string text = PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\";
			text = Path.GetFullPath(text.TrimEnd('\\'));
			string text2 = Directory.GetParent(CurrentFolderPath)?.FullName;
			if (text2 == null || text2.Equals(text, StringComparison.OrdinalIgnoreCase) || !text2.StartsWith(text + "\\", StringComparison.OrdinalIgnoreCase))
			{
				CurrentFolderPath = "";
			}
			else
			{
				CurrentFolderPath = text2;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "路径处理失败");
			CurrentFolderPath = "";
		}
		ModBase.Log("[原理图] 返回上级文件夹：" + (string.IsNullOrEmpty(CurrentFolderPath) ? "根目录" : CurrentFolderPath));
		string loadPath;
		if (string.IsNullOrEmpty(CurrentFolderPath))
		{
			loadPath = PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\";
		}
		else
		{
			loadPath = CurrentFolderPath;
		}
		ModBase.RunInUi(() =>
		{
			BtnManageBack.Visibility = (string.IsNullOrEmpty(CurrentFolderPath) ? Visibility.Collapsed : Visibility.Visible);
		});
		ModBase.RunInUi(() =>
		{
			ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath, ModLoader.LoaderFolderRunType.ForceRun, 0, "", waitForExit: false, GetRequireLoaderData());
		}, forceWaitUntilLoaded: true);
	}

	private void LoadUIFromLoaderOutput()
	{
		try
		{
			if (ModLocalComp.compResourceListLoader.output.Any())
			{
				PanBack.Visibility = Visibility.Visible;
				PanEmpty.Visibility = Visibility.Collapsed;
				PanSchematicEmpty.Visibility = Visibility.Collapsed;
				modItems.Clear();
				string rootPath = PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\";
				rootPath = Path.GetFullPath(rootPath.TrimEnd('\\'));
				foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile item) =>
				{
					string text = Directory.GetParent(item.IsFolder ? item.ActualPath : item.path)?.FullName;
					return string.IsNullOrEmpty(CurrentFolderPath) ? text.Equals(rootPath, StringComparison.OrdinalIgnoreCase) : text.Equals(CurrentFolderPath, StringComparison.OrdinalIgnoreCase);
				}).ToList())
				{
					modItems[item.RawPath] = BuildLocalCompItem(item);
				}
				ModBase.RunInUi(() =>
				{
					Filter = FilterType.All;
					SearchBox.Text = "";
					RefreshUI();
					SetSortMethod(SortMethod.CompName);
				});
				return;
			}
			if (currentCompType == ModComp.CompType.Schematic && !Directory.Exists(PageInstanceLeft.McInstance.PathIndie + "schematics\\"))
			{
				PanSchematicEmpty.Visibility = Visibility.Visible;
				PanEmpty.Visibility = Visibility.Collapsed;
				PanBack.Visibility = Visibility.Collapsed;
				return;
			}
			if (currentCompType == ModComp.CompType.Schematic)
			{
				if (!string.IsNullOrEmpty(CurrentFolderPath))
				{
					TxtEmptyTitle.Text = Lang.Text("Instance.Resource.EmptyFolder.Title");
					TxtEmptyDescription.Text = Lang.Text("Instance.Resource.EmptyFolder.Description");
				}
				else
				{
					TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Empty.Title");
					TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Empty.Description");
				}
			}
			else
			{
				TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Empty.Title");
				TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Empty.DescriptionWithDownload");
			}
			if (!string.IsNullOrEmpty(CurrentFolderPath))
			{
				BtnHintBack.Visibility = Visibility.Visible;
			}
			else
			{
				BtnHintBack.Visibility = Visibility.Collapsed;
			}
			PanEmpty.Visibility = Visibility.Visible;
			PanBack.Visibility = Visibility.Collapsed;
			PanSchematicEmpty.Visibility = Visibility.Collapsed;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, $"加载 {currentCompType} 列表 UI 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	private MyLocalCompItem BuildLocalCompItem(ModLocalComp.LocalCompFile entry)
	{
		try
		{
			ModAnimation.AniControlEnabled++;
			MyLocalCompItem newItem = new MyLocalCompItem
			{
				SnapsToDevicePixels = true,
				Entry = entry,
				buttonHandler = BuildLocalCompItemBtnHandler,
				Checked = selectedMods.Contains(entry.RawPath)
			};
			newItem.CurrentSwipe = currentSwipSelect;
			newItem.Tags = entry.Tags;
			entry.OnCompUpdate += (ModLocalComp.LocalCompFile _) =>
			{
				newItem.Refresh();
			};
			newItem.Refresh();
			ModAnimation.AniControlEnabled--;
			return newItem;
		}
		catch (Exception ex)
		{
			ModAnimation.AniControlEnabled--;
			ModBase.Log(ex, "创建 UI 项失败：" + entry.RawPath);
			throw;
		}
	}

	private void BuildLocalCompItemBtnHandler(MyLocalCompItem sender, EventArgs e)
	{
		sender.Changed += (object ss, ModBase.RouteEventArgs ee) =>
		{
			CheckChanged((MyLocalCompItem)ss, ee);
		};
		if (sender.Entry.IsFolder)
		{
			DateTime lastClickTime = DateTime.MinValue;
			sender.Click += (object sss, MouseButtonEventArgs _) =>
			{
				MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sss;
				DateTime now = DateTime.Now;
				if ((now - lastClickTime).TotalMilliseconds <= 300.0)
				{
					EnterFolderWithCheck(myLocalCompItem.Entry.ActualPath);
				}
				else
				{
					myLocalCompItem.Checked = !myLocalCompItem.Checked;
				}
				lastClickTime = now;
			};
		}
		else
		{
			sender.Click += (object sss, MouseButtonEventArgs _) =>
			{
				MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sss;
				myLocalCompItem.Checked = !myLocalCompItem.Checked;
			};
		}
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/folder-open",
			Tag = sender
		};
		myIconButton.ToolTip = Lang.Text("Instance.Saves.OpenFileLocation");
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object ss, EventArgs ee) =>
		{
			Open_Click((MyIconButton)ss, ee);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.0,
			SvgIcon = "lucide/info",
			Tag = sender
		};
		myIconButton2.ToolTip = Lang.Text("Instance.Saves.Detail");
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += Info_Click;
		sender.MouseRightButtonUp += Info_Click;
		MyIconButton myIconButton3 = new MyIconButton
		{
			LogoScale = 1.0,
			SvgIcon = "lucide/trash-2",
			Tag = sender
		};
		myIconButton3.ToolTip = Lang.Text("Common.Action.Delete");
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton3, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton3, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton3, 2.0);
		myIconButton3.Click += (object ss, EventArgs ee) =>
		{
			Delete_Click((MyIconButton)ss, ee);
		};
		if (currentCompType != ModComp.CompType.Mod || sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
		{
			sender.Buttons = new MyIconButton[3] { myIconButton2, myIconButton, myIconButton3 };
			return;
		}
		MyIconButton myIconButton4 = new MyIconButton
		{
			LogoScale = 1.0,
			SvgIcon = ((sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? "lucide/circle-minus" : "lucide/circle-check"),
			Tag = sender
		};
		myIconButton4.ToolTip = ((sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? Lang.Text("Instance.Resource.Disable") : Lang.Text("Instance.Resource.Enable"));
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton4, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton4, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton4, 2.0);
		myIconButton4.Click += (object ss, EventArgs ee) =>
		{
			ED_Click((MyIconButton)ss, ee);
		};
		sender.Buttons = new MyIconButton[4] { myIconButton2, myIconButton, myIconButton4, myIconButton3 };
	}

	public void RefreshUI()
	{
		if (PanList == null)
		{
			return;
		}
		IEnumerable<ModLocalComp.LocalCompFile> source;
		if (!IsSearching)
		{
			source = modItems.Values.Select((MyLocalCompItem i) => i.Entry);
		}
		else
		{
			IEnumerable<ModLocalComp.LocalCompFile> enumerable = searchResult;
			source = enumerable;
		}
		List<ModLocalComp.LocalCompFile> showingMods = source.Where((ModLocalComp.LocalCompFile m) => CanPassFilter(m)).ToList();
		if (showingMods.Any())
		{
			Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> sortMethod = GetSortMethod(currentSortMethod);
			showingMods.Sort((ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) => sortMethod(a, b));
		}
		ModAnimation.AniControlEnabled++;
		if (showingMods.Any())
		{
			PanList.Visibility = Visibility.Visible;
			PanList.Children.Clear();
			foreach (ModLocalComp.LocalCompFile item in showingMods)
			{
				if (modItems.ContainsKey(item.RawPath))
				{
					MyLocalCompItem myLocalCompItem = modItems[item.RawPath];
					if (myLocalCompItem.Parent != null)
					{
						((Panel)(object)myLocalCompItem.Parent).Children.Remove(myLocalCompItem);
					}
					ModStyle.MinecraftFormatter.SetColorfulTextLab(myLocalCompItem.LabTitle.Text, myLocalCompItem.LabTitle, ThemeService.IsDarkMode);
					ModStyle.MinecraftFormatter.SetColorfulTextLab(myLocalCompItem.LabInfo.Text, myLocalCompItem.LabInfo, ThemeService.IsDarkMode);
					myLocalCompItem.Checked = selectedMods.Contains(item.RawPath);
					PanList.Children.Add(myLocalCompItem);
				}
			}
		}
		else
		{
			PanList.Visibility = Visibility.Collapsed;
		}
		ModAnimation.AniControlEnabled--;
		selectedMods = new HashSet<string>(selectedMods.Where((string m) => showingMods.Any((ModLocalComp.LocalCompFile s) => (s.RawPath ?? "") == (m ?? ""))));
		RefreshBars();
	}

	public void RefreshBars()
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			int anyCount = 0;
			int enabledCount = 0;
			int disabledCount = 0;
			int updateCount = 0;
			int unavalialeCount = 0;
			IEnumerable<ModLocalComp.LocalCompFile> source;
			if (!IsSearching)
			{
				source = modItems.Values.Select((MyLocalCompItem i) => i.Entry);
			}
			else
			{
				IEnumerable<ModLocalComp.LocalCompFile> enumerable = searchResult;
				source = enumerable;
			}
			ModLocalComp.LocalCompFile[] itemSource = source.ToArray();
			await Task.Run(() =>
			{
				ModLocalComp.LocalCompFile[] array = itemSource;
				foreach (ModLocalComp.LocalCompFile obj in array)
				{
					anyCount++;
					if (obj.CanUpdate)
					{
						updateCount++;
					}
					if (obj.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
					{
						enabledCount++;
					}
					if (obj.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
					{
						disabledCount++;
					}
					if (obj.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
					{
						unavalialeCount++;
					}
				}
			});
			BtnFilterAll.Text = (IsSearching ? Lang.Text("Instance.Resource.Filter.SearchResult") : Lang.Text("Instance.Resource.Filter.AllWithCount", new object[1] { anyCount }));
			BtnFilterCanUpdate.Text = Lang.Text("Instance.Resource.Filter.UpdatableWithCount", new object[1] { updateCount });
			BtnFilterCanUpdate.Visibility = ((Filter != FilterType.CanUpdate && updateCount <= 0) ? Visibility.Collapsed : Visibility.Visible);
			BtnFilterEnabled.Text = Lang.Text("Instance.Resource.Filter.EnabledWithCount", new object[1] { enabledCount });
			BtnFilterEnabled.Visibility = ((Filter != FilterType.Enabled && (enabledCount <= 0 || enabledCount >= anyCount)) ? Visibility.Collapsed : Visibility.Visible);
			BtnFilterDisabled.Text = Lang.Text("Instance.Resource.Filter.DisabledWithCount", new object[1] { disabledCount });
			BtnFilterDisabled.Visibility = ((Filter != FilterType.Disabled && disabledCount <= 0) ? Visibility.Collapsed : Visibility.Visible);
			BtnFilterError.Text = Lang.Text("Instance.Resource.Filter.ErrorWithCount", new object[1] { unavalialeCount });
			BtnFilterError.Visibility = ((Filter != FilterType.Unavailable && unavalialeCount <= 0) ? Visibility.Collapsed : Visibility.Visible);
			List<ModLocalComp.LocalCompFile> list = await Task.Run(() => (from m in itemSource
				group m by ((object)m.Comp == null) ? ":Nothing:" : m.Comp.Id into g
				where g.Count() > 1 && (object)g.First().Comp != null
				select g).SelectMany((IGrouping<string, ModLocalComp.LocalCompFile> g) => g).ToList());
			BtnFilterDuplicate.Text = Lang.Text("Instance.Resource.Filter.DuplicateWithCount", new object[1] { list.Count });
			BtnFilterDuplicate.Visibility = ((Filter != FilterType.Duplicate && !list.Any()) ? Visibility.Collapsed : Visibility.Visible);
			if (!string.IsNullOrEmpty(CurrentFolderPath))
			{
				BtnManageBack.Visibility = Visibility.Visible;
			}
			else
			{
				BtnManageBack.Visibility = Visibility.Collapsed;
			}
			int newCount = selectedMods.Count;
			bool selected = newCount > 0;
			if (selected)
			{
				LabSelect.Text = Lang.Text("Instance.Resource.SelectedCount", new object[1] { newCount });
			}
			if (selected)
			{
				bool hasUpdate = false;
				bool hasEnabled = false;
				bool hasDisabled = false;
				bool canFavoriteAndShare = true;
				await Task.Run(() =>
				{
					foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output)
					{
						if (selectedMods.Contains(item.RawPath))
						{
							if (item.CanUpdate)
							{
								hasUpdate = true;
							}
							if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
							{
								hasEnabled = true;
							}
							else if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
							{
								hasDisabled = true;
							}
							if ((object)item.Comp == null || string.IsNullOrEmpty(item.Comp.Id))
							{
								canFavoriteAndShare = false;
							}
						}
					}
				});
				BtnSelectDisable.IsEnabled = hasEnabled;
				BtnSelectEnable.IsEnabled = hasDisabled;
				BtnSelectUpdate.IsEnabled = hasUpdate;
				if (currentCompType == ModComp.CompType.Schematic)
				{
					BtnSelectUpdate.Visibility = Visibility.Collapsed;
					BtnSelectFavorites.Visibility = Visibility.Collapsed;
					BtnSelectShare.Visibility = Visibility.Collapsed;
				}
				else
				{
					BtnSelectUpdate.Visibility = Visibility.Visible;
					BtnSelectFavorites.Visibility = Visibility.Visible;
					BtnSelectShare.Visibility = Visibility.Visible;
					BtnSelectFavorites.IsEnabled = canFavoriteAndShare;
					BtnSelectShare.IsEnabled = canFavoriteAndShare;
				}
			}
			if (ModAnimation.AniControlEnabled == 0)
			{
				PanListBack.Margin = new Thickness(0.0, 0.0, 0.0, selected ? 95 : 15);
				if (selected)
				{
					if (bottomBarShownCount >= newCount)
					{
						bottomBarShownCount = newCount;
					}
					else
					{
						bottomBarShownCount = newCount;
						CardSelect.Visibility = Visibility.Visible;
						ModAnimation.AniStart(new ModAnimation.AniData[4]
						{
							ModAnimation.AaOpacity(CardSelect, 1.0 - CardSelect.Opacity, 60),
							ModAnimation.AaTranslateY(CardSelect, -27.0 - TransSelect.Y, 120, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
							ModAnimation.AaTranslateY(CardSelect, 3.0, 150, 120, new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak)),
							ModAnimation.AaTranslateY(CardSelect, -1.0, 90, 270, new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak))
						}, "Mod Sidebar");
					}
				}
				else if (bottomBarShownCount != 0)
				{
					bottomBarShownCount = 0;
					ModAnimation.AniStart(new ModAnimation.AniData[3]
					{
						ModAnimation.AaOpacity(CardSelect, 0.0 - CardSelect.Opacity, 90),
						ModAnimation.AaTranslateY(CardSelect, -10.0 - TransSelect.Y, 90, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
						ModAnimation.AaCode(() =>
						{
							CardSelect.Visibility = Visibility.Collapsed;
						}, 0, after: true)
					}, "Mod Sidebar");
				}
			}
			else
			{
				ModAnimation.AniStop("Mod Sidebar");
				bottomBarShownCount = newCount;
				if (selected)
				{
					CardSelect.Visibility = Visibility.Visible;
					CardSelect.Opacity = 1.0;
					TransSelect.Y = -25.0;
				}
				else
				{
					CardSelect.Visibility = Visibility.Collapsed;
					CardSelect.Opacity = 0.0;
					TransSelect.Y = -10.0;
				}
			}
		}), Array.Empty<object>());
	}

	private void BtnManageBack_Click(object sender, EventArgs e)
	{
		GoBackToParentFolder();
	}

	private void BtnHintBack_Click(object sender, EventArgs e)
	{
		GoBackToParentFolder();
	}

	private void BtnManageOpen_Click(object sender, EventArgs e)
	{
		try
		{
			string text;
			if (string.IsNullOrEmpty(CurrentFolderPath))
			{
				text = PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\";
			}
			else
			{
				text = (CurrentFolderPath.EndsWith("\\") ? CurrentFolderPath : (CurrentFolderPath + "\\"));
			}
			Directory.CreateDirectory(text);
			ModBase.OpenExplorer(text);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "打开 Mods 文件夹失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	private void BtnManageSelectAll_Click(object sender, MouseButtonEventArgs e)
	{
		ChangeAllSelected(selectedMods.Count < PanList.Children.Count);
	}

	private void BtnManageInstall_Click(object sender, MouseButtonEventArgs e)
	{
		string[] array = null;
		switch (currentCompType)
		{
		case ModComp.CompType.Mod:
			array = SystemDialogs.SelectFiles(Lang.Text("Instance.Resource.Install.FileDialog.Mod.Filter"), Lang.Text("Instance.Resource.Install.FileDialog.Mod.Title"), (string)null, true);
			break;
		case ModComp.CompType.ResourcePack:
			array = SystemDialogs.SelectFiles(Lang.Text("Instance.Resource.Install.FileDialog.ResourcePack.Filter"), Lang.Text("Instance.Resource.Install.FileDialog.ResourcePack.Title"), (string)null, true);
			break;
		case ModComp.CompType.Shader:
			array = SystemDialogs.SelectFiles(Lang.Text("Instance.Resource.Install.FileDialog.Shader.Filter"), Lang.Text("Instance.Resource.Install.FileDialog.Shader.Title"), (string)null, true);
			break;
		case ModComp.CompType.Schematic:
			array = SystemDialogs.SelectFiles(Lang.Text("Instance.Resource.Install.FileDialog.Schematic.Filter"), Lang.Text("Instance.Resource.Install.FileDialog.Schematic.Title"), (string)null, true);
			break;
		}
		if (array != null && array.Any())
		{
			InstallCompFiles(array, currentCompType, CurrentFolderPath);
		}
	}

	public static bool InstallMods(IEnumerable<string> filePathList)
	{
		if (!filePathList.Any())
		{
			return false;
		}
		string text = filePathList.First();
		string value = text.Split('.').LastOrDefault()?.ToLower();
		if (!new string[4] { "jar", "litemod", "disabled", "old" }.Contains(value))
		{
			return false;
		}
		LogWrapper.Info("[System] 文件格式为 jar/litemod，尝试安装为 Mod");
		if (text.Contains(":\\$RECYCLE.BIN\\"))
		{
			HintWrapper.Show(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), (HintTheme)2);
			return true;
		}
		McInstance mcInstance = ModInstanceList.McMcInstanceSelected;
		if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup)
		{
			mcInstance = PageInstanceLeft.McInstance;
		}
		if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect || (object)mcInstance == null || !mcInstance.Modable)
		{
			HintWrapper.Show(Lang.Text("Instance.Resource.Install.SelectModableInstance"), (HintTheme)0);
			return true;
		}
		bool flag = ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup && ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.DownloadResourcePack;
		if (!flag && ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.ModConfirm.Message", new object[1] { mcInstance.Name }), Lang.Text("Instance.Resource.Install.ModConfirm.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 1)
		{
			return true;
		}
		ExecuteModInstallation(mcInstance, filePathList, flag);
		return true;
	}

	private static void ExecuteModInstallation(McInstance targetMcInstance, IEnumerable<string> filePathList, bool refreshList)
	{
		string text = (targetMcInstance.Info.HasLabyMod ? ("labymod-neo\\fabric\\" + targetMcInstance.Info.VanillaName + "\\") : "");
		string text2 = targetMcInstance.PathIndie + text + "mods\\";
		try
		{
			foreach (string filePath in filePathList)
			{
				string text3 = ModBase.GetFileNameFromPath(filePath).Replace(".disabled", "").Replace(".old", "");
				if (!text3.Contains("."))
				{
					text3 += ".jar";
				}
				ModBase.CopyFile(filePath, Path.Combine(text2, text3));
			}
			if (filePathList.Count() == 1)
			{
				string text4 = ModBase.GetFileNameFromPath(filePathList.First()).Replace(".disabled", "").Replace(".old", "");
				HintWrapper.Show(Lang.Text("Instance.Resource.Install.SuccessSingle", new object[1] { text4 }), (HintTheme)1);
			}
			else
			{
				HintWrapper.Show(Lang.Text("Instance.Resource.Install.SuccessMultiple", new object[2]
				{
					filePathList.Count(),
					Lang.Text("Download.Comp.Type.Mod")
				}), (HintTheme)1);
			}
			if (refreshList)
			{
				ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, text2, ModLoader.LoaderFolderRunType.ForceRun, 0, "", waitForExit: false, ModMain.frmInstanceMod.GetRequireLoaderData());
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "拷贝文件失败");
		}
	}

	public static void InstallCompFiles(IEnumerable<string> filePathList, ModComp.CompType compType, string targetFolderPath = "")
	{
		if (!filePathList.Any())
		{
			return;
		}
		string text = filePathList.First().AfterLast(".").ToLower();
		string[] array = null;
		string text2 = "";
		string text3 = "";
		if (filePathList.First().Contains(":\\$RECYCLE.BIN\\"))
		{
			HintService.Hint(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintType.Error);
			return;
		}
		McInstance mcInstance = ModInstanceList.McMcInstanceSelected;
		if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup)
		{
			mcInstance = PageInstanceLeft.McInstance;
		}
		switch (compType)
		{
		case ModComp.CompType.Mod:
			array = new string[4] { "jar", "litemod", "disabled", "old" };
			text2 = "Mod";
			text3 = ((!string.IsNullOrEmpty(targetFolderPath)) ? targetFolderPath : (mcInstance.PathIndie + (mcInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", mcInstance.Info.VanillaName) : "") + "mods\\"));
			break;
		case ModComp.CompType.ResourcePack:
			array = new string[1] { "zip" };
			text2 = Lang.Text("Download.Comp.Type.ResourcePack");
			text3 = ((!string.IsNullOrEmpty(targetFolderPath)) ? targetFolderPath : (mcInstance.PathIndie + "resourcepacks\\"));
			break;
		case ModComp.CompType.Shader:
			array = new string[1] { "zip" };
			text2 = Lang.Text("Download.Comp.Type.Shader");
			text3 = ((!string.IsNullOrEmpty(targetFolderPath)) ? targetFolderPath : (mcInstance.PathIndie + "shaderpacks\\"));
			break;
		case ModComp.CompType.Schematic:
			array = new string[4] { "litematic", "nbt", "schematic", "schem" };
			text2 = Lang.Text("Download.Comp.Type.Schematic");
			text3 = ((!string.IsNullOrEmpty(targetFolderPath)) ? targetFolderPath : (mcInstance.PathIndie + "schematics\\"));
			break;
		}
		if (!array.Contains(text))
		{
			HintService.Hint(Lang.Text("Instance.Resource.Install.UnsupportedFormat", new object[3]
			{
				text,
				text2,
				string.Join(", ", array)
			}), HintType.Error);
			return;
		}
		ModBase.Log($"[System] 文件为 {text} 格式，尝试作为{text2}安装");
		if (compType == ModComp.CompType.Mod && (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSelect || (object)mcInstance == null || !mcInstance.Modable))
		{
			HintService.Hint(Lang.Text("Instance.Resource.Install.SelectModableInstance"));
			return;
		}
		FormMain.PageSubType pageSubType = FormMain.PageSubType.DownloadResourcePack;
		switch (compType)
		{
		case ModComp.CompType.Mod:
			pageSubType = FormMain.PageSubType.DownloadResourcePack;
			break;
		case ModComp.CompType.ResourcePack:
			pageSubType = FormMain.PageSubType.DownloadWorld;
			break;
		case ModComp.CompType.Shader:
			pageSubType = FormMain.PageSubType.DownloadCompFavorites;
			break;
		case ModComp.CompType.Schematic:
			pageSubType = FormMain.PageSubType.DownloadClient;
			break;
		}
		if ((!(ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup) || ModMain.frmMain.PageCurrentSub != pageSubType) && ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.GenericConfirm.Message", new object[2] { text2, mcInstance.Name }), Lang.Text("Instance.Resource.Install.GenericConfirm.Title", new object[1] { text2 }), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 1)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(text3);
			foreach (string filePath in filePathList)
			{
				string text4 = ModBase.GetFileNameFromPath(filePath);
				if (compType == ModComp.CompType.Mod)
				{
					text4 = text4.Replace(".disabled", "").Replace(".old", "");
					if (!text4.Contains("."))
					{
						text4 += ".jar";
					}
				}
				string text5 = text3 + text4;
				if (!File.Exists(text5) || ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.OverwriteConfirm.Message", new object[1] { text4 }), Lang.Text("Instance.Resource.Install.OverwriteConfirm.Title"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) == 1)
				{
					ModBase.CopyFile(filePath, text5);
				}
			}
			if (filePathList.Count() == 1)
			{
				HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessSingle", new object[1] { ModBase.GetFileNameFromPath(filePathList.First()) }), HintType.Success);
			}
			else
			{
				HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessMultiple", new object[2]
				{
					filePathList.Count(),
					text2
				}), HintType.Success);
			}
			if (!(ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup) || ModMain.frmMain.PageCurrentSub != pageSubType)
			{
				return;
			}
			switch (compType)
			{
			case ModComp.CompType.Mod:
				if (ModMain.frmInstanceMod != null)
				{
					ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, text3, ModLoader.LoaderFolderRunType.ForceRun, 0, "", waitForExit: false, ModMain.frmInstanceMod?.GetRequireLoaderData());
				}
				break;
			case ModComp.CompType.ResourcePack:
			case ModComp.CompType.Shader:
			case ModComp.CompType.Schematic:
			{
				PageInstanceCompResource currentForm = GetCurrentCompResourceForm();
				if (currentForm != null)
				{
					ModBase.RunInUi(() =>
					{
						currentForm.ReloadCompFileList(forceReload: true);
					});
				}
				break;
			}
			case ModComp.CompType.ModPack:
			case ModComp.CompType.DataPack:
			case ModComp.CompType.Plugin:
				break;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "复制" + text2 + "文件失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	private static PageInstanceCompResource GetCurrentCompResourceForm()
	{
		return ModMain.frmMain.PageCurrentSub switch
		{
			FormMain.PageSubType.DownloadResourcePack => ModMain.frmInstanceMod, 
			FormMain.PageSubType.DownloadWorld => ModMain.frmInstanceResourcePack, 
			FormMain.PageSubType.DownloadCompFavorites => ModMain.frmInstanceShader, 
			FormMain.PageSubType.DownloadClient => ModMain.frmInstanceSchematic, 
			_ => null, 
		};
	}

	private void BtnManageInfoExport_Click(object sender, MouseButtonEventArgs e)
	{
		switch (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Export.Mode.Message"), Lang.Text("Instance.Resource.Export.Mode.Title"), Lang.Text("Instance.Resource.Export.Mode.Txt"), Lang.Text("Instance.Resource.Export.Mode.Csv"), Lang.Text("Common.Action.Cancel")))
		{
		case 1:
		{
			List<string> list2 = new List<string>();
			foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output)
			{
				list2.Add(item.FileName);
				_AppendEmbeddedForExport(list2, item.EmbeddedMods, 1);
			}
			ExportText(list2.Join("\r\n"), PageInstanceLeft.McInstance.Name + "已安装的资源信息.txt");
			break;
		}
		case 2:
		{
			List<string> list = new List<string>();
			list.Add("文件名,资源名称,资源版本,此版本更新时间,Mod ID,对应平台工程 ID,文件大小（字节）,文件路径,内嵌模组");
			foreach (ModLocalComp.LocalCompFile item2 in ModLocalComp.compResourceListLoader.output)
			{
				list.Add($"{item2.FileName},{item2.Comp?.TranslatedName},{item2.Version},{item2.compFile?.ReleaseDate},{item2.ModId},{item2.Comp?.Id},{GetModFileInfo(item2.path).Length},{item2.path},{string.Join(";", _FlattenEmbeddedNames(item2.EmbeddedMods))}");
			}
			ExportText(list.Join("\r\n"), PageInstanceLeft.McInstance.Name + "已安装的资源信息.csv");
			break;
		}
		}
		static void ExportText(string content, string fileName)
		{
			try
			{
				string text = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Resource.Export.SelectSaveLocation"), fileName, Lang.Text("Instance.Resource.Export.FilesFilter"), (string)null);
				if (!string.IsNullOrWhiteSpace(text))
				{
					File.WriteAllText(text, content, Encoding.UTF8);
					ModBase.OpenExplorer(text);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "导出资源信息失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
			}
		}
	}

	private static void _AppendEmbeddedForExport(List<string> lines, List<ModLocalComp.LocalCompFile> mods, int depth)
	{
		string text = new string('\t', depth);
		foreach (ModLocalComp.LocalCompFile mod in mods)
		{
			string text2 = text + "└ " + (mod.Name ?? mod.ModId ?? mod.FileName);
			if (!string.IsNullOrWhiteSpace(mod.Version))
			{
				text2 = text2 + " (" + mod.Version + ")";
			}
			lines.Add(text2);
			List<ModLocalComp.LocalCompFile> embeddedMods = mod.EmbeddedMods;
			if (embeddedMods != null && embeddedMods.Count > 0)
			{
				_AppendEmbeddedForExport(lines, mod.EmbeddedMods, depth + 1);
			}
		}
	}

	private static IEnumerable<string> _FlattenEmbeddedNames(List<ModLocalComp.LocalCompFile> mods)
	{
		foreach (ModLocalComp.LocalCompFile mod in mods)
		{
			yield return mod.Name ?? mod.ModId ?? mod.FileName;
			List<ModLocalComp.LocalCompFile> embeddedMods = mod.EmbeddedMods;
			if (embeddedMods == null || embeddedMods.Count <= 0)
			{
				continue;
			}
			foreach (string item in _FlattenEmbeddedNames(mod.EmbeddedMods))
			{
				yield return item;
			}
		}
	}

	private void BtnManageDownload_Click(object sender, MouseButtonEventArgs e)
	{
		switch (currentCompType)
		{
		case ModComp.CompType.Mod:
			ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod);
			break;
		case ModComp.CompType.ResourcePack:
			ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadResourcePack);
			break;
		case ModComp.CompType.Shader:
			ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadShader);
			break;
		}
		PageComp.targetVersion = PageInstanceLeft.McInstance;
	}

	private void BtnSchematicDownloadMod_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod);
		PageComp.targetVersion = PageInstanceLeft.McInstance;
	}

	private void BtnSchematicVersionSelect_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Launch);
		ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
	}

	public void CheckChanged(MyLocalCompItem sender, ModBase.RouteEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			string rawPath = sender.Entry.RawPath;
			if (sender.Checked)
			{
				selectedMods.Add(rawPath);
			}
			else
			{
				selectedMods.Remove(rawPath);
			}
			RefreshBars();
		}
	}

	private void ChangeAllSelected(bool value)
	{
		ModAnimation.AniControlEnabled++;
		selectedMods.Clear();
		foreach (MyLocalCompItem value2 in modItems.Values)
		{
			if (value2.Checked = value && PanList.Children.Contains(value2))
			{
				selectedMods.Add(value2.Entry.RawPath);
			}
		}
		ModAnimation.AniControlEnabled--;
		RefreshBars();
	}

	private void UnselectedAllWithAnimation()
	{
		int aniControlEnabled = ModAnimation.AniControlEnabled;
		ModAnimation.AniControlEnabled = 0;
		ChangeAllSelected(value: false);
		ModAnimation.AniControlEnabled += aniControlEnabled;
	}

	private void FrmMain_KeyDown(object sender, KeyEventArgs e)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		if (ModMain.frmMain.pageRight == this && (Keyboard.IsKeyDown((Key)118) || Keyboard.IsKeyDown((Key)119)) && (int)e.Key == 44)
		{
			ChangeAllSelected(value: true);
		}
	}

	private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		if (!SearchBox.Text.Any() && (Keyboard.IsKeyDown((Key)118) || Keyboard.IsKeyDown((Key)119)) && (int)e.Key == 44)
		{
			PanBack.Focus();
		}
	}

	private bool CanPassFilter(ModLocalComp.LocalCompFile checkingMod)
	{
		switch (Filter)
		{
		case FilterType.All:
			return true;
		case FilterType.Enabled:
			return checkingMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine;
		case FilterType.Disabled:
			return checkingMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled;
		case FilterType.CanUpdate:
			return checkingMod.CanUpdate;
		case FilterType.Unavailable:
			return checkingMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable;
		case FilterType.Duplicate:
		{
			List<ModLocalComp.LocalCompFile> list = (IsSearching ? searchResult : (ModLocalComp.compResourceListLoader.output ?? new List<ModLocalComp.LocalCompFile>()));
			if (list != null)
			{
				return list.Where((ModLocalComp.LocalCompFile m) => (object)checkingMod.Comp != null && (object)m.Comp != null && (checkingMod.Comp.Id ?? "") == (m.Comp.Id ?? "")).Skip(1).Any();
			}
			return false;
		}
		default:
			return false;
		}
	}

	private void ChangeFilter(MyRadioButton sender, bool raiseByMouse)
	{
		Filter = (FilterType)Convert.ToInt32(sender.Tag);
		RefreshUI();
		DoSort();
	}

	private void SetSortMethod(SortMethod target)
	{
		currentSortMethod = target;
		BtnSort.Text = Lang.Text("Instance.Resource.Sort.Text", new object[1] { GetSortName(target) });
		DoSort();
	}

	private string GetSortName(SortMethod method)
	{
		return method switch
		{
			SortMethod.FileName => Lang.Text("Instance.Resource.Sort.FileName"), 
			SortMethod.CompName => Lang.Text("Instance.Resource.Sort.ResourceName"), 
			SortMethod.TagNums => Lang.Text("Instance.Resource.Sort.TagCount"), 
			SortMethod.CreateTime => Lang.Text("Instance.Resource.Sort.AddTime"), 
			SortMethod.ModFileSize => Lang.Text("Instance.Resource.Sort.FileSize"), 
			_ => Lang.Text("Instance.Resource.Sort.ResourceName"), 
		};
	}

	private void BtnSortClick(object sender, ModBase.RouteEventArgs e)
	{
		ContextMenu contextMenu = new ContextMenu();
		foreach (SortMethod i in Enum.GetValues(typeof(SortMethod)))
		{
			MyMenuItem myMenuItem = new MyMenuItem();
			myMenuItem.Header = GetSortName(i);
			myMenuItem.Click += (object _, RoutedEventArgs _) =>
			{
				SetSortMethod(i);
			};
			contextMenu.Items.Add(myMenuItem);
		}
		contextMenu.PlacementTarget = (UIElement)sender;
		contextMenu.Placement = PlacementMode.Bottom;
		contextMenu.IsOpen = true;
	}

	private void DoSort()
	{
		lock (sortLock)
		{
			try
			{
				if (PanList != null && PanList.Children.Count >= 2)
				{
					List<MyLocalCompItem> list = PanList.Children.OfType<MyLocalCompItem>().ToList();
					Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> method = GetSortMethod(currentSortMethod);
					List<MyLocalCompItem> second = list.Where((MyLocalCompItem i) => i.Entry == null || (currentSortMethod == SortMethod.TagNums && (object)i.Entry.Comp == null && !i.Entry.IsFolder)).ToList();
					List<MyLocalCompItem> list2 = list.Except(second).ToList();
					list2.Sort((MyLocalCompItem x, MyLocalCompItem y) => method(x.Entry, y.Entry));
					List<MyLocalCompItem> list3 = list2.Concat(second).ToList();
					PanList.Children.Clear();
					list3.ForEach((MyLocalCompItem i) =>
					{
						PanList.Children.Add(i);
					});
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "执行排序时出错", ModBase.LogLevel.Hint, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
			}
		}
	}

	private Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> GetSortMethod(SortMethod method)
	{
		return method switch
		{
			SortMethod.FileName => (Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int>)((ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				int num = folderFirstCompare(a, b);
				return (num != 0) ? num : string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
			}), 
			SortMethod.CompName => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				int num = folderFirstCompare(a, b);
				return (num != 0) ? num : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			}, 
			SortMethod.TagNums => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				int num = folderFirstCompare(a, b);
				if (num != 0)
				{
					return num;
				}
				if (a.IsFolder && b.IsFolder)
				{
					return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
				}
				if (!a.IsFolder && !b.IsFolder)
				{
					int valueOrDefault = (a.Comp?.Tags?.Count).GetValueOrDefault();
					return (b.Comp?.Tags?.Count).GetValueOrDefault().CompareTo(valueOrDefault);
				}
				return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			}, 
			SortMethod.CreateTime => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				int num = folderFirstCompare(a, b);
				if (num != 0)
				{
					return num;
				}
				string path = (a.IsFolder ? a.ActualPath : a.path);
				string path2 = (b.IsFolder ? b.ActualPath : b.path);
				DateTime item = GetModFileInfo(path).CreationTime;
				DateTime item2 = GetModFileInfo(path2).CreationTime;
				if (item == DateTime.MinValue && item2 == DateTime.MinValue)
				{
					return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
				}
				if (item == DateTime.MinValue)
				{
					return 1;
				}
				return (item2 == DateTime.MinValue) ? (-1) : item2.CompareTo(item);
			}, 
			SortMethod.ModFileSize => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				int num = folderFirstCompare(a, b);
				if (num != 0)
				{
					return num;
				}
				if (a.IsFolder && b.IsFolder)
				{
					return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
				}
				if (!a.IsFolder && !b.IsFolder)
				{
					long item = GetModFileInfo(a.ActualPath).Length;
					long item2 = GetModFileInfo(b.ActualPath).Length;
					if (item == 0L && item2 == 0L)
					{
						return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
					}
					if (item == 0L)
					{
						return 1;
					}
					if (item2 == 0L)
					{
						return -1;
					}
					return item2.CompareTo(item);
				}
				return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			}, 
			_ => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				int num = folderFirstCompare(a, b);
				return (num != 0) ? num : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			}, 
		};
		static int folderFirstCompare(ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b)
		{
			if (a.IsFolder && !b.IsFolder)
			{
				return -1;
			}
			if (!a.IsFolder && b.IsFolder)
			{
				return 1;
			}
			return 0;
		}
	}

	private void BtnSelectED_Click(object sender, ModBase.RouteEventArgs e)
	{
		EDMods(ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedMods.Contains(m.RawPath)).ToList(), !sender.Equals(BtnSelectDisable));
		ChangeAllSelected(value: false);
	}

	private void EDMods(IEnumerable<ModLocalComp.LocalCompFile> modList, bool isEnable)
	{
		List<ModLocalComp.LocalCompFile> list = modList.ToList();
		if (!isEnable)
		{
			if (!_ConfirmCascadeReady())
			{
				return;
			}
			List<ModLocalComp.LocalCompFile> list2 = _JijFindAffected(list);
			if (list2.Count > 0)
			{
				int? num = _AskJijCascade(list2, isDelete: false);
				if (!num.HasValue)
				{
					return;
				}
				if (num == 1)
				{
					list = list.Concat(list2).ToList();
				}
			}
		}
		EDModsCore(list, isEnable);
	}

	private void DeleteMods(IEnumerable<ModLocalComp.LocalCompFile> modList)
	{
		bool isShiftPressed = Keyboard.IsKeyDown((Key)116) || Keyboard.IsKeyDown((Key)117);
		if (!_ConfirmCascadeReady())
		{
			return;
		}
		List<ModLocalComp.LocalCompFile> list = modList.ToList();
		List<ModLocalComp.LocalCompFile> list2 = _JijFindAffected(list);
		if (list2.Count > 0)
		{
			int? num = _AskJijCascade(list2, isDelete: true);
			if (!num.HasValue)
			{
				return;
			}
			switch (num)
			{
			case 1:
				if (!EDModsCore(list2, isEnable: false))
				{
					return;
				}
				break;
			case 2:
				if (!EDModsCore(list2, isEnable: false))
				{
					return;
				}
				list = list.Concat(list2).ToList();
				break;
			}
		}
		DeleteModsCore(list, isShiftPressed);
	}

	private bool _ConfirmCascadeReady()
	{
		if (currentCompType != ModComp.CompType.Mod || ModLocalComp.CompJijResolved)
		{
			return true;
		}
		if (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Mod.JarInJar.ParsePending.Message"), Lang.Text("Instance.Resource.Mod.JarInJar.ParsePending.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
		{
			return false;
		}
		List<ModLocalComp.LocalCompFile> output = ModLocalComp.compResourceListLoader.output;
		if (output == null)
		{
			return false;
		}
		bool flag = true;
		try
		{
			ModJarInJarCache.UseInstance(PageInstanceLeft.McInstance?.PathInstance);
			ModLocalComp.JijScanContext = new ModJarInJar.ScanContext();
			foreach (ModLocalComp.LocalCompFile item in output.Where((ModLocalComp.LocalCompFile m) => !m.IsFolder && m.JijPending))
			{
				flag &= item.ResolveJijNow();
			}
			if (flag)
			{
				ModJarInJarCache.Prune(from m in output
					where !m.IsFolder
					select m.path);
				ModJarInJarCache.Flush();
			}
		}
		finally
		{
			ModLocalComp.JijScanContext = null;
			ModJarInJarCache.UseInstance(null);
		}
		ModLocalComp.CompJijResolved = flag;
		ModMain.frmInstanceModJarInJar?.OnJijResolved();
		if (!flag)
		{
			HintService.Hint(Lang.Text("Instance.Resource.Ed.ToggleFailed"), HintType.Error);
		}
		return flag;
	}

	private List<ModLocalComp.LocalCompFile> _JijFindAffected(List<ModLocalComp.LocalCompFile> targets)
	{
		if (currentCompType != ModComp.CompType.Mod)
		{
			return new List<ModLocalComp.LocalCompFile>();
		}
		List<ModLocalComp.LocalCompFile> output = ModLocalComp.compResourceListLoader.output;
		if (output == null)
		{
			return new List<ModLocalComp.LocalCompFile>();
		}
		return new ModJarInJarIndex(output, PageInstanceLeft.McInstance?.Info?.VanillaName).FindAffected(targets);
	}

	private int? _AskJijCascade(List<ModLocalComp.LocalCompFile> affected, bool isDelete)
	{
		List<IMyRadio> list = new List<IMyRadio>
		{
			new MyRadioBox
			{
				Text = Lang.Text("Instance.Resource.Mod.JarInJar.Cascade.OnlySelf")
			},
			new MyRadioBox
			{
				Text = Lang.Text("Instance.Resource.Mod.JarInJar.Cascade.WithDisable")
			}
		};
		if (isDelete)
		{
			list.Add(new MyRadioBox
			{
				Text = Lang.Text("Instance.Resource.Mod.JarInJar.Cascade.WithDelete")
			});
		}
		return ModMain.MyMsgBoxSelect(list, Lang.Text(isDelete ? "Instance.Resource.Mod.JarInJar.Cascade.DeleteTitle" : "Instance.Resource.Mod.JarInJar.Cascade.DisableTitle", new object[1] { affected.Count }), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true);
	}

	private bool EDModsCore(IEnumerable<ModLocalComp.LocalCompFile> modList, bool isEnable)
	{
		bool flag = true;
		foreach (ModLocalComp.LocalCompFile mod in modList)
		{
			ModLocalComp.LocalCompFile modEntity = mod;
			if (currentCompType == ModComp.CompType.Mod && modEntity.JijPending)
			{
				bool flag2 = false;
				try
				{
					ModJarInJarCache.UseInstance(PageInstanceLeft.McInstance?.PathInstance);
					ModLocalComp.JijScanContext = new ModJarInJar.ScanContext();
					flag2 = modEntity.ResolveJijNow();
					if (flag2)
					{
						ModJarInJarCache.Flush();
					}
				}
				finally
				{
					ModLocalComp.JijScanContext = null;
					ModJarInJarCache.UseInstance(null);
				}
				if (!flag2)
				{
					HintService.Hint(Lang.Text("Instance.Resource.Ed.ToggleFailed"), HintType.Error);
					flag = false;
					continue;
				}
			}
			string text = null;
			if (modEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && !isEnable)
			{
				text = modEntity.path + (File.Exists(modEntity.path + ".old") ? ".old" : ".disabled");
			}
			else
			{
				if (!((modEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled) & isEnable))
				{
					continue;
				}
				text = modEntity.RawPath;
			}
			try
			{
				if (!File.Exists(text))
				{
					goto IL_01b9;
				}
				if (File.Exists(modEntity.path))
				{
					if (!((ModBase.GetFileMD5(modEntity.path) ?? "") != (ModBase.GetFileMD5(text) ?? "")))
					{
						goto IL_01b9;
					}
					ModMain.MyMsgBox(Lang.Text("Instance.Resource.Ed.FileConflict.Message", new object[2] { text, modEntity.path }), Lang.Text("Instance.Resource.Ed.FileConflict"));
					flag = false;
				}
				else
				{
					ModBase.Log("[Mod] Mod 的状态已被切换", ModBase.LogLevel.Debug);
				}
				continue;
				IL_01b9:
				File.Delete(text);
				Microsoft.VisualBasic.FileSystem.Rename(modEntity.path, text);
				goto IL_0249;
			}
			catch (FileNotFoundException ex)
			{
				ModBase.Log(ex, "未找到需要重命名的 Mod（" + (modEntity.path ?? "null") + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
				ReloadCompFileList(forceReload: true);
				return false;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "重命名 Mod 失败（" + (modEntity.path ?? "null") + "）");
				flag = false;
			}
			continue;
			IL_0249:
			ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(text);
			localCompFile.FromJson(modEntity.ToJson());
			localCompFile.CopyLoadedStateFrom(modEntity);
			if (ModLocalComp.compResourceListLoader.output.Contains(modEntity))
			{
				int index = ModLocalComp.compResourceListLoader.output.IndexOf(modEntity);
				ModLocalComp.compResourceListLoader.output.RemoveAt(index);
				ModLocalComp.compResourceListLoader.output.Insert(index, localCompFile);
			}
			if (searchResult != null && searchResult.Contains(modEntity))
			{
				int index2 = searchResult.IndexOf(modEntity);
				searchResult.Remove(modEntity);
				searchResult.Insert(index2, localCompFile);
			}
			try
			{
				MyLocalCompItem myLocalCompItem = BuildLocalCompItem(localCompFile);
				modItems[modEntity.RawPath] = myLocalCompItem;
				int num = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>().FirstOrDefault((MyLocalCompItem i) => i.Entry == modEntity));
				if (num != -1)
				{
					PanList.Children.RemoveAt(num);
					PanList.Children.Insert(num, myLocalCompItem);
				}
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "更新 UI 列表项失败：" + modEntity.FileName, ModBase.LogLevel.Hint, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
			}
		}
		((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
		{
			PanList.UpdateLayout();
		}), (DispatcherPriority)4);
		if (flag)
		{
			RefreshBars();
		}
		else
		{
			HintService.Hint(Lang.Text("Instance.Resource.Ed.ToggleFailed"), HintType.Error);
			ReloadCompFileList(forceReload: true);
		}
		LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
		return flag;
	}

	private void BtnSelectUpdate_Click(object sender, ModBase.RouteEventArgs e)
	{
		List<ModLocalComp.LocalCompFile> list = ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedMods.Contains(m.RawPath) && m.CanUpdate).ToList();
		if (list.Any())
		{
			UpdateResource(list);
			ChangeAllSelected(value: false);
		}
	}

	public void UpdateResource(IEnumerable<ModLocalComp.LocalCompFile> modList)
	{
		if (currentCompType == ModComp.CompType.Mod && (!States.Hint.UpdateMod || modList.Count() >= 15))
		{
			if (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Update.Warning.Message"), Lang.Text("Instance.Resource.Update.Warning.Title"), Lang.Text("Instance.Resource.Update.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
			{
				return;
			}
			States.Hint.UpdateMod = true;
		}
		try
		{
			modList = modList.ToList();
			List<DownloadFile> list = new List<DownloadFile>();
			Dictionary<string, string> fileCopyList = new Dictionary<string, string>();
			foreach (ModLocalComp.LocalCompFile mod in modList)
			{
				ModComp.CompFile updateFile = mod.UpdateFile;
				if (updateFile.Available)
				{
					string text = mod.compFile.FileName.Replace(".jar", "").Replace(".old", "").Replace(".disabled", "");
					string text2 = mod.UpdateFile.FileName.Replace(".jar", "").Replace(".old", "").Replace(".disabled", "");
					List<string> list2 = text.Split('-').ToList();
					List<string> list3 = text2.Split('-').ToList();
					bool flag = false;
					while (list2.Any() && list3.Any() && !((list2.First() ?? "") != (list3.First() ?? "")))
					{
						list2.RemoveAt(0);
						list3.RemoveAt(0);
						flag = true;
					}
					while (list2.Any() && list3.Any() && !((list2.Last() ?? "") != (list3.Last() ?? "")))
					{
						list2.RemoveAt(list2.Count - 1);
						list3.RemoveAt(list3.Count - 1);
						flag = true;
					}
					if (flag && list2.Any() && list3.Any())
					{
						text = list2.Join("-");
						text2 = list3.Join("-");
					}
					string text3 = ModBase.pathTemp + "DownloadedComp\\" + mod.FileName.Replace(text, text2);
					string value = ModBase.GetPathFromFullPath(mod.path) + mod.FileName.Replace(text, text2);
					list.Add(updateFile.ToNetFile(text3, ModComp.DownloadReason.Update));
					fileCopyList[text3] = value;
				}
			}
			List<ModLoader.LoaderBase> list4 = new List<ModLoader.LoaderBase>();
			List<string> finishedFileNames = new List<string>();
			list4.Add(new LoaderDownload(Lang.Text("Instance.Resource.Update.Task.DownloadFiles"), list)
			{
				ProgressWeight = (double)modList.Count() * 1.5
			});
			list4.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Resource.Update.Task.ReplaceFiles"), (ModLoader.LoaderTask<int, int> _) =>
			{
				try
				{
					foreach (ModLocalComp.LocalCompFile mod2 in modList)
					{
						if (File.Exists(mod2.path))
						{
							Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(mod2.path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
						}
						else
						{
							ModBase.Log("[CompUpdate] 未找到更新前的资源文件，跳过对它的删除：" + mod2.path, ModBase.LogLevel.Debug);
						}
					}
					foreach (KeyValuePair<string, string> item in fileCopyList)
					{
						if (File.Exists(item.Value))
						{
							Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(item.Value, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
							ModBase.Log("[Mod] 更新后的资源文件已存在，将会把它放入回收站：" + item.Value, ModBase.LogLevel.Debug);
						}
						if (Directory.Exists(ModBase.GetPathFromFullPath(item.Value)))
						{
							File.Move(item.Key, item.Value);
							finishedFileNames.Add(ModBase.GetFileNameFromPath(item.Value));
						}
						else
						{
							ModBase.Log("[Mod] 更新后的目标文件夹已被删除：" + item.Value, ModBase.LogLevel.Debug);
						}
					}
				}
				catch (OperationCanceledException ex2)
				{
					ModBase.Log(ex2, "替换旧版资源文件时被主动取消");
				}
			}));
			ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>> loader = new ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>>(Lang.Text("Instance.Resource.Update.Task.Title", new object[1] { PageInstanceLeft.McInstance.Name }), list4);
			string pathMods = PageInstanceLeft.McInstance.PathIndie + (PageInstanceLeft.McInstance.Info.HasLabyMod ? Path.Combine("labymod-neo", "fabric", PageInstanceLeft.McInstance.Info.VanillaName) : "") + ModLocalComp.GetPathNameByCompType(currentCompType) + "\\";
			loader.OnStateChanged = (ModLoader.LoaderBase _) =>
			{
				switch (loader.State)
				{
				default:
					return;
				case ModBase.LoadState.Finished:
					switch (finishedFileNames.Count)
					{
					case 0:
						ModBase.Log("[CompUpdate] 没有资源被成功更新");
						break;
					case 1:
						HintService.Hint(Lang.Text("Instance.Resource.Update.SuccessSingle", new object[1] { finishedFileNames.Single() }), HintType.Success);
						break;
					default:
						HintService.Hint(Lang.Text("Instance.Resource.Update.SuccessMultiple", new object[1] { finishedFileNames.Count }), HintType.Success);
						break;
					}
					break;
				case ModBase.LoadState.Failed:
					HintService.Hint(Lang.Text("Instance.Resource.Update.Failed", new object[1] { loader.Error.Message }), HintType.Error);
					break;
				case ModBase.LoadState.Aborted:
					HintService.Hint(Lang.Text("Instance.Resource.Update.Aborted"));
					break;
				}
				ModBase.Log("[CompUpdate] 已从正在进行资源更新的文件夹列表移除：" + pathMods);
				updatingVersions.Remove(pathMods);
				ModBase.RunInNewThread(() =>
				{
					try
					{
						foreach (string key in fileCopyList.Keys)
						{
							if (File.Exists(key))
							{
								File.Delete(key);
							}
						}
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "清理资源更新缓存失败");
					}
				}, "Clean Comp Update Cache", ThreadPriority.BelowNormal);
			};
			ModBase.Log($"[CompUpdate] 开始更新 {modList.Count()} 个资源：{pathMods}");
			updatingVersions.Add(pathMods);
			loader.Start();
			ModLoader.LoaderTaskbarAdd(loader);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
			ReloadCompFileList(forceReload: true);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "初始化资源更新失败");
		}
	}

	private void BtnSelectDelete_Click(object sender, ModBase.RouteEventArgs e)
	{
		DeleteMods(ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedMods.Contains(m.RawPath)));
		ChangeAllSelected(value: false);
	}

	private void DeleteModsCore(IEnumerable<ModLocalComp.LocalCompFile> modList, bool isShiftPressed)
	{
		try
		{
			bool flag = true;
			modList = (from m in modList.SelectMany((ModLocalComp.LocalCompFile target) =>
				{
					string[] result = ((!target.IsFolder) ? ((target.State != ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? new string[2] { target.path, target.RawPath } : new string[2]
					{
						target.path,
						target.path + (File.Exists(target.path + ".old") ? ".old" : ".disabled")
					}) : new string[1] { target.path });
					return result;
				}).Distinct()
				where (!m.EndsWithF("\\__FOLDER__", ignoreCase: true)) ? File.Exists(m) : Directory.Exists(m.Replace("\\__FOLDER__", ""))
				select new ModLocalComp.LocalCompFile(m)).ToList();
			foreach (ModLocalComp.LocalCompFile ModEntity in modList)
			{
				try
				{
					if (ModEntity.IsFolder)
					{
						if (isShiftPressed)
						{
							Directory.Delete(ModEntity.ActualPath, recursive: true);
						}
						else
						{
							Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(ModEntity.ActualPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
						}
					}
					else if (isShiftPressed)
					{
						File.Delete(ModEntity.path);
					}
					else
					{
						Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(ModEntity.path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
					}
				}
				catch (OperationCanceledException ex)
				{
					ModBase.Log(ex, "删除资源被主动取消");
					ReloadCompFileList(forceReload: true);
					return;
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, "删除资源失败（" + ModEntity.path + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
					flag = false;
					continue;
				}
				selectedMods.Remove(ModEntity.RawPath);
				ModLocalComp.compResourceListLoader.output.Remove(ModEntity);
				searchResult?.Remove(ModEntity);
				modItems.Remove(ModEntity.RawPath);
				int num = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>().FirstOrDefault((MyLocalCompItem i) => i.Entry.Equals(ModEntity)));
				if (num >= 0)
				{
					PanList.Children.RemoveAt(num);
				}
			}
			RefreshBars();
			if (!flag)
			{
				HintService.Hint(Lang.Text("Instance.Resource.Delete.Failed"), HintType.Error);
				ReloadCompFileList(forceReload: true);
			}
			else if (PanList.Children.Count == 0)
			{
				ReloadCompFileList(forceReload: true);
			}
			else
			{
				RefreshBars();
			}
			if (!flag)
			{
				return;
			}
			if (isShiftPressed)
			{
				if (modList.Count() == 1)
				{
					HintService.Hint(Lang.Text("Instance.Resource.Delete.PermanentSingle", new object[1] { modList.Single().FileName }), HintType.Success);
				}
				else
				{
					HintService.Hint(Lang.Text("Instance.Resource.Delete.PermanentMultiple", new object[1] { modList.Count() }), HintType.Success);
				}
			}
			else if (modList.Count() == 1)
			{
				HintService.Hint(Lang.Text("Instance.Resource.Delete.RecycleSingle", new object[1] { modList.Single().FileName }), HintType.Success);
			}
			else
			{
				HintService.Hint(Lang.Text("Instance.Resource.Delete.RecycleMultiple", new object[1] { modList.Count() }), HintType.Success);
			}
		}
		catch (OperationCanceledException ex3)
		{
			ModBase.Log(ex3, "删除资源被主动取消");
			ReloadCompFileList(forceReload: true);
		}
		catch (Exception ex4)
		{
			ModBase.Log(ex4, "删除资源出现未知错误", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
			ReloadCompFileList(forceReload: true);
		}
		LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
	}

	private void BtnSelectCancel_Click(object sender, ModBase.RouteEventArgs e)
	{
		ChangeAllSelected(value: false);
	}

	private void BtnSelectFavorites_Click(object sender, ModBase.RouteEventArgs e)
	{
		ModComp.CompFavorites.ShowMenu((from i in ModLocalComp.compResourceListLoader.output
			where selectedMods.Contains(i.RawPath) && (object)i.Comp != null
			select i.Comp).ToList(), (UIElement)sender);
	}

	private void BtnSelectShare_Click(object sender, ModBase.RouteEventArgs e)
	{
		ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode((from i in ModLocalComp.compResourceListLoader.output
			where selectedMods.Contains(i.RawPath) && (object)i.Comp != null
			select i.Comp.Id).ToHashSet()));
		ChangeAllSelected(value: false);
	}

	public void Info_Click(object sender, EventArgs e)
	{
		try
		{
			ModLocalComp.LocalCompFile entry = ((MyLocalCompItem)((sender is MyIconButton myIconButton) ? myIconButton.Tag : sender)).Entry;
			bool flag = PageInstanceLeft.McInstance.Info.HasLabyMod && PageInstanceLeft.McInstance.Modable;
			if (entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
			{
				ModMain.MyMsgBox(Lang.Text("Instance.Resource.Item.Info.FailedMessage.WithDetail", new object[1] { entry.FileUnavailableReason.ToString() }), Lang.Text("Instance.Resource.Item.Info.FailedTitle"));
				return;
			}
			if ((object)entry.Comp != null)
			{
				ModMain.frmMain.PageChange(new FormMain.PageStackData
				{
					page = FormMain.PageType.CompDetail,
					additional = (entry.Comp, new List<string>(), PageInstanceLeft.McInstance.Info.VanillaName, PageInstanceLeft.McInstance.Info.HasForge ? ModComp.CompLoaderType.Forge : (PageInstanceLeft.McInstance.Info.HasNeoForge ? ModComp.CompLoaderType.NeoForge : ((PageInstanceLeft.McInstance.Info.HasFabric | flag) ? ModComp.CompLoaderType.Fabric : ModComp.CompLoaderType.Any)), currentCompType, null)
				});
				return;
			}
			if (entry.path.EndsWithF(".litematic", ignoreCase: true) || entry.path.EndsWithF(".schem", ignoreCase: true) || entry.path.EndsWithF(".schematic", ignoreCase: true) || entry.path.EndsWithF(".nbt", ignoreCase: true))
			{
				ShowSchematicInfoAsync(entry);
				return;
			}
			List<string> list = new List<string>();
			if (entry.IsFolder)
			{
				string actualPath = entry.ActualPath;
				if (Directory.Exists(actualPath))
				{
					int num = 0;
					try
					{
						num = currentCompType switch
						{
							ModComp.CompType.Schematic => (from f in new DirectoryInfo(actualPath).EnumerateFiles("*", System.IO.SearchOption.AllDirectories)
								where ModLocalComp.LocalCompFile.IsCompFile(f.FullName, ModComp.CompType.Schematic)
								select f).Count(), 
							ModComp.CompType.Mod => new DirectoryInfo(actualPath).EnumerateFiles("*.jar", System.IO.SearchOption.AllDirectories).Count(), 
							ModComp.CompType.ResourcePack => new DirectoryInfo(actualPath).EnumerateFiles("*.zip", System.IO.SearchOption.AllDirectories).Count(), 
							ModComp.CompType.Shader => new DirectoryInfo(actualPath).EnumerateFiles("*.zip", System.IO.SearchOption.AllDirectories).Count(), 
							_ => new DirectoryInfo(actualPath).EnumerateFiles("*", System.IO.SearchOption.AllDirectories).Count(), 
						};
					}
					catch (Exception)
					{
						num = 0;
					}
					switch (num)
					{
					case 0:
						list.Add(Lang.Text("Instance.Resource.Item.Info.EmptyFolder") + "\r\n");
						break;
					case 1:
						list.Add(Lang.Text("Instance.Resource.Item.Info.ContainsOne") + "\r\n");
						break;
					default:
						list.Add(Lang.Text("Instance.Resource.Item.Info.ContainsMany", new object[1] { num }) + "\r\n");
						break;
					}
				}
				else
				{
					list.Add(Lang.Text("Instance.Resource.Item.Info.FolderNotFound") + "\r\n");
				}
				list.Add(Lang.Text("Instance.Resource.Item.Info.Path", new object[1] { actualPath }));
			}
			else
			{
				if (entry.Description != null)
				{
					list.Add(entry.Description + "\r\n");
				}
				if (entry.Authors != null)
				{
					list.Add(Lang.Text("Instance.Resource.Item.Info.Author", new object[1] { entry.Authors }));
				}
				list.Add(Lang.Text("Instance.Resource.Item.Info.File", new object[2]
				{
					entry.FileName,
					ModBase.GetString(GetModFileInfo(entry.path).Length)
				}));
				if (entry.Version != null)
				{
					list.Add(Lang.Text("Instance.Resource.Item.Info.Version", new object[1] { entry.Version }));
				}
			}
			if (!entry.IsFolder)
			{
				List<string> list2 = new List<string>();
				if (entry.ModId != null)
				{
					list2.Add(Lang.Text("Instance.Resource.Item.Info.ModId", new object[1] { entry.ModId }));
				}
				if (entry.Dependencies.Any())
				{
					list2.Add(Lang.Text("Instance.Resource.Item.Info.Dependency"));
					foreach (KeyValuePair<string, string> dependency in entry.Dependencies)
					{
						list2.Add(" - " + ((dependency.Value == null) ? dependency.Key : Lang.Text("Instance.Resource.Item.Info.DependencyVersion", new object[2] { dependency.Key, dependency.Value })));
					}
				}
				if (list2.Any())
				{
					list.Add("");
					list.AddRange(list2);
				}
			}
			if (entry.IsFolder)
			{
				ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
				return;
			}
			string text = entry.Name.Replace(" ", "+");
			string text2 = text.Substring(0, 1);
			int num2 = 1;
			for (int num3 = text.Count() - 1; num2 <= num3; num2++)
			{
				bool flag2 = text[num2 - 1].ToString().ToLower().Equals(text[num2 - 1].ToString());
				bool flag3 = text[num2].ToString().ToLower().Equals(text[num2].ToString());
				if (flag2 && !flag3)
				{
					text2 += "+";
				}
				text2 += text[num2];
			}
			text2 = text2.Replace("++", "+").Replace("pti+Fine", "ptiFine");
			if (currentCompType == ModComp.CompType.Schematic || !Lang.IsChineseMainland)
			{
				if (entry.Url == null)
				{
					ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
				}
				else if (ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
				{
					ModBase.OpenWebsite(entry.Url);
				}
				return;
			}
			if (entry.Url == null)
			{
				if (ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.McMod"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
				{
					ModBase.OpenWebsite("https://www.mcmod.cn/s?key=" + text2 + "&site=all&filter=0");
				}
				return;
			}
			switch (ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.McMod"), Lang.Text("Instance.Resource.Item.Info.Return")))
			{
			case 1:
				ModBase.OpenWebsite(entry.Url);
				break;
			case 2:
				ModBase.OpenWebsite("https://www.mcmod.cn/s?key=" + text2 + "&site=all&filter=0");
				break;
			}
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "获取资源详情失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	public void Open_Click(MyIconButton sender, EventArgs e)
	{
		try
		{
			MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sender.Tag;
			ModBase.OpenExplorer(myLocalCompItem.Entry.IsFolder ? myLocalCompItem.Entry.ActualPath : myLocalCompItem.Entry.path);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "打开资源文件位置失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
		}
	}

	public void Delete_Click(MyIconButton sender, EventArgs e)
	{
		MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sender.Tag;
		DeleteMods(new ModLocalComp.LocalCompFile[1] { myLocalCompItem.Entry });
	}

	public void ED_Click(MyIconButton sender, EventArgs e)
	{
		MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sender.Tag;
		EDMods(new ModLocalComp.LocalCompFile[1] { myLocalCompItem.Entry }, myLocalCompItem.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled);
	}

	private void ShowSchematicInfoAsync(ModLocalComp.LocalCompFile modEntry)
	{
		HintService.Hint(Lang.Text("Instance.Resource.Item.Info.LoadingDetail"));
		ModBase.RunInNewThread(() =>
		{
			try
			{
				modEntry.LoadNbtDataIfNeeded();
				ModBase.RunInUi(() =>
				{
					try
					{
						List<string> list = new List<string>();
						if (modEntry.Description != null)
						{
							list.Add(modEntry.Description + "\r\n");
						}
						if (modEntry.Authors != null)
						{
							list.Add(Lang.Text("Instance.Resource.Item.Info.Author", new object[1] { modEntry.Authors }));
						}
						list.Add(Lang.Text("Instance.Resource.Item.Info.File", new object[2]
						{
							modEntry.FileName,
							ModBase.GetString(GetModFileInfo(modEntry.path).Length)
						}));
						if (modEntry.Version != null)
						{
							list.Add(Lang.Text("Instance.Resource.Item.Info.Version", new object[1] { modEntry.Version }));
						}
						if (modEntry.path.EndsWithF(".litematic", ignoreCase: true))
						{
							ShowLitematicDetails(list, modEntry);
						}
						else if (modEntry.path.EndsWithF(".schem", ignoreCase: true))
						{
							ShowSchemDetails(list, modEntry);
						}
						else if (modEntry.path.EndsWithF(".schematic", ignoreCase: true))
						{
							ShowSchematicDetails(list, modEntry);
						}
						else if (modEntry.path.EndsWithF(".nbt", ignoreCase: true))
						{
							ShowNbtDetails(list, modEntry);
						}
						ShowDebugInfo(list, modEntry);
						ShowSchematicDialog(list, modEntry);
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "显示原理图详情失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
					}
				});
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "加载原理图 NBT 数据失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Resource.Error.OperationFailed"));
			}
		});
	}

	private void ShowLitematicDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
	{
		contentLines.Add("");
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));
		if (modEntry.LitematicOriginalName != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.OriginalName") + modEntry.LitematicOriginalName);
		}
		if (modEntry.LitematicVersion.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.Version") + modEntry.LitematicVersion.Value);
		}
		if (modEntry.LitematicEnclosingSize != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingSize") + modEntry.LitematicEnclosingSize);
		}
		if (modEntry.LitematicTotalBlocks.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number<int>(modEntry.LitematicTotalBlocks.Value, "N0"));
		}
		if (modEntry.LitematicTotalVolume.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number<int>(modEntry.LitematicTotalVolume.Value, "N0"));
		}
		if (modEntry.LitematicRegionCount.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);
		}
		if (modEntry.LitematicTimeCreated.HasValue)
		{
			try
			{
				DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(modEntry.LitematicTimeCreated.Value).ToLocalTime().DateTime;
				contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.CreatedTime") + Lang.Date(dateTime, "G"));
			}
			catch
			{
				contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.CreatedTime") + modEntry.LitematicTimeCreated.Value);
			}
		}
		if (modEntry.LitematicTimeModified.HasValue)
		{
			try
			{
				DateTime dateTime2 = DateTimeOffset.FromUnixTimeMilliseconds(modEntry.LitematicTimeModified.Value).ToLocalTime().DateTime;
				contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.ModifiedTime") + Lang.Date(dateTime2, "G"));
			}
			catch
			{
				contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.ModifiedTime") + modEntry.LitematicTimeModified.Value);
			}
		}
	}

	private void ShowSchemDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
	{
		contentLines.Add("");
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));
		if (modEntry.SchemOriginalName != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.OriginalName") + modEntry.SchemOriginalName);
		}
		if (modEntry.StructureGameVersion != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.GameVersion") + modEntry.StructureGameVersion);
		}
		if (modEntry.SpongeVersion.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.SpongeVersion") + modEntry.SpongeVersion.Value);
		}
		if (modEntry.StructureDataVersion.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DataVersion") + modEntry.StructureDataVersion.Value);
		}
		if (modEntry.LitematicEnclosingSize != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingDimensions") + modEntry.LitematicEnclosingSize);
		}
		if (modEntry.LitematicTotalBlocks.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number<int>(modEntry.LitematicTotalBlocks.Value, "N0"));
		}
		if (modEntry.LitematicTotalVolume.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number<int>(modEntry.LitematicTotalVolume.Value, "N0"));
		}
		if (modEntry.LitematicRegionCount.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);
		}
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType", new object[1] { Lang.Text("Instance.Resource.Item.Schematic.FileType.Sponge") }));
	}

	private void ShowSchematicDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
	{
		contentLines.Add("");
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));
		if (modEntry.LitematicEnclosingSize != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.Size") + modEntry.LitematicEnclosingSize);
		}
		if (modEntry.LitematicTotalBlocks.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number<int>(modEntry.LitematicTotalBlocks.Value, "N0"));
		}
		if (modEntry.LitematicTotalVolume.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number<int>(modEntry.LitematicTotalVolume.Value, "N0"));
		}
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType", new object[1] { Lang.Text("Instance.Resource.Item.Schematic.FileType.Mcedit") }));
	}

	private void ShowNbtDetails(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
	{
		contentLines.Add("");
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DetailInfo"));
		if (modEntry.StructureAuthor != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Info.Author", new object[1] { modEntry.StructureAuthor }));
		}
		if (modEntry.StructureGameVersion != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.GameVersion") + modEntry.StructureGameVersion);
		}
		if (modEntry.StructureDataVersion.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.DataVersion") + modEntry.StructureDataVersion.Value);
		}
		if (modEntry.LitematicEnclosingSize != null)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.EnclosingDimensions") + modEntry.LitematicEnclosingSize);
		}
		if (modEntry.LitematicTotalBlocks.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalBlocks") + Lang.Number<int>(modEntry.LitematicTotalBlocks.Value, "N0"));
		}
		if (modEntry.LitematicTotalVolume.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.TotalVolume") + Lang.Number<int>(modEntry.LitematicTotalVolume.Value, "N0"));
		}
		if (modEntry.LitematicRegionCount.HasValue)
		{
			contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.RegionCount") + modEntry.LitematicRegionCount.Value);
		}
		contentLines.Add(Lang.Text("Instance.Resource.Item.Schematic.FileType", new object[1] { Lang.Text("Instance.Resource.Item.Schematic.FileType.Nbt") }));
	}

	private void ShowDebugInfo(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
	{
		List<string> list = new List<string>();
		if (modEntry.ModId != null)
		{
			list.Add(Lang.Text("Instance.Resource.Item.Info.ModId", new object[1] { modEntry.ModId }));
		}
		if (modEntry.Dependencies.Any())
		{
			list.Add(Lang.Text("Instance.Resource.Item.Info.Dependency"));
			foreach (KeyValuePair<string, string> dependency in modEntry.Dependencies)
			{
				list.Add(" - " + dependency.Key + ((dependency.Value == null) ? dependency.Key : Lang.Text("Instance.Resource.Item.Info.DependencyVersion", new object[2] { dependency.Key, dependency.Value })));
			}
		}
		if (list.Any())
		{
			contentLines.Add("");
			contentLines.AddRange(list);
		}
	}

	private void ShowSchematicDialog(List<string> contentLines, ModLocalComp.LocalCompFile modEntry)
	{
		if (modEntry.Url == null)
		{
			ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
		}
		else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), modEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
		{
			ModBase.OpenWebsite(modEntry.Url);
		}
	}

	public void SearchRun(object sender, EventArgs e)
	{
		CancellationTokenSource curToken = new CancellationTokenSource();
		CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref _cancelToken, curToken);
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			_ = 1;
			try
			{
				CancellationToken token = curToken.Token;
				await Task.Delay(350, token);
				if (!token.IsCancellationRequested)
				{
					if (IsSearching)
					{
						string searchText = SearchBox.Text;
						List<ModLocalComp.LocalCompFile> list = await Task.Run(() => GetSearchResult(searchText), token);
						searchResult = list;
					}
					if (!token.IsCancellationRequested)
					{
						RefreshUI();
					}
				}
			}
			catch (TaskCanceledException)
			{
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "搜索过程中发生异常");
			}
		}), Array.Empty<object>());
	}

	private List<ModLocalComp.LocalCompFile> GetSearchResult(string query)
	{
		List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>> list = new List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>>();
		foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output.AsReadOnly())
		{
			List<ModBase.SearchSource> list2 = new List<ModBase.SearchSource>();
			list2.Add(new ModBase.SearchSource(item.Name));
			list2.Add(new ModBase.SearchSource(item.FileName));
			if (item.Version != null)
			{
				list2.Add(new ModBase.SearchSource(item.Version, 0.2));
			}
			if (item.Description != null && !string.IsNullOrEmpty(item.Description))
			{
				list2.Add(new ModBase.SearchSource(item.Description, 0.4));
			}
			if ((object)item.Comp != null)
			{
				if ((item.Comp.RawName ?? "") != (item.Name ?? ""))
				{
					list2.Add(new ModBase.SearchSource(item.Comp.RawName));
				}
				if ((item.Comp.TranslatedName ?? "") != (item.Comp.RawName ?? ""))
				{
					list2.Add(new ModBase.SearchSource(item.Comp.TranslatedName));
				}
				if ((item.Comp.Description ?? "") != (item.Description ?? ""))
				{
					list2.Add(new ModBase.SearchSource(item.Comp.Description, 0.4));
				}
				list2.Add(new ModBase.SearchSource(string.Join("", item.Comp.Tags), 0.2));
			}
			list.Add(new ModBase.SearchEntry<ModLocalComp.LocalCompFile>
			{
				item = item,
				searchSource = list2
			});
		}
		return (from r in ModBase.Search(list, query, 25, 0.35)
			select r.item).ToList();
	}

}
