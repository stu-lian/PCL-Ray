using System;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Essentials;

[DependencyCollector<PromoteOperationFunction>("promote", AttributeTargets.Method)]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PromoteOperationAttribute : Attribute
{
	public PromoteOperationAttribute(string name)
	{
	}
}
