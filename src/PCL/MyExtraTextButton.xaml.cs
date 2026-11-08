using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyExtraTextButton : Grid, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	private const int animationColorIn = 120;

	private const int animationColorOut = 150;

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyExtraTextButton), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		MyExtraTextButton myExtraTextButton = (MyExtraTextButton)(object)sender;
		if (myExtraTextButton != null)
		{
			myExtraTextButton.LabText.Text = (string)e.NewValue;
		}
	})));

	private bool isLeftMouseHeld;

	public int Uuid = ModBase.GetUuid();


	public string Logo
	{
		get;
		set
		{
			if (!((value ?? "") == (field ?? "")))
			{
				field = value ?? string.Empty;
				Path.Data = (string.IsNullOrWhiteSpace(value) ? null : ((Geometry)new GeometryConverter().ConvertFromString(value)));
				SvgIconControlHelper.ApplyVisibility(Path, ShapeSvgIcon, IsUsingSvgIcon);
				RefreshIconHostVisibility();
			}
		}
	} = string.Empty;

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
					RefreshIconHostVisibility();
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
				return !string.IsNullOrWhiteSpace(Logo);
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
			if (value != null)
			{
				((DependencyObject)this).SetValue(TextProperty, (object)value);
			}
		}
	}

	public bool Show
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}
			field = value;
			ModBase.RunInUi(() =>
			{
				if (value)
				{
					Opacity = 0.0;
					ModAnimation.AniStart(new ModAnimation.AniData[3]
					{
						ModAnimation.AaOpacity(this, 1.0 - Opacity, 80, 50),
						ModAnimation.AaScaleTransform(this, 0.15 - ((ScaleTransform)RenderTransform).ScaleX, 400, 50, new ModAnimation.AniEaseOutBack()),
						ModAnimation.AaScaleTransform(this, 0.85, 160, 50, new ModAnimation.AniEaseOutFluent())
					}, "MyExtraTextButton MainScale " + Uuid);
				}
				else
				{
					ModAnimation.AniStart(new ModAnimation.AniData[2]
					{
						ModAnimation.AaOpacity(this, 0.0 - Opacity, 50, 50),
						ModAnimation.AaScaleTransform(this, 0.0 - ((ScaleTransform)RenderTransform).ScaleX, 100, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak))
					}, "MyExtraTextButton MainScale " + Uuid);
				}
				IsHitTestVisible = value;
			});
		}
	}

	public event ClickEventHandler? Click;

	public MyExtraTextButton()
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		InitializeComponent();
		RefreshIconHostVisibility();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshColor();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
		PanClick.MouseLeftButtonDown += Button_LeftMouseDown;
		PanClick.MouseLeftButtonUp += Button_LeftMouseUp;
		PanClick.MouseLeave += Button_MouseLeave;
		PanClick.MouseRightButtonUp += Button_RightMouseUp;
		PanClick.MouseEnter += (object sender, MouseEventArgs e) =>
		{
			RefreshColor();
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

	private void RefreshIconHostVisibility()
	{
		if (IconHost != null && LabText != null)
		{
			if (HasAnyIcon)
			{
				IconHost.Visibility = Visibility.Visible;
				IconHost.Width = 16.0;
				IconHost.Margin = new Thickness(2.0, 12.0, 0.0, 12.0);
				LabText.Margin = new Thickness(12.0, 0.0, 0.0, 0.8);
			}
			else
			{
				IconHost.Visibility = Visibility.Collapsed;
				IconHost.Width = 0.0;
				IconHost.Margin = new Thickness(0.0, 12.0, 0.0, 12.0);
				LabText.Margin = new Thickness(0.0, 0.0, 0.0, 0.8);
			}
		}
	}

	private void StartScaleAnimation(double targetScale, double reboundScale, int reboundDuration = 60)
	{
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaScaleTransform(PanScale, targetScale - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
			ModAnimation.AaScaleTransform(PanScale, reboundScale, reboundDuration, 0, new ModAnimation.AniEaseOutFluent())
		}, "MyExtraTextButton Scale " + Uuid);
	}

	private void RefreshScaleAfterRelease()
	{
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScaleTransform(PanScale, 1.0 - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 300, 0, new ModAnimation.AniEaseOutBack()) }, "MyExtraTextButton Scale " + Uuid);
	}

	private void Button_LeftMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isLeftMouseHeld)
		{
			ModBase.Log("[Control] 按下附加图标按钮：" + Text);
			Click?.Invoke(sender, e);
			e.Handled = true;
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
			Button_LeftMouseUp();
		}
	}

	private void Button_LeftMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!isLeftMouseHeld)
		{
			StartScaleAnimation(0.85, -0.05);
		}
		isLeftMouseHeld = true;
		Focus();
	}

	private void Button_LeftMouseUp()
	{
		RefreshScaleAfterRelease();
		isLeftMouseHeld = false;
		RefreshColor();
	}

	private void Button_RightMouseUp(object sender, MouseEventArgs e)
	{
		if (!isLeftMouseHeld)
		{
			RefreshScaleAfterRelease();
		}
		RefreshColor();
	}

	private void Button_MouseLeave(object sender, MouseEventArgs e)
	{
		isLeftMouseHeld = false;
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScaleTransform(PanScale, 1.0 - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 500, 0, new ModAnimation.AniEaseOutFluent()) }, "MyExtraTextButton Scale " + Uuid);
		RefreshColor();
	}

	public void RefreshColor()
	{
		try
		{
			if (ControlVisualHelpers.ShouldAnimate(this))
			{
				if (!IsEnabled)
				{
					ModAnimation.AniStart(ModAnimation.AaColor(PanColor, Panel.BackgroundProperty, "ColorBrushGray4", 120), "MyExtraTextButton Color " + Uuid);
				}
				else if (IsMouseOver)
				{
					ModAnimation.AniStart(ModAnimation.AaColor(PanColor, Panel.BackgroundProperty, "ColorBrush4", 120), "MyExtraTextButton Color " + Uuid);
				}
				else
				{
					ModAnimation.AniStart(ModAnimation.AaColor(PanColor, Panel.BackgroundProperty, "ColorBrush3", 150), "MyExtraTextButton Color " + Uuid);
				}
			}
			else
			{
				Border panColor = PanColor;
				DependencyProperty backgroundProperty = Panel.BackgroundProperty;
				string resourceKey;
				if (IsEnabled)
				{
					resourceKey = (IsMouseOver ? "ColorBrush4" : "ColorBrush3");
				}
				else
				{
					resourceKey = "ColorBrushGray4";
				}
				ControlVisualHelpers.AnimateColorOrSetResource(panColor, backgroundProperty, resourceKey, (!IsEnabled || IsMouseOver) ? 120 : 150, "MyExtraTextButton Color " + Uuid, shouldAnimate: false);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新附加图标按钮颜色出错");
		}
	}

	static MyExtraTextButton()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
	}
}
