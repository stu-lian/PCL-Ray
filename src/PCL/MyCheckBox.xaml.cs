using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyCheckBox : Grid, IComponentConnector
{
	public delegate void ChangeEventHandler(object sender, bool user);

	public delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);

	private const int animationTimeOfCheck = 150;

	private const int animationTimeOfMouseIn = 100;

	private const int animationTimeOfMouseOut = 200;

	public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register("Checked", typeof(bool?), typeof(MyCheckBox), new PropertyMetadata((object)false, (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		MyCheckBox myCheckBox = (MyCheckBox)(object)d;
		if (!myCheckBox.IsLoaded)
		{
			myCheckBox.SyncUI();
		}
	})));

	public static readonly DependencyProperty IsThreeStateProperty = DependencyProperty.Register("IsThreeState", typeof(bool), typeof(MyCheckBox), new PropertyMetadata((object)false));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyCheckBox), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender != null)
		{
			((MyCheckBox)(object)sender).LabText.Text = (string)e.NewValue;
		}
	})));

	private bool? _previousState = false;

	private bool allowMouseDown = true;

	private bool mouseDowned;

	public int Uuid = ModBase.GetUuid();

	public bool? Checked
	{
		get
		{
			return (bool?)((DependencyObject)this).GetValue(CheckedProperty);
		}
		set
		{
			SetChecked(value, user: false);
		}
	}

	public bool IsThreeState
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(IsThreeStateProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(IsThreeStateProperty, (object)value);
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

	public event ChangeEventHandler? Change;

	public event PreviewChangeEventHandler? PreviewChange;

	public MyCheckBox()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected Obj, but got Unknown
		InitializeComponent();
		MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			Checkbox_MouseUp();
		};
		MouseLeftButtonDown += (object _, MouseButtonEventArgs _) =>
		{
			Checkbox_MouseDown();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			Checkbox_MouseLeave();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			Checkbox_IsEnabledChanged();
		};
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			Checkbox_MouseEnterAnimation();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			Checkbox_MouseLeaveAnimation();
		};
	}

	public void SetChecked(bool? value, bool user)
	{
		try
		{
			if (Checked.HasValue && value.HasValue && Checked.Value == value.Value)
			{
				return;
			}
			if ((value.HasValue && value.Value) & user)
			{
				ModBase.RouteEventArgs e = new ModBase.RouteEventArgs(user);
				PreviewChange?.Invoke(this, e);
				if (e.handled)
				{
					mouseDowned = true;
					Checkbox_MouseLeave();
					mouseDowned = false;
					return;
				}
			}
			bool? finalState = GetFinalState(value, IsThreeState);
			_previousState = Checked;
			((DependencyObject)this).SetValue(CheckedProperty, (object)finalState);
			if (IsLoaded)
			{
				Change?.Invoke(this, user);
			}
			SyncUI();
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "设置 Checked 失败");
		}
	}

	private void SyncUI()
	{
		if (ControlVisualHelpers.ShouldAnimate(this))
		{
			allowMouseDown = false;
			bool? finalState = GetFinalState(Checked, IsThreeState);
			bool? previousState = _previousState;
			if (finalState.HasValue)
			{
				if (finalState == true)
				{
					if (previousState.HasValue)
					{
						if (previousState != true)
						{
							AniBackgroundScale();
							AniCheckShow();
							AniColorChecked();
							AniAllowMouseDown();
						}
					}
					else
					{
						AniBackgroundScale();
						AniIndeterminateHide();
						AniCheckShow();
						AniColorChecked();
						AniAllowMouseDown();
					}
				}
				else if (previousState.HasValue)
				{
					if (previousState == true)
					{
						AniBackgroundScale();
						AniCheckHide();
						AniColorUnchecked();
						AniAllowMouseDown();
					}
				}
				else
				{
					AniBackgroundScale();
					AniIndeterminateHide();
					AniCheckHide();
					AniColorUnchecked();
					AniAllowMouseDown();
				}
			}
			else if (previousState.HasValue)
			{
				if (previousState == true)
				{
					AniBackgroundScale();
					AniCheckHide();
					AniIndeterminateShow();
					AniColorUnchecked();
					AniAllowMouseDown();
				}
				else
				{
					AniBackgroundScale();
					AniIndeterminateShow();
					AniColorUnchecked();
					AniAllowMouseDown();
				}
			}
		}
		else
		{
			ModAnimation.AniStop("MyCheckBox Background Scale " + Uuid);
			ModAnimation.AniStop("MyCheckBox Check Scale Show" + Uuid);
			ModAnimation.AniStop("MyCheckBox Check Scale Hide" + Uuid);
			ModAnimation.AniStop("MyCheckBox Indeterminate Scale Show" + Uuid);
			ModAnimation.AniStop("MyCheckBox Indeterminate Scale Hide" + Uuid);
			ModAnimation.AniStop("MyCheckBox BorderColor " + Uuid);
			ModAnimation.AniStop("MyCheckBox AllowMouseDown " + Uuid);
			if (Checked == true)
			{
				((ScaleTransform)ShapeCheck.RenderTransform).ScaleX = 1.0;
				((ScaleTransform)ShapeCheck.RenderTransform).ScaleY = 1.0;
				ShapeBorder.SetResourceReference(Border.BorderBrushProperty, IsEnabled ? "ColorBrush2" : "ColorBrushGray4");
			}
			else
			{
				((ScaleTransform)ShapeCheck.RenderTransform).ScaleX = 0.0;
				((ScaleTransform)ShapeCheck.RenderTransform).ScaleY = 0.0;
				ShapeBorder.SetResourceReference(Border.BorderBrushProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
			}
		}
	}

	private void Checkbox_MouseUp()
	{
		if (!mouseDowned)
		{
			return;
		}
		ModBase.Log("[Control] 按下复选框（" + !Checked + "）：" + Text);
		mouseDowned = false;
		if (IsThreeState)
		{
			bool? flag = Checked;
			if (flag.HasValue)
			{
				if (flag == true)
				{
					SetChecked(null, user: true);
				}
				else
				{
					SetChecked(true, user: true);
				}
			}
			else
			{
				SetChecked(false, user: true);
			}
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " + Uuid);
		}
		else
		{
			SetChecked(!Checked, user: true);
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " + Uuid);
		}
	}

	private void Checkbox_MouseDown()
	{
		if (allowMouseDown)
		{
			mouseDowned = true;
			Focus();
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushBg1", 100), "MyCheckBox Background " + Uuid);
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData> { ModAnimation.AaScale(ShapeBorder, 16.5 - ShapeBorder.Width, 1000, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong), after: false, absolute: true) };
			if (Checked == true)
			{
				list.Add(ModAnimation.AaScaleTransform(ShapeCheck, 0.9 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX, 1000, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)));
			}
			ModAnimation.AniStart(list.ToArray(), "MyCheckBox Scale " + Uuid);
		}
	}

	private void Checkbox_MouseLeave()
	{
		if (mouseDowned)
		{
			mouseDowned = false;
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100), "MyCheckBox Background " + Uuid);
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData> { ModAnimation.AaScale(ShapeBorder, 18.0 - ShapeBorder.Width, 400, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong), after: false, absolute: true) };
			if (Checked == true)
			{
				list.Add(ModAnimation.AaScaleTransform(ShapeCheck, 1.0 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX, 500, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)));
			}
			ModAnimation.AniStart(list.ToArray(), "MyCheckBox Scale " + Uuid);
		}
	}

	private void Checkbox_IsEnabledChanged()
	{
		if (IsLoaded && ModAnimation.AniControlEnabled == 0)
		{
			if (IsEnabled)
			{
				Checkbox_MouseLeaveAnimation();
				return;
			}
			ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty, ThemeManager.colorGray4 - ShapeBorder.BorderBrush, 200), "MyCheckBox BorderColor " + Uuid);
			ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, ThemeManager.colorGray4 - LabText.Foreground, 200), "MyCheckBox TextColor " + Uuid);
		}
		else
		{
			ModAnimation.AniStop("MyCheckBox TextColor " + Uuid);
			ModAnimation.AniStop("MyCheckBox BorderColor " + Uuid);
			LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
			Border shapeBorder = ShapeBorder;
			DependencyProperty borderBrushProperty = Border.BorderBrushProperty;
			string name;
			if (IsEnabled)
			{
				name = ((Checked == true) ? "ColorBrush2" : "ColorBrush1");
			}
			else
			{
				name = "ColorBrushGray4";
			}
			shapeBorder.SetResourceReference(borderBrushProperty, name);
		}
	}

	private void Checkbox_MouseEnterAnimation()
	{
		ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", 100), "MyCheckBox TextColor " + Uuid);
		ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty, "ColorBrush3", 100), "MyCheckBox BorderColor " + Uuid);
	}

	private void Checkbox_MouseLeaveAnimation()
	{
		if (IsEnabled)
		{
			ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4", 200), "MyCheckBox TextColor " + Uuid);
			Border shapeBorder = ShapeBorder;
			DependencyProperty borderBrushProperty = Border.BorderBrushProperty;
			string res;
			if (IsEnabled)
			{
				res = ((Checked == true) ? "ColorBrush2" : "ColorBrush1");
			}
			else
			{
				res = "ColorBrushGray4";
			}
			ModAnimation.AniStart(ModAnimation.AaColor(shapeBorder, borderBrushProperty, res, 200), "MyCheckBox BorderColor " + Uuid);
		}
	}

	private void AniBackgroundScale()
	{
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaScale(ShapeBorder, 12.0 - ShapeBorder.Width, 150, 0, new ModAnimation.AniEaseOutFluent(), after: false, absolute: true),
			ModAnimation.AaScale(ShapeBorder, 6.0, 300, (int)Math.Round(105.0), new ModAnimation.AniEaseOutBack(), after: false, absolute: true)
		}, "MyCheckBox Background Scale " + Uuid);
	}

	private void AniCheckShow()
	{
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(ShapeCheck, 1.0 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX, 300, (int)Math.Round(105.0), new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)), "MyCheckBox Check Scale Show" + Uuid);
	}

	private void AniCheckHide()
	{
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(ShapeCheck, 0.0 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX, (int)Math.Round(135.0), 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)), "MyCheckBox Check Scale Hide" + Uuid);
	}

	private void AniIndeterminateShow()
	{
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(ShapeIndeterminate, 1.0 - ((ScaleTransform)ShapeIndeterminate.RenderTransform).ScaleX, 300, (int)Math.Round(105.0), new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)), "MyCheckBox Indeterminate Scale Show" + Uuid);
	}

	private void AniIndeterminateHide()
	{
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(ShapeIndeterminate, 0.0 - ((ScaleTransform)ShapeIndeterminate.RenderTransform).ScaleX, (int)Math.Round(135.0), 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)), "MyCheckBox Indeterminate Scale Hide" + Uuid);
	}

	private void AniAllowMouseDown()
	{
		ModAnimation.AniStart(ModAnimation.AaCode(() =>
		{
			allowMouseDown = true;
		}, 300), "MyCheckBox AllowMouseDown " + Uuid);
	}

	private void AniColorChecked()
	{
		Border shapeBorder = ShapeBorder;
		DependencyProperty borderBrushProperty = Border.BorderBrushProperty;
		string res;
		if (IsEnabled)
		{
			res = (IsMouseOver ? "ColorBrush3" : "ColorBrush2");
		}
		else
		{
			res = "ColorBrushGray4";
		}
		ModAnimation.AniStart(ModAnimation.AaColor(shapeBorder, borderBrushProperty, res, 150), "MyCheckBox BorderColor " + Uuid);
	}

	private void AniColorUnchecked()
	{
		Border shapeBorder = ShapeBorder;
		DependencyProperty borderBrushProperty = Border.BorderBrushProperty;
		string res;
		if (IsEnabled)
		{
			res = (IsMouseOver ? "ColorBrush3" : "ColorBrush1");
		}
		else
		{
			res = "ColorBrushGray4";
		}
		ModAnimation.AniStart(ModAnimation.AaColor(shapeBorder, borderBrushProperty, res, 150), "MyCheckBox BorderColor " + Uuid);
	}

	private static bool? GetFinalState(bool? value, bool isThreeState)
	{
		if (!isThreeState)
		{
			return value == true;
		}
		return value;
	}

	static MyCheckBox()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected Obj, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected Obj, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected Obj, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected Obj, but got Unknown
	}
}
