using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.App.Localization;
using PCL.Core.Link.McPing;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Minecraft;
using PCL.Core.UI;
using PCL.Core.UI.Controls;

namespace PCL;

public partial class MinecraftServer : Grid, IComponentConnector
{
	private const string fallbackImageUri = "pack://application:,,,/Plain Craft Launcher 2;component/Images/Icons/DefaultServer.png";

	private static readonly DependencyProperty AddressProperty = DependencyProperty.Register("Address", typeof(string), typeof(MinecraftServer), new PropertyMetadata((object)string.Empty, (PropertyChangedCallback)OnAddressChanged));


	public string Address
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(AddressProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(AddressProperty, (object)value);
		}
	}

	public MinecraftServer()
	{
		InitializeComponent();
	}

	private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MinecraftServer server = (MinecraftServer)(object)d;
		((DispatcherObject)d).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(() => server.UpdateServerInfoAsync(e.NewValue?.ToString())), Array.Empty<object>());
	}

	public async Task UpdateServerInfoAsync(string address)
	{
		if (address == null)
		{
			return;
		}
		address = address.Replace("：", ":");
		LabServerDesc.Foreground = Brushes.White;
		LabServerDesc.Text = Lang.Text("Tools.ServerQuery.State.Querying");
		LabServerPlayer.Text = "-/-";
		LabServerPlayer.ToolTip = null;
		LabServerLatency.Text = string.Empty;
		ImageLoaderHelper.SetFallbackImage(ImgServerLogo, "pack://application:,,,/Plain Craft Launcher 2;component/Images/Icons/DefaultServer.png");
		try
		{
			ServerAddressResolver.ResolvedServerAddress val = await ServerAddressResolver.GetResolvedServerAddressAsync(address, default(CancellationToken));
			IMcPingService query = McPingServiceFactory.CreateService(val.Host, val.Ip, val.Port);
			try
			{
				McPingResult ret = await query.PingAsync(default(CancellationToken));
				if (ret == null)
				{
					throw new Exception(Lang.Text("Tools.ServerQuery.State.NoInfo"));
				}
				await ImageLoaderHelper.SetServerLogoAsync(ret.Favicon, ImgServerLogo, (string)null);
				UpdateServerStatus(ret);
			}
			finally
			{
				((IDisposable)query)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[MinecraftServer] 信息查询失败");
			LabServerDesc.Text = Lang.Text("Tools.ServerQuery.Error.UnableToConnect", new object[1] { ex.Message });
			LabServerDesc.Foreground = Brushes.Red;
			ImageLoaderHelper.SetFallbackImage(ImgServerLogo, "pack://application:,,,/Plain Craft Launcher 2;component/Images/Icons/DefaultServer.png");
		}
	}

	private void UpdateServerStatus(McPingResult ret)
	{
		string value;
		if (ret.Latency < 150)
		{
			value = "a";
		}
		else
		{
			value = ((ret.Latency < 400) ? "6" : "c");
		}
		LabServerDesc.Text = Lang.Text("Tools.ServerQuery.Title.MinecraftServer");
		MotdRenderer.RenderMotd(ret.Description, false, 2, 14.0, true);
		MotdRenderer.RenderCanvas();
		LabServerPlayer.Text = $"{ret.Players.Online}/{ret.Players.Max}";
		ModStyle.MinecraftFormatter.SetColorfulTextLab($"§{value}{ret.Latency}ms", LabServerLatency, isDarkMode: false);
		if (ret.Players.Samples != null && ret.Players.Samples.Any())
		{
			LabServerPlayer.ToolTip = string.Join("\r\n", ret.Players.Samples.Select((McPingPlayerSampleResult x) => x.Name));
			ToolTipService.SetPlacement((DependencyObject)(object)LabServerPlayer, PlacementMode.Mouse);
		}
	}

	static MinecraftServer()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected Obj, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
	}
}
