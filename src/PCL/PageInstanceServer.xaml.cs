using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using FluentValidation;
using PCL.Core.App.Localization;
using PCL.Core.Link.McPing;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Minecraft;
using PCL.Core.Utils.Validate;
using fNbt;

namespace PCL;

public partial class PageInstanceServer : MyPageRight, IComponentConnector
{
	private const int debounceInterval = 2000;

	public static readonly List<MinecraftServerInfo> serverList = new List<MinecraftServerInfo>();

	private static readonly List<ServerCard> serverCardList = new List<ServerCard>();

	private CancellationTokenSource _cts;

	private DateTime _lastRefresh = DateTime.MinValue;

	public PageInstanceServer()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected Obj, but got Unknown
		InitializeComponent();
		Loaded += PageLoaded;
		IsVisibleChanged += PageInstanceServer_IsVisibleChanged;
	}

	private async void PageLoaded(object e, RoutedEventArgs sender)
	{
		serverList.Clear();
		serverCardList.Clear();
		PanServers.Children.Clear();
		await LoadServersFromFileAsync();
		RefreshTip();
		foreach (MinecraftServerInfo server in serverList)
		{
			ServerCard serverCard = new ServerCard();
			serverCard.RemoveServer += RemoveServerEvent;
			serverCard.EditServer += (object? a, EventArgs b) =>
			{
				EditServer(a, (ServerCard.ResultEventArgs)b);
			};
			serverCard.UpdateServerInfo(server);
			serverCardList.Add(serverCard);
			PanServers.Children.Add(serverCard);
		}
		PingAllServers();
	}

	private void PageInstanceServer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!IsVisible && _cts != null)
		{
			_cts.Cancel();
			_cts.Dispose();
			_cts = null;
		}
	}

	private async void RemoveServerEvent(object sender, EventArgs e)
	{
		int index = PanServers.Children.IndexOf((UIElement)sender);
		if (index < 0)
		{
			HintService.Hint(Lang.Text("Instance.Server.IndexNotFound"), HintType.Error);
			return;
		}
		NbtList val = await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat"), "servers", default(CancellationToken));
		if (val == null)
		{
			HintService.Hint(Lang.Text("Instance.Server.ReadDataFailed"), HintType.Error);
			return;
		}
		val.RemoveAt(index);
		if (!(await NbtFileHandler.WriteTagInNbtFileAsync((NbtTag)(NbtList)((NbtTag)val).Clone(), Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat"), (NbtCompression)1, default(CancellationToken))))
		{
			HintService.Hint(Lang.Text("Instance.Server.WriteDataFailed"), HintType.Error);
			return;
		}
		serverList.RemoveAt(index);
		serverCardList.Remove((ServerCard)sender);
		if (serverList.Count == 0)
		{
			RefreshTip();
		}
		PanServers.Children.Remove((UIElement)sender);
		HintService.Hint(Lang.Text("Instance.Server.Removed"), HintType.Success);
	}

	private async void EditServer(object sender, ServerCard.ResultEventArgs e)
	{
		NbtList val = await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat"), "servers", default(CancellationToken));
		if (val == null)
		{
			HintService.Hint(Lang.Text("Instance.Server.ReadDataFailed"), HintType.Error);
			return;
		}
		int num = PanServers.Children.IndexOf((UIElement)sender);
		if (num < 0 || num >= val.Count)
		{
			HintService.Hint(Lang.Text("Instance.Server.IndexNotFound"), HintType.Error);
			return;
		}
		NbtTag val2 = ((NbtTag)val)[num];
		NbtTag obj = ((val2 is NbtCompound) ? val2 : null);
		obj["name"] = (NbtTag)new NbtString("name", e.Param1);
		obj["ip"] = (NbtTag)new NbtString("ip", e.Param2);
		if (!(await NbtFileHandler.WriteTagInNbtFileAsync((NbtTag)(NbtList)((NbtTag)val).Clone(), Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat"), (NbtCompression)1, default(CancellationToken))))
		{
			HintService.Hint(Lang.Text("Instance.Server.WriteDataFailed"), HintType.Error);
			return;
		}
		ServerCard obj2 = sender as ServerCard;
		obj2.server.Name = e.Param1;
		obj2.server.Address = e.Param2;
		await obj2.RefreshServerStatusAsync(withHint: true);
		HintService.Hint(Lang.Text("Instance.Server.Updated"), HintType.Success);
	}

	public async void RefreshServers()
	{
		ModBase.Log("刷新服务器列表");
		try
		{
			await LoadServersFromFileAsync();
			ModBase.RunInUi(() =>
			{
				UpdateServerUi();
			});
			PingAllServers();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			ModBase.Log(ex3, Lang.Text("Instance.Server.RefreshFailed"), ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Server.RefreshFailed"));
			ModBase.RunInUi(() =>
			{
				HintService.Hint(Lang.Text("Instance.Server.RefreshFailed.WithDetail", new object[1] { ex3.ToString() }), HintType.Error);
			});
		}
	}

	private void BtnRefresh_Click(object sender, MouseButtonEventArgs e)
	{
		if ((DateTime.Now - _lastRefresh).TotalMilliseconds < 2000.0)
		{
			HintService.Hint(Lang.Text("Instance.Server.NoFrequentRefresh"));
			return;
		}
		_lastRefresh = DateTime.Now;
		HintService.Hint(Lang.Text("Instance.Server.RefreshingList"));
		try
		{
			RefreshServers();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Server.RefreshFailed"), ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Server.RefreshFailed"));
			HintService.Hint(Lang.Text("Instance.Server.RefreshFailed.WithDetail", new object[1] { ex.ToString() }), HintType.Error);
		}
	}

	private async void BtnAddServer_Click(object sender, MouseButtonEventArgs e)
	{
		(string Name, string Address, bool Success) result = GetServerInfo(new MinecraftServerInfo
		{
			Name = Lang.Text("Instance.Server.DefaultName"),
			Address = ""
		});
		if (!result.Success)
		{
			return;
		}
		MinecraftServerInfo minecraftServerInfo = new MinecraftServerInfo
		{
			Name = result.Name,
			Address = result.Address,
			Status = ServerStatus.Unknown
		};
		serverList.Add(minecraftServerInfo);
		RefreshTip();
		ServerCard serverCard = new ServerCard();
		serverCard.RemoveServer += RemoveServerEvent;
		serverCard.EditServer += (object? a, EventArgs b) =>
		{
			EditServer(a, (ServerCard.ResultEventArgs)b);
		};
		serverCard.UpdateServerInfo(minecraftServerInfo);
		serverCardList.Add(serverCard);
		PanServers.Children.Add(serverCard);
		await serverCard.RefreshServerStatusAsync(withHint: false);
		string serversDatPath = Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat");
		NbtList val;
		if (!File.Exists(serversDatPath))
		{
			val = new NbtList("servers", (NbtTagType)10);
			RefreshTip();
		}
		else
		{
			val = await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(serversDatPath, "servers", default(CancellationToken));
		}
		if (val != null)
		{
			NbtCompound val2 = new NbtCompound();
			((NbtTag)val2)["name"] = (NbtTag)new NbtString("name", result.Name);
			((NbtTag)val2)["ip"] = (NbtTag)new NbtString("ip", result.Address);
			if (val.Count == 0)
			{
				val.ListType = (NbtTagType)10;
			}
			val.Add((NbtTag)(object)val2);
			await NbtFileHandler.WriteTagInNbtFileAsync((NbtTag)(NbtList)((NbtTag)val).Clone(), serversDatPath, (NbtCompression)1, default(CancellationToken));
		}
	}

	public static (string Name, string Address, bool Success) GetServerInfo(MinecraftServerInfo server)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected Obj, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected Obj, but got Unknown
		string text = ModMain.MyMsgBoxInput(Lang.Text("Instance.Server.EditTitle"), Lang.Text("Instance.Server.NamePrompt"), server.Name, new Collection<IValidator<string>> { (IValidator<string>)new NullOrWhiteSpaceValidator() });
		if (string.IsNullOrEmpty(text))
		{
			return (Name: string.Empty, Address: string.Empty, Success: false);
		}
		string text2 = ModMain.MyMsgBoxInput(Lang.Text("Instance.Server.EditTitle"), Lang.Text("Instance.Server.AddressPrompt"), server.Address, new Collection<IValidator<string>> { (IValidator<string>)new NullOrWhiteSpaceValidator() });
		if (string.IsNullOrEmpty(text2))
		{
			return (Name: string.Empty, Address: string.Empty, Success: false);
		}
		return (Name: text, Address: text2, Success: true);
	}

	private async Task LoadServersFromFileAsync()
	{
		serverList.Clear();
		string text = Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat");
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			ParseServersFromNBT(await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(text, "servers", default(CancellationToken)));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Server.ReadFileFailed"));
		}
	}

	private void ParseServersFromNBT(NbtList serversList)
	{
		if (serversList != null)
		{
			ModBase.Log($"Found {serversList.Count} servers:");
			int i = 0;
			for (int num = serversList.Count - 1; i <= num; i++)
			{
				NbtTag val = ((NbtTag)serversList)[i];
				NbtCompound val2 = (NbtCompound)(object)((val is NbtCompound) ? val : null);
				if (val2 != null)
				{
					NbtString val3 = val2.Get<NbtString>("ip");
					string text = ((val3 != null) ? val3.Value : null) ?? "Unknown";
					NbtString val4 = val2.Get<NbtString>("name");
					string text2 = ((val4 != null) ? val4.Value : null) ?? "Unknown";
					NbtString val5 = val2.Get<NbtString>("icon");
					string icon = ((val5 != null) ? val5.Value : null);
					ModBase.Log($"服务器 {i + 1}:");
					ModBase.Log("  名字: " + text2);
					ModBase.Log("  IP: " + text);
					serverList.Add(new MinecraftServerInfo
					{
						Name = text2,
						Address = text,
						Status = ServerStatus.Unknown,
						Icon = icon
					});
				}
			}
		}
		else
		{
			ModBase.Log("No 'servers' list found in servers.dat.");
		}
	}

	private void UpdateServerUi()
	{
		PanServers.Children.Clear();
		RefreshTip();
		foreach (MinecraftServerInfo server in serverList)
		{
			ServerCard serverCard = new ServerCard();
			serverCard.RemoveServer += RemoveServerEvent;
			serverCard.EditServer += (object? a, EventArgs b) =>
			{
				EditServer(a, (ServerCard.ResultEventArgs)b);
			};
			serverCard.UpdateServerInfo(server);
			serverCardList.Add(serverCard);
			PanServers.Children.Add(serverCard);
		}
	}

	private void RefreshTip()
	{
		if (serverList.Count == 0)
		{
			ModBase.Log(Lang.Text("Instance.Server.NoServersFound"));
			PanNoServer.Visibility = Visibility.Visible;
			PanContent.Visibility = Visibility.Collapsed;
			PanServers.Visibility = Visibility.Collapsed;
		}
		else
		{
			ModBase.Log(Lang.Text("Instance.Server.FoundServers"));
			PanNoServer.Visibility = Visibility.Collapsed;
			PanContent.Visibility = Visibility.Visible;
			PanServers.Visibility = Visibility.Visible;
		}
	}

	private async void PingAllServers()
	{
		if (_cts != null)
		{
			_cts.Cancel();
			_cts.Dispose();
		}
		_cts = new CancellationTokenSource();
		CancellationToken token = _cts.Token;
		SemaphoreSlim semaphore = new SemaphoreSlim(5);
		List<Task> tasks = new List<Task>();
		try
		{
			List<ServerCard> list = serverCardList.ToList();
			foreach (ServerCard item in list)
			{
				ServerCard currentServer = item;
				await semaphore.WaitAsync(token);
				tasks.Add(Task.Run(async () =>
				{
					try
					{
						await currentServer.RefreshServerStatusAsync(withHint: false, token);
					}
					catch (Exception ex3)
					{
						ModBase.Log(ex3, $"Ping 服务器失败: {currentServer}");
					}
					finally
					{
						semaphore.Release();
					}
				}, token));
			}
			await Task.WhenAll(tasks);
		}
		catch (OperationCanceledException)
		{
			ModBase.Log("PingAllServers 被取消", ModBase.LogLevel.Debug);
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "PingAllServers 失败");
		}
	}

	public static async Task<MinecraftServerInfo> PingServerAsync(MinecraftServerInfo server, CancellationToken token)
	{
		_ = 1;
		try
		{
			ServerAddressResolver.ResolvedServerAddress val = await ServerAddressResolver.GetResolvedServerAddressAsync(server.Address, token);
			IMcPingService query = McPingServiceFactory.CreateService(val.Host, val.Ip, val.Port);
			try
			{
				ModBase.Log("Pinging server: " + server.Address + ":" + val.Port);
				McPingResult val2 = await query.PingAsync(token);
				ModBase.Log("Ping result: " + ((val2 != null) ? "Success" : "Failed"));
				if (val2 != null)
				{
					server.Status = ServerStatus.Online;
					server.PlayerCount = val2.Players.Online;
					server.MaxPlayers = val2.Players.Max;
					server.Description = val2.Description ?? string.Empty;
					server.Version = val2.Version.Name;
					server.Ping = (int)val2.Latency;
					server.Icon = val2.Favicon;
				}
				else
				{
					server.Status = ServerStatus.Offline;
				}
			}
			finally
			{
				((IDisposable)query)?.Dispose();
			}
		}
		catch (OperationCanceledException)
		{
			server.Status = ServerStatus.Offline;
			ModBase.Log("Ping 服务器被取消: " + server.Address, ModBase.LogLevel.Debug);
		}
		catch (Exception ex2)
		{
			server.Status = ServerStatus.Offline;
			ModBase.Log(ex2, $"Ping 服务器失败: {server.Address}:{server.Port}");
		}
		return server;
	}

}
