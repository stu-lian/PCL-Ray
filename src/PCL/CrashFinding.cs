using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL;

internal sealed class CrashFinding
{
	public CrashCause Cause { get; }

	public CrashConfidence Confidence { get; }

	public List<CrashEvidence> Evidence { get; }

	public bool ShouldStop { get; set; }

	public IReadOnlyList<string> Details => (from item in Evidence
		select item.Value into value
		where !string.IsNullOrWhiteSpace(value)
		select value).Distinct(StringComparer.Ordinal).ToList();

	public CrashFinding(CrashCause cause, CrashConfidence confidence, IEnumerable<CrashEvidence>? evidence = null)
	{
		Cause = cause;
		Confidence = confidence;
		Evidence = evidence?.Where((CrashEvidence item) => !string.IsNullOrWhiteSpace(item.Value)).ToList() ?? new List<CrashEvidence>();
	}

	public void AddEvidence(CrashEvidence evidence)
	{
		if (!string.IsNullOrWhiteSpace(evidence.Value) && !Evidence.Any((CrashEvidence item) => item.EqualsTo(evidence)))
		{
			Evidence.Add(evidence);
		}
	}
}
