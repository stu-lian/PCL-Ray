using System;

namespace PCL.Core.Utils;

public class VersionRange(Version? minVersion, Version? maxVersion)
{
	public Version? MinVersion { get; set; } = minVersion;

	public Version? MaxVersion { get; set; } = maxVersion;

	public bool IsInRange(Version target)
	{
		if (((object)MinVersion != null || (object)MaxVersion != null) && (MinVersion ?? target) <= target)
		{
			return (MaxVersion ?? target) >= target;
		}
		return false;
	}

	public bool SetMin(Version target)
	{
		Version version = (MinVersion = (((object)MinVersion == null || target > MinVersion) ? target : MinVersion));
		return version == target;
	}

	public bool SetMax(Version target)
	{
		Version version = (MaxVersion = (((object)MaxVersion == null || target < MaxVersion) ? target : MaxVersion));
		return version == target;
	}
}
