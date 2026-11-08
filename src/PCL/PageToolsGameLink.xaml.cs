using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Link;
using PCL.Core.Link.McPing;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Logging;
using PCL.Core.Utils.Validate;
using PCL.Network;

namespace PCL;

public partial class PageToolsGameLink : MyPageRight, IComponentConnector
{
	private enum Subpages
	{
		PanSelect,
		PanFinish
	}

	private readonly ObservableCollection<FoundWorld> _discoveredWorlds = new ObservableCollection<FoundWorld>();

	private CancellationTokenSource? _discoveryCts;

	private bool _isActionRunning;

	private Subpages CurrentSubpage
	{
		get;
		set
		{
			if (field != value)
			{
				field = value;
				ApplySubpage();
			}
		}
	}

	public PageToolsGameLink()
	{
		InitializeComponent();
		Loaded += (_, _) => Reload();
		PageEnter += ApplySubpage;
		ModTerracotta.StateChanged += OnTerracottaStateChanged;
		_discoveredWorlds.CollectionChanged += OnDiscoveredWorldsChanged;
	}

	public void Reload()
	{
		ModTerracotta.TerracottaState state = ModTerracotta.GetState();
		if (state.Status != ModTerracotta.TerracottaStatus.Idle && state.Status != ModTerracotta.TerracottaStatus.Error)
		{
			CurrentSubpage = Subpages.PanFinish;
			UpdateFinishUi(state);
		}
		else
		{
			CurrentSubpage = Subpages.PanSelect;
			ShowStatus(state.Status == ModTerracotta.TerracottaStatus.Error ? state.ErrorMessage ?? Lang.Text("Tools.GameLink.Terracotta.Error") : Lang.Text("Tools.GameLink.Terracotta.Hint"), state.Status == ModTerracotta.TerracottaStatus.Error ? MyHint.Themes.Red : MyHint.Themes.Blue);
			TextTerracottaVersion.Text = string.IsNullOrWhiteSpace(state.InstalledVersion)
				? Lang.Text("Setup.GameLink.Terracotta.NotInstalled")
				: Lang.Text("Setup.GameLink.Terracotta.InstalledVersion", state.InstalledVersion);
			TextTerracottaStatus.Text = state.HttpPort.HasValue
				? Lang.Text("Setup.GameLink.Terracotta.Running", state.HttpPort.Value)
				: Lang.Text("Setup.GameLink.Terracotta.NotRunning");
			TextTerracottaPath.Text = ModTerracotta.BinaryPath;
			BtnStopTerracotta.IsEnabled = state.HttpPort.HasValue;
		}
		DiscoverWorlds();
	}

	private void ApplySubpage()
	{
		if (PanSelect == null || PanFinish == null)
		{
			return;
		}
		PanSelect.Visibility = CurrentSubpage == Subpages.PanSelect ? Visibility.Visible : Visibility.Collapsed;
		PanFinish.Visibility = CurrentSubpage == Subpages.PanFinish ? Visibility.Visible : Visibility.Collapsed;
	}

	private void ShowStatus(string text, MyHint.Themes theme)
	{
		ModBase.RunInUi(() =>
		{
			HintStatus.Visibility = Visibility.Visible;
			HintStatus.Theme = theme;
			HintStatus.Text = text;
		});
	}

