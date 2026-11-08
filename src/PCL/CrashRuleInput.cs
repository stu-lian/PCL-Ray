namespace PCL;

internal sealed class CrashRuleInput
{
	public required CrashLogSet Logs { get; init; }

	public required CrashModIndex ModIndex { get; init; }
}
