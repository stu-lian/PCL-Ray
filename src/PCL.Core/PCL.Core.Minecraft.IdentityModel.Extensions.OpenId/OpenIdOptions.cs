using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel.Extensions.JsonWebToken;
using PCL.Core.Minecraft.IdentityModel.OAuth;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.OpenId;

public record OpenIdOptions
{
	public required string OpenIdDiscoveryAddress { get; set; }

	public required string ClientId { get; set; }

	public bool OnlyDeviceAuthorize { get; set; }

	public string? RedirectUri { get; set; }

	public Dictionary<string, string>? Headers { get; set; }

	public bool EnablePkceSupport { get; set; } = true;

	public required Func<HttpClient> GetClient { get; set; }

	public OpenIdMetadata? Meta { get; internal set; }

	public virtual async Task InitializeAsync(CancellationToken token)
	{
		using HttpResponseMessage response = await HttpRequest.Create(OpenIdDiscoveryAddress).WithHeaders(Headers ?? new Dictionary<string, string>()).SendAsync(GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		Meta = await response.AsJsonAsync<OpenIdMetadata>(null, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<JsonWebKey> GetSignatureKeyAsync(string kid, CancellationToken token)
	{
		if (Meta?.JwksUri == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 加载 OpenID 元数据");
		}
		using HttpResponseMessage response = await HttpRequest.Create(Meta.JwksUri).WithHeaders(Headers ?? new Dictionary<string, string>()).SendAsync(GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		return (await response.AsJsonAsync<JsonWebKeys>(null, token).ConfigureAwait(continueOnCapturedContext: false))?.Keys.SingleOrDefault((JsonWebKey k) => k.Kid == kid) ?? throw new IdentityModelConfigurationException("找不到匹配的 Jwk：" + kid);
	}

	public virtual OAuthClientOptions BuildOAuthOptions()
	{
		if ((object)Meta == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 加载 OpenID 元数据");
		}
		if (string.IsNullOrEmpty(Meta.TokenEndpoint))
		{
			throw new IdentityModelConfigurationException("OpenID 元数据缺少 TokenEndpoint");
		}
		if (!OnlyDeviceAuthorize && string.IsNullOrEmpty(RedirectUri))
		{
			throw new IdentityModelConfigurationException("授权代码流需要设置 RedirectUri");
		}
		return new OAuthClientOptions
		{
			GetClient = GetClient,
			ClientId = ClientId,
			RedirectUri = (OnlyDeviceAuthorize ? string.Empty : RedirectUri),
			Meta = new EndpointMeta
			{
				AuthorizeEndpoint = (Meta?.AuthorizationEndpoint ?? string.Empty),
				DeviceEndpoint = (Meta?.DeviceAuthorizationEndpoint ?? string.Empty),
				TokenEndpoint = Meta.TokenEndpoint
			}
		};
	}
}
