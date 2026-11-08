using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Network;

namespace PCL;

public partial class PageSpeedLeft : MyPageLeft, IComponentConnector
{
	private const int watcherInterval = 300;

	private readonly Dictionary<string, MyCard> rightCards = new Dictionary<string, MyCard>();

	private bool isLoad;

	public PageSpeedLeft()
	{
		InitializeComponent();
		Loaded += Page_Loaded;
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Watcher();
		TryReturnToHome();
		if (!isLoad)
		{
			isLoad = true;
			DispatcherTimer val = new DispatcherTimer
			{
				Interval = new TimeSpan(0, 0, 0, 0, 300)
			};
			val.Tick += (object? _, EventArgs _) =>
			{
				Watcher();
			};
			val.Start();
			if (!ModBase.modeDebug)
			{
				RowDefinitions[12].Height = new GridLength(0.0);
				RowDefinitions[13].Height = new GridLength(0.0);
				RowDefinitions[14].Height = new GridLength(0.0);
				RowDefinitions[15].Height = new GridLength(0.0);
			}
		}
	}

	private void Watcher()
	{
		if (ModMain.frmMain.pageCurrent != FormMain.PageType.TaskManager)
		{
			return;
		}
		try
		{
			if (!ModLoader.loaderTaskbar.Any())
			{
				LabProgress.Text = Lang.Number<double>(1.0, "P0");
				LabSpeed.Text = ModBase.GetString(0L) + "/s";
				LabFile.Text = Lang.Number<int>(0, "N0");
				LabThread.Text = Lang.Number<int>(0, "N0") + " / " + Lang.Number<int>(ModNet.NetTaskConnectionLimit, "N0");
			}
			else
			{
				List<ModLoader.LoaderBase> source = ModLoader.loaderTaskbar.Where((ModLoader.LoaderBase l) => l.show).ToList();
				double num = (source.Any() ? ModBase.MathClamp(source.Average((ModLoader.LoaderBase l) => l.Progress), 0.0, 1.0) : 1.0);
				string text = Lang.Number<double>(num, "P2");
				LabProgress.Text = ((num > 0.999999) ? Lang.Number<double>(1.0, "P0") : text);
				LabSpeed.Text = ModBase.GetString(ModNet.NetManager.Speed) + "/s";
				LabFile.Text = ((ModNet.NetManager.FileRemain < 0) ? "0*" : Lang.Number<int>(ModNet.NetManager.FileRemain, "N0"));
				LabThread.Text = Lang.Number<int>(ModNet.NetManager.ConnectionCount, "N0") + " / " + Lang.Number<int>(ModNet.NetTaskConnectionLimit, "N0");
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "任务管理左栏监视出错", ModBase.LogLevel.Feedback, null, Lang.Text("Speed.Error.OperationFailed"));
		}
		if (ModMain.frmSpeedRight == null || ModMain.frmSpeedRight.PanMain == null)
		{
			return;
		}
		try
		{
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				TaskRefresh(item);
			}
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "任务管理右栏监视出错", ModBase.LogLevel.Feedback, null, Lang.Text("Speed.Error.OperationFailed"));
		}
	}

	public void TaskRefresh(ModLoader.LoaderBase loader)
	{
		if (loader == null || !loader.show)
		{
			return;
		}
		try
		{
			List<ModLoader.LoaderBase> loaderList = ((ModLoader.LoaderCombo)loader).GetLoaderList();
			if (rightCards.ContainsKey(loader.name))
			{
				Grid grid = rightCards[loader.name];
				double num = loader.Progress + (double)loader.State;
				if (ModBase.Val(grid.Tag) == num)
				{
					return;
				}
				grid.Tag = num;
				if (grid.Children.Count > 3)
				{
					grid = (Grid)grid.Children[3];
					try
					{
						switch (loader.State)
						{
						case ModBase.LoadState.Failed:
						{
							grid.RowDefinitions.Clear();
							grid.Children.Clear();
							grid.Children.Add((UIElement)ModBase.GetObjectFromXML("<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Stretch=\"Uniform\" Tag=\"Failed\" Data=\"F1 M2.5,0 L0,2.5 7.5,10 0,17.5 2.5,20 10,12.5 17.5,20 20,17.5 12.5,10 20,2.5 17.5,0 10,7.5 2.5,0Z\" Height=\"15\" Width=\"15\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"0\" Fill=\"{DynamicResource ColorBrush3}\" Margin=\"0,1,0,0\" VerticalAlignment=\"Top\"/>"));
							TextBlock textBlock = (TextBlock)ModBase.GetObjectFromXML("<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TextWrapping=\"Wrap\" HorizontalAlignment=\"Left\" ToolTip=\"" + Lang.Text("Speed.Error.ClickToCopy") + "\" Grid.Column=\"1\" Grid.Row=\"0\" Margin=\"0,0,0,5\" />");
							textBlock.Text = loader.Error.ToString();
							textBlock.MouseLeftButtonDown += (object sender, MouseButtonEventArgs _) =>
							{
								ModBase.ClipboardSet(((TextBlock)sender).Text, showSuccessHint: false);
								HintService.Hint(Lang.Text("Speed.Error.Copied"), HintType.Success);
							};
							grid.Children.Add(textBlock);
							break;
						}
						case ModBase.LoadState.Finished:
							ModAnimation.AniDispose((MyCard)(object)grid.Parent, removeFromChildren: true, (object? _) =>
							{
								TryReturnToHome();
							});
							break;
						case ModBase.LoadState.Waiting:
						case ModBase.LoadState.Loading:
							try
							{
								if (grid.Children.Count < loaderList.Count * 2)
								{
									ModBase.Log($"[Watcher] 刷新任务管理卡片 {loader.name} 失败：卡片中仅有 {grid.Children.Count} 个子项，要求至少有 {loaderList.Count * 2} 个子项", ModBase.LogLevel.Debug);
									break;
								}
								int num2 = 0;
								foreach (ModLoader.LoaderBase item in loaderList)
								{
									switch (item.State)
									{
									case ModBase.LoadState.Waiting:
										if ((string)((FrameworkElement)grid.Children[num2 * 2]).Tag != "Waiting")
										{
											grid.Children.RemoveAt(num2 * 2);
											grid.Children.Insert(num2 * 2, (UIElement)ModBase.GetObjectFromXML("<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\" Stretch=\"Uniform\" Tag=\"Waiting\" Data=\"F1 M5,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 Z\" Width=\"18\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"" + num2 + "\" Fill=\"{DynamicResource ColorBrush3}\" Margin=\"0,7,0,0\" VerticalAlignment=\"Top\" Height=\"6\"/>"));
										}
										break;
									case ModBase.LoadState.Loading:
										if ((string)((FrameworkElement)grid.Children[num2 * 2]).Tag != "Loading")
										{
											grid.Children.RemoveAt(num2 * 2);
											grid.Children.Insert(num2 * 2, (UIElement)ModBase.GetObjectFromXML($"<TextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\" Text=\"{Lang.Number<double>(item.Progress, "P0")}\" Tag=\"Loading\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"{num2}\" Foreground=\"{{DynamicResource ColorBrush3}}\"/>"));
										}
										else
										{
											((TextBlock)grid.Children[num2 * 2]).Text = Lang.Number<double>(item.Progress, "P0") ?? "";
										}
										break;
									case ModBase.LoadState.Finished:
										if ((string)((FrameworkElement)grid.Children[num2 * 2]).Tag != "Finished")
										{
											grid.Children.RemoveAt(num2 * 2);
											grid.Children.Insert(num2 * 2, (UIElement)ModBase.GetObjectFromXML($"<Path xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\" Stretch=\"Uniform\" Tag=\"Finished\" Data=\"F1 M 23.7501,33.25L 34.8334,44.3333L 52.2499,22.1668L 56.9999,26.9168L 34.8334,53.8333L 19.0001,38L 23.7501,33.25 Z\" Height=\"16\" Width=\"15\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"{num2}\" Fill=\"{{DynamicResource ColorBrush3}}\" Margin=\"0,3,0,0\" VerticalAlignment=\"Top\"/>"));
										}
										break;
									}
									num2++;
								}
								break;
							}
							catch (Exception ex)
							{
								ModBase.Log(ex, "刷新任务管理卡片 " + loader.name + " 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Speed.Error.OperationFailed"));
								break;
							}
						}
						return;
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, $"更新任务管理显示失败（{loader.State}）", ModBase.LogLevel.Feedback, null, Lang.Text("Speed.Error.OperationFailed"));
						return;
					}
				}
				ModBase.Log("[Watcher] 元素不足的卡片：" + loader.name, ModBase.LogLevel.Debug);
			}
			else
			{
				if (loader.State == ModBase.LoadState.Aborted || loader.State == ModBase.LoadState.Finished)
				{
					return;
				}
				try
				{
					string text = $"\n                        <local:MyCard xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\"\n                            Tag=\"{loader.Progress + (double)loader.State}\" Title=\"{ModBase.EscapeXML(loader.name)}\" Margin=\"0,0,0,15\">\n                            <Grid Margin=\"14,40,15,10\">\n                                <Grid.ColumnDefinitions>\n                                    <ColumnDefinition Width=\"50\"/>\n                                    <ColumnDefinition/>\n                                </Grid.ColumnDefinitions>\n                                <Grid.RowDefinitions>";
					foreach (ModLoader.LoaderBase item2 in loaderList)
					{
						_ = item2;
						text += "<RowDefinition Height=\"26\"/>";
					}
					text += "</Grid.RowDefinitions>";
					int num3 = 0;
					foreach (ModLoader.LoaderBase item3 in loaderList)
					{
						text = item3.State switch
						{
							ModBase.LoadState.Waiting => text + $"<Path Stretch=\"Uniform\" Tag=\"Waiting\" Data=\"F1 M5,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 m15,0 a5,5 360 1 0 0,0.0001 Z\" Width=\"18\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"{num3}\" Fill=\"{{DynamicResource ColorBrush3}}\" Margin=\"0,7,0,0\" VerticalAlignment=\"Top\" Height=\"6\"/>", 
							ModBase.LoadState.Loading => text + $"<TextBlock Text=\"{Lang.Number<double>(item3.Progress, "P0")}\" Tag=\"Loading\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"{num3}\" Foreground=\"{{DynamicResource ColorBrush3}}\" />", 
							ModBase.LoadState.Finished => text + $"<Path Stretch=\"Uniform\" Tag=\"Finished\" Data=\"F1 M 23.7501,33.25L 34.8334,44.3333L 52.2499,22.1668L 56.9999,26.9168L 34.8334,53.8333L 19.0001,38L 23.7501,33.25 Z\" Height=\"16\" Width=\"15\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"{num3}\" Fill=\"{{DynamicResource ColorBrush3}}\" Margin=\"0,3,0,0\" VerticalAlignment=\"Top\"/>", 
							_ => text + $"<Path Stretch=\"Uniform\" Tag=\"Failed\" Data=\"F1 M2.5,0 L0,2.5 7.5,10 0,17.5 2.5,20 10,12.5 17.5,20 20,17.5 12.5,10 20,2.5 17.5,0 10,7.5 2.5,0Z\" Height=\"15\" Width=\"15\" HorizontalAlignment=\"Center\" Grid.Column=\"0\" Grid.Row=\"{num3}\" Fill=\"{{DynamicResource ColorBrush3}}\" Margin=\"0,1,0,0\" VerticalAlignment=\"Top\"/>", 
						};
						text += $"<TextBlock Text=\"{ModBase.EscapeXML(item3.name)}\" HorizontalAlignment=\"Left\" Grid.Column=\"1\" Grid.Row=\"{num3}\"/>";
						num3++;
					}
					text += "</Grid></local:MyCard>";
					MyCard card;
					try
					{
						card = (MyCard)ModBase.GetObjectFromXML(text);
					}
					catch (Exception ex3)
					{
						ModBase.Log(ex3, "新建任务管理卡片失败");
						ModBase.Log("出错的卡片内容：\r\n" + text);
						throw;
					}
					ModMain.frmSpeedRight.PanMain.Children.Insert(0, card);
					rightCards.Add(loader.name, card);
					ModBase.Log("[Watcher] 新建任务管理卡片：" + loader.name);
					MyIconButton myIconButton = new MyIconButton
					{
						Name = "BtnCancel",
						SvgIcon = "lucide/x",
						Height = 25.0,
						Width = 25.0,
						Margin = new Thickness(0.0, 10.0, 10.0, 0.0),
						LogoScale = 1.1,
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Top
					};
					card.Children.Add(myIconButton);
					myIconButton.Click += (object sender, EventArgs e) =>
					{
						ModAnimation.AniDispose((MyIconButton)sender, removeFromChildren: false);
						ModAnimation.AniDispose(card, removeFromChildren: true, (object? _) =>
						{
							if (ModMain.frmSpeedRight.PanMain.Children.Count == 0 && ModMain.frmMain.pageCurrent == FormMain.PageType.TaskManager)
							{
								ModMain.frmMain.PageBack();
							}
						});
						rightCards.Remove(loader.name);
						ModLoader.loaderTaskbar.Remove(loader);
						ModBase.Log("[Taskbar] 关闭任务管理卡片：" + loader.name + "，且移出任务列表");
						ModBase.RunInThread(() =>
						{
							loader.Abort();
						});
					};
					if (loader.State == ModBase.LoadState.Failed)
					{
						card.Tag = null;
						TaskRefresh(loader);
					}
					return;
				}
				catch (Exception ex4)
				{
					ModBase.Log(ex4, "添加任务管理卡片失败", ModBase.LogLevel.Feedback, null, Lang.Text("Speed.Error.OperationFailed"));
					return;
				}
			}
		}
		catch (Exception ex5)
		{
			ModBase.Log(ex5, "刷新任务管理显示失败", ModBase.LogLevel.Feedback, null, Lang.Text("Speed.Error.OperationFailed"));
		}
	}

	public void TaskRemove(ModLoader.LoaderBase loader)
	{
		if (rightCards.ContainsKey(loader.name))
		{
			ModBase.RunInUiWait(() =>
			{
				Grid element = rightCards[loader.name];
				ModMain.frmSpeedRight.PanMain.Children.Remove(element);
				rightCards.Remove(loader.name);
				ModBase.Log("[Watcher] 移除任务管理卡片：" + loader.name);
			});
		}
	}

	private void TryReturnToHome()
	{
		if (ModMain.frmSpeedRight.PanMain.Children.Count == 0 && ModMain.frmMain.pageCurrent == FormMain.PageType.TaskManager)
		{
			ModMain.frmMain.PageBack();
		}
	}

}
