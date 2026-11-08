using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PCL;

public class MyScrollBar : ScrollBar
{
	public int Uuid = ModBase.GetUuid();

	public MyScrollBar()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected Obj, but got Unknown
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
		GotMouseCapture += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		LostMouseCapture += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		IsVisibleChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
	}

	private void RefreshColor()
	{
		try
		{
			double num;
			int time;
			string text;
			if (!IsVisible)
			{
				num = 0.0;
				time = 20;
				text = "ColorBrush4";
			}
			else if (IsMouseCaptureWithin)
			{
				num = 1.0;
				text = "ColorBrush4";
				time = 50;
			}
			else if (IsMouseOver)
			{
				num = 0.9;
				text = "ColorBrush3";
				time = 130;
			}
			else
			{
				num = 0.5;
				text = "ColorBrush4";
				time = 180;
			}
			if (IsLoaded && ModAnimation.AniControlEnabled == 0)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaColor(this, Control.ForegroundProperty, text, time),
					ModAnimation.AaOpacity(this, num - Opacity, time)
				}, "MyScrollBar Color " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("MyScrollBar Color " + Uuid);
				SetResourceReference(Control.ForegroundProperty, text);
				Opacity = num;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "滚动条颜色改变出错");
		}
	}
}
