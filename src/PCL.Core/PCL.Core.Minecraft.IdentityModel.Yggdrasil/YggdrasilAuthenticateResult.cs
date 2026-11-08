using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record YggdrasilAuthenticateResult
{
	[JsonPropertyName("error")]
	public string? Error { get; init; }

	[JsonPropertyName("errorMessage")]
	public string? ErrorMessage { get; init; }

	[JsonPropertyName("accessToken")]
	public string? AccessToken { get; init; }

	[JsonPropertyName("clientToken")]
	public string? ClientToken { get; init; }

	[JsonPropertyName("selectedProfile")]
	public Profile? SelectedProfile { get; init; }

	[JsonPropertyName("availableProfiles")]
	public Profile[]? AvailableProfiles { get; init; }

	[JsonPropertyName("user")]
	public YggdrasilLegacyUser? User { get; init; }
}
