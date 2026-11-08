using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Profile.Models;

public class SafeProfile
{
	[JsonPropertyName("profileId")]
	public string ProfileId { get; set; } = string.Empty;

	[JsonPropertyName("username")]
	public string UserName { get; set; } = string.Empty;

	[JsonPropertyName("accessToken")]
	public string AccessToken { get; set; } = string.Empty;

	[JsonPropertyName("uuid")]
	public string Uuid { get; set; } = string.Empty;

	[JsonPropertyName("tokenType")]
	public string TokenType { get; set; } = "Bearer";

	[JsonPropertyName("profileType")]
	public ProfileType ProfileType { get; set; } = ProfileType.Offline;
}
