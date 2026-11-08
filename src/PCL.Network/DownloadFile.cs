using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Network.Loaders;

namespace PCL.Network;

public class DownloadFile
{
	private readonly object _sync = new object();

	private readonly List<Exception> _errors = new List<Exception>();

	private readonly List<LoaderDownload> _loaders = new List<LoaderDownload>();

	public int Id { get; } = ModBase.GetUuid();

	public string LocalPath { get; set; }

	public string LocalName { get; }

	public List<string> Urls { get; }

	public ModBase.FileChecker? Check { get; }

	public bool UseBrowserUserAgent { get; }

	public string CustomUserAgent { get; }

	public NetState State { get; set; } = NetState.WaitingToCheck;

	public long TotalSize { get; set; } = -1L;

	public bool IsUnknownSize { get; set; } = true;

	public long DownloadedBytes { get; set; }

	public bool IsCopy { get; set; }

	public IReadOnlyList<Exception> Errors
	{
		get
		{
			lock (_sync)
			{
				return _errors.ToArray();
			}
		}
	}

	public IReadOnlyList<LoaderDownload> Loaders
	{
		get
		{
			lock (_sync)
			{
				return _loaders.ToArray();
			}
		}
	}

	public long Speed { get; set; }

	public int ActiveThreads { get; set; }

	public double Progress
	{
		get
		{
			switch (State)
			{
			case NetState.WaitingToCheck:
				return 0.0;
			case NetState.WaitingToDownload:
				return 0.01;
			case NetState.Connecting:
				return 0.02;
			case NetState.Reading:
				return 0.04;
			case NetState.Downloading:
				if (TotalSize > 0)
				{
					return Math.Clamp((double)DownloadedBytes / (double)TotalSize, 0.05, 1.0);
				}
				return 0.5;
			case NetState.Merging:
				return 0.99;
			case NetState.Finished:
			case NetState.Interrupted:
				return 1.0;
			default:
				return 0.0;
			}
		}
	}

	public void AddError(Exception error)
	{
		lock (_sync)
		{
			_errors.Add(error);
		}
	}

	public void AddErrors(IEnumerable<Exception> errors)
	{
		lock (_sync)
		{
			_errors.AddRange(errors);
		}
	}

	public bool RegisterLoader(LoaderDownload loader)
	{
		lock (_sync)
		{
			if (_loaders.Contains(loader))
			{
				return false;
			}
			_loaders.Add(loader);
			return true;
		}
	}

	public DownloadFile(IEnumerable<string> urls, string localPath, ModBase.FileChecker? checker = null, bool useBrowserUserAgent = false, string customUserAgent = "")
	{
		Urls = urls.Where((string url) => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
		LocalPath = localPath;
		LocalName = ModBase.GetFileNameFromPath(localPath);
		Check = checker;
		UseBrowserUserAgent = useBrowserUserAgent;
		CustomUserAgent = customUserAgent;
	}
}
