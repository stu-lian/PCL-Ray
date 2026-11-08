using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace PCL;

public partial class MySearchBox : MyCard, IComponentConnector
{
	public delegate void SearchEventHandler(object sender, EventArgs e);

	public delegate void TextChangedEventHandler(object sender, EventArgs e);

	public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register("HintText", typeof(string), typeof(MySearchBox), new PropertyMetadata((object)string.Empty, (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		((MySearchBox)(object)d).TextBox.HintText = (string)e.NewValue;
	})));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MySearchBox), new PropertyMetadata((object)string.Empty, (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		((MySearchBox)(object)d).TextBox.Text = (string)e.NewValue;
	})));

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

	public Visibility SearchButtonVisibility
	{
		get
		{
			return BtnSearch.Visibility;
		}
		set
		{
			BtnClear.Margin = new Thickness(0.0, 0.0, (value == Visibility.Visible) ? 70 : 10, 0.0);
			BtnSearch.Visibility = value;
		}
	}

	public event TextChangedEventHandler? TextChanged;

	public event SearchEventHandler? Search;

	public MySearchBox()
	{
		InitializeComponent();
		Loaded += MySearchBox_Loaded;
	}

	private void MySearchBox_KeyUp(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Key == 6)
		{
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
	}

	private void MySearchBox_Loaded(object sender, RoutedEventArgs e)
	{
		TextBox.Focus();
	}

	private void Text_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdateClearButtonState();
		((DependencyObject)this).SetCurrentValue(TextProperty, (object)TextBox.Text);
		TextChanged?.Invoke(sender, e);
	}

	private void BtnClear_Click(object sender, EventArgs e)
	{
		TextBox.Text = "";
		TextBox.Focus();
	}

	private void BtnSearch_Click(object sender, MouseButtonEventArgs e)
	{
		Search?.Invoke(sender, e);
	}

	private void UpdateClearButtonState()
	{
		bool flag = !string.IsNullOrEmpty(TextBox.Text);
		ModAnimation.AniStart(ModAnimation.AaOpacity(BtnClear, flag ? (1.0 - BtnClear.Opacity) : (0.0 - BtnClear.Opacity), 90), "MySearchBox ClearBtn " + uuid);
		BtnClear.IsHitTestVisible = flag;
	}


	static MySearchBox()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected Obj, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected Obj, but got Unknown
	}
}
