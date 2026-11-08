using System;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Essentials;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
[DependencyCollector<RpcFunction>("rpc-function", AttributeTargets.Method)]
[DependencyCollector<string>("rpc-property", AttributeTargets.Property)]
public sealed class RegisterRpc : Attribute
{
	public RegisterRpc(string name)
	{
	}
}
