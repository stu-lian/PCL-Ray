using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public partial class PageSelectRight : MyPageRight, IComponentConnector
{
	private const int normalDelay = 75;

	private const int quickDelay = 50;

	private bool isRefreshing;

	private DateTime lastInputTime = DateTime.MinValue;

	private DispatcherTimer reloadTimer;

	public bool showHidden;

	public PageSelectRight()
	{
		InitializeComponent();
		PanVerSearchBox.HintText = Lang.Text("Select.Instance.Search.Hint");
		Loaded += PageSelectRight_Loaded;
		Unloaded += PageSelectRight_Unloaded;
		LoaderInit();
	}

	private void PageSelectRight_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected Obj, but got Unknown
		ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.RunOnUpdated, 1, "versions\\");
		PanBack.ScrollToHome();
		PanVerSearchBox.TextChanged += (object a, EventArgs b) =>
		{
			PanVerSearchBox_TextChanged(a, (TextChangedEventArgs)b);
		};
		reloadTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(75L)
		};
		reloadTimer.Tick += ReloadTimer_Tick;
	}

	private void PanVerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		lastInputTime = DateTime.Now;
		isRefreshing = false;
		if (string.IsNullOrWhiteSpace(PanVerSearchBox.Text))
		{
			if (reloadTimer.Interval.TotalMilliseconds != 50.0)
			{
				reloadTimer.Interval = TimeSpan.FromMilliseconds(50L);
			}
		}
		else if (reloadTimer.Interval.TotalMilliseconds != 75.0)
		{
			reloadTimer.Interval = TimeSpan.FromMilliseconds(75L);
		}
		if (!reloadTimer.IsEnabled)
		{
			reloadTimer.Start();
		}
	}

	private void ReloadTimer_Tick(object sender, EventArgs e)
	{
		double totalMilliseconds = (DateTime.Now - lastInputTime).TotalMilliseconds;
		double totalMilliseconds2 = reloadTimer.Interval.TotalMilliseconds;
		if (totalMilliseconds >= totalMilliseconds2 && ModInstanceList.mcInstanceListLoader.State == ModBase.LoadState.Finished && !isRefreshing)
		{
			isRefreshing = true;
			((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
			{
				McInstanceListUI(ModInstanceList.mcInstanceListLoader);
				isRefreshing = false;
			}), Array.Empty<object>());
			reloadTimer.Stop();
		}
	}

	private void PageSelectRight_Unloaded(object sender, RoutedEventArgs e)
	{
		if (reloadTimer != null)
		{
			reloadTimer.Stop();
			reloadTimer.Tick -= ReloadTimer_Tick;
			reloadTimer = null;
		}
	}

	private void LoaderInit()
	{
		PageLoaderInit(Load, PanLoad, PanAllBack, null, ModInstanceList.mcInstanceListLoader, (ModLoader.LoaderBase a) =>
		{
			McInstanceListUI((ModLoader.LoaderTask<string, int>)a);
		}, null, autoRun: false);
	}

	private void Load_Click(object sender, MouseButtonEventArgs e)
	{
		if (ModInstanceList.mcInstanceListLoader.State == ModBase.LoadState.Failed)
		{
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
	}

	private void McInstanceListUI(ModLoader.LoaderTask<string, int> loader)
	{
		try
		{
			_ = loader.input;
			PanMain.Children.Clear();
			bool flag = false;
			string searchText = PanVerSearchBox.Text.Trim().ToLower();
			bool flag2 = false;
			bool flag3 = ModInstanceList.mcInstanceList.ToArray().Any((KeyValuePair<McInstanceCardType, List<McInstance>> c) => c.Value.Count > 0);
			PanEmptySearch.Visibility = Visibility.Collapsed;
			KeyValuePair<McInstanceCardType, List<McInstance>>[] array = ModInstanceList.mcInstanceList.ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				KeyValuePair<McInstanceCardType, List<McInstance>> keyValuePair = array[num];
				if ((keyValuePair.Key == McInstanceCardType.Hidden) ^ showHidden)
				{
					continue;
				}
				List<McInstance> list = keyValuePair.Value.Where((McInstance v) =>
				{
					if (string.IsNullOrEmpty(searchText))
					{
						return true;
					}
					return v.Name.ToLower().Contains(searchText) || (v.Desc != null && v.Desc.ToLower().Contains(searchText)) || v.GetDefaultDescription().Replace(",", "").ToLower()
						.Trim()
						.Contains(searchText);
				}).ToList();
				if (list.Count == 0)
				{
					continue;
				}
				flag = true;
				flag2 = true;
				if (list.Count == 0)
				{
					continue;
				}
				flag = true;
				string text = "";
				switch (keyValuePair.Key)
				{
				case McInstanceCardType.OriginalLike:
					text = Lang.Text("Select.Instance.Card.Regular");
					break;
				case McInstanceCardType.API:
				{
					bool flag4 = false;
					bool flag5 = false;
					bool flag6 = false;
					bool flag7 = false;
					bool flag8 = false;
					bool flag9 = false;
					bool flag10 = false;
					foreach (McInstance item in keyValuePair.Value)
					{
						if (!item.IsLoaded)
						{
							item.Load();
						}
						if (item.Info.HasFabric)
						{
							flag6 = true;
						}
						if (item.Info.HasQuilt)
						{
							flag7 = true;
						}
						if (item.Info.HasLiteLoader)
						{
							flag8 = true;
						}
						if (item.Info.HasForge)
						{
							flag4 = true;
						}
						if (item.Info.HasNeoForge)
						{
							flag5 = true;
						}
						if (item.Info.HasCleanroom)
						{
							flag9 = true;
						}
						if (item.Info.HasLabyMod)
						{
							flag10 = true;
						}
					}
					if ((flag8 ? 1 : 0) + (flag4 ? 1 : 0) + (flag6 ? 1 : 0) + (flag5 ? 1 : 0) + (flag7 ? 1 : 0) + (flag9 ? 1 : 0) + (flag10 ? 1 : 0) <= 1)
					{
						if (flag4)
						{
							text = Lang.Text("Select.Instance.Card.Forge");
						}
						else if (flag5)
						{
							text = Lang.Text("Select.Instance.Card.NeoForge");
						}
						else if (flag9)
						{
							text = Lang.Text("Select.Instance.Card.Cleanroom");
						}
						else if (flag10)
						{
							text = Lang.Text("Select.Instance.Card.LabyMod");
						}
						else if (flag8)
						{
							text = Lang.Text("Select.Instance.Card.LiteLoader");
						}
						else
						{
							text = ((!flag7) ? Lang.Text("Select.Instance.Card.Fabric") : Lang.Text("Select.Instance.Card.Quilt"));
						}
					}
					else
					{
						text = Lang.Text("Select.Instance.Card.Modable");
					}
					break;
				}
				case McInstanceCardType.Error:
					text = Lang.Text("Select.Instance.Card.Error");
					break;
				case McInstanceCardType.Hidden:
					text = Lang.Text("Select.Instance.Card.Hidden");
					break;
				case McInstanceCardType.Rubbish:
					text = Lang.Text("Select.Instance.Card.LessUsed");
					break;
				case McInstanceCardType.Star:
					text = Lang.Text("Select.Instance.Card.Favorites");
					break;
				case McInstanceCardType.Fool:
					text = Lang.Text("Select.Instance.Card.AprilFools");
					break;
				default:
					throw new ArgumentException($"未知的卡片种类（{(int)keyValuePair.Key}）");
				}
				string title = text + ((keyValuePair.Key == McInstanceCardType.Star) ? "" : (" (" + Lang.Number<int>(list.Count, "N0") + ")"));
				MyCard myCard = new MyCard
				{
					Title = title,
					Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
				};
				StackPanel stack = new StackPanel
				{
					Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
					VerticalAlignment = VerticalAlignment.Top,
					RenderTransform = new TranslateTransform(0.0, 0.0),
					Tag = list
				};
				myCard.Children.Add(stack);
				myCard.SwapControl = stack;
				PanMain.Children.Add(myCard);
				if (keyValuePair.Key == McInstanceCardType.Rubbish || keyValuePair.Key == McInstanceCardType.Error || keyValuePair.Key == McInstanceCardType.Fool)
				{
					myCard.IsSwapped = true;
					myCard.InstallMethod = PutMethod;
				}
				else
				{
					MyCard.StackInstall(ref stack, PutMethod);
				}
			}
			if (PanMain.Children.Count == 1 && ((MyCard)PanMain.Children[0]).IsSwapped)
			{
				((MyCard)PanMain.Children[0]).IsSwapped = false;
			}
			PanVerSearchBox.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			if (!flag2)
			{
				if (!flag3)
				{
					PanEmpty.Visibility = Visibility.Visible;
					PanBack.Visibility = Visibility.Collapsed;
					if (showHidden)
					{
						LabEmptyTitle.Text = Lang.Text("Select.Instance.Hidden.EmptyTitle");
						LabEmptyContent.Text = Lang.Text("Select.Instance.Hidden.EmptyMessage");
						BtnEmptyDownload.Visibility = Visibility.Collapsed;
					}
					else
					{
						LabEmptyTitle.Text = Lang.Text("Select.Instance.Empty.Title");
						LabEmptyContent.Text = Lang.Text("Select.Instance.Empty.Message");
						BtnEmptyDownload.Visibility = ((Config.Preference.Hide.PageDownload && !PageSetupUI.HiddenForceShow) ? Visibility.Collapsed : Visibility.Visible);
					}
				}
				else if (showHidden && ModInstanceList.mcInstanceList.ToArray().Any((KeyValuePair<McInstanceCardType, List<McInstance>> c) => c.Key == McInstanceCardType.Hidden && c.Value.Count > 0))
				{
					PanVerSearchBox.Visibility = Visibility.Visible;
					PanEmpty.Visibility = Visibility.Collapsed;
					PanBack.Visibility = Visibility.Visible;
					PanEmptySearch.Visibility = Visibility.Visible;
					LabEmptySearchTitle.Text = Lang.Text("Select.Instance.Hidden.EmptySearchTitle");
					LabEmptySearchContent.Text = (string.IsNullOrWhiteSpace(searchText) ? Lang.Text("Select.Instance.Search.EmptyInput") : Lang.Text("Select.Instance.Search.NoHiddenResult", new object[1] { searchText }));
				}
				else if (showHidden)
				{
					PanEmpty.Visibility = Visibility.Visible;
					PanBack.Visibility = Visibility.Collapsed;
					LabEmptyTitle.Text = Lang.Text("Select.Instance.Hidden.EmptyTitle");
					LabEmptyContent.Text = Lang.Text("Select.Instance.Hidden.EmptyMessage");
					BtnEmptyDownload.Visibility = Visibility.Collapsed;
					PanVerSearchBox.Visibility = Visibility.Collapsed;
				}
				else
				{
					PanVerSearchBox.Visibility = Visibility.Visible;
					PanEmpty.Visibility = Visibility.Collapsed;
					PanBack.Visibility = Visibility.Visible;
					PanEmptySearch.Visibility = Visibility.Visible;
					LabEmptySearchTitle.Text = Lang.Text("Select.Instance.EmptySearch.Title");
					LabEmptySearchContent.Text = (string.IsNullOrWhiteSpace(searchText) ? Lang.Text("Select.Instance.Search.EmptyInput") : Lang.Text("Select.Instance.Search.NoResult", new object[1] { searchText }));
				}
			}
			else
			{
				PanBack.Visibility = Visibility.Visible;
				PanEmpty.Visibility = Visibility.Collapsed;
				PanEmptySearch.Visibility = Visibility.Collapsed;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Select.Instance.Error.UiUpdate"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Instance.Error.UiUpdate"));
		}
		static void PutMethod(StackPanel stackPanel)
		{
			foreach (object item2 in (IEnumerable)stackPanel.Tag)
			{
				stackPanel.Children.Add(McVersionListItem((McInstance)item2));
			}
		}
	}

	public static MyListItem McVersionListItem(McInstance mcInstance)
	{
		MyListItem myListItem = new MyListItem
		{
			Title = mcInstance.Name,
			Info = mcInstance.Desc,
			Height = 42.0,
			Tag = mcInstance,
			SnapsToDevicePixels = true,
			Type = MyListItem.CheckType.Clickable
		};
		McInstanceInfo info = mcInstance.Info;
		List<string> list = new List<string>();
		list.Add(info.VanillaName);
		if (info.HasForge)
		{
			list.Add("Forge " + info.Forge);
		}
		else if (info.HasNeoForge)
		{
			list.Add("NeoForge " + info.NeoForge);
		}
		else if (info.HasCleanroom)
		{
			list.Add("Cleanroom " + info.Cleanroom);
		}
		else if (info.HasLabyMod)
		{
			list.Add("LabyMod " + info.LabyMod);
		}
		else if (info.HasQuilt)
		{
			list.Add("Quilt " + info.Quilt);
		}
		else if (info.HasFabric)
		{
			list.Add("Fabric " + info.Fabric);
		}
		if (info.HasLiteLoader)
		{
			list.Add("LiteLoader");
		}
		if (info.HasOptiFine)
		{
			list.Add("OptiFine " + info.OptiFine);
		}
		myListItem.Tags = list;
		try
		{
			if (mcInstance.Logo.EndsWith("PCL\\Logo.png"))
			{
				myListItem.Logo = mcInstance.PathInstance + "PCL\\Logo.png";
			}
			else
			{
				myListItem.Logo = mcInstance.Logo;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Select.Instance.Error.IconLoad"), ModBase.LogLevel.Hint, null, Lang.Text("Select.Instance.Error.IconLoad"));
			myListItem.Logo = "pack://application:,,,/images/Blocks/RedstoneBlock.png";
		}
		myListItem.ContentHandler = McVersionListContent;
		return myListItem;
	}

	private static void McVersionListContent(MyListItem sender, EventArgs e)
	{
		McInstance version = (McInstance)sender.Tag;
		sender.Click += (object a, MouseButtonEventArgs b) =>
		{
			Item_Click((MyListItem)a, b);
		};
		MyIconButton myIconButton = new MyIconButton();
		if (version.IsStar)
		{
			myIconButton.ToolTip = Lang.Text("Select.Instance.Unfavorite");
			ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
			ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
			ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
			myIconButton.LogoScale = 1.1;
			myIconButton.SvgIcon = "lucide/heart-filled";
		}
		else
		{
			myIconButton.ToolTip = Lang.Text("Select.Instance.Favorite");
			ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
			ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
			ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
			myIconButton.LogoScale = 1.1;
			myIconButton.SvgIcon = "lucide/heart";
		}
		myIconButton.Click += (object _, EventArgs _) =>
		{
			((ParameterizedProperty<object, bool>)(object)States.Instance.Starred)[(object)version.PathInstance] = !version.IsStar;
			ModInstanceList.mcInstanceListForceRefresh = true;
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.1,
			SvgIcon = "lucide/folder-open"
		};
		myIconButton2.ToolTip = Lang.Text("Select.Instance.OpenFolder");
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object _, EventArgs _) =>
		{
			PageInstanceOverall.OpenVersionFolder(version);
		};
		MyIconButton myIconButton3 = new MyIconButton
		{
			LogoScale = 1.1,
			SvgIcon = "lucide/trash-2"
		};
		myIconButton3.ToolTip = Lang.Text("Common.Action.Delete");
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton3, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton3, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton3, 2.0);
		myIconButton3.Click += (object _, EventArgs _) =>
		{
			DeleteVersion(sender, version);
		};
		if (version.state != McInstanceState.Error)
		{
			MyIconButton myIconButton4 = new MyIconButton
			{
				LogoScale = 1.1,
				SvgIcon = "lucide/settings"
			};
			myIconButton4.ToolTip = Lang.Text("Select.Instance.Settings");
			ToolTipService.SetPlacement((DependencyObject)(object)myIconButton4, PlacementMode.Center);
			ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton4, 30.0);
			ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton4, 2.0);
			myIconButton4.Click += (object _, EventArgs _) =>
			{
				PageInstanceLeft.McInstance = version;
				ModMain.frmMain.PageChange(FormMain.PageType.InstanceSetup);
			};
			sender.MouseRightButtonUp += (object _, MouseButtonEventArgs _) =>
			{
				PageInstanceLeft.McInstance = version;
				ModMain.frmMain.PageChange(FormMain.PageType.InstanceSetup);
			};
			sender.Buttons = new MyIconButton[4] { myIconButton, myIconButton2, myIconButton3, myIconButton4 };
		}
		else
		{
			MyIconButton myIconButton5 = new MyIconButton
			{
				LogoScale = 1.15,
				SvgIcon = "lucide/folder-open"
			};
			myIconButton5.ToolTip = Lang.Text("Common.Action.OpenFolder");
			ToolTipService.SetPlacement((DependencyObject)(object)myIconButton5, PlacementMode.Center);
			ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton5, 30.0);
			ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton5, 2.0);
			myIconButton5.Click += (object _, EventArgs _) =>
			{
				PageInstanceOverall.OpenVersionFolder(version);
			};
			sender.MouseRightButtonUp += (object _, MouseButtonEventArgs _) =>
			{
				PageInstanceOverall.OpenVersionFolder(version);
			};
			sender.Buttons = new MyIconButton[4] { myIconButton, myIconButton2, myIconButton3, myIconButton5 };
		}
	}

	public static void Item_Click(MyListItem sender, EventArgs e)
	{
		McInstance mcInstance = (McInstance)sender.Tag;
		if (new McInstance(mcInstance.PathInstance).Check())
		{
			ModInstanceList.McMcInstanceSelected = mcInstance;
			States.Game.SelectedInstance = ModInstanceList.McMcInstanceSelected.Name;
			ModMain.frmMain.PageBack();
		}
		else
		{
			PageInstanceOverall.OpenVersionFolder(mcInstance);
		}
	}

	private void BtnDownload_Click(object sender, MouseButtonEventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall);
	}

	public static void DeleteVersion(MyListItem item, McInstance mcInstance)
	{
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bool flag = Keyboard.IsKeyDown((Key)116) || Keyboard.IsKeyDown((Key)117);
			bool flag2 = mcInstance.state != McInstanceState.Error && (mcInstance.PathIndie ?? "") != (ModFolder.mcFolderSelected ?? "");
			switch (ModMain.MyMsgBox((flag ? Lang.Text("Select.Instance.Delete.ConfirmPermanentMessage", new object[1] { mcInstance.Name }) : Lang.Text("Select.Instance.Delete.ConfirmMessage", new object[1] { mcInstance.Name })) + (flag2 ? ("\r\n" + Lang.Text("Select.Instance.Delete.IsolatedWarning")) : ""), Lang.Text("Select.Instance.Delete.ConfirmTitle"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true))
			{
			case 1:
				ModBase.IniClearCache(Path.Combine(mcInstance.PathIndie, "options.txt"));
				((DynamicCacheConfigStorage)ConfigService.GetProvider((ConfigSource)3)).InvalidateCache((object)mcInstance.PathInstance);
				if (flag)
				{
					ModBase.DeleteDirectory(mcInstance.PathInstance);
					HintService.Hint(Lang.Text("Select.Instance.Delete.PermanentSuccess", new object[1] { mcInstance.Name }), HintType.Success);
				}
				else
				{
					FileSystem.DeleteDirectory(mcInstance.PathInstance, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
					HintService.Hint(Lang.Text("Select.Instance.Delete.RecycleBinSuccess", new object[1] { mcInstance.Name }), HintType.Success);
				}
				break;
			case 2:
				return;
			}
			if (mcInstance.displayType == McInstanceCardType.Hidden || !mcInstance.IsStar)
			{
				StackPanel stackPanel = (StackPanel)(object)item.Parent;
				if (stackPanel.Children.Count > 2)
				{
					MyCard myCard = (MyCard)(object)stackPanel.Parent;
					myCard.Title = myCard.Title.Replace(Lang.Number<int>(stackPanel.Children.Count - 1, "N0"), Lang.Number<int>(stackPanel.Children.Count - 2, "N0"));
					stackPanel.Children.Remove(item);
					if ((object)ModInstanceList.McMcInstanceSelected != null && (mcInstance.PathInstance ?? "") == (ModInstanceList.McMcInstanceSelected.PathInstance ?? ""))
					{
						ModInstanceList.McMcInstanceSelected = (McInstance)((MyListItem)stackPanel.Children[0]).Tag;
					}
					ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.UpdateOnly, 1, "versions\\");
				}
				else
				{
					ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
				}
			}
			else
			{
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
			}
		}
		catch (OperationCanceledException ex)
		{
			ModBase.Log(ex, "删除实例 " + mcInstance.Name + " 被主动取消");
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Select.Instance.Error.Delete", new object[1] { mcInstance.Name }), ModBase.LogLevel.Msgbox, null, Lang.Text("Select.Instance.Error.Delete", new object[1] { mcInstance.Name }));
		}
	}

	public void BtnEmptyDownload_Loaded()
	{
		Visibility visibility = (((Config.Preference.Hide.PageDownload && !PageSetupUI.HiddenForceShow) || showHidden) ? Visibility.Collapsed : Visibility.Visible);
		if (BtnEmptyDownload.Visibility != visibility)
		{
			BtnEmptyDownload.Visibility = visibility;
			PanLoad.TriggerForceResize();
		}
	}

}
