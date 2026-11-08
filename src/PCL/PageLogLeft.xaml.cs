using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageLogLeft : MyPageLeft, IComponentConnector
{
	public ModWatcher.Watcher currentLog;

	public int currentUuid;

	public Dictionary<int, FlowDocument> flowDocuments = new Dictionary<int, FlowDocument>();

	public int isLoading;

	public List<KeyValuePair<int, ModWatcher.Watcher>> shownLogs = new List<KeyValuePair<int, ModWatcher.Watcher>>();

	public PageLogLeft()
	{
		InitializeComponent();
		Loaded += PageLogLeft_Loaded;
		Unloaded += PageLogLeft_Unloaded;
	}

	private void PageLogLeft_Loaded(object sender, RoutedEventArgs e)
	{
		Reload();
		ModMain.frmMain.BtnExtraLog.ShowRefresh();
	}

	private void PageLogLeft_Unloaded(object sender, RoutedEventArgs e)
	{
		ModMain.frmMain.BtnExtraLog.ShowRefresh();
	}

	private void Reload()
	{
		try
		{
			if (shownLogs.Count == 0)
			{
				ModMain.frmMain.PageChange((FormMain.PageType)ModMain.frmMain.PageCurrentSub);
				return;
			}
			isLoading++;
			ModMain.frmLogLeft.PanList.Children.Clear();
			ModMain.frmLogLeft.PanList.Children.Add(new TextBlock
			{
				Text = Lang.Text("LogPage.Left.InstancesTitle"),
				Margin = new Thickness(13.0, 18.0, 5.0, 4.0),
				Opacity = 0.6,
				FontSize = 12.0
			});
			foreach (KeyValuePair<int, ModWatcher.Watcher> shownLog in shownLogs)
			{
				int key = shownLog.Key;
				McInstance version = shownLog.Value.version;
				Process gameProcess = shownLog.Value.gameProcess;
				MyListItem myListItem = new MyListItem
				{
					IsScaleAnimationEnabled = false,
					Type = MyListItem.CheckType.RadioBox,
					MinPaddingRight = 30,
					Title = version.Name,
					Info = $"{version.Info} - {Lang.Date(gameProcess.StartTime, "T")}",
					Height = 40.0,
					Tag = key
				};
				myListItem.Changed += ModMain.frmLogLeft.Version_Change;
				MyIconButton myIconButton = new MyIconButton
				{
					SvgIcon = "lucide/trash-2",
					LogoScale = 1.1
				};
				myIconButton.Click += (object a, EventArgs b) =>
				{
					ModMain.frmLogLeft.Remove_Click(a, (RoutedEventArgs)b);
				};
				myListItem.Buttons = new MyIconButton[1] { myIconButton };
				if (key == currentUuid)
				{
					myListItem.Checked = true;
				}
				ModMain.frmLogLeft.PanList.Children.Add(myListItem);
			}
			if (!States.Hint.MaxGameLog)
			{
				States.Hint.MaxGameLog = true;
				HintService.Hint(Lang.Text("LogPage.MaxLines.Hint", new object[1] { 500 }));
			}
			isLoading--;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "构建游戏实时日志 UI 出错", ModBase.LogLevel.Feedback, null, Lang.Text("LogPage.Error.OperationFailed"));
		}
	}

	private void OnLogOutput(ModWatcher.Watcher sender, ModWatcher.LogOutputEventArgs e)
	{
		foreach (KeyValuePair<int, ModWatcher.Watcher> shownLog in shownLogs)
		{
			if (shownLog.Value.gameProcess.Id != sender.gameProcess.Id)
			{
				continue;
			}
			int uuid = shownLog.Key;
			Thickness margin;
			if (shownLog.Value.gameProcess.HasExited)
			{
				margin = new Thickness(0.0, 12.0, 0.0, 0.0);
			}
			else
			{
				margin = new Thickness(0.0);
			}
			ModBase.RunInUi(() =>
			{
				Paragraph item = new Paragraph(new Run(e.logText))
				{
					Foreground = e.color,
					Margin = margin
				};
				flowDocuments[uuid].Blocks.Add(item);
				ulong num = (ulong)Config.System.MaxGameLog;
				switch (num)
				{
				case 0uL:
				case 1uL:
				case 2uL:
				case 3uL:
				case 4uL:
				case 5uL:
					num = (ulong)Math.Round((decimal)num * 10m + 50m);
					break;
				case 6uL:
				case 7uL:
				case 8uL:
				case 9uL:
				case 10uL:
				case 11uL:
				case 12uL:
				case 13uL:
					num = (ulong)Math.Round((decimal)num * 50m - 150m);
					break;
				case 14uL:
				case 15uL:
				case 16uL:
				case 17uL:
				case 18uL:
				case 19uL:
				case 20uL:
				case 21uL:
				case 22uL:
				case 23uL:
				case 24uL:
				case 25uL:
				case 26uL:
				case 27uL:
				case 28uL:
					num = (ulong)Math.Round((decimal)num * 100m - 800m);
					break;
				default:
					num = ulong.MaxValue;
					break;
				}
				while ((decimal)flowDocuments[uuid].Blocks.Count > (decimal)num)
				{
					flowDocuments[uuid].Blocks.Remove(flowDocuments[uuid].Blocks.FirstBlock);
				}
			});
			break;
		}
	}

	public void Add(ModWatcher.Watcher watcher)
	{
		int uuid = ModBase.GetUuid();
		shownLogs.Add(new KeyValuePair<int, ModWatcher.Watcher>(uuid, watcher));
		watcher.LogOutput += OnLogOutput;
		ModBase.RunInUi(() =>
		{
			flowDocuments.Add(uuid, new FlowDocument());
		});
		SelectionChange(uuid);
		ModMain.frmMain.BtnExtraLog.ShowRefresh();
	}

	public void SelectionChange(int uuid)
	{
		if (isLoading > 0)
		{
			return;
		}
		if (uuid <= 0)
		{
			currentUuid = -1;
			currentLog = null;
		}
		else
		{
			foreach (KeyValuePair<int, ModWatcher.Watcher> shownLog in shownLogs)
			{
				if (shownLog.Key == uuid)
				{
					currentUuid = uuid;
					currentLog = shownLog.Value;
					break;
				}
			}
		}
		ModBase.RunInUi(() =>
		{
			ModMain.frmLogRight.Reload();
			Reload();
		});
	}

	public void RemoveItem(int uuid)
	{
		int i = 0;
		for (int num = shownLogs.Count - 1; i <= num; i++)
		{
			KeyValuePair<int, ModWatcher.Watcher> keyValuePair = shownLogs[i];
			if (keyValuePair.Key != uuid)
			{
				continue;
			}
			shownLogs.RemoveAt(i);
			if (currentUuid == keyValuePair.Key)
			{
				if (shownLogs.Count == 0)
				{
					SelectionChange(-1);
					break;
				}
				SelectionChange(shownLogs[new int[2]
				{
					new int[2]
					{
						i,
						shownLogs.Count - 1
					}.Min(),
					0
				}.Max()].Key);
			}
			else
			{
				ModBase.RunInUi(() =>
				{
					ModMain.frmLogRight.Reload();
					Reload();
				});
			}
			break;
		}
		ModMain.frmMain.BtnExtraLog.ShowRefresh();
	}

	public void Remove_Click(object sender, RoutedEventArgs e)
	{
		RemoveItem((int)((MyListItem)(object)((MyIconButton)sender).Parent).Tag);
	}

	public void Version_Change(object sender, ModBase.RouteEventArgs e)
	{
		SelectionChange((int)((MyListItem)sender).Tag);
	}

}
