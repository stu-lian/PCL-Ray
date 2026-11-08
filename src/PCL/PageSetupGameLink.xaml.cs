using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupGameLink : MyPageRight, IComponentConnector
{
	private bool isLoaded;

	public PageSetupGameLink()
	{
		InitializeComponent();
		Loaded += PageSetupLink_Loaded;
	}

	private void PageSetupLink_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		if (!isLoaded)
		{
			isLoaded = true;
			Reload();
		}
	}

	public void Reload()
	{
		ModTerracotta.TerracottaState state = ModTerracotta.GetState();
		TextTerracottaVersion.Text = string.IsNullOrWhiteSpace(state.InstalledVersion)
			? Lang.Text("Setup.GameLink.Terracotta.NotInstalled")
			: Lang.Text("Setup.GameLink.Terracotta.InstalledVersion", state.InstalledVersion);
		TextTerracottaStatus.Text = state.HttpPort.HasValue
			? Lang.Text("Setup.GameLink.Terracotta.Running", state.HttpPort.Value)
			: Lang.Text("Setup.GameLink.Terracotta.NotRunning");
		TextTerracottaPath.Text = ModTerracotta.BinaryPath;
		BtnStopTerracotta.IsEnabled = state.HttpPort.HasValue;
	}

	public void Reset()
	{
		Reload();
		HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.ResetDone"), HintType.Success, log: false);
	}

	private async void BtnDownloadTerracotta_Click(object sender, MouseButtonEventArgs e)
	{
		BtnDownloadTerracotta.IsEnabled = false;
		try
		{
			TextTerracottaStatus.Text = Lang.Text("Setup.GameLink.Terracotta.Downloading");
			await ModTerracotta.DownloadAsync();
			HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.DownloadDone"), HintType.Success);
		}
		catch (Exception ex)
		{
			HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.DownloadFailed", ex.Message), HintType.Error);
		}
		finally
		{
			BtnDownloadTerracotta.IsEnabled = true;
			Reload();
		}
	}

	private async void BtnCheckTerracottaUpdate_Click(object sender, MouseButtonEventArgs e)
	{
		BtnCheckTerracottaUpdate.IsEnabled = false;
		try
		{
			string latest = await ModTerracotta.GetLatestVersionAsync();
			string? installed = ModTerracotta.ReadInstalledVersion();
			if (string.Equals(latest, installed, StringComparison.OrdinalIgnoreCase))
			{
				HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.UpToDate", latest), HintType.Success);
			}
			else
			{
				HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.UpdateAvailable", latest, installed ?? "-"), HintType.Info);
			}
		}
		catch (Exception ex)
		{
			HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.UpdateCheckFailed", ex.Message), HintType.Error);
		}
		finally
		{
			BtnCheckTerracottaUpdate.IsEnabled = true;
			Reload();
		}
	}

	private void BtnOpenTerracottaFolder_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			Directory.CreateDirectory(ModTerracotta.InstallDirectory);
			Process.Start(new ProcessStartInfo
			{
				FileName = ModTerracotta.InstallDirectory,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			HintService.Hint(ex.Message, HintType.Error);
		}
	}

	private async void BtnStopTerracotta_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			await ModTerracotta.StopAsync();
			HintService.Hint(Lang.Text("Setup.GameLink.Terracotta.Stopped"), HintType.Success);
		}
		catch (Exception ex)
		{
			HintService.Hint(ex.Message, HintType.Error);
		}
		Reload();
	}
}
