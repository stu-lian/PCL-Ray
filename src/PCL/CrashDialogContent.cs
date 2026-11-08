namespace PCL;

internal sealed class CrashDialogContent(string text, CrashSuggestedAction suggestedAction)
{
	public string Text { get; } = text;

	public CrashSuggestedAction SuggestedAction { get; } = suggestedAction;
}
