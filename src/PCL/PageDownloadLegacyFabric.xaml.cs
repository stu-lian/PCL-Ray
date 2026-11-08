using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadLegacyFabric : MyPageRight, IComponentConnector
{
	public PageDownloadLegacyFabric()
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
		PageLoaderInit(Load, PanLoad, CardVersions, CardTip, ModDownload.dlLegacyFabricListLoader, (ModLoader.LoaderBase _) =>
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
			JsonArray jsonArray = (JsonArray)ModDownload.dlLegacyFabricListLoader.output.Value["installer"];
			PanVersions.Children.Clear();
			foreach (JsonNode item in jsonArray)
			{
				PanVersions.Children.Add(ModDownloadLib.LegacyFabricDownloadListItem((JsonObject)item, (object a, MouseButtonEventArgs b) =>
				{
					LegacyFabric_Selected((MyListItem)a, b);
				}));
			}
			CardVersions.Title = Lang.Text("Download.Version.VersionListCount", new object[1] { jsonArray.Count });
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 LegacyFabric 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	private void LegacyFabric_Selected(MyListItem sender, EventArgs e)
	{
		ModDownloadLib.McDownloadLegacyFabricLoaderSave((JsonObject)sender.Tag);
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://legacyfabric.net/");
	}

}
