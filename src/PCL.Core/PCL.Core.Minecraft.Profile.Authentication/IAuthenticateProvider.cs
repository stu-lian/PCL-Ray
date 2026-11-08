using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL.Core.Minecraft.Profile.Authentication;

public interface IAuthenticateProvider
{
	Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken token);

	Task<AuthenticationResult> RefreshAsync(McProfile profile, CancellationToken token);

	Task<bool> ValidateAsync(McProfile profile, CancellationToken token);
}
