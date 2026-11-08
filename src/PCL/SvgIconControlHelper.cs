using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.UI;
using PCL.Core.UI.Animation.Easings;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

internal static class SvgIconControlHelper
{
	internal static bool HasSvgIcon(string? icon)
	{
		return !string.IsNullOrWhiteSpace(icon);
	}

	internal static void ApplyVisibility(Path legacyIcon, SvgIcon svgIcon, bool useSvgIcon)
	{
		legacyIcon.Visibility = (useSvgIcon ? Visibility.Collapsed : Visibility.Visible);
		((UIElement)(object)svgIcon).Visibility = ((!useSvgIcon) ? Visibility.Collapsed : Visibility.Visible);
	}

	internal static void ApplyIcon(Path legacyIcon, SvgIcon svgIcon, string? svgIconName)
	{
		bool useSvgIcon = HasSvgIcon(svgIconName);
		svgIcon.Icon = svgIconName ?? string.Empty;
		ApplyVisibility(legacyIcon, svgIcon, useSvgIcon);
	}

	internal static void SetIconBrush(Path legacyIcon, SvgIcon svgIcon, bool useSvgIcon, Brush brush)
	{
		if (useSvgIcon)
		{
			svgIcon.IconBrush = brush;
		}
		else
		{
			legacyIcon.Fill = brush;
		}
	}

	internal static void SetIconResource(Path legacyIcon, SvgIcon svgIcon, bool useSvgIcon, string resourceKey)
	{
		if (useSvgIcon)
		{
			((FrameworkElement)(object)svgIcon).SetResourceReference(SvgIcon.IconBrushProperty, (object)resourceKey);
		}
		else
		{
			legacyIcon.SetResourceReference(Shape.FillProperty, resourceKey);
		}
	}

	internal static void AnimateSvgIconBrushTo(SvgIcon svgIcon, string resourceKey, int duration, string? animationKey = null)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (((UIElement)(object)svgIcon).Visibility == Visibility.Visible)
		{
			svgIcon.AnimateIconBrushTo(ResolveResourceColor(resourceKey), (TimeSpan?)TimeSpan.FromMilliseconds(duration), (IEasing)null, animationKey);
		}
	}

	internal static void AnimateSvgIconBrushTo(SvgIcon svgIcon, ModBase.MyColor color, int duration, string? animationKey = null)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (((UIElement)(object)svgIcon).Visibility == Visibility.Visible)
		{
			svgIcon.AnimateIconBrushTo(new NColor((Color)color), (TimeSpan?)TimeSpan.FromMilliseconds(duration), (IEasing)null, animationKey);
		}
	}

	private static NColor ResolveResourceColor(string resourceKey)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!ThemeManager.AppResources.Contains(resourceKey))
		{
			return new NColor(resourceKey);
		}
		object obj = ThemeManager.AppResources[resourceKey];
		if (!(obj is SolidColorBrush solidColorBrush))
		{
			if (!(obj is Color color))
			{
				if (obj is Brush brush)
				{
					return new NColor(brush);
				}
				return new NColor(resourceKey);
			}
			return new NColor(color);
		}
		return new NColor(solidColorBrush);
	}
}
