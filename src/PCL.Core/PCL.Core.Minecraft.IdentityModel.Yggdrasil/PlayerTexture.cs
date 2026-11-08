using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record PlayerTexture
{
	[JsonPropertyName("url")]
	public required string Url { get; init; }

	[JsonPropertyName("metadata")]
	public PlayerTextureMetadata? Metadata { get; init; }
}
