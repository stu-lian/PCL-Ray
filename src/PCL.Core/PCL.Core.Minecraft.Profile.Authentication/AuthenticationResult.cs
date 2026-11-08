using System;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

public sealed class AuthenticationResult
{
	public required ProfileType ProfileType { get; init; }

	public required string UserName { get; init; }

	public required string Uuid { get; init; }

	public required string AccessToken { get; init; }

	public string RefreshToken { get; init; } = string.Empty;

	public string ClientToken { get; init; } = string.Empty;

	public string TokenType { get; init; } = "Bearer";

	public DateTimeOffset? ExpiresAt { get; init; }

	public string RawJson { get; init; } = string.Empty;

	public string? Server { get; init; }

	public string? ServerName { get; set; }

	public string? LoginName { get; init; }

	public string? Password { get; init; }

	public string? Provider { get; init; }

	public string? DiscoveryAddress { get; set; }

	public string? IdToken { get; set; }
}
