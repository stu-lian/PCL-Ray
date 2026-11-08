using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.IdentityModel.Extensions.Pkce;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.OpenId;

public class OpenIdClient(OpenIdOptions options) : IOAuthClient
{
	private IOAuthClient? _client;

	public async Task InitializeAsync(CancellationToken token, bool checkAddress = false)
	{
		await options.InitializeAsync(token);
		OAuthClientOptions oAuthClientOptions = options.BuildOAuthOptions();
		if (checkAddress && oAuthClientOptions.Meta.AuthorizeEndpoint.IsNullOrEmpty() && oAuthClientOptions.Meta.DeviceEndpoint.IsNullOrEmpty())
		{
			throw new IdentityModelConfigurationException("OpenID 元数据缺少授权代码流端点和设备代码流端点");
		}
		OpenIdClient openIdClient = this;
		IOAuthClient client;
		if (!options.EnablePkceSupport)
		{
			IOAuthClient iOAuthClient = new SimpleOAuthClient(oAuthClientOptions);
			client = iOAuthClient;
		}
		else
		{
			IOAuthClient iOAuthClient = new PkceClient(oAuthClientOptions);
			client = iOAuthClient;
		}
		openIdClient._client = client;
	}

	public string GetAuthorizeUrl(string[] scopes, string state, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 OpenID 客户端");
		}
		return _client.GetAuthorizeUrl(scopes, state, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithCodeAsync(string code, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 OpenID 客户端");
		}
		return await _client.AuthorizeWithCodeAsync(code, token, extData);
	}

	public async Task<DeviceCodeData?> GetCodePairAsync(string[] scopes, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 OpenID 客户端");
		}
		return await _client.GetCodePairAsync(scopes, token, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithDeviceAsync(DeviceCodeData data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 OpenID 客户端");
		}
		return await _client.AuthorizeWithDeviceAsync(data, token, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithSilentAsync(AuthorizeResult data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 OpenID 客户端");
		}
		return await _client.AuthorizeWithSilentAsync(data, token, extData);
	}
}
