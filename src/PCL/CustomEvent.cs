using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.Utils.OS;
using PCL.Network;

namespace PCL;

public class CustomEvent
{
	private static readonly HashSet<string> SecuritySensitiveSettingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "LaunchAdvanceRun", "VersionAdvanceRun" };

	private static readonly Dictionary<EventType, Action<string, EventType>> ActionMap = new Dictionary<EventType, Action<string, EventType>>
	{
		[EventType.OpenUrl] = _OpenUrl,
		[EventType.OpenFile] = _OpenFileOrCommand,
		[EventType.ExecuteCommand] = _OpenFileOrCommand,
		[EventType.LaunchGame] = _LaunchGame,
		[EventType.CopyText] = _CopyText,
		[EventType.RefreshHomepage] = _Refresh,
		[EventType.RefreshPage] = _Refresh,
		[EventType.DailyFortune] = _DailyFortune,
		[EventType.ClearTrash] = _ClearTrash,
		[EventType.ShowDialog] = _ShowDialog,
		[EventType.ShowHint] = _ShowHint,
		[EventType.SwitchPage] = _SwitchPage,
		[EventType.ImportModpack] = _ModpackInstall,
		[EventType.InstallModpack] = _ModpackInstall,
		[EventType.DownloadFile] = _DownloadFile,
		[EventType.ModifySetting] = _WriteSetting,
		[EventType.WriteSetting] = _WriteSetting,
		[EventType.ModifyVariable] = _WriteVariable,
		[EventType.WriteVariable] = _WriteVariable,
		[EventType.OpenHelp] = _OpenHelp
	};

	public EventType Type { get; set; }

	public string Data { get; set; } = string.Empty;

	public CustomEvent()
	{
	}

	public CustomEvent(EventType type, string data)
	{
		Type = type;
		Data = data;
	}

	public void Raise()
	{
		Raise(Type, Data);
	}

	public static void Raise(EventType type, string arg)
	{
		if (type == EventType.None)
		{
			return;
		}
		ModBase.Log($"[Control] 执行自定义事件: {type}, {arg}");
		try
		{
			if (ActionMap.TryGetValue(type, out Action<string, EventType> value))
			{
				value(arg, type);
				return;
			}
			ModMain.MyMsgBox(Lang.Text("Event.Error.UnknownType", new object[1] { type.ToString() }), Lang.Text("Event.Error.Title"));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Event.Error.ExecutionFailed", new object[2] { type, arg }), ModBase.LogLevel.Msgbox, null, Lang.Text("Event.Error.ExecutionFailed", new object[2] { type, arg }));
		}
	}

	public static string GetCustomVariable(string name, string defaultValue = "")
	{
		return States.CustomVariables.GetValueOrDefault(name, defaultValue);
	}

	private static string[] SplitArgs(string arg)
	{
		return arg.Split('|');
	}

	private static string FixNewlines(string s)
	{
		return s.Replace("\\n", "\r\n");
	}

	private static void _OpenUrl(string arg, EventType type)
	{
		arg = arg.Replace('\\', '/');
		if (!arg.Contains("://") || arg.StartsWithF("file", ignoreCase: true))
		{
			ModMain.MyMsgBox(Lang.Text("Event.Error.UrlRequired"), Lang.Text("Event.Error.Title"));
			return;
		}
		HintService.Hint(Lang.Text("Event.OpenUrl.Opening", new object[1] { arg }));
		ModBase.RunInThread(() =>
		{
			ModBase.OpenWebsite(arg);
		});
	}

	private static void _OpenFileOrCommand(string arg, EventType type)
	{
		string[] args = SplitArgs(arg);
		ModBase.RunInThread(() =>
		{
			try
			{
				string[] absoluteUrls = GetAbsoluteUrls(args[0], type);
				if (EventSafetyConfirm(absoluteUrls[0] + ((args.Length >= 2) ? (" " + args[1]) : "")))
				{
					ProcessInterop.Start(absoluteUrls[0], (args.Length >= 2) ? args[1] : "", false);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Event.Error.ExecutionFailed", new object[2] { type, arg }), ModBase.LogLevel.Msgbox, null, Lang.Text("Event.Error.ExecutionFailed", new object[2] { type, arg }));
			}
		});
	}

	private static void _LaunchGame(string arg, EventType type)
	{
		string[] args = SplitArgs(arg);
		if (args[0] == "\\current")
		{
			if ((object)ModInstanceList.McMcInstanceSelected == null)
			{
				throw new InvalidOperationException(Lang.Text("Event.LaunchGame.SelectVersion"));
			}
			args[0] = ModInstanceList.McMcInstanceSelected.Name;
		}
		ModBase.RunInUi(() =>
		{
			if (ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
			{
				ServerIp = ((args.Length >= 2) ? args[1] : null),
				instance = new McInstance(args[0])
			}))
			{
				HintService.Hint(Lang.Text("Event.LaunchGame.Starting", new object[1] { args[0] }));
			}
		});
	}

	private static void _CopyText(string arg, EventType _)
	{
		ModBase.ClipboardSet(arg);
	}

	private static void _Refresh(string arg, EventType _)
	{
		MyPageRight myPageRight = ModMain.frmMain?.pageRight;
		IRefreshable refreshable = myPageRight as IRefreshable;
		if (refreshable != null)
		{
			ModBase.RunInUiWait(() =>
			{
				refreshable.Refresh();
			});
			if (string.IsNullOrEmpty(arg))
			{
				HintService.Hint(Lang.Text("Event.Refresh.Success"), HintType.Success);
			}
		}
		else
		{
			HintService.Hint(Lang.Text("Event.Refresh.NotSupported"), HintType.Error);
		}
	}

	private static void _DailyFortune(string _, EventType __)
	{
		PageToolsTest.Jrrp();
	}

	private static void _ClearTrash(string _, EventType __)
	{
		ModBase.RunInThread(PageToolsTest.RubbishClear);
	}

	private static void _ShowDialog(string arg, EventType type)
	{
		string[] array = SplitArgs(arg);
		if (array.Length == 1)
		{
			throw new ArgumentException(Lang.Text("Event.Error.MissingArgs", new object[2]
			{
				type.ToString(),
				"Title|Content"
			}));
		}
		ModMain.MyMsgBox(FixNewlines(array[1]), FixNewlines(array[0]), (array.Length > 2) ? array[2] : Lang.Text("Common.Action.Confirm"));
	}

	private static void _ShowHint(string arg, EventType _)
	{
		string[] array = SplitArgs(arg);
		HintType type = ((array.Length != 1) ? ((HintType)Enum.Parse(typeof(HintType), NormalizeHintTypeName(array[1]), ignoreCase: true)) : HintType.Info);
		HintService.Hint(FixNewlines(array[0]), type);
	}

	private static string NormalizeHintTypeName(string name)
	{
		string text = name.ToLowerInvariant();
		if (!(text == "finish"))
		{
			if (text == "critical")
			{
				return "Error";
			}
			return name;
		}
		return "Success";
	}

	private static void _SwitchPage(string arg, EventType _)
	{
		string[] args = SplitArgs(arg);
		ModBase.RunInUi(() =>
		{
			FormMain.PageType pageType = (FormMain.PageType)Enum.Parse(typeof(FormMain.PageType), args[0], ignoreCase: true);
			FormMain.PageSubType subType = ((args.Length != 1) ? ((FormMain.PageSubType)Enum.Parse(typeof(FormMain.PageSubType), args[1], ignoreCase: true)) : FormMain.PageSubType.Default);
			ModMain.frmMain?.PageChange(pageType, subType);
		});
	}

	private static void _ModpackInstall(string _, EventType __)
	{
		ModBase.RunInUi(ModModpack.ModpackInstall);
	}

	private static void _DownloadFile(string arg, EventType _)
	{
		string[] array = SplitArgs(arg);
		array[0] = array[0].Replace('\\', '/');
		if (!array[0].StartsWithF("http://", ignoreCase: true) && !array[0].StartsWithF("https://", ignoreCase: true))
		{
			ModMain.MyMsgBox(Lang.Text("Event.Error.DownloadUrlRequired"), Lang.Text("Event.Error.Title"));
		}
		else if (EventSafetyConfirm(Lang.Text("Event.Download.Confirm", new object[1] { array[0] })))
		{
			try
			{
				PageToolsTest.StartCustomDownload(array[0], (array.Length >= 2) ? array[1] : ModBase.GetFileNameFromPath(array[0]), (array.Length >= 3) ? array[2] : null);
			}
			catch
			{
				PageToolsTest.StartCustomDownload(array[0], Lang.Text("Common.State.Unknown"));
			}
		}
	}

	private static void _WriteSetting(string arg, EventType type)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Invalid comparison between Unknown and I4
		string[] array = SplitArgs(arg);
		if (array.Length == 1)
		{
			throw new ArgumentException(Lang.Text("Event.Error.MissingArgs", new object[2]
			{
				type.ToString(),
				"SettingName|Value"
			}));
		}
		ConfigItem val = default;
		if (ConfigService.TryGetConfigItemNoType(array[0], out val) && (int)val.Source != 1)
		{
			if (!CanWriteSettingFromCustomEvent(array[0]))
			{
				return;
			}
			val.SetValueNoType((object)array[1], (object)ModInstanceList.McMcInstanceSelected?.PathInstance);
		}
		if (array.Length == 2)
		{
			HintService.Hint(Lang.Text("Event.Setting.Written", new object[2]
			{
				array[0],
				array[1]
			}), HintType.Success);
		}
	}

	private static bool CanWriteSettingFromCustomEvent(string key)
	{
		if (!SecuritySensitiveSettingKeys.Contains(key))
		{
			return true;
		}
		ModBase.Log("[Control] 已阻止自定义事件写入高危设置：" + key, ModBase.LogLevel.Developer);
		HintService.Hint(Lang.Text("Event.Safety.DangerousSettingBlocked", new object[1] { key }), HintType.Error);
		return false;
	}

	private static void _WriteVariable(string arg, EventType type)
	{
		string[] array = SplitArgs(arg);
		if (array.Length == 1)
		{
			throw new ArgumentException(Lang.Text("Event.Error.MissingArgs", new object[2]
			{
				type.ToString(),
				"VariableName|Value"
			}));
		}
		States.CustomVariables[array[0]] = array[1];
		States.CustomVariables = States.CustomVariables;
		if (array.Length == 2)
		{
			HintService.Hint(Lang.Text("Event.Variable.Written", new object[2]
			{
				array[0],
				array[1]
			}), HintType.Success);
		}
	}

	private static void _OpenHelp(string arg, EventType type)
	{
		string[] args = SplitArgs(arg);
		if (!Uri.TryCreate(args[0], UriKind.Absolute, out Uri result) || (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps))
		{
			ModBase.OpenWebsite("https://docs.pclc.cc/ce");
			return;
		}
		ModBase.RunInThread(() =>
		{
			try
			{
				string jsonPath = GetAbsoluteUrls(args[0], type)[0];
				PageHelpDetail.HelpContent content = PageHelpDetail.LoadContent(jsonPath);
				ModBase.RunInUiWait(() =>
				{
					PageHelpDetail helpPage = new PageHelpDetail(content);
					ModMain.frmMain.PageChange(new FormMain.PageStackData
					{
						page = FormMain.PageType.HelpDetail,
						helpPage = helpPage
					});
				});
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Event.Error.ExecutionFailed", new object[2] { type, arg }), ModBase.LogLevel.Msgbox, null, Lang.Text("Event.Error.ExecutionFailed", new object[2] { type, arg }));
			}
		});
	}

	public static string[] GetAbsoluteUrls(string relativeUrl, EventType type)
	{
		if (type == EventType.OpenHelp && Uri.TryCreate(relativeUrl, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
		{
			if (ModBase.RunInUi())
			{
				throw new Exception("能打开联网帮助页面的 MyListItem 必须手动设置 Title、Info 属性！");
			}
			string text;
			try
			{
				text = Uri.UnescapeDataString(Path.GetFileName(result.AbsolutePath));
				if (!Path.GetExtension(text).Equals(".json", StringComparison.OrdinalIgnoreCase))
				{
					throw new Exception("未指向 .json 后缀的文件");
				}
				if (text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				{
					throw new Exception("文件名包含非法字符");
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("联网帮助页面须指向一个帮助 JSON 文件，并在同路径下包含相应 XAML 文件！\n例如：\n - https://www.baidu.com/test.json（填写这个路径）\n - https://www.baidu.com/test.xaml（同时也需要包含这个文件）", innerException);
			}
			string text2 = Path.Combine(ModMain.RequestTaskTempFolder(), text);
			string localFile = Path.ChangeExtension(text2, ".xaml");
			string absoluteUri = new UriBuilder(result)
			{
				Path = Path.ChangeExtension(result.AbsolutePath, ".xaml").Replace('\\', '/')
			}.Uri.AbsoluteUri;
			ModBase.Log("[Control] 转换网络帮助资源：" + relativeUrl + " -> " + text2);
			try
			{
				InlineArray2<Task> buffer = default;
				buffer[0] = ModNet.NetDownloadByClient(result.AbsoluteUri, text2);
				buffer[1] = ModNet.NetDownloadByClient(absoluteUri, localFile);
				Task.WhenAll(buffer).GetAwaiter().GetResult();
			}
			catch (Exception innerException2)
			{
				throw new Exception("下载指定的文件失败！\n注意，联网帮助页面须指向一个帮助 JSON 文件，并在同路径下包含相应 XAML 文件！\n例如：\n - https://www.baidu.com/test.json（填写这个路径）\n - https://www.baidu.com/test.xaml（同时也需要包含这个文件）", innerException2);
			}
			relativeUrl = text2;
		}
		relativeUrl = relativeUrl.Replace('/', Path.DirectorySeparatorChar);
		string text3 = Path.Combine(Basics.ExecutableDirectory, "PCL");
		if (Path.IsPathFullyQualified(relativeUrl))
		{
			ModBase.Log($"[Control] 自定义事件中由绝对路径 {type}: {relativeUrl}");
			return new string[2] { relativeUrl, text3 };
		}
		if (File.Exists(Path.Combine(text3, relativeUrl)))
		{
			string text4 = Path.Combine(text3, relativeUrl);
			if (!Path.GetFullPath(text4).StartsWith(text3, StringComparison.OrdinalIgnoreCase))
			{
				throw new UnauthorizedAccessException(Lang.Text("Event.Error.FileNotFound", new object[1] { relativeUrl }));
			}
			ModBase.Log($"[Control] 自定义事件中由相对 PCL 文件夹的路径 {type}: {text4}");
			return new string[2] { text4, text3 };
		}
		if ((uint)(type - 2) <= 1u)
		{
			ModBase.Log($"[Control] 自定义事件中直接 {type}: {relativeUrl}");
			return new string[2] { relativeUrl, text3 };
		}
		throw new FileNotFoundException(Lang.Text("Event.Error.FileNotFound", new object[1] { relativeUrl }), relativeUrl);
	}

	private static bool EventSafetyConfirm(string message)
	{
		if (States.Hint.HomepageCommand)
		{
			return true;
		}
		return ModMain.MyMsgBox(Lang.Text("Event.Safety.ConfirmMessage", new object[1] { message }), Lang.Text("Event.Safety.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Event.Safety.ContinueAlways"), Lang.Text("Common.Action.Cancel")) switch
		{
			1 => true, 
			2 => States.Hint.HomepageCommand = true, 
			_ => false, 
		};
	}
}
