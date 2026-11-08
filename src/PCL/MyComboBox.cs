using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PCL.Core.App.Localization;

namespace PCL;

public class MyComboBox : ComboBox
{
	public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

	public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register("HintText", typeof(string), typeof(MyComboBox), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		MyComboBox myComboBox = (MyComboBox)(object)d;
		if (myComboBox.textBox != null)
		{
			myComboBox.textBox.HintText = (string)e.NewValue;
		}
	})));

	private string _Text;

	private bool isMouseDown;

	private bool isTextChanging;

	private double realWidth;

	private MyTextBox textBox;

	public int Uuid = ModBase.GetUuid();

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

	public new string Text
	{
		get
		{
			if (IsEditable)
			{
				if (textBox == null)
				{
					return _Text ?? "";
				}
				return textBox.Text ?? "";
			}
			return (SelectedItem ?? "").ToString();
		}
		set
		{
			if (IsEditable)
			{
				if (textBox == null)
				{
					_Text = value;
				}
				else
				{
					textBox.Text = value;
				}
				return;
			}
			throw new NotSupportedException("该 ComboBox 不支持修改文本。");
		}
	}

	public bool DropDownWidthSync { get; set; } = true;

	public ContentPresenter ContentPresenter => (ContentPresenter)Template.FindName("PART_Content", this);

	public event TextChangedEventHandler? TextChanged;

	public MyComboBox()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected Obj, but got Unknown
		_Text = SelectedItem?.ToString() ?? "";
		PreviewMouseLeftButtonDown += MyComboBox_PreviewMouseLeftButtonDown;
		PreviewMouseLeftButtonUp += MyComboBox_PreviewMouseLeftButtonUp;
		MouseLeave += MyComboBox_PreviewMouseLeftButtonUp;
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
		PreviewMouseLeftButtonDown += (object _, MouseButtonEventArgs _) =>
		{
			RefreshColor();
		};
		PreviewMouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			RefreshColor();
		};
		GotKeyboardFocus += (object _, KeyboardFocusChangedEventArgs _) =>
		{
			RefreshColor();
		};
		DropDownOpened += MyComboBox_DropDownOpened;
		DropDownClosed += MyComboBox_DropDownClosed;
		TextChanged += MyComboBox_TextChanged;
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		if (!IsEditable)
		{
			return;
		}
		try
		{
			textBox = (MyTextBox)Template.FindName("PART_EditableTextBox", this);
			textBox.AddHandler(UIElement.LostFocusEvent, (RoutedEventHandler)((object _, RoutedEventArgs _) =>
			{
				RefreshColor();
			}));
			textBox.changedEventList.Add((object sender, RoutedEventArgs e) =>
			{
				TextChanged?.Invoke(sender, (TextChangedEventArgs)e);
			});
			textBox.Tag = Tag;
			if (string.IsNullOrEmpty(Text))
			{
				textBox.Text = _Text;
			}
			else
			{
				TextChanged?.Invoke(this, null);
			}
			if (HintText.Length > 0)
			{
				textBox.HintText = HintText;
			}
			textBox.SetResourceReference(TextBoxBase.CaretBrushProperty, "ColorBrushGray1");
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "初始化可编辑文本框失败（" + Name + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Application.Control.Error.OperationFailed"));
		}
	}

	private void MyComboBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		isMouseDown = true;
	}

	private void MyComboBox_PreviewMouseLeftButtonUp(object sender, EventArgs e)
	{
		isMouseDown = false;
	}

	public void RefreshColor()
	{
		string text;
		string text2;
		int time;
		if (IsEnabled)
		{
			if (isMouseDown || IsDropDownOpen || (IsEditable && ((MyTextBox)Template.FindName("PART_EditableTextBox", this)).IsFocused))
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
			text = "ColorBrushGray5";
			text2 = "ColorBrushGray6";
			time = 200;
		}
		if (IsLoaded && ModAnimation.AniControlEnabled == 0)
		{
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaColor(this, Control.ForegroundProperty, text, time),
				ModAnimation.AaColor(this, Control.BackgroundProperty, text2, time)
			}, "MyComboBox Color " + Uuid);
		}
		else
		{
			ModAnimation.AniStop("MyComboBox Color " + Uuid);
			SetResourceReference(Control.ForegroundProperty, text);
			SetResourceReference(Control.BackgroundProperty, text2);
		}
	}

	private void MyComboBox_DropDownOpened(object sender, EventArgs e)
	{
		realWidth = Width;
		if (DropDownWidthSync)
		{
			Width = ActualWidth;
		}
		try
		{
			Grid grid = (Grid)Template.FindName("PanPopup", this);
			grid.Opacity = ModMain.frmMain.Opacity;
			if (!DropDownWidthSync)
			{
				grid.MinWidth = ActualWidth;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "设置下拉框属性失败", ModBase.LogLevel.Feedback, null, Lang.Text("Application.Control.Error.OperationFailed"));
		}
	}

	private void MyComboBox_DropDownClosed(object sender, EventArgs e)
	{
		Width = realWidth;
	}

	private void MyComboBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!isTextChanging && IsEditable && SelectedItem != null && !(Text == SelectedItem.ToString()))
		{
			string text = Text;
			int selectionStart = textBox.SelectionStart;
			isTextChanging = true;
			SelectedItem = null;
			Text = text;
			textBox.SelectionStart = selectionStart;
			isTextChanging = false;
		}
	}

	protected override DependencyObject GetContainerForItemOverride()
	{
		return (DependencyObject)(object)new MyComboBoxItem();
	}

	private void MyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (IsLoaded && ModAnimation.AniControlEnabled == 0)
		{
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
	}

	protected override bool IsItemItsOwnContainerOverride(object item)
	{
		if (!(item is MyComboBoxItem))
		{
			return base.IsItemItsOwnContainerOverride(item);
		}
		return true;
	}

	static MyComboBox()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
	}
}
