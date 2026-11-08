using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageHelpDetail : MyPageRight, IRefreshable, IComponentConnector
{
	internal sealed record HelpContent(string SourcePath, string Title, string Xaml);

	private HelpContent _content;

	public string Title => _content.Title;

	internal PageHelpDetail(HelpContent content)
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			PanBack.ScrollToHome();
		};
		ApplyContent(content);
	}

	internal static HelpContent LoadContent(string jsonPath)
	{
		if (!File.Exists(jsonPath))
		{
			throw new FileNotFoundException("未找到帮助 JSON 文件", jsonPath);
		}
		using JsonDocument jsonDocument = JsonDocument.Parse(ModMain.ArgumentReplace(File.ReadAllText(jsonPath), SecurityElement.Escape));
		if (!jsonDocument.RootElement.TryGetProperty("Title", out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
		{
			throw new ArgumentException("帮助 JSON 中未找到有效的 Title 项", "jsonPath");
		}
		string text = Path.ChangeExtension(jsonPath, ".xaml");
		if (!File.Exists(text))
		{
			throw new FileNotFoundException("未找到帮助 JSON 对应的 XAML 文件", text);
		}
		string text2 = File.ReadAllText(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			throw new InvalidDataException("帮助 XAML 文件为空");
		}
		return new HelpContent(jsonPath, value.GetString(), text2);
	}

	private void ApplyContent(HelpContent content)
	{
		string text = ModMain.ArgumentReplace(content.Xaml);
		while (text.Contains("xmlns"))
		{
			text = text.RegexReplace("xmlns[^\"']*(\"|')[^\"']*(\"|')", "").Replace("xmlns", "");
		}
		text = "<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:sys=\"clr-namespace:System;assembly=System.Runtime\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\">" + text + "</StackPanel>";
		UIElement element = (UIElement)ModBase.GetObjectFromXML(text, out XamlEventSanitizer.SanitizeResult sanitizeResult);
		foreach (string item in sanitizeResult.UnsupportedTypesFound)
		{
			HintService.Hint(Lang.Text("Event.Sanitize.UnsupportedTypeHint", new object[1] { item }), HintType.Error);
		}
		foreach (string unrecognizedType in sanitizeResult.UnrecognizedTypes)
		{
			HintService.Hint(Lang.Text("Event.Sanitize.UnknownTypeHint", new object[1] { unrecognizedType }), HintType.Error);
		}
		_content = content;
		PanCustom.Children.Clear();
		PanCustom.Children.Add(element);
		FormMain? frmMain = ModMain.frmMain;
		if (frmMain != null && frmMain.pageCurrent.page == FormMain.PageType.HelpDetail)
		{
			ModMain.frmMain.PageNameRefresh();
		}
	}

	public void Refresh()
	{
		try
		{
			ApplyContent(LoadContent(_content.SourcePath));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新帮助详情页失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Event.Error.ExecutionFailed", new object[2]
			{
				EventType.OpenHelp,
				_content.SourcePath
			}));
		}
	}

}
