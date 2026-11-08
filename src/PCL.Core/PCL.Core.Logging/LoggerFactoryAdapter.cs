using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PCL.Core.Logging;

public class LoggerFactoryAdapter(Logger logger) : ILoggerFactory, IDisposable
{
	private readonly Logger _logger = logger ?? throw new ArgumentNullException("logger");

	private readonly List<IDisposable> _disposables = new List<IDisposable>();

	public void AddProvider(ILoggerProvider provider)
	{
		_disposables.Add(provider);
	}

	public ILogger CreateLogger(string categoryName)
	{
		return new LoggerAdapter(_logger, categoryName);
	}

	public void Dispose()
	{
		foreach (IDisposable disposable in _disposables)
		{
			disposable.Dispose();
		}
		_disposables.Clear();
	}
}
