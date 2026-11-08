using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI.Controls.SvgIcon;
using PCL.Core.UI.Theme;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyRadioButton : Border, IComponentConnector
{
	public delegate void ChangeEventHandler(MyRadioButton sender, bool raiseByMouse);

	public delegate void CheckEventHandler(MyRadioButton sender, bool raiseByMouse);

	public delegate void PreviewClickEventHandler(object sender, ModBase.RouteEventArgs e);

	public enum ColorState
	{
		White,
		Highlight
	}

	private const int animationTimeOfMouseIn = 90;

	private const int animationTimeOfMouseOut = 150;

	private const int animationTimeOfCheck = 120;

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyRadioButton), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender is MyRadioButton { LabText: not null } myRadioButton)
		{
			myRadioButton.LabText.Text = (string)e.NewValue;
		}
	})));

	private bool _Checked;

	private bool _hasLegacyLogo;

	private bool isMouseDown;

	public int Uuid = ModBase.GetUuid();


	public string Logo
	{
		get
		{
			return ((object)ShapeLogo.Data)?.ToString() ?? string.Empty;
		}
		set
		{
			if (ShapeLogo != null)
			{
				_hasLegacyLogo = !string.IsNullOrWhiteSpace(value);
				ShapeLogo.Data = (_hasLegacyLogo ? ((Geometry)new GeometryConverter().ConvertFromString(value)) : null);
				SvgIconControlHelper.ApplyVisibility(ShapeLogo, ShapeSvgIcon, IsUsingSvgIcon);
				RefreshLogoHostVisibility();
			}
		}
	}

	public string SvgIcon
	{
		get;
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (!(value == field))
			{
				field = value;
				if (ShapeLogo != null && ShapeSvgIcon != null)
				{
					SvgIconControlHelper.ApplyIcon(ShapeLogo, ShapeSvgIcon, field);
					ApplyLogoScale();
					RefreshLogoHostVisibility();
					RefreshColor();
				}
			}
		}
	} = string.Empty;

	private bool IsUsingSvgIcon => SvgIconControlHelper.HasSvgIcon(SvgIcon);

	private double EffectiveLogoScale
	{
		get
		{
			if (!IsUsingSvgIcon)
			{
				return LogoScale;
			}
			return 1.0;
		}
	}

	private bool HasAnyIcon
	{
		get
		{
			if (!IsUsingSvgIcon)
			{
				return _hasLegacyLogo;
			}
			return true;
		}
	}

	public double LogoScale
	{
		get;
		set
		{
			field = value;
			ApplyLogoScale();
		}
	} = 1.0;

	public bool Checked
	{
		get
		{
			return _Checked;
		}
		set
		{
			SetChecked(value, raiseByMouse: false, anime: true);
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

	public ColorState ColorType
	{
		get;
		set
		{
			field = value;
			RefreshColor();
		}
	}

	public event CheckEventHandler? Check;

	public event PreviewClickEventHandler? PreviewClick;

	public MyRadioButton()
	{
		InitializeComponent();
		RefreshLogoHostVisibility();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Expected Obj, but got Unknown
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Expected Obj, but got Unknown
			TextBlock labText = LabText;
			if (labText != null)
			{
				labText.Text = (string)((DependencyObject)this).GetValue(TextProperty);
			}
			ThemeService.ColorModeChanged += OnColorModeChanged;
			ThemeService.ColorThemeChanged += OnColorThemeChanged;
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected Obj, but got Unknown
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Expected Obj, but got Unknown
			ThemeService.ColorModeChanged -= OnColorModeChanged;
			ThemeService.ColorThemeChanged -= OnColorThemeChanged;
		};
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
		MouseEnter += RefreshColor;
		MouseLeave += RefreshColor;
		Loaded += RefreshColor;
	}

	private void OnColorModeChanged(bool isDarkMode, ColorTheme theme)
	{
		((DispatcherObject)this).Dispatcher.Invoke((Action)RefreshMyRadioButtonColor);
	}

	private void OnColorThemeChanged(ColorTheme theme)
	{
		((DispatcherObject)this).Dispatcher.Invoke((Action)RefreshMyRadioButtonColor);
	}

	private void ApplyLogoScale()
	{
		Grid logoHost = LogoHost;
		if (logoHost != null)
		{
			logoHost.RenderTransform = new ScaleTransform
			{
				ScaleX = EffectiveLogoScale,
				ScaleY = EffectiveLogoScale
			};
		}
	}

	private void RefreshLogoHostVisibility()
	{
		if (LogoHost != null && LabText != null)
		{
			if (HasAnyIcon)
			{
				LogoHost.Visibility = Visibility.Visible;
				LogoHost.Width = 16.0;
			}
			else
			{
				LogoHost.Visibility = Visibility.Visible;
				LogoHost.Width = 0.0;
			}
			LogoHost.Height = 16.0;
			LogoHost.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
			LabText.Margin = new Thickness(8.0, 0.0, 12.0, 0.0);
		}
	}

	public void SetChecked(bool value, bool raiseByMouse, bool anime)
	{
		try
		{
			bool flag = false;
			if (_Checked != value)
			{
				_Checked = value;
				flag = true;
			}
			if (Parent == null)
			{
				return;
			}
			List<MyRadioButton> list = new List<MyRadioButton>();
			int num = 0;
			foreach (object child in ((Panel)(object)Parent).Children)
			{
				if (child is MyRadioButton myRadioButton)
				{
					list.Add(myRadioButton);
					if (myRadioButton.Checked)
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
					foreach (MyRadioButton item in list)
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
					foreach (MyRadioButton item2 in list)
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
				RefreshColor(null, anime);
				if (Checked)
				{
					Check?.Invoke(this, raiseByMouse);
				}
				ModMain.RaiseCustomEvent((DependencyObject)(object)this);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "单选按钮勾选改变错误", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.Error.OperationFailed"));
		}
	}

	private void Radiobox_MouseUp()
	{
		if (!Checked && isMouseDown)
		{
			ModBase.Log("[Control] 按下单选按钮：" + Text);
			isMouseDown = false;
			ModBase.RouteEventArgs e = new ModBase.RouteEventArgs(raiseByMouse: true);
			PreviewClick?.Invoke(this, e);
			if (!e.handled)
			{
				SetChecked(value: true, raiseByMouse: true, anime: true);
			}
		}
	}

	private void Radiobox_MouseDown()
	{
		if (!Checked)
		{
			isMouseDown = true;
			RefreshColor();
		}
	}

	private void Radiobox_MouseLeave()
	{
		isMouseDown = false;
	}

	private void RefreshColor(object obj = null, object e = null)
	{
		try
		{
			if (IsLoaded && ModAnimation.AniControlEnabled == 0 && !false.Equals(e))
			{
				switch (ColorType)
				{
				case ColorState.White:
					if (Checked)
					{
						ModBase.MyColor myColor = new ModBase.MyColor(ThemeManager.AppResources["ColorObject3"]);
						ModAnimation.AniStart(new ModAnimation.AniData[2]
						{
							ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, myColor - ShapeLogo.Fill, 120),
							ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, myColor - LabText.Foreground, 120)
						}, "MyRadioButton Checked " + Uuid);
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - Background, 120), "MyRadioButton Color " + Uuid);
					}
					else if (isMouseDown)
					{
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, new ModBase.MyColor(120.0, new ModBase.MyColor(ThemeManager.AppResources["ColorObject8"])) - Background, 60), "MyRadioButton Color " + Uuid);
					}
					else if (IsMouseOver)
					{
						ModAnimation.AniStart(new ModAnimation.AniData[2]
						{
							ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - ShapeLogo.Fill, 90),
							ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - LabText.Foreground, 90)
						}, "MyRadioButton Checked " + Uuid);
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, new ModBase.MyColor(50.0, new ModBase.MyColor(ThemeManager.AppResources["ColorObject8"])) - Background, 90), "MyRadioButton Color " + Uuid);
					}
					else
					{
						ModAnimation.AniStart(new ModAnimation.AniData[2]
						{
							ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - ShapeLogo.Fill, 150),
							ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - LabText.Foreground, 150)
						}, "MyRadioButton Checked " + Uuid);
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, new ModBase.MyColor(ThemeManager.AppResources["ColorBrushSemiTransparent"]) - Background, 150), "MyRadioButton Color " + Uuid);
					}
					break;
				case ColorState.Highlight:
					if (Checked)
					{
						ModAnimation.AniStart(new ModAnimation.AniData[2]
						{
							ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - ShapeLogo.Fill, 120),
							ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, new ModBase.MyColor(255.0, 255.0, 255.0) - LabText.Foreground, 120)
						}, "MyRadioButton Checked " + Uuid);
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, "ColorBrush3", 120), "MyRadioButton Color " + Uuid);
					}
					else if (isMouseDown)
					{
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, "ColorBrush6", 90), "MyRadioButton Color " + Uuid);
					}
					else if (IsMouseOver)
					{
						ModAnimation.AniStart(new ModAnimation.AniData[2]
						{
							ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, "ColorBrush3", 90),
							ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", 90)
						}, "MyRadioButton Checked " + Uuid);
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, "ColorBrush7", 90), "MyRadioButton Color " + Uuid);
					}
					else
					{
						ModAnimation.AniStart(new ModAnimation.AniData[2]
						{
							ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, "ColorBrush3", 150),
							ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", 150)
						}, "MyRadioButton Checked " + Uuid);
						ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, new ModBase.MyColor(ThemeManager.AppResources["ColorBrushSemiTransparent"]) - Background, 150), "MyRadioButton Color " + Uuid);
					}
					break;
				}
				return;
			}
			ModAnimation.AniStop("MyRadioButton Checked " + Uuid);
			ModAnimation.AniStop("MyRadioButton Color " + Uuid);
			switch (ColorType)
			{
			case ColorState.White:
				if (Checked)
				{
					Background = new ModBase.MyColor(255.0, 255.0, 255.0);
					ShapeLogo.SetResourceReference(Shape.FillProperty, "ColorBrush3");
					LabText.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3");
				}
				else
				{
					Background = (Brush)ThemeManager.AppResources["ColorBrushSemiTransparent"];
					ShapeLogo.Fill = new ModBase.MyColor(255.0, 255.0, 255.0);
					LabText.Foreground = new ModBase.MyColor(255.0, 255.0, 255.0);
				}
				break;
			case ColorState.Highlight:
				if (Checked)
				{
					SetResourceReference(Border.BackgroundProperty, "ColorBrush3");
					ShapeLogo.Fill = new ModBase.MyColor(255.0, 255.0, 255.0);
					LabText.Foreground = new ModBase.MyColor(255.0, 255.0, 255.0);
				}
				else
				{
					Background = (Brush)ThemeManager.AppResources["ColorBrushSemiTransparent"];
					ShapeLogo.SetResourceReference(Shape.FillProperty, "ColorBrush3");
					LabText.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3");
				}
				break;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新单选按钮颜色出错");
		}
	}

	public void RefreshMyRadioButtonColor()
	{
		RefreshColor();
	}

	static MyRadioButton()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
	}
}
