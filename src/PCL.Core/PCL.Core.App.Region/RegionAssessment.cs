namespace PCL.Core.App.Region;

public sealed record RegionAssessment(RegionClassification Classification, string Reason)
{
	// Disabled for the PCL Ray build: region detection no longer restricts features.
	public bool IsRestricted => false;
}
