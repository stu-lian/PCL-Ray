using System;
using System.Text.Json.Serialization;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.IdentityModel.OAuth;

public record AuthorizeResult
{
	public bool IsError => !Error.IsNullOrEmpty();

	[JsonPropertyName("error")]
	public string? Error { get; init; }

	[JsonPropertyName("error_description")]
	public string? ErrorDescription { get; init; }

	[JsonPropertyName("access_token")]
	public string? AccessToken { get; init; }

	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; init; }

	[JsonPropertyName("id_token")]
	public string? IdToken { get; init; }

	[JsonPropertyName("token_type")]
	public string? TokenType { get; init; }

	[JsonPropertyName("expires_in")]
	public int? ExpiresIn { get; init; }

	[JsonPropertyName("scope")]
	public string? Scope { get; init; }

	[JsonPropertyName("error_uri")]
	public string? ErrorUri { get; init; }

	public void Validate(bool requireIdToken = false, bool requireRefreshToken = false)
	{
		if (!IsError)
		{
			if (AccessToken.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "令牌响应缺少必须字段 access_token");
			}
			if (TokenType.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "令牌响应缺少必须字段 token_type");
			}
			if (!string.Equals(TokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
			{
				throw new IdentityModelAuthenticationException("invalid_request", "不支持的 token_type：" + TokenType);
			}
			int? expiresIn = ExpiresIn;
			if ((!expiresIn.HasValue || expiresIn.GetValueOrDefault() < 0) ? true : false)
			{
				throw new IdentityModelAuthenticationException("invalid_request", "令牌响应缺少必须字段 expires_in");
			}
			if (requireRefreshToken && RefreshToken.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "已申请 offline_access 但令牌响应缺少 refresh_token");
			}
			if (requireIdToken && IdToken.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "已申请 openid 但令牌响应缺少 id_token");
			}
		}
	}
}
