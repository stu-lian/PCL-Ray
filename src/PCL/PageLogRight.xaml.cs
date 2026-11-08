using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL;

public partial class PageLogRight : MyPageRight, IComponentConnector
{
	public Run labDebug;

	public Run labError;

	public Run labFatal;

	public Run labInfo;

	public Run labWarn;

	public PageLogRight()
	{
		Initialized += (object? _, EventArgs _) =>
		{
			Init();
		};
		Loaded += PageLogRight_Loaded;
		InitializeComponent();
	}

	public void Init()
	{
		PanLogCard.Inlines.Clear();
		PanLogCard.Inlines.Add(new Run(Lang.Text("LogPage.Title")));
		PanLogCard.Inlines.Add(new Run(" | "));
		labDebug = new Run("0 " + Lang.Text("LogPage.Level.Debug"))
		{
			Foreground = (Brush)System.Windows.Application.Current.Resources["ColorBrushDebug"]
		};
		PanLogCard.Inlines.Add(labDebug);
		PanLogCard.Inlines.Add(new Run(" | "));
		labInfo = new Run("0 " + Lang.Text("LogPage.Level.Info"))
		{
			Foreground = (Brush)System.Windows.Application.Current.Resources[ThemeManager.IsDarkMode ? "ColorBrushInfoDark" : "ColorBrushInfo"]
		};
		PanLogCard.Inlines.Add(labInfo);
		PanLogCard.Inlines.Add(new Run(" | "));
		labWarn = new Run("0 " + Lang.Text("LogPage.Level.Warn"))
		{
			Foreground = (Brush)System.Windows.Application.Current.Resources["ColorBrushWarn"]
		};
		PanLogCard.Inlines.Add(labWarn);
		PanLogCard.Inlines.Add(new Run(" | "));
		labError = new Run("0 " + Lang.Text("LogPage.Level.Error"))
		{
			Foreground = (Brush)System.Windows.Application.Current.Resources["ColorBrushError"]
		};
		PanLogCard.Inlines.Add(labError);
		PanLogCard.Inlines.Add(new Run(" | "));
		labFatal = new Run("0 " + Lang.Text("LogPage.Level.Fatal"))
		{
			Foreground = (Brush)System.Windows.Application.Current.Resources["ColorBrushFatal"]
		};
		PanLogCard.Inlines.Add(labFatal);
	}

	private void PageLogRight_Loaded(object sender, RoutedEventArgs e)
	{
		ModAnimation.AniControlEnabled++;
		Reload();
		ModAnimation.AniControlEnabled--;
	}

	public void Reload()
	{
		if (ModMain.frmLogLeft.currentLog == null || ModMain.frmLogLeft.currentUuid <= 0 || ModMain.frmLogLeft.shownLogs.Count == 0)
		{
			ModMain.frmMain.PageChange(ModMain.frmMain.pageCurrent);
			return;
		}
		PanAllBack.Visibility = Visibility.Visible;
		CardOperation.Visibility = Visibility.Visible;
		BtnOperationKill.IsEnabled = !ModMain.frmLogLeft.currentLog.gameProcess.HasExited;
		BtnOperationExportStackDump.IsEnabled = !ModMain.frmLogLeft.currentLog.gameProcess.HasExited && !string.IsNullOrWhiteSpace(ModMain.frmLogLeft.currentLog.jStackPath);
		SliderMaxLog.Value = Config.System.MaxGameLog;
		SliderMaxLog.getHintText = (Func<object, object>)((object v) =>
		{
			if ((int)v <= 5)
			{
				return ((int)v * 10 + 50).ToString();
			}
			if ((int)v <= 13)
			{
				return ((int)v * 50 - 150).ToString();
			}
			return ((int)v <= 28) ? ((int)v * 100 - 800).ToString() : Lang.Text("LogPage.MaxLines.Unlimited");
		});
		PanLog.Document = ModMain.frmLogLeft.flowDocuments[ModMain.frmLogLeft.currentUuid];
		ModMain.frmLogLeft.currentLog.LogOutput += OnLogOutput;
		ModMain.frmLogLeft.currentLog.GameExit += OnGameExit;
		RefreshLabText();
	}

