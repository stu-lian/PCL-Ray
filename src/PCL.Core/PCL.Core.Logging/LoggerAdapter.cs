using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace PCL.Core.Logging;

public class LoggerAdapter(Logger logger, string categoryName) : ILogger
{
	private class ScopeDisposable : IDisposable
	{
		private bool _disposed;

		public ScopeDisposable(object state)
		{
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				Stack<object> value = _ScopeStack.Value;
				if (value != null && value.Count > 0)
				{
					_ScopeStack.Value.Pop();
				}
				_disposed = true;
			}
		}
	}

	private readonly Logger _innerLogger = logger ?? throw new ArgumentNullException("logger");

	private readonly string _categoryName = categoryName ?? throw new ArgumentNullException("categoryName");

	private static readonly AsyncLocal<Stack<object>> _ScopeStack = new AsyncLocal<Stack<object>>();

	IDisposable ILogger.BeginScope<TState>(TState state)
	{
		AsyncLocal<Stack<object>> scopeStack = _ScopeStack;
		if (scopeStack.Value == null)
		{
			Stack<object> stack = (scopeStack.Value = new Stack<object>());
		}
		_ScopeStack.Value.Push(state);
		return new ScopeDisposable(state);
	}

	public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level)
	{
		return true;
	}

	public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(formatter, "formatter");
		string value = formatter(state, exception);
		StringBuilder stringBuilder = new StringBuilder();
		if (!string.IsNullOrEmpty(_categoryName))
		{
			stringBuilder.Append('[').Append(_categoryName).Append("] ");
		}
		if (eventId.Id != 0 || !string.IsNullOrEmpty(eventId.Name))
		{
			stringBuilder.Append("[EventId:");
			if (!string.IsNullOrEmpty(eventId.Name))
			{
				stringBuilder.Append(eventId.Id).Append(':').Append(eventId.Name);
			}
			else
			{
				stringBuilder.Append(eventId.Id);
			}
			stringBuilder.Append("] ");
		}
		if (!string.IsNullOrEmpty(_BuildScopeContext()))
		{
			stringBuilder.Append('[').Append(_categoryName).Append("] ");
		}
		stringBuilder.Append(value);
		string message = stringBuilder.ToString();
		switch (logLevel)
		{
		case Microsoft.Extensions.Logging.LogLevel.Trace:
			_innerLogger.Trace(message);
			break;
		case Microsoft.Extensions.Logging.LogLevel.Debug:
			_innerLogger.Debug(message);
			break;
		case Microsoft.Extensions.Logging.LogLevel.Information:
			_innerLogger.Info(message);
			break;
		case Microsoft.Extensions.Logging.LogLevel.Warning:
			_innerLogger.Warn(message);
			break;
		case Microsoft.Extensions.Logging.LogLevel.Error:
			_innerLogger.Error(message);
			break;
		case Microsoft.Extensions.Logging.LogLevel.Critical:
			_innerLogger.Fatal(message);
			break;
		}
		if (exception != null)
		{
			string text = $"Exception: {exception}";
			_innerLogger.Log("[" + _categoryName + "] " + text);
		}
	}

	private string _BuildScopeContext()
	{
		Stack<object> value = _ScopeStack.Value;
		if (value == null || value.Count == 0)
		{
			return string.Empty;
		}
		IEnumerable<object> source = value.AsEnumerable().Reverse();
		return string.Join(" => ", source.Select((object s) => s.ToString()));
	}
}
