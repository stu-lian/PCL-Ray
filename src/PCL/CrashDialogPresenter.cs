using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;

namespace PCL;

internal sealed class CrashDialogPresenter(CrashAnalysisContext context)
{
	private readonly CrashReportExporter _exporter = new CrashReportExporter();

	private readonly CrashResultFormatter _formatter = new CrashResultFormatter();

	public void Output(bool isHandAnalyze, List<string>? extraFiles)
	{
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected Obj, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected Obj, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected Obj, but got Unknown
		ModMain.frmMain.ShowWindowToTop();
		CrashDialogContent crashDialogContent = _formatter.Format(context, isHandAnalyze);
		string text = crashDialogContent.Text;
		CrashLogEntry directOpenFile = context.DirectOpenFile;
		bool flag = (object)context.Instance != null && crashDialogContent.SuggestedAction == CrashSuggestedAction.OpenInstanceSettings;
		string text2 = (isHandAnalyze ? Lang.Text("Crash.Dialog.Title.Manual") : Lang.Text("Crash.Dialog.Title.Auto"));
		string text3 = _GetSecondButtonText(isHandAnalyze, directOpenFile, flag);
		string text4 = (isHandAnalyze ? "" : Lang.Text("Crash.Dialog.Button.ExportReport"));
		Action action = _GetSecondButtonAction(isHandAnalyze, directOpenFile, flag);
		switch (MsgBoxWrapper.ShowWithCustomButtons(text, text2, (MsgBoxTheme)0, true, new MsgBoxButtonInfo[3]
		{
			new MsgBoxButtonInfo(Lang.Text("Common.Action.Confirm"), 1, (Action)null),
			new MsgBoxButtonInfo(text3, 2, action),
			new MsgBoxButtonInfo(text4, 3, (Action)null)
		}))
		{
		case 2:
			if (flag)
			{
				_OpenModLoaderInstallPage();
			}
			else if (directOpenFile != null)
			{
				_OpenDirectFile(directOpenFile);
			}
			break;
		case 3:
			_ExportReport(extraFiles);
			break;
		}
	}

	private static string _GetSecondButtonText(bool isHandAnalyze, CrashLogEntry? directFile, bool openInstanceSettings)
	{
		if (isHandAnalyze || directFile == null)
		{
			return "";
		}
		if (!openInstanceSettings)
		{
			return Lang.Text("Crash.Dialog.Button.OpenLog");
		}
		return Lang.Text("Crash.Dialog.Button.GoToModify");
	}

	private static Action? _GetSecondButtonAction(bool isHandAnalyze, CrashLogEntry? directFile, bool openInstanceSettings)
	{
		if ((isHandAnalyze || directFile == null) | openInstanceSettings)
		{
			return null;
		}
		return () =>
		{
			_OpenDirectFile(directFile);
		};
	}

	private void _OpenModLoaderInstallPage()
	{
		PageInstanceLeft.McInstance = context.Instance;
		ModBase.RunInUi(() =>
		{
			ModMain.frmMain.PageChange(FormMain.PageType.InstanceSetup, FormMain.PageSubType.DownloadOptiFine);
		});
	}

	private static void _OpenDirectFile(CrashLogEntry directFile)
	{
		if (File.Exists(directFile.FullPath))
		{
			Basics.OpenPath(directFile.FullPath, (string)null);
			return;
		}
		string text = Path.Combine(Paths.Temp, "Crash.txt");
		CrashFileIo.WriteText(text, string.Join("\r\n", directFile.Lines));
		Basics.OpenPath(text, (string)null);
	}

	private void _ExportReport(List<string>? extraFiles)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected Obj, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected Obj, but got Unknown
		string text = null;
		try
		{
			text = _SelectReportSavePath();
			if (!string.IsNullOrEmpty(text))
			{
				_exporter.Export(context, text, extraFiles);
				HintWrapper.Show(Lang.Text("Crash.Report.Export.Success"), (HintTheme)1);
				Basics.OpenPath(Path.GetDirectoryName(text) ?? text, (string)null);
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Crash", "导出错误报告失败");
			string message = _CreateExportFailureMessage(text, ex);
			MsgBoxWrapper.ShowWithCustomButtons(message, Lang.Text("Crash.Export.Failed.Title"), (MsgBoxTheme)2, false, new MsgBoxButtonInfo[2]
			{
				new MsgBoxButtonInfo(Lang.Text("Common.Action.Confirm"), 1, (Action)null),
				new MsgBoxButtonInfo(Lang.Text("Crash.Export.Failed.CopyDetails"), 2, (Action)(() =>
				{
					ModBase.ClipboardSet(message, showSuccessHint: false);
				}))
			});
		}
	}

	private static string _CreateExportFailureMessage(string? targetZipPath, Exception exception)
	{
		return ExceptionDetails.Compose(string.IsNullOrWhiteSpace(targetZipPath) ? Lang.Text("Crash.Export.Failed.MessageWithoutPath") : Lang.Text("Crash.Export.Failed.Message", new object[1] { targetZipPath }), exception);
	}

	private static string? _SelectReportSavePath()
	{
		string fileAddress = null;
		ModBase.RunInUiWait(() => fileAddress = SystemDialogs.SelectSaveFile(Lang.Text("Crash.Report.SaveDialog.Title"), _GetDefaultReportFileName(), Lang.Text("Crash.Report.SaveDialog.Filter"), (string)null));
		return fileAddress;
	}

	private static string _GetDefaultReportFileName()
	{
		string text = DateTime.Now.ToString("G", CultureInfo.InvariantCulture).Replace("/", "-").Replace(":", ".")
			.Replace(" ", "_");
		return Lang.Text("Crash.Report.SaveDialog.DefaultFileName", new object[1] { text });
	}
}
