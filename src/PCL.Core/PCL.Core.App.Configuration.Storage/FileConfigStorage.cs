using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;

namespace PCL.Core.App.Configuration.Storage;

public class FileConfigStorage : ConfigStorage
{
	private readonly Channel<(string, Action)> _writeActionChannel;

	private readonly CancellationTokenSource _writeActionCts;

	private readonly ManualResetEventSlim _writeStopEvent = new ManualResetEventSlim(initialState: false);

	public IKeyValueFileProvider File { get; }

	public FileConfigStorage(IKeyValueFileProvider file)
	{
		File = file;
		_writeActionChannel = Channel.CreateUnbounded<(string, Action)>();
		_writeActionCts = new CancellationTokenSource();
		Task.Run(async () =>
		{
			long lastSyncTick = 0L;
			CancellationToken cancelToken = _writeActionCts.Token;
			Dictionary<string, Action> writeActionMap = new Dictionary<string, Action>();
			ChannelReader<(string, Action)> reader = _writeActionChannel.Reader;
			try
			{
				while (!cancelToken.IsCancellationRequested)
				{
					var (key, value) = await reader.ReadAsync(cancelToken);
					writeActionMap[key] = value;
					if (Environment.TickCount64 - lastSyncTick >= 10000 && !cancelToken.IsCancellationRequested)
					{
						Sync();
						lastSyncTick = Environment.TickCount64;
						writeActionMap.Clear();
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				(string, Action) item;
				while (reader.TryRead(out item))
				{
					writeActionMap[item.Item1] = item.Item2;
				}
				Sync();
				_writeStopEvent.Set();
			}
			void Sync()
			{
				try
				{
					LogWrapper.Trace("Config", "正在保存 " + File.FilePath);
					foreach (Action value2 in writeActionMap.Values)
					{
						value2();
					}
					File.Sync();
				}
				catch (Exception ex2)
				{
					LogWrapper.Error(ex2, "Config", "配置文件保存失败");
					MsgBoxWrapper.Show(ExceptionDetails.Compose(Lang.Text("Config.Error.SaveFailed.Message", File.FilePath), ex2), Lang.Text("Config.Error.SaveFailed.Title"), MsgBoxTheme.Error, true);
				}
			}
		});
	}

	protected override void OnStop()
	{
		_writeActionCts.Cancel();
		_writeStopEvent.Wait();
		_writeStopEvent.Dispose();
	}

	protected override bool OnAccess<TKey, TValue>(StorageAction action, ref TKey key, [NotNullWhen(true)] ref TValue value, object? argument)
	{
		TKey val = key;
		string strKey = val as string;
		if (strKey == null)
		{
			throw new NotSupportedException($"Key '{key}' is not supported");
		}
		switch (action)
		{
		case StorageAction.Get:
			if (!File.Exists(strKey))
			{
				return false;
			}
			try
			{
				value = File.Get<TValue>(strKey);
			}
			catch (Exception ex) when ((ex is JsonException || ex is InvalidCastException || ex is FormatException || ex is OverflowException || ex is ArgumentException || ex is KeyNotFoundException || ex is InvalidDataException) ? true : false)
			{
				LogWrapper.Warn(ex, "Config", "配置项 " + strKey + " 读取失败（可能已损坏），重置为默认值");
				if (!_writeActionChannel.Writer.TryWrite((strKey, () =>
				{
					File.Remove(strKey);
				})))
				{
					LogWrapper.Warn("Config", "配置项 " + strKey + " 清理任务入队失败，改为同步删除");
					try
					{
						File.Remove(strKey);
						File.Sync();
					}
					catch (Exception ex2)
					{
						LogWrapper.Error(ex2, "Config", "配置项 " + strKey + " 同步删除失败，可能需人工处理");
					}
				}
				return false;
			}
			return true;
		case StorageAction.Exists:
			if (typeof(TValue) == typeof(bool))
			{
				Unsafe.As<TValue, bool>(ref value) = File.Exists(strKey);
				return true;
			}
			throw new InvalidOperationException($"Storage action '{StorageAction.Exists}' must have a boolean value");
		case StorageAction.Set:
		{
			TValue localValue = value;
			_writeActionChannel.Writer.TryWrite((strKey, () =>
			{
				File.Set(strKey, localValue);
			}));
			return false;
		}
		case StorageAction.Delete:
			_writeActionChannel.Writer.TryWrite((strKey, () =>
			{
				File.Remove(strKey);
			}));
			return false;
		default:
			throw new InvalidOperationException($"Invalid storage action: {action}");
		}
	}

	public override string ToString()
	{
		return base.ToString() + " (" + File.FilePath + ")";
	}
}
