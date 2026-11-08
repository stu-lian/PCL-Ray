namespace PCL;

internal sealed class CrashModInfo
{
	public string? ModId { get; init; }

	public string? DisplayName { get; init; }

	public string? FileName { get; init; }

	public string? Version { get; init; }

	public string Source { get; init; } = "";

	public string DisplayNameOrFileName
	{
		get
		{
			string? text;
			if (string.IsNullOrWhiteSpace(DisplayName))
			{
				if (!string.IsNullOrWhiteSpace(FileName))
				{
					return FileName;
				}
				text = ModId;
				if (text == null)
				{
					return "";
				}
			}
			else
			{
				text = DisplayName;
			}
			return text;
		}
	}
}
