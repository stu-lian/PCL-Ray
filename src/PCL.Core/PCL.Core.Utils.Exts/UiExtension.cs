using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCL.Core.Utils.Exts;

public static class UiExtension
{
	public static bool IsVisibleInWindow(this FrameworkElement element, Window mainWindow)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (!element.IsVisible)
		{
			return false;
		}
		try
		{
			Rect val = element.TransformToAncestor(mainWindow).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
			Rect val2 = new Rect(0.0, 0.0, mainWindow.ActualWidth, mainWindow.ActualHeight);
			return val2.IntersectsWith(val);
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	public static bool IsTextTrimmed(this TextBlock textBlock)
	{
		if (textBlock.TextTrimming == TextTrimming.None)
		{
			return false;
		}
		try
		{
			return new FormattedText(textBlock.Text, CultureInfo.CurrentCulture, textBlock.FlowDirection, new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch), textBlock.FontSize, textBlock.Foreground, VisualTreeHelper.GetDpi(textBlock).PixelsPerDip).Width > textBlock.ActualWidth;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
