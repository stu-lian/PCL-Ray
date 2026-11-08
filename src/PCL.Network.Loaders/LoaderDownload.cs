using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Network.Loaders;

public class LoaderDownload : ModLoader.LoaderBase
{
	public ModBase.SafeList<DownloadFile> files;

	private int _fileRemain;

	private readonly object _fileRemainLock = new object();

	private CancellationTokenSource? _cancellationTokenSource;

	public int FailCount { get; set; }

	public override double Progress
	{
		get
		{
			if (State < ModBase.LoadState.Finished)
			{
				if (!files.Any())
				{
					return 0.0;
				}
				return files.Average((DownloadFile file) => file.Progress);
			}
			return 1.0;
		}
		set
		{
			throw new Exception("文件下载不允许指定进度");
		}
	}

	public LoaderDownload(string name, List<DownloadFile> fileTasks)
	{
		base.name = name;
		files = new ModBase.SafeList<DownloadFile>(fileTasks ?? new List<DownloadFile>());
	}

	public void RefreshStat()
	{
	}

	public override void Start(object input = null, bool isForceRestart = false)
	{
		if (input is List<DownloadFile> data)
		{
			files = new ModBase.SafeList<DownloadFile>(data);
		}
		lock (lockState)
		{
			if (State == ModBase.LoadState.Loading)
			{
				return;
			}
			State = ModBase.LoadState.Loading;
		}
		_cancellationTokenSource = new CancellationTokenSource();
		lock (_fileRemainLock)
		{
			_fileRemain = files.Count;
		}
		ModNet.NetManager.Start(this);
		ModBase.RunInNewThread(() =>
		{
			Run(_cancellationTokenSource.Token);
		}, $"DL/{Uuid}");
	}

	private void Run(CancellationToken cancellationToken)
	{
		try
		{
			if (!files.Any())
			{
				OnFinish();
				return;
			}
			ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
			SemaphoreSlim semaphore = new SemaphoreSlim(GetMaxParallelFiles());
			try
			{
				Task.WhenAll(((IEnumerable<DownloadFile>)files).Select((Func<DownloadFile, Task>)(async (DownloadFile file) =>
				{
					bool entered = false;
					try
					{
						await semaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						entered = true;
						await ProcessFileAsync(file, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
					{
					}
					catch (Exception ex3)
					{
						file.AddError(ex3);
						file.State = NetState.Interrupted;
						exceptions.Enqueue(ex3);
						_cancellationTokenSource?.Cancel();
					}
					finally
					{
						if (entered)
						{
							semaphore.Release();
						}
					}
				})).ToList()).GetAwaiter().GetResult();
				if (!exceptions.IsEmpty)
				{
					OnFail(exceptions.ToList());
				}
			}
			finally
			{
				if (semaphore != null)
				{
					((IDisposable)semaphore).Dispose();
				}
			}
		}
		catch (OperationCanceledException)
		{
			Abort();
		}
		catch (Exception item)
		{
			OnFail(new List<Exception> { item });
		}
	}

	private int GetMaxParallelFiles()
	{
		return Math.Max(1, Math.Min(files.Count, Math.Clamp(ModNet.NetTaskConnectionLimit, 1, 256)));
	}

	private async Task ProcessFileAsync(DownloadFile file, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		file.RegisterLoader(this);
		if (State >= ModBase.LoadState.Finished)
		{
			return;
		}
		Directory.CreateDirectory(Path.GetDirectoryName(file.LocalPath) ?? throw new IOException("下载路径无效"));
		ModBase.FileChecker checker = file.Check;
		if ((checker?.canUseExistsFile ?? false) && File.Exists(file.LocalPath))
		{
			string text = ((!string.IsNullOrEmpty(checker.hash)) ? (await Task.Run(() => checker.Check(file.LocalPath), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) : checker.Check(file.LocalPath));
			if (text == null)
			{
				file.IsCopy = true;
				file.State = NetState.Finished;
				try
				{
					file.TotalSize = new FileInfo(file.LocalPath).Length;
				}
				catch (IOException)
				{
					file.TotalSize = -1L;
				}
				file.DownloadedBytes = file.TotalSize;
				file.Speed = 0L;
				file.ActiveThreads = 0;
				OnFileFinish(file);
				return;
			}
		}
		file.State = NetState.Connecting;
		long num = file.Check?.actualSize ?? (-1);
		bool enableParallelChunks = files.Count <= 1 || num >= 4194304;
		await FileDownloader.DownloadAsync(file.Urls, file.LocalPath, file.UseBrowserUserAgent, file.CustomUserAgent, cancellationToken, enableParallelChunks, file).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			file.TotalSize = new FileInfo(file.LocalPath).Length;
		}
		catch (IOException)
		{
			file.TotalSize = -1L;
		}
		file.IsUnknownSize = file.TotalSize < 0;
		file.DownloadedBytes = Math.Max(0L, file.TotalSize);
		file.Speed = 0L;
		file.ActiveThreads = 0;
		file.State = NetState.Finished;
		OnFileFinish(file);
	}

	public void OnFileFinish(DownloadFile file)
	{
		lock (_fileRemainLock)
		{
			_fileRemain--;
			if (_fileRemain > 0)
			{
				return;
			}
		}
		OnFinish();
	}

	public void OnFinish()
	{
		RaisePreviewFinish();
		lock (lockState)
		{
			if (State > ModBase.LoadState.Loading)
			{
				return;
			}
			State = ModBase.LoadState.Finished;
		}
		ModNet.NetManager.Finish(this);
	}

	public void OnFileFail(DownloadFile file)
	{
		IReadOnlyList<Exception> errors = file.Errors;
		OnFail((errors.Count > 0) ? errors.ToList() : new List<Exception>
		{
			new Exception("文件下载失败：" + file.LocalPath)
		});
	}

	public void OnFail(List<Exception> exList)
	{
		lock (lockState)
		{
			if (State > ModBase.LoadState.Loading)
			{
				return;
			}
			Error = exList.FirstOrDefault() ?? new Exception("未知下载错误");
			State = ModBase.LoadState.Failed;
		}
		FailCount += exList.Count;
		foreach (DownloadFile item in files.Where((DownloadFile file) => file.State < NetState.Finished))
		{
			item.State = NetState.Interrupted;
			item.Speed = 0L;
			item.ActiveThreads = 0;
			item.AddErrors(exList);
		}
		ModNet.NetManager.Finish(this);
	}

	public override void Abort()
	{
		lock (lockState)
		{
			if (State >= ModBase.LoadState.Finished)
			{
				return;
			}
			State = ModBase.LoadState.Aborted;
		}
		_cancellationTokenSource?.Cancel();
		foreach (DownloadFile item in files.Where((DownloadFile file) => file.State < NetState.Finished))
		{
			item.State = NetState.Interrupted;
			item.Speed = 0L;
			item.ActiveThreads = 0;
		}
		ModNet.NetManager.Finish(this);
	}
}
