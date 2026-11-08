using System.Text.Json.Serialization;

namespace PCL.Core.Link;

public enum LinkAnnounceType
{
	[JsonStringEnumMemberName("notice")]
	Notice,
	[JsonStringEnumMemberName("warning")]
	Warning,
	[JsonStringEnumMemberName("important")]
	Important
}
