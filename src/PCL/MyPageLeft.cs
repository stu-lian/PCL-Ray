using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCL;

public class MyPageLeft : Grid
{
	public static DependencyProperty AnimatedControlProperty = DependencyProperty.Register("AnimatedControl", typeof(FrameworkElement), typeof(MyPageLeft));

	private readonly int uuid = ModBase.GetUuid();

	private bool _animatedControlNullWarned;

	public FrameworkElement AnimatedControl
	{
		get
		{
			object value = ((DependencyObject)this).GetValue(AnimatedControlProperty);
			if (value == null && !_animatedControlNullWarned)
			{
				_animatedControlNullWarned = true;
				ModBase.Log("[MyPageLeft] 获取到 AnimatedControl(来自 " + Name + ") 的值为 null", ModBase.LogLevel.Debug);
			}
			return (FrameworkElement)value;
		}
		set
		{
			((DependencyObject)this).SetValue(AnimatedControlProperty, (object)value);
		}
	}

	public void TriggerShowAnimation()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (AnimatedControl == null)
		{
			if (!(RenderTransform is ScaleTransform))
			{
				RenderTransform = new ScaleTransform(0.96, 0.96);
				RenderTransformOrigin = new Point(0.5, 0.5);
			}
			Opacity = 0.0;
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaScaleTransform(this, 1.0 - ((ScaleTransform)RenderTransform).ScaleX, 400, 0, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaOpacity(this, 1.0, 100)
			}, "PageLeft PageChange " + uuid);
			return;
		}
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		int num = 0;
		int num2 = 0;
		foreach (FrameworkElement allAnimControl in GetAllAnimControls(ignoreInvisibility: true))
		{
			FrameworkElement element = MyVirtualizingElement.TryInit(allAnimControl);
			if (element.Visibility == Visibility.Collapsed)
			{
				element.Opacity = 1.0;
				element.RenderTransform = new TranslateTransform(0.0, 0.0);
				if (element is MyListItem)
				{
					((MyListItem)element).isMouseOverAnimationEnabled = true;
				}
				continue;
			}
			element.Opacity = 0.0;
			element.RenderTransform = new TranslateTransform(-25.0, 0.0);
			if (element is MyListItem)
			{
				((MyListItem)element).isMouseOverAnimationEnabled = false;
			}
			list.Add(ModAnimation.AaOpacity(element, (element is TextBlock) ? 0.6 : 1.0, 100, num2, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
			list.Add(ModAnimation.AaTranslateX(element, 5.0, 200, num2, new ModAnimation.AniEaseOutFluent()));
			list.Add(ModAnimation.AaTranslateX(element, 20.0, 300, num2, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)));
			if (element is MyListItem)
			{
				list.Add(ModAnimation.AaCode(() =>
				{
					((MyListItem)element).isMouseOverAnimationEnabled = true;
					((MyListItem)element).RefreshColor(this, new EventArgs());
				}, num2 + 280));
			}
			num2 += Math.Max(15 - num, 7) * 2;
			num++;
		}
		ModAnimation.AniStart(list, "PageLeft PageChange " + uuid);
	}

	public void TriggerHideAnimation()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (AnimatedControl == null)
		{
			if (!(RenderTransform is ScaleTransform))
			{
				RenderTransform = new ScaleTransform(1.0, 1.0);
				RenderTransformOrigin = new Point(0.5, 0.5);
			}
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaScaleTransform(this, 0.95 - ((ScaleTransform)RenderTransform).ScaleX, 110, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaOpacity(this, 0.0 - Opacity, 80, 30)
			}, "PageLeft PageChange " + uuid);
			return;
		}
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		int num = 0;
		List<FrameworkElement> allAnimControls = GetAllAnimControls();
		foreach (FrameworkElement item in allAnimControls)
		{
			list.Add(ModAnimation.AaOpacity(item, 0.0 - item.Opacity, 50, (int)Math.Round(70.0 / (double)allAnimControls.Count * (double)num)));
			list.Add(ModAnimation.AaTranslateX(item, -6.0, 50, (int)Math.Round(70.0 / (double)allAnimControls.Count * (double)num)));
			num++;
		}
		ModAnimation.AniStart(list, "PageLeft PageChange " + uuid);
	}

	private List<FrameworkElement> GetAllAnimControls(bool ignoreInvisibility = false)
	{
		List<FrameworkElement> allControls = new List<FrameworkElement>();
		GetAllAnimControls(AnimatedControl, ref allControls, ignoreInvisibility);
		return allControls;
	}

	private void GetAllAnimControls(FrameworkElement element, ref List<FrameworkElement> allControls, bool ignoreInvisibility)
	{
		if (!ignoreInvisibility && element.Visibility == Visibility.Collapsed)
		{
			return;
		}
		if (element is MyTextButton)
		{
			allControls.Add(element);
			return;
		}
		if (element is MyListItem)
		{
			allControls.Add(element);
			return;
		}
		if (element is ContentControl)
		{
			GetAllAnimControls((FrameworkElement)((ContentControl)element).Content, ref allControls, ignoreInvisibility);
			return;
		}
		if (element is Panel)
		{
			foreach (FrameworkElement child in ((Panel)element).Children)
			{
				GetAllAnimControls(child, ref allControls, ignoreInvisibility);
			}
			return;
		}
		allControls.Add(element);
	}
}
