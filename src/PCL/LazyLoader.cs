using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace PCL;

internal static class LazyLoader
{
	public static void EnableLazyLoad(this FrameworkElement element, Action action)
	{
		LazyLoadBehavior lazyLoadBehavior = new LazyLoadBehavior();
		lazyLoadBehavior.Action = action;
		((FreezableCollection<Behavior>)(object)Interaction.GetBehaviors((DependencyObject)(object)element)).Add((Behavior)(object)lazyLoadBehavior);
	}
}
