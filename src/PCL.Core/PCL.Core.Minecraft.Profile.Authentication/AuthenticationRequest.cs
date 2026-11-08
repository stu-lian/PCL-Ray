using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

public sealed record AuthenticationRequest
{
	public string? Username { get; init; }

	public string? Password { get; init; }

	public string? RefreshToken { get; init; }

	public string? IdToken { get; init; }

	public McProfile? ExistingProfile { get; init; }

	public AuthorizeResult? OAuthResult { get; init; }

	public string? Server { get; init; }

	public string? DiscoveryAddress { get; init; }

	public string? ClientId { get; init; }

	public bool ForceRefresh { get; init; }

	public bool ForceReselectProfile { get; init; }

	public Func<DeviceCodeAuthenticationContext, CancellationToken, Task<AuthorizeResult?>>? DeviceCodeHandler { get; init; }

	public Func<Exception, CancellationToken, Task<bool>>? RefreshFailureHandler { get; init; }

	public Func<IReadOnlyList<AuthenticationCandidate>, CancellationToken, Task<AuthenticationCandidate?>>? ProfileSelector { get; init; }
}
