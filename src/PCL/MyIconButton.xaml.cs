using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

public partial class MyIconButton : Border, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, EventArgs e);

	public enum Themes
	{
		Color,
		White,
		Black,
		Red,
		Custom
	}

	private const int animationColorIn = 120;

	private const int animationColorOut = 150;

	private bool isMouseDown;

	public int Uuid = ModBase.GetUuid();


	public string Logo
	{
		get
		{
			return ((object)Path.Data)?.ToString() ?? string.Empty;
		}
		set
		{
			if (Path != null)
			{
				Path.Data = (Geometry)new GeometryConverter().ConvertFromString(value);
				SvgIconControlHelper.ApplyVisibility(Path, ShapeSvgIcon, IsUsingSvgIcon);
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
				if (Path != null && ShapeSvgIcon != null)
				{
					SvgIconControlHelper.ApplyIcon(Path, ShapeSvgIcon, field);
					ApplyLogoScale();
					RefreshAnim();
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

	public double LogoScale
	{
		get;
		set
		{
			field = value;
			ApplyLogoScale();
		}
	} = 1.0;

	public Themes Theme { get; set; }

	public SolidColorBrush Foreground
	{
		get;
		set
		{
			field = value;
			ModAnimation.AniControlEnabled++;
			RefreshAnim();
			ModAnimation.AniControlEnabled--;
		}
	} = new SolidColorBrush(Color.FromRgb(128, 128, 128));

	private string ColorAnimationKey => "MyIconButton Color " + Uuid;

	public event ClickEventHandler? Click;

	protected override Size MeasureOverride(Size constraint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		Size result = base.MeasureOverride(constraint);
		if (double.IsNaN(Width) && !double.IsNaN(Height) && Height > 0.0 && !double.IsInfinity(Height))
		{
			return new Size(Height, Height);
		}
		return result;
	}

	public MyIconButton()
	{
		InitializeComponent();
		MouseLeftButtonUp += Button_MouseUp;
		MouseLeftButtonDown += Button_MouseDown;
		MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			Button_MouseUp();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			Button_MouseLeave();
		};
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			RefreshAnim();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			RefreshAnim();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshAnim();
		};
	}

	private void ApplyLogoScale()
	{
		Grid iconHost = IconHost;
		if (iconHost != null)
		{
			iconHost.RenderTransform = new ScaleTransform
			{
				ScaleX = EffectiveLogoScale,
				ScaleY = EffectiveLogoScale
			};
		}
	}

	private static ModBase.MyColor GetTransparentBackground()
	{
		return new ModBase.MyColor(0.0, 255.0, 255.0, 255.0);
	}

	private ModBase.MyColor? GetBaseFillColor()
	{
		return Theme switch
		{
			Themes.Red => new ModBase.MyColor(160.0, 255.0, 76.0, 76.0), 
			Themes.Black => ThemeManager.IsDarkMode ? new ModBase.MyColor(160.0, 255.0, 255.0, 255.0) : new ModBase.MyColor(160.0, 0.0, 0.0, 0.0), 
			Themes.Custom => new ModBase.MyColor(160.0, Foreground), 
			_ => null, 
		};
	}

	private void EnsureBaseBrushes()
	{
		Border panBack = PanBack;
		if (panBack.Background == null)
		{
			Brush brush = (panBack.Background = GetTransparentBackground());
		}
		ModBase.MyColor baseFillColor = GetBaseFillColor();
		if ((object)baseFillColor != null && !IsUsingSvgIcon)
		{
			Path path = Path;
			if (path.Fill == null)
			{
				Brush brush = (path.Fill = baseFillColor);
			}
		}
	}

	private void AnimateActiveSvgIconBrush(string resourceKey, int duration)
	{
		if (IsUsingSvgIcon)
		{
			SvgIconControlHelper.AnimateSvgIconBrushTo(ShapeSvgIcon, resourceKey, duration, ColorAnimationKey);
		}
	}

	private void AnimateActiveSvgIconBrush(ModBase.MyColor color, int duration)
	{
		if (IsUsingSvgIcon)
		{
			SvgIconControlHelper.AnimateSvgIconBrushTo(ShapeSvgIcon, color, duration, ColorAnimationKey);
		}
	}

	private void SetActiveIconResource(string resourceKey)
	{
		SvgIconControlHelper.SetIconResource(Path, ShapeSvgIcon, IsUsingSvgIcon, resourceKey);
	}

	private void SetActiveIconBrush(Brush brush)
	{
		SvgIconControlHelper.SetIconBrush(Path, ShapeSvgIcon, IsUsingSvgIcon, brush);
	}

	private List<ModAnimation.AniData> GetHoverAnimations()
	{
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		switch (Theme)
		{
		case Themes.Color:
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush("ColorBrush2", 120);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, "ColorBrush2", 120));
			}
			break;
		case Themes.White:
			list.Add(ModAnimation.AaColor(PanBack, Border.BackgroundProperty, new ModBase.MyColor(50.0, 255.0, 255.0, 255.0) - PanBack.Background, 120));
			break;
		case Themes.Red:
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(new ModBase.MyColor(255.0, 76.0, 76.0), 120);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, new ModBase.MyColor(255.0, 76.0, 76.0) - Path.Fill, 120));
			}
			break;
		case Themes.Black:
		{
			ModBase.MyColor myColor2 = (ThemeManager.IsDarkMode ? new ModBase.MyColor(230.0, 255.0, 255.0, 255.0) : new ModBase.MyColor(230.0, 0.0, 0.0, 0.0));
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(myColor2, 120);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, myColor2 - Path.Fill, 120));
			}
			break;
		}
		case Themes.Custom:
		{
			ModBase.MyColor myColor = new ModBase.MyColor(255.0, Foreground);
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(myColor, 120);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, myColor - Path.Fill, 120));
			}
			break;
		}
		}
		return list;
	}

	private List<ModAnimation.AniData> GetNormalAnimations()
	{
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		switch (Theme)
		{
		case Themes.Color:
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush("ColorBrush4", 150);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, "ColorBrush4", 150));
			}
			PanBack.Background = GetTransparentBackground();
			break;
		case Themes.White:
		{
			ModBase.MyColor myColor3 = new ModBase.MyColor(234.0, 242.0, 254.0);
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(myColor3, 150);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, myColor3, 150));
			}
			list.Add(ModAnimation.AaColor(PanBack, Border.BackgroundProperty, GetTransparentBackground() - PanBack.Background, 150));
			break;
		}
		case Themes.Red:
		{
			ModBase.MyColor myColor2 = new ModBase.MyColor(160.0, 255.0, 76.0, 76.0);
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(myColor2, 150);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, myColor2 - Path.Fill, 150));
			}
			PanBack.Background = GetTransparentBackground();
			break;
		}
		case Themes.Black:
		{
			ModBase.MyColor myColor4 = (ThemeManager.IsDarkMode ? new ModBase.MyColor(160.0, 255.0, 255.0, 255.0) : new ModBase.MyColor(160.0, 0.0, 0.0, 0.0));
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(myColor4, 150);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, myColor4 - Path.Fill, 150));
			}
			PanBack.Background = GetTransparentBackground();
			break;
		}
		case Themes.Custom:
		{
			ModBase.MyColor myColor = new ModBase.MyColor(160.0, Foreground);
			if (IsUsingSvgIcon)
			{
				AnimateActiveSvgIconBrush(myColor, 150);
			}
			else
			{
				list.Add(ModAnimation.AaColor(Path, Shape.FillProperty, myColor - Path.Fill, 150));
			}
			PanBack.Background = GetTransparentBackground();
			break;
		}
		}
		return list;
	}

	private void ApplyNonAnimatedTheme()
	{
		switch (Theme)
		{
		case Themes.Color:
			SetActiveIconResource("ColorBrush4");
			break;
		case Themes.White:
			SetActiveIconBrush(new ModBase.MyColor(234.0, 242.0, 254.0));
			break;
		case Themes.Red:
			SetActiveIconBrush(new ModBase.MyColor(160.0, 255.0, 76.0, 76.0));
			break;
		case Themes.Black:
			SetActiveIconBrush(ThemeManager.IsDarkMode ? new ModBase.MyColor(160.0, 255.0, 255.0, 255.0) : new ModBase.MyColor(160.0, 0.0, 0.0, 0.0));
			break;
		case Themes.Custom:
			SetActiveIconBrush(new ModBase.MyColor(160.0, Foreground));
			break;
		}
		PanBack.Background = GetTransparentBackground();
	}

	private void Button_MouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isMouseDown)
		{
			ModBase.Log("[Control] 按下图标按钮" + (string.IsNullOrEmpty(Name) ? "" : ("：" + Name)));
			Click?.Invoke(sender, e);
			e.Handled = true;
			Button_MouseUp();
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
	}

	private void Button_MouseDown(object sender, MouseButtonEventArgs e)
	{
		isMouseDown = true;
		Focus();
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(PanBack, 0.8 - ((ScaleTransform)PanBack.RenderTransform).ScaleX, 400, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)), "MyIconButton Scale " + Uuid);
	}

	private void Button_MouseUp()
	{
		if (isMouseDown)
		{
			isMouseDown = false;
			ModAnimation.AniStart(new ModAnimation.AniData[2]
			{
				ModAnimation.AaScaleTransform(PanBack, 1.05 - ((ScaleTransform)PanBack.RenderTransform).ScaleX, 250, 0, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaScaleTransform(PanBack, -0.05, 250, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong))
			}, "MyIconButton Scale " + Uuid);
		}
		RefreshAnim();
	}

	private void Button_MouseLeave()
	{
		isMouseDown = false;
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScaleTransform(PanBack, 1.0 - ((ScaleTransform)PanBack.RenderTransform).ScaleX, 250, 0, new ModAnimation.AniEaseOutFluent()) }, "MyIconButton Scale " + Uuid);
		RefreshAnim();
	}

	public void RefreshAnim()
	{
		try
		{
			if (ControlVisualHelpers.ShouldAnimate(this))
			{
				EnsureBaseBrushes();
				ModAnimation.AniStart(IsMouseOver ? GetHoverAnimations() : GetNormalAnimations(), ColorAnimationKey);
			}
			else
			{
				ModAnimation.AniStop(ColorAnimationKey);
				ApplyNonAnimatedTheme();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新图标按钮动画状态出错");
		}
	}
}
