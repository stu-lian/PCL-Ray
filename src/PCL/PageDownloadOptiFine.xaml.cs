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

public partial class PageDownloadOptiFine : MyPageRight, IComponentConnector
{
	public PageDownloadOptiFine()
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
		PageLoaderInit(Load, PanLoad, PanMain, CardTip, ModDownload.dlOptiFineListLoader, (ModLoader.LoaderBase _) =>
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
			Dictionary<string, List<ModDownload.DlOptiFineListEntry>> dictionary = new Dictionary<string, List<ModDownload.DlOptiFineListEntry>>();
			dictionary.Add("Snapshot", new List<ModDownload.DlOptiFineListEntry>());
			for (int num = 50; num >= 0; num--)
			{
				dictionary.Add("1." + num, new List<ModDownload.DlOptiFineListEntry>());
			}
			foreach (ModDownload.DlOptiFineListEntry item in ModDownload.dlOptiFineListLoader.output.Value)
			{
				if (item.Inherit.StartsWith("1."))
				{
					string key = "1." + item.DisplayName.Split(".")[1].Split(" ")[0];
					if (dictionary.ContainsKey(key))
					{
						dictionary[key].Add(item);
					}
					else
					{
						dictionary["Snapshot"].Add(item);
					}
				}
				else
				{
					dictionary["Snapshot"].Add(item);
				}
			}
			PanMain.Children.Clear();
			foreach (KeyValuePair<string, List<ModDownload.DlOptiFineListEntry>> item2 in dictionary)
			{
				if (!item2.Value.Any())
				{
					continue;
				}
				string text = ((item2.Key == "Snapshot") ? Lang.Text("Download.Version.Optifine.Snapshot") : item2.Key);
				MyCard myCard = new MyCard
				{
					Title = text + " (" + item2.Value.Count + ")",
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
					stack.Tag = ((List<ModDownload.DlOptiFineListEntry>)stack.Tag).Sort((ModDownload.DlOptiFineListEntry a, ModDownload.DlOptiFineListEntry b) => McVersionComparer.CompareVersion(a.DisplayName, b.DisplayName) == 1);
					foreach (object item3 in (IEnumerable)stack.Tag)
					{
						stack.Children.Add(ModDownloadLib.OptiFineDownloadListItem((ModDownload.DlOptiFineListEntry)item3, ModDownloadLib.OptiFineSave_Click, isSaveOnly: true));
					}
				};
				PanMain.Children.Add(myCard);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 OptiFine 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://www.optifine.net/");
	}

}
