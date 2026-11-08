using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PCL.Core.Logging;

public static class LoggerExtensions
{
	private class PerformanceLoggerDisposable(ILogger logger, string operationName) : IDisposable
	{
		private readonly long _startTime = Stopwatch.GetTimestamp();

		public void Dispose()
		{
			TimeSpan elapsedTime = Stopwatch.GetElapsedTime(_startTime);
			logger.LogInformation("完成执行: {OperationName}, 耗时: {ElapsedMs}ms", operationName, elapsedTime.TotalMilliseconds);
		}
	}

	public static ILogger CreateLogger(this Logger logger, string categoryName)
	{
		return new LoggerAdapter(logger, categoryName);
	}

	public static ILoggerFactory CreateLoggerFactory(this Logger logger)
	{
		return new LoggerFactoryAdapter(logger);
	}

	public static void LogInformation<T0>(this ILogger logger, string message, T0 arg0)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, message, arg0);
	}

	public static void LogInformation<T0, T1>(this ILogger logger, string message, T0 arg0, T1 arg1)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, message, arg0, arg1);
	}

	public static void LogInformation<T0, T1, T2>(this ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, message, arg0, arg1, arg2);
	}

	public static void LogWarning<T0>(this ILogger logger, string message, T0 arg0)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, message, arg0);
	}

	public static void LogWarning<T0, T1>(this ILogger logger, string message, T0 arg0, T1 arg1)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, message, arg0, arg1);
	}

	public static void LogError<T0>(this ILogger logger, Exception? exception, string message, T0 arg0)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, exception, message, arg0);
	}

	public static void LogError<T0, T1>(this ILogger logger, Exception? exception, string message, T0 arg0, T1 arg1)
	{
		logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, exception, message, arg0, arg1);
	}

	public static void LogIf(this ILogger logger, bool condition, Microsoft.Extensions.Logging.LogLevel level, string message)
	{
		if (condition)
		{
			logger.Log(level, message);
		}
	}

	public static void LogIf(this ILogger logger, bool condition, Microsoft.Extensions.Logging.LogLevel level, Exception? exception, string message)
	{
		if (condition)
		{
			logger.Log(level, exception, message);
		}
	}

	public static IDisposable LogPerformance(this ILogger logger, string operationName)
	{
		logger.LogInformation("开始执行: {OperationName}", operationName);
		return new PerformanceLoggerDisposable(logger, operationName);
	}
}
