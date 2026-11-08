using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record PlayerTextureProperty
{
	[JsonPropertyName("timestamp")]
	public required long Timestamp { get; init; }

	[JsonPropertyName("profileId")]
	public required string ProfileId { get; init; }

	[JsonPropertyName("profileName")]
	public required string ProfileName { get; init; }

	[JsonPropertyName("textures")]
	public required PlayerTextures Textures { get; init; }
}
