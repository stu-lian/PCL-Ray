using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PCL;

public class MyTextButton : Label
{
	public delegate void ClickEventHandler(object sender, EventArgs e);

	private const int animationTimeIn = 100;

	private const int animationTimeOut = 200;

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyTextButton), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		if (!object.Equals(e.OldValue, e.NewValue))
		{
			MyTextButton button = (MyTextButton)(object)sender;
			ModAnimation.AniStart(new ModAnimation.AniData[3]
			{
				ModAnimation.AaOpacity(button, 0.0 - button.Opacity, 50),
				ModAnimation.AaCode(() =>
				{
					button.Content = e.NewValue;
				}, 0, after: true),
				ModAnimation.AaOpacity(button, 1.0, 170)
			}, "MyTextButton Text " + button.Uuid);
		}
	})));

	private string colorName;

	public bool isMouseDown;

	public int Uuid = ModBase.GetUuid();

	public string Text
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TextProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TextProperty, (object)value);
		}
	}

	public event ClickEventHandler? Click;

	public MyTextButton()
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected Obj, but got Unknown
		SetResourceReference(Control.ForegroundProperty, "ColorBrush1");
		Background = ThemeManager.colorSemiTransparent;
		PreviewMouseLeftButtonDown += MyTextButton_MouseLeftButtonDown;
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			MyTextButton_MouseLeave();
		};
		PreviewMouseLeftButtonUp += MyTextButton_MouseLeftButtonUp;
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			RefreshColor();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeftButtonDown += (object _, MouseButtonEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			RefreshColor();
		};
	}

	private (string ForeName, int Time) GetVisualState()
	{
		if (isMouseDown)
		{
			return (ForeName: "ColorBrush4", Time: 30);
		}
		if (IsMouseOver)
		{
			return (ForeName: "ColorBrush3", Time: 100);
		}
		return (ForeName: "ColorBrush1", Time: 200);
	}

	private void MyTextButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		isMouseDown = true;
		e.Handled = true;
	}

	private void MyTextButton_MouseLeave()
	{
		isMouseDown = false;
	}

	private void MyTextButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (isMouseDown)
		{
			isMouseDown = false;
			ModBase.Log("[Control] 按下文本按钮：" + Text);
			Click?.Invoke(this, null);
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
			e.Handled = true;
		}
	}

	private void RefreshColor()
	{
		var (text, duration) = GetVisualState();
		if (!((colorName ?? "") == (text ?? "")))
		{
			colorName = text;
			ControlVisualHelpers.AnimateColorOrSetResource(this, Control.ForegroundProperty, text, duration, "MyTextButton Color " + Uuid, ControlVisualHelpers.ShouldAnimate(this));
		}
	}

	static MyTextButton()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
	}
}
