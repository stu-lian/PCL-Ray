namespace PCL;

internal sealed class CrashLogSet
{
	public CrashLogText? Game { get; init; }

	public CrashLogText? Debug { get; init; }

	public CrashLogText? CrashReport { get; init; }

	public CrashLogText? HsErr { get; init; }

	public string All { get; init; } = string.Empty;

	public bool HasAnalyzableLog
	{
		get
		{
			if (Game == null && CrashReport == null)
			{
				return HsErr != null;
			}
			return true;
		}
	}

	public string? GetText(CrashLogKind kind)
	{
		return kind switch
		{
			CrashLogKind.Game => Game?.Text, 
			CrashLogKind.Debug => Debug?.Text, 
			CrashLogKind.CrashReport => CrashReport?.Text, 
			CrashLogKind.HsErr => HsErr?.Text, 
			CrashLogKind.All => All, 
			_ => null, 
		};
	}
}
