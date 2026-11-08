using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Http;

namespace PCL;

public partial class PageToolsHeadGenerator : MyPageRight, IComponentConnector
{
	private const string ApiBase = "https://minecraft-heads.com/scripts/api.php";
	private static string ApiKey => (Environment.GetEnvironmentVariable("PCL_MINECRAFT_HEADS_API_KEY") ?? "").Trim();

	private static readonly (string Cat, string Key)[] Categories =
	{
		("alphabet", "Tools.Head.Cat.Alphabet"),
		("animals", "Tools.Head.Cat.Animals"),
		("blocks", "Tools.Head.Cat.Blocks"),
		("decoration", "Tools.Head.Cat.Decoration"),
		("food-drinks", "Tools.Head.Cat.FoodDrinks"),
		("humans", "Tools.Head.Cat.Humans"),
		("humanoid", "Tools.Head.Cat.Humanoid"),
		("miscellaneous", "Tools.Head.Cat.Misc"),
		("monsters", "Tools.Head.Cat.Monsters"),
		("plants", "Tools.Head.Cat.Plants"),
	};

	private readonly List<(string Name, string Uuid, string Value)> _heads = new List<(string, string, string)>();

	private (string Name, string Uuid, string Value)? _selected;

	private bool _suppress;

	private bool _loading;

	private string _lastCategory = "";

	public PageToolsHeadGenerator()
	{
		InitializeComponent();
		Loaded += PageToolsHeadGenerator_Loaded;
	}

	private void PageToolsHeadGenerator_Loaded(object sender, RoutedEventArgs e)
	{
		if (_suppress)
		{
			return;
		}
		_suppress = true;
		foreach ((string cat, string key) in Categories)
		{
			ComboCategory.Items.Add(new ComboBoxItem { Content = Lang.Text(key), Tag = cat });
		}
		if (ComboCategory.Items.Count > 0)
		{
			ComboCategory.SelectedIndex = 0;
		}
		_suppress = false;
		// 之前 _suppress 挡住了 SelectionChanged，导致首次进入时从不加载数据、列表一直是空的
		if (ComboCategory.SelectedItem is ComboBoxItem first && first.Tag is string firstCat)
		{
			_ = LoadCategory(firstCat);
		}
	}

