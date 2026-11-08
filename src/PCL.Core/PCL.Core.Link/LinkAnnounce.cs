using System.Text.Json.Serialization;

namespace PCL.Core.Link;

public record LinkAnnounce([property: JsonPropertyName("notices")] LinkAnnounceInfo[] Announces);
