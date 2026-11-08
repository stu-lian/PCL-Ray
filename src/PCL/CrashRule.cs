using System;

namespace PCL;

internal sealed class CrashRule
{
	public required CrashCause Cause { get; init; }

	public required DetectionPhase Phase { get; init; }

	public CrashConfidence Confidence { get; init; }

	public bool StopOnMatch { get; init; }

	public required Func<CrashRuleInput, CrashFinding?> Evaluate { get; init; }
}
