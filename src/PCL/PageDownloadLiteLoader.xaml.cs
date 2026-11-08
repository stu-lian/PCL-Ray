using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadLiteLoader : MyPageRight, IComponentConnector
{
	public PageDownloadLiteLoader()
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
		BtnWeb.Click += BtnWeb_Click;
	}

	private void LoaderInit()
	{
		PageLoaderInit(Load, PanLoad, PanMain, CardTip, ModDownload.dlLiteLoaderListLoader, (ModLoader.LoaderBase _) =>
		{
			Load_OnFinish();
		});
	}

	private void Init()
	{
		PanBack.ScrollToHome();
	}

	private void Load_OnFinish()
	{
		try
		{
			Dictionary<string, List<ModDownload.DlLiteLoaderListEntry>> dictionary = new Dictionary<string, List<ModDownload.DlLiteLoaderListEntry>>();
			for (int num = 30; num >= 0; num--)
			{
				dictionary.Add("1." + num, new List<ModDownload.DlLiteLoaderListEntry>());
			}
			dictionary.Add("UnknownVersion", new List<ModDownload.DlLiteLoaderListEntry>());
			foreach (ModDownload.DlLiteLoaderListEntry item in ModDownload.dlLiteLoaderListLoader.output.Value)
			{
				string key = "1." + item.Inherit.Split(".")[1];
				if (dictionary.ContainsKey(key))
				{
					dictionary[key].Add(item);
				}
				else
				{
					dictionary["UnknownVersion"].Add(item);
				}
			}
			PanMain.Children.Clear();
			foreach (KeyValuePair<string, List<ModDownload.DlLiteLoaderListEntry>> item2 in dictionary)
			{
				if (!item2.Value.Any())
				{
					continue;
				}
				MyCard myCard = new MyCard
				{
					Title = ((item2.Key == "UnknownVersion") ? Lang.Text("Minecraft.Version.Unknown") : item2.Key) + " (" + item2.Value.Count + ")",
					Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
				};
				StackPanel stackPanel = new StackPanel
				{
					Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
					VerticalAlignment = VerticalAlignment.Top,
					RenderTransform = new TranslateTransform(0.0, 0.0),
					Tag = item2.Value
				};
				myCard.Children.Add(stackPanel);
				myCard.SwapControl = stackPanel;
				myCard.IsSwapped = true;
				myCard.InstallMethod = (StackPanel stack) =>
				{
					stack.Tag = ((List<ModDownload.DlLiteLoaderListEntry>)stack.Tag).Sort((ModDownload.DlLiteLoaderListEntry a, ModDownload.DlLiteLoaderListEntry b) => McVersionComparer.CompareVersion(a.Inherit, b.Inherit) == 1);
					foreach (object item3 in (IEnumerable)stack.Tag)
					{
						stack.Children.Add(ModDownloadLib.LiteLoaderDownloadListItem((ModDownload.DlLiteLoaderListEntry)item3, ModDownloadLib.LiteLoaderSave_Click, isSaveOnly: true));
					}
				};
				PanMain.Children.Add(myCard);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 LiteLoader 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	public void DownloadStart(MyListItem sender, object e)
	{
		ModDownloadLib.McDownloadLiteLoader((ModDownload.DlLiteLoaderListEntry)sender.Tag);
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://www.liteloader.com");
	}

}
