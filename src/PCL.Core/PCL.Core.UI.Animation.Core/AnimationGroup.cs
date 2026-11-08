using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Core;

[ContentProperty("Children")]
public abstract class AnimationGroup : AnimationBase
{
	public static readonly DependencyProperty ChildrenProperty = DependencyProperty.Register("Children", typeof(ObservableCollection<IAnimation>), typeof(AnimationGroup), new PropertyMetadata((PropertyChangedCallback)null));

	public ObservableCollection<IAnimation> Children
	{
		get
		{
			return (ObservableCollection<IAnimation>)((DependencyObject)this).GetValue(ChildrenProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ChildrenProperty, (object)value);
		}
	}

	protected List<IAnimation> ChildrenCore { get; } = new List<IAnimation>();

	public override int CurrentFrame { get; set; }

	protected AnimationGroup()
	{
		((DependencyObject)this).SetCurrentValue(ChildrenProperty, (object)new ObservableCollection<IAnimation>());
	}

	public override void Cancel()
	{
		Status = AnimationStatus.Canceled;
		CurrentFrame = 0;
		lock (ChildrenCore)
		{
			foreach (IAnimation item in ChildrenCore)
			{
				item.Cancel();
			}
			ChildrenCore.Clear();
		}
	}

	public void CancelAndClear()
	{
		Cancel();
		AnimationService.UIAccessProvider.Invoke(() =>
		{
			Children.Clear();
		});
	}

	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		return null;
	}

	protected static IAnimatable ResolveTarget(IAnimation animation, IAnimatable defaultTarget)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected Obj, but got Unknown
		DependencyObject val = (DependencyObject)((animation is DependencyObject) ? animation : null);
		if (val == null)
		{
			return defaultTarget;
		}
		DependencyObject val2 = null;
		DependencyProperty val3 = null;
		if (WpfUtils.IsDependencyPropertySet(val, AnimationExtensions.TargetProperty))
		{
			val2 = (DependencyObject)val.GetValue(AnimationExtensions.TargetProperty);
		}
		else if (defaultTarget is WpfAnimatable wpfAnimatable)
		{
			val2 = wpfAnimatable.Owner;
		}
		if (WpfUtils.IsDependencyPropertySet(val, AnimationExtensions.TargetPropertyProperty))
		{
			val3 = (DependencyProperty)val.GetValue(AnimationExtensions.TargetPropertyProperty);
		}
		else if (defaultTarget is WpfAnimatable wpfAnimatable2)
		{
			val3 = wpfAnimatable2.Property;
		}
		if (val2 == null || val3 == null)
		{
			return defaultTarget;
		}
		return new WpfAnimatable(val2, val3);
	}

	protected static Task CreateChildAwaiter(IAnimation animation)
	{
		TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		if (animation.Status == AnimationStatus.Completed)
		{
			tcs.TrySetResult();
			return tcs.Task;
		}
		EventHandler handler = null;
		handler = (object? _, EventArgs _) =>
		{
			animation.Completed -= handler;
			tcs.TrySetResult();
		};
		animation.Completed += handler;
		if (animation.Status != AnimationStatus.Completed)
		{
			return tcs.Task;
		}
		animation.Completed -= handler;
		tcs.TrySetResult();
		return tcs.Task;
	}

	static AnimationGroup()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected Obj, but got Unknown
	}
}
