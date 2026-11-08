using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Minecraft.Profile.Authentication.Utils;
using PCL.Core.Minecraft.Profile.Authentication.Utils.Models;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

public sealed class MicrosoftProvider : IAuthenticateProvider
{
	private const string DeviceEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";

	private const string TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";

	private readonly string _clientId;

	public MicrosoftProvider(string? clientId = null)
	{
		_clientId = (string.IsNullOrWhiteSpace(clientId) ? Secrets.MSOAuthClientId : clientId);
		if (string.IsNullOrWhiteSpace(_clientId))
		{
			throw new InvalidOperationException("Microsoft OAuth client id is not configured.");
		}
	}

	public async Task<DeviceCodeData> GetDeviceCodeAsync(CancellationToken token)
	{
		using HttpResponseMessage response = await HttpRequest.CreatePost("https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode").WithContent(new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["client_id"] = _clientId,
			["scope"] = "XboxLive.signin offline_access"
		})).SendAsync(NetworkService.GetClient("microsoft_id"), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		DeviceCodeData deviceCodeData = (await response.AsJsonAsync<DeviceCodeData>(null, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Microsoft device authorization response was empty.");
		if (deviceCodeData.IsError)
		{
			throw new IdentityModelAuthenticationException(deviceCodeData.Error, deviceCodeData.ErrorDescription);
		}
		deviceCodeData.Validate();
		return deviceCodeData;
	}

	public async Task<AuthorizeResult?> PollDeviceCodeAsync(DeviceCodeData data, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		using HttpResponseMessage response = await HttpRequest.CreatePost("https://login.microsoftonline.com/consumers/oauth2/v2.0/token").WithContent(new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["client_id"] = _clientId,
			["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
			["device_code"] = data.DeviceCode ?? string.Empty
		})).SendAsync(NetworkService.GetClient("microsoft_id"), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		AuthorizeResult authorizeResult = await response.AsJsonAsync<AuthorizeResult>(null, token).ConfigureAwait(continueOnCapturedContext: false);
		authorizeResult?.Validate();
		return authorizeResult;
	}

	public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		AuthorizeResult authorizeResult = request.OAuthResult;
		if ((object)authorizeResult == null && !string.IsNullOrWhiteSpace(request.RefreshToken))
		{
			authorizeResult = await RefreshOAuthAsync(request.RefreshToken, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		if ((object)authorizeResult == null && request.DeviceCodeHandler != null)
		{
			DeviceCodeData device = await GetDeviceCodeAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			authorizeResult = await request.DeviceCodeHandler(new DeviceCodeAuthenticationContext(device, (CancellationToken pollToken) => PollDeviceCodeAsync(device, pollToken)), token).ConfigureAwait(continueOnCapturedContext: false);
		}
		if ((object)authorizeResult == null)
		{
			throw new IdentityModelConfigurationException("Microsoft login requires an OAuth result or device-code handler.");
		}
		return await CompleteAsync(authorizeResult, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<AuthenticationResult> RefreshAsync(McProfile profile, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(profile.RefreshToken))
		{
			throw new IdentityModelAuthenticationException("invalid_grant", "Microsoft refresh token is empty.");
		}
		return await AuthenticateAsync(new AuthenticationRequest
		{
			RefreshToken = profile.RefreshToken
		}, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<bool> ValidateAsync(McProfile profile, CancellationToken token)
	{
		if (profile.IsExpired || string.IsNullOrWhiteSpace(profile.AccessToken))
		{
			return false;
		}
		return await MojangUtils.CheckLicenseAsync(profile, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<AuthorizeResult> RefreshOAuthAsync(string refreshToken, CancellationToken token)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, "refreshToken");
		using HttpResponseMessage response = await HttpRequest.CreatePost("https://login.microsoftonline.com/consumers/oauth2/v2.0/token").WithContent(new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["client_id"] = _clientId,
			["grant_type"] = "refresh_token",
			["refresh_token"] = refreshToken,
			["scope"] = "XboxLive.signin offline_access"
		})).SendAsync(NetworkService.GetClient("microsoft_id"), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		AuthorizeResult authorizeResult = (await response.AsJsonAsync<AuthorizeResult>(null, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Microsoft token response was empty.");
		if (authorizeResult.IsError)
		{
			throw new IdentityModelAuthenticationException(authorizeResult.Error, authorizeResult.ErrorDescription);
		}
		authorizeResult.Validate();
		return authorizeResult with
		{
			RefreshToken = (authorizeResult.RefreshToken ?? refreshToken)
		};
	}

	public async Task<AuthenticationResult> CompleteAsync(AuthorizeResult oauth, CancellationToken token)
	{
		if (oauth.IsError)
		{
			throw new IdentityModelAuthenticationException(oauth.Error, oauth.ErrorDescription);
		}
		if (string.IsNullOrWhiteSpace(oauth.AccessToken))
		{
			throw new IdentityModelAuthenticationException("invalid_token", "Microsoft OAuth did not return an access token.");
		}
		XboxLiveResponse xboxLiveResponse = (await XboxUtils.AuthenticateAsync(new XboxAuthenticate<XboxProperty>
		{
			Properties = new XboxProperty
			{
				RpsTicket = "d=" + oauth.AccessToken
			},
			RelyingParty = "http://auth.xboxlive.com",
			TokenType = "JWT"
		}, isXsts: false, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new IdentityModelAuthenticationException("xbox_authentication_failed", "Xbox Live returned no token.");
		XboxLiveResponse xboxLiveResponse2 = (await XboxUtils.AuthenticateAsync(new XboxAuthenticate<XSTSProperty>
		{
			Properties = new XSTSProperty
			{
				UserTokens = new List<string>(1) { xboxLiveResponse.Token }
			},
			RelyingParty = "rp://api.minecraftservices.com/",
			TokenType = "JWT"
		}, isXsts: true, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new IdentityModelAuthenticationException("xsts_authentication_failed", "XSTS returned no token.");
		List<XboxUserHash> list = xboxLiveResponse2.DisplayClaims?.Xui;
		string text = ((list != null && list.Count > 0) ? xboxLiveResponse2.DisplayClaims.Xui[0].UserHash : null);
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new IdentityModelAuthenticationException("xsts_missing_user_hash", "XSTS response did not contain a user hash.");
		}
		MojangResponse minecraft = (await MojangUtils.AuthenticateAsync(xboxLiveResponse2.Token, text, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new IdentityModelAuthenticationException("minecraft_authentication_failed", "Minecraft authentication returned no token.");
		if (string.IsNullOrWhiteSpace(minecraft.AccessToken))
		{
			throw new IdentityModelAuthenticationException("minecraft_authentication_failed", "Minecraft access token is empty.");
		}
		SafeProfile safe = new SafeProfile
		{
			UserName = minecraft.UserName,
			AccessToken = minecraft.AccessToken,
			Uuid = string.Empty,
			TokenType = minecraft.TokenType
		};
		if (!(await MojangUtils.CheckLicenseAsync(safe, token).ConfigureAwait(continueOnCapturedContext: false)))
		{
			throw new IdentityModelAuthenticationException("minecraft_not_owned", "The Microsoft account does not own Minecraft.");
		}
		MojangProfile mojangProfile = await MojangUtils.GetProfileAsync(safe, token).ConfigureAwait(continueOnCapturedContext: false);
		return new AuthenticationResult
		{
			ProfileType = ProfileType.Microsoft,
			UserName = mojangProfile.Name,
			Uuid = mojangProfile.Uuid,
			AccessToken = minecraft.AccessToken,
			RefreshToken = (oauth.RefreshToken ?? string.Empty),
			ClientToken = mojangProfile.Uuid,
			TokenType = minecraft.TokenType,
			ExpiresAt = ((minecraft.ExpiresIn > 0) ? new DateTimeOffset?(DateTimeOffset.UtcNow.AddSeconds(minecraft.ExpiresIn)) : ((DateTimeOffset?)null)),
			RawJson = JsonSerializer.Serialize(mojangProfile),
			Provider = "microsoft"
		};
	}
}
