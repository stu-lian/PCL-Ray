using System;

namespace PCL.Core.App.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigItemAttribute<TValue> : Attribute
{
	public ConfigItemAttribute(string key, TValue? defaultValue, ConfigSource source = ConfigSource.Shared)
	{
	}
}
