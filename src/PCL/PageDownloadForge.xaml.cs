using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadForge : MyPageRight, IComponentConnector
{
	public PageDownloadForge()
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
		PageLoaderInit(Load, PanLoad, PanMain, CardTip, ModDownload.dlForgeListLoader, (ModLoader.LoaderBase _) =>
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
			PanMain.Children.Clear();
			foreach (string item in ModBase.Sort(ModDownload.dlForgeListLoader.output.Value, McVersionComparer.CompareVersionGe))
			{
				MyCard myCard = new MyCard
				{
					Title = item.Replace("_p", " P"),
					Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
				};
				StackPanel stackPanel = new StackPanel
				{
					Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
					VerticalAlignment = VerticalAlignment.Top,
					RenderTransform = new TranslateTransform(0.0, 0.0),
					Tag = item
				};
				myCard.Children.Add(stackPanel);
				myCard.SwapControl = stackPanel;
				myCard.InstallMethod = (StackPanel stack) =>
				{
					MyLoading myLoading = new MyLoading
					{
						Text = Lang.Text("Download.Version.Forge.LoadingList"),
						Margin = new Thickness(5.0)
					};
					ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>> loaderTask = (ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)(myLoading.State = new ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>("DlForgeVersion Main", ModDownload.DlForgeVersionMain));
					loaderTask.Start(stack.Tag);
					myLoading.StateChanged += (object a, MyLoading.MyLoadingState b, MyLoading.MyLoadingState c) =>
					{
						ModMain.frmDownloadForge.Forge_StateChanged((MyLoading)a, b, c);
					};
					myLoading.Click += (object a, MouseButtonEventArgs b) =>
					{
						ModMain.frmDownloadForge.Forge_Click((MyLoading)a, b);
					};
					stack.Children.Add(myLoading);
				};
				myCard.IsSwapped = true;
				PanMain.Children.Add(myCard);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化 Forge 版本列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
		}
	}

	public void Forge_Click(MyLoading sender, MouseButtonEventArgs e)
	{
		if (sender.State.LoadingState == MyLoading.MyLoadingState.Error)
		{
			((ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)sender.State).Start(null, isForceRestart: true);
		}
	}

	public void Forge_StateChanged(MyLoading sender, MyLoading.MyLoadingState newState, MyLoading.MyLoadingState oldState)
	{
		if (newState != MyLoading.MyLoadingState.Stop)
		{
			return;
		}
		MyCard myCard = (MyCard)(object)((FrameworkElement)(object)sender.Parent).Parent;
		ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>> loaderTask = (ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)sender.State;
		((StackPanel)myCard.SwapControl).Children.Clear();
		((StackPanel)myCard.SwapControl).Tag = loaderTask.output;
		myCard.InstallMethod = (StackPanel stack) =>
		{
			stack.Tag = ((List<ModDownload.DlForgeVersionEntry>)stack.Tag).Sort((ModDownload.DlForgeVersionEntry a, ModDownload.DlForgeVersionEntry b) => a.version > b.version);
			ModDownloadLib.ForgeDownloadListItemPreload(stack, (List<ModDownload.DlForgeVersionEntry>)stack.Tag, ModDownloadLib.ForgeSave_Click, isSaveOnly: true);
			foreach (object item in (IEnumerable)stack.Tag)
			{
				stack.Children.Add(ModDownloadLib.ForgeDownloadListItem((ModDownload.DlForgeVersionEntry)item, ModDownloadLib.ForgeSave_Click, isSaveOnly: true));
			}
		};
		myCard.StackInstall();
	}

	private void BtnWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://files.minecraftforge.net");
	}

}
