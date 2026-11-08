using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record YggdrasilLegacyUser
{
	[JsonPropertyName("id")]
	public required string Id { get; init; }

	[JsonPropertyName("properties")]
	public PlayerProperty[]? Properties { get; init; }
}
