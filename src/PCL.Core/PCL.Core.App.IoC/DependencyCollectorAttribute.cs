using System;

namespace PCL.Core.App.IoC;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DependencyCollectorAttribute<TDependency> : Attribute
{
	public DependencyCollectorAttribute(string identifier, AttributeTargets targets)
	{
	}
}
