using System;

namespace PCL.Core.App.IoC;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LifecycleServiceAttribute(LifecycleState startState) : Attribute
{
	public LifecycleState StartState { get; } = startState;

	public int Priority { get; init; }
}
