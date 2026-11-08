using System.Text.Json.Serialization;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.IdentityModel.OAuth;

public record DeviceCodeData
{
	public bool IsError => !Error.IsNullOrEmpty();

	[JsonPropertyName("error")]
	public string? Error { get; init; }

	[JsonPropertyName("error_description")]
	public string? ErrorDescription { get; init; }

	[JsonPropertyName("user_code")]
	public string? UserCode { get; init; }

	[JsonPropertyName("device_code")]
	public string? DeviceCode { get; init; }

	[JsonPropertyName("verification_uri")]
	public string? VerificationUri { get; init; }

	[JsonPropertyName("verification_uri_complete")]
	public string? VerificationUriComplete { get; init; }

	[JsonPropertyName("interval")]
	public int? Interval { get; init; }

	[JsonPropertyName("expires_in")]
	public int? ExpiresIn { get; init; }

	public void Validate()
	{
		if (!IsError)
		{
			if (DeviceCode.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "设备授权响应缺少必须字段 device_code");
			}
			if (UserCode.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "设备授权响应缺少必须字段 user_code");
			}
			if (VerificationUri.IsNullOrEmpty())
			{
				throw new IdentityModelAuthenticationException("invalid_request", "设备授权响应缺少必须字段 verification_uri");
			}
			int? expiresIn = ExpiresIn;
			if ((!expiresIn.HasValue || expiresIn.GetValueOrDefault() < 0) ? true : false)
			{
				throw new IdentityModelAuthenticationException("invalid_request", "设备授权响应缺少必须字段 expires_in");
			}
		}
	}
}
