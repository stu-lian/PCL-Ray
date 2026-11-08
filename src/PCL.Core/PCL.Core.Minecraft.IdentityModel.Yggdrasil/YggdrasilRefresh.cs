using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record YggdrasilRefresh
{
	[JsonPropertyName("accessToken")]
	public required string AccessToken { get; set; }

	[JsonPropertyName("selectedProfile")]
	public Profile? SelectedProfile { get; set; }

	[JsonPropertyName("clientToken")]
	public string? ClientToken { get; set; }

	[JsonPropertyName("requestUser")]
	public bool RequestUser { get; set; }
}
