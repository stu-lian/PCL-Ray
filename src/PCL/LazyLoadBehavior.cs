using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace PCL;

public class LazyLoadBehavior : Behavior<FrameworkElement>
{
	public static readonly DependencyProperty ActionProperty = DependencyProperty.Register("Action", typeof(Action), typeof(LazyLoadBehavior), new PropertyMetadata((PropertyChangedCallback)null));

	public Action Action
	{
		get
		{
			return (Action)((DependencyObject)this).GetValue(ActionProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ActionProperty, (object)value);
		}
	}

	protected override void OnAttached()
	{
		base.OnAttached();
		base.AssociatedObject.LayoutUpdated += OnLayoutUpdated;
	}

	protected override void OnDetaching()
	{
		base.AssociatedObject.LayoutUpdated -= OnLayoutUpdated;
		base.OnDetaching();
	}

	private void OnLayoutUpdated(object sender, EventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		FrameworkElement associatedObject = base.AssociatedObject;
		if (associatedObject == null)
		{
			return;
		}
		Size renderSize = associatedObject.RenderSize;
		if (renderSize.Width < double.Epsilon || !associatedObject.IsVisible)
		{
			return;
		}
		ScrollViewer scrollViewer = FindParentScrollViewer((DependencyObject)(object)associatedObject);
		if (scrollViewer != null)
		{
			Rect val = associatedObject.TransformToAncestor(scrollViewer).TransformBounds(new Rect(new Point(0.0, 0.0), associatedObject.RenderSize));
			Rect val2 = new Rect(0.0, 0.0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
			if (val2.IntersectsWith(val))
			{
				Action?.Invoke();
				associatedObject.LayoutUpdated -= OnLayoutUpdated;
			}
		}
	}

	private static ScrollViewer FindParentScrollViewer(DependencyObject d)
	{
		for (DependencyObject val = d; val != null; val = VisualTreeHelper.GetParent(val))
		{
			if (val is ScrollViewer result)
			{
				return result;
			}
		}
		return null;
	}

	static LazyLoadBehavior()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected Obj, but got Unknown
	}
}
