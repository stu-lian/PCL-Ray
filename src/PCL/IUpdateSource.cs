using System.Collections.Generic;
using PCL.Core.Utils;

namespace PCL;

public interface IUpdateSource
{
	string SourceName { get; set; }

	bool IsAvailable();

	bool RefreshCache();

	VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch);

	bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode);

	VersionAnnouncementDataModel GetAnnouncementList();

	List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output);
}
