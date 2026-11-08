using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL;

public class VersionAnnouncementDataModel
{
	[JsonPropertyName("content")]
	public List<VersionAnnouncementContentModel> Content { get; set; }
}
