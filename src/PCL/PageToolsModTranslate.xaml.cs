using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net;
using PCL.Core.UI;

namespace PCL;

public partial class PageToolsModTranslate : MyPageRight, IComponentConnector
{
	private const int ItemsPerBatch = 20;

	private static readonly string[] LangCodes = { "zh-CN", "zh-TW", "ja", "ko" };
	private static readonly string[] LangFileNames = { "zh_cn", "zh_tw", "ja_jp", "ko_kr" };

	public PageToolsModTranslate()
	{
		InitializeComponent();
	}

	private string CurrentTargetCode()
	{
		int index = ComboTargetLang.SelectedIndex;
		if (index < 0 || index >= LangCodes.Length)
		{
			index = 0;
		}
		return LangCodes[index];
	}

	private string CurrentLangFileName()
	{
		int index = ComboTargetLang.SelectedIndex;
		if (index < 0 || index >= LangFileNames.Length)
		{
			index = 0;
		}
		return LangFileNames[index];
	}

	private void DropZone_DragEnter(object? sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop) && IsJarFile(((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault()))
		{
			e.Effects = DragDropEffects.Copy;
		}
		else
		{
			e.Effects = DragDropEffects.None;
		}
		e.Handled = true;
	}

	private void DropZone_DragLeave(object? sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void DropZone_Click(object? sender, MouseButtonEventArgs e)
	{
		SelectJar();
	}

	private void DropZone_Drop(object? sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string path = ((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault() ?? "";
			if (IsJarFile(path))
			{
				_ = TranslateJarAsync(path);
			}
		}
		e.Handled = true;
	}

	private void BtnTranslate_Click(object? sender, MouseButtonEventArgs e)
	{
		SelectJar();
	}

	private void SelectJar()
	{
		string path = SystemDialogs.SelectFile("JAR 文件|*.jar|ZIP 文件|*.zip", Lang.Text("Tools.ModTranslate.Title"));
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		_ = TranslateJarAsync(path);
	}

	private static bool IsJarFile(string? path)
	{
		string ext = Path.GetExtension(path ?? "").ToLowerInvariant();
		return ext is ".jar" or ".zip";
	}

	private async Task TranslateJarAsync(string jarPath)
	{
		BtnTranslate.IsEnabled = false;
		ProgressBar.Value = 0;
		LabJarPath.Text = jarPath;
		LabStatus.Text = Lang.Text("Tools.ModTranslate.Started");
		try
		{
			string targetLang = CurrentLangFileName();
			string targetCode = CurrentTargetCode();
			using (ZipArchive source = ZipFile.OpenRead(jarPath))
			{
				string outputPath = GetOutputJarPath(jarPath, targetLang);
				List<LangEntry> entries = CollectLangEntries(source);
				if (entries.Count == 0)
				{
					LabStatus.Text = Lang.Text("Tools.ModTranslate.NoLangFile");
					return;
				}

				LabStatus.Text = string.Format(Lang.Text("Tools.ModTranslate.Reading"), entries.Count, entries.Sum(e => e.Items.Count));
				List<KeyValuePair<string, string>> allItems = entries.SelectMany(e => e.Items).Distinct().ToList();
				var translated = new Dictionary<string, string>();
				for (int i = 0; i < allItems.Count; i += ItemsPerBatch)
				{
					List<KeyValuePair<string, string>> batchItems = allItems.Skip(i).Take(ItemsPerBatch).ToList();
					List<string> values = batchItems.Select(kv => kv.Value).ToList();
					List<string> translatedValues = await TranslateLinesAsync(values, targetCode);
					for (int j = 0; j < batchItems.Count; j++)
					{
						string translatedText = translatedValues.ElementAtOrDefault(j) ?? batchItems[j].Value;
						translated[batchItems[j].Key] = translatedText;
					}
					ProgressBar.Value = Math.Min(100, (int)((double)(i + ItemsPerBatch) * 95 / allItems.Count));
					LabStatus.Text = string.Format(Lang.Text("Tools.ModTranslate.BatchProgress"), Math.Min(i + ItemsPerBatch, allItems.Count), allItems.Count);
					await Task.Yield();
				}

				LabStatus.Text = Lang.Text("Tools.ModTranslate.Writing");
				WriteTranslatedJar(source, outputPath, entries, translated, targetLang);
				ProgressBar.Value = 100;
				LabStatus.Text = string.Format(Lang.Text("Tools.ModTranslate.OutputFile"), outputPath);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Tools.ModTranslate.Failed"), ModBase.LogLevel.Feedback, null, Lang.Text("Tools.ModTranslate.Failed"));
		}
		finally
		{
			BtnTranslate.IsEnabled = true;
		}
	}

	private static string GetOutputJarPath(string sourcePath, string targetLang)
	{
		string dir = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Environment.CurrentDirectory;
		string fileName = Path.GetFileNameWithoutExtension(sourcePath);
		return Path.Combine(dir, fileName + "-" + targetLang + ".jar");
	}

	private static List<LangEntry> CollectLangEntries(ZipArchive archive)
	{
		var result = new List<LangEntry>();
		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			string name = entry.FullName.Replace('\\', '/');
			if (!name.StartsWith("assets/") || !name.Contains("/lang/"))
			{
				continue;
			}
			string language = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
			if (!language.StartsWith("en_"))
			{
				continue;
			}
			if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".lang", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			using Stream stream = entry.Open();
			using StreamReader reader = new(stream, Encoding.UTF8);
			string content = reader.ReadToEnd();
			if (string.IsNullOrWhiteSpace(content))
			{
				continue;
			}
			LangEntry lang = new()
			{
				Path = name,
				IsJson = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase),
				Items = ParseLangContent(content, name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			};
			if (lang.Items.Count > 0)
			{
				result.Add(lang);
			}
		}
		return result;
	}

	private static List<KeyValuePair<string, string>> ParseLangContent(string content, bool isJson)
	{
		var items = new List<KeyValuePair<string, string>>();
		if (isJson)
		{
			try
			{
				JsonObject? root = ModBase.GetJson(content)?.AsObject();
				if (root == null)
				{
					return items;
				}
				foreach (KeyValuePair<string, JsonNode?> pair in root)
				{
					if (pair.Value is JsonValue value && value.TryGetValue<string>(out string? text) && !string.IsNullOrEmpty(text))
					{
						items.Add(new KeyValuePair<string, string>(pair.Key, text));
					}
				}
			}
			catch
			{
				// 忽略无法解析的语言文件
			}
			return items;
		}

		foreach (string line in content.Replace("\r\n", "\n").Split('\n'))
		{
			string trimmed = line.Trim();
			if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
			{
				continue;
			}
			int separator = trimmed.IndexOf('=');
			if (separator <= 0)
			{
				continue;
			}
			string key = trimmed[..separator].Trim();
			string value = trimmed[(separator + 1)..].Trim();
			if (!string.IsNullOrEmpty(value))
			{
				items.Add(new KeyValuePair<string, string>(key, value));
			}
		}
		return items;
	}

	private static string TargetDisplayName(string code)
	{
		return code switch
		{
			"zh-CN" => "简体中文",
			"zh-TW" => "繁体中文",
			"ja" => "日语",
			"ko" => "韩语",
			_ => "简体中文",
		};
	}

	/// <summary>
	/// 用 AI 批量翻译一组文本，保持条目数与顺序完全一致。
	/// 返回条数与输入不一致时，自动缩小批次重试。
	/// </summary>
	private async Task<List<string>> TranslateLinesAsync(List<string> lines, string targetCode)
	{
		if (lines.Count == 0)
		{
			return new List<string>();
		}
		string system =
			"你是 Minecraft 模组本地化翻译。把用户给出的每一行英文文本翻译成" + TargetDisplayName(targetCode) + "。\n" +
			"严格规则：\n" +
			"1. 输入是一行一条，输出也必须一行一条，行数必须与输入完全一致，不能合并或拆分。\n" +
			"2. 保留 %s %d %1$s {0} 等格式占位符、颜色代码（§/&）、命令、URL、代码标识符原样不变。\n" +
			"3. 不要输出解释、编号、引号或任何多余内容，只输出译文行。";
		string joined = string.Join("\n", lines);
		string result;
		try
		{
			result = await AiSupport.ChatAsync(system, joined, 0.3, 60);
		}
		catch
		{
			// 整批失败时退回原文，避免整份输出被阻断
			return new List<string>(lines);
		}
		string[] split = result.Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n').Split('\n');
		if (split.Length == lines.Count)
		{
			return split.ToList();
		}
		if (lines.Count == 1)
		{
			return new List<string> { split.Length > 0 ? split[0] : lines[0] };
		}
		// 行数对不上：对半拆开重试，保证不串行
		int half = lines.Count / 2;
		List<string> head = await TranslateLinesAsync(lines.Take(half).ToList(), targetCode);
		List<string> tail = await TranslateLinesAsync(lines.Skip(half).ToList(), targetCode);
		head.AddRange(tail);
		return head;
	}

	private static void WriteTranslatedJar(ZipArchive source, string outputPath, List<LangEntry> entries, Dictionary<string, string> translated, string targetLang)
	{
		string tempPath = outputPath + ".tmp";
		File.Delete(tempPath);
		try
		{
			using (FileStream output = new(tempPath, FileMode.CreateNew, FileAccess.Write))
			using (ZipArchive target = new(output, ZipArchiveMode.Create))
			{
				foreach (ZipArchiveEntry sourceEntry in source.Entries)
				{
					ZipArchiveEntry targetEntry = target.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
					using Stream sourceStream = sourceEntry.Open();
					using Stream targetStream = targetEntry.Open();
					sourceStream.CopyTo(targetStream);
				}

				foreach (LangEntry entry in entries)
				{
					string targetPath = LangTargetPath(entry.Path, targetLang);
					ZipArchiveEntry targetEntry = target.CreateEntry(targetPath, CompressionLevel.Optimal);
					using Stream targetStream = targetEntry.Open();
					WriteLangContent(targetStream, entry, translated, targetLang);
				}
			}
			File.Delete(outputPath);
			File.Move(tempPath, outputPath);
		}
		catch
		{
			File.Delete(tempPath);
			throw;
		}
	}

	private static string LangTargetPath(string sourcePath, string targetLang)
	{
		string name = Path.GetFileNameWithoutExtension(sourcePath);
		string extension = Path.GetExtension(sourcePath);
		return sourcePath.Replace(name + extension, targetLang + extension);
	}

	private static void WriteLangContent(Stream stream, LangEntry entry, Dictionary<string, string> translated, string targetLang)
	{
		if (entry.IsJson)
		{
			JsonObject root = new();
			foreach (KeyValuePair<string, string> item in entry.Items)
			{
				root[item.Key] = translated.TryGetValue(item.Key, out string? value) ? value : item.Value;
			}
			byte[] bytes = Encoding.UTF8.GetBytes(root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
			stream.Write(bytes, 0, bytes.Length);
			return;
		}

		StringBuilder builder = new();
		foreach (KeyValuePair<string, string> item in entry.Items)
		{
			translated.TryGetValue(item.Key, out string? value);
			builder.Append(item.Key).Append('=').Append(value ?? item.Value).Append('\n');
		}
		byte[] langBytes = Encoding.UTF8.GetBytes(builder.ToString());
		stream.Write(langBytes, 0, langBytes.Length);
	}

	private sealed class LangEntry
	{
		public string Path { get; init; } = "";

		public bool IsJson { get; init; }

		public List<KeyValuePair<string, string>> Items { get; init; } = new();
	}
}