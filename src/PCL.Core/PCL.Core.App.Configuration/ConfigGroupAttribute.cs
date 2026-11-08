using System;

namespace PCL.Core.App.Configuration;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigGroupAttribute : Attribute
{
	public ConfigGroupAttribute(string name, ConfigSource source = ConfigSource.Shared)
	{
	}
}
