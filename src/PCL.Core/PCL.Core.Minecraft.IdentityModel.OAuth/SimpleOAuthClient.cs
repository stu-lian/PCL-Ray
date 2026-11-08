using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;

namespace PCL.Core.Minecraft.IdentityModel.OAuth;

public sealed class SimpleOAuthClient(OAuthClientOptions options) : IOAuthClient
{
	public string GetAuthorizeUrl(string[] scopes, string state, Dictionary<string, string>? extData = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(options.Meta.AuthorizeEndpoint, "options.Meta.AuthorizeEndpoint");
		Dictionary<string, string> source = new Dictionary<string, string>(extData ?? new Dictionary<string, string>())
		{
			["response_type"] = "code",
			["scope"] = string.Join(" ", scopes),
			["redirect_uri"] = options.RedirectUri,
			["client_id"] = options.ClientId,
			["state"] = state
		};
		string text = (options.Meta.AuthorizeEndpoint.Contains('?') ? "&" : "?");
		return options.Meta.AuthorizeEndpoint + text + string.Join("&", source.Select((KeyValuePair<string, string> item) => Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value)));
	}

	public async Task<AuthorizeResult?> AuthorizeWithCodeAsync(string code, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		extData = new Dictionary<string, string>(extData ?? new Dictionary<string, string>());
		extData["client_id"] = options.ClientId;
		extData["grant_type"] = "authorization_code";
		extData["code"] = code;
		extData.TryAdd("redirect_uri", options.RedirectUri);
		HttpClient httpClient = options.GetClient();
		using FormUrlEncodedContent content = new FormUrlEncodedContent(extData);
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.Meta.TokenEndpoint).WithContent(content).WithHeaders(options.Headers ?? new Dictionary<string, string>())
			.SendAsync(httpClient, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		AuthorizeResult authorizeResult = await response.AsJsonAsync<AuthorizeResult>(null, token).ConfigureAwait(continueOnCapturedContext: false);
		authorizeResult?.Validate();
		return authorizeResult;
	}

	public async Task<DeviceCodeData?> GetCodePairAsync(string[] scopes, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(options.Meta.DeviceEndpoint, "options.Meta.DeviceEndpoint");
		HttpClient httpClient = options.GetClient();
		extData = new Dictionary<string, string>(extData ?? new Dictionary<string, string>());
		extData["scope"] = string.Join(" ", scopes);
		extData["client_id"] = options.ClientId;
		FormUrlEncodedContent content = new FormUrlEncodedContent(extData);
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.Meta.DeviceEndpoint).WithContent(content).WithHeaders(options.Headers ?? new Dictionary<string, string>())
			.SendAsync(httpClient, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		DeviceCodeData deviceCodeData = await response.AsJsonAsync<DeviceCodeData>(null, token).ConfigureAwait(continueOnCapturedContext: false);
		deviceCodeData?.Validate();
		return deviceCodeData;
	}

	public async Task<AuthorizeResult?> AuthorizeWithDeviceAsync(DeviceCodeData data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (data.IsError)
		{
			throw new IdentityModelAuthenticationException(data.Error, data.ErrorDescription);
		}
		HttpClient httpClient = options.GetClient();
		extData = new Dictionary<string, string>(extData ?? new Dictionary<string, string>());
		extData["client_id"] = options.ClientId;
		extData["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code";
		extData["device_code"] = data.DeviceCode;
		using FormUrlEncodedContent content = new FormUrlEncodedContent(extData);
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.Meta.TokenEndpoint).WithContent(content).WithHeaders(options.Headers ?? new Dictionary<string, string>())
			.SendAsync(httpClient, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		AuthorizeResult authorizeResult = await response.AsJsonAsync<AuthorizeResult>(null, token).ConfigureAwait(continueOnCapturedContext: false);
		authorizeResult?.Validate();
		return authorizeResult;
	}

	public async Task<AuthorizeResult?> AuthorizeWithSilentAsync(AuthorizeResult data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		HttpClient httpClient = options.GetClient();
		if (data.IsError)
		{
			throw new IdentityModelAuthenticationException(data.Error, data.ErrorDescription);
		}
		extData = new Dictionary<string, string>(extData ?? new Dictionary<string, string>());
		extData["refresh_token"] = data.RefreshToken;
		extData["grant_type"] = "refresh_token";
		extData["client_id"] = options.ClientId;
		using FormUrlEncodedContent content = new FormUrlEncodedContent(extData);
		using HttpResponseMessage response = await HttpRequest.CreatePost(options.Meta.TokenEndpoint).WithHeaders(options.Headers ?? new Dictionary<string, string>()).WithContent(content)
			.SendAsync(httpClient, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		AuthorizeResult authorizeResult = await response.AsJsonAsync<AuthorizeResult>(null, token).ConfigureAwait(continueOnCapturedContext: false);
		authorizeResult?.Validate();
		return authorizeResult;
	}
}