	private void DiscoverWorlds()
	{
		_discoveryCts?.Cancel();
		_discoveryCts?.Dispose();
		_discoveryCts = new CancellationTokenSource();
		CancellationToken token = _discoveryCts.Token;
		_ = Task.Run(async () =>
		{
			HashSet<int> seen = new HashSet<int>();
			try
			{
				using BroadcastListener listener = new BroadcastListener();
				listener.OnReceive += (BroadcastRecord record, System.Net.IPEndPoint remote) =>
				{
					if (!seen.Add(record.Address.Port))
					{
						return;
					}
					_ = Task.Run(async () =>
					{
						try
						{
							using IMcPingService pinger = McPingServiceFactory.CreateService(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, record.Address.Port));
							using CancellationTokenSource pingCts = new CancellationTokenSource(2000);
							McPingResult result = await pinger.PingAsync(pingCts.Token);
							if (result == null)
							{
								return;
							}
							string name = Lang.Text("Link.Lobby.WorldNameFormat", result.Description, result.Version.Name, record.Address.Port);
							ModBase.RunInUi(() =>
							{
								if (!token.IsCancellationRequested)
								{
									_discoveredWorlds.Add(new FoundWorld(name, record.Address.Port));
								}
							});
						}
						catch
						{
						}
					}, token);
				};
				listener.Start();
				await Task.Delay(TimeSpan.FromSeconds(3), token);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				LogWrapper.Warn(ex, "[Terracotta] World discovery failed");
			}
		}, token);
	}

	private void OnDiscoveredWorldsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		ModBase.RunInUi(() =>
		{
			HashSet<int> existing = ComboWorldList.Items.OfType<MyComboBoxItem>().Select(item => (int)item.Tag).ToHashSet();
			foreach (FoundWorld world in _discoveredWorlds)
			{
				if (existing.Add(world.Port))
				{
					ComboWorldList.Items.Add(new MyComboBoxItem { Tag = world.Port, Content = world.Name });
				}
			}
			if (ComboWorldList.SelectedIndex < 0 && ComboWorldList.Items.Count > 0)
			{
				ComboWorldList.SelectedIndex = 0;
			}
			BtnCreate.IsEnabled = ComboWorldList.Items.Count > 0;
		});
	}

	private void OnTerracottaStateChanged()
	{
		ModTerracotta.TerracottaState state = ModTerracotta.GetState();
		ModBase.RunInUi(() =>
		{
			if (state.Status is ModTerracotta.TerracottaStatus.HostReady or ModTerracotta.TerracottaStatus.GuestReady)
			{
				CurrentSubpage = Subpages.PanFinish;
			}
			else if (state.Status is ModTerracotta.TerracottaStatus.HostScanning or ModTerracotta.TerracottaStatus.HostStarting or ModTerracotta.TerracottaStatus.GuestConnecting or ModTerracotta.TerracottaStatus.GuestStarting or ModTerracotta.TerracottaStatus.Waiting or ModTerracotta.TerracottaStatus.Starting or ModTerracotta.TerracottaStatus.Downloading)
			{
				CurrentSubpage = Subpages.PanFinish;
			}
			else if (state.Status is ModTerracotta.TerracottaStatus.Idle)
			{
				CurrentSubpage = Subpages.PanSelect;
			}
			UpdateFinishUi(state);
		});
	}

	private void UpdateFinishUi(ModTerracotta.TerracottaState state)
	{
		if (LabFinishQuality == null)
		{
			return;
		}
		LabFinishQuality.Text = GetStatusText(state.Status);
		LabConnectUserName.Text = GetPlayerName();
		LabConnectUserType.Text = state.Status is ModTerracotta.TerracottaStatus.GuestReady or ModTerracotta.TerracottaStatus.GuestStarting or ModTerracotta.TerracottaStatus.GuestConnecting ? Lang.Text("Tools.GameLink.Finish.Guest") : Lang.Text("Tools.GameLink.Finish.Host");
		LabFinishId.Text = state.RoomCode ?? Lang.Text("Tools.GameLink.Terracotta.RoomCodePending");
		LabFinishAddress.Text = state.ServerPort.HasValue ? $"127.0.0.1:{state.ServerPort}" : "-";
		CardPlayerList.Title = Lang.Text("Tools.GameLink.Member.ListCount", state.Players.Count);
		StackPlayerList.Children.Clear();
		foreach (ModTerracotta.TerracottaPlayer player in state.Players)
		{
			MyListItem item = new MyListItem
			{
				Title = player.Name,
				Info = player.Kind.Equals("HOST", StringComparison.OrdinalIgnoreCase) ? Lang.Text("Tools.GameLink.Player.Details", Lang.Text("Tools.GameLink.Player.Host"), player.Vendor) : player.Vendor,
				Type = MyListItem.CheckType.Clickable,
				Tag = player
			};
			item.Click += PlayerInfoClick;
			StackPlayerList.Children.Add(item);
		}
		if (state.Status == ModTerracotta.TerracottaStatus.Error || state.Status == ModTerracotta.TerracottaStatus.Fatal)
		{
			ShowStatus(state.ErrorMessage ?? Lang.Text("Tools.GameLink.Terracotta.Error"), MyHint.Themes.Red);
		}
		else
		{
			ShowStatus(Lang.Text("Tools.GameLink.Terracotta.Hint"), MyHint.Themes.Blue);
		}
	}

	private static string GetStatusText(ModTerracotta.TerracottaStatus status)
	{
		return status switch
		{
			ModTerracotta.TerracottaStatus.Downloading => Lang.Text("Tools.GameLink.Terracotta.Status.Downloading"),
			ModTerracotta.TerracottaStatus.Starting => Lang.Text("Tools.GameLink.Terracotta.Status.Starting"),
			ModTerracotta.TerracottaStatus.Waiting => Lang.Text("Tools.GameLink.Terracotta.Status.Waiting"),
			ModTerracotta.TerracottaStatus.HostScanning => Lang.Text("Tools.GameLink.Terracotta.Status.HostScanning"),
			ModTerracotta.TerracottaStatus.HostStarting => Lang.Text("Tools.GameLink.Terracotta.Status.HostStarting"),
			ModTerracotta.TerracottaStatus.HostReady => Lang.Text("Tools.GameLink.Terracotta.Status.HostReady"),
			ModTerracotta.TerracottaStatus.GuestConnecting => Lang.Text("Tools.GameLink.Terracotta.Status.GuestConnecting"),
			ModTerracotta.TerracottaStatus.GuestStarting => Lang.Text("Tools.GameLink.Terracotta.Status.GuestStarting"),
			ModTerracotta.TerracottaStatus.GuestReady => Lang.Text("Tools.GameLink.Terracotta.Status.GuestReady"),
			ModTerracotta.TerracottaStatus.Error => Lang.Text("Tools.GameLink.Terracotta.Status.Error"),
			ModTerracotta.TerracottaStatus.Fatal => Lang.Text("Tools.GameLink.Terracotta.Status.Fatal"),
			_ => Lang.Text("Tools.GameLink.Terracotta.Status.Idle")
		};
	}

	private static string GetPlayerName()
	{
		string name = ProfileService.Current?.UserName ?? string.Empty;
		return string.IsNullOrWhiteSpace(name) ? "Anonymous" : name;
	}

	private void PlayerInfoClick(object sender, MouseButtonEventArgs e)
	{
		ModTerracotta.TerracottaPlayer player = (ModTerracotta.TerracottaPlayer)((MyListItem)sender).Tag;
		ModMain.MyMsgBox(Lang.Text("Tools.GameLink.Player.InfoMessage", player.Name, player.Vendor), Lang.Text("Tools.GameLink.Player.InfoTitle", player.Name));
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

	private async void BtnCreate_Click(object sender, MouseButtonEventArgs e)
	{
		if (ComboWorldList.SelectedItem == null)
		{
			HintService.Hint(Lang.Text("Tools.GameLink.Create.NoWorld"));
			return;
		}
		await RunActionAsync(async () =>
		{
			CurrentSubpage = Subpages.PanFinish;
			await ModTerracotta.HostAsync(GetPlayerName());
		});
	}

	private async void BtnJoin_Click(object sender, MouseButtonEventArgs e)
	{
		string code = ModTerracotta.NormalizeRoomCode(TextJoinLobbyId.Text);
		if (!ModTerracotta.IsValidRoomCode(code))
		{
			HintService.Hint(Lang.Text("Tools.GameLink.Join.InvalidText"), HintType.Error);
			return;
		}
		await RunActionAsync(async () =>
		{
			CurrentSubpage = Subpages.PanFinish;
			await ModTerracotta.JoinAsync(code, GetPlayerName());
		});
	}

	private async Task RunActionAsync(Func<Task> action)
	{
		if (_isActionRunning)
		{
			return;
		}
		_isActionRunning = true;
		try
		{
			await action();
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "[Terracotta] Multiplayer action failed");
			ShowStatus(ex.Message, MyHint.Themes.Red);
			HintService.Hint(ex.Message, HintType.Error);
		}
		finally
		{
			_isActionRunning = false;
		}
	}

	private async void BtnInputPort_Click(object sender, MouseButtonEventArgs e)
	{
		BtnInputPort.IsEnabled = false;
		try
		{
			string input = ModMain.MyMsgBoxInput(Lang.Text("Tools.GameLink.Create.EnterPort"), "", "", new Collection<IValidator<string>> { new IntValidator(65535, 1024) });
			if (!int.TryParse(input, out int port))
			{
				return;
			}
			using IMcPingService ping = McPingServiceFactory.CreateService("127.0.0.1", port, 5000);
			McPingResult result = await ping.PingAsync(CancellationToken.None);
			if (result == null || result.Version.Protocol == 0)
			{
				HintService.Hint(Lang.Text("Tools.GameLink.Create.NotMcPort"), HintType.Error);
				return;
			}
			MyComboBoxItem existing = ComboWorldList.Items.OfType<MyComboBoxItem>().FirstOrDefault(item => (int)item.Tag == port);
			if (existing == null)
			{
				existing = new MyComboBoxItem { Tag = port, Content = $"127.0.0.1:{port}" };
				ComboWorldList.Items.Add(existing);
			}
			ComboWorldList.SelectedItem = existing;
		}
		catch (Exception ex)
		{
			HintService.Hint(ex.Message, HintType.Error);
		}
		finally
		{
			BtnInputPort.IsEnabled = true;
		}
	}

	private void BtnRefresh_Click(object sender, MouseButtonEventArgs e)
	{
		_discoveredWorlds.Clear();
		ComboWorldList.Items.Clear();
		DiscoverWorlds();
	}

	private void PasteLobbyId(object sender, MouseButtonEventArgs e)
	{
		try
		{
			string text = Clipboard.GetText(TextDataFormat.Text);
			if (!string.IsNullOrWhiteSpace(text))
			{
				TextJoinLobbyId.Text = text;
			}
			else
			{
				HintService.Hint(Lang.Text("Tools.GameLink.Join.InvalidText"));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "从剪贴板识别大厅编号出错");
		}
	}

	private void ClearLobbyId(object sender, MouseButtonEventArgs e)
	{
		TextJoinLobbyId.Text = string.Empty;
	}

	private void TextJoinLobbyId_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			BtnJoin_Click(sender, null);
		}
	}

	private async void BtnFinishExit_Click(object sender, ModBase.RouteEventArgs routeEventArgs)
	{
		if (ModMain.MyMsgBox(Lang.Text("Tools.GameLink.Terracotta.StopConfirm"), Lang.Text("Tools.GameLink.Exit.ConfirmTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
		{
			return;
		}
		await ModTerracotta.StopAsync();
		CurrentSubpage = Subpages.PanSelect;
	}

	private void BtnFinishCopy_Click(object sender, ModBase.RouteEventArgs routeEventArgs)
	{
		if (!string.IsNullOrWhiteSpace(LabFinishId.Text))
		{
			ModBase.ClipboardSet(LabFinishId.Text);
			HintService.Hint(Lang.Text("Tools.GameLink.Terracotta.Copied"));
		}
	}

	private void BtnFinishCopyIp_Click(object sender, ModBase.RouteEventArgs routeEventArgs)
	{
		if (LabFinishAddress.Text != "-")
		{
			ModBase.ClipboardSet(LabFinishAddress.Text);
			HintService.Hint(Lang.Text("Tools.GameLink.Terracotta.Copied"));
		}
	}
}
