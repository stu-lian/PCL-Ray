using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PCL.Core.Minecraft.IdentityModel.Yggdrasil;

public record YggdrasilLegacyAuthenticateOptions
{
	public required string YggdrasilApiLocation { get; set; }

	public string? Username { get; set; }

	public string? Password { get; set; }

	public string? AccessToken { get; set; }

	public string? ClientToken { get; set; }

	public required Func<HttpClient> GetClient { get; set; }

	public Dictionary<string, string>? Headers { get; set; }
}
