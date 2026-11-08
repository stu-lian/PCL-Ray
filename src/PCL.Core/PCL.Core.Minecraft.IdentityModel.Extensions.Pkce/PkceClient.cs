using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.Pkce;

public class PkceClient(OAuthClientOptions options) : IOAuthClient
{
	private bool _isCallGetAuthorizeUrl;

	private readonly SimpleOAuthClient _client = new SimpleOAuthClient(options);

	private byte[] _ChallengeCode { get; set; } = new byte[32];

	public PkceChallengeOptions ChallengeMethod { get; private set; }

	public string GetAuthorizeUrl(string[] scopes, string state, Dictionary<string, string>? extData)
	{
		RandomNumberGenerator.Fill(_ChallengeCode);
		if (extData == null)
		{
			extData = new Dictionary<string, string>();
		}
		extData["code_challenge"] = ((ChallengeMethod == PkceChallengeOptions.Sha256) ? SHA256Provider.Instance.ComputeHash(_ChallengeCode).ToHexString() : _ChallengeCode.FromBytesToB64UrlSafe());
		extData["code_challenge_method"] = ((ChallengeMethod == PkceChallengeOptions.Sha256) ? "S256" : "plain");
		_isCallGetAuthorizeUrl = true;
		return _client.GetAuthorizeUrl(scopes, state, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithCodeAsync(string code, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		if (!_isCallGetAuthorizeUrl)
		{
			throw new InvalidOperationException("Challenge code is invalid");
		}
		string value = _ChallengeCode.FromBytesToB64UrlSafe();
		if (extData == null)
		{
			extData = new Dictionary<string, string>();
		}
		extData["code_verifier"] = value;
		_isCallGetAuthorizeUrl = false;
		return await _client.AuthorizeWithCodeAsync(code, token, extData);
	}

	public async Task<DeviceCodeData?> GetCodePairAsync(string[] scopes, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		return await _client.GetCodePairAsync(scopes, token, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithDeviceAsync(DeviceCodeData data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		return await _client.AuthorizeWithDeviceAsync(data, token, extData);
	}

	public async Task<AuthorizeResult?> AuthorizeWithSilentAsync(AuthorizeResult data, CancellationToken token, Dictionary<string, string>? extData = null)
	{
		return await _client.AuthorizeWithSilentAsync(data, token, extData);
	}
}
