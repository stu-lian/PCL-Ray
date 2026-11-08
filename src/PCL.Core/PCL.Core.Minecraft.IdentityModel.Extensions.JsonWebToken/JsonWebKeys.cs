using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.JsonWebToken;

public record JsonWebKeys
{
	[JsonPropertyName("keys")]
	public required JsonWebKey[] Keys { get; init; }
}
