using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using fNbt;
using Microsoft.Web.WebView2.Core;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageToolsMapViewer : MyPageRight, IComponentConnector
{
	private const string HostName = "pclray.mapviewer";
	private const string AssetsDir = "MapViewerAssets";

	private static readonly (string Label, int Major, int Minor, int Patch)[] Versions =
	{
		("1.19 – 1.19.3", 1, 19, 0),
		("1.19.4 – 1.19.x", 1, 19, 4),
		("1.20 – 1.21.4", 1, 20, 0),
		("1.21.5 – 1.21.11", 1, 21, 5),
		("26.1 – 26.2", 26, 1, 0),
		("26.3", 26, 3, 0),
		("26.4+", 26, 4, 0),
	};

	private string _assetsPath = "";
	private int _navigationToken;

	public PageToolsMapViewer()
	{
		InitializeComponent();
		Loaded += PageToolsMapViewer_Loaded;
		Unloaded += PageToolsMapViewer_Unloaded;
	}

	private void PageToolsMapViewer_Loaded(object sender, RoutedEventArgs e)
	{
		LoadAssets();
		InitCombos();
		_ = NavigateAsync();
	}

	private void PageToolsMapViewer_Unloaded(object sender, RoutedEventArgs e)
	{
		Interlocked.Increment(ref _navigationToken);
	}

	private void LoadAssets()
	{
		try
		{
			string dir = Path.Combine(ModBase.pathTemp, "mapviewer");
			Directory.CreateDirectory(dir);
			ExtractAsset(dir, "index.html");
			ExtractAsset(dir, "worker.js");
			ExtractAsset(dir, "mcseedmap.wasm");
			ExtractAsset(dir, "comlink.mjs");
			ExtractAsset(dir, "btree192.dat");
			ExtractAsset(dir, "btree19.dat");
			ExtractAsset(dir, "btree20.dat");
			ExtractAsset(dir, "btree215.dat");
			ExtractAsset(dir, "btree262.dat");
			ExtractAsset(dir, "btree263.dat");
			ExtractAsset(dir, "btree264s3.dat");
			Directory.CreateDirectory(Path.Combine(dir, "structures"));
			ExtractStructureAssets(dir);
			_assetsPath = dir;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Tools.MapViewer.AssetsFailed"), ModBase.LogLevel.Feedback);
		}
	}

	private static void ExtractAsset(string dir, string name)
	{
		string target = Path.Combine(dir, name);
		// 始终覆盖:避免临时目录残留旧资源导致修复不生效
		using Stream? stream = ModBase.GetResourceStream(AssetsDir + "/" + name);
		if (stream == null)
		{
			throw new FileNotFoundException("资源缺失: " + name);
		}
		using var fs = new FileStream(target, FileMode.Create, FileAccess.Write);
		stream.CopyTo(fs);
	}

	private static readonly string[] StructureAssets =
	{
			"abandoned_camp.png",
			"amethyst_geode.webp",
			"ancient_city.webp",
			"bastion_remnant.webp",
			"buried_treasure.webp",
			"desert_pyramid.webp",
			"desert_well.webp",
			"end_city.webp",
			"end_city_no_ship.webp",
			"end_city_ship.webp",
			"end_gateway.webp",
			"igloo.webp",
			"jungle_temple.webp",
			"mineshaft.webp",
			"nether_fortress.webp",
			"nether_fossil.png",
			"ocean_monument.webp",
			"ocean_ruin.webp",
			"pillager_outpost.webp",
			"ruined_portal.webp",
			"shipwreck.webp",
			"slime_chunks.webp",
			"spawn_point.webp",
			"stronghold.webp",
			"swamp_hut.webp",
			"trail_ruin.webp",
			"trial_chambers.webp",
			"village.webp",
			"woodland_mansion.webp",
	};

	private static void ExtractStructureAssets(string dir)
	{
		string sub = Path.Combine(dir, "structures");
		foreach (string name in StructureAssets)
		{
			using Stream? stream = ModBase.GetResourceStream(AssetsDir + "/structures/" + name);
			if (stream == null)
			{
				continue;
			}
			using var fs = new FileStream(Path.Combine(sub, name), FileMode.Create, FileAccess.Write);
			stream.CopyTo(fs);
		}
	}

	private void InitCombos()
	{
		if (ComboDimension.Items.Count == 0)
		{
			ComboDimension.Items.Add(Lang.Text("Tools.MapViewer.Dimension.Overworld"));
			ComboDimension.Items.Add(Lang.Text("Tools.MapViewer.Dimension.Nether"));
			ComboDimension.Items.Add(Lang.Text("Tools.MapViewer.Dimension.End"));
		}
		if (ComboVersion.Items.Count == 0)
		{
			foreach (var v in Versions)
			{
				ComboVersion.Items.Add(v.Label);
			}
			ComboVersion.SelectedIndex = 2; // 默认 1.20
		}
		ComboDimension.SelectedIndex = 0;
	}

	private (int Major, int Minor, int Patch) SelectedVersion()
	{
		int i = ComboVersion.SelectedIndex;
		if (i < 0 || i >= Versions.Length)
		{
			i = 2;
		}
		var v = Versions[i];
		return (v.Major, v.Minor, v.Patch);
	}

	private int SelectedDimension()
	{
		int i = ComboDimension.SelectedIndex;
		if (i < 0 || i > 2)
		{
			i = 0;
		}
		return i switch
		{
			1 => -1, // 下界
			2 => 1,  // 末地
			_ => 0,  // 主世界
		};
	}

	private async Task NavigateAsync()
	{
		int token = Volatile.Read(ref _navigationToken);
		try
		{
			if (string.IsNullOrEmpty(_assetsPath))
			{
				LoadAssets();
			}
			if (string.IsNullOrEmpty(_assetsPath))
			{
				return;
			}
			string seed = TextSeed.Text?.Trim() ?? "";
			var ver = SelectedVersion();
			int dim = SelectedDimension();

			await WebMap.EnsureCoreWebView2Async(null);
			if (token != Volatile.Read(ref _navigationToken))
			{
				return;
			}
			var core = WebMap.CoreWebView2;
			try
			{
				core.SetVirtualHostNameToFolderMapping(HostName, _assetsPath,
					Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
			}
			catch
			{
				// 已存在映射,忽略
			}
			if (token != Volatile.Read(ref _navigationToken))
			{
				return;
			}
			string url = $"https://{HostName}/index.html?seed={Uri.EscapeDataString(seed)}&v={ver.Major}.{ver.Minor}.{ver.Patch}&d={dim}";
			if (token != Volatile.Read(ref _navigationToken))
			{
				return;
			}
			WebMap.Source = new Uri(url);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Tools.MapViewer.WebViewFailed"), ModBase.LogLevel.Feedback);
		}
	}

	private void BtnRandom_Click(object sender, MouseButtonEventArgs e)
	{
		var rnd = new Random();
		TextSeed.Text = rnd.NextInt64(1, long.MaxValue).ToString();
		_ = NavigateAsync();
	}

	private void ComboVersion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
	{
		if (IsLoaded)
		{
			_ = NavigateAsync();
		}
	}

	private void ComboDimension_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
	{
		if (IsLoaded)
		{
			_ = NavigateAsync();
		}
	}

	private void BtnRender_Click(object sender, MouseButtonEventArgs e)
	{
		_ = NavigateAsync();
	}
}