using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PCL.Core.Logging;

public sealed class Logger : IAsyncDisposable
{
	private StreamWriter? _currentStream;

	private FileStream? _currentFile;

	private readonly List<string> _files = new List<string>();

	private long _droppedCount;

	private readonly Task _processingTask;

	private readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
	{
		SingleReader = true
	});

	private bool _disposed;

	public long DroppedLogCount => Interlocked.Read(in _droppedCount);

	public ReadOnlyCollection<string> CurrentLogFiles => _files.AsReadOnly();

	public LoggerConfiguration Configuration { get; }

	public Logger(LoggerConfiguration configuration)
	{
		Configuration = configuration;
		_CreateNewFile();
		_processingTask = _ProcessLogQueueAsync();
	}

	private void _CreateNewFile()
	{
		DateTime now = DateTime.Now;
		string text = Path.Combine(path2: ((Configuration.FileNameFormat ?? ("Launch-" + now.ToString("yyyy-M-d", CultureInfo.InvariantCulture) + "-{0}")) + ".log").Replace("{0}", now.ToString("HHmmssfff", CultureInfo.InvariantCulture)), path1: Configuration.StoreFolder);
		_files.Add(text);
		StreamWriter lastWriter = _currentStream;
		FileStream lastFile = _currentFile;
		Directory.CreateDirectory(Configuration.StoreFolder);
		_currentFile = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.Read);
		_currentStream = new StreamWriter(_currentFile);
		Task.Run(async () =>
		{
			if (lastWriter != null)
			{
				try
				{
					await lastWriter.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception)
				{
				}
			}
			if (lastFile != null)
			{
				try
				{
					await lastFile.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception)
				{
				}
			}
			if (!Configuration.AutoDeleteOldFile)
			{
				return;
			}
			string[] files = Directory.GetFiles(Configuration.StoreFolder, "*.log", SearchOption.TopDirectoryOnly);
			foreach (FileInfo item in (from x in files
				select new FileInfo(x) into x
				orderby x.CreationTime
				select x).Take(files.Length - Configuration.MaxKeepOldFile))
			{
				item.Delete();
			}
		});
	}

	public void Trace(string message)
	{
		Log("[" + _GetTimeFormatted() + "] [TRA] " + message);
	}

	public void Debug(string message)
	{
		Log("[" + _GetTimeFormatted() + "] [DBG] " + message);
	}

	public void Info(string message)
	{
		Log("[" + _GetTimeFormatted() + "] [INFO] " + message);
	}

	public void Warn(string message)
	{
		Log("[" + _GetTimeFormatted() + "] [WARN] " + message);
	}

	public void Error(string message)
	{
		Log("[" + _GetTimeFormatted() + "] [ERR!] " + message);
	}

	public void Fatal(string message)
	{
		Log("[" + _GetTimeFormatted() + "] [FTL!] " + message);
	}

	private static string _GetTimeFormatted()
	{
		return $"{DateTime.Now:HH:mm:ss.fff}";
	}

	public void Log(string message)
	{
		if (!_disposed && !_logChannel.Writer.TryWrite(message))
		{
			Interlocked.Increment(ref _droppedCount);
			Console.WriteLine("Log dropped error: " + message);
		}
	}

	private async Task _ProcessLogQueueAsync()
	{
		TimeSpan writeTimeout = TimeSpan.FromMilliseconds(325L);
		StringBuilder batch = new StringBuilder(4096);
		uint lineCount = 0u;
		long lastFlush = Stopwatch.GetTimestamp();
		try
		{
			string item;
			while (!_disposed || _logChannel.Reader.TryPeek(out item))
			{
				if (_logChannel.Reader.TryRead(out string item2))
				{
					batch.AppendLine(item2);
					lineCount++;
					TimeSpan elapsedTime = Stopwatch.GetElapsedTime(lastFlush);
					if (lineCount >= 198 || elapsedTime > writeTimeout)
					{
						await DoRefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				else
				{
					if (lineCount != 0)
					{
						await DoRefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					await Task.Delay(80).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[" + _GetTimeFormatted() + "] [ERROR] An error occurred while processing log queue: " + ex.Message);
			throw;
		}
		async Task DoRefreshAsync()
		{
			await _DoWriteAsync(batch).ConfigureAwait(continueOnCapturedContext: false);
			batch.Clear();
			lineCount = 0u;
			lastFlush = Stopwatch.GetTimestamp();
		}
	}

	private async Task _DoWriteAsync(StringBuilder ctx)
	{
		try
		{
			if (_currentFile?.Length >= Configuration.MaxFileSize)
			{
				_CreateNewFile();
			}
			await _currentStream.WriteAsync(ctx).ConfigureAwait(continueOnCapturedContext: false);
			await _currentStream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[" + _GetTimeFormatted() + "] [ERROR] An error occurred while writing log file: " + ex.Message);
			await File.AppendAllTextAsync(Path.Combine(Configuration.StoreFolder, "Error.log"), $"[{_GetTimeFormatted}] LogCycle Error: {ex}\n");
			throw;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			_disposed = true;
			_logChannel.Writer.Complete();
			await _processingTask.ConfigureAwait(continueOnCapturedContext: false);
			if (_currentStream != null)
			{
				await _currentStream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_currentFile != null)
			{
				await _currentFile.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}
}
