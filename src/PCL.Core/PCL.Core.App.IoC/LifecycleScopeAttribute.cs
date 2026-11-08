using System;

namespace PCL.Core.App.IoC;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LifecycleScopeAttribute : Attribute
{
	public LifecycleScopeAttribute(string identifier, string name, bool asyncStart = true)
	{
	}
}
