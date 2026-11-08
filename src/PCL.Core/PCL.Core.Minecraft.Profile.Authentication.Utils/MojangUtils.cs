using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Authentication.Utils.Models;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication.Utils;

public static class MojangUtils
{
	private const string MojangAuthenticateEndpoint = "https://api.minecraftservices.com/authentication/login_with_xbox";

	private const string LicenseEndpoint = "https://api.minecraftservices.com/entitlements/mcstore";

	private const string ProfileEndpont = "https://api.minecraftservices.com/minecraft/profile";

	public static async Task<MojangResponse?> AuthenticateAsync(string xstsToken, string userHash, CancellationToken token)
	{
		using HttpResponseMessage response = await HttpRequest.CreatePost("https://api.minecraftservices.com/authentication/login_with_xbox").WithContent(new StringContent(JsonSerializer.Serialize(new MojangIdentityToken
		{
			IdentityToken = "XBL3.0 x=" + userHash + ";" + xstsToken
		}), Encoding.UTF8, "application/json")).SendAsync(null, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		return await response.AsJsonAsync<MojangResponse>(null, token).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task<bool> CheckLicenseAsync(SafeProfile profile, CancellationToken token)
	{
		using HttpResponseMessage response = await HttpRequest.Create("https://api.minecraftservices.com/entitlements/mcstore").WithAuthentication(profile.TokenType, profile.AccessToken).SendAsync(null, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		return (await response.AsJsonAsync<JsonObject>(null, token).ConfigureAwait(continueOnCapturedContext: false))?["items"] is JsonArray source && source.Any((JsonNode item) =>
		{
			string text = item?["name"]?.ToString();
			return (text == "product_minecraft" || text == "game_minecraft") ? true : false;
		});
	}

	public static async Task<MojangProfile> GetProfileAsync(SafeProfile profile, CancellationToken token)
	{
		using HttpResponseMessage response = await HttpRequest.Create("https://api.minecraftservices.com/minecraft/profile").WithAuthentication(profile.TokenType, profile.AccessToken).SendAsync(null, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			throw new IdentityModelAuthenticationException("profile_not_created", "The Microsoft account has not created a Minecraft Java profile yet.");
		}
		response.EnsureSuccessStatusCode();
		return (await response.AsJsonAsync<MojangProfile>(null, token).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Minecraft profile response was empty.");
	}
}
