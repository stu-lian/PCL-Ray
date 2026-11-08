using System;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Xaml.Behaviors;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Core;

[ContentProperty("Animation")]
public class RunAnimationAction : TriggerAction<DependencyObject>
{
	public static readonly DependencyProperty AnimationProperty = DependencyProperty.Register("Animation", typeof(IAnimation), typeof(RunAnimationAction), new PropertyMetadata((object)null));

	public static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.Register("TargetProperty", typeof(DependencyProperty), typeof(RunAnimationAction), new PropertyMetadata((object)null));

	public IAnimation Animation
	{
		get
		{
			return (IAnimation)((DependencyObject)this).GetValue(AnimationProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(AnimationProperty, (object)value);
		}
	}

	public DependencyProperty TargetProperty
	{
		get
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected Obj, but got Unknown
			return (DependencyProperty)((DependencyObject)this).GetValue(TargetPropertyProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TargetPropertyProperty, (object)value);
		}
	}

	protected override void Invoke(object parameter)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected Obj, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected Obj, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		DependencyObject val = (DependencyObject)Animation;
		DependencyObject owner;
		if (WpfUtils.IsDependencyPropertySet(val, AnimationExtensions.TargetProperty))
		{
			owner = (DependencyObject)val.GetValue(AnimationExtensions.TargetProperty);
		}
		else
		{
			if (base.AssociatedObject == null)
			{
				throw new InvalidOperationException("未指定动画的目标对象。");
			}
			owner = base.AssociatedObject;
		}
		DependencyProperty property;
		if (WpfUtils.IsDependencyPropertySet(val, AnimationExtensions.TargetPropertyProperty))
		{
			property = (DependencyProperty)val.GetValue(AnimationExtensions.TargetPropertyProperty);
		}
		else if (WpfUtils.IsDependencyPropertySet((DependencyObject)(object)this, TargetPropertyProperty))
		{
			property = TargetProperty;
		}
		else
		{
			if (!(Animation is AnimationGroup))
			{
				throw new InvalidOperationException("未指定动画的目标属性。");
			}
			property = null;
		}
		Animation.RunFireAndForget(new WpfAnimatable(owner, property));
	}

	static RunAnimationAction()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected Obj, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected Obj, but got Unknown
	}
}
