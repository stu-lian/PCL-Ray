using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FluentValidation;
using FluentValidation.Results;
using PCL.Core.App.Localization;

namespace PCL;

public class MyTextBox : TextBox
{
	public delegate void ValidateChangedEventHandler(object sender, EventArgs e);

	private enum ValidateState
	{
		NotInited,
		Success,
		FailedButTextNotChanged,
		FailedAndShowDetail,
		FailedAndHideDetail,
		NotLoaded
	}

	public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(MyTextBox), new PropertyMetadata((object)new CornerRadius(3.0)));

	public static readonly DependencyProperty ValidateResultProperty = DependencyProperty.Register("ValidateResult", typeof(string), typeof(MyTextBox), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		d.SetValue(IsValidatedPropertyKey, (object)string.IsNullOrEmpty((string)e.NewValue));
	})));

	private static readonly DependencyPropertyKey IsValidatedPropertyKey = DependencyProperty.RegisterReadOnly("IsValidated", typeof(bool), typeof(MyTextBox), new PropertyMetadata((object)true));

	public static readonly DependencyProperty IsValidatedProperty = IsValidatedPropertyKey.DependencyProperty;

	public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register("HintText", typeof(string), typeof(MyTextBox), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject t, DependencyPropertyChangedEventArgs e) =>
	{
		((MyTextBox)(object)t).UpdateHintText();
	})));

	public List<RoutedEventHandler> changedEventList = new List<RoutedEventHandler>();

	private bool isTextChanged;

	private ValidateState shownValidateResult;

	public int Uuid = ModBase.GetUuid();

	public bool HasBackground { get; set; } = true;

	public bool ShowValidateResult { get; set; } = true;

	public CornerRadius CornerRadius
	{
		get
		{
			return (CornerRadius)((DependencyObject)this).GetValue(CornerRadiusProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(CornerRadiusProperty, (object)value);
		}
	}

	private TextBlock labWrong
	{
		get
		{
			if (Template == null)
			{
				return null;
			}
			if (field == null)
			{
				field = (TextBlock)Template.FindName("labWrong", this);
			}
			return field;
		}
	}

	private TextBlock labHint
	{
		get
		{
			if (Template == null)
			{
				return null;
			}
			if (field == null)
			{
				field = (TextBlock)Template.FindName("labHint", this);
			}
			return field;
		}
	}

	public string ValidateResult
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(ValidateResultProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ValidateResultProperty, (object)value);
		}
	}

	public bool IsValidated => (bool)((DependencyObject)this).GetValue(IsValidatedProperty);

	public Collection<IValidator<string>> ValidateRules
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			Validate();
		}
	} = new Collection<IValidator<string>>();

	public string HintText
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(HintTextProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(HintTextProperty, (object)value);
		}
	}

	public event ValidateChangedEventHandler? ValidateChanged;

	public event RoutedEventHandler ValidatedTextChanged
	{
		add
		{
			changedEventList.Add(value);
		}
		remove
		{
			changedEventList.Remove(value);
		}
	}

	public MyTextBox()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected Obj, but got Unknown
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected Obj, but got Unknown
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Validate();
		};
		TextChanged += (object a, TextChangedEventArgs b) =>
		{
			MyTextBox_TextChanged((MyTextBox)a, b);
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
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
		GotFocus += (object _, RoutedEventArgs _) =>
		{
			RefreshColor();
		};
		LostFocus += (object _, RoutedEventArgs _) =>
		{
			RefreshColor();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshTextColor();
		};
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		if (!string.IsNullOrEmpty(HintText) && string.IsNullOrEmpty(labHint.Text))
		{
			UpdateHintText();
		}
	}

	private void OnValidatedTextChanged(object sender, TextChangedEventArgs e)
	{
		foreach (RoutedEventHandler changedEvent in changedEventList)
		{
			changedEvent?.Invoke(sender, e);
		}
	}

	public void Validate()
	{
		string validateResult = GetValidateResult();
		ValidateResult = validateResult;
		if (shownValidateResult != (ValidateState)(IsValidated ? 1 : 3))
		{
			if (IsLoaded && labWrong != null)
			{
				ChangeValidateResult(IsValidated, isLoaded: true);
			}
			else
			{
				ModBase.RunInNewThread(() =>
				{
					Thread.Sleep(30);
					ModBase.RunInUi(() =>
					{
						ChangeValidateResult(IsValidated, isLoaded: false);
					});
				}, "DelayedValidate Change");
			}
		}
		if (!ShowValidateResult || IsValidated)
		{
			return;
		}
		if (IsLoaded && labWrong != null)
		{
			labWrong.Text = ValidateResult;
			return;
		}
		ModBase.RunInNewThread(() =>
		{
			bool isFinished = false;
			while (!isFinished)
			{
				Thread.Sleep(20);
				ModBase.RunInUiWait(() =>
				{
					if (labWrong != null)
					{
						labWrong.Text = ValidateResult;
						isFinished = true;
					}
					if (!IsLoaded)
					{
						isFinished = true;
					}
				});
			}
		}, "DelayedValidate Text");
	}

	public void ForceShowAsSuccess()
	{
		isTextChanged = false;
		ChangeValidateResult(IsValidated, isLoaded: true);
	}

	private void ChangeValidateResult(bool isSuccessful, bool isLoaded)
	{
		if (isLoaded && ModAnimation.AniControlEnabled == 0 && labWrong != null)
		{
			if (isSuccessful || !isTextChanged)
			{
				shownValidateResult = (isSuccessful ? ValidateState.Success : ValidateState.FailedButTextNotChanged);
				ModAnimation.AniStart(new ModAnimation.AniData[3]
				{
					ModAnimation.AaOpacity(labWrong, 0.0 - labWrong.Opacity, 150),
					ModAnimation.AaHeight(labWrong, 0.0 - labWrong.Height, 150, 0, new ModAnimation.AniEaseOutFluent()),
					ModAnimation.AaCode(() =>
					{
						labWrong.Visibility = Visibility.Collapsed;
					}, 0, after: true)
				}, "MyTextBox Validate " + Uuid);
			}
			else if (ShowValidateResult)
			{
				shownValidateResult = ValidateState.FailedAndShowDetail;
				labWrong.Visibility = Visibility.Visible;
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaOpacity(labWrong, 1.0 - labWrong.Opacity, 150),
					ModAnimation.AaHeight(labWrong, 21.0 - labWrong.Height, 150, 0, new ModAnimation.AniEaseOutFluent())
				}, "MyTextBox Validate " + Uuid);
			}
			else
			{
				shownValidateResult = ValidateState.FailedAndHideDetail;
			}
		}
		else
		{
			shownValidateResult = ValidateState.NotLoaded;
		}
		RefreshColor();
		ValidateChanged?.Invoke(this, new EventArgs());
	}

	private void MyTextBox_TextChanged(MyTextBox sender, TextChangedEventArgs e)
	{
		try
		{
			UpdateHintText();
			isTextChanged = IsLoaded;
			Validate();
			if (IsValidated)
			{
				OnValidatedTextChanged(sender, e);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "进行输入验证时出错", ModBase.LogLevel.Critical, null, Lang.Text("Application.Control.Error.OperationFailed"));
		}
	}

	private void UpdateHintText()
	{
		if (labHint != null)
		{
			labHint.Text = (string.IsNullOrEmpty(Text) ? HintText : "");
		}
	}

	private string GetValidateResult()
	{
		string result = string.Empty;
		foreach (IValidator<string> validateRule in ValidateRules)
		{
			FluentValidation.Results.ValidationResult val = validateRule.Validate(Text);
			result = (val.IsValid ? "" : val.Errors[0].ErrorMessage);
		}
		return result;
	}

	private void RefreshColor()
	{
		try
		{
			if (TemplatedParent != null && TemplatedParent is MyComboBox)
			{
				return;
			}
			string text;
			string text2;
			int time;
			if (IsEnabled)
			{
				if (IsValidated || !isTextChanged)
				{
					if (IsFocused)
					{
						text = "ColorBrush3";
						text2 = "ColorBrush7";
						time = 10;
					}
					else if (IsMouseOver)
					{
						text = "ColorBrush4";
						text2 = "ColorBrush7";
						time = 100;
					}
					else
					{
						text = "ColorBrushBg0";
						text2 = "ColorBrushHalfWhite";
						time = 100;
					}
				}
				else
				{
					text = "ColorBrushRedLight";
					text2 = "ColorBrushRedBack";
					time = 200;
				}
			}
			else
			{
				text = "ColorBrushGray5";
				text2 = "ColorBrushGray6";
				time = 200;
			}
			if (!HasBackground)
			{
				text2 = "ColorBrushTransparent";
			}
			if (IsLoaded && ModAnimation.AniControlEnabled == 0)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaColor(this, Control.BorderBrushProperty, text, time),
					ModAnimation.AaColor(this, Control.BackgroundProperty, text2, time)
				}, "MyTextBox Color " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("MyTextBox Color " + Uuid);
				SetResourceReference(Control.BorderBrushProperty, text);
				SetResourceReference(Control.BackgroundProperty, text2);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "文本框颜色改变出错");
		}
	}

	private void RefreshTextColor()
	{
		ModBase.MyColor myColor = (IsEnabled ? ThemeManager.colorGray1 : ThemeManager.colorGray4);
		if ((double)(int)((SolidColorBrush)Foreground).Color.R != myColor.r)
		{
			if (IsLoaded && ModAnimation.AniControlEnabled == 0 && !string.IsNullOrEmpty(Text))
			{
				ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaColor(this, Control.ForegroundProperty, IsEnabled ? "ColorBrushGray1" : "ColorBrushGray4", 200) }, "MyTextBox TextColor " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("MyTextBox TextColor " + Uuid);
				Foreground = myColor;
			}
		}
	}

	private void MyTextBox_KeyUp(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Key == 6)
		{
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
	}

	static MyTextBox()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected Obj, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected Obj, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected Obj, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected Obj, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected Obj, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected Obj, but got Unknown
	}
}
