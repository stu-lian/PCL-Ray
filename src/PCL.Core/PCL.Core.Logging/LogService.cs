using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PCL.Core.App;
using PCL.Core.App.Essentials;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.UI;

namespace PCL.Core.Logging;

[LifecycleService(LifecycleState.Loading, Priority = int.MaxValue)]
public class LogService : ILifecycleLogService, ILifecycleService
{
	private static LifecycleContext? _context;

	private static Logger? _logger;

	private static bool _wrapperRegistered;

	private static LifecycleContext Context => _context;

	public static Logger Logger => _logger;

	public string Identifier => "log";

	public string Name => "日志服务";

	public bool SupportAsync => false;

	private LogService()
	{
		_context = Lifecycle.GetContext(this);
	}

	public Task StartAsync()
	{
		Context.Trace("正在初始化 Logger 实例");
		_logger = new Logger(new LoggerConfiguration(Path.Combine(Basics.ExecutableDirectory, "PCL", "Log"), 33554432L));
		Context.Trace("正在注册日志事件");
		LogWrapper.OnLog += _OnWrapperLog;
		_wrapperRegistered = true;
		return Task.CompletedTask;
	}

	public async Task StopAsync()
	{
		if (_wrapperRegistered)
		{
			LogWrapper.OnLog -= _OnWrapperLog;
		}
		if (_logger != null)
		{
			await _logger.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	public void OnLog(LifecycleLogItem item)
	{
		_LogAction(item.Level, item.ActionLevel, item.ComposeMessage(), item.Message, item.Exception);
	}

	private static void _LogAction(LogLevel level, ActionLevel actionLevel, string formatted, string plain, Exception? ex)
	{
		if (ex != null)
		{
			TelemetryService.ReportException(ex, plain, level);
		}
		if (actionLevel != ActionLevel.TraceLog)
		{
			Logger.Log(formatted);
		}
		if (actionLevel <= ActionLevel.NormalLog)
		{
			return;
		}
		if ((uint)(actionLevel - 20) > 1u)
		{
			if ((uint)(actionLevel - 30) > 1u)
			{
				if (actionLevel == ActionLevel.MsgBoxFatal)
				{
					MessageBox.Show(Lang.Text("SystemDialog.Fatal.Message.WithFeedbackGuidance", _ComposeUserError(plain, ex)), Lang.Text("SystemDialog.Fatal.Title"), MessageBoxButton.OK, MessageBoxImage.Hand);
				}
				return;
			}
			MsgBoxTheme theme = ((actionLevel == ActionLevel.MsgBoxErr) ? MsgBoxTheme.Error : MsgBoxTheme.Info);
			string caption = ((ex == null) ? null : Lang.Text("SystemDialog.Error.Unexpected.Title"));
			string text = _ComposeUserError(plain, ex);
			if (actionLevel == ActionLevel.MsgBoxErr)
			{
				text = Lang.Text("SystemDialog.Error.Message.WithLogExportGuidance", text);
			}
			MsgBoxWrapper.Show(text, caption, theme, false);
		}
		else
		{
			HintWrapper.Show(plain, (actionLevel != ActionLevel.Hint) ? HintTheme.Error : HintTheme.Info);
		}
	}

	private static string _ComposeUserError(string plain, Exception? exception)
	{
		string text = (string.IsNullOrWhiteSpace(plain) ? Lang.Text("SystemDialog.Error.Unexpected.Message") : plain);
		if (exception != null)
		{
			return ExceptionDetails.Compose(text, exception);
		}
		return text;
	}

	private static void _OnWrapperLog(LogLevel level, string msg, string? module, Exception? ex)
	{
		string value = Thread.CurrentThread.Name ?? $"#{Environment.CurrentManagedThreadId}";
		if (module != null)
		{
			module = "[" + module + "] ";
		}
		string text = $"[{DateTime.Now:HH:mm:ss.fff}] [{level.PrintName()}] [{value}] {module}{msg}";
		_LogAction(level, level.DefaultActionLevel(), (ex == null) ? text : $"{text}\n{ex}", msg, ex);
	}
}
