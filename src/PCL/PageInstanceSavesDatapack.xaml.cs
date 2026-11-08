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
using PCL.Core.UI;
using PCL.Core.UI.Theme;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageInstanceSavesDatapack : MyPageRight, IRefreshable, IComponentConnector
{
	public enum FilterType
	{
		All,
		Enabled,
		Disabled,
		CanUpdate,
		Unavailable
	}

	private enum SortMethod
	{
		FileName,
		CompName,
		CreateTime,
		DatapackFileSize
	}

	private readonly Dictionary<string, (DateTime CreationTime, long Length)> datapackFileInfoCache = new Dictionary<string, (DateTime, long)>();

	private readonly MyLocalCompItem.SwipeSelect currentSwipSelect;

	private bool isLoad;

	public Dictionary<string, MyLocalCompItem> datapackItems = new Dictionary<string, MyLocalCompItem>();

	private int bottomBarShownCount;

	public HashSet<string> selectedDatapacks = new HashSet<string>();

	private SortMethod currentSortMethod = SortMethod.CompName;

	private readonly object sortLock = new object();

	public static List<string> updatingVersions = new List<string>();

	private List<ModLocalComp.LocalCompFile> searchResult;

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
				default:
					BtnFilterError.Checked = true;
					break;
				}
				RefreshUI();
			}
		}
	}

	public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);

	private (DateTime CreationTime, long Length) GetDatapackFileInfo(string path)
	{
		if (datapackFileInfoCache.TryGetValue(path, out (DateTime, long) value))
		{
			return value;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			(DateTime, long) tuple = (fileInfo.CreationTime, fileInfo.Length);
			if (!datapackFileInfoCache.ContainsKey(path))
			{
				datapackFileInfoCache.Add(path, tuple);
			}
			return tuple;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取数据包信息失败: " + path);
			return (CreationTime: DateTime.MinValue, Length: 0L);
		}
	}

	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		datapackFileInfoCache.Clear();
	}

	public PageInstanceSavesDatapack()
	{
		currentSwipSelect = new MyLocalCompItem.SwipeSelect
		{
			TargetFrm = this
		};
		InitializeComponent();
		Unloaded += Page_Unloaded;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			PageOther_Loaded();
		};
		LoaderInit();
		PageExit += UnselectedAllWithAnimation;
		Load.Click += Load_Click;
		BtnManageOpen.Click += BtnManageOpen_Click;
		BtnHintOpen.Click += BtnManageOpen_Click;
		BtnManageSelectAll.Click += BtnManageSelectAll_Click;
		BtnManageInstall.Click += BtnManageInstall_Click;
		BtnHintInstall.Click += BtnManageInstall_Click;
		BtnManageDownload.Click += BtnManageDownload_Click;
		BtnHintDownload.Click += BtnManageDownload_Click;
		BtnManageInfoExport.Click += BtnManageInfoExport_Click;
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
		BtnSort.Click += BtnSortClick;
		BtnSelectEnable.Click += BtnSelectEnable_Click;
		BtnSelectDisable.Click += BtnSelectDisable_Click;
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
		compLocalLoaderData.frm = null;
		compLocalLoaderData.loaders = new ModComp.CompLoaderType[1] { ModComp.CompLoaderType.Minecraft }.ToList();
		compLocalLoaderData.compPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
		compLocalLoaderData.compType = ModComp.CompType.DataPack;
		return compLocalLoaderData;
	}

	public void PageOther_Loaded()
	{
		if (ModMain.frmMain.pageLast.page != FormMain.PageType.CompDetail)
		{
			PanBack.ScrollToHome();
		}
		ModAnimation.AniControlEnabled++;
		selectedDatapacks.Clear();
		ReloadDatapackFileList();
		ChangeAllSelected(value: false);
		ModAnimation.AniControlEnabled--;
		if (isLoad)
		{
			return;
		}
		isLoad = true;
		ModMain.frmMain.KeyDown += FrmMain_KeyDown;
		foreach (MyRadioButton child in PanFilter.Children)
		{
			child.LabText.Margin = new Thickness(-2.0, 0.0, 8.0, 0.0);
		}
	}

	public void ReloadDatapackFileList(bool forceReload = false)
	{
		if (LoaderRun(forceReload ? ModLoader.LoaderFolderRunType.ForceRun : ModLoader.LoaderFolderRunType.RunOnUpdated))
		{
			ModBase.Log("[System] 已刷新数据包列表");
			datapackFileInfoCache.Clear();
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
		Refresh();
	}

	void IRefreshable.Refresh()
	{
		RefreshSelf();
	}

	public void Refresh()
	{
		ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList(forceReload: true);
		ModBase.Log("[Datapack] 刷新数据包列表");
	}

	private void LoaderInit()
	{
		PageLoaderInit(Load, PanLoad, PanAllBack, null, ModLocalComp.compResourceListLoader, (ModLoader.LoaderBase _) =>
		{
			LoadUIFromLoaderOutput();
		}, () => ModComp.CompType.DataPack, autoRun: false);
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
		string folderPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
		return ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, folderPath, type, 0, "", waitForExit: false, GetRequireLoaderData());
	}

	private void LoadUIFromLoaderOutput()
	{
		try
		{
			if (ModLocalComp.compResourceListLoader.output.Any())
			{
				PanBack.Visibility = Visibility.Visible;
				PanEmpty.Visibility = Visibility.Collapsed;
				datapackItems.Clear();
				foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output.ToList())
				{
					datapackItems[item.RawPath] = BuildLocalCompItem(item);
				}
				ModBase.RunInUi(() =>
				{
					Filter = FilterType.All;
					SearchBox.Text = "";
					RefreshUI();
					SetSortMethod(SortMethod.CompName);
				});
			}
			else
			{
				TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Datapack.Empty.Title");
				TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Datapack.Empty.Description");
				PanEmpty.Visibility = Visibility.Visible;
				PanBack.Visibility = Visibility.Collapsed;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "加载数据包列表 UI 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
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
				Checked = selectedDatapacks.Contains(entry.RawPath)
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
		sender.Changed += (object ss, ModBase.RouteEventArgs e2) =>
		{
			CheckChanged((MyLocalCompItem)ss, e2);
		};
		sender.Click += (object ss, MouseButtonEventArgs e2) =>
		{
			MyLocalCompItem myLocalCompItem = (MyLocalCompItem)ss;
			myLocalCompItem.Checked = !myLocalCompItem.Checked;
		};
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
		myIconButton.Click += (object obj, EventArgs e2) =>
		{
			Open_Click((MyIconButton)obj, e2);
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
		myIconButton3.Click += (object obj, EventArgs e2) =>
		{
			Delete_Click((MyIconButton)obj, e2);
		};
		if (sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
		{
			MyIconButton myIconButton4 = new MyIconButton
			{
				LogoScale = 1.0,
				SvgIcon = "lucide/circle-minus",
				Tag = sender
			};
			myIconButton4.ToolTip = Lang.Text("Instance.Resource.Disable");
			ToolTipService.SetPlacement((DependencyObject)(object)myIconButton4, PlacementMode.Center);
			ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton4, 30.0);
			ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton4, 2.0);
			myIconButton4.Click += (object ss, EventArgs e2) =>
			{
				Disable_Click((MyIconButton)ss, e2);
			};
			sender.Buttons = new MyIconButton[4] { myIconButton2, myIconButton, myIconButton4, myIconButton3 };
		}
		else if (sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
		{
			MyIconButton myIconButton5 = new MyIconButton
			{
				LogoScale = 1.0,
				SvgIcon = "lucide/circle-check",
				Tag = sender
			};
			myIconButton5.ToolTip = Lang.Text("Instance.Resource.Enable");
			ToolTipService.SetPlacement((DependencyObject)(object)myIconButton5, PlacementMode.Center);
			ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton5, 30.0);
			ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton5, 2.0);
			myIconButton5.Click += (object ss, EventArgs e2) =>
			{
				Enable_Click((MyIconButton)ss, e2);
			};
			sender.Buttons = new MyIconButton[4] { myIconButton2, myIconButton, myIconButton5, myIconButton3 };
		}
		else
		{
			sender.Buttons = new MyIconButton[3] { myIconButton2, myIconButton, myIconButton3 };
		}
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
			source = datapackItems.Values.Select((MyLocalCompItem i) => i.Entry);
		}
		else
		{
			IEnumerable<ModLocalComp.LocalCompFile> enumerable = searchResult;
			source = enumerable;
		}
		List<ModLocalComp.LocalCompFile> showingDatapacks = source.Where((ModLocalComp.LocalCompFile m) => CanPassFilter(m)).ToList();
		if (showingDatapacks.Any())
		{
			Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> sortMethod = GetSortMethod(currentSortMethod);
			showingDatapacks.Sort((ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) => sortMethod(a, b));
		}
		ModAnimation.AniControlEnabled++;
		if (showingDatapacks.Any())
		{
			PanList.Visibility = Visibility.Visible;
			PanList.Children.Clear();
			foreach (ModLocalComp.LocalCompFile item in showingDatapacks)
			{
				if (datapackItems.ContainsKey(item.RawPath))
				{
					MyLocalCompItem myLocalCompItem = datapackItems[item.RawPath];
					if (myLocalCompItem.Parent != null)
					{
						((Panel)(object)myLocalCompItem.Parent).Children.Remove(myLocalCompItem);
					}
					ModStyle.MinecraftFormatter.SetColorfulTextLab(myLocalCompItem.LabTitle.Text, myLocalCompItem.LabTitle, ThemeService.IsDarkMode);
					ModStyle.MinecraftFormatter.SetColorfulTextLab(myLocalCompItem.LabInfo.Text, myLocalCompItem.LabInfo, ThemeService.IsDarkMode);
					myLocalCompItem.Checked = selectedDatapacks.Contains(item.RawPath);
					PanList.Children.Add(myLocalCompItem);
				}
			}
		}
		else
		{
			PanList.Visibility = Visibility.Collapsed;
		}
		ModAnimation.AniControlEnabled--;
		selectedDatapacks = new HashSet<string>(selectedDatapacks.Where((string m) => showingDatapacks.Any((ModLocalComp.LocalCompFile s) => (s.RawPath ?? "") == (m ?? ""))));
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
				source = datapackItems.Values.Select((MyLocalCompItem i) => i.Entry);
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
			int newCount = selectedDatapacks.Count;
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
						if (selectedDatapacks.Contains(item.RawPath))
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
				BtnSelectFavorites.IsEnabled = canFavoriteAndShare;
				BtnSelectShare.IsEnabled = canFavoriteAndShare;
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
						}, "Datapack Sidebar");
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
					}, "Datapack Sidebar");
				}
			}
			else
			{
				ModAnimation.AniStop("Datapack Sidebar");
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

	private void BtnManageOpen_Click(object sender, EventArgs e)
	{
		try
		{
			string text = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
			Directory.CreateDirectory(text);
			ModBase.OpenExplorer(text);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "打开 datapacks 文件夹失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
		}
	}

	private void BtnManageSelectAll_Click(object sender, MouseButtonEventArgs e)
	{
		ChangeAllSelected(selectedDatapacks.Count < PanList.Children.Count);
	}

	private void BtnManageInstall_Click(object sender, MouseButtonEventArgs e)
	{
		string[] array = SystemDialogs.SelectFiles(Lang.Text("Instance.Saves.Datapack.Install.FileDialog.Filter"), Lang.Text("Instance.Saves.Datapack.Install.FileDialog.Title"), (string)null, true);
		if (array != null && array.Length != 0)
		{
			InstallDatapackFiles(array);
			Refresh();
		}
	}

	public static void InstallDatapackFiles(IEnumerable<string> filePathList)
	{
		if (!filePathList.Any())
		{
			return;
		}
		string text = filePathList.First().AfterLast(".").ToLower();
		if (text != "zip")
		{
			HintService.Hint(Lang.Text("Instance.Resource.Install.UnsupportedFormat", new object[3]
			{
				text,
				Lang.Text("Download.Comp.Type.DataPack"),
				"zip"
			}), HintType.Error);
			return;
		}
		if (filePathList.First().Contains(":\\$RECYCLE.BIN\\"))
		{
			HintService.Hint(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintType.Error);
			return;
		}
		ModBase.Log("[System] 文件为 " + text + " 格式，尝试作为数据包安装");
		if ((!(ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup) || ModMain.frmMain.PageCurrentSub != FormMain.PageSubType.DownloadInstall) && ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Install.Message"), Lang.Text("Instance.Saves.Datapack.Install.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 1)
		{
			return;
		}
		try
		{
			string text2 = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
			Directory.CreateDirectory(text2);
			foreach (string filePath in filePathList)
			{
				string fileNameFromPath = ModBase.GetFileNameFromPath(filePath);
				string text3 = text2 + fileNameFromPath;
				if (!File.Exists(text3) || ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.OverwriteConfirm.Message", new object[1] { fileNameFromPath }), Lang.Text("Instance.Resource.Install.OverwriteConfirm.Title"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) == 1)
				{
					ModBase.CopyFile(filePath, text3);
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
					Lang.Text("Download.Comp.Type.DataPack")
				}), HintType.Success);
			}
			if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup && ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.DownloadInstall && ModMain.frmInstanceSavesDatapack != null)
			{
				ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList(forceReload: true);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "复制数据包文件失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
		}
	}

	private void BtnManageDownload_Click(object sender, MouseButtonEventArgs e)
	{
		string text = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
		Directory.CreateDirectory(text);
		PageDownloadCompDetail.cachedFolder[ModComp.CompType.DataPack] = text;
		ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadDataPack);
		PageComp.targetVersion = PageInstanceLeft.McInstance;
	}

	private void BtnManageInfoExport_Click(object sender, MouseButtonEventArgs e)
	{
		switch (ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Export.Mode.Message"), Lang.Text("Instance.Resource.Export.Mode.Title"), Lang.Text("Instance.Resource.Export.Mode.Txt"), Lang.Text("Instance.Resource.Export.Mode.Csv"), Lang.Text("Common.Action.Cancel")))
		{
		case 1:
		{
			List<string> list2 = new List<string>();
			foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output)
			{
				list2.Add(item.FileName);
			}
			ExportText(list2.Join("\r\n"), ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) + "的数据包信息.txt");
			break;
		}
		case 2:
		{
			List<string> list = new List<string>();
			list.Add("文件名,数据包名称,数据包版本,此版本更新时间,工程 ID,文件大小（字节）,文件路径");
			foreach (ModLocalComp.LocalCompFile item2 in ModLocalComp.compResourceListLoader.output)
			{
				list.Add($"{item2.FileName},{item2.Comp?.TranslatedName},{item2.Version},{item2.compFile?.ReleaseDate},{item2.Comp?.Id},{GetDatapackFileInfo(item2.path).Length},{item2.path}");
			}
			ExportText(list.Join("\r\n"), ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) + "的数据包信息.csv");
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
				ModBase.Log(ex, "导出数据包信息失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
			}
		}
	}

	public void CheckChanged(MyLocalCompItem sender, ModBase.RouteEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0)
		{
			string rawPath = sender.Entry.RawPath;
			if (sender.Checked)
			{
				selectedDatapacks.Add(rawPath);
			}
			else
			{
				selectedDatapacks.Remove(rawPath);
			}
			RefreshBars();
		}
	}

	private void ChangeAllSelected(bool value)
	{
		ModAnimation.AniControlEnabled++;
		selectedDatapacks.Clear();
		foreach (MyLocalCompItem value2 in datapackItems.Values)
		{
			if (value2.Checked = value && PanList.Children.Contains(value2))
			{
				selectedDatapacks.Add(value2.Entry.RawPath);
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

	private bool CanPassFilter(ModLocalComp.LocalCompFile checkingDatapack)
	{
		return Filter switch
		{
			FilterType.All => true, 
			FilterType.Enabled => checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine, 
			FilterType.Disabled => checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled, 
			FilterType.CanUpdate => checkingDatapack.CanUpdate, 
			FilterType.Unavailable => checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable, 
			_ => false, 
		};
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
			SortMethod.CreateTime => Lang.Text("Instance.Resource.Sort.AddTime"), 
			SortMethod.DatapackFileSize => Lang.Text("Instance.Resource.Sort.FileSize"), 
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
					List<MyLocalCompItem> second = list.Where((MyLocalCompItem i) => i.Entry == null).ToList();
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
				ModBase.Log(ex, "执行排序时出错", ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
			}
		}
	}

	private Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> GetSortMethod(SortMethod method)
	{
		return method switch
		{
			SortMethod.FileName => (Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int>)((ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase)), 
			SortMethod.CompName => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase), 
			SortMethod.CreateTime => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				DateTime item = GetDatapackFileInfo(a.path).CreationTime;
				DateTime item2 = GetDatapackFileInfo(b.path).CreationTime;
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
			SortMethod.DatapackFileSize => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) =>
			{
				long item = GetDatapackFileInfo(a.path).Length;
				long item2 = GetDatapackFileInfo(b.path).Length;
				if (item == 0L && item2 == 0L)
				{
					return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
				}
				if (item == 0L)
				{
					return 1;
				}
				return (item2 == 0L) ? (-1) : item2.CompareTo(item);
			}, 
			_ => (ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase), 
		};
	}

	private void BtnSelectEnable_Click(object sender, ModBase.RouteEventArgs e)
	{
		ToggleDatapacks(ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedDatapacks.Contains(m.RawPath)).ToList(), isEnable: true);
		ChangeAllSelected(value: false);
	}

	private void BtnSelectDisable_Click(object sender, ModBase.RouteEventArgs e)
	{
		ToggleDatapacks(ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedDatapacks.Contains(m.RawPath)).ToList(), isEnable: false);
		ChangeAllSelected(value: false);
	}

	private void ToggleDatapacks(IEnumerable<ModLocalComp.LocalCompFile> datapackList, bool isEnable)
	{
		bool flag = true;
		foreach (ModLocalComp.LocalCompFile datapack in datapackList)
		{
			ModLocalComp.LocalCompFile datapackEntity = datapack;
			string text = null;
			if (datapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && !isEnable)
			{
				text = datapackEntity.path + ".disabled";
			}
			else
			{
				if (!((datapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled) & isEnable))
				{
					continue;
				}
				text = datapackEntity.RawPath;
			}
			try
			{
				if (File.Exists(text))
				{
					ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Replace.FileNameConflict", new object[1] { ModBase.GetFileNameFromPath(text) }));
					continue;
				}
				Microsoft.VisualBasic.FileSystem.Rename(datapackEntity.path, text);
			}
			catch (FileNotFoundException ex)
			{
				ModBase.Log(ex, "未找到需要重命名的数据包（" + (datapackEntity.path ?? "null") + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
				ReloadDatapackFileList(forceReload: true);
				return;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "重命名数据包失败（" + (datapackEntity.path ?? "null") + "）");
				flag = false;
			}
			ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(text);
			localCompFile.FromJson(datapackEntity.ToJson());
			if (ModLocalComp.compResourceListLoader.output.Contains(datapackEntity))
			{
				int index = ModLocalComp.compResourceListLoader.output.IndexOf(datapackEntity);
				ModLocalComp.compResourceListLoader.output.RemoveAt(index);
				ModLocalComp.compResourceListLoader.output.Insert(index, localCompFile);
			}
			if (searchResult != null && searchResult.Contains(datapackEntity))
			{
				int index2 = searchResult.IndexOf(datapackEntity);
				searchResult.Remove(datapackEntity);
				searchResult.Insert(index2, localCompFile);
			}
			try
			{
				MyLocalCompItem myLocalCompItem = BuildLocalCompItem(localCompFile);
				datapackItems[datapackEntity.RawPath] = myLocalCompItem;
				int num = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>().FirstOrDefault((MyLocalCompItem i) => i.Entry == datapackEntity));
				if (num != -1)
				{
					PanList.Children.RemoveAt(num);
					PanList.Children.Insert(num, myLocalCompItem);
				}
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "更新 UI 列表项失败：" + datapackEntity.FileName, ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
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
			HintService.Hint(Lang.Text("Instance.Saves.Datapack.ToggleWarning"), HintType.Error);
			ReloadDatapackFileList(forceReload: true);
		}
		LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
	}

	private void BtnSelectUpdate_Click(object sender, ModBase.RouteEventArgs e)
	{
		List<ModLocalComp.LocalCompFile> list = ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedDatapacks.Contains(m.RawPath) && m.CanUpdate).ToList();
		if (list.Any())
		{
			UpdateResource(list);
			ChangeAllSelected(value: false);
		}
	}

	private static bool TryGetSafeDatapackUpdateFileName(ModComp.CompFile file, out string fileName)
	{
		fileName = file.FileName?.Trim() ?? "";
		if (string.IsNullOrEmpty(fileName))
		{
			return false;
		}
		if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (fileName.IndexOfAny(new char[3] { '\\', '/', ':' }) >= 0)
		{
			return false;
		}
		if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			return false;
		}
		if (fileName == Path.GetFileName(fileName) && fileName != ".")
		{
			return fileName != "..";
		}
		return false;
	}

	private static bool TryBuildDatapackUpdatePath(string rootPath, string fileName, out string fullPath)
	{
		string text = Path.GetFullPath(rootPath);
		if (!text.EndsWith(Path.DirectorySeparatorChar.ToString()) && !text.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
		{
			text += Path.DirectorySeparatorChar;
		}
		fullPath = Path.GetFullPath(Path.Combine(text, fileName));
		return fullPath.StartsWith(text, StringComparison.OrdinalIgnoreCase);
	}

	public void UpdateResource(IEnumerable<ModLocalComp.LocalCompFile> datapackList)
	{
		if (!States.Hint.FunctionDatapackUpdate || datapackList.Count() >= 15)
		{
			if (ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Update.Warning.Message"), Lang.Text("Instance.Saves.Datapack.Update.Warning.Title"), Lang.Text("Instance.Saves.Datapack.Update.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
			{
				return;
			}
			States.Hint.FunctionDatapackUpdate = true;
		}
		try
		{
			datapackList = datapackList.ToList();
			List<DownloadFile> list = new List<DownloadFile>();
			Dictionary<string, string> fileCopyList = new Dictionary<string, string>();
			List<ModLocalComp.LocalCompFile> updateEntryList = new List<ModLocalComp.LocalCompFile>();
			string rootPath = Path.Combine(ModBase.pathTemp, "DownloadedComp");
			string rootPath2 = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
			int num = 0;
			foreach (ModLocalComp.LocalCompFile datapack in datapackList)
			{
				ModComp.CompFile updateFile = datapack.UpdateFile;
				if (updateFile.Available)
				{
					if (!TryGetSafeDatapackUpdateFileName(updateFile, out string fileName) || !TryBuildDatapackUpdatePath(rootPath, fileName, out string fullPath) || !TryBuildDatapackUpdatePath(rootPath2, fileName, out string fullPath2))
					{
						num++;
						ModBase.Log("[DatapackUpdate] 已跳过不安全的数据包更新文件名：" + updateFile.FileName, ModBase.LogLevel.Debug);
					}
					else
					{
						list.Add(updateFile.ToNetFile(fullPath, ModComp.DownloadReason.Update, updateFile.RawGameVersions.FirstOrDefault()));
						fileCopyList[fullPath] = fullPath2;
						updateEntryList.Add(datapack);
					}
				}
			}
			if (num > 0)
			{
				HintService.Hint(Lang.Text("Instance.Saves.Datapack.Update.UnsafeFilesSkipped", new object[1] { num }), HintType.Error);
			}
			if (!list.Any())
			{
				return;
			}
			List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
			List<string> finishedFileNames = new List<string>();
			list2.Add(new LoaderDownload(Lang.Text("Instance.Saves.Datapack.Update.Task.DownloadFiles"), list)
			{
				ProgressWeight = (double)updateEntryList.Count * 1.5
			});
			list2.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Saves.Datapack.Update.Task.ReplaceFiles"), (ModLoader.LoaderTask<int, int> _) =>
			{
				try
				{
					foreach (ModLocalComp.LocalCompFile item in updateEntryList)
					{
						if (File.Exists(item.path))
						{
							Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(item.path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
						}
						else
						{
							ModBase.Log("[DatapackUpdate] 未找到更新前的数据包文件，跳过对它的删除：" + item.path, ModBase.LogLevel.Debug);
						}
					}
					foreach (KeyValuePair<string, string> item2 in fileCopyList)
					{
						if (File.Exists(item2.Value))
						{
							Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(item2.Value, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
							ModBase.Log("[Datapack] 更新后的数据包文件已存在，将会把它放入回收站：" + item2.Value, ModBase.LogLevel.Debug);
						}
						if (Directory.Exists(ModBase.GetPathFromFullPath(item2.Value)))
						{
							File.Move(item2.Key, item2.Value);
							finishedFileNames.Add(ModBase.GetFileNameFromPath(item2.Value));
						}
						else
						{
							ModBase.Log("[Datapack] 更新后的目标文件夹已被删除：" + item2.Value, ModBase.LogLevel.Debug);
						}
					}
				}
				catch (OperationCanceledException ex2)
				{
					ModBase.Log(ex2, "替换旧版数据包文件时被主动取消");
				}
			}));
			ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>> loader = new ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>>(Lang.Text("Instance.Saves.Datapack.Update.Task.Title", new object[1] { ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) }), list2);
			string pathDatapacks = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
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
						ModBase.Log("[DatapackUpdate] 没有数据包被成功更新");
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
				ModBase.Log("[DatapackUpdate] 已从正在进行数据包更新的文件夹列表移除：" + pathDatapacks);
				updatingVersions.Remove(pathDatapacks);
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
						ModBase.Log(ex2, "清理数据包更新缓存失败");
					}
				}, "Clean Datapack Update Cache", ThreadPriority.BelowNormal);
			};
			ModBase.Log($"[DatapackUpdate] 开始更新 {datapackList.Count()} 个数据包：{pathDatapacks}");
			updatingVersions.Add(pathDatapacks);
			loader.Start();
			ModLoader.LoaderTaskbarAdd(loader);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
			ReloadDatapackFileList(forceReload: true);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "初始化数据包更新失败");
		}
	}

	private void BtnSelectDelete_Click(object sender, ModBase.RouteEventArgs e)
	{
		DeleteDatapacks(ModLocalComp.compResourceListLoader.output.Where((ModLocalComp.LocalCompFile m) => selectedDatapacks.Contains(m.RawPath)));
		ChangeAllSelected(value: false);
	}

	private void DeleteDatapacks(IEnumerable<ModLocalComp.LocalCompFile> datapackList)
	{
		try
		{
			bool flag = true;
			bool flag2 = Keyboard.IsKeyDown((Key)116) || Keyboard.IsKeyDown((Key)117);
			datapackList = (from m in datapackList.SelectMany((ModLocalComp.LocalCompFile target) => (target.State != ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? new string[2] { target.path, target.RawPath } : new string[2]
				{
					target.path,
					target.path + ".disabled"
				}).Distinct()
				where File.Exists(m)
				select new ModLocalComp.LocalCompFile(m)).ToList();
			foreach (ModLocalComp.LocalCompFile DatapackEntity in datapackList)
			{
				try
				{
					if (flag2)
					{
						File.Delete(DatapackEntity.path);
					}
					else
					{
						Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(DatapackEntity.path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
					}
				}
				catch (OperationCanceledException ex)
				{
					ModBase.Log(ex, "删除数据包被主动取消");
					ReloadDatapackFileList(forceReload: true);
					return;
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, "删除数据包失败（" + DatapackEntity.path + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
					flag = false;
				}
				selectedDatapacks.Remove(DatapackEntity.RawPath);
				ModLocalComp.compResourceListLoader.output.Remove(DatapackEntity);
				searchResult?.Remove(DatapackEntity);
				datapackItems.Remove(DatapackEntity.RawPath);
				int num = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>().FirstOrDefault((MyLocalCompItem i) => i.Entry.Equals(DatapackEntity)));
				if (num >= 0)
				{
					PanList.Children.RemoveAt(num);
				}
			}
			RefreshBars();
			if (!flag)
			{
				HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.FileOccupied"), HintType.Error);
				ReloadDatapackFileList(forceReload: true);
			}
			else if (PanList.Children.Count == 0)
			{
				ReloadDatapackFileList(forceReload: true);
			}
			else
			{
				RefreshBars();
			}
			if (!flag)
			{
				return;
			}
			if (flag2)
			{
				if (datapackList.Count() == 1)
				{
					HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.PermanentSingle", new object[1] { datapackList.Single().FileName }), HintType.Success);
				}
				else
				{
					HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.PermanentMultiple", new object[1] { datapackList.Count() }), HintType.Success);
				}
			}
			else if (datapackList.Count() == 1)
			{
				HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.RecycleSingle", new object[1] { datapackList.Single().FileName }), HintType.Success);
			}
			else
			{
				HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.RecycleMultiple", new object[1] { datapackList.Count() }), HintType.Success);
			}
		}
		catch (OperationCanceledException ex3)
		{
			ModBase.Log(ex3, "删除数据包被主动取消");
			ReloadDatapackFileList(forceReload: true);
		}
		catch (Exception ex4)
		{
			ModBase.Log(ex4, "删除数据包出现未知错误", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
			ReloadDatapackFileList(forceReload: true);
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
			where selectedDatapacks.Contains(i.RawPath) && (object)i.Comp != null
			select i.Comp).ToList(), (UIElement)sender);
	}

	private void BtnSelectShare_Click(object sender, ModBase.RouteEventArgs e)
	{
		ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode((from i in ModLocalComp.compResourceListLoader.output
			where selectedDatapacks.Contains(i.RawPath) && (object)i.Comp != null
			select i.Comp.Id).ToHashSet()));
		ChangeAllSelected(value: false);
	}

	public void Info_Click(object sender, EventArgs e)
	{
		try
		{
			ModLocalComp.LocalCompFile entry = ((MyLocalCompItem)((sender is MyIconButton myIconButton) ? myIconButton.Tag : sender)).Entry;
			if (entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
			{
				ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Info.ReadFailed.WithDetail", new object[1] { entry.FileUnavailableReason.ToString() }), Lang.Text("Instance.Saves.Datapack.Info.ReadFailedTitle"));
				return;
			}
			if ((object)entry.Comp != null)
			{
				ModMain.frmMain.PageChange(new FormMain.PageStackData
				{
					page = FormMain.PageType.CompDetail,
					additional = (entry.Comp, new List<string>(), PageInstanceLeft.McInstance.Info.VanillaName, ModComp.CompLoaderType.Minecraft, ModComp.CompType.DataPack, null)
				});
				return;
			}
			List<string> list = new List<string>();
			if (entry.Description != null)
			{
				list.Add(entry.Description + "\r\n");
			}
			if (entry.Authors != null)
			{
				list.Add(Lang.Text("Instance.Saves.Datapack.Info.Author") + entry.Authors);
			}
			list.Add(Lang.Text("Instance.Saves.Datapack.Info.File") + entry.FileName + "（" + ModBase.GetString(GetDatapackFileInfo(entry.path).Length) + "）");
			if (entry.Version != null)
			{
				list.Add(Lang.Text("Instance.Saves.Datapack.Info.Version") + entry.Version);
			}
			List<string> list2 = new List<string>();
			if (entry.ModId != null)
			{
				list2.Add(Lang.Text("Instance.Saves.Datapack.Info.DatapackId") + entry.ModId);
			}
			if (list2.Any())
			{
				list.Add("");
				list.AddRange(list2);
			}
			if (entry.Url == null)
			{
				ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
			}
			else if (ModMain.MyMsgBox(list.Join("\r\n"), entry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
			{
				ModBase.OpenWebsite(entry.Url);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取数据包详情失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
		}
	}

	public void Open_Click(MyIconButton sender, EventArgs e)
	{
		try
		{
			ModBase.OpenExplorer(((MyLocalCompItem)sender.Tag).Entry.path);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "打开数据包文件位置失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
		}
	}

	public void Delete_Click(MyIconButton sender, EventArgs e)
	{
		MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sender.Tag;
		DeleteDatapacks(new ModLocalComp.LocalCompFile[1] { myLocalCompItem.Entry });
	}

	public void Enable_Click(MyIconButton sender, EventArgs e)
	{
		MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sender.Tag;
		ToggleDatapacks(new ModLocalComp.LocalCompFile[1] { myLocalCompItem.Entry }, isEnable: true);
	}

	public void Disable_Click(MyIconButton sender, EventArgs e)
	{
		MyLocalCompItem myLocalCompItem = (MyLocalCompItem)sender.Tag;
		ToggleDatapacks(new ModLocalComp.LocalCompFile[1] { myLocalCompItem.Entry }, isEnable: false);
	}

	public void SearchRun(object sender, EventArgs e)
	{
		try
		{
			if (IsSearching)
			{
				List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>> list = new List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>>();
				foreach (ModLocalComp.LocalCompFile item in ModLocalComp.compResourceListLoader.output)
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
				searchResult = (from r in ModBase.Search(list, SearchBox.Text, 25, 0.35)
					select r.item).ToList();
			}
			RefreshUI();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "搜索过程中发生异常");
		}
	}

}
