using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public class UpdatesWrapperModel : IUpdateSource
{
	private readonly IEnumerable<IUpdateSource> _sources;

	private IUpdateSource _announcementSource;

	private IUpdateSource _versionSource;

	public string SourceName
	{
		get
		{
			return _versionSource?.SourceName ?? "";
		}
		set
		{
			if (_versionSource != null)
			{
				_versionSource.SourceName = value;
			}
		}
	}

	public UpdatesWrapperModel(IEnumerable<IUpdateSource> sources)
	{
		_sources = sources;
	}

	public bool IsAvailable()
	{
		return _sources.Any((IUpdateSource x) => x.IsAvailable());
	}

	public bool RefreshCache()
	{
		foreach (IUpdateSource source in _sources)
		{
			try
			{
				source.RefreshCache();
				_versionSource = source;
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[Update] " + source.SourceName + " 暂不可用");
				continue;
			}
			break;
		}
		return _versionSource != null;
	}

	public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
	{
		foreach (IUpdateSource source in _sources)
		{
			try
			{
				if (_versionSource != null)
				{
					try
					{
						return _versionSource.GetLatestVersion(channel, arch);
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[Update] 缓存的版本源 " + _versionSource.SourceName + " 不可用");
					}
				}
				VersionDataModel latestVersion = source.GetLatestVersion(channel, arch);
				_versionSource = source;
				return latestVersion;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[Update] " + source.SourceName + " 无法获取最新版本信息");
			}
		}
		ModBase.Log("[Update] 错误！所有的版本源都无法使用！");
		throw new Exception(Lang.Text("Update.Task.GetVersionInfoFailed"));
	}

	public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		foreach (IUpdateSource source in _sources)
		{
			try
			{
				if (_versionSource != null)
				{
					try
					{
						return _versionSource.IsLatest(channel, arch, currentVersion, currentVersionCode);
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[Update] 缓存的版本源 " + _versionSource.SourceName + " 不可用");
					}
				}
				bool result = source.IsLatest(channel, arch, currentVersion, currentVersionCode);
				_versionSource = source;
				return result;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[Update] " + source.SourceName + " 无法获取最新版本信息");
			}
		}
		ModBase.Log("[Update] 错误！所有的版本源都无法使用！");
		throw new Exception(Lang.Text("Update.Task.GetVersionInfoFailed"));
	}

	public VersionAnnouncementDataModel GetAnnouncementList()
	{
		foreach (IUpdateSource source in _sources)
		{
			try
			{
				if (_announcementSource != null)
				{
					try
					{
						return _announcementSource.GetAnnouncementList();
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[Update] 缓存的公告源 " + _announcementSource.SourceName + " 不可用");
					}
				}
				VersionAnnouncementDataModel announcementList = source.GetAnnouncementList();
				_announcementSource = source;
				return announcementList;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[Update] " + source.SourceName + " 无法获取最新公告信息");
			}
		}
		ModBase.Log("[Update] 错误！所有的公告源都无法使用！");
		throw new Exception(Lang.Text("Update.Task.GetAnnouncementFailed"));
	}

	public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
	{
		foreach (IUpdateSource source in _sources)
		{
			try
			{
				if (_versionSource != null)
				{
					try
					{
						return _versionSource.GetDownloadLoader(channel, arch, output);
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[Update] 缓存的版本源 " + _versionSource.SourceName + " 不可用");
					}
				}
				List<ModLoader.LoaderBase> downloadLoader = source.GetDownloadLoader(channel, arch, output);
				_versionSource = source;
				return downloadLoader;
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[Update] " + source.SourceName + " 无法获取最新版本信息");
			}
		}
		ModBase.Log("[Update] 错误！所有的版本源都无法使用！");
		throw new Exception(Lang.Text("Update.Task.GetVersionInfoFailed"));
	}

	public async Task<bool> IsLatestAsync(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		return await Task.Run(() => IsLatest(channel, arch, currentVersion, currentVersionCode));
	}
}
