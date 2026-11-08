using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel.Extensions.OpenId;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.YggdrasilConnect;

public record YggdrasilOptions : OpenIdOptions
{
	private readonly string[] _scopesRequired = new string[3] { "openid", "Yggdrasil.PlayerProfiles.Select", "Yggdrasil.Server.Join" };

	public override async Task InitializeAsync(CancellationToken token)
	{
		if (!Uri.TryCreate(OpenIdDiscoveryAddress, UriKind.Absolute, out Uri discoveryUri) || discoveryUri.Scheme != Uri.UriSchemeHttps)
		{
			throw new IdentityModelConfigurationException("Yggdrasil Connect discovery address must use HTTPS.");
		}
		using HttpResponseMessage response = await HttpRequest.Create(OpenIdDiscoveryAddress).WithHeaders(Headers ?? new Dictionary<string, string>()).SendAsync(GetClient(), addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		_ValidateMetadata(metadata: (YggdrasilConnectMetaData)(Meta = (await response.AsJsonAsync<YggdrasilConnectMetaData>(null, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new IdentityModelConfigurationException("无法加载 Yggdrasil Connect 元数据")), discoveryUri: discoveryUri);
		string[] array = _scopesRequired.Except(Meta.ScopesSupported).ToArray();
		if (array.Length != 0)
		{
			throw new IdentityModelConfigurationException("Yggdrasil Connect 元数据缺少必要 scope：" + string.Join(", ", array));
		}
	}

	public override OAuthClientOptions BuildOAuthOptions()
	{
		if (Meta is YggdrasilConnectMetaData yggdrasilConnectMetaData)
		{
			if (ClientId.IsNullOrEmpty() && !yggdrasilConnectMetaData.SharedClientId.IsNullOrEmpty())
			{
				ClientId = yggdrasilConnectMetaData.SharedClientId;
			}
			OAuthClientOptions oAuthClientOptions = base.BuildOAuthOptions();
			if (oAuthClientOptions.ClientId.IsNullOrEmpty())
			{
				throw new IdentityModelConfigurationException("Yggdrasil Connect 需要设置 ClientId，或由元数据提供 sharedClient_id");
			}
			return oAuthClientOptions;
		}
		throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 加载 Yggdrasil Connect 元数据");
	}

	private static void _ValidateMetadata(Uri discoveryUri, YggdrasilConnectMetaData metadata)
	{
		if (!Uri.TryCreate(metadata.Issuer, UriKind.Absolute, out Uri result) || result.Scheme != Uri.UriSchemeHttps)
		{
			throw new IdentityModelConfigurationException("Yggdrasil Connect issuer must use HTTPS.");
		}
		if (!string.Equals(discoveryUri.GetLeftPart(UriPartial.Authority), result.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
		{
			throw new IdentityModelConfigurationException("Yggdrasil Connect discovery address must belong to the issuer.");
		}
		if (!string.IsNullOrWhiteSpace(metadata.AuthorizationEndpoint))
		{
			_ValidateHttpsEndpoint(metadata.AuthorizationEndpoint, "authorization");
		}
		_ValidateHttpsEndpoint(metadata.TokenEndpoint, "token");
		_ValidateHttpsEndpoint(metadata.DeviceAuthorizationEndpoint, "device authorization");
		_ValidateHttpsEndpoint(metadata.JwksUri, "JWKS");
		if (!string.IsNullOrWhiteSpace(metadata.UserInfoEndpoint))
		{
			_ValidateHttpsEndpoint(metadata.UserInfoEndpoint, "userinfo");
		}
	}

	private static void _ValidateHttpsEndpoint(string? value, string name)
	{
		if (!Uri.TryCreate(value, UriKind.Absolute, out Uri result) || result.Scheme != Uri.UriSchemeHttps)
		{
			throw new IdentityModelConfigurationException("Yggdrasil Connect " + name + " endpoint must use HTTPS.");
		}
	}
}
