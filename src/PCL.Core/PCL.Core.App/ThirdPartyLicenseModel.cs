using System.Text.Json.Serialization;

namespace PCL.Core.App;

public sealed record ThirdPartyLicenseModel([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("info")] string Information, [property: JsonPropertyName("website")] string? WebsiteUri = null, [property: JsonPropertyName("license")] string? LicenseUri = null);
