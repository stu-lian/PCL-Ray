using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace PCL.Core.App.IoC;

public abstract class DependencyGroup
{
	public abstract void InvokeInjection(Delegate injection);
}
public class DependencyGroup<TValue> : DependencyGroup
{
	public ImmutableList<TValue> Items { get; set; } = ImmutableList<TValue>.Empty;

	public override void InvokeInjection(Delegate injection)
	{
		if (injection is Action<ImmutableList<TValue>> action)
		{
			action(Items);
			return;
		}
		if (injection is Func<ImmutableList<TValue>, Task> func)
		{
			func(Items).Wait();
			return;
		}
		throw new InvalidCastException("Injection point signature mismatch, must be: void/Task (ImmutableList<" + typeof(TValue).Name + ">)");
	}
}
public class DependencyGroup<TValue, TArguments> : DependencyGroup
{
	public ImmutableList<(TValue value, TArguments args)> Items { get; set; } = ImmutableList<(TValue value, TArguments args)>.Empty;

	public override void InvokeInjection(Delegate injection)
	{
		if (injection is Action<ImmutableList<(TValue, TArguments)>> action)
		{
			action(Items);
			return;
		}
		if (injection is Func<ImmutableList<(TValue, TArguments)>, Task> func)
		{
			func(Items).Wait();
			return;
		}
		throw new InvalidCastException($"Injection point signature mismatch, must be: void/Task (ImmutableList<({typeof(TValue).Name}, {typeof(TArguments).Name})>)");
	}
}
