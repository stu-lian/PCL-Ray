using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Validate;
using PCL.Network;

namespace PCL;

public partial class PageSelectLeft : MyPageLeft, IRefreshable, IComponentConnector
{
	private bool isFirstLoad = true;

	private List<ModFolder.McFolder> mcFolderListLast;

	public PageSelectLeft()
	{
		Initialized += PageSelectLeft_Initialized;
		Loaded += PageSelectLeft_Loaded;
		InitializeComponent();
	}

	void IRefreshable.Refresh()
	{
		RefreshCurrent();
	}

	private void PageSelectLeft_Initialized(object sender, EventArgs e)
	{
		ModFolder.mcFolderListLoader.PreviewFinish += (ModLoader.LoaderBase _) =>
		{
			if (ModMain.frmSelectLeft != null)
			{
				ModBase.RunInUiWait(McFolderListUI);
			}
		};
	}

	private void PageSelectLeft_Loaded(object sender, RoutedEventArgs e)
	{
		if (isFirstLoad)
		{
			McFolderListUI();
		}
		isFirstLoad = false;
	}

	private void McFolderListUI()
	{
		try
		{
			if (mcFolderListLast != null && mcFolderListLast.SequenceEqual(ModFolder.mcFolderList))
			{
				return;
			}
			mcFolderListLast = new List<ModFolder.McFolder>(ModFolder.mcFolderList);
			ModMain.frmSelectLeft.PanList.Children.Clear();
			ModMain.frmSelectLeft.PanList.Children.Add(new TextBlock
			{
				Text = Lang.Text("Select.Folder.ListTitle"),
				Margin = new Thickness(13.0, 18.0, 5.0, 4.0),
				Opacity = 0.6,
				FontSize = 12.0
			});
			for (int i = 0; i < ModFolder.mcFolderList.Count; i++)
			{
				ModFolder.McFolder mcFolder = ModFolder.mcFolderList[i];
				ContextMenu contMenu = new ContextMenu();
				switch (mcFolder.type)
				{
				case ModFolder.McFolder.Types.Original:
					AddMenuItem("Rename", Lang.Text("Select.Folder.Rename"), "lucide/pencil", new Thickness(0.0, 2.0, 0.0, 0.0), ModMain.frmSelectLeft.Rename_Click);
					AddMenuItem("MoveUp", Lang.Text("Select.Folder.MoveUp"), "lucide/move-up", null, ModMain.frmSelectLeft.MoveUp_Click);
					AddMenuItem("MoveDown", Lang.Text("Select.Folder.MoveDown"), "lucide/move-down", null, ModMain.frmSelectLeft.MoveDown_Click);
					AddMenuItem("Open", Lang.Text("Common.Action.Open"), "lucide/folder-open", null, ModMain.frmSelectLeft.Open_Click);
					AddMenuItem("Refresh", Lang.Text("Common.Action.Refresh"), "lucide/refresh-cw", null, ModMain.frmSelectLeft.Refresh_Click);
					AddMenuItem("Delete", (ModFolder.mcFolderList.Count == 1 && mcFolder.Location == Path.Combine(ModBase.exePath, ".minecraft") + "\\") ? Lang.Text("Select.Folder.Clear") : Lang.Text("Common.Action.Delete"), "lucide/trash-2", new Thickness(0.0, 0.0, 0.0, 2.0), ModMain.frmSelectLeft.Delete_Click);
					break;
				case ModFolder.McFolder.Types.RenamedOriginal:
					AddMenuItem("Restore", Lang.Text("Select.Folder.RestoreName"), "lucide/rotate-ccw", new Thickness(0.0, 2.0, 0.0, 0.0), ModMain.frmSelectLeft.Restore_Click);
					AddMenuItem("Rename", Lang.Text("Select.Folder.Rename"), "lucide/pencil", null, ModMain.frmSelectLeft.Rename_Click);
					AddMenuItem("MoveUp", Lang.Text("Select.Folder.MoveUp"), "lucide/move-up", null, ModMain.frmSelectLeft.MoveUp_Click);
					AddMenuItem("MoveDown", Lang.Text("Select.Folder.MoveDown"), "lucide/move-down", null, ModMain.frmSelectLeft.MoveDown_Click);
					AddMenuItem("Open", Lang.Text("Common.Action.Open"), "lucide/folder-open", null, ModMain.frmSelectLeft.Open_Click);
					AddMenuItem("Refresh", Lang.Text("Common.Action.Refresh"), "lucide/refresh-cw", null, ModMain.frmSelectLeft.Refresh_Click);
					AddMenuItem("Delete", Lang.Text("Common.Action.Delete"), "lucide/trash-2", new Thickness(0.0, 0.0, 0.0, 2.0), ModMain.frmSelectLeft.Delete_Click);
					break;
				case ModFolder.McFolder.Types.Custom:
					AddMenuItem("Rename", Lang.Text("Select.Folder.Rename"), "lucide/pencil", new Thickness(0.0, 2.0, 0.0, 0.0), ModMain.frmSelectLeft.Rename_Click);
					AddMenuItem("MoveUp", Lang.Text("Select.Folder.MoveUp"), "lucide/move-up", null, ModMain.frmSelectLeft.MoveUp_Click);
					AddMenuItem("MoveDown", Lang.Text("Select.Folder.MoveDown"), "lucide/move-down", null, ModMain.frmSelectLeft.MoveDown_Click);
					AddMenuItem("Open", Lang.Text("Common.Action.Open"), "lucide/folder-open", null, ModMain.frmSelectLeft.Open_Click);
					AddMenuItem("Refresh", Lang.Text("Common.Action.Refresh"), "lucide/refresh-cw", null, ModMain.frmSelectLeft.Refresh_Click);
					AddMenuItem("Remove", Lang.Text("Select.Folder.RemoveFromList"), "lucide/list-x", null, ModMain.frmSelectLeft.Remove_Click);
					AddMenuItem("Delete", Lang.Text("Common.Action.Delete"), "lucide/trash-2", new Thickness(0.0, 0.0, 0.0, 2.0), ModMain.frmSelectLeft.Delete_Click);
					break;
				}
				MyMenuItem myMenuItem = contMenu.Items.OfType<MyMenuItem>().FirstOrDefault((MyMenuItem x) => x.Name == "MoveUp");
				MyMenuItem myMenuItem2 = contMenu.Items.OfType<MyMenuItem>().FirstOrDefault((MyMenuItem x) => x.Name == "MoveDown");
				if (i == 0)
				{
					myMenuItem.Visibility = Visibility.Collapsed;
				}
				if (i == ModFolder.mcFolderList.Count - 1)
				{
					myMenuItem2.Visibility = Visibility.Collapsed;
				}
				MyListItem newItem = new MyListItem
				{
					IsScaleAnimationEnabled = false,
					Type = MyListItem.CheckType.RadioBox,
					MinPaddingRight = 30,
					Title = mcFolder.Name,
					Info = mcFolder.Location,
					Height = 40.0,
					ContextMenu = contMenu,
					Tag = mcFolder
				};
				newItem.Changed += (object a, ModBase.RouteEventArgs b) =>
				{
					ModMain.frmSelectLeft.Folder_Change((MyListItem)a, b);
				};
				newItem.AllowDrop = true;
				newItem.MouseMove += ModMain.frmSelectLeft.Item_MouseMove;
				newItem.DragEnter += ModMain.frmSelectLeft.Item_DragEnter;
				newItem.DragOver += ModMain.frmSelectLeft.Item_DragOver;
				newItem.DragLeave += ModMain.frmSelectLeft.Item_DragLeave;
				newItem.Drop += ModMain.frmSelectLeft.Item_Drop;
				MyIconButton myIconButton = new MyIconButton
				{
					SvgIcon = "lucide/settings",
					LogoScale = 1.1
				};
				myIconButton.Click += (object _, EventArgs _) =>
				{
					contMenu.PlacementTarget = newItem;
					contMenu.IsOpen = true;
				};
				newItem.Buttons = new _003C_003Ez__ReadOnlySingleElementList<MyIconButton>(myIconButton);
				ModMain.frmSelectLeft.PanList.Children.Add(newItem);
				LogWrapper.Info("[Minecraft] 有效的 Minecraft 文件夹：" + mcFolder.Name + " > " + mcFolder.Location);
				void AddMenuItem(string name, string header, string svgIcon = null, Thickness? padding = null, RoutedEventHandler clickHandler = null)
				{
					MyMenuItem myMenuItem3 = new MyMenuItem
					{
						Name = name,
						Header = header,
						SvgIcon = svgIcon,
						Padding = (padding ?? new Thickness(0.0))
					};
					if (clickHandler != null)
					{
						myMenuItem3.Click += clickHandler;
					}
					contMenu.Items.Add(myMenuItem3);
				}
			}
			ModMain.frmSelectLeft.PanList.Children.Add(new TextBlock
			{
				Text = Lang.Text("Select.Folder.AddOrImport"),
				Margin = new Thickness(13.0, 18.0, 5.0, 4.0),
				Opacity = 0.6,
				FontSize = 12.0
			});
			if (!Directory.Exists(Path.Combine(ModBase.exePath, ".minecraft")))
			{
				MyListItem myListItem = new MyListItem
				{
					IsScaleAnimationEnabled = false,
					Type = MyListItem.CheckType.Clickable,
					Title = Lang.Text("Select.Folder.CreateNew.Title"),
					Height = 34.0,
					ToolTip = Lang.Text("Select.Folder.CreateNew.ToolTip"),
					LogoScale = 0.9,
					SvgIcon = "lucide/folder-plus"
				};
				ToolTipService.SetPlacement((DependencyObject)(object)myListItem, PlacementMode.Right);
				myListItem.Click += (object _, MouseButtonEventArgs _) =>
				{
					ModMain.frmSelectLeft.Create_Click();
				};
				ModMain.frmSelectLeft.PanList.Children.Add(myListItem);
			}
			MyListItem myListItem2 = new MyListItem
			{
				IsScaleAnimationEnabled = false,
				Type = MyListItem.CheckType.Clickable,
				Title = Lang.Text("Select.Folder.AddExisting.Title"),
				Height = 34.0,
				ToolTip = Lang.Text("Select.Folder.AddExisting.ToolTip"),
				SvgIcon = "lucide/folder-input"
			};
			ToolTipService.SetPlacement((DependencyObject)(object)myListItem2, PlacementMode.Right);
			myListItem2.Click += (object _, MouseButtonEventArgs _) =>
			{
				ModMain.frmSelectLeft.Add_Click();
			};
			ModMain.frmSelectLeft.PanList.Children.Add(myListItem2);
			MyListItem myListItem3 = new MyListItem
			{
				IsScaleAnimationEnabled = false,
				Type = MyListItem.CheckType.Clickable,
				Title = Lang.Text("Select.Folder.ImportModpack.Title"),
				Height = 34.0,
				ToolTip = Lang.Text("Select.Folder.ImportModpack.ToolTip"),
				SvgIcon = "lucide/package-plus"
			};
			ToolTipService.SetPlacement((DependencyObject)(object)myListItem3, PlacementMode.Right);
			myListItem3.Click += (object _, MouseButtonEventArgs _) =>
			{
				ModModpack.ModpackInstall();
			};
			ModMain.frmSelectLeft.PanList.Children.Add(myListItem3);
			ModMain.frmSelectLeft.PanList.Children.Add(new FrameworkElement
			{
				Height = 10.0,
				IsHitTestVisible = false
			});
			for (int num = 0; num < ModFolder.mcFolderList.Count; num++)
			{
				if (ModFolder.mcFolderList[num].Location == ModFolder.mcFolderSelected)
				{
					((MyListItem)ModMain.frmSelectLeft.PanList.Children[num + 1]).Checked = true;
					return;
				}
			}
			if (ModFolder.mcFolderList.Count == 0)
			{
				throw new ArgumentNullException("没有可用的 Minecraft 文件夹");
			}
			States.Game.SelectedFolder = ModFolder.mcFolderList[0].Location.Replace(ModBase.exePath, "$");
			((MyListItem)ModMain.frmSelectLeft.PanList.Children[1]).Checked = true;
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "构建 Minecraft 文件夹列表 UI 出错");
		}
		finally
		{
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.RunOnUpdated, 1, "versions\\");
		}
	}

	private void MoveUp_Click(object sender, RoutedEventArgs e)
	{
		ModFolder.McFolder item = (ModFolder.McFolder)((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Tag;
		int num = ModFolder.mcFolderList.IndexOf(item);
		if (num > 0)
		{
			ModFolder.mcFolderList.RemoveAt(num);
			ModFolder.mcFolderList.Insert(num - 1, item);
			UpdateFolderOrder();
		}
	}

	private void MoveDown_Click(object sender, RoutedEventArgs e)
	{
		ModFolder.McFolder item = (ModFolder.McFolder)((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Tag;
		int num = ModFolder.mcFolderList.IndexOf(item);
		if (num < ModFolder.mcFolderList.Count - 1)
		{
			ModFolder.mcFolderList.RemoveAt(num);
			ModFolder.mcFolderList.Insert(num + 1, item);
			UpdateFolderOrder();
		}
	}

	private void UpdateFolderOrder()
	{
		States.Game.Folders = ModFolder.mcFolderList.Select((ModFolder.McFolder folder) => folder.Name + ">" + folder.Location).ToArray().Join("|");
		McFolderListUI();
	}

	private void Restore_Click(object sender, RoutedEventArgs e)
	{
		ModFolder.McFolder item = (ModFolder.McFolder)((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Tag;
		int index = ModFolder.mcFolderList.IndexOf(item);
		ModFolder.mcFolderList[index].type = ModFolder.McFolder.Types.Original;
		ModFolder.mcFolderList[index].Name = Lang.Text("Select.Folder.OfficialLauncherFolder");
		UpdateFolderOrder();
	}

	private void Add_Click()
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected Obj, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected Obj, but got Unknown
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Expected Obj, but got Unknown
		string text = "";
		if (ModNet.HasDownloadingTask())
		{
			HintService.Hint(Lang.Text("Select.Folder.CannotAddWhileDownloading"), HintType.Error);
			return;
		}
		try
		{
			text = SystemDialogs.SelectFolder((string)null, (string)null);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (text.Contains('!') || text.Contains(';'))
			{
				HintService.Hint(Lang.Text("Select.Folder.InvalidPathChars"), HintType.Error);
				return;
			}
			string[] array = text.TrimEnd('\\').Split("\\");
			string text2;
			if (array.Last() == ".minecraft")
			{
				text2 = ((array.Length >= 3) ? array[^2] : "");
			}
			else
			{
				text2 = array.Last();
			}
			if (text2.Length > 40)
			{
				text2 = text2.Substring(0, 39);
			}
			string title = Lang.Text("Select.Folder.InputDisplayName.Title");
			string text3 = Lang.Text("Select.Folder.InputDisplayName.Message");
			string defaultInput = text2;
			Collection<IValidator<string>> collection = new Collection<IValidator<string>>
			{
				(IValidator<string>)new NullOrWhiteSpaceValidator(),
				(IValidator<string>)new StringLengthValidator()
			};
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			span[0] = ">";
			span[1] = "|";
			collection.Add((IValidator<string>)new BlacklistValidator(list));
			string text4 = ModMain.MyMsgBoxInput(title, text3, defaultInput, collection);
			if (!string.IsNullOrWhiteSpace(text4))
			{
				AddFolder(text, text4, showHint: true);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Select.Folder.Error.Add", new object[1] { text }), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Folder.Error.Add", new object[1] { text }));
		}
	}

	public static void AddFolder(string folderPath, string displayName, bool showHint)
	{
		ModBase.RunInThread(() =>
		{
			try
			{
				if (!folderPath.EndsWith("\\"))
				{
					folderPath += "\\";
				}
				if (!ModBase.CheckPermission(folderPath))
				{
					if (!showHint)
					{
						throw new Exception("PCL 没有访问文件夹的权限：" + folderPath);
					}
					HintService.Hint(Lang.Text("Select.Folder.AccessDenied"), HintType.Error);
				}
				else
				{
					if (!ModBase.CheckPermission(folderPath + "versions\\"))
					{
						DirectoryInfo[] directories = new DirectoryInfo(folderPath).GetDirectories();
						foreach (DirectoryInfo directoryInfo in directories)
						{
							if (ModBase.CheckPermission(Path.Combine(directoryInfo.FullName, "versions")))
							{
								folderPath = directoryInfo.FullName + "\\";
								break;
							}
						}
					}
					List<string> list = new List<string>(States.Game.Folders.Split("|"));
					bool flag = false;
					bool flag2 = false;
					int j = 0;
					for (int num = list.Count - 1; j <= num; j++)
					{
						string text = list[j];
						if (!string.IsNullOrEmpty(text) && !(text.Split(">")[1] != (folderPath ?? "")))
						{
							flag = true;
							if (!(text.Split(">")[0] == displayName))
							{
								list[j] = displayName + ">" + folderPath;
								flag2 = true;
								if (showHint)
								{
									HintService.Hint(Lang.Text("Select.Folder.NameUpdated", new object[1] { displayName }), HintType.Success);
								}
								break;
							}
							if (showHint)
							{
								HintService.Hint(Lang.Text("Select.Folder.AlreadyInList"));
							}
							return;
						}
					}
					if (!flag)
					{
						list.Add(displayName + ">" + folderPath);
					}
					States.Game.Folders = list.ToArray().Join("|");
					States.Game.SelectedFolder = folderPath.Replace(ModBase.exePath, "$");
					ModFolder.mcFolderListLoader.Start(null, isForceRestart: true);
					if (!flag2)
					{
						if (showHint)
						{
							HintService.Hint(Lang.Text("Select.Folder.Added", new object[1] { displayName }), HintType.Success);
						}
						DirectoryInfo directoryInfo2 = new DirectoryInfo(folderPath + "mods\\");
						if (directoryInfo2.Exists && directoryInfo2.EnumerateFiles().Count() >= 3)
						{
							DirectoryInfo directoryInfo3 = new DirectoryInfo(folderPath + "versions\\");
							if (directoryInfo3.Exists && directoryInfo3.EnumerateDirectories().Count() <= 3)
							{
								foreach (DirectoryInfo item in directoryInfo3.EnumerateDirectories())
								{
									McInstance mcInstance = new McInstance(item.FullName);
									mcInstance.Load();
									if (mcInstance.Modable)
									{
										DirectoryInfo directoryInfo4 = new DirectoryInfo(mcInstance.PathInstance + "mods\\");
										if (directoryInfo4.Exists && directoryInfo4.EnumerateFiles().Any())
										{
											break;
										}
										((ParameterizedProperty<object, int>)(object)Config.Instance.IndieV1)[(object)mcInstance.PathInstance] = 2;
										((ParameterizedProperty<object, bool>)(object)Config.Instance.IndieV2)[(object)mcInstance.PathInstance] = false;
										ModBase.Log("[Setup] 已自动关闭单版本隔离：" + mcInstance.Name, ModBase.LogLevel.Debug);
									}
								}
								return;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Select.Folder.Error.AddNew"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Folder.Error.AddNew"));
			}
		});
	}

	public void Create_Click()
	{
		if (ModNet.HasDownloadingTask())
		{
			HintService.Hint(Lang.Text("Select.Folder.CannotCreateWhileDownloading"), HintType.Error);
			return;
		}
		if (!Directory.Exists(ModBase.exePath + ".minecraft\\"))
		{
			Directory.CreateDirectory(ModBase.exePath + ".minecraft\\");
			Directory.CreateDirectory(ModBase.exePath + ".minecraft\\versions\\");
			States.Game.SelectedFolder = "$.minecraft\\";
			ModFolder.McFolderLauncherProfilesJsonCreate(ModBase.exePath + ".minecraft\\");
			HintService.Hint(Lang.Text("Select.Folder.CreateSuccess"), HintType.Success);
		}
		ModFolder.mcFolderListLoader.Start(null, isForceRestart: true);
	}

	public void Remove_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ModFolder.McFolder mcFolder = (ModFolder.McFolder)((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Tag;
			switch (ModMain.MyMsgBox(Lang.Text("Select.Folder.Cleanup.Message"), Lang.Text("Select.Folder.Cleanup.Title"), Lang.Text("Common.Action.Delete"), Lang.Text("Select.Folder.Cleanup.Keep"), Lang.Text("Common.Action.Cancel")))
			{
			case 1:
				if (File.Exists(mcFolder.Location + "PCL.ini"))
				{
					File.Delete(mcFolder.Location + "PCL.ini");
				}
				if (!Directory.Exists(mcFolder.Location + "versions\\"))
				{
					break;
				}
				foreach (DirectoryInfo item in new DirectoryInfo(mcFolder.Location + "versions\\").EnumerateDirectories())
				{
					if (Directory.Exists(Path.Combine(item.FullName, "PCL")))
					{
						Directory.Delete(Path.Combine(item.FullName, "PCL"), recursive: true);
					}
				}
				break;
			case 3:
				return;
			}
			List<string> list = new List<string>(States.Game.Folders.Split("|"));
			string text = "";
			int i = 0;
			for (int num = list.Count - 1; i <= num && !string.IsNullOrEmpty(list[i]); i++)
			{
				if (list[i].EndsWith(mcFolder.Location))
				{
					text = list[i].BeforeFirst(">");
					list.RemoveAt(i);
					break;
				}
			}
			States.Game.Folders = ((list.Count == 0) ? "" : list.ToArray().Join("|"));
			HintService.Hint((mcFolder.type == ModFolder.McFolder.Types.Custom) ? Lang.Text("Select.Folder.RemoveSuccess", new object[1] { text }) : Lang.Text("Select.Folder.RestoreSuccess"), HintType.Success);
			ModFolder.mcFolderListLoader.Start(null, isForceRestart: true);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Select.Folder.Error.Remove"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Folder.Error.Remove"));
		}
	}

	public void Delete_Click(object sender, RoutedEventArgs e)
	{
		MyListItem myListItem = (MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget;
		ModFolder.McFolder folder = (ModFolder.McFolder)myListItem.Tag;
		ModFolder.McFolder.Types type = folder.type;
		bool flag = (uint)type <= 1u;
		bool isClearing = flag && folder.Location == ModBase.exePath + ".minecraft\\" && ModFolder.mcFolderList.Count == 1;
		string deleteText = Lang.Text(isClearing ? "Select.Folder.Clear" : "Common.Action.Delete");
		string caption = Lang.Text(isClearing ? "Select.Folder.Clear.FirstWarning" : "Select.Folder.Delete.FirstWarning", new object[1] { folder.Location });
		string caption2 = Lang.Text(isClearing ? "Select.Folder.Clear.FinalWarning" : "Select.Folder.Delete.FinalWarning", new object[1] { folder.Location });
		string button = Lang.Text(isClearing ? "Select.Folder.Clear.Confirm" : "Select.Folder.Delete.Confirm");
		string inProgress = Lang.Text(isClearing ? "Select.Folder.Clear.InProgress" : "Select.Folder.Delete.InProgress", new object[1] { folder.Name });
		string success = Lang.Text(isClearing ? "Select.Folder.Clear.Success" : "Select.Folder.Delete.Success", new object[1] { folder.Name });
		if (ModMain.MyMsgBox(caption, Lang.Text("Select.Folder.Delete.WarningTitle"), Lang.Text("Common.Action.Cancel"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 2 || ModMain.MyMsgBox(caption2, Lang.Text("Select.Folder.Delete.WarningTitle"), button, Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
		{
			return;
		}
		List<string> list = States.Game.Folders.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
		int num = list.FindIndex((string f) => f.EndsWith(folder.Location));
		if (num >= 0)
		{
			list.RemoveAt(num);
		}
		States.Game.Folders = string.Join("|", list);
		ModBase.RunInNewThread(() =>
		{
			try
			{
				HintService.Hint(inProgress);
				ModBase.DeleteDirectory(folder.Location);
				if (isClearing)
				{
					Directory.CreateDirectory(folder.Location);
				}
				HintService.Hint(success, HintType.Success);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Select.Folder.Error.Operate", new object[2] { deleteText, folder.Name }), ModBase.LogLevel.Hint, null, Lang.Text("Select.Folder.Error.Operate", new object[2] { deleteText, folder.Name }));
			}
			finally
			{
				ModFolder.mcFolderListLoader.Start(null, isForceRestart: true);
			}
		}, "Folder Delete " + ModBase.GetUuid(), ThreadPriority.BelowNormal);
	}

	public void Open_Click(object sender, RoutedEventArgs e)
	{
		ModBase.OpenExplorer(((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Info);
	}

	public void Refresh_Click(object sender, RoutedEventArgs e)
	{
		RefreshCurrent(((ModFolder.McFolder)((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Tag).Location);
	}

	public void RefreshCurrent()
	{
		RefreshCurrent(ModFolder.mcFolderSelected);
	}

	public static void RefreshCurrent(string folder)
	{
		ModBase.WriteIni(Path.Combine(folder, "PCL.ini"), "InstanceCache", "");
		if (folder == ModFolder.mcFolderSelected)
		{
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
	}

	public void Rename_Click(object sender, RoutedEventArgs e)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected Obj, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected Obj, but got Unknown
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected Obj, but got Unknown
		ModFolder.McFolder mcFolder = (ModFolder.McFolder)((MyListItem)((Popup)(object)((ContextMenu)(object)((MyMenuItem)sender).Parent).Parent).PlacementTarget).Tag;
		try
		{
			string title = Lang.Text("Select.Folder.Rename.Title");
			string name = mcFolder.Name;
			Collection<IValidator<string>> collection = new Collection<IValidator<string>>
			{
				(IValidator<string>)new NullOrWhiteSpaceValidator(),
				(IValidator<string>)new StringLengthValidator(1, 30)
			};
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			span[0] = ">";
			span[1] = "|";
			collection.Add((IValidator<string>)new BlacklistValidator(list));
			string text = ModMain.MyMsgBoxInput(title, "", name, collection);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			List<string> list2 = new List<string>(States.Game.Folders.Split("|"));
			bool flag = false;
			int i = 0;
			for (int num2 = list2.Count - 1; i <= num2; i++)
			{
				string text2 = list2[i];
				if (!string.IsNullOrEmpty(text2) && !(text2.Split(">")[1] != (mcFolder.Location ?? "")))
				{
					flag = true;
					if (text2.Split(">")[0] == text)
					{
						return;
					}
					list2[i] = text + ">" + mcFolder.Location;
					break;
				}
			}
			if (!flag)
			{
				list2.Add(text + ">" + mcFolder.Location);
			}
			HintService.Hint(Lang.Text("Select.Folder.NameUpdated", new object[1] { text }), HintType.Success);
			States.Game.Folders = list2.ToArray().Join("|");
			ModFolder.mcFolderListLoader.Start(null, isForceRestart: true);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Select.Folder.Error.Rename"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Folder.Error.Rename"));
		}
	}

	public void Folder_Change(MyListItem sender, ModBase.RouteEventArgs e)
	{
		if (e.raiseByMouse && sender.Checked)
		{
			if (ModNet.HasDownloadingTask(ignoreCustomDownload: true))
			{
				HintService.Hint(Lang.Text("Select.Folder.SwitchBlockedByDownload"), HintType.Error);
				e.handled = true;
			}
			else
			{
				States.Game.SelectedFolder = ((ModFolder.McFolder)sender.Tag).Location.Replace(ModBase.exePath, "$");
				ModFolder.mcFolderListLoader.Start(null, isForceRestart: true);
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.RunOnUpdated, 1, "versions\\");
			}
		}
	}

	private void Item_MouseMove(object sender, MouseEventArgs e)
	{
		MyListItem myListItem = (MyListItem)sender;
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		try
		{
			DragDrop.DoDragDrop((DependencyObject)(object)myListItem, myListItem.Tag, DragDropEffects.Move);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始拖拽操作失败");
		}
	}

	private void Item_DragEnter(object sender, DragEventArgs e)
	{
		try
		{
			if (e.Data.GetDataPresent(typeof(ModFolder.McFolder)))
			{
				e.Effects = DragDropEffects.Move;
				((MyListItem)sender).Opacity = 0.7;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}
		catch (Exception)
		{
			e.Effects = DragDropEffects.None;
		}
		e.Handled = true;
	}

	private void Item_DragOver(object sender, DragEventArgs e)
	{
		try
		{
			e.Effects = (e.Data.GetDataPresent(typeof(ModFolder.McFolder)) ? DragDropEffects.Move : DragDropEffects.None);
		}
		catch (Exception)
		{
			e.Effects = DragDropEffects.None;
		}
		e.Handled = true;
	}

	private void Item_DragLeave(object sender, DragEventArgs e)
	{
		try
		{
			((MyListItem)sender).Opacity = 1.0;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "拖拽离开处理失败");
		}
		e.Handled = true;
	}

	private void Item_Drop(object sender, DragEventArgs e)
	{
		try
		{
			MyListItem myListItem = (MyListItem)sender;
			ModFolder.McFolder mcFolder = (ModFolder.McFolder)myListItem.Tag;
			myListItem.Opacity = 1.0;
			if (!e.Data.GetDataPresent(typeof(ModFolder.McFolder)))
			{
				e.Handled = true;
				return;
			}
			ModFolder.McFolder mcFolder2 = (ModFolder.McFolder)e.Data.GetData(typeof(ModFolder.McFolder));
			if (mcFolder2 == mcFolder)
			{
				e.Handled = true;
				return;
			}
			if (!ModFolder.mcFolderList.Contains(mcFolder2) || !ModFolder.mcFolderList.Contains(mcFolder))
			{
				e.Handled = true;
				return;
			}
			int num = ModFolder.mcFolderList.IndexOf(mcFolder2);
			int num2 = ModFolder.mcFolderList.IndexOf(mcFolder);
			if (num != num2)
			{
				ModFolder.mcFolderList.RemoveAt(num);
				int num3 = num2;
				if (num3 > ModFolder.mcFolderList.Count)
				{
					num3 = ModFolder.mcFolderList.Count;
				}
				else if (num3 < 0)
				{
					num3 = 0;
				}
				ModFolder.mcFolderList.Insert(num3, mcFolder2);
				UpdateFolderOrder();
				string value = ((num < num2) ? "后面" : "前面");
				ModBase.Log($"[Control] 文件夹拖拽排序：{mcFolder2.Name} -> 位置 {num3} (在 {mcFolder.Name} {value})", ModBase.LogLevel.Debug);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Select.Folder.Error.DragDrop"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Folder.Error.DragDrop"));
		}
		finally
		{
			e.Handled = true;
		}
	}

}
