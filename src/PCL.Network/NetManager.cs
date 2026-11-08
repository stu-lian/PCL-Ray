using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Network.Loaders;

namespace PCL.Network;

public sealed class NetManager
{
	private static readonly Lazy<NetManager> _instance = new Lazy<NetManager>(() => new NetManager());

	public static NetManager Instance => _instance.Value;

	public Dictionary<string, DownloadFile> Files { get; } = new Dictionary<string, DownloadFile>();

	public object LockFiles { get; } = new object();

	public ModBase.SafeList<LoaderDownload> Tasks { get; } = new ModBase.SafeList<LoaderDownload>();

	public object LockRemain { get; } = new object();

	public int FileRemain
	{
		get
		{
			lock (LockFiles)
			{
				return Enumerable.Count(Files.Values, (DownloadFile file) => file.State != NetState.Finished);
			}
		}
	}

	public object LockDone { get; } = new object();

	public long DownloadDone
	{
		get
		{
			lock (LockDone)
			{
				return field;
			}
		}
		set
		{
			lock (LockDone)
			{
				field = value;
			}
		}
	}

	public long Speed => DownloadResourceManager.DownloadSpeed;

	public int ConnectionCount => DownloadResourceManager.ActiveConnectionCount;

	[Obsolete("请使用 ConnectionCount。")]
	public int ThreadCount => ConnectionCount;

	public void Start(LoaderDownload task)
	{
		lock (LockFiles)
		{
			Tasks.Remove(task);
			Tasks.Add(task);
			foreach (DownloadFile file in task.files)
			{
				Files[file.LocalPath] = file;
			}
		}
	}

	public void Finish(LoaderDownload task)
	{
		lock (LockFiles)
		{
			Tasks.Remove(task);
			foreach (DownloadFile file in task.files)
			{
				Files.Remove(file.LocalPath);
			}
		}
	}
}
