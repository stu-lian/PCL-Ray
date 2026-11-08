using System.Collections.Generic;

namespace PCL;

internal sealed class CrashAnalysisContext(int processId, string tempFolder)
{
	public int ProcessId { get; } = processId;

	public string TempFolder { get; } = tempFolder;

	public List<CrashLogEntry> RawFiles { get; } = new List<CrashLogEntry>();

	public List<string> OutputFiles { get; } = new List<string>();

	public McInstance? Instance { get; set; }

	public CrashLogEntry? DirectOpenFile { get; set; }

	public CrashLogSet? PreparedLogs { get; set; }

	public CrashAnalysisResult? Result { get; set; }

	public string LogAll { get; set; } = string.Empty;
}
