using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadFabric : MyPageRight, IComponentConnector
{
	public PageDownloadFabric()
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
		PageLoaderInit(Load, PanLoad, CardVersions, CardTip, ModDownload.dlFabricListLoader, (ModLoader.LoaderBase _) =>
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
			JsonArray jsonArray = (JsonArray)ModDownload.dlFabricListLoader.output.Value["installer"];
			PanVersions.Children.Clear();
			foreach (JsonNode item in jsonArray)
			{
				PanVersions.Children.Add(ModDownloadLib.FabricDownloadListItem((JsonObject)item, (object sender, MouseButtonEventArgs e) =>
				{
					Fabric_Selected((MyListItem)sender, e);
				}));
			}
			CardVersions.Title = Lang.Text("Download.Version.VersionListCount", new object[1] { jsonArray.Count });
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Fabric 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	private void Fabric_Selected(MyListItem sender, EventArgs e)
	{
		ModDownloadLib.McDownloadFabricLoaderSave((JsonObject)sender.Tag);
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://www.fabricmc.net");
	}

}
