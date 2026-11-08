using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Minecraft.IdentityModel.OAuth;

namespace PCL.Core.Minecraft.Profile.Authentication;

public sealed record DeviceCodeAuthenticationContext(DeviceCodeData Data, Func<CancellationToken, Task<AuthorizeResult?>> PollAsync);