	private async void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_suppress || _loading || ComboCategory.SelectedItem is not ComboBoxItem item || item.Tag is not string cat)
		{
			return;
		}
		await LoadCategory(cat);
	}

	private async Task LoadCategory(string category)
	{
		_loading = true;
		BtnCopyCommand.IsEnabled = false;
		BtnCopyValue.IsEnabled = false;
		ListHeads.Items.Clear();
		LabLoadedCount.Text = Lang.Text("Common.State.Loading");
		if (string.IsNullOrEmpty(ApiKey))
		{
			LabLoadedCount.Text = Lang.Text("Tools.Head.Failed");
			_loading = false;
			return;
		}
		try
		{
			string url = ApiBase + "?apikey=" + Uri.EscapeDataString(ApiKey) + "&cat=" + Uri.EscapeDataString(category);
			using (HttpResponseMessage response = await HttpSenderExtension.SendAsync(HttpRequest.Create(url), null, true, true, HttpCompletionOption.ResponseContentRead, 6, CancellationToken.None))
			{
				string json = await response.AsStringAsync(CancellationToken.None);
				if (!response.IsSuccess)
				{
					throw new Exception(Lang.Text("Tools.Head.Failed"));
				}
				JsonArray? array = ModBase.GetJson(json)?.AsArray();
				_heads.Clear();
				if (array != null)
				{
					foreach (JsonNode? node in array)
					{
						JsonObject? obj = node?.AsObject();
						if (obj == null)
						{
							continue;
						}
						string name = obj["name"]?.GetValue<string>() ?? "";
						string uuid = obj["uuid"]?.GetValue<string>() ?? "";
						string value = obj["value"]?.GetValue<string>() ?? "";
						if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
						{
							_heads.Add((name, uuid, value));
						}
					}
				}
				_lastCategory = category;
				RenderHeads();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Tools.Head.Failed"), ModBase.LogLevel.Feedback, null, Lang.Text("Tools.Head.Failed"));
		}
		finally
		{
			_loading = false;
		}
	}

	private void RenderHeads()
	{
		ListHeads.Items.Clear();
		int shown = 0;
		string filter = TextSearch.Text?.Trim().ToLowerInvariant() ?? "";
		// 同一名字出现多次时，用序号区分，避免"看起来全是重复"
		Dictionary<string, int> nameIndex = new Dictionary<string, int>();
		Dictionary<string, int> nameTotal = new Dictionary<string, int>();
		foreach ((string name, _, _) in _heads)
		{
			string lower = name.ToLowerInvariant();
			nameTotal[lower] = nameTotal.TryGetValue(lower, out int count) ? count + 1 : 1;
		}
		foreach ((string name, string uuid, string value) in _heads)
		{
			string lower = name.ToLowerInvariant();
			if (!string.IsNullOrEmpty(filter) && !lower.Contains(filter))
			{
				continue;
			}
			Border border = new Border
			{
				Width = 66,
				Height = 66,
				Margin = new Thickness(3),
				CornerRadius = new CornerRadius(4),
				Background = new SolidColorBrush(Color.FromRgb(45, 50, 58)),
				Tag = (name, uuid, value)
			};
			StackPanel panel = new StackPanel
			{
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			Image image = new Image
			{
				Width = 44,
				Height = 44,
				Stretch = Stretch.Uniform,
				StretchDirection = StretchDirection.Both,
				Opacity = 0
			};
			panel.Children.Add(image);
			string displayName = name;
			if (nameTotal[lower] > 1)
			{
				nameIndex[lower] = nameIndex.TryGetValue(lower, out int index) ? index + 1 : 1;
				displayName = name + " #" + nameIndex[lower] + "/" + nameTotal[lower];
			}
			TextBlock label = new TextBlock
			{
				Text = displayName,
				FontSize = 10,
				Foreground = new SolidColorBrush(Color.FromRgb(200, 205, 215)),
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxWidth = 60,
				Margin = new Thickness(2, 2, 2, 0)
			};
			panel.Children.Add(label);
			border.Child = panel;
			border.MouseLeftButtonUp += Head_MouseLeftButtonUp;
			ListHeads.Items.Add(border);
			_ = FillHeadImageAsync(image, value);
			shown++;
			if (shown >= 500)
			{
				break;
			}
		}
		int total = _heads.Count;
		int uniqueNames = nameTotal.Count;
		LabLoadedCount.Text = Lang.Text("Tools.Head.Count").Replace("{0}", shown.ToString()).Replace("{1}", total.ToString()).Replace("{2}", uniqueNames.ToString());
		if (shown == 0)
		{
			LabLoadedCount.Text = Lang.Text("Tools.Head.NoResult");
		}
	}

	private static string GetTextureUrlFromValue(string value)
	{
		string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
		JsonObject? root = ModBase.GetJson(json)?.AsObject();
		string url = root?["textures"]?["SKIN"]?["url"]?.GetValue<string>() ?? "";
		return url.Replace("http://", "https://");
	}

	// 限制同时进行的下载，避免一次性发起上百个请求导致全部失败
	private static readonly System.Threading.SemaphoreSlim _downloadGate = new(6);

	private static async Task FillHeadImageAsync(Image image, string value)
	{
		BitmapSource? head = null;
		try
		{
			await _downloadGate.WaitAsync();
			try
			{
				head = await LoadHeadAsync(value);
			}
			finally
			{
				_downloadGate.Release();
			}
		}
		catch
		{
			head = null;
		}
		if (head == null)
		{
			return;
		}
		image.Source = head;
		image.Opacity = 1;
	}

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BitmapSource> _skinCache = new();

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Task<BitmapSource?>> _skinTasks = new();

	/// <summary>
	/// 异步下载皮肤并按 Minecraft 头部结构合成头像。
	/// 旧实现会在 UI 线程逐个同步下载皮肤，一屏数百个头像必然超时/卡死导致全部空白。
	/// </summary>
	private static Task<BitmapSource?> LoadHeadAsync(string value)
	{
		string url = GetTextureUrlFromValue(value);
		if (string.IsNullOrEmpty(url))
		{
			return Task.FromResult<BitmapSource?>(null);
		}
		if (_skinCache.TryGetValue(url, out BitmapSource? cached))
		{
			return Task.FromResult<BitmapSource?>(cached);
		}
		return _skinTasks.GetOrAdd(url, _ => LoadHeadCoreAsync(url));
	}

	private static async Task<BitmapSource?> LoadHeadCoreAsync(string url)
	{
		try
		{
			byte[] bytes;
			using (HttpResponseMessage response = await HttpSenderExtension.SendAsync(
				HttpRequest.Create(url), null, true, true, HttpCompletionOption.ResponseContentRead, 15, CancellationToken.None))
			{
				if (!response.IsSuccessStatusCode)
				{
					return null;
				}
				bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
			}
			if (bytes.Length == 0)
			{
				return null;
			}
			BitmapSource? head = null;
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
				if (decoder.Frames.Count > 0)
				{
					head = BuildHeadBitmap(decoder.Frames[0]);
				}
			}
			if (head != null)
			{
				_skinCache[url] = head;
			}
			return head;
		}
		catch
		{
			return null;
		}
		finally
		{
			_skinTasks.TryRemove(url, out _);
		}
	}

	/// <summary>取皮肤正面 8x8 区域，叠加帽子层，得到一个 8x8 的头像。</summary>
	private static BitmapSource? BuildHeadBitmap(BitmapSource skin)
	{
		int width = skin.PixelWidth;
		int height = skin.PixelHeight;
		// 64x64 / 128x128 -> unit = 1 / 2；老版 64x32 皮肤同样用 unit=1
		int unit = width / 64;
		if (unit < 1)
		{
			return null;
		}
		int faceX = 8 * unit;
		int faceY = 8 * unit;
		int hatX = 40 * unit;
		int hatY = 8 * unit;
		if (faceY + 8 * unit > height || hatX + 8 * unit > width)
		{
			// 少见的非标准皮肤，直接裁切可用区域
			hatX = faceX;
		}

		FormatConvertedBitmap bgra = new FormatConvertedBitmap(skin, PixelFormats.Bgra32, null, 0);
		int stride = bgra.PixelWidth * 4;
		byte[] pixels = new byte[stride * bgra.PixelHeight];
		bgra.CopyPixels(pixels, stride, 0);

		byte[] head = new byte[8 * 4 * 8];
		for (int y = 0; y < 8; y++)
		{
			for (int x = 0; x < 8; x++)
			{
				int dst = (y * 8 + x) * 4;
				int fx = faceX + x * unit;
				int fy = faceY + y * unit;
				int src = fy * stride + fx * 4;
				byte b = pixels[src];
				byte g = pixels[src + 1];
				byte r = pixels[src + 2];
				byte a = pixels[src + 3];

				int hx = hatX + x * unit;
				int hy = hatY + y * unit;
				if (hy * stride + hx * 4 + 3 < pixels.Length)
				{
					int hs = hy * stride + hx * 4;
					byte ha = pixels[hs + 3];
					if (ha > 0)
					{
						// 帽子层按 alpha 叠加在面部之上
						float alpha = ha / 255f;
						r = (byte)(pixels[hs + 2] * alpha + r * (1 - alpha));
						g = (byte)(pixels[hs + 1] * alpha + g * (1 - alpha));
						b = (byte)(pixels[hs] * alpha + b * (1 - alpha));
						a = (byte)Math.Max(a, ha);
					}
				}

				head[dst] = b;
				head[dst + 1] = g;
				head[dst + 2] = r;
				head[dst + 3] = a;
			}
		}

		BitmapSource bmp = BitmapSource.Create(8, 8, 96, 96, PixelFormats.Bgra32, null, head, 32);
		bmp.Freeze();
		return bmp;
	}

	private void Head_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (sender is Border border && border.Tag is ValueTuple<string, string, string> data)
		{
			Select(data);
		}
	}

	private void Select((string Name, string Uuid, string Value) data)
	{
		_selected = data;
		LabHeadName.Text = data.Name;
		LabHeadInfo.Text = data.Uuid;
		HeadPreviewImage.Source = null;
		_ = FillPreviewAsync(data.Value);
		BtnCopyCommand.IsEnabled = true;
		BtnCopyValue.IsEnabled = true;
	}

	private async Task FillPreviewAsync(string value)
	{
		BitmapSource? head = await LoadHeadAsync(value);
		if (head != null)
		{
			HeadPreviewImage.Source = head;
		}
	}

	private void TextSearch_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!_loading)
		{
			RenderHeads();
		}
	}

	private void BtnCopyCommand_Click(object sender, MouseButtonEventArgs e)
	{
		if (_selected is (string name, string uuid, string value))
		{
			string command = "/give @p minecraft:player_head{SkullOwner:\"" + uuid + "\"}";
			ModBase.ClipboardSet(command);
		}
	}

	private void BtnCopyValue_Click(object sender, MouseButtonEventArgs e)
	{
		if (_selected is (string name, string uuid, string value))
		{
			ModBase.ClipboardSet(value);
		}
	}

	private void BtnOpenSite_Click(object sender, MouseButtonEventArgs e)
	{
		if (_selected is (string name, string uuid, string value))
		{
			ModBase.OpenWebsite("https://minecraft-heads.com/custom-heads/" + uuid);
		}
	}
}