using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation.Animatable;

public sealed class WpfAnimatable(DependencyObject owner, DependencyProperty? property) : IAnimatable
{
	public DependencyObject Owner { get; set; } = owner;

	public DependencyProperty? Property { get; set; } = property;

	public object? GetValue()
	{
		DependencyProperty val;
		if (Property == FrameworkElement.WidthProperty)
		{
			val = FrameworkElement.ActualWidthProperty;
		}
		else
		{
			val = ((Property != FrameworkElement.HeightProperty) ? Property : FrameworkElement.ActualHeightProperty);
		}
		ArgumentNullException.ThrowIfNull(val, "actualProperty");
		object value = Owner.GetValue(val);
		if (!(value is SolidColorBrush solidColorBrush))
		{
			if (!(value is Color color))
			{
				if (!(value is ScaleTransform scaleTransform))
				{
					if (value is RotateTransform rotateTransform)
					{
						return (NRotateTransform)rotateTransform;
					}
					return value;
				}
				return (NScaleTransform)scaleTransform;
			}
			return (NColor)color;
		}
		return (NColor)solidColorBrush;
	}

	public void SetValue(object value)
	{
		value = ValueProcessorManager.Filter(value);
		ArgumentNullException.ThrowIfNull(Property, "Property");
		object obj;
		if (!(value is NColor nColor))
		{
			if (value is NScaleTransform nScaleTransform)
			{
				obj = (ScaleTransform)nScaleTransform;
			}
			else
			{
				obj = ((!(value is NRotateTransform nRotateTransform)) ? value : ((RotateTransform)nRotateTransform));
			}
		}
		else
		{
			object obj2 = ((!(Property.Name == "Color")) ? ((SolidColorBrush)nColor) : ((object)(Color)nColor));
			obj = obj2;
		}
		value = obj;
		Owner.SetValue(Property, value);
	}

	public void SetValue<T>(T value)
	{
		value = ValueProcessorManager.Filter(value);
		ArgumentNullException.ThrowIfNull(Property, "Property");
		_SetValueCore(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void _SetValueCore<T>(T value)
	{
		ArgumentNullException.ThrowIfNull(Property, "Property");
		if (typeof(T) == typeof(NColor))
		{
			NColor nColor = Unsafe.As<T, NColor>(ref value);
			Owner.SetValue(Property, (Property.Name == "Color") ? ((object)(Color)nColor) : ((SolidColorBrush)nColor));
		}
		else if (typeof(T) == typeof(NScaleTransform))
		{
			NScaleTransform nScaleTransform = Unsafe.As<T, NScaleTransform>(ref value);
			Owner.SetValue(Property, (object)(ScaleTransform)nScaleTransform);
		}
		else if (typeof(T) == typeof(NRotateTransform))
		{
			NRotateTransform nRotateTransform = Unsafe.As<T, NRotateTransform>(ref value);
			Owner.SetValue(Property, (object)(RotateTransform)nRotateTransform);
		}
		else
		{
			Owner.SetValue(Property, (object)value);
		}
	}
}
