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

public partial class PageDownloadCleanroom : MyPageRight, IComponentConnector
{
	public PageDownloadCleanroom()
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
		PageLoaderInit(Load, PanLoad, PanMain, CardTip, ModDownload.dlCleanroomListLoader, (ModLoader.LoaderBase _) =>
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
			Dictionary<string, List<ModDownload.DlCleanroomListEntry>> dictionary = (from d in ModDownload.dlCleanroomListLoader.output.Value
				group d by d.Inherit into g
				orderby g.Key descending
				select g).ToDictionary((IGrouping<string, ModDownload.DlCleanroomListEntry> g) => g.Key, (IGrouping<string, ModDownload.DlCleanroomListEntry> g) => g.ToList());
			PanMain.Children.Clear();
			foreach (KeyValuePair<string, List<ModDownload.DlCleanroomListEntry>> item in dictionary)
			{
				if (!item.Value.Any())
				{
					continue;
				}
				MyCard myCard = new MyCard
				{
					Title = item.Key + " (" + item.Value.Count + ")",
					Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
				};
				StackPanel stackPanel = new StackPanel
				{
					Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
					VerticalAlignment = VerticalAlignment.Top,
					RenderTransform = new TranslateTransform(0.0, 0.0),
					Tag = item.Value
				};
				myCard.Children.Add(stackPanel);
				myCard.SwapControl = stackPanel;
				myCard.IsSwapped = true;
				myCard.InstallMethod = (StackPanel stack) =>
				{
					foreach (object item2 in (IEnumerable)stack.Tag)
					{
						stack.Children.Add(ModDownloadLib.CleanroomDownloadListItem((ModDownload.DlCleanroomListEntry)item2, ModDownloadLib.CleanroomSave_Click, isSaveOnly: true));
					}
				};
				PanMain.Children.Add(myCard);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Cleanroom 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://cleanroommc.com/zh/");
	}

}
