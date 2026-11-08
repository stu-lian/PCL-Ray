using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.UI.Controls;
using PCL.Core.UI.Theme;

namespace PCL;

public partial class ServerCard : MyCard, IComponentConnector
{
	public class ResultEventArgs : EventArgs
	{
		public string Param1 { get; set; }

		public string Param2 { get; set; }

		public ResultEventArgs(string param1, string param2)
		{
			Param1 = param1;
			Param2 = param2;
		}
	}

	private readonly IconManager _manager;

	public MinecraftServerInfo server;


	public event EventHandler? RemoveServer;

	public event EventHandler? EditServer;

	public ServerCard()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected Obj, but got Unknown
		InitializeComponent();
		DataContext = (object)new IconManager();
		object dataContext = DataContext;
		_manager = (IconManager)((dataContext is IconManager) ? dataContext : null);
		_manager.AddIconFromXaml("signal_1", "<Viewbox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"20\" Height=\"20\"><Canvas UseLayoutRounding=\"False\" Width=\"1024.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1024.0,1024.0\"/></Canvas.Clip><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"234.666667\" Canvas.Top=\"610.56\" Width=\"80.853333\" Height=\"127.04\" Fill=\"#ff00ff21\"/></Canvas><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"353.066667\" Canvas.Top=\"541.226667\" Width=\"80.853333\" Height=\"196.373333\" Fill=\"#ff888888\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"471.445333\" Canvas.Top=\"460.373333\" Width=\"80.896\" Height=\"277.226667\" Fill=\"#ff888888\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"589.866667\" Canvas.Top=\"379.52\" Width=\"80.853333\" Height=\"358.08\" Fill=\"#ff888888\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"708.266667\" Canvas.Top=\"298.666667\" Width=\"80.853333\" Height=\"438.933333\" Fill=\"#ff888888\"/></Canvas></Canvas></Viewbox>");
		_manager.AddIconFromXaml("signal_2", "<Viewbox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"20\" Height=\"20\"><Canvas UseLayoutRounding=\"False\" Width=\"1024.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1024.0,1024.0\"/></Canvas.Clip><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"234.666667\" Canvas.Top=\"610.56\" Width=\"80.853333\" Height=\"127.04\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"353.066667\" Canvas.Top=\"541.226667\" Width=\"80.853333\" Height=\"196.373333\" Fill=\"#ff00ff21\"/></Canvas><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"471.445333\" Canvas.Top=\"460.373333\" Width=\"80.896\" Height=\"277.226667\" Fill=\"#ff888888\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"589.866667\" Canvas.Top=\"379.52\" Width=\"80.853333\" Height=\"358.08\" Fill=\"#ff888888\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"708.266667\" Canvas.Top=\"298.666667\" Width=\"80.853333\" Height=\"438.933333\" Fill=\"#ff888888\"/></Canvas></Canvas></Viewbox>");
		_manager.AddIconFromXaml("signal_3", "<Viewbox Width=\"20\" Height=\"20\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Canvas UseLayoutRounding=\"False\" Width=\"1024.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1024.0,1024.0\"/></Canvas.Clip><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"234.666667\" Canvas.Top=\"610.56\" Width=\"80.853333\" Height=\"127.04\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"353.066667\" Canvas.Top=\"541.226667\" Width=\"80.853333\" Height=\"196.373333\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"471.445333\" Canvas.Top=\"460.373333\" Width=\"80.896\" Height=\"277.226667\" Fill=\"#ff00ff21\"/></Canvas><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"589.866667\" Canvas.Top=\"379.52\" Width=\"80.853333\" Height=\"358.08\" Fill=\"#ff888888\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"708.266667\" Canvas.Top=\"298.666667\" Width=\"80.853333\" Height=\"438.933333\" Fill=\"#ff888888\"/></Canvas></Canvas></Viewbox>");
		_manager.AddIconFromXaml("signal_4", "<Viewbox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"20\" Height=\"20\"><Canvas UseLayoutRounding=\"False\" Width=\"1024.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1024.0,1024.0\"/></Canvas.Clip><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"234.666667\" Canvas.Top=\"610.56\" Width=\"80.853333\" Height=\"127.04\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"353.066667\" Canvas.Top=\"541.226667\" Width=\"80.853333\" Height=\"196.373333\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"471.445333\" Canvas.Top=\"460.373333\" Width=\"80.896\" Height=\"277.226667\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"589.866667\" Canvas.Top=\"379.52\" Width=\"80.853333\" Height=\"358.08\" Fill=\"#ff00ff21\"/></Canvas><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"708.266667\" Canvas.Top=\"298.666667\" Width=\"80.853333\" Height=\"438.933333\" Fill=\"#ff888888\"/></Canvas></Canvas></Viewbox>");
		_manager.AddIconFromXaml("signal_5", "<Viewbox Width=\"20\" Height=\"20\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Canvas UseLayoutRounding=\"False\" Width=\"1024.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1024.0,1024.0\"/></Canvas.Clip><Canvas UseLayoutRounding=\"False\"><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"234.666667\" Canvas.Top=\"610.56\" Width=\"80.853333\" Height=\"127.04\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"353.066667\" Canvas.Top=\"541.226667\" Width=\"80.853333\" Height=\"196.373333\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"471.445333\" Canvas.Top=\"460.373333\" Width=\"80.896\" Height=\"277.226667\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"589.866667\" Canvas.Top=\"379.52\" Width=\"80.853333\" Height=\"358.08\" Fill=\"#ff00ff21\"/><Rectangle RadiusX=\"0.0\" RadiusY=\"0.0\" Canvas.Left=\"708.266667\" Canvas.Top=\"298.666667\" Width=\"80.853333\" Height=\"438.933333\" Fill=\"#ff00ff21\"/></Canvas></Canvas></Viewbox>");
		_manager.AddIconFromXaml("signal_offline", "<Viewbox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"14\" Height=\"14\" Margin=\"3\"><Canvas UseLayoutRounding=\"False\" Width=\"1280.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1280.0,1024.0\"/></Canvas.Clip><Path Fill=\"#ff000000\"><Path.Data><PathGeometry Figures=\"M 317.63 349.235 l -67.951 -67.951 l -67.95 67.95 c -18.964 18.964 -48.988 18.964 -67.951 0 c -18.963 -18.962 -18.963 -48.987 0 -67.95 l 67.95 -67.95 l -66.37 -67.951 c -18.963 -18.963 -18.963 -48.988 0 -67.95 c 18.963 -18.964 48.988 -18.964 67.95 0 l 67.951 67.95 l 67.95 -67.95 c 18.964 -18.964 48.989 -18.964 67.951 0 c 18.963 18.962 18.963 48.987 0 67.95 l -67.95 67.95 l 67.95 67.951 c 18.963 18.963 18.963 48.988 0 67.95 c -9.481 9.482 -20.543 14.223 -33.185 14.223 c -14.222 0 -26.864 -6.321 -36.345 -14.222 z M 216.494 752.198 h -48.988 c -26.864 0 -48.987 26.864 -48.987 60.049 v 120.099 c 0 33.185 22.123 60.05 48.987 60.05 h 48.988 c 26.864 0 48.987 -26.865 48.987 -60.05 v -120.1 c 0 -33.184 -22.123 -60.048 -48.987 -60.048 z M 516.74 512 h -48.988 c -26.864 0 -48.988 26.864 -48.988 60.05 v 360.296 c 0 33.185 22.124 60.05 48.988 60.05 h 48.988 c 26.864 0 48.987 -26.865 48.987 -60.05 V 572.049 c 0 -33.185 -22.123 -60.049 -48.987 -60.049 z m 300.247 -240.198 H 768 c -26.864 0 -48.988 26.865 -48.988 60.05 v 600.494 c 0 33.185 22.124 60.05 48.988 60.05 h 48.988 c 26.864 0 48.987 -26.865 48.987 -60.05 V 331.852 c 0 -33.185 -22.123 -60.05 -48.987 -60.05 z m 300.247 -240.197 h -48.988 c -26.864 0 -48.988 26.864 -48.988 60.05 v 840.69 c 0 33.186 22.124 60.05 48.988 60.05 h 48.988 c 26.864 0 48.987 -26.864 48.987 -60.05 V 91.656 c -1.58 -33.186 -22.123 -60.05 -48.987 -60.05 z\" FillRule=\"Nonzero\"/></Path.Data></Path></Canvas></Viewbox>");
		_manager.AddIconFromXaml("loading", "<Viewbox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"20\" Height=\"20\"><Canvas UseLayoutRounding=\"False\" Width=\"1024.0\" Height=\"1024.0\"><Canvas.Clip><RectangleGeometry Rect=\"0.0,0.0,1024.0,1024.0\"/></Canvas.Clip><Path Fill=\"#ff000000\"><Path.Data><PathGeometry Figures=\"M 256 490.667 a 64 64 0 1 1 -128 0 a 64 64 0 0 1 128 0 z m -42.6667 0 a 21.3333 21.3333 0 1 0 -42.6667 0 a 21.3333 21.3333 0 0 0 42.6667 0 z m 384 0 a 106.667 106.667 0 1 1 -213.376 -0.042667 A 106.667 106.667 0 0 1 597.333 490.667 z m -42.6667 0 a 64 64 0 1 0 -128.043 0.042666 A 64 64 0 0 0 554.667 490.667 z m 298.667 0 a 64 64 0 1 1 -128 0 a 64 64 0 0 1 128 0 z m -42.6667 0 a 21.3333 21.3333 0 1 0 -42.6667 0 a 21.3333 21.3333 0 0 0 42.6667 0 z\" FillRule=\"Nonzero\"/></Path.Data></Path></Canvas></Viewbox>");
	}

	private void BtnSkin_Click(object sender, EventArgs eventArgs)
	{
		BtnSetting.ContextMenu.IsOpen = true;
	}

	public void UpdateServerInfo(MinecraftServerInfo serverInfo)
	{
		server = serverInfo;
		ModBase.RunInUi(() =>
		{
			UpdateServerUi();
		});
	}

	private async void UpdateServerUi()
	{
		if (server == null)
		{
			return;
		}
		ServerName.Text = server.Name;
		await ImageLoaderHelper.SetServerLogoAsync(server.Icon, ServerIcon, (string)null);
		if (server.Status == ServerStatus.Online)
		{
			_manager.SetSelectedIconByName(GetSignalIcon(server.Ping));
			Signal.ToolTip = $"{server.Ping}ms";
			ToolTipService.SetInitialShowDelay((DependencyObject)(object)Signal, 0);
			ToolTipService.SetBetweenShowDelay((DependencyObject)(object)Signal, 50);
			ToolTipService.SetPlacement((DependencyObject)(object)Signal, PlacementMode.Top);
			if (server.PlayerCount != 0 && server.MaxPlayers != 0)
			{
				ServerPlayer.Text = $"{server.PlayerCount} / {server.MaxPlayers}";
			}
			else
			{
				ServerPlayer.Text = "???";
			}
			ServerMotD.Visibility = Visibility.Collapsed;
			MotdRenderer.RenderMotd(server.Description, ThemeService.IsDarkMode, 2, 12.0, true);
			MotdRenderer.RenderCanvas();
		}
		else if (server.Status == ServerStatus.Pinging)
		{
			_manager.SetSelectedIconByName("loading");
			MotdRenderer.ClearCanvas();
			ServerPlayer.Text = Lang.Text("Instance.Server.Card.Connecting");
			ServerMotD.Text = Lang.Text("Instance.Server.Card.ConnectingDots");
			ServerMotD.Visibility = Visibility.Visible;
		}
		else if (server.Status == ServerStatus.Offline)
		{
			_manager.SetSelectedIconByName("signal_offline");
			MotdRenderer.ClearCanvas();
			ServerPlayer.Text = Lang.Text("Instance.Server.Card.Offline");
			ServerMotD.Text = Lang.Text("Instance.Server.Card.ServerOffline");
			ServerMotD.Visibility = Visibility.Visible;
		}
	}

	private string GetSignalIcon(int ping)
	{
		if (0 <= ping && ping <= 99)
		{
			return "signal_5";
		}
		if (100 <= ping && ping <= 299)
		{
			return "signal_4";
		}
		if (300 <= ping && ping <= 599)
		{
			return "signal_3";
		}
		if (600 <= ping && ping <= 999)
		{
			return "signal_2";
		}
		return "signal_1";
	}

	public async Task RefreshServerStatusAsync(bool withHint, CancellationToken token = default(CancellationToken))
	{
		if (withHint)
		{
			HintService.Hint(Lang.Text("Instance.Server.Card.RefreshingStatus", new object[1] { server.Name }));
		}
		server.Status = ServerStatus.Pinging;
		await ((DispatcherObject)this).Dispatcher.InvokeAsync((Action)(() =>
		{
			UpdateServerUi();
		}));
		UpdateServerInfo(await PageInstanceServer.PingServerAsync(server, token));
	}

	private void BtnConnect_Click(object sender, EventArgs e)
	{
		try
		{
			ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
			{
				ServerIp = server.Address,
				instance = PageInstanceLeft.McInstance
			});
			ModMain.frmMain.PageChange(new FormMain.PageStackData
			{
				page = FormMain.PageType.Launch
			});
			HintService.Hint(Lang.Text("Instance.Server.Card.ConnectingTo", new object[1] { server.Name }));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Server.Card.LaunchFailed"), ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Server.Card.LaunchFailed"));
			HintService.Hint(Lang.Text("Instance.Server.Card.LaunchFailedMsg", new object[1] { ex.Message }), HintType.Error);
		}
	}

	private void BtnCopy_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(server.Address);
			HintService.Hint(Lang.Text("Instance.Server.Card.AddressCopied", new object[1] { server.Address }), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Server.Card.CopyAddressFailed"));
			HintService.Hint(Lang.Text("Instance.Server.Card.CopyAddressFailed"), HintType.Error);
		}
	}

	private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
	{
		await Task.Run(async () =>
		{
			await RefreshServerStatusAsync(withHint: true);
		});
	}

	private void BtnEdit_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			(string, string, bool) serverInfo = PageInstanceServer.GetServerInfo(server);
			if (serverInfo.Item3)
			{
				EditServer?.Invoke(this, new ResultEventArgs(serverInfo.Item1, serverInfo.Item2));
			}
		}
		catch (Exception ex)
		{
			HintService.Hint(Lang.Text("Instance.Server.Card.EditFailed", new object[1] { ex.Message }), HintType.Error);
		}
	}

	private void BtnRemove_Click(object sender, RoutedEventArgs e)
	{
		if (ModMain.MyMsgBox(Lang.Text("Instance.Server.Card.RemoveConfirmMessage", new object[2] { server.Name, server.Address }), Lang.Text("Instance.Server.Card.RemoveConfirmTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) == 1)
		{
			RemoveServer?.Invoke(this, EventArgs.Empty);
		}
	}

}
