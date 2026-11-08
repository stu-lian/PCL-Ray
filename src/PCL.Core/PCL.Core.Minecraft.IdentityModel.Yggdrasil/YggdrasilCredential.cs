using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record YggdrasilCredential
{
	[JsonPropertyName("username")]
	public required string User { get; init; }

	[JsonPropertyName("password")]
	public required string Password { get; init; }

	[JsonPropertyName("agent")]
	public Agent Agent { get; init; } = new Agent();

	[JsonPropertyName("requestUser")]
	public bool RequestUser { get; set; }

	[JsonPropertyName("clientToken")]
	public string? ClientToken { get; set; }
}
