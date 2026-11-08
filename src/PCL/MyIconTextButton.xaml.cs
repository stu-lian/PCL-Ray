using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyIconTextButton : Border, IComponentConnector
{
	public delegate void ChangeEventHandler(object sender, bool raiseByMouse);

	public delegate void CheckEventHandler(object sender, bool raiseByMouse);

	public delegate void ClickEventHandler(object sender, ModBase.RouteEventArgs e);

	public enum ColorState
	{
		Black,
		Highlight
	}

	private const int animationTimeOfMouseIn = 100;

	private const int animationTimeOfMouseOut = 150;

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyIconTextButton), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender != null)
		{
			((MyIconTextButton)(object)sender).LabText.Text = (string)e.NewValue;
		}
	})));

	public static readonly DependencyProperty ColorTypeProperty = DependencyProperty.Register("ColorType", typeof(ColorState), typeof(MyIconTextButton), new PropertyMetadata((object)ColorState.Black));

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
		get
		{
			return (ColorState)((DependencyObject)this).GetValue(ColorTypeProperty);
		}
		set
		{
			if (ColorType != value)
			{
				((DependencyObject)this).SetValue(ColorTypeProperty, (object)value);
				RefreshColor();
			}
		}
	}

	private string CheckedAnimationKey => "MyIconTextButton Checked " + Uuid;

	private string ColorAnimationKey => "MyIconTextButton Color " + Uuid;

	public event CheckEventHandler? Check;

	public event ChangeEventHandler? Change;

	public event ClickEventHandler? Click;

	public MyIconTextButton()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected Obj, but got Unknown
		InitializeComponent();
		RefreshLogoHostVisibility();
		MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			MyIconTextButton_MouseUp();
		};
		MouseLeftButtonDown += (object _, MouseButtonEventArgs _) =>
		{
			MyIconTextButton_MouseDown();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			MyIconTextButton_MouseLeave();
		};
		MouseEnter += RefreshColor;
		Loaded += RefreshColor;
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
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
				LogoHost.Height = 16.0;
				LogoHost.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
				LabText.Margin = new Thickness(7.0, 0.0, 12.0, 1.0);
			}
			else
			{
				LogoHost.Visibility = Visibility.Collapsed;
				LogoHost.Width = 0.0;
				LogoHost.Height = 16.0;
				LogoHost.Margin = new Thickness(0.0);
				LabText.Margin = new Thickness(12.0, 0.0, 12.0, 1.0);
			}
		}
	}

	private string GetDefaultForegroundResourceKey()
	{
		if (ColorType != ColorState.Highlight)
		{
			return "ColorBrush1";
		}
		return "ColorBrush3";
	}

	private void StartForegroundAnimation(string resourceKey, int duration)
	{
		if (IsUsingSvgIcon)
		{
			SvgIconControlHelper.AnimateSvgIconBrushTo(ShapeSvgIcon, resourceKey, duration, CheckedAnimationKey);
			ModAnimation.AniStart(ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, resourceKey, duration), CheckedAnimationKey);
		}
		else
		{
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaColor(ShapeLogo, Shape.FillProperty, resourceKey, duration),
				ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, resourceKey, duration)
			}, CheckedAnimationKey);
		}
	}

	private void StartBackgroundAnimation(string resourceKey, int duration)
	{
		ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, resourceKey, duration), ColorAnimationKey);
	}

	private void StartBackgroundAnimation(ModBase.MyColor delta, int duration)
	{
		ModAnimation.AniStart(ModAnimation.AaColor(this, Border.BackgroundProperty, delta, duration), ColorAnimationKey);
	}

	private void MyIconTextButton_MouseUp()
	{
		if (isMouseDown)
		{
			ModBase.Log("[Control] 按下带图标按钮：" + Text);
			isMouseDown = false;
			Click?.Invoke(this, new ModBase.RouteEventArgs(raiseByMouse: true));
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
			RefreshColor();
		}
	}

	private void MyIconTextButton_MouseDown()
	{
		isMouseDown = true;
		RefreshColor();
	}

	private void MyIconTextButton_MouseLeave()
	{
		isMouseDown = false;
		RefreshColor();
	}

	private void RefreshColor(object? obj = null, object? e = null)
	{
		try
		{
			if (ControlVisualHelpers.ShouldAnimate(this, e))
			{
				if (isMouseDown)
				{
					StartBackgroundAnimation("ColorBrush6", 70);
				}
				else if (IsMouseOver)
				{
					StartForegroundAnimation("ColorBrush3", 100);
					StartBackgroundAnimation("ColorBrushBg1", 100);
				}
				else if (IsEnabled)
				{
					StartForegroundAnimation(GetDefaultForegroundResourceKey(), 150);
					StartBackgroundAnimation(ThemeManager.colorSemiTransparent - Background, 150);
				}
				else
				{
					StartForegroundAnimation("ColorBrushGray5", 100);
					StartBackgroundAnimation(ThemeManager.colorSemiTransparent - Background, 150);
				}
			}
			else
			{
				ModAnimation.AniStop(CheckedAnimationKey);
				ModAnimation.AniStop(ColorAnimationKey);
				Background = ThemeManager.colorSemiTransparent;
				string text = (IsEnabled ? GetDefaultForegroundResourceKey() : "ColorBrushGray5");
				SvgIconControlHelper.SetIconResource(ShapeLogo, ShapeSvgIcon, IsUsingSvgIcon, text);
				LabText.SetResourceReference(TextBlock.ForegroundProperty, text);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新带图标按钮颜色出错");
		}
	}

	static MyIconTextButton()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected Obj, but got Unknown
	}
}
