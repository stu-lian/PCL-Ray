using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadLabyMod : MyPageRight, IComponentConnector
{
	public PageDownloadLabyMod()
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
		PageLoaderInit(Load, PanLoad, CardVersions, CardTip, ModDownload.dlLabyModListLoader, (ModLoader.LoaderBase _) =>
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
			JsonObject value = ModDownload.dlLabyModListLoader.output.Value;
			if (value != null)
			{
				JsonObject jsonObject = new JsonObject();
				jsonObject.Add("channel", "production");
				jsonObject.Add("version", value["production"]["labyModVersion"].ToString());
				JsonObject jsonObject2 = new JsonObject();
				jsonObject2.Add("channel", "snapshot");
				jsonObject2.Add("version", value["snapshot"]["labyModVersion"].ToString());
				PanVersions.Children.Clear();
				PanVersions.Children.Add(ModDownloadLib.LabyModDownloadListItem(jsonObject, (object a, MouseButtonEventArgs b) =>
				{
					LabyMod_Production_Selected((MyListItem)a, b);
				}));
				PanVersions.Children.Add(ModDownloadLib.LabyModDownloadListItem(jsonObject2, (object a, MouseButtonEventArgs b) =>
				{
					LabyMod_Snapshot_Selected((MyListItem)a, b);
				}));
				CardVersions.Title = Lang.Text("Download.Version.VersionListCount", new object[1] { value.Count });
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 LabyMod 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	private void LabyMod_Production_Selected(MyListItem sender, EventArgs e)
	{
		ModDownloadLib.McDownloadLabyModProductionLoaderSave();
	}

	private void LabyMod_Snapshot_Selected(MyListItem sender, EventArgs e)
	{
		ModDownloadLib.McDownloadLabyModSnapshotLoaderSave();
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://labymod.net");
	}

}
