using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Network;

namespace PCL;

public partial class PageLaunchRight : MyPageRight, IRefreshable, IComponentConnector
{
	private readonly object refreshLock = new object();

	private readonly ModLoader.LoaderTask<string, int> onlineLoader;

	private int loadedContentHash = -1;

	private readonly object loadContentLock = new object();

	private const string homepageLivePatchFileName = "CustomLive.json";

	private const string homepageLiveSupportFileName = "CustomLive.supported.json";

	private static readonly Dictionary<string, string> _homepageLiveAllowedProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["text"] = "Text",
		["title"] = "Title",
		["info"] = "Info",
		["tooltip"] = "ToolTip",
		["visibility"] = "Visibility",
		["isEnabled"] = "IsEnabled",
		["opacity"] = "Opacity"
	};

	private FileSystemWatcher? _homepageLiveWatcher;

	private DispatcherTimer? _homepageLivePatchTimer;

	public PageLaunchRight()
	{
		InitializeComponent();
		onlineLoader = new ModLoader.LoaderTask<string, int>(Lang.Text("Launch.Homepage.Task.Download"), OnlineLoaderSub)
		{
			reloadTimeout = 600000
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Refresh();
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			_DisposeHomepageLiveWatcher();
		};
	}

	private void Init()
	{
		PanBack.ScrollToHome();
		PanScroll = PanBack;
		PanLog.Visibility = ((!ModBase.modeDebug) ? Visibility.Collapsed : Visibility.Visible);
		PanHint.Visibility = ((!States.Hint.CEMessage) ? Visibility.Collapsed : Visibility.Visible);
		LabHint1.Text = Lang.Text("Launch.Right.CommunityHint.Message");
		LabHint2.Visibility = Visibility.Collapsed;
		_EnsureHomepageLiveWatcher();
	}

	private void BtnHintClose_Click(object sender, EventArgs e)
	{
		ModAnimation.AniDispose(PanHint, removeFromChildren: true);
		States.Hint.CEMessage = false;
	}

	private void Refresh()
	{
		ModBase.RunInNewThread(() =>
		{
			try
			{
				lock (refreshLock)
				{
					RefreshReal();
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "加载 PCL 主页自定义信息失败", ModBase.modeDebug ? ModBase.LogLevel.Msgbox : ModBase.LogLevel.Hint, null, Lang.Text("Launch.Error.OperationFailed"));
			}
		}, $"刷新主页 #{ModBase.GetUuid()}");
	}

	private void RefreshReal()
	{
		string content = "";
		string text = null;
		switch (Config.Preference.Homepage.Type)
		{
		case 1:
			LogWrapper.Info("[Page] 主页自定义数据来源：本地文件");
			content = ModBase.ReadFile(Path.Combine(ModBase.exePath, "PCL", "Custom.xaml"));
			break;
		case 2:
			text = Config.Preference.Homepage.CustomUrl;
			content = LoadFromNetwork(text);
			break;
		case 3:
			switch (Config.Preference.Homepage.SelectedPreset)
			{
			case 0:
			{
				LogWrapper.Info("[Page] 主页预设：你知道吗");
				string randomHint = GetRandomHint();
				content = "\n    <local:MyCard Title=\"{DynamicResource Launch.Status.Trivia}\" Margin=\"0,0,0,15\">\n        <TextBlock Margin=\"25,38,23,15\" FontSize=\"13.5\" IsHitTestVisible=\"False\" Text=\"" + randomHint + "\" TextWrapping=\"Wrap\" Foreground=\"{DynamicResource ColorBrush1}\" />\n        <local:MyIconButton Height=\"22\" Width=\"22\" Margin=\"9\" VerticalAlignment=\"Top\" HorizontalAlignment=\"Right\" \n            EventType=\"刷新主页\" EventData=\"/\"\n            SvgIcon=\"lucide/refresh-cw\" />\n    </local:MyCard>";
				break;
			}
			case 1:
				LogWrapper.Info("[Page] 主页预设：回声洞 已被移除");
				ModMain.MyMsgBox(Lang.Text("Launch.Homepage.Preset.EchoCave.Removed"));
				return;
			case 2:
				LogWrapper.Info("[Page] 主页预设：Minecraft 新闻");
				text = "https://news.bugjump.net";
				content = LoadFromNetwork(text);
				break;
			case 3:
				LogWrapper.Info("[Page] 主页预设：每日整合包推荐");
				text = "https://pclsub.sodamc.com/";
				content = LoadFromNetwork(text);
				break;
			case 4:
				LogWrapper.Info("[Page] 主页预设：Minecraft 皮肤推荐");
				text = "https://forgepixel.com/pcl_sub_file";
				content = LoadFromNetwork(text);
				break;
			case 5:
				LogWrapper.Info("[Page] 主页预设：OpenBMCLAPI 仪表盘 Lite");
				text = "https://pcl-bmcl.milu.ink/";
				content = LoadFromNetwork(text);
				break;
			case 6:
				LogWrapper.Info("[Page] 主页预设：PCL 新功能说明书");
				text = "https://raw.gitcode.com/WForst-Breeze/WhatsNewPCL/raw/main/Custom.xaml";
				content = LoadFromNetwork(text);
				break;
			case 7:
				LogWrapper.Info("[Page] 主页预设：杂志主页");
				text = "https://pclhomeplazaoss.lingyunawa.top:26994/d/Homepages/Ext1nguisher/Custom.xaml";
				content = LoadFromNetwork(text);
				break;
			case 8:
				LogWrapper.Info("[Page] 主页预设：PCL GitHub 仪表盘");
				text = "https://ddf.pcl-community.org/Custom.xaml";
				content = LoadFromNetwork(text);
				break;
			case 9:
				LogWrapper.Info("[Page] 主页预设：Minecraft 更新摘要");
				text = "https://raw.gitcode.com/ENC_Euphony/PCL-AI-Summary-HomePage/raw/master/Custom.xaml";
				content = LoadFromNetwork(text);
				break;
			case 10:
				LogWrapper.Info("[Page] 主页预设：今日新闻热点");
				text = "https://pcl.wyc-w.top/index.xaml";
				content = LoadFromNetwork(text);
				break;
			case 11:
				LogWrapper.Info("[Page] 主页预设：Minecraft 芝士站");
				text = "https://www.xxag.top/mkss";
				content = LoadFromNetwork(text);
				break;
			case 12:
				LogWrapper.Info("[Page] 主页预设：整合包推荐引擎");
				text = "https://qawsedrftgyhujiko.fun/pcl2/Custom.xaml";
				content = LoadFromNetwork(text);
				break;
			case 13:
				LogWrapper.Info("[Page] 主页预设：Bangumi 番剧主页");
				text = "https://bangumi.p.kaphia.qzz.io";
				content = LoadFromNetwork(text);
				break;
			case 14:
				LogWrapper.Info("[Page] 主页预设：PCL Ray 公告栏");
				text = "https://s3.pysio.online/pcl2-ce/apiv2/pages/announce.xaml";
				content = LoadFromNetwork(text);
				break;
			case 15:
				LogWrapper.Info("[Page] 主页预设：Minecraft 信息流");
				((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
				{
					if (ModMain.frmHomepageNews == null)
					{
						ModMain.frmHomepageNews = new PageHomepageNewsView();
					}
					PanCustom.Children.Clear();
					PanCustom.Children.Add(ModMain.frmHomepageNews);
				}));
				return;
			}
			break;
		}
		ModBase.RunInUi(() =>
		{
			LoadContent(content);
		});
	}

	private string LoadFromNetwork(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return "";
		}
		string text = Path.Combine(ModBase.pathTemp, "Cache", "Custom.xaml");
		string savedHomepageUrl = States.UI.SavedHomepageUrl;
		if (url == savedHomepageUrl && File.Exists(text))
		{
			LogWrapper.Info("[Page] 主页自定义数据来源：联网缓存文件");
			onlineLoader.Start(url);
			return ModBase.ReadFile(text);
		}
		LogWrapper.Info("[Page] 主页自定义数据来源：联网全新下载");
		HintWrapper.Show(Lang.Text("Launch.Homepage.Loading"), (HintTheme)0);
		ModBase.RunInUiWait(() =>
		{
			LoadContent("");
		});
		States.UI.SavedHomepageVersion = "";
		onlineLoader.Start(url);
		return "";
	}

	public static string GetRandomHint(bool enableLengthLimit = false, bool raw = false)
	{
		string[] array = null;
		string text = Path.Combine(ModBase.exePath, "PCL", "hints.txt");
		if (File.Exists(text))
		{
			try
			{
				array = (from l in File.ReadAllLines(text)
					where !string.IsNullOrWhiteSpace(l)
					select l.Trim()).ToArray();
			}
			catch
			{
				ModBase.Log(Lang.Text("Launch.Homepage.Error.ExternalFile", new object[1] { text }), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Homepage.Error.ExternalFile", new object[1] { text }));
			}
		}
		if (array == null || array.Length == 0)
		{
			array = _LoadEmbeddedHints(LocalizationService.CurrentLanguage.Code) ?? _LoadEmbeddedHints("zh-CN");
		}
		if (enableLengthLimit)
		{
			string[] array2 = array.Where((string l) => l.Length < 50).ToArray();
			if (array2.Length != 0)
			{
				array = array2;
			}
		}
		string text2 = array[Random.Shared.Next(array.Length)];
		if (!raw)
		{
			return text2.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
				.Replace("\"", "&quot;");
		}
		return text2;
	}

	private static string[]? _LoadEmbeddedHints(string langCode)
	{
		try
		{
			using Stream stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Plain Craft Launcher 2;component/Resources/hints/" + langCode + ".txt", UriKind.Absolute))?.Stream;
			if (stream == null)
			{
				return null;
			}
			using StreamReader streamReader = new StreamReader(stream);
			return (from l in streamReader.ReadToEnd().Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				where !string.IsNullOrWhiteSpace(l)
				select l.Trim()).ToArray();
		}
		catch
		{
			return null;
		}
	}

	private void OnlineLoaderSub(ModLoader.LoaderTask<string, int> task)
	{
		string input = task.input;
		try
		{
			string text;
			if (input.Contains(".xaml"))
			{
				text = input.Replace(".xaml", ".xaml.ini");
			}
			else
			{
				text = input.BeforeFirst("?");
				if (!text.EndsWith("/"))
				{
					text += "/";
				}
				text += "version";
				if (input.Contains("?"))
				{
					text = text + "?" + input.AfterFirst("?");
				}
			}
			string text2 = "";
			bool flag = true;
			try
			{
				text2 = Requester.FetchString(text);
				if (text2.Length > 1000)
				{
					throw new Exception($"获取的主页版本过长（{text2.Length} 字符）");
				}
				string savedHomepageVersion = States.UI.SavedHomepageVersion;
				if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(savedHomepageVersion) && (text2 ?? "") == (savedHomepageVersion ?? ""))
				{
					ModBase.Log("[Page] 当前缓存的主页已为最新，当前版本：" + text2 + "，检查源：" + text);
					flag = false;
				}
				else
				{
					ModBase.Log("[Page] 需要下载联网主页，当前版本：" + text2 + "，检查源：" + text);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "联网获取主页版本失败", ModBase.LogLevel.Developer);
				ModBase.Log("[Page] 无法检查联网主页版本，将直接下载，检查源：" + text);
			}
			if (flag)
			{
				string text3 = Requester.FetchString(input);
				ModBase.Log($"[Page] 已联网下载主页，内容长度：{text3.Length}，来源：{input}");
				States.UI.SavedHomepageUrl = input;
				States.UI.SavedHomepageVersion = text2;
				ModBase.WriteFile(ModBase.pathTemp + "Cache\\Custom.xaml", text3);
			}
			ModBase.RunInUi(Refresh);
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Launch.Homepage.Error.Download", new object[1] { input }), ModBase.modeDebug ? ModBase.LogLevel.Msgbox : ModBase.LogLevel.Hint, null, Lang.Text("Launch.Homepage.Error.Download", new object[1] { input }));
		}
	}

	public void ForceRefresh()
	{
		ModBase.Log("[Page] 要求强制刷新主页");
		ClearCache();
		if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.Launch)
		{
			PanBack.ScrollToHome();
			Refresh();
		}
		else
		{
			ModMain.frmMain.PageChange(FormMain.PageType.Launch);
		}
	}

	void IRefreshable.Refresh()
	{
		ForceRefresh();
	}

	private void ClearCache()
	{
		loadedContentHash = -1;
		onlineLoader.input = "";
		States.UI.SavedHomepageUrl = "";
		States.UI.SavedHomepageVersion = "";
		ModBase.Log("[Page] 已清空主页缓存");
	}

	private void LoadContent(string content)
	{
		lock (loadContentLock)
		{
			int hashCode = content.GetHashCode();
			if (hashCode == loadedContentHash)
			{
				_ApplyHomepageLivePatchesFromFile();
				return;
			}
			loadedContentHash = hashCode;
			PanCustom.Children.Clear();
			if (string.IsNullOrWhiteSpace(content))
			{
				ModBase.Log("[Page] 实例化：清空主页 UI，来源为空");
				return;
			}
			DateTime now = DateTime.Now;
			try
			{
				content = ModMain.ArgumentReplace(content);
				while (content.Contains("xmlns"))
				{
					content = content.RegexReplace("xmlns[^\"']*(\"|')[^\"']*(\"|')", "").Replace("xmlns", "");
				}
				content = "<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:sys=\"clr-namespace:System;assembly=System.Runtime\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\">" + content + "</StackPanel>";
				ModBase.Log($"[Page] 实例化：加载主页 UI 开始，最终内容长度：{content.Count()}");
				PanCustom.Children.Add((UIElement)ModBase.GetObjectFromXML(content, out XamlEventSanitizer.SanitizeResult sanitizeResult));
				_ShowSanitizeHints(sanitizeResult);
				_ApplyHomepageLivePatchesFromFile();
			}
			catch (Exception ex)
			{
				if (!ModBase.modeDebug)
				{
					ModBase.Log(ex, Lang.Text("Launch.Homepage.LoadFailed.Title"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Homepage.LoadFailed.Title"));
					return;
				}
				ModBase.Log(ex, "加载失败的主页内容：\r\n" + content);
				if (ModMain.MyMsgBox((ex is UnauthorizedAccessException) ? ex.Message : Lang.Text("Launch.Homepage.LoadFailed.Message", new object[1] { ex }), Lang.Text("Launch.Homepage.LoadFailed.Title"), Lang.Text("Launch.Homepage.LoadFailed.Retry"), Lang.Text("Common.Action.Cancel")) != 1)
				{
					return;
				}
				goto end_IL_0009;
			}
			int milliseconds = (DateTime.Now - now).Milliseconds;
			ModBase.Log($"[Page] 实例化：加载主页 UI 完成，耗时 {milliseconds}ms");
			if (milliseconds > 3000)
			{
				HintService.Hint(Lang.Text("Launch.Homepage.SlowWarning", new object[1] { Lang.Number<double>(Math.Round((double)milliseconds / 1000.0, 1), "N1") }));
			}
			return;
			end_IL_0009:;
		}
		ForceRefresh();
	}

	private static void _ShowSanitizeHints(XamlEventSanitizer.SanitizeResult result)
	{
		foreach (string item in result.UnsupportedTypesFound)
		{
			HintService.Hint(Lang.Text("Event.Sanitize.UnsupportedTypeHint", new object[1] { item }), HintType.Error);
		}
		foreach (string unrecognizedType in result.UnrecognizedTypes)
		{
			HintService.Hint(Lang.Text("Event.Sanitize.UnknownTypeHint", new object[1] { unrecognizedType }), HintType.Error);
		}
	}

	private void _EnsureHomepageLiveWatcher()
	{
		if (_homepageLiveWatcher != null || Config.Preference.Homepage.Type != 1)
		{
			return;
		}
		try
		{
			string text = _GetHomepageLiveDirectory();
			Directory.CreateDirectory(text);
			_WriteHomepageLiveSupportMarker(text);
			_homepageLiveWatcher = new FileSystemWatcher(text, "CustomLive.json")
			{
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite)
			};
			_homepageLiveWatcher.Changed += (object _, FileSystemEventArgs _) =>
			{
				_QueueHomepageLivePatchApply();
			};
			_homepageLiveWatcher.Created += (object _, FileSystemEventArgs _) =>
			{
				_QueueHomepageLivePatchApply();
			};
			_homepageLiveWatcher.Renamed += (object _, RenamedEventArgs _) =>
			{
				_QueueHomepageLivePatchApply();
			};
			_homepageLiveWatcher.EnableRaisingEvents = true;
			_QueueHomepageLivePatchApply();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Page] Failed to start custom homepage live patch watcher", ModBase.LogLevel.Developer);
		}
	}

	private void _DisposeHomepageLiveWatcher()
	{
		try
		{
			_homepageLiveWatcher?.Dispose();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Page] Failed to dispose custom homepage live patch watcher", ModBase.LogLevel.Developer);
		}
		_homepageLiveWatcher = null;
		try
		{
			if (_homepageLivePatchTimer != null)
			{
				_homepageLivePatchTimer.Stop();
				_homepageLivePatchTimer.Tick -= _HomepageLivePatchTimerTick;
				_homepageLivePatchTimer = null;
			}
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "[Page] Failed to dispose custom homepage live patch debounce timer", ModBase.LogLevel.Developer);
		}
		_DeleteHomepageLiveSupportMarker();
	}

	private void _QueueHomepageLivePatchApply()
	{
		ModBase.RunInUi(() =>
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected Obj, but got Unknown
			if (_homepageLivePatchTimer == null)
			{
				_homepageLivePatchTimer = new DispatcherTimer
				{
					Interval = TimeSpan.FromMilliseconds(120L)
				};
			}
			_homepageLivePatchTimer.Tick -= _HomepageLivePatchTimerTick;
			_homepageLivePatchTimer.Tick += _HomepageLivePatchTimerTick;
			_homepageLivePatchTimer.Stop();
			_homepageLivePatchTimer.Start();
		});
	}

	private void _HomepageLivePatchTimerTick(object? sender, EventArgs e)
	{
		DispatcherTimer? homepageLivePatchTimer = _homepageLivePatchTimer;
		if (homepageLivePatchTimer != null)
		{
			homepageLivePatchTimer.Stop();
		}
		_ApplyHomepageLivePatchesFromFile();
	}

	private void _ApplyHomepageLivePatchesFromFile()
	{
		if (PanCustom.Children.Count == 0 || Config.Preference.Homepage.Type != 1)
		{
			return;
		}
		string text = Path.Combine(_GetHomepageLiveDirectory(), "CustomLive.json");
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			foreach (JsonObject item in _EnumerateHomepageLivePatches(JsonNode.Parse(_ReadHomepageLivePatchFile(text), new JsonNodeOptions
			{
				PropertyNameCaseInsensitive = true
			}, new JsonDocumentOptions
			{
				CommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true
			})))
			{
				_ApplyHomepageLivePatch(item);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Page] Failed to apply custom homepage live patches", ModBase.LogLevel.Developer);
		}
	}

	private static string _ReadHomepageLivePatchFile(string file)
	{
		Exception ex = null;
		for (int i = 0; i < 3; i++)
		{
			try
			{
				using FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
				using StreamReader streamReader = new StreamReader(stream);
				return streamReader.ReadToEnd();
			}
			catch (Exception ex2)
			{
				ex = ex2;
				Thread.Sleep(50);
			}
		}
		throw ex ?? new IOException("Unable to read custom homepage live patch file.");
	}

	private static string _GetHomepageLiveDirectory()
	{
		return Path.Combine(ModBase.exePath, "PCL");
	}

	private static void _WriteHomepageLiveSupportMarker(string directory)
	{
		try
		{
			JsonObject jsonObject = new JsonObject(new JsonNodeOptions
			{
				PropertyNameCaseInsensitive = true
			})
			{
				["processId"] = Environment.ProcessId,
				["processPath"] = Environment.ProcessPath ?? "",
				["patchFile"] = "CustomLive.json",
				["startedAt"] = DateTime.Now.ToString("O", CultureInfo.InvariantCulture)
			};
			File.WriteAllText(Path.Combine(directory, "CustomLive.supported.json"), jsonObject.ToJsonString());
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Page] Failed to write custom homepage live patch support marker", ModBase.LogLevel.Developer);
		}
	}

	private static void _DeleteHomepageLiveSupportMarker()
	{
		try
		{
			string text = Path.Combine(_GetHomepageLiveDirectory(), "CustomLive.supported.json");
			if (File.Exists(text) && ((JsonObject)JsonNode.Parse(_ReadHomepageLivePatchFile(text), new JsonNodeOptions
			{
				PropertyNameCaseInsensitive = true
			}, new JsonDocumentOptions
			{
				CommentHandling = JsonCommentHandling.Skip,
				AllowTrailingCommas = true
			}))["processId"]?.GetValue<int>() == Environment.ProcessId)
			{
				File.Delete(text);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Page] Failed to delete custom homepage live patch support marker", ModBase.LogLevel.Developer);
		}
	}

	private static IEnumerable<JsonObject> _EnumerateHomepageLivePatches(JsonNode token)
	{
		if (token is JsonObject jsonObject)
		{
			if (jsonObject["patches"] is JsonArray source)
			{
				foreach (JsonObject item in source.OfType<JsonObject>())
				{
					yield return item;
				}
				yield break;
			}
			if (_TryGetString(jsonObject, "target", "tag", "name") != null)
			{
				yield return jsonObject;
				yield break;
			}
			foreach (KeyValuePair<string, JsonNode> item2 in jsonObject)
			{
				if (item2.Value is JsonObject jsonObject2)
				{
					JsonObject jsonObject3 = (JsonObject)JsonNode.Parse(jsonObject2.ToJsonString(), new JsonNodeOptions
					{
						PropertyNameCaseInsensitive = true
					}, new JsonDocumentOptions
					{
						CommentHandling = JsonCommentHandling.Skip,
						AllowTrailingCommas = true
					});
					JsonObject jsonObject4 = jsonObject3;
					if (jsonObject4["target"] == null)
					{
						jsonObject4["target"] = item2.Key;
					}
					yield return jsonObject3;
				}
			}
		}
		else
		{
			if (!(token is JsonArray source2))
			{
				yield break;
			}
			foreach (JsonObject item3 in source2.OfType<JsonObject>())
			{
				yield return item3;
			}
		}
	}

	private void _ApplyHomepageLivePatch(JsonObject patch)
	{
		string text = _TryGetString(patch, "target", "tag", "name");
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		foreach (FrameworkElement item in _FindElementsByTag((DependencyObject)(object)PanCustom, text))
		{
			_ApplyHomepageLivePatchToElement(item, patch);
		}
	}

	private void _ApplyHomepageLivePatchToElement(FrameworkElement element, JsonObject patch)
	{
		_SetPropertyIfPresent(element, patch, "text", "Text");
		_SetPropertyIfPresent(element, patch, "title", "Title");
		_SetPropertyIfPresent(element, patch, "info", "Info");
		_SetPropertyIfPresent(element, patch, "tooltip", "ToolTip");
		_SetPropertyIfPresent(element, patch, "toolTip", "ToolTip");
		_SetPropertyIfPresent(element, patch, "visibility", "Visibility");
		_SetPropertyIfPresent(element, patch, "isEnabled", "IsEnabled");
		_SetPropertyIfPresent(element, patch, "opacity", "Opacity");
		if (patch["properties"] is JsonObject jsonObject)
		{
			foreach (KeyValuePair<string, JsonNode> item in jsonObject)
			{
				_TrySetElementProperty(element, item.Key, item.Value?.ToString() ?? "");
			}
		}
		string text = _TryGetString(patch, "childrenXaml", "ChildrenXaml");
		if (!string.IsNullOrEmpty(text) && element is Panel panel)
		{
			_ReplacePanelChildren(panel, text);
		}
	}

	private static void _SetPropertyIfPresent(FrameworkElement element, JsonObject patch, string jsonName, string propertyName)
	{
		if (patch.TryGetPropertyValue(jsonName, out JsonNode jsonNode))
		{
			_TrySetElementProperty(element, propertyName, jsonNode?.ToString() ?? "");
		}
	}

	private static bool _TrySetElementProperty(FrameworkElement element, string propertyName, string value)
	{
		if (!_homepageLiveAllowedProperties.TryGetValue(propertyName, out string value2))
		{
			ModBase.Log("[Page] Skipped unsupported live patch property " + propertyName, ModBase.LogLevel.Developer);
			return false;
		}
		propertyName = value2;
		PropertyInfo property = ((object)element).GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
		if (property == null || !property.CanWrite)
		{
			return false;
		}
		try
		{
			Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
			string text = value.Trim();
			object value3;
			bool result;
			int result2;
			double result3;
			if (type == typeof(string))
			{
				value3 = value;
			}
			else if (type == typeof(object))
			{
				value3 = value;
			}
			else if (type == typeof(bool) && bool.TryParse(text, out result))
			{
				value3 = result;
			}
			else if (type == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result2))
			{
				value3 = result2;
			}
			else if (type == typeof(double) && double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result3))
			{
				value3 = result3;
			}
			else if (type == typeof(Visibility))
			{
				if (!Enum.TryParse<Visibility>(text, ignoreCase: true, out var result4))
				{
					return false;
				}
				value3 = result4;
			}
			else
			{
				if (!type.IsEnum || !Enum.TryParse(type, text, ignoreCase: true, out object result5))
				{
					return false;
				}
				value3 = result5;
			}
			property.SetValue(element, value3);
			return true;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[Page] Failed to set live patch property " + propertyName, ModBase.LogLevel.Developer);
			return false;
		}
	}

	private static void _ReplacePanelChildren(Panel panel, string childrenXaml)
	{
		string text = ModMain.ArgumentReplace(childrenXaml);
		while (text.Contains("xmlns"))
		{
			text = text.RegexReplace("xmlns[^\"']*(\"|')[^\"']*(\"|')", "").Replace("xmlns", "");
		}
		if (!(ModBase.GetObjectFromXML("<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:sys=\"clr-namespace:System;assembly=System.Runtime\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\">" + text + "</StackPanel>") is Panel panel2))
		{
			return;
		}
		List<UIElement> list = panel2.Children.OfType<UIElement>().ToList();
		panel2.Children.Clear();
		panel.Children.Clear();
		foreach (UIElement item in list)
		{
			panel.Children.Add(item);
		}
	}

	private static IEnumerable<FrameworkElement> _FindElementsByTag(DependencyObject root, string tag)
	{
		if (root is FrameworkElement { Tag: var tag2 } frameworkElement && string.Equals(tag2?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
		{
			yield return frameworkElement;
		}
		int count;
		try
		{
			count = VisualTreeHelper.GetChildrenCount(root);
		}
		catch
		{
			yield break;
		}
		for (int i = 0; i < count; i++)
		{
			foreach (FrameworkElement item in _FindElementsByTag(VisualTreeHelper.GetChild(root, i), tag))
			{
				yield return item;
			}
		}
	}

	private static string? _TryGetString(JsonObject obj, params string[] names)
	{
		foreach (string propertyName in names)
		{
			if (obj.TryGetPropertyValue(propertyName, out JsonNode jsonNode))
			{
				return jsonNode?.ToString();
			}
		}
		return null;
	}

}
