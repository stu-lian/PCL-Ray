using System;

namespace PCL.Core.App.Configuration;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AnyConfigItemAttribute<TValue> : Attribute
{
	public AnyConfigItemAttribute(string key, ConfigSource source = ConfigSource.Shared)
	{
	}
}
