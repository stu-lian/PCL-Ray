using System;

namespace PCL.Core.Minecraft.IdentityModel;

public class IdentityModelAuthenticationException(string? error, string? errorDescription, Exception? innerException = null) : IdentityModelException(_BuildMessage(error, errorDescription), innerException)
{
	public string? Error { get; } = error;

	public string? ErrorDescription { get; } = errorDescription;

	private static string _BuildMessage(string? error, string? errorDescription)
	{
		if (!string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(errorDescription))
		{
			return "认证失败：" + error + " - " + errorDescription;
		}
		if (!string.IsNullOrWhiteSpace(errorDescription))
		{
			return "认证失败：" + errorDescription;
		}
		if (!string.IsNullOrWhiteSpace(error))
		{
			return "认证失败：" + error;
		}
		return "认证失败";
	}
}
