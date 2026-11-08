using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.IdentityModel.Extensions.OpenId;
using PCL.Core.Minecraft.IdentityModel.OAuth;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.YggdrasilConnect;

public class YggdrasilClient : IOAuthClient
{
	private OpenIdClient? _client;

	private YggdrasilOptions _options;

	public YggdrasilClient(YggdrasilOptions options)
	{
		_options = options;
	}

	public async Task InitializeAsync(CancellationToken token)
	{
		_client = new OpenIdClient(_options);
		await _client.InitializeAsync(token, checkAddress: true);
	}

	public string GetAuthorizeUrl(string[] scopes, string state, Dictionary<string, string>? extData)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 Yggdrasil Connect 客户端");
		}
		return _client.GetAuthorizeUrl(scopes, state, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithCodeAsync(string code, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 Yggdrasil Connect 客户端");
		}
		return await _client.AuthorizeWithCodeAsync(code, token, extData);
	}

	public async Task<DeviceCodeData?> GetCodePairAsync(string[] scopes, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 Yggdrasil Connect 客户端");
		}
		return await _client.GetCodePairAsync(scopes, token, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithDeviceAsync(DeviceCodeData data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 Yggdrasil Connect 客户端");
		}
		return await _client.AuthorizeWithDeviceAsync(data, token, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithSilentAsync(AuthorizeResult data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (_client == null)
		{
			throw new IdentityModelConfigurationException("请先调用 InitializeAsync() 初始化 Yggdrasil Connect 客户端");
		}
		return await _client.AuthorizeWithSilentAsync(data, token, extData);
	}
}
