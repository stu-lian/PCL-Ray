using System;
using PCL.Core.Logging;

namespace PCL.Core.App.IoC;

public class LifecycleContext(ILifecycleService service, Action<LifecycleLogItem> onLog, Action<int> onRequestExit, Action<string?> onRequestRestart, Action onDeclareStopped, Action onRequestStopLoading)
{
	public ILifecycleService ServiceInstance => service;

	public void CustomLog(string message, Exception? ex = null, LogLevel level = LogLevel.Trace, ActionLevel? actionLevel = null)
	{
		onLog(new LifecycleLogItem(service, message, ex, level, actionLevel ?? level.DefaultActionLevel()));
	}

	public void Trace(string message, Exception? ex = null, ActionLevel? actionLevel = null)
	{
		CustomLog(message, ex, LogLevel.Trace, actionLevel);
	}

	public void Debug(string message, Exception? ex = null, ActionLevel? actionLevel = null)
	{
		CustomLog(message, ex, LogLevel.Debug, actionLevel);
	}

	public void Info(string message, Exception? ex = null, ActionLevel? actionLevel = null)
	{
		CustomLog(message, ex, LogLevel.Info, actionLevel);
	}

	public void Warn(string message, Exception? ex = null, ActionLevel? actionLevel = null)
	{
		CustomLog(message, ex, LogLevel.Warning, actionLevel);
	}

	public void Error(string message, Exception? ex = null, ActionLevel? actionLevel = null)
	{
		CustomLog(message, ex, LogLevel.Error, actionLevel);
	}

	public void Fatal(string message, Exception? ex = null, ActionLevel? actionLevel = null)
	{
		CustomLog(message, ex, LogLevel.Fatal, actionLevel);
	}

	public void RequestExit(int statusCode = 0)
	{
		onRequestExit(statusCode);
	}

	public void RequestRestartOnExit(string? arguments = null)
	{
		onRequestRestart(arguments);
	}

	public void DeclareStopped()
	{
		onDeclareStopped();
	}

	public void RequestStopLoading()
	{
		onRequestStopLoading();
	}
}
