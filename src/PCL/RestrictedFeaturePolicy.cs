using PCL.Core.App.Region;

namespace PCL;

internal static class RestrictedFeaturePolicy
{
	internal static bool IsRestricted => Assess().IsRestricted;

	internal static RegionAssessment Assess()
	{
		return RegionPolicy.Evaluate(RegionPolicy.ReadWindowsSignals());
	}

	internal static bool Check(string feature, bool showHint = true)
	{
		// PCL Ray build: region restriction disabled, always allow.
		return true;
	}

	internal static void ThrowIfRestricted(string feature)
	{
		// PCL Ray build: region restriction disabled, never throws.
		return;
	}
}
