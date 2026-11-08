using System;

namespace PCL.Core.App.IoC;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LifecycleDependencyInjectionAttribute : Attribute
{
	public LifecycleDependencyInjectionAttribute(string identifier, AttributeTargets targets)
	{
	}
}
