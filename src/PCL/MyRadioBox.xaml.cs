using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using PCL.Core.App.Localization;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyRadioBox : Grid, IMyRadio, IComponentConnector
{
	public delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);

	public delegate void PreviewCheckEventHandler(object sender, ModBase.RouteEventArgs e);

	private const int animationTimeOfMouseIn = 100;

	private const int animationTimeOfMouseOut = 200;

	private const int animationTimeOfCheck = 150;

	public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register("Checked", typeof(bool), typeof(MyRadioBox), new PropertyMetadata((object)false, (PropertyChangedCallback)((DependencyObject dRaw, DependencyPropertyChangedEventArgs e) =>
	{
		MyRadioBox myRadioBox = (MyRadioBox)(object)dRaw;
		if (!myRadioBox.IsLoaded)
		{
			myRadioBox.SyncUI();
		}
	})));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyRadioBox), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender != null)
		{
			((MyRadioBox)(object)sender).LabText.Text = (string)e.NewValue;
		}
	})));

	private bool allowMouseDown = true;

	private bool mouseDowned;

	public int Uuid = ModBase.GetUuid();

	public bool Checked
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(CheckedProperty);
		}
		set
		{
			SetChecked(value, user: false);
		}
	}

	public InlineCollection Inlines => LabText.Inlines;

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

	public event IMyRadio.CheckEventHandler? Check;

	public event IMyRadio.ChangedEventHandler? Changed;

	public event PreviewCheckEventHandler? PreviewCheck;

	public event PreviewChangeEventHandler? PreviewChange;

	public MyRadioBox()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		InitializeComponent();
		MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			Radiobox_MouseUp();
		};
		MouseLeftButtonDown += (object _, MouseButtonEventArgs _) =>
		{
			Radiobox_MouseDown();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			Radiobox_MouseLeave();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			Radiobox_IsEnabledChanged();
		};
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			Radiobox_MouseEnterAnimation();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			Radiobox_MouseLeaveAnimation();
		};
	}

	public void SetChecked(bool value, bool user)
	{
		try
		{
			if (value & user)
			{
				ModBase.RouteEventArgs e = new ModBase.RouteEventArgs(user);
				PreviewCheck?.Invoke(this, e);
				if (e.handled)
				{
					Radiobox_MouseLeave();
					return;
				}
			}
			bool flag = false;
			if (IsLoaded && value != Checked)
			{
				PreviewChange?.Invoke(this, new ModBase.RouteEventArgs(user));
			}
			if (value != Checked)
			{
				((DependencyObject)this).SetValue(CheckedProperty, (object)value);
				flag = true;
			}
			if (Parent == null)
			{
				return;
			}
			List<MyRadioBox> list = new List<MyRadioBox>();
			int num = 0;
			foreach (object child in ((Panel)(object)Parent).Children)
			{
				if (child is MyRadioBox myRadioBox)
				{
					list.Add(myRadioBox);
					if (myRadioBox.Checked)
					{
						num++;
					}
				}
			}
			int num2 = num;
			if (num2 == 0)
			{
				list[0].Checked = true;
			}
			else if (num2 > 1)
			{
				if (Checked)
				{
					foreach (MyRadioBox item in list)
					{
						if (item.Checked && !((object)item).Equals((object?)this))
						{
							item.Checked = false;
						}
					}
				}
				else
				{
					bool flag2 = false;
					foreach (MyRadioBox item2 in list)
					{
						if (item2.Checked)
						{
							if (flag2)
							{
								item2.Checked = false;
							}
							else
							{
								flag2 = true;
							}
						}
					}
				}
			}
			if (flag)
			{
				if (Checked)
				{
					Check?.Invoke(this, new ModBase.RouteEventArgs(user));
				}
				Changed?.Invoke(this, new ModBase.RouteEventArgs(user));
				ModMain.RaiseCustomEvent((DependencyObject)(object)this);
			}
			SyncUI();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "单选框勾选改变错误", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.Error.OperationFailed"));
		}
	}

	private void SyncUI()
	{
		if (ControlVisualHelpers.ShouldAnimate(this))
		{
			if (Checked)
			{
				if (ShapeDot.Opacity < 0.01)
				{
					ShapeDot.Opacity = 1.0;
				}
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaScale(ShapeBorder, 10.0 - ShapeBorder.Width, 150, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak), after: false, absolute: true),
					ModAnimation.AaScale(ShapeBorder, 8.0, 300, (int)Math.Round(90.0), new ModAnimation.AniEaseOutBack(), after: false, absolute: true)
				}, "MyRadioBox Border " + Uuid);
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaScale(ShapeDot, 9.0 - ShapeDot.Width, (int)Math.Round(390.0), 0, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak), after: false, absolute: true),
					ModAnimation.AaOpacity(ShapeDot, 1.0 - ShapeDot.Opacity, (int)Math.Round(75.0), (int)Math.Round(90.0))
				}, "MyRadioBox Dot " + Uuid);
				Ellipse shapeBorder = ShapeBorder;
				DependencyProperty strokeProperty = Shape.StrokeProperty;
				string res;
				if (IsMouseOver)
				{
					res = "ColorBrush3";
				}
				else
				{
					res = (IsEnabled ? "ColorBrush2" : "ColorBrushGray4");
				}
				ModAnimation.AniStart(ModAnimation.AaColor(shapeBorder, strokeProperty, res, 150), "MyRadioBox BorderColor " + Uuid);
			}
			else
			{
				ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScale(ShapeBorder, 18.0 - ShapeBorder.Width, 150, 0, new ModAnimation.AniEaseOutFluent(), after: false, absolute: true) }, "MyRadioBox Border " + Uuid);
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaScale(ShapeDot, 0.0 - ShapeDot.Width, 150, 0, new ModAnimation.AniEaseInFluent(), after: false, absolute: true),
					ModAnimation.AaOpacity(ShapeDot, 0.0 - ShapeDot.Opacity, (int)Math.Round(75.0), (int)Math.Round(30.0))
				}, "MyRadioBox Dot " + Uuid);
				Ellipse shapeBorder2 = ShapeBorder;
				DependencyProperty strokeProperty2 = Shape.StrokeProperty;
				string res2;
				if (IsMouseOver)
				{
					res2 = "ColorBrush3";
				}
				else
				{
					res2 = (IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
				}
				ModAnimation.AniStart(ModAnimation.AaColor(shapeBorder2, strokeProperty2, res2, 150), "MyRadioBox BorderColor " + Uuid);
			}
		}
		else
		{
			ModAnimation.AniStop("MyRadioBox Border " + Uuid);
			ModAnimation.AniStop("MyRadioBox Dot " + Uuid);
			ModAnimation.AniStop("MyRadioBox BorderColor " + Uuid);
			if (Checked)
			{
				ShapeDot.Width = 9.0;
				ShapeDot.Height = 9.0;
				ShapeDot.Opacity = 1.0;
				ShapeDot.Margin = new Thickness(5.5, 0.0, 0.0, 0.0);
				ShapeBorder.SetResourceReference(Shape.StrokeProperty, IsEnabled ? "ColorBrush2" : "ColorBrushGray4");
			}
			else
			{
				ShapeDot.Width = 0.0;
				ShapeDot.Height = 0.0;
				ShapeDot.Opacity = 0.0;
				ShapeDot.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
				ShapeBorder.SetResourceReference(Shape.StrokeProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
			}
		}
	}

	private void Radiobox_MouseUp()
	{
		if (mouseDowned)
		{
			ModBase.Log("[Control] 按下单选框：" + Text);
			SetChecked(value: true, user: true);
			mouseDowned = false;
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Shape.FillProperty, "ColorBrushHalfWhite", 100), "MyRadioBox Background " + Uuid);
		}
	}

	private void Radiobox_MouseDown()
	{
		mouseDowned = true;
		Focus();
		ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Shape.FillProperty, "ColorBrushBg1", 100), "MyRadioBox Background " + Uuid);
		if (!Checked)
		{
			ModAnimation.AniStart(ModAnimation.AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong), after: false, absolute: true), "MyRadioBox Border " + Uuid);
		}
	}

	private void Radiobox_MouseLeave()
	{
		if (mouseDowned)
		{
			mouseDowned = false;
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Shape.FillProperty, "ColorBrushHalfWhite", 100), "MyRadioBox Background " + Uuid);
			if (!Checked)
			{
				ModAnimation.AniStart(ModAnimation.AaScale(ShapeBorder, 18.0 - ShapeBorder.Width, 400, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong), after: false, absolute: true), "MyRadioBox Border " + Uuid);
			}
		}
	}

	private void Radiobox_IsEnabledChanged()
	{
		if (ControlVisualHelpers.ShouldAnimate(this))
		{
			if (IsEnabled)
			{
				Radiobox_MouseLeaveAnimation();
				return;
			}
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Shape.StrokeProperty, ThemeManager.colorGray4 - ShapeBorder.Stroke, 200), "MyRadioBox BorderColor " + Uuid);
			ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, ThemeManager.colorGray4 - LabText.Foreground, 200), "MyRadioBox TextColor " + Uuid);
		}
		else
		{
			ModAnimation.AniStop("MyRadioBox BorderColor " + Uuid);
			ModAnimation.AniStop("MyRadioBox TextColor " + Uuid);
			LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
			Ellipse shapeBorder = ShapeBorder;
			DependencyProperty strokeProperty = Shape.StrokeProperty;
			string name;
			if (IsEnabled)
			{
				name = (Checked ? "ColorBrush2" : "ColorBrush1");
			}
			else
			{
				name = "ColorBrushGray4";
			}
			shapeBorder.SetResourceReference(strokeProperty, name);
		}
	}

	private void Radiobox_MouseEnterAnimation()
	{
		ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Shape.StrokeProperty, "ColorBrush3", 100), "MyRadioBox BorderColor " + Uuid);
		ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", 100), "MyRadioBox TextColor " + Uuid);
	}

	private void Radiobox_MouseLeaveAnimation()
	{
		if (IsEnabled)
		{
			if (ControlVisualHelpers.ShouldAnimate(this))
			{
				Ellipse shapeBorder = ShapeBorder;
				DependencyProperty strokeProperty = Shape.StrokeProperty;
				string res;
				if (IsEnabled)
				{
					res = (Checked ? "ColorBrush2" : "ColorBrush1");
				}
				else
				{
					res = "ColorBrushGray4";
				}
				ModAnimation.AniStart(ModAnimation.AaColor(shapeBorder, strokeProperty, res, 200), "MyRadioBox BorderColor " + Uuid);
				ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4", 200), "MyRadioBox TextColor " + Uuid);
				return;
			}
			ModAnimation.AniStop("MyRadioBox BorderColor " + Uuid);
			ModAnimation.AniStop("MyRadioBox TextColor " + Uuid);
			Ellipse shapeBorder2 = ShapeBorder;
			DependencyProperty strokeProperty2 = Shape.StrokeProperty;
			string name;
			if (IsEnabled)
			{
				name = (Checked ? "ColorBrush2" : "ColorBrush1");
			}
			else
			{
				name = "ColorBrushGray4";
			}
			shapeBorder2.SetResourceReference(strokeProperty2, name);
			LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
		}
	}

	static MyRadioBox()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected Obj, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected Obj, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected Obj, but got Unknown
	}
}