	private void RefreshLabText()
	{
		labFatal.Text = $"{ModMain.frmLogLeft.currentLog.countFatal} {Lang.Text("LogPage.Level.Fatal")}";
		labError.Text = $"{ModMain.frmLogLeft.currentLog.countError} {Lang.Text("LogPage.Level.Error")}";
		labWarn.Text = $"{ModMain.frmLogLeft.currentLog.countWarn} {Lang.Text("LogPage.Level.Warn")}";
		labInfo.Text = $"{ModMain.frmLogLeft.currentLog.countInfo} {Lang.Text("LogPage.Level.Info")}";
		labDebug.Text = $"{ModMain.frmLogLeft.currentLog.countDebug} {Lang.Text("LogPage.Level.Debug")}";
	}

	private void OnLogOutput(ModWatcher.Watcher sender, ModWatcher.LogOutputEventArgs e)
	{
		ModBase.RunInUi(() =>
		{
			if (ModMain.frmLogLeft.currentLog != null)
			{
				if (CheckAutoScroll.Checked == true)
				{
					PanBack.ScrollToBottom();
				}
				RefreshLabText();
			}
		});
	}

	private void SliderMaxLog_ValueChanged(object o, bool user)
	{
		MySlider mySlider = (MySlider)o;
		Config.System.MaxGameLog = mySlider.Value;
		if (ModMain.frmSetupLauncherMisc != null)
		{
			ModMain.frmSetupLauncherMisc.SliderMaxLog.Value = mySlider.Value;
		}
	}

	private void BtnOperationClear_Click(object sender, ModBase.RouteEventArgs e)
	{
		ModMain.frmLogLeft.flowDocuments[ModMain.frmLogLeft.currentUuid].Blocks.Clear();
	}

	private void BtnOperationExport_Click(object sender, ModBase.RouteEventArgs e)
	{
		string text = SystemDialogs.SelectSaveFile(Lang.Text("LogPage.Export.SelectLocation"), Lang.Text("LogPage.Export.GameLog.FileName", new object[1] { ModMain.frmLogLeft.currentLog.version.Name }), Lang.Text("LogPage.Export.GameLog.Filter"), (string)null);
		if (text.Length >= 3)
		{
			File.WriteAllLines(text, ModMain.frmLogLeft.currentLog.fullLog);
			HintService.Hint(Lang.Text("LogPage.Export.Success"), HintType.Success);
			ModBase.OpenExplorer(text);
		}
	}

	private void BtnOperationKill_Click(object sender, ModBase.RouteEventArgs e)
	{
		if (ModMain.frmLogLeft.currentLog.State <= ModWatcher.Watcher.MinecraftState.Running)
		{
			ModMain.frmLogLeft.currentLog.Kill();
			HintService.Hint(Lang.Text("LogPage.Action.GameClosed", new object[1] { ModMain.frmLogLeft.currentLog.version.Name }), HintType.Success);
		}
	}

	private void BtnOperationExportStackDump_Click(object sender, ModBase.RouteEventArgs e)
	{
		string text = DateTime.Now.ToString("G", CultureInfo.InvariantCulture).Replace("/", "-").Replace(":", ".")
			.Replace(" ", "_");
		string savePath = SystemDialogs.SelectSaveFile(Lang.Text("LogPage.Export.SelectLocation"), Lang.Text("LogPage.ExportStack.FileName", new object[1] { text }), Lang.Text("LogPage.ExportStack.Filter"), (string)null);
		if (savePath.Length < 3)
		{
			return;
		}
		HintService.Hint(Lang.Text("LogPage.ExportStack.Progress"));
		BtnOperationExportStackDump.IsEnabled = false;
		ModBase.RunInNewThread(() =>
		{
			List<string> contents = ModMain.frmLogLeft.currentLog.ExportStackDump(savePath);
			File.WriteAllLines(savePath, contents);
			ModBase.RunInUi(() =>
			{
				HintService.Hint(Lang.Text("LogPage.ExportStack.Success"), HintType.Success);
				BtnOperationExportStackDump.IsEnabled = true;
			});
			ModBase.OpenExplorer(savePath);
		});
	}

	private void OnGameExit()
	{
		ModBase.RunInUi(() =>
		{
			BtnOperationKill.IsEnabled = false;
		});
		ModBase.RunInUi(() =>
		{
			BtnOperationExportStackDump.IsEnabled = false;
		});
	}

}
