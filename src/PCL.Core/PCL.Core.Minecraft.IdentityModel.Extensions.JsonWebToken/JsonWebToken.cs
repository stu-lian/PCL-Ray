using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PCL.Core.Minecraft.IdentityModel.Extensions.OpenId;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.IdentityModel.Extensions.JsonWebToken;

public class JsonWebToken(string token, OpenIdMetadata meta)
{
	public delegate SecurityToken? TokenValidateCallback(OpenIdMetadata metadata, string token, JsonWebKey? key, string? clientId);

	private bool _verified;

	private JwtSecurityToken? _parsedToken;

	private readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

	public TokenValidateCallback SecurityTokenValidateCallback { get; set; } = (OpenIdMetadata openIdMetadata, string text, JsonWebKey? key, string? clientId) =>
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected Obj, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			JwtSecurityTokenHandler val = new JwtSecurityTokenHandler();
			TokenValidationParameters val2 = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidIssuer = openIdMetadata.Issuer,
				ValidateAudience = !string.IsNullOrEmpty(clientId),
				ValidAudience = clientId,
				ValidateIssuerSigningKey = (key != null),
				IssuerSigningKey = (SecurityKey)(object)((key != null) ? new JsonWebKeySet
				{
					Keys = { key }
				}.Keys[0] : null),
				ValidateLifetime = true,
				ClockSkew = TimeSpan.FromSeconds(60L),
				ValidAlgorithms = ((openIdMetadata.IdTokenSigningAlgValuesSupported.Count == 0) ? null : openIdMetadata.IdTokenSigningAlgValuesSupported)
			};
			SecurityToken val3 = default;
			((SecurityTokenHandler)val).ValidateToken(text, val2, out val3);
			JwtSecurityToken val4 = (JwtSecurityToken)(object)((val3 is JwtSecurityToken) ? val3 : null);
			if (val4 != null && val4.IssuedAt != DateTime.MinValue && val4.IssuedAt > DateTime.UtcNow.Add(val2.ClockSkew))
			{
				throw new SecurityTokenInvalidLifetimeException("Token issued-at time is in the future.");
			}
			return val3;
		}
		catch (Exception ex)
		{
			throw new SecurityException("令牌验证失败：" + ex.Message, ex);
		}
	};

	public bool IsVerified => _verified;

	private JwtSecurityToken _ParseToken()
	{
		if (_parsedToken != null)
		{
			return _parsedToken;
		}
		try
		{
			if (!((SecurityTokenHandler)_tokenHandler).CanReadToken(token))
			{
				throw new SecurityException("无法读取令牌：格式无效");
			}
			_parsedToken = _tokenHandler.ReadJwtToken(token);
			return _parsedToken;
		}
		catch (Exception ex)
		{
			throw new SecurityException("令牌解析失败：" + ex.Message, ex);
		}
	}

	public T? ReadTokenPayload<T>(bool allowUnverifyToken = false)
	{
		if (!allowUnverifyToken && !_verified)
		{
			throw new SecurityException("不安全的令牌");
		}
		try
		{
			JwtSecurityToken val = _ParseToken();
			if (val.Payload == null || ((Dictionary<string, object>)(object)val.Payload).Count == 0)
			{
				throw new InvalidOperationException("令牌 Payload 无效");
			}
			if (typeof(T).IsAssignableFrom(typeof(Dictionary<string, object>)))
			{
				return (T)(object)val.Payload;
			}
			if (typeof(T) == typeof(JwtPayload))
			{
				return (T)(object)val.Payload;
			}
			return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize<JwtPayload>(val.Payload, JsonCompat.SerializerOptions), JsonCompat.SerializerOptions);
		}
		catch (SecurityException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			throw new SecurityException("读取令牌 payload 失败：" + ex2.Message, ex2);
		}
	}

	public T? ReadTokenHeader<T>()
	{
		try
		{
			JwtSecurityToken val = _ParseToken();
			if (val.Header == null || ((Dictionary<string, object>)(object)val.Header).Count == 0)
			{
				throw new InvalidOperationException("令牌中不存在 header 数据");
			}
			if (typeof(T).IsAssignableFrom(typeof(Dictionary<string, object>)))
			{
				return (T)(object)val.Header;
			}
			if (typeof(T) == typeof(JwtHeader))
			{
				return (T)(object)val.Header;
			}
			return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize<JwtHeader>(val.Header, JsonCompat.SerializerOptions), JsonCompat.SerializerOptions);
		}
		catch (Exception ex)
		{
			throw new SecurityException("读取令牌 header 失败：" + ex.Message, ex);
		}
	}

	public SecurityToken? VerifySignature(JsonWebKey key, string? clientId = null)
	{
		try
		{
			SecurityToken? val = SecurityTokenValidateCallback(meta, token, key, clientId);
			if (val != null)
			{
				_verified = true;
			}
			return val;
		}
		catch (Exception ex)
		{
			throw new SecurityException("令牌签名验证失败：" + ex.Message, ex);
		}
	}

	public SecurityToken? VerifySignature()
	{
		try
		{
			SecurityToken? val = SecurityTokenValidateCallback(meta, token, null, null);
			if (val != null)
			{
				_verified = true;
			}
			return val;
		}
		catch (Exception ex)
		{
			throw new SecurityException("令牌验证失败：" + ex.Message, ex);
		}
	}

	public DateTime? GetExpirationTime()
	{
		try
		{
			JwtSecurityToken val = _ParseToken();
			return (((SecurityToken)val).ValidTo != DateTime.MinValue) ? new DateTime?(((SecurityToken)val).ValidTo) : ((DateTime?)null);
		}
		catch (Exception ex)
		{
			throw new SecurityException("获取令牌过期时间失败：" + ex.Message, ex);
		}
	}

	public DateTime? GetIssuedAtTime()
	{
		try
		{
			JwtSecurityToken val = _ParseToken();
			return (((SecurityToken)val).ValidFrom != DateTime.MinValue) ? new DateTime?(((SecurityToken)val).ValidFrom) : ((DateTime?)null);
		}
		catch (Exception ex)
		{
			throw new SecurityException("获取令牌签发时间失败：" + ex.Message, ex);
		}
	}

	public bool IsExpired()
	{
		try
		{
			DateTime? expirationTime = GetExpirationTime();
			return expirationTime.HasValue && DateTime.UtcNow > expirationTime.Value;
		}
		catch
		{
			return true;
		}
	}

	public string? GetClaimValue(string claimType, bool allowUnverifyToken = false)
	{
		try
		{
			Dictionary<string, object>? dictionary = ReadTokenPayload<Dictionary<string, object>>(allowUnverifyToken);
			object value;
			return (dictionary != null && dictionary.TryGetValue(claimType, out value)) ? value.ToString() : null;
		}
		catch (Exception ex)
		{
			throw new SecurityException("获取声明值失败（" + claimType + "）：" + ex.Message, ex);
		}
	}

	public string GetTokenString()
	{
		return token;
	}
}
