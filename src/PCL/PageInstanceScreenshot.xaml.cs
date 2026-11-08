using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceScreenshot : MyPageRight, IRefreshable, IComponentConnector
{
	private bool _AppendLock;

	private int _Offset;

	private List<string> fileList = new List<string>();

	private bool isLoad;

	private string screenshotPath;

	private static string[] allowedSuffix = new string[6] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tiff" };

	public PageInstanceScreenshot()
	{
		InitializeComponent();
		Loaded += PageSetupLaunch_Loaded;
		PanBack.ScrollChanged += RequireAppend;
		BtnOpenFolder.Click += BtnOpenFolder_Click;
		BtnOpenFolderTop.Click += BtnOpenFolder_Click;
	}

	void IRefreshable.Refresh()
	{
		RefreshSelf();
	}

	private void RefreshSelf()
	{
		RefreshAsync();
	}

	public static async Task RefreshAsync()
	{
		if (ModMain.frmInstanceScreenshot != null)
		{
			await ModMain.frmInstanceScreenshot.ReloadAsync();
		}
		ModMain.frmInstanceLeft.ItemScreenshot.Checked = true;
		HintService.Hint(Lang.Text("Instance.Saves.Status.Refreshing"), HintType.Info, log: false);
	}

	private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		screenshotPath = PageInstanceLeft.McInstance.PathIndie + "screenshots\\";
		if (!Directory.Exists(screenshotPath))
		{
			Directory.CreateDirectory(screenshotPath);
		}
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)new Func<Task>(ReloadAsync), Array.Empty<object>());
		if (!isLoad)
		{
			isLoad = true;
		}
	}

	public async Task ReloadAsync()
	{
		ModAnimation.AniControlEnabled++;
		PanBack.ScrollToHome();
		await LoadFileListAsync();
		ModAnimation.AniControlEnabled--;
	}

	private void RefreshTip()
	{
		if (fileList.Count.Equals(0))
		{
			PanNoPic.Visibility = Visibility.Visible;
			PanContent.Visibility = Visibility.Collapsed;
		}
		else
		{
			PanNoPic.Visibility = Visibility.Collapsed;
			PanContent.Visibility = Visibility.Visible;
		}
	}

	private async Task LoadFileListAsync()
	{
		ModBase.Log("[Screenshot] 刷新截图文件");
		fileList.Clear();
		if (Directory.Exists(screenshotPath))
		{
			fileList = (from f in allowedSuffix.SelectMany((string suffix) => Directory.EnumerateFiles(screenshotPath, suffix, System.IO.SearchOption.TopDirectoryOnly))
				orderby File.GetCreationTime(f) descending
				select f).ToList();
		}
		PanList.Children.Clear();
		RefreshTip();
		ModBase.Log("[Screenshot] 共发现 " + fileList.Count + " 个截图文件");
		if (fileList.Count != 0)
		{
			await ListAppendAsync(20, 0);
		}
	}

	private void RequireAppend(object sender, ScrollChangedEventArgs e)
	{
		if (fileList.Count != 0 && !_AppendLock && PanBack.VerticalOffset + PanBack.ViewportHeight >= PanBack.ExtentHeight)
		{
			((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
			{
				await ListAppendAsync();
			}), Array.Empty<object>());
		}
	}

	private async Task ListAppendAsync(int count = 20, int offset = -1)
	{
		_AppendLock = true;
		if (offset == -1)
		{
			if (_Offset * count > fileList.Count)
			{
				return;
			}
			offset = _Offset + 1;
			_Offset++;
		}
		else
		{
			_Offset = offset;
		}
		if (count * offset > fileList.Count)
		{
			return;
		}
		int j = count * offset;
		for (int loopTo = count * (offset + 1) - 1; j <= loopTo; j++)
		{
			if (j >= fileList.Count)
			{
				break;
			}
			string i = fileList.ElementAt(j);
			try
			{
				if (!File.Exists(i) || File.GetAttributes(i).HasFlag(FileAttributes.Hidden) || new FileInfo(i).Length == 0L)
				{
					continue;
				}
				MyCard myCard = new MyCard
				{
					Margin = new Thickness(7.0),
					Tag = i,
					ToolTip = i.Replace(screenshotPath, "")
				};
				Grid grid = new Grid();
				myCard.Children.Add(grid);
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = new GridLength(9.0)
				});
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = new GridLength(120.0)
				});
				grid.RowDefinitions.Add(new RowDefinition());
				Image image = new Image();
				Image image2 = image;
				image2.Source = await Task.Run(() =>
				{
					BitmapImage bitmapImage = new BitmapImage();
					using FileStream streamSource = new FileStream(i, FileMode.Open, FileAccess.Read);
					bitmapImage.BeginInit();
					bitmapImage.DecodePixelHeight = 200;
					bitmapImage.DecodePixelWidth = 400;
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = streamSource;
					bitmapImage.EndInit();
					((Freezable)bitmapImage).Freeze();
					return bitmapImage;
				});
				image.Stretch = Stretch.Uniform;
				image.Cursor = Cursors.Hand;
				image.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
				{
					try
					{
						Basics.OpenPath(i, (string)null);
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, Lang.Text("Instance.Screenshot.OpenFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Screenshot.OpenFailed"));
					}
				};
				Grid.SetRow(image, 1);
				grid.Children.Add(image);
				StackPanel stackPanel = new StackPanel();
				stackPanel.Orientation = Orientation.Horizontal;
				stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
				stackPanel.Margin = new Thickness(3.0, 5.0, 3.0, 5.0);
				Grid.SetRow(stackPanel, 2);
				grid.Children.Add(stackPanel);
				MyIconTextButton myIconTextButton = new MyIconTextButton
				{
					Name = "BtnOpen",
					Text = Lang.Text("Common.Action.Open"),
					LogoScale = 0.8,
					SvgIcon = "lucide/folder-open",
					Tag = i
				};
				myIconTextButton.Click += (object s, ModBase.RouteEventArgs ev) =>
				{
					BtnOpen_Click((MyIconTextButton)s, ev);
				};
				stackPanel.Children.Add(myIconTextButton);
				MyIconTextButton myIconTextButton2 = new MyIconTextButton
				{
					Name = "BtnDelete",
					Text = Lang.Text("Common.Action.Delete"),
					LogoScale = 0.8,
					SvgIcon = "lucide/trash-2",
					Tag = i
				};
				myIconTextButton2.Click += (object s, ModBase.RouteEventArgs ev) =>
				{
					BtnDelete_Click((MyIconTextButton)s, ev);
				};
				stackPanel.Children.Add(myIconTextButton2);
				MyIconTextButton myIconTextButton3 = new MyIconTextButton
				{
					Name = "BtnCopy",
					Text = Lang.Text("Common.Action.Copy"),
					LogoScale = 0.8,
					SvgIcon = "lucide/copy",
					Tag = i
				};
				myIconTextButton3.Click += (object s, ModBase.RouteEventArgs ev) =>
				{
					BtnCopy_Click((MyIconTextButton)s, ev);
				};
				stackPanel.Children.Add(myIconTextButton3);
				PanList.Children.Add(myCard);
				myCard.Opacity = 0.0;
				ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaOpacity(myCard, 1.0, 200) });
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[Screenshot] 创建 " + i + " 截图预览失败，图像可能损坏");
			}
		}
		_AppendLock = false;
	}

	private void RemoveItem(string path)
	{
		try
		{
			foreach (object child in PanList.Children)
			{
				if (((MyCard)child).Tag.Equals(path))
				{
					PanList.Children.Remove((UIElement)child);
					break;
				}
			}
			fileList.Remove(path);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "未能找到对应 UI");
		}
	}

	private string GetPathFromSender(MyIconTextButton sender)
	{
		return (string)sender.Tag;
	}

	private void BtnOpen_Click(MyIconTextButton sender, EventArgs e)
	{
		ModBase.OpenExplorer(GetPathFromSender(sender));
	}

	private void BtnDelete_Click(MyIconTextButton sender, EventArgs e)
	{
		string pathFromSender = GetPathFromSender(sender);
		try
		{
			FileSystem.DeleteFile(pathFromSender, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
			RemoveItem(pathFromSender);
			RefreshTip();
			HintService.Hint(Lang.Text("Instance.Screenshot.Deleted"));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Screenshot.DeleteFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Screenshot.DeleteFailed"));
		}
	}

	private void BtnCopy_Click(MyIconTextButton sender, EventArgs e)
	{
		string pathFromSender = GetPathFromSender(sender);
		if (File.Exists(pathFromSender))
		{
			int num = 0;
			while (num <= 5)
			{
				try
				{
					ModBase.Log("[Screenshot] 尝试复制" + pathFromSender + "到剪贴板");
					Clipboard.SetImage(new BitmapImage(new Uri(pathFromSender)));
					HintService.Hint(Lang.Text("Instance.Screenshot.CopiedToClipboard"));
					num = 6;
					return;
				}
				catch (Exception ex)
				{
					num++;
					ModBase.Log(ex, $"[Screenshot]第 {num} 次复制尝试失败");
				}
			}
			HintService.Hint(Lang.Text("Instance.Screenshot.CopyFailed"), HintType.Error);
		}
		else
		{
			HintService.Hint(Lang.Text("Instance.Screenshot.FileNotFound"));
		}
	}

	private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e)
	{
		if (!Directory.Exists(screenshotPath))
		{
			Directory.CreateDirectory(screenshotPath);
		}
		ModBase.OpenExplorer(screenshotPath);
	}

}
