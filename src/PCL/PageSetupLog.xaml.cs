using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;

namespace PCL;

public partial class PageSetupLog : MyPageRight, IComponentConnector
{
	private static string LogDirectory => LogService.Logger.Configuration.StoreFolder;

	private static List<string> CurrentLogs => LogService.Logger.CurrentLogFiles.Select((string item) => Path.GetFullPath(item)).ToList();

	public PageSetupLog()
	{
		InitializeComponent();
		Loaded += PageOtherLog_Loaded;
	}

	private void PageOtherLog_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		LoadList();
		_ = IsLoaded;
	}

	public void LoadList()
	{
		PanList.Children.Clear();
		List<string> currentLogs = CurrentLogs;
		string[] array = (from f in Directory.GetFiles(LogDirectory)
			orderby File.GetLastWriteTime(f) descending
			select f).ToArray();
		foreach (string path in array)
		{
			string fullPath = Path.GetFullPath(path);
			string text = Path.GetFileName(path);
			if (text.StartsWith("Launch"))
			{
				text = text.Substring(7, text.Length - 11);
				if (DateTime.TryParseExact(text, "yyyy-M-d-HHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					text = Lang.Date(result, "G");
				}
				if (currentLogs.Any((string log) => log.Equals(fullPath)))
				{
					text = Lang.Text("Setup.Log.CurrentSuffix", new object[1] { text });
				}
			}
			else if (text.StartsWith("LastPending"))
			{
				text = text.Substring(11, text.Length - 15);
				text = ((text.Length <= 1) ? Lang.Text("Setup.Log.TempUnoutput") : Lang.Text("Setup.Log.TempStored", new object[1] { text.Substring(1) }));
			}
			MyListItem myListItem = new MyListItem
			{
				Type = MyListItem.CheckType.Clickable,
				Title = text,
				Info = fullPath,
				Tag = fullPath
			};
			myListItem.Click += (object sender, MouseButtonEventArgs e) =>
			{
				Basics.OpenPath((string)((MyListItem)sender).Tag, (string)null);
			};
			PanList.Children.Add(myListItem);
		}
	}

	private static void ExportLog(IEnumerable<string> sourceFiles)
	{
		string text = Lang.Text("Setup.Log.ExportFilter");
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		string text2 = "PCL_CE_Logs_" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		string text3 = text2 + ".tmp";
		string text4 = text2 + ".zip";
		string text5 = SystemDialogs.SelectSaveFile(Lang.Text("Setup.Log.ExportSaveTitle"), text4, text, folderPath);
		if (string.IsNullOrEmpty(text5))
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(text3);
			if (File.Exists(text5))
			{
				File.Delete(text5);
			}
			using (ZipArchive destination = ZipFile.Open(text5, ZipArchiveMode.Create))
			{
				foreach (string sourceFile in sourceFiles)
				{
					string fileName = Path.GetFileName(sourceFile);
					string text6 = Path.Combine(text3, fileName);
					File.Copy(sourceFile, text6);
					destination.CreateEntryFromFile(text6, fileName, CompressionLevel.Fastest);
					File.Delete(text6);
				}
			}
			HintService.Hint(Lang.Text("Setup.Log.ExportSuccess"), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.Log.ExportFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Setup.Log.ExportFailed"));
		}
		finally
		{
			if (Directory.Exists(text3))
			{
				Directory.Delete(text3);
			}
		}
	}

	private void ButtonOpenDir_OnClick(object sender, MouseButtonEventArgs e)
	{
		Basics.OpenPath(LogDirectory, (string)null);
	}

	private void ButtonClean_OnClick(object sender, MouseButtonEventArgs e)
	{
		if (ModMain.MyMsgBox(Lang.Text("Setup.Log.Clear.Confirm.Message"), Lang.Text("Setup.Log.Clear.Confirm.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) != 1)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(CurrentLogs);
		string[] files = Directory.GetFiles(LogDirectory);
		foreach (string text in files)
		{
			if (!hashSet.Contains(text))
			{
				File.Delete(text);
			}
		}
		HintService.Hint(Lang.Text("Setup.Log.Clear.Success"), HintType.Success);
		LoadList();
	}

	private void ButtonExportAll_OnClick(object sender, MouseButtonEventArgs e)
	{
		ExportLog(Directory.GetFiles(LogDirectory));
	}

	private void ButtonExport_OnClick(object sender, MouseButtonEventArgs e)
	{
		string[] second = Array.FindAll(Directory.GetFiles(LogDirectory), (string s) => StringExtension.IsMatch(s, RegexPatterns.LastPendingLogPath));
		ExportLog(CurrentLogs.Concat(second));
	}

}
