using System;

namespace PCL;

internal sealed class CrashEvidence
{
	public required string Value { get; init; }

	public CrashLogKind? Source { get; init; }

	public string? Pattern { get; init; }

	public string? DisplayKind { get; init; }

	public bool EqualsTo(CrashEvidence other)
	{
		if (Source == other.Source && string.Equals(Value, other.Value, StringComparison.Ordinal))
		{
			return string.Equals(DisplayKind, other.DisplayKind, StringComparison.Ordinal);
		}
		return false;
	}
}
