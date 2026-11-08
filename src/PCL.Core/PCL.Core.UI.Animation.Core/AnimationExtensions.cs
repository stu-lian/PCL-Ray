using System;
using System.Windows;
using System.Windows.Media;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.UI.Animation.Core;

public static class AnimationExtensions
{
	public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached("Target", typeof(DependencyObject), typeof(AnimationExtensions), new PropertyMetadata((object)null));

	public static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.RegisterAttached("TargetProperty", typeof(DependencyProperty), typeof(AnimationExtensions), new PropertyMetadata((object)null));

	public static void SetTarget(DependencyObject element, DependencyObject value)
	{
		if (!(element is IAnimation))
		{
			throw new InvalidOperationException("AnimationExtensions.Target 只能附加到 IAnimation 实例上。");
		}
		element.SetValue(TargetProperty, (object)value);
	}

	public static DependencyObject GetTarget(DependencyObject element)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		return (DependencyObject)element.GetValue(TargetProperty);
	}

	public static void SetTargetProperty(DependencyObject element, DependencyProperty value)
	{
		if (!(element is IAnimation))
		{
			throw new InvalidOperationException("AnimationExtensions.TargetProperty 只能附加到 IAnimation 实例上。");
		}
		element.SetValue(TargetPropertyProperty, (object)value);
	}

	public static DependencyProperty GetTargetProperty(DependencyObject element)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		return (DependencyProperty)element.GetValue(TargetPropertyProperty);
	}

	public static void Animate(this DependencyObject target, TimeSpan? duration = null, TimeSpan? delay = null, IEasing? easing = null, AnimationValueType valueType = AnimationValueType.Relative, int iterationCount = 1, double? width = null, double? height = null, double? opacity = null, double? radius = null, TranslateTransform? translate = null, double? translateX = null, double? translateY = null, RotateTransform? rotate = null, double? rotateAngle = null, ScaleTransform? scale = null, double? scaleX = null, double? scaleY = null, SkewTransform? skew = null, double? skewX = null, double? skewY = null, Thickness? margin = null, double? marginLeft = null, double? marginTop = null, double? marginRight = null, double? marginBottom = null, Thickness? padding = null, double? paddingLeft = null, double? paddingTop = null, double? paddingRight = null, double? paddingBottom = null, NColor? background = null, NColor? foreground = null)
	{
	}

	static AnimationExtensions()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected Obj, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected Obj, but got Unknown
	}
}
