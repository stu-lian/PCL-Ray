using System.Text.Json.Serialization;

namespace PCL.Core.App;

public sealed record MetadataModel([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("version")] LauncherVersionModel Version, [property: JsonPropertyName("licenses")] ThirdPartyLicenseModel[] Licenses);
