using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.UI.Theme;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyHint : Border, IComponentConnector
{
	public enum Themes
	{
		Blue,
		Red,
		Yellow
	}

	public static readonly DependencyProperty IsWarnProperty = DependencyProperty.Register("IsWarn", typeof(bool), typeof(MyHint), new PropertyMetadata((object)true, (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		MyHint myHint = (MyHint)(object)d;
		object newValue = e.NewValue;
		myHint.Theme = ((newValue is bool && (bool)newValue) ? Themes.Red : Themes.Blue);
	})));

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyHint), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		((MyHint)(object)d).LabText.Text = (string)e.NewValue;
	})));

	private bool isMouseDown;

	public int Uuid = ModBase.GetUuid();

	public bool HasBorder
	{
		get
		{
			return BorderThickness.Top > 0.0;
		}
		set
		{
			if (value)
			{
				BorderThickness = new Thickness(3.0, ModBase.GetWPFSize(1.0), ModBase.GetWPFSize(1.0), ModBase.GetWPFSize(1.0));
			}
			else
			{
				BorderThickness = new Thickness(3.0, 0.0, 0.0, 0.0);
			}
		}
	}

	public Themes Theme
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			UpdateUI();
		}
	} = Themes.Red;

	[Obsolete("IsWarn 已过时。请换用 Theme 属性。")]
	public bool IsWarn
	{
		get
		{
			return Theme == Themes.Red;
		}
		set
		{
			Theme = (value ? Themes.Red : Themes.Blue);
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

	public bool CanClose
	{
		get
		{
			return BtnClose.Visibility == Visibility.Visible;
		}
		set
		{
			BtnClose.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public string RelativeSetup { get; set; } = "";

	public MyHint()
	{
		InitializeComponent();
		UpdateUI();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			UpdateUI();
		};
		Loaded += MyHint_Loaded;
		MouseLeftButtonUp += MyHint_MouseUp;
		MouseLeftButtonDown += MyHint_MouseDown;
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			MyHint_MouseLeave();
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			Dispose();
		};
	}

	private void UpdateUI()
	{
		double sH = 0.0;
		switch (Theme)
		{
		case Themes.Blue:
			sH = 210.0;
			break;
		case Themes.Red:
			sH = 355.0;
			break;
		case Themes.Yellow:
			sH = 40.0;
			break;
		}
		ToneProfile currentTone = ThemeService.CurrentTone;
		Background = new ModBase.MyColor().FromHSL2(sH, 90.0, currentTone.L7 * 100.0);
		BorderBrush = new ModBase.MyColor().FromHSL2(sH, 90.0, currentTone.L2 * 100.0);
		LabText.Foreground = new ModBase.MyColor().FromHSL2(sH, 90.0, currentTone.L2 * 100.0);
		BtnClose.Foreground = new ModBase.MyColor().FromHSL2(sH, 90.0, currentTone.L2 * 100.0);
		HasBorder = HasBorder;
	}

	private void MyHint_Loaded(object sender, RoutedEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		ThemeService.ColorModeChanged += (bool v, ColorTheme theme) =>
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			_ThemeChanged(v, theme);
		};
		ConfigItem val = default;
		if (CanClose && ConfigService.TryGetConfigItemNoType(RelativeSetup, out val) && val.GetValueNoType((object)null) != null)
		{
			Visibility = Visibility.Collapsed;
		}
	}

	private void BtnClose_Click(object sender, EventArgs e)
	{
		ConfigItem val = default;
		if (ConfigService.TryGetConfigItemNoType(RelativeSetup, out val))
		{
			val.SetValueNoType((object)true, (object)null);
		}
		ModAnimation.AniDispose(this, removeFromChildren: false);
	}

	private void MyHint_MouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isMouseDown)
		{
			isMouseDown = false;
			ModBase.Log("[Control] 按下提示条" + (string.IsNullOrEmpty(Name) ? "" : ("：" + Name)));
			e.Handled = true;
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
	}

	private void MyHint_MouseDown(object sender, MouseButtonEventArgs e)
	{
		isMouseDown = true;
	}

	private void MyHint_MouseLeave()
	{
		isMouseDown = false;
	}

	private void _ThemeChanged(bool isDarkMode, ColorTheme theme)
	{
		UpdateUI();
	}

	private void Dispose()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		ThemeService.ColorModeChanged -= _ThemeChanged;
	}


	static MyHint()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected Obj, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected Obj, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected Obj, but got Unknown
	}
}
