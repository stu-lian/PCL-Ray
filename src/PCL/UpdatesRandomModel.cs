using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.Utils;

namespace PCL;

public class UpdatesRandomModel : IUpdateSource
{
	private readonly int _randIndex;

	private readonly IEnumerable<IUpdateSource> _sources;

	public string SourceName
	{
		get
		{
			return _sources.ElementAt(_randIndex).SourceName;
		}
		set
		{
			_sources.ElementAt(_randIndex).SourceName = value;
		}
	}

	public UpdatesRandomModel(IEnumerable<IUpdateSource> sources)
	{
		_sources = sources;
		Random random = new Random(DateTime.Now.Millisecond);
		_randIndex = random.Next(0, _sources.Count() - 1);
	}

	public bool IsAvailable()
	{
		return _sources.ElementAt(_randIndex).IsAvailable();
	}

	public bool RefreshCache()
	{
		return _sources.ElementAt(_randIndex).RefreshCache();
	}

	public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
	{
		return _sources.ElementAt(_randIndex).GetLatestVersion(channel, arch);
	}

	public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		return _sources.ElementAt(_randIndex).IsLatest(channel, arch, currentVersion, currentVersionCode);
	}

	public VersionAnnouncementDataModel GetAnnouncementList()
	{
		return _sources.ElementAt(_randIndex).GetAnnouncementList();
	}

	public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
	{
		return _sources.ElementAt(_randIndex).GetDownloadLoader(channel, arch, output);
	}
}
