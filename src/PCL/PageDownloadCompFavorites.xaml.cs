using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageDownloadCompFavorites : MyPageRight, IComponentConnector
{
	public class CompListItemContainer
	{
		public MyCard Card { get; set; }

		public StackPanel ContentList { get; set; }

		public string Title { get; set; }

		public int CompType { get; set; }
	}

	private readonly List<MyListItem> compItemList = new List<MyListItem>();

	private List<MyListItem> selectedItemList = new List<MyListItem>();

	public ModLoader.LoaderTask<List<string>, List<ModComp.CompProject>> loader;

	private readonly List<CompListItemContainer> itemList = new List<CompListItemContainer>();

	private int bottomBarShownCount;

	private bool allowSearch = true;

	private List<MyListItem> searchResult = new List<MyListItem>();

	private ModComp.CompFavorites.FavData CurrentFavTarget
	{
		get
		{
			MyComboBoxItem selectedItem = (MyComboBoxItem)ComboTargetFav.SelectedItem;
			if (selectedItem == null)
			{
				ModBase.Log("[Favorites] 异常：未选择收藏夹");
				selectedItem = (MyComboBoxItem)ComboTargetFav.Items.GetItemAt(0);
			}
			return ModComp.CompFavorites.FavoritesList.First((ModComp.CompFavorites.FavData e) => string.Equals(e.Id, selectedItem.Tag?.ToString(), StringComparison.OrdinalIgnoreCase));
		}
	}

	private bool IsSearching => !string.IsNullOrWhiteSpace(PanSearchBox.Text);

	public PageDownloadCompFavorites()
	{
		loader = new ModLoader.LoaderTask<List<string>, List<ModComp.CompProject>>("CompProject Favorites", CompFavoritesGet, LoaderInput);
		Initialized += PageDownloadCompFavorites_Inited;
		Loaded += PageDownloadCompFavorites_Loaded;
		KeyDown += Page_KeyDown;
		InitializeComponent();
		Btn_ManageTargetFav.SvgIcon = "lucide/settings";
		Btn_ManageTargetFav.Click += Manage_Click;
		Load.StateChanged += Load_State;
		Btn_FavoritesCancel.Click += Btn_FavoritesCancel_Clicked;
		Btn_SelectCancel.Click += Btn_SelectCancel_Clicked;
		Btn_FavoritesShare.Click += Btn_FavoritesShare_Clicked;
		Btn_FavoritesDownload.Click += Btn_FavoritesDownload_Clicked;
		ComboTargetFav.SelectionChanged += ComboTargetFav_Selected;
		HintGetFail.MouseLeftButtonDown += HintGetFail_MouseLeftButtonDown;
		PanSearchBox.TextChanged += SearchRun;
		WeakLanguageChanged.Add<PageDownloadCompFavorites>(this, (Action<PageDownloadCompFavorites>)OnLanguageChanged);
	}

	private static void OnLanguageChanged(PageDownloadCompFavorites page)
	{
		ModBase.RunInUi(page._RefreshCategoryTitles);
	}

	private void PageDownloadCompFavorites_Inited(object sender, EventArgs e)
	{
		RefreshFavTargets();
		PageLoaderInit(Load, PanLoad, PanContent, null, loader, (ModLoader.LoaderBase _) =>
		{
			Load_OnFinish();
		}, LoaderInput);
	}

	private void PageDownloadCompFavorites_Loaded(object sender, EventArgs e)
	{
		Items_SetSelectAll(targetStatus: false);
		RefreshBar();
		if (loader.input != null && !loader.input.Count.Equals(CurrentFavTarget.Favs.Count))
		{
			RefreshFavTargets();
		}
	}

	private List<string> LoaderInput()
	{
		List<string> list = null;
		try
		{
			list = CurrentFavTarget.Favs.Distinct().ToList();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Favorites] 加载收藏夹列表时出错");
		}
		return (List<string>)list.Clone();
	}

	private void CompFavoritesGet(ModLoader.LoaderTask<List<string>, List<ModComp.CompProject>> task)
	{
		task.output = ModComp.CompRequest.GetCompProjectsByIds(task.input);
	}

	private void RefreshFavTargets()
	{
		ComboTargetFav.Items.Clear();
		foreach (ModComp.CompFavorites.FavData favorites in ModComp.CompFavorites.FavoritesList)
		{
			MyComboBoxItem newItem = new MyComboBoxItem
			{
				Content = favorites.Name,
				Tag = favorites.Id
			};
			ComboTargetFav.Items.Add(newItem);
		}
		if (ComboTargetFav.SelectedIndex == -1)
		{
			ComboTargetFav.SelectedIndex = 0;
		}
	}

	private CompListItemContainer GetSuitListContainer(int type)
	{
		if (itemList.Any((CompListItemContainer e) => e.CompType.Equals(type)))
		{
			return itemList.First((CompListItemContainer e) => e.CompType.Equals(type));
		}
		CompListItemContainer compListItemContainer = new CompListItemContainer
		{
			Card = new MyCard
			{
				CanSwap = true,
				Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
			},
			ContentList = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Margin = new Thickness(12.0, 38.0, 12.0, 12.0)
			},
			CompType = type
		};
		compListItemContainer.Title = _GetCategoryTitleFormat(type);
		compListItemContainer.Card.Title = string.Format(compListItemContainer.Title, 0);
		compListItemContainer.Card.Children.Add(compListItemContainer.ContentList);
		itemList.Add(compListItemContainer);
		return compListItemContainer;
	}

	private void RefreshContent()
	{
		foreach (CompListItemContainer item in itemList)
		{
			item.ContentList.Children.Clear();
		}
		PanContentList.Children.Clear();
		foreach (MyListItem item2 in IsSearching ? searchResult : compItemList)
		{
			GetSuitListContainer((int)(IsSearching ? ModComp.CompType.Any : ((ModComp.CompProject)item2.Tag).Type)).ContentList.Children.Add(item2);
		}
		foreach (CompListItemContainer item3 in itemList)
		{
			if (item3.ContentList.Children.Count != 0)
			{
				PanContentList.Children.Add(item3.Card);
			}
		}
	}

	private static string _GetCategoryTitleFormat(int type)
	{
		return type switch
		{
			-1 => Lang.Text("Download.Comp.Favorites.SearchResults.Title"), 
			0 => Lang.Text("Download.Comp.Favorites.Category.Mod"), 
			1 => Lang.Text("Download.Comp.Type.Modpack") + " ({0})", 
			2 => Lang.Text("Download.Comp.Type.ResourcePack") + " ({0})", 
			3 => Lang.Text("Download.Comp.Type.Shader") + " ({0})", 
			4 => Lang.Text("Download.Comp.Type.DataPack") + " ({0})", 
			5 => Lang.Text("Download.Comp.Type.Plugin") + " ({0})", 
			7 => Lang.Text("Download.Comp.Type.World") + " ({0})", 
			_ => Lang.Text("Download.Comp.Favorites.UnknownType") + " ({0})", 
		};
	}

	private void _RefreshCategoryTitles()
	{
		foreach (CompListItemContainer item in itemList)
		{
			item.Title = _GetCategoryTitleFormat(item.CompType);
		}
		RefreshCardTitle();
	}

	private void RefreshCardTitle()
	{
		foreach (CompListItemContainer item in itemList)
		{
			item.Card.Title = string.Format(item.Title, compItemList.Where((MyListItem e) => ((ModComp.CompProject)e.Tag).Type == (ModComp.CompType)item.CompType).Count());
		}
		if (itemList.Any((CompListItemContainer e) => e.CompType.Equals(-1)))
		{
			CompListItemContainer compListItemContainer = itemList.First((CompListItemContainer e) => e.CompType.Equals(-1));
			if (compListItemContainer != null)
			{
				compListItemContainer.Card.Title = string.Format(compListItemContainer.Title, searchResult.Count);
			}
		}
	}

	private void Load_OnFinish()
	{
		itemList.Clear();
		try
		{
			allowSearch = false;
			PanSearchBox.Text = string.Empty;
			allowSearch = true;
			compItemList.Clear();
			bool flag = loader.input.Count != loader.output.Count;
			HintGetFail.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			foreach (ModComp.CompProject item in loader.output)
			{
				MyListItem myListItem = item.ToListItem();
				ListItemBuild(myListItem);
				compItemList.Add(myListItem);
			}
			if (compItemList.Any())
			{
				if (!IsSearching)
				{
					PanSearchBox.Visibility = Visibility.Visible;
					PanContentList.Visibility = Visibility.Visible;
					CardNoContent.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				PanSearchBox.Visibility = Visibility.Collapsed;
				PanContentList.Visibility = Visibility.Collapsed;
				CardNoContent.Visibility = Visibility.Visible;
			}
			RefreshContent();
			RefreshCardTitle();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化收藏夹列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Comp.Favorites.Error.OperationFailed"));
		}
	}

	private void ListItemBuild(MyListItem compItem)
	{
		compItem.Type = MyListItem.CheckType.CheckBox;
		string compId = ((ModComp.CompProject)compItem.Tag).Id;
		string notes = "";
		CurrentFavTarget.Notes.TryGetValue(compId, out notes);
		Run noteItem = new Run
		{
			Foreground = new SolidColorBrush(Color.FromRgb(0, 184, 148))
		};
		if (!string.IsNullOrWhiteSpace(notes))
		{
			noteItem.Text = " (" + notes + ")";
		}
		compItem.LabTitle.Inlines.Add(noteItem);
		MyIconButton myIconButton = new MyIconButton();
		myIconButton.SvgIcon = "lucide/pencil";
		myIconButton.ToolTip = Lang.Text("Download.Comp.Favorites.EditNote");
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender, EventArgs e) =>
		{
			CurrentFavTarget.Notes.TryGetValue(compId, out notes);
			string text = ModMain.MyMsgBoxInput(Lang.Text("Download.Comp.Favorites.EditNote"), "", notes);
			if (text != null)
			{
				CurrentFavTarget.Notes[compId] = text;
				noteItem.Text = (string.IsNullOrWhiteSpace(text) ? "" : (" (" + text + ")"));
				ModComp.CompFavorites.Save();
			}
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			SvgIcon = "lucide/heart-filled",
			ToolTip = Lang.Text("Download.Comp.Favorites.Action.Unfavorite")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object sender, EventArgs e) =>
		{
			Items_CancelFavorites(compItem);
			RefreshContent();
			RefreshCardTitle();
			RefreshBar();
		};
		compItem.Buttons = new _003C_003Ez__ReadOnlyArray<MyIconButton>(new MyIconButton[2] { myIconButton, myIconButton2 });
		if (compItem.Tag is ModComp.CompProject)
		{
			compItem.MouseRightButtonUp += (object _, MouseButtonEventArgs _) =>
			{
				ModMain.frmMain.PageChange(new FormMain.PageStackData
				{
					page = FormMain.PageType.CompDetail,
					additional = ((ModComp.CompProject)compItem.Tag, new List<string>(), string.Empty, ModComp.CompLoaderType.Any, ((ModComp.CompProject)compItem.Tag).Type, null)
				});
			};
		}
		compItem.Changed += ItemCheckStatusChanged;
	}

	private void RefreshBar()
	{
		int count = selectedItemList.Count;
		bool flag = count > 0;
		if (flag)
		{
			LabSelect.Text = Lang.Text("Download.Comp.Favorites.Hint.SelectedCount", new object[1] { count });
		}
		if (ModAnimation.AniControlEnabled == 0)
		{
			PanContentList.Margin = new Thickness(0.0, 0.0, 0.0, flag ? 80 : 0);
			if (flag)
			{
				if (bottomBarShownCount >= count)
				{
					bottomBarShownCount = count;
					return;
				}
				bottomBarShownCount = count;
				CardSelect.Visibility = Visibility.Visible;
				ModAnimation.AniStart(new ModAnimation.AniData[4]
				{
					ModAnimation.AaOpacity(CardSelect, 1.0 - CardSelect.Opacity, 60),
					ModAnimation.AaTranslateY(CardSelect, -27.0 - TransSelect.Y, 120, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaTranslateY(CardSelect, 3.0, 150, 120, new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak)),
					ModAnimation.AaTranslateY(CardSelect, -1.0, 90, 270, new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak))
				}, "CompFavorites Sidebar");
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
				}, "CompFavorites Sidebar");
			}
		}
		else
		{
			ModAnimation.AniStop("CompFavorites Sidebar");
			bottomBarShownCount = count;
			if (flag)
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
	}

	private void ItemCheckStatusChanged(object sender, ModBase.RouteEventArgs e)
	{
		MyListItem myListItem = (MyListItem)sender;
		if (selectedItemList.Contains(myListItem))
		{
			selectedItemList.Remove(myListItem);
		}
		if (myListItem.Checked)
		{
			selectedItemList.Add(myListItem);
		}
		RefreshBar();
	}

	private void Load_State(object sender, MyLoading.MyLoadingState state, MyLoading.MyLoadingState oldState)
	{
		if (loader.State == ModBase.LoadState.Failed)
		{
			string text = "";
			if (loader.Error != null)
			{
				text = loader.Error.Message;
			}
			if (text.Contains(Lang.Text("Common.Error.InvalidJson")))
			{
				ModBase.Log("[Download] 下载的工程列表 JSON 文件损坏，已自动重试", ModBase.LogLevel.Debug);
				PageLoaderRestart();
			}
		}
	}

	private void Btn_FavoritesCancel_Clicked(object sender, ModBase.RouteEventArgs e)
	{
		foreach (MyListItem item in selectedItemList.Clone())
		{
			Items_CancelFavorites(item);
		}
		if (compItemList.Any())
		{
			RefreshContent();
			RefreshCardTitle();
		}
		else
		{
			loader.Start();
		}
		RefreshBar();
	}

	private void Btn_SelectCancel_Clicked(object sender, ModBase.RouteEventArgs e)
	{
		Items_SetSelectAll(targetStatus: false);
	}

	private void Btn_FavoritesShare_Clicked(object sender, ModBase.RouteEventArgs e)
	{
		try
		{
			ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode(selectedItemList.Select((MyListItem i) => ((ModComp.CompProject)i.Tag).Id).ToHashSet()));
			Items_SetSelectAll(targetStatus: false);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[CompFavourites] 分享收藏时发生错误", ModBase.LogLevel.Hint, null, Lang.Text("Download.Comp.Favorites.Error.OperationFailed"));
		}
	}

	private void Btn_FavoritesDownload_Clicked(object sender, ModBase.RouteEventArgs e)
	{
		try
		{
			if (1 != ModMain.MyMsgBox(Lang.Text("Download.Comp.Favorites.Dialog.BulkDownload.Content"), Lang.Text("Download.Comp.Favorites.Dialog.BulkDownload.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"), "", isWarn: true))
			{
				return;
			}
			List<ModComp.CompLoaderType> list = new List<ModComp.CompLoaderType>();
			bool flag = true;
			bool flag2 = false;
			foreach (MyListItem selectedItem in selectedItemList)
			{
				ModComp.CompProject compProject = (ModComp.CompProject)selectedItem.Tag;
				if (compProject.Type == ModComp.CompType.Mod)
				{
					flag2 = true;
					if (flag)
					{
						flag = false;
						list = compProject.ModLoaders;
					}
					else
					{
						list = list.Intersect(compProject.ModLoaders).ToList();
					}
				}
			}
			if (flag2 && list.Count == 0)
			{
				HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.SelectLoader"), HintType.Error);
				return;
			}
			ModComp.CompLoaderType desiredModLoader = ModComp.CompLoaderType.Any;
			if (flag2 && list.Count > 0 && list.Count > 0)
			{
				List<IMyRadio> list2 = new List<IMyRadio>();
				foreach (ModComp.CompLoaderType item in list)
				{
					list2.Add(new MyRadioBox
					{
						Text = item.ToString()
					});
				}
				int? num = ModMain.MyMsgBoxSelect(list2, Lang.Text("Download.Comp.Favorites.Dialog.SelectLoader.Title"), null, Lang.Text("Common.Action.Cancel"));
				if (!num.HasValue)
				{
					return;
				}
				desiredModLoader = list[num.Value];
			}
			HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.LoadingVersions"));
			List<ModLoader.LoaderBase> list3 = new List<ModLoader.LoaderBase>();
			list3.Add(new ModLoader.LoaderTask<List<string>, List<DownloadFile>>(Lang.Text("Download.Comp.Favorites.LoaderName.QueryInfo"), (ModLoader.LoaderTask<List<string>, List<DownloadFile>> ts) =>
			{
				List<List<ModComp.CompFile>> allFiles = new List<List<ModComp.CompFile>>();
				List<string> suitVersion = new List<string>();
				bool flag3 = true;
				Func<List<List<string>>, List<string>> func = (List<List<string>> ls) =>
				{
					List<string> list7 = new List<string>();
					foreach (List<string> l in ls)
					{
						list7.AddRange(l);
					}
					return list7.Distinct().ToList();
				};
				int finishedTasks = 0;
				foreach (string Item in ts.input)
				{
					ModBase.RunInNewThread(() =>
					{
						try
						{
							allFiles.Add((from i in ModComp.CompFilesGet(Item, ModComp.CompRequest.IsFromCurseForge(Item))
								where i.Type != ModComp.CompType.Mod || i.ModLoaders.Contains(desiredModLoader)
								select i).ToList());
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, "获取 " + Item + " 的下载信息失败", ModBase.LogLevel.Hint, null, Lang.Text("Download.Comp.Favorites.Error.OperationFailed"));
						}
						finally
						{
							finishedTasks++;
						}
					});
				}
				while (finishedTasks != ts.input.Count)
				{
					Thread.Sleep(200);
				}
				foreach (List<ModComp.CompFile> item2 in allFiles)
				{
					List<string> list4 = func(item2.Select((ModComp.CompFile i) => i.GameVersions).ToList());
					if (flag3)
					{
						flag3 = false;
						suitVersion = list4;
					}
					else
					{
						suitVersion = suitVersion.Intersect(list4).ToList();
					}
					if (suitVersion.Count == 0)
					{
						HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.NoResource"), HintType.Error);
						ts.Abort();
						return;
					}
				}
				int? selectedVersion = 0;
				ModBase.RunInUiWait(() =>
				{
					List<IMyRadio> list7 = new List<IMyRadio>();
					foreach (string item3 in suitVersion)
					{
						list7.Add(new MyRadioBox
						{
							Text = item3
						});
					}
					selectedVersion = ModMain.MyMsgBoxSelect(list7, Lang.Text("Download.Comp.Favorites.Dialog.SelectVersion.Title"), null, Lang.Text("Common.Action.Cancel"));
					if (!selectedVersion.HasValue)
					{
						ts.Abort();
					}
				});
				string selectedVersionStr = suitVersion[selectedVersion.Value];
				HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.SelectSaveLocation", new object[1] { selectedVersionStr }));
				string text = SystemDialogs.SelectFolder((string)null, (string)null);
				if (string.IsNullOrWhiteSpace(text))
				{
					ts.Abort();
				}
				else
				{
					List<DownloadFile> list5 = new List<DownloadFile>();
					foreach (List<ModComp.CompFile> item4 in allFiles)
					{
						List<ModComp.CompFile> list6 = item4.Where((ModComp.CompFile i) => i.GameVersions.Contains(selectedVersionStr)).ToList();
						list6.Sort((ModComp.CompFile a, ModComp.CompFile b) => a.ReleaseDate > b.ReleaseDate);
						string path = ModComp.CompFileNameGet(ModComp.compProjectCache[list6.First().ProjectId], list6.First());
						list5.Add(list6.First().ToNetFile(Path.Combine(text, path)));
					}
					ts.output = list5;
				}
			})
			{
				ProgressWeight = 2.0
			});
			list3.Add(new LoaderDownload(Lang.Text("Download.Comp.Favorites.LoaderName.BatchDownloadSuitable"), new List<DownloadFile>())
			{
				ProgressWeight = 8.0
			});
			ModLoader.LoaderCombo<List<string>> loaderCombo = new ModLoader.LoaderCombo<List<string>>(Lang.Text("Download.Comp.Favorites.LoaderName.BatchDownload", new object[1] { ModBase.GetUuid() }), list3);
			loaderCombo.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
			loaderCombo.Start(selectedItemList.Select((MyListItem i) => ((ModComp.CompProject)i.Tag).Id).ToList());
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
			Items_SetSelectAll(targetStatus: false);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "批量下载收藏时发生错误", ModBase.LogLevel.Hint, null, Lang.Text("Download.Comp.Favorites.Error.OperationFailed"));
		}
	}

	private void Items_SetSelectAll(bool targetStatus)
	{
		if (IsSearching)
		{
			foreach (MyListItem item in searchResult)
			{
				item.Checked = targetStatus;
			}
		}
		else
		{
			foreach (MyListItem compItem in compItemList)
			{
				compItem.Checked = targetStatus;
			}
		}
		selectedItemList = compItemList.Where((MyListItem e) => e.Checked).ToList();
	}

	private void Items_CancelFavorites(MyListItem item)
	{
		try
		{
			compItemList.Remove(item);
			if (selectedItemList.Contains(item))
			{
				selectedItemList.Remove(item);
			}
			if (searchResult.Contains(item))
			{
				searchResult.Remove(item);
			}
			CurrentFavTarget.Favs.Remove(((ModComp.CompProject)item.Tag).Id);
			ModComp.CompFavorites.Save();
			if (!compItemList.Any())
			{
				ModMain.frmDownloadCompFavorites.PageLoaderRestart();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[CompFavourites] 移除收藏时发生错误");
		}
	}

	private void Page_KeyDown(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		if ((int)e.Key == 44 && (e.KeyboardDevice.IsKeyDown((Key)118) || e.KeyboardDevice.IsKeyDown((Key)119)))
		{
			Items_SetSelectAll(targetStatus: true);
		}
	}

	private void Manage_Click(object sender, EventArgs _)
	{
		ContextMenu contextMenu = new ContextMenu();
		MyMenuItem myMenuItem = new MyMenuItem
		{
			Header = Lang.Text("Download.Comp.Favorites.Menu.Share"),
			SvgIcon = "lucide/share-2"
		};
		myMenuItem.Click += (object obj, RoutedEventArgs e) =>
		{
			try
			{
				if (CurrentFavTarget.Favs.Count == 0)
				{
					HintWrapper.Show(Lang.Text("Download.Comp.Favorites.Hint.NothingShared"), (HintTheme)0);
				}
				else
				{
					ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode(CurrentFavTarget.Favs));
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[Favourites] 分享收藏时发生错误", ModBase.LogLevel.Hint, null, Lang.Text("Download.Comp.Favorites.Error.OperationFailed"));
			}
		};
		contextMenu.Items.Add(myMenuItem);
		myMenuItem = new MyMenuItem
		{
			Header = Lang.Text("Download.Comp.Favorites.Menu.Import"),
			SvgIcon = "lucide/circle-plus"
		};
		myMenuItem.Click += (object obj, RoutedEventArgs e) =>
		{
			try
			{
				string text = ModMain.MyMsgBoxInput(Lang.Text("Download.Comp.Favorites.Dialog.Import.Input"), "", "", null, Lang.Text("Download.Comp.Favorites.Dialog.Import.Hint"));
				if (!string.IsNullOrWhiteSpace(text))
				{
					HashSet<string> idsByShareCode = ModComp.CompFavorites.GetIdsByShareCode(text);
					if (idsByShareCode.Count == 0)
					{
						HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.NothingShared"));
					}
					else
					{
						switch (ModMain.MyMsgBox(Lang.Text("Download.Comp.Favorites.Dialog.Import.Type"), null, Lang.Text("Download.Comp.Favorites.Dialog.Import.NewFolder"), Lang.Text("Download.Comp.Favorites.Dialog.Import.CurrentFolder")))
						{
						case 1:
						{
							string text2 = ModMain.MyMsgBoxInput(Lang.Text("Download.Comp.Favorites.Dialog.Import.New"), Lang.Text("Download.Comp.Favorites.Dialog.NewPrompt"));
							if (!string.IsNullOrWhiteSpace(text2))
							{
								ModComp.CompFavorites.FavoritesList.Add(ModComp.CompFavorites.GetNewFav(text2, idsByShareCode));
								ModComp.CompFavorites.Save();
								RefreshFavTargets();
								ComboTargetFav.SelectedIndex = ComboTargetFav.Items.Count - 1;
							}
							break;
						}
						case 2:
							idsByShareCode.ToList().ForEach((string x) =>
							{
								CurrentFavTarget.Favs.Add(x);
							});
							ModComp.CompFavorites.Save();
							loader.Start(null, isForceRestart: true);
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "解析分享数据失败", ModBase.LogLevel.Hint, null, Lang.Text("Download.Comp.Favorites.Error.OperationFailed"));
			}
		};
		contextMenu.Items.Add(myMenuItem);
		myMenuItem = new MyMenuItem
		{
			Header = Lang.Text("Download.Comp.Favorites.Menu.New"),
			SvgIcon = "lucide/folder-plus"
		};
		myMenuItem.Click += (object obj, RoutedEventArgs e) =>
		{
			string text = ModMain.MyMsgBoxInput(Lang.Text("Download.Comp.Favorites.Menu.New"), Lang.Text("Download.Comp.Favorites.Dialog.NewPrompt"));
			if (!string.IsNullOrWhiteSpace(text))
			{
				ModComp.CompFavorites.FavoritesList.Add(ModComp.CompFavorites.GetNewFav(text, null));
				ModComp.CompFavorites.Save();
				RefreshFavTargets();
				ComboTargetFav.SelectedIndex = ComboTargetFav.Items.Count - 1;
			}
		};
		contextMenu.Items.Add(myMenuItem);
		myMenuItem = new MyMenuItem
		{
			Header = Lang.Text("Download.Comp.Favorites.Menu.Rename"),
			SvgIcon = "lucide/pencil"
		};
		myMenuItem.Click += (object obj, RoutedEventArgs e) =>
		{
			string text = ModMain.MyMsgBoxInput(Lang.Text("Download.Comp.Favorites.Dialog.Rename.Title"), "", CurrentFavTarget.Name);
			if (!string.IsNullOrWhiteSpace(text) && !((CurrentFavTarget.Name ?? "") == (text ?? "")))
			{
				CurrentFavTarget.Name = text;
				ModComp.CompFavorites.Save();
				RefreshFavTargets();
			}
		};
		contextMenu.Items.Add(myMenuItem);
		myMenuItem = new MyMenuItem
		{
			Header = Lang.Text("Download.Comp.Favorites.Menu.Delete"),
			SvgIcon = "lucide/trash-2"
		};
		myMenuItem.Click += (object obj, RoutedEventArgs e) =>
		{
			if (ModComp.CompFavorites.FavoritesList.Count == 1)
			{
				HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.LastCollection"));
			}
			else if (ModMain.MyMsgBox(Lang.Text("Download.Comp.Favorites.Dialog.Delete.Confirm", new object[3]
			{
				CurrentFavTarget.Name,
				CurrentFavTarget.Favs.Count,
				CurrentFavTarget.Id
			}), Lang.Text("Download.Comp.Favorites.Dialog.Delete.Title"), Lang.Text("Common.Option.No"), Lang.Text("Common.Option.Yes"), Lang.Text("Common.Option.No"), isWarn: true) == 2)
			{
				ModComp.CompFavorites.FavoritesList.Remove(CurrentFavTarget);
				ModComp.CompFavorites.Save();
				HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.Deleted"), HintType.Success);
				RefreshFavTargets();
				ComboTargetFav.SelectedIndex = 0;
			}
		};
		contextMenu.Items.Add(myMenuItem);
		contextMenu.PlacementTarget = (UIElement)sender;
		contextMenu.Placement = PlacementMode.Bottom;
		contextMenu.IsOpen = true;
	}

	private void ComboTargetFav_Selected(object sender, RoutedEventArgs e)
	{
		if (ComboTargetFav.SelectedItem != null)
		{
			Items_SetSelectAll(targetStatus: false);
			loader.Start(null, isForceRestart: true);
		}
	}

	private void HintGetFail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		string text = Lang.Text("Download.Comp.Favorites.Dialog.GetFailed.Content") + "\r\n\r\n";
		List<string> failIds = loader.input.Except(loader.output.Select((ModComp.CompProject i) => i.Id).ToList()).ToList();
		foreach (string item in failIds)
		{
			text = text + " - " + item + "\r\n";
		}
		ModMain.MyMsgBox(text, Lang.Text("Download.Comp.Favorites.Dialog.GetFailed.Title"), null, Lang.Text("Download.Comp.Favorites.Dialog.GetFailed.CopyIds"), Lang.Text("Download.Comp.Favorites.Dialog.GetFailed.Remove"), isWarn: false, highLight: true, forceWait: false, null, () =>
		{
			ModBase.ClipboardSet(failIds.Join("\r\n"));
		}, () =>
		{
			foreach (string item2 in failIds)
			{
				CurrentFavTarget.Favs.Remove(item2);
			}
			ModComp.CompFavorites.Save();
			HintService.Hint(Lang.Text("Download.Comp.Favorites.Hint.Removed"), HintType.Success);
		});
	}

	public void SearchRun(object sender, EventArgs e)
	{
		if (!allowSearch)
		{
			return;
		}
		if (IsSearching)
		{
			List<ModBase.SearchEntry<MyListItem>> list = new List<ModBase.SearchEntry<MyListItem>>();
			foreach (MyListItem compItem in compItemList)
			{
				if (compItem.Tag is ModComp.CompProject)
				{
					ModComp.CompProject compProject = (ModComp.CompProject)compItem.Tag;
					List<ModBase.SearchSource> list2 = new List<ModBase.SearchSource>();
					list2.Add(new ModBase.SearchSource(compProject.RawName));
					if (compProject.Description != null && !string.IsNullOrEmpty(compProject.Description))
					{
						list2.Add(new ModBase.SearchSource(compProject.Description, 0.4));
					}
					if ((compProject.TranslatedName ?? "") != (compProject.RawName ?? ""))
					{
						list2.Add(new ModBase.SearchSource(compProject.TranslatedName));
					}
					list2.Add(new ModBase.SearchSource(string.Join("", compProject.Tags), 0.2));
					list.Add(new ModBase.SearchEntry<MyListItem>
					{
						item = compItem,
						searchSource = list2
					});
				}
			}
			searchResult = (from r in ModBase.Search(list, PanSearchBox.Text, 25, 0.35)
				select r.item).ToList();
		}
		RefreshContent();
		RefreshCardTitle();
	}

}
