using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.Profile.Authentication.Utils.Models;

namespace PCL.Core.Minecraft.Profile.Authentication.Utils;

public static class XboxUtils
{
	private const string XboxLiveAuthServer = "https://user.auth.xboxlive.com/user/authenticate";

	private const string XstsAuthorizeServer = "https://xsts.auth.xboxlive.com/xsts/authorize";

	public static async Task<XboxLiveResponse?> AuthenticateAsync<T>(XboxAuthenticate<T> authData, bool isXsts, CancellationToken token)
	{
		using HttpResponseMessage response = await HttpRequest.CreatePost(isXsts ? "https://xsts.auth.xboxlive.com/xsts/authorize" : "https://user.auth.xboxlive.com/user/authenticate").WithJsonContent(authData).SendAsync(null, addMetedata: true, enableLogging: true, HttpCompletionOption.ResponseContentRead, 3, token)
			.ConfigureAwait(continueOnCapturedContext: false);
		if (!HttpResponseExtension.get_IsSuccess(response) & isXsts)
		{
			string json = await response.AsStringAsync(token).ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				JsonNode jsonNode = JsonNode.Parse(json);
				if (long.TryParse(jsonNode?["XErr"]?.ToString(), out var result))
				{
					throw new IdentityModelAuthenticationException($"xsts_{result}", jsonNode?["Message"]?.ToString() ?? $"XSTS authorization failed with XErr {result}.");
				}
			}
			catch (JsonException)
			{
			}
		}
		response.EnsureSuccessStatusCode();
		return await response.AsJsonAsync<XboxLiveResponse>(null, token);
	}
}
