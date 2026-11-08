namespace PCL.Core.UI;

public static class HintWrapper
{
	public static event HintHandler? OnShow;

	public static void Show(string message, HintTheme theme = HintTheme.Info)
	{
		OnShow?.Invoke(message, theme);
	}
}
