using System;

namespace PCL.Core.App.IoC;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LifecycleStopAttribute : Attribute
{
}
