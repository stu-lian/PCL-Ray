using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PCL;

public class MyComboBoxItem : ComboBoxItem
{
	private const int animationTimeIn = 100;

	private const int animationTimeOut = 300;

	private string backColorName;

	private double fontOpacity;

	public int Uuid = ModBase.GetUuid();

	public MyComboBoxItem()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected Obj, but got Unknown
		Style = (Style)FindResource("MyComboBoxItem");
		Unselected += (object _, RoutedEventArgs _) =>
		{
			RefreshColor();
		};
		MouseMove += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		Selected += (object _, RoutedEventArgs _) =>
		{
			RefreshColor();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeftButtonUp += MyComboBoxItem_MouseLeftButtonUp;
	}

	private void RefreshColor()
	{
		string text;
		double num;
		int time;
		if (IsSelected)
		{
			text = "ColorBrush6";
			num = 1.0;
			time = 100;
		}
		else if (IsMouseOver)
		{
			text = "ColorBrush8";
			num = 1.0;
			time = 100;
		}
		else if (IsEnabled)
		{
			text = "ColorBrushTransparent";
			num = 1.0;
			time = 300;
		}
		else
		{
			text = "ColorBrushTransparent";
			num = 0.4;
			time = 300;
		}
		if (!((backColorName ?? "") == (text ?? "")) || fontOpacity != num)
		{
			backColorName = text;
			fontOpacity = num;
			if (IsLoaded && ModAnimation.AniControlEnabled == 0)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaColor(this, Control.BackgroundProperty, backColorName, time),
					ModAnimation.AaOpacity(this, fontOpacity - Opacity, time)
				}, "ComboBoxItem Color " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("ComboBoxItem Color " + Uuid);
				SetResourceReference(Control.BackgroundProperty, backColorName);
				Opacity = fontOpacity;
			}
		}
	}

	public override string ToString()
	{
		return Content?.ToString() ?? "";
	}

	public static implicit operator string(MyComboBoxItem value)
	{
		return value.Content?.ToString() ?? "";
	}

	private void MyComboBoxItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		ModBase.Log("[Control] 选择下拉列表项：" + ((object)this).ToString());
	}
}
