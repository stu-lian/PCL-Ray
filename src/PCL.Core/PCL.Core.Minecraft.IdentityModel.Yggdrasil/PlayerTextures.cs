using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record PlayerTextures
{
	[JsonPropertyName("skin")]
	public PlayerTexture? Skin { get; init; }

	[JsonPropertyName("cape")]
	public PlayerTexture? Cape { get; init; }
}
