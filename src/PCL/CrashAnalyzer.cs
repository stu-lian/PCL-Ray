using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.Logging;

namespace PCL;

public sealed class CrashAnalyzer
{
	private readonly CrashLogCollector _collector;

	private readonly CrashAnalysisContext _context;

	private readonly CrashDetector _detector;

	private readonly CrashDialogPresenter _dialogPresenter;

	private readonly CrashLogImporter _importer;

	private readonly CrashLogPreparer _preparer;

	public CrashAnalyzer(int uUid)
	{
		string text = ModMain.RequestTaskTempFolder();
		Directory.CreateDirectory(Path.Combine(text, "Temp"));
		Directory.CreateDirectory(Path.Combine(text, "Report"));
		_context = new CrashAnalysisContext(uUid, text);
		_collector = new CrashLogCollector(_context);
		_importer = new CrashLogImporter(_context);
		_preparer = new CrashLogPreparer(_context);
		_detector = new CrashDetector();
		_dialogPresenter = new CrashDialogPresenter(_context);
		LogWrapper.Info("Crash", "崩溃分析暂存文件夹：" + text);
	}

	public void Collect(string versionPathIndie, IList<string>? latestLog = null)
	{
		_collector.Collect(versionPathIndie, latestLog);
	}

	public void Import(string filePath)
	{
		_importer.Import(filePath);
	}

	public bool Prepare()
	{
		return _preparer.Prepare();
	}

	public void Analyze(McInstance? version = null)
	{
		_context.Instance = version;
		CrashLogSet crashLogSet = _context.PreparedLogs ?? throw new InvalidOperationException("Prepare must be called before Analyze.");
		_context.Result = _detector.Analyze(crashLogSet, version);
		_context.LogAll = crashLogSet.All;
	}

	public void Output(bool isHandAnalyze, List<string>? extraFiles = null)
	{
		_dialogPresenter.Output(isHandAnalyze, extraFiles);
	}
}
