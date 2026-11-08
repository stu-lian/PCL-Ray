using System;
using System.Collections.Generic;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.IdentityModel.Extensions.JsonWebToken;
using PCL.Core.Minecraft.IdentityModel.Extensions.YggdrasilConnect;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Minecraft.IdentityModel.Yggdrasil;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

public sealed class YggdrasilConnectProvider : IAuthenticateProvider
{
	private sealed record YggdrasilConnectClaims
	{
		[JsonPropertyName("sub")]
		public required string? Subject { get; init; }

		[JsonPropertyName("aud")]
		public required string? Audience { get; init; }

		[JsonPropertyName("selectedProfile")]
		public PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile? SelectedProfile { get; init; }

		[JsonPropertyName("availableProfiles")]
		public PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile[]? AvailableProfiles { get; init; }
	}

	private static readonly IReadOnlyDictionary<string, string> _BuiltInClientIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["littleskin.cn"] = "1514" };

	private static readonly string[] _Scopes = new string[4] { "openid", "offline_access", "Yggdrasil.PlayerProfiles.Select", "Yggdrasil.Server.Join" };

	private readonly YggdrasilOptions _options;

	private readonly string? _yggdrasilServer;

	private YggdrasilClient? _client;

	public YggdrasilConnectProvider(string discoveryAddress, string? clientId = null, string? yggdrasilServer = null)
	{
		string text = clientId;
		if (string.IsNullOrWhiteSpace(text) && Uri.TryCreate(yggdrasilServer, UriKind.Absolute, out Uri result) && TryGetBuiltInClientId(result.Host, out string clientId2))
		{
			text = clientId2;
		}
		_options = new YggdrasilOptions
		{
			OpenIdDiscoveryAddress = discoveryAddress,
			ClientId = (text?.Trim() ?? string.Empty),
			OnlyDeviceAuthorize = true,
			GetClient = () => NetworkService.GetClient()
		};
		_yggdrasilServer = yggdrasilServer;
	}

	public static bool TryGetBuiltInClientId(string host, out string clientId)
	{
		string key = host.Trim().TrimEnd('.');
		return _BuiltInClientIds.TryGetValue(key, out clientId);
	}

	public Task InitializeAsync(CancellationToken token)
	{
		_client = new YggdrasilClient(_options);
		return _client.InitializeAsync(token);
	}

	public Task<DeviceCodeData?> GetCodePairAsync(CancellationToken token)
	{
		_EnsureInitialized();
		return _client.GetCodePairAsync(_Scopes, token);
	}

	public Task<AuthorizeResult?> PollDeviceCodeAsync(DeviceCodeData data, CancellationToken token)
	{
		_EnsureInitialized();
		return _client.AuthorizeWithDeviceAsync(data, token);
	}

	public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken token)
	{
		_EnsureInitialized();
		AuthorizeResult authorizeResult = request.OAuthResult;
		bool isSilentRefresh = false;
		if ((object)authorizeResult == null && !string.IsNullOrWhiteSpace(request.RefreshToken))
		{
			isSilentRefresh = true;
			authorizeResult = await _client.AuthorizeWithSilentAsync(new AuthorizeResult
			{
				RefreshToken = request.RefreshToken
			}, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		if ((object)authorizeResult == null && request.DeviceCodeHandler != null)
		{
			DeviceCodeData code = (await GetCodePairAsync(token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new IdentityModelAuthenticationException("device_authorization_failed", "The server returned no device code.");
			authorizeResult = await request.DeviceCodeHandler(new DeviceCodeAuthenticationContext(code, (CancellationToken pollToken) => PollDeviceCodeAsync(code, pollToken)), token).ConfigureAwait(continueOnCapturedContext: false);
		}
		if ((object)authorizeResult == null)
		{
			throw new IdentityModelConfigurationException("Yggdrasil Connect login requires an OAuth result or device-code handler.");
		}
		return await _CompleteAsync(authorizeResult, request, isSilentRefresh, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task<AuthenticationResult> RefreshAsync(McProfile profile, CancellationToken token)
	{
		return AuthenticateAsync(new AuthenticationRequest
		{
			RefreshToken = profile.RefreshToken,
			IdToken = profile.IdToken,
			ExistingProfile = profile
		}, token);
	}

	public async Task<bool> ValidateAsync(McProfile profile, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(profile.AccessToken))
		{
			return false;
		}
		_EnsureInitialized();
		using HttpResponseMessage responseMessage = await HttpRequest.Create(_options.Meta.UserInfoEndpoint).WithBearerToken(profile.AccessToken).SendAsync(NetworkService.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		return HttpResponseExtension.get_IsSuccess(responseMessage);
	}

	private async Task<AuthenticationResult> _CompleteAsync(AuthorizeResult oauth, AuthenticationRequest request, bool isSilentRefresh, CancellationToken token)
	{
		if (oauth.IsError)
		{
			throw new IdentityModelAuthenticationException(oauth.Error, oauth.ErrorDescription);
		}
		oauth.Validate(requireIdToken: true, !isSilentRefresh);
		YggdrasilConnectClaims yggdrasilConnectClaims = await _ReadIdTokenAsync(oauth.IdToken ?? request.IdToken, token).ConfigureAwait(continueOnCapturedContext: false);
		PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile profile = yggdrasilConnectClaims.SelectedProfile;
		PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile[] available = yggdrasilConnectClaims.AvailableProfiles ?? Array.Empty<PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile>();
		if ((object)profile == null || available.Length == 0)
		{
			YggdrasilConnectClaims yggdrasilConnectClaims2 = await _GetUserInfoAsync(oauth.AccessToken, token).ConfigureAwait(continueOnCapturedContext: false);
			if ((object)profile == null)
			{
				profile = yggdrasilConnectClaims2?.SelectedProfile;
			}
			if (available.Length == 0)
			{
				available = yggdrasilConnectClaims2?.AvailableProfiles ?? Array.Empty<PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile>();
			}
		}
		if ((object)profile == null && !request.ForceReselectProfile)
		{
			McProfile existingProfile = request.ExistingProfile;
			if (existingProfile != null)
			{
				string existingUuid = existingProfile.Uuid;
				if (existingUuid != null && !string.IsNullOrWhiteSpace(existingUuid))
				{
					profile = available.FirstOrDefault((PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile p) => string.Equals(p.Id, existingUuid, StringComparison.Ordinal));
				}
			}
		}
		if ((object)profile == null && available.Length == 0)
		{
			McProfile existingProfile2 = request.ExistingProfile;
			if (existingProfile2 != null && !string.IsNullOrWhiteSpace(existingProfile2.Uuid) && !string.IsNullOrWhiteSpace(existingProfile2.UserName))
			{
				profile = new PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile
				{
					Id = existingProfile2.Uuid,
					Name = existingProfile2.UserName
				};
			}
		}
		if ((request.ForceReselectProfile || (object)profile == null) && available.Length > 1)
		{
			if (request.ProfileSelector == null)
			{
				throw new IdentityModelAuthenticationException("profile_selection_required", "Yggdrasil Connect returned multiple player profiles.");
			}
			AuthenticationCandidate[] arg = available.Select((PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile p) => new AuthenticationCandidate(p.Id, p.Name)).ToArray();
			AuthenticationCandidate selected = await request.ProfileSelector(arg, token).ConfigureAwait(continueOnCapturedContext: false);
			if ((object)selected == null)
			{
				throw new IdentityModelAuthenticationException("access_denied", "Yggdrasil Connect profile selection was cancelled.");
			}
			profile = available.FirstOrDefault((PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile p) => p.Id == selected.Id);
		}
		if ((object)profile == null)
		{
			profile = available.FirstOrDefault();
		}
		if ((object)profile == null)
		{
			throw new IdentityModelAuthenticationException("invalid_profile", "Yggdrasil Connect returned no player profile.");
		}
		AuthenticationResult authenticationResult = new AuthenticationResult
		{
			ProfileType = ProfileType.YggdrasilConnect,
			UserName = profile.Name,
			Uuid = profile.Id,
			AccessToken = oauth.AccessToken,
			RefreshToken = (oauth.RefreshToken ?? request.RefreshToken),
			ClientToken = profile.Id,
			TokenType = oauth.TokenType,
			ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(oauth.ExpiresIn.Value),
			Provider = "yggdrasil-connect",
			Server = _GetServerAddress()
		};
		AuthenticationResult authenticationResult2 = authenticationResult;
		authenticationResult2.ServerName = await _GetServerNameAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		authenticationResult.DiscoveryAddress = _options.OpenIdDiscoveryAddress;
		authenticationResult.IdToken = oauth.IdToken ?? request.IdToken;
		return authenticationResult;
	}

	private async Task<YggdrasilConnectClaims> _ReadIdTokenAsync(string? idToken, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(idToken))
		{
			throw new SecurityException("Yggdrasil Connect did not return an ID token.");
		}
		JwtSecurityToken val = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
		if (string.IsNullOrWhiteSpace(val.Header.Kid))
		{
			throw new SecurityException("Yggdrasil Connect ID token has no key id.");
		}
		JsonWebKey key = await _options.GetSignatureKeyAsync(val.Header.Kid, token).ConfigureAwait(continueOnCapturedContext: false);
		JsonWebToken jsonWebToken = new JsonWebToken(idToken, _options.Meta);
		if (jsonWebToken.VerifySignature(key, _options.ClientId) == null)
		{
			throw new SecurityException("Yggdrasil Connect ID token validation failed.");
		}
		return jsonWebToken.ReadTokenPayload<YggdrasilConnectClaims>() ?? throw new SecurityException("Yggdrasil Connect ID token payload was empty.");
	}

	private async Task<YggdrasilConnectClaims?> _GetUserInfoAsync(string accessToken, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(_options.Meta?.UserInfoEndpoint))
		{
			return null;
		}
		using HttpResponseMessage response = await HttpRequest.Create(_options.Meta.UserInfoEndpoint).WithBearerToken(accessToken).SendAsync(NetworkService.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		if (!HttpResponseExtension.get_IsSuccess(response))
		{
			return null;
		}
		try
		{
			return await response.AsJsonAsync<YggdrasilConnectClaims>(null, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (InvalidDataException)
		{
			return null;
		}
	}

	private async Task<string?> _GetServerNameAsync(CancellationToken token)
	{
		_ = 1;
		try
		{
			string text = _GetServerAddress().TrimEnd('/');
			if (text.EndsWith("/authserver", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "/authserver".Length;
				text = text2.Substring(0, text2.Length - length);
			}
			using HttpResponseMessage response = await HttpRequest.Create(text).SendAsync(NetworkService.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token).ConfigureAwait(continueOnCapturedContext: false);
			if (!HttpResponseExtension.get_IsSuccess(response))
			{
				return null;
			}
			return (await response.AsJsonAsync<JsonObject>(null, token).ConfigureAwait(continueOnCapturedContext: false))?["meta"]?["serverName"]?.ToString();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			return null;
		}
	}

	private void _EnsureInitialized()
	{
		if (_client == null || (object)_options.Meta == null)
		{
			throw new IdentityModelConfigurationException("Please initialize Yggdrasil Connect before use.");
		}
	}

	private string _GetServerAddress()
	{
		if (!string.IsNullOrWhiteSpace(_yggdrasilServer))
		{
			string text = _yggdrasilServer.TrimEnd('/');
			if (!text.EndsWith("/authserver", StringComparison.OrdinalIgnoreCase))
			{
				return text + "/authserver";
			}
			return text;
		}
		string openIdDiscoveryAddress = _options.OpenIdDiscoveryAddress;
		int num = openIdDiscoveryAddress.IndexOf("/.well-known/", StringComparison.OrdinalIgnoreCase);
		if (num <= 0)
		{
			return openIdDiscoveryAddress.TrimEnd('/');
		}
		return openIdDiscoveryAddress.Substring(0, num).TrimEnd('/');
	}
}
