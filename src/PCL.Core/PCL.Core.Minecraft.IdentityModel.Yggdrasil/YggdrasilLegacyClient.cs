using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public sealed class YggdrasilLegacyClient(YggdrasilLegacyAuthenticateOptions options)
{
	public async Task<YggdrasilAuthenticateResult?> AuthenticateAsync(CancellationToken token)
	{
		ArgumentException.ThrowIfNullOrEmpty(options.Username, "options.Username");
		ArgumentException.ThrowIfNullOrEmpty(options.Password, "options.Password");
		YggdrasilCredential content = new YggdrasilCredential
		{
			User = options.Username,
			Password = options.Password,
			ClientToken = options.ClientToken
		};
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.YggdrasilApiLocation + "/authserver/authenticate").WithHeaders(options.Headers ?? new Dictionary<string, string>()).WithJsonContent(content)
			.SendAsync(options.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		return await response.AsJsonAsync<YggdrasilAuthenticateResult>(null, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<YggdrasilAuthenticateResult?> RefreshAsync(CancellationToken token, Profile? seleectedProfile)
	{
		ArgumentException.ThrowIfNullOrEmpty(options.AccessToken, "options.AccessToken");
		YggdrasilRefresh yggdrasilRefresh = new YggdrasilRefresh
		{
			AccessToken = options.AccessToken,
			ClientToken = options.ClientToken
		};
		if ((object)seleectedProfile != null)
		{
			yggdrasilRefresh.SelectedProfile = seleectedProfile;
		}
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.YggdrasilApiLocation + "/authserver/refresh").WithJsonContent(yggdrasilRefresh).WithHeaders(options.Headers ?? new Dictionary<string, string>())
			.SendAsync(options.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		return await response.AsJsonAsync<YggdrasilAuthenticateResult>(null, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<bool> ValidateAsync(CancellationToken token)
	{
		ArgumentException.ThrowIfNullOrEmpty(options.AccessToken, "options.AccessToken");
		YggdrasilRefresh content = new YggdrasilRefresh
		{
			AccessToken = options.AccessToken,
			ClientToken = options.ClientToken
		};
		using HttpResponseMessage httpResponseMessage = await HttpRequest.CreatePost(options.YggdrasilApiLocation + "/authserver/validate").WithHeaders(options.Headers ?? new Dictionary<string, string>()).WithJsonContent(content)
			.SendAsync(options.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		return httpResponseMessage.StatusCode == HttpStatusCode.NoContent;
	}

	public async Task InvalidateAsync(CancellationToken token)
	{
		ArgumentException.ThrowIfNullOrEmpty(options.AccessToken, "options.AccessToken");
		YggdrasilRefresh content = new YggdrasilRefresh
		{
			AccessToken = options.AccessToken,
			ClientToken = options.ClientToken
		};
		using (await HttpRequest.CreatePost(options.YggdrasilApiLocation + "/authserver/invalidate").WithHeaders(options.Headers ?? new Dictionary<string, string>()).WithJsonContent(content)
			.SendAsync(options.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false))
		{
		}
	}

	public async Task<(bool IsSuccess, string ErrorDescription)> SignOutAsync(CancellationToken token)
	{
		JsonObject content = new JsonObject
		{
			["username"] = options.Username,
			["password"] = options.Password
		};
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.YggdrasilApiLocation + "/authserver/signout").WithHeaders(options.Headers ?? new Dictionary<string, string>()).WithJsonContent(content)
			.SendAsync(options.GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		if (response.StatusCode == HttpStatusCode.NoContent)
		{
			return (IsSuccess: true, ErrorDescription: string.Empty);
		}
		string text = await response.AsStringAsync(token).ConfigureAwait(continueOnCapturedContext: false);
		if (string.IsNullOrWhiteSpace(text))
		{
			return (IsSuccess: false, ErrorDescription: string.Empty);
		}
		JsonNode jsonNode;
		try
		{
			jsonNode = JsonCompat.ParseNode(text);
		}
		catch
		{
			return (IsSuccess: false, ErrorDescription: text);
		}
		string item = jsonNode?["errorMessage"]?.ToString() ?? string.Empty;
		return (IsSuccess: false, ErrorDescription: item);
	}
}
