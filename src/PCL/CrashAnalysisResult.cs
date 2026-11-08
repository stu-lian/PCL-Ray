using System.Collections.Generic;
using System.Linq;

namespace PCL;

internal sealed class CrashAnalysisResult
{
	private readonly List<CrashFinding> _findings = new List<CrashFinding>();

	public IReadOnlyList<CrashFinding> Findings => _findings;

	public bool HasFinding => _findings.Count > 0;

	public bool Any => HasFinding;

	public void Add(CrashFinding finding)
	{
		CrashFinding crashFinding = _findings.FirstOrDefault((CrashFinding item) => item.Cause == finding.Cause);
		if (crashFinding == null)
		{
			_findings.Add(finding);
			return;
		}
		foreach (CrashEvidence item in finding.Evidence)
		{
			crashFinding.AddEvidence(item);
		}
		crashFinding.ShouldStop |= finding.ShouldStop;
	}

	public void Add(CrashCause cause, CrashConfidence confidence, IEnumerable<string>? details = null, CrashLogKind? source = null, string? pattern = null, string? displayKind = null, bool shouldStop = false)
	{
		Add(new CrashFinding(cause, confidence, ToEvidence(details, source, pattern, displayKind))
		{
			ShouldStop = shouldStop
		});
	}

	private static IEnumerable<CrashEvidence> ToEvidence(IEnumerable<string>? details, CrashLogKind? source, string? pattern, string? displayKind)
	{
		if (details == null)
		{
			yield break;
		}
		foreach (string detail in details)
		{
			if (!string.IsNullOrWhiteSpace(detail))
			{
				yield return new CrashEvidence
				{
					Value = detail.Trim(),
					Source = source,
					Pattern = pattern,
					DisplayKind = displayKind
				};
			}
		}
	}
}
