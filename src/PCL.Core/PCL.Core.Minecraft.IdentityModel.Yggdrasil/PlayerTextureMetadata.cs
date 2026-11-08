using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record PlayerTextureMetadata
{
	[JsonPropertyName("model")]
	public string? Model { get; init; }
}
