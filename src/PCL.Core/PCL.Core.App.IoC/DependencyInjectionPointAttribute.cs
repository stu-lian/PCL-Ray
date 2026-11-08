using System;

namespace PCL.Core.App.IoC;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class DependencyInjectionPointAttribute : Attribute
{
	public DependencyInjectionPointAttribute(string identifier, bool lifecycleAutoInvoke = true)
	{
	}
}
