using System.Text.Json.Serialization;

namespace PCL;

public class AnnouncementBtnInfoModel
{
	[JsonPropertyName("text")]
	public string Text { get; set; }

	[JsonPropertyName("command")]
	public string Command { get; set; }

	[JsonPropertyName("command_paramter")]
	public string CommandParameter { get; set; }
}
