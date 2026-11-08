using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL;

public partial class PageToolsLogAnalyse : MyPageRight, IComponentConnector
{
	private const int _maxChars = 60000;

	private bool _suppressTextEvent;

	public PageToolsLogAnalyse()
	{
		InitializeComponent();
	}

	private void TextLogContent_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
	{
		if (_suppressTextEvent)
		{
			return;
		}
		if (TextLogContent.Text.Length > 1000000)
		{
			_suppressTextEvent = true;
			TextLogContent.Text = TextLogContent.Text.Substring(0, 1000000);
			TextLogContent.CaretIndex = TextLogContent.Text.Length;
			_suppressTextEvent = false;
		}
		LabLogChars.Text = TextLogContent.Text.Length.ToString("#,0") + " 字符";
		BtnAnalyse.IsEnabled = !string.IsNullOrWhiteSpace(TextLogContent.Text);
	}

	private void BtnChooseLog_Click(object sender, MouseButtonEventArgs e)
	{
		string filter = Lang.Text("Tools.LogAnalyse.FileFilter");
		string file = SystemDialogs.SelectFile(filter, Lang.Text("Tools.LogAnalyse.ChooseFileTitle"));
		if (string.IsNullOrEmpty(file))
		{
			return;
		}
		try
		{
			string content = System.IO.File.ReadAllText(file);
			_suppressTextEvent = true;
			TextLogContent.Text = content.Length > 1000000 ? content.Substring(content.Length - 1000000) : content;
			_suppressTextEvent = false;
			TextLogContent_TextChanged(null, null);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Tools.LogAnalyse.ReadError"), ModBase.LogLevel.Msgbox);
		}
	}

	private const string SystemPrompt =
		"你是一个专业的 Minecraft 启动器日志分析助手。用户会给你一段 PCL Ray / Minecraft 的日志，" +
		"请找出导致崩溃、无法启动或明显异常的原因。\n" +
		"只输出一个 JSON 对象，不要输出多余的说明文字，格式如下：\n" +
		"{\"type\":\"日志类型（例如 Java 版本不兼容 / 模组冲突 / 内存不足 / 正常）\"," +
		"\"summary\":\"一句话总结\"," +
		"\"problems\":[{\"label\":\"问题标题\",\"message\":\"具体说明与修复建议\"}]}\n" +
		"如果日志看起来正常，problems 给空数组。所有内容使用简体中文。";

	private async void BtnAnalyse_Click(object sender, MouseButtonEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(TextLogContent.Text))
		{
			return;
		}
		BtnAnalyse.IsEnabled = false;
		BtnAnalyse.Text = Lang.Text("Common.State.Loading");
		LabAnalyseType.Text = "";
		LabAnalyseProblems.Text = "";
		try
		{
			string content = TextLogContent.Text;
			if (content.Length > _maxChars)
			{
				// 保留头部（版本/系统信息）与尾部（崩溃栈），中间截断
				int headSize = _maxChars / 4;
				int tailSize = _maxChars - headSize - 32;
				content = content.Substring(0, headSize) +
					"\n\n...[日志过长，已省略中间部分]...\n\n" +
					content.Substring(content.Length - tailSize);
			}

			string text = await AiSupport.ChatAsync(SystemPrompt, content, 0.2, 60);
			ShowResult(text);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Tools.LogAnalyse.Failed"), ModBase.LogLevel.Feedback, null, Lang.Text("Tools.LogAnalyse.Failed"));
		}
		finally
		{
			BtnAnalyse.IsEnabled = true;
			BtnAnalyse.Text = Lang.Text("Tools.LogAnalyse.Analyse");
		}
	}

	private void ShowResult(string text)
	{
		JsonObject? result = null;
		try
		{
			string json = text.Trim();
			// 兼容模型使用 ```json 代码块包裹的情况
			Match fenced = Regex.Match(json, "\\{[\\s\\S]*\\}");
			if (fenced.Success)
			{
				json = fenced.Value;
			}
			result = ModBase.GetJson(json)?.AsObject();
		}
		catch
		{
			result = null;
		}

		if (result == null)
		{
			// 模型没按格式返回，直接展示原文
			LabAnalyseType.Text = Lang.Text("Tools.LogAnalyse.UnknownType");
			LabAnalyseProblems.Text = text.Trim();
			BtnCopyResult.Visibility = Visibility.Visible;
			return;
		}

		LabAnalyseType.Text = result["type"]?.GetValue<string>()?.Trim() ?? Lang.Text("Tools.LogAnalyse.UnknownType");

		var lines = new System.Collections.Generic.List<string>();
		string? summary = result["summary"]?.GetValue<string>();
		if (!string.IsNullOrWhiteSpace(summary))
		{
			lines.Add(summary.Trim());
		}
		JsonArray? problems = result["problems"]?.AsArray();
		if (problems != null && problems.Count > 0)
		{
			foreach (JsonNode? node in problems)
			{
				JsonObject? obj = node as JsonObject;
				string label = obj?["label"]?.GetValue<string>() ?? "";
				string message = obj?["message"]?.GetValue<string>() ?? "";
				lines.Add((string.IsNullOrWhiteSpace(label) ? "" : label.Trim() + "：") + message.Trim());
			}
		}
		LabAnalyseProblems.Text = lines.Count > 0 ? string.Join("\r\n\r\n", lines) : Lang.Text("Tools.LogAnalyse.NoProblems");
		BtnCopyResult.Visibility = Visibility.Visible;
	}

	private void BtnCopyResult_Click(object sender, MouseButtonEventArgs e)
	{
		string summary = Lang.Text("Tools.LogAnalyse.LogType") + LabAnalyseType.Text + "\r\n\r\n" + LabAnalyseProblems.Text;
		ModBase.ClipboardSet(summary);
	}
}
