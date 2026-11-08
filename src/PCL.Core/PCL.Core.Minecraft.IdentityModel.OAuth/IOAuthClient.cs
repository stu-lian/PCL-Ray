using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.IdentityModel.OAuth;

public interface IOAuthClient
{
	string GetAuthorizeUrl(string[] scopes, string state, Dictionary<string, string>? extData);

	Task<AuthorizeResult?> AuthorizeWithCodeAsync(string code, CancellationToken token, Dictionary<string, string>? extData = null);

	Task<DeviceCodeData?> GetCodePairAsync(string[] scopes, CancellationToken token, Dictionary<string, string>? extData = null);

	Task<AuthorizeResult?> AuthorizeWithDeviceAsync(DeviceCodeData data, CancellationToken token, Dictionary<string, string>? extData = null);

	Task<AuthorizeResult?> AuthorizeWithSilentAsync(AuthorizeResult data, CancellationToken token, Dictionary<string, string>? extData = null);
}
