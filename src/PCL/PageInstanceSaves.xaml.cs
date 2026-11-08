using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceSaves : MyPageRight, IRefreshable, IComponentConnector
{
	private enum SortMethod
	{
		FileName,
		CreateTime,
		ModifyTime
	}

	private readonly DispatcherTimer fileSystemRefreshTimer;

	private readonly DispatcherTimer searchTimer;

	private FileSystemWatcher fileSystemWatcher;

	private bool isLoad;

	private object quickPlayFeature = false;

	private List<string> saveFolders = new List<string>();

	private string worldPath;

	private SortMethod _currentSortMethod;

	private List<string> _searchResult;

	public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);

	public PageInstanceSaves()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected Obj, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected Obj, but got Unknown
		InitializeComponent();
		fileSystemRefreshTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(100.0)
		};
		searchTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(100.0)
		};
		Loaded += PageSetupLaunch_Loaded;
		Unloaded += Page_Unloaded;
		fileSystemRefreshTimer.Tick += FileSystemRefreshTimer_Tick;
		searchTimer.Tick += SearchTimer_Tick;
		SearchBox.TextChanged += SearchRun;
	}

	void IRefreshable.Refresh()
	{
		RefreshSelf();
	}

	private void RefreshSelf()
	{
		Refresh();
		CheckQuickPlay();
	}

	public static void Refresh()
	{
		if (ModMain.frmInstanceSaves != null)
		{
			ModMain.frmInstanceSaves.Reload();
		}
		ModMain.frmInstanceLeft.ItemWorld.Checked = true;
		HintService.Hint(Lang.Text("Instance.Saves.Status.Refreshing"), HintType.Info, log: false);
	}

	private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		worldPath = PageInstanceLeft.McInstance.PathIndie + "saves\\";
		if (!Directory.Exists(worldPath))
		{
			Directory.CreateDirectory(worldPath);
		}
		Reload();
		if (!isLoad)
		{
			isLoad = true;
			CheckQuickPlay();
			SetupFileSystemWatcher();
			BtnSort.Click += BtnSortClick;
			SetSortMethod(_currentSortMethod);
		}
	}

	private string GetFolderNameFromPath(string fullPath)
	{
		if (!string.IsNullOrEmpty(fullPath))
		{
			if (!fullPath.EndsWith("\\"))
			{
				return new DirectoryInfo(fullPath).Name;
			}
			return new DirectoryInfo(fullPath).Parent?.Name;
		}
		return "";
	}

	private string GetFileNameFromPath(string fullPath)
	{
		return Path.GetFileName(fullPath);
	}

	private void SetupFileSystemWatcher()
	{
		if (fileSystemWatcher != null)
		{
			fileSystemWatcher.Dispose();
		}
		if (!Directory.Exists(worldPath))
		{
			Directory.CreateDirectory(worldPath);
		}
		fileSystemWatcher = new FileSystemWatcher();
		fileSystemWatcher.Path = worldPath;
		fileSystemWatcher.IncludeSubdirectories = false;
		fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
		fileSystemWatcher.Created += OnFileSystemChanged;
		fileSystemWatcher.Deleted += OnFileSystemChanged;
		fileSystemWatcher.Renamed += OnFileSystemChanged;
		fileSystemWatcher.EnableRaisingEvents = true;
	}

	private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
	{
		fileSystemRefreshTimer.Stop();
		fileSystemRefreshTimer.Start();
	}

	private void FileSystemRefreshTimer_Tick(object sender, EventArgs e)
	{
		fileSystemRefreshTimer.Stop();
		ModBase.RunInUi(() =>
		{
			Reload();
		}, forceWaitUntilLoaded: true);
	}

	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		if (fileSystemWatcher != null)
		{
			fileSystemWatcher.Created -= OnFileSystemChanged;
			fileSystemWatcher.Deleted -= OnFileSystemChanged;
			fileSystemWatcher.Renamed -= OnFileSystemChanged;
			fileSystemWatcher.Dispose();
			fileSystemWatcher = null;
		}
		fileSystemRefreshTimer.Stop();
		searchTimer.Stop();
	}

	public void Reload()
	{
		ModAnimation.AniControlEnabled++;
		PanBack.ScrollToHome();
		LoadFileList();
		ModAnimation.AniControlEnabled--;
	}

	private void RefreshUI()
	{
		try
		{
			if (IsSearching)
			{
				int num = ((_searchResult != null) ? _searchResult.Count : 0);
				PanListBack.Title = Lang.Text("Instance.Saves.SearchResultTitle", new object[1] { num.ToString() });
			}
			else
			{
				PanListBack.Title = Lang.Text("Instance.Saves.SaveListTitle", new object[1] { saveFolders.Count.ToString() });
			}
			if (saveFolders.Count == 0)
			{
				PanNoWorld.Visibility = Visibility.Visible;
				PanContent.Visibility = Visibility.Collapsed;
				PanNoWorld.UpdateLayout();
				return;
			}
			PanNoWorld.Visibility = Visibility.Collapsed;
			PanContent.Visibility = Visibility.Visible;
			PanContent.UpdateLayout();
			List<string> list = (IsSearching ? _searchResult : saveFolders).ToList();
			if (list.Any())
			{
				Func<string, string, int> sortMethod = GetSortMethod(_currentSortMethod);
				list.Sort((string a, string b) => sortMethod(a, b));
			}
			ModAnimation.AniControlEnabled++;
			PanList.Children.Clear();
			foreach (string item in list)
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string text = Path.Combine(item, "icon.png");
				string tmpCurFolder = item;
				if (File.Exists(text))
				{
					string text2 = PageInstanceLeft.McInstance.PathInstance + "PCL\\ImgCache\\" + ModBase.GetStringMD5(text) + ".png";
					ModBase.CopyFile(text, text2);
					text = text2;
				}
				else
				{
					text = ModBase.pathImage + "Icons/NoIcon.png";
				}
				MyListItem worldItem = new MyListItem
				{
					Logo = text,
					Title = GetFolderNameFromPath(item),
					Info = Lang.Text("Instance.Saves.CreationTime", new object[2]
					{
						Lang.Date(Directory.GetCreationTime(item), "d"),
						Lang.Date(Directory.GetLastWriteTime(item), "d")
					}),
					Type = MyListItem.CheckType.Clickable
				};
				worldItem.Click += (object _, MouseButtonEventArgs _) =>
				{
					ModMain.frmMain.PageChange(new FormMain.PageStackData
					{
						page = FormMain.PageType.VersionSaves,
						additional = (null, null, null, ModComp.CompLoaderType.Any, ModComp.CompType.Any, tmpCurFolder)
					});
				};
				MyIconButton myIconButton = new MyIconButton
				{
					SvgIcon = "lucide/folder-open",
					ToolTip = Lang.Text("Common.Action.Open")
				};
				myIconButton.Click += (object _, EventArgs _) =>
				{
					ModBase.OpenExplorer(tmpCurFolder);
				};
				MyIconButton myIconButton2 = new MyIconButton
				{
					SvgIcon = "lucide/trash-2",
					ToolTip = Lang.Text("Common.Action.Delete")
				};
				myIconButton2.Click += (object _, EventArgs _) =>
				{
					worldItem.IsEnabled = false;
					worldItem.Info = Lang.Text("Instance.Saves.Deleting");
					ModBase.RunInNewThread(() =>
					{
						try
						{
							FileSystem.DeleteDirectory(tmpCurFolder, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
							HintService.Hint(Lang.Text("Instance.Saves.DeletedToRecycleBin"));
							ModBase.RunInUiWait(() =>
							{
								RemoveItem(worldItem);
							});
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, Lang.Text("Instance.Saves.DeleteFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.DeleteFailed"));
							ModBase.RunInUiWait(() =>
							{
								Reload();
							});
						}
					});
				};
				MyIconButton myIconButton3 = new MyIconButton
				{
					SvgIcon = "lucide/copy",
					ToolTip = Lang.Text("Common.Action.Copy")
				};
				myIconButton3.Click += (object _, EventArgs _) =>
				{
					try
					{
						if (Directory.Exists(tmpCurFolder))
						{
							Clipboard.SetFileDropList(new StringCollection { tmpCurFolder });
							HintService.Hint(Lang.Text("Instance.Saves.CopiedToClipboard"));
							HintService.Hint(Lang.Text("Instance.Saves.CopyPasteWarning"));
						}
						else
						{
							HintService.Hint(Lang.Text("Instance.Saves.FolderNotFound"));
						}
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, Lang.Text("Instance.Saves.CopyFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.CopyFailed"));
					}
				};
				MyIconButton myIconButton4 = new MyIconButton
				{
					SvgIcon = "lucide/info",
					ToolTip = Lang.Text("Instance.Saves.Details")
				};
				myIconButton4.Click += (object _, EventArgs _) =>
				{
					ModMain.frmMain.PageChange(new FormMain.PageStackData
					{
						page = FormMain.PageType.VersionSaves,
						additional = (null, null, null, ModComp.CompLoaderType.Any, ModComp.CompType.Any, tmpCurFolder)
					});
				};
				MyIconButton myIconButton5 = new MyIconButton
				{
					SvgIcon = "lucide/play",
					ToolTip = Lang.Text("Instance.Saves.QuickPlay")
				};
				myIconButton5.Click += (object _, EventArgs _) =>
				{
					string fileNameFromPath = GetFileNameFromPath(tmpCurFolder);
					ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
					{
						WorldName = fileNameFromPath,
						instance = PageInstanceLeft.McInstance
					});
					ModMain.frmMain.PageChange(new FormMain.PageStackData
					{
						page = FormMain.PageType.Launch
					});
				};
				if ((bool)quickPlayFeature)
				{
					worldItem.Buttons = new MyIconButton[5] { myIconButton, myIconButton2, myIconButton3, myIconButton4, myIconButton5 };
				}
				else
				{
					worldItem.Buttons = new MyIconButton[4] { myIconButton, myIconButton2, myIconButton3, myIconButton4 };
				}
				PanList.Children.Add(worldItem);
			}
			ModAnimation.AniControlEnabled--;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Saves.RefreshUiFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.RefreshUiFailed"));
		}
	}

	private void CheckQuickPlay()
	{
		try
		{
			ModLaunch.LaunchArgument launchArgument = new ModLaunch.LaunchArgument(PageInstanceLeft.McInstance);
			quickPlayFeature = launchArgument.HasArguments("--quickPlaySingleplayer");
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "检查存档快捷启动失败", ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Error.OperationFailed"));
		}
	}

	private void LoadFileList()
	{
		try
		{
			ModBase.Log("[World] 刷新存档文件");
			saveFolders.Clear();
			if (Directory.Exists(worldPath))
			{
				saveFolders = Directory.EnumerateDirectories(worldPath).ToList();
			}
			else
			{
				saveFolders = new List<string>();
			}
			if (ModBase.modeDebug)
			{
				ModBase.Log("[World] 共发现 " + saveFolders.Count + " 个存档文件夹", ModBase.LogLevel.Debug);
			}
			PanList.Children.Clear();
			CheckQuickPlay();
			if (ModBase.modeDebug)
			{
				if ((bool)quickPlayFeature)
				{
					ModBase.Log("[World] 该实例支持存档快捷启动", ModBase.LogLevel.Debug);
				}
				else
				{
					ModBase.Log("[World] 该实例不支持存档快捷启动", ModBase.LogLevel.Debug);
				}
			}
			RefreshUI();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Saves.LoadListFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.LoadListFailed"));
		}
	}

	private void RemoveItem(MyListItem item)
	{
		if (PanList.Children.IndexOf(item) != -1)
		{
			PanList.Children.Remove(item);
			RefreshUI();
		}
	}

	private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.OpenExplorer(worldPath);
	}

	private void BtnPaste_Click(object sender, MouseButtonEventArgs e)
	{
		StringCollection files = Clipboard.GetFileDropList();
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		list.Add(new ModLoader.LoaderTask<int, int>("Copy saves", (ModLoader.LoaderTask<int, int> _) =>
		{
			int num = 0;
			StringEnumerator enumerator = files.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string current = enumerator.Current;
					try
					{
						if (Directory.Exists(current))
						{
							if (Directory.Exists(worldPath + GetFolderNameFromPath(current)))
							{
								HintService.Hint(Lang.Text("Instance.Saves.DuplicateFolder", new object[1] { GetFolderNameFromPath(current) }));
							}
							else
							{
								ModBase.CopyDirectory(current, worldPath + GetFolderNameFromPath(current));
								num++;
							}
						}
						else
						{
							HintService.Hint(Lang.Text("Instance.Saves.SourceNotFolder"));
						}
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, Lang.Text("Instance.Saves.PasteFolderFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.PasteFolderFailed"));
					}
				}
			}
			finally
			{
				if (enumerator is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			if (num > 0)
			{
				HintService.Hint(Lang.Text("Instance.Saves.PastedCount", new object[1] { num.ToString() }), HintType.Success);
			}
			ModBase.RunInUi(() =>
			{
				Reload();
			});
		}));
		ModLoader.LoaderCombo<int> loaderCombo = new ModLoader.LoaderCombo<int>(PageInstanceLeft.McInstance.Name + " - " + Lang.Text("Instance.Saves.CopySave"), list);
		loaderCombo.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
		loaderCombo.Start(1);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
	}

	private void BtnDownloadNew_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadWorld);
		PageComp.targetVersion = PageInstanceLeft.McInstance;
	}

	private string GetSortName(SortMethod method)
	{
		return method switch
		{
			SortMethod.FileName => Lang.Text("Instance.Saves.SortFileName"), 
			SortMethod.CreateTime => Lang.Text("Instance.Saves.SortCreateTime"), 
			SortMethod.ModifyTime => Lang.Text("Instance.Saves.SortModifyTime"), 
			_ => Lang.Text("Instance.Saves.SortFileName"), 
		};
	}

	private void SetSortMethod(SortMethod target)
	{
		_currentSortMethod = target;
		BtnSort.Text = Lang.Text("Instance.Saves.SortBy", new object[1] { GetSortName(target) });
		RefreshUI();
	}

	private void BtnSortClick(object sender, EventArgs e)
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

	private void SearchRun(object sender, EventArgs e)
	{
		searchTimer.Stop();
		searchTimer.Start();
	}

	private void SearchTimer_Tick(object sender, EventArgs e)
	{
		searchTimer.Stop();
		PerformSearch();
	}

	private void PerformSearch()
	{
		try
		{
			if (IsSearching)
			{
				List<ModBase.SearchEntry<string>> list = new List<ModBase.SearchEntry<string>>();
				foreach (string saveFolder in saveFolders)
				{
					string folderNameFromPath = GetFolderNameFromPath(saveFolder);
					List<ModBase.SearchSource> list2 = new List<ModBase.SearchSource>();
					list2.Add(new ModBase.SearchSource(folderNameFromPath));
					list.Add(new ModBase.SearchEntry<string>
					{
						item = saveFolder,
						searchSource = list2
					});
				}
				_searchResult = (from r in ModBase.Search(list, SearchBox.Text, 25, 0.35)
					select r.item).ToList();
			}
			else
			{
				_searchResult = null;
			}
			RefreshUI();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Saves.SearchError"));
		}
	}

	private Func<string, string, int> GetSortMethod(SortMethod method)
	{
		return method switch
		{
			SortMethod.FileName => (Func<string, string, int>)((string a, string b) => string.Compare(GetFolderNameFromPath(a), GetFolderNameFromPath(b), StringComparison.OrdinalIgnoreCase)), 
			SortMethod.CreateTime => (string a, string b) => Directory.GetCreationTime(b).CompareTo(Directory.GetCreationTime(a)), 
			SortMethod.ModifyTime => (string a, string b) => Directory.GetLastWriteTime(b).CompareTo(Directory.GetLastWriteTime(a)), 
			_ => (string a, string b) => string.Compare(GetFolderNameFromPath(a), GetFolderNameFromPath(b), StringComparison.OrdinalIgnoreCase), 
		};
	}

}
