using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.IdentityModel.Yggdrasil;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

public sealed class AuthlibProvider : IAuthenticateProvider
{
	private readonly string _apiRoot;

	private readonly string _authServer;

	public AuthlibProvider(string server)
	{
		_apiRoot = _NormalizeServer(server);
		_authServer = _apiRoot + "/authserver";
	}

	public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken token)
	{
		YggdrasilLegacyAuthenticateOptions options = new YggdrasilLegacyAuthenticateOptions
		{
			YggdrasilApiLocation = _apiRoot,
			Username = request.Username,
			Password = request.Password,
			GetClient = () => NetworkService.GetClient()
		};
		YggdrasilAuthenticateResult result = (await new YggdrasilLegacyClient(options).AuthenticateAsync(token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Yggdrasil authentication response was empty.");
		if (!string.IsNullOrWhiteSpace(result.Error))
		{
			throw new IdentityModelAuthenticationException(result.Error, result.ErrorMessage);
		}
		PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile selected = await _SelectProfileAsync(result, request, token).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)selected == null)
		{
			throw new IdentityModelAuthenticationException("invalid_profile", "The authentication server returned no selectable profile.");
		}
		if (result.SelectedProfile?.Id != selected.Id)
		{
			options.AccessToken = result.AccessToken;
			options.ClientToken = result.ClientToken;
			result = (await new YggdrasilLegacyClient(options).RefreshAsync(token, selected).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Yggdrasil profile selection response was empty.");
			if (!string.IsNullOrWhiteSpace(result.Error))
			{
				throw new IdentityModelAuthenticationException(result.Error, result.ErrorMessage);
			}
			selected = result.SelectedProfile ?? selected;
		}
		string serverName = await _GetServerNameAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		return _CreateResult(result, selected, request.Username, request.Password, serverName);
	}

	public async Task<AuthenticationResult> RefreshAsync(McProfile profile, CancellationToken token)
	{
		YggdrasilAuthenticateResult result = (await new YggdrasilLegacyClient(new YggdrasilLegacyAuthenticateOptions
		{
			YggdrasilApiLocation = _apiRoot,
			AccessToken = profile.AccessToken,
			ClientToken = profile.ClientToken,
			GetClient = () => NetworkService.GetClient()
		}).RefreshAsync(token, new PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile
		{
			Id = profile.Uuid,
			Name = profile.UserName
		}).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Yggdrasil refresh response was empty.");
		if (!string.IsNullOrWhiteSpace(result.Error))
		{
			throw new IdentityModelAuthenticationException(result.Error, result.ErrorMessage);
		}
		PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile selected = result.SelectedProfile ?? new PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile
		{
			Id = profile.Uuid,
			Name = profile.UserName
		};
		string serverName = await _GetServerNameAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		return _CreateResult(result, selected, profile.LoginName, profile.Password, serverName);
	}

	public async Task<bool> ValidateAsync(McProfile profile, CancellationToken token)
	{
		if (string.IsNullOrWhiteSpace(profile.AccessToken))
		{
			return false;
		}
		return await new YggdrasilLegacyClient(new YggdrasilLegacyAuthenticateOptions
		{
			YggdrasilApiLocation = _apiRoot,
			AccessToken = profile.AccessToken,
			ClientToken = profile.ClientToken,
			GetClient = () => NetworkService.GetClient()
		}).ValidateAsync(token).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile?> _SelectProfileAsync(YggdrasilAuthenticateResult result, AuthenticationRequest request, CancellationToken token)
	{
		if (!request.ForceReselectProfile && (object)result.SelectedProfile != null)
		{
			return result.SelectedProfile;
		}
		PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile[] available = result.AvailableProfiles ?? Array.Empty<PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile>();
		if (available.Length == 0)
		{
			return result.SelectedProfile;
		}
		if (available.Length == 1 || request.ProfileSelector == null)
		{
			return available[0];
		}
		AuthenticationCandidate[] arg = available.Select((PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile p) => new AuthenticationCandidate(p.Id, p.Name ?? p.Id)).ToArray();
		AuthenticationCandidate selected = await request.ProfileSelector(arg, token).ConfigureAwait(continueOnCapturedContext: false);
		return ((object)selected == null) ? null : available.FirstOrDefault((PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile p) => p.Id == selected.Id);
	}

	private AuthenticationResult _CreateResult(YggdrasilAuthenticateResult result, PCL.Core.Minecraft.IdentityModel.Yggdrasil.Profile selected, string? username, string? password, string? serverName)
	{
		return new AuthenticationResult
		{
			ProfileType = ProfileType.Authlib,
			UserName = (selected.Name ?? string.Empty),
			Uuid = selected.Id,
			AccessToken = (result.AccessToken ?? string.Empty),
			ClientToken = (result.ClientToken ?? string.Empty),
			LoginName = username,
			Password = password,
			Server = _authServer,
			ServerName = serverName,
			Provider = "authlib"
		};
	}

	private async Task<string?> _GetServerNameAsync(CancellationToken token)
	{
		_ = 1;
		try
		{
			using HttpResponseMessage response = await HttpRequest.Create(_apiRoot).SendAsync(NetworkService.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token).ConfigureAwait(continueOnCapturedContext: false);
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

	private static string _NormalizeServer(string server)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(server, "server");
		string text = server.TrimEnd('/');
		if (!text.EndsWith("/authserver", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		string text2 = text;
		int length = "/authserver".Length;
		return text2.Substring(0, text2.Length - length);
	}
}
