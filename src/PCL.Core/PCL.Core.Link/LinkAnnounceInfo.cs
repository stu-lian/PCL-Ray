using System.Text.Json.Serialization;

namespace PCL.Core.Link;

public record LinkAnnounceInfo([property: JsonPropertyName("type")] LinkAnnounceType Type, [property: JsonPropertyName("content")] string Content);
