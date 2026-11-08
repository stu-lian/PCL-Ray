using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

public partial class MyExtraButton : Grid, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	public delegate void RightClickEventHandler(object sender, MouseButtonEventArgs e);

	public delegate bool ShowCheckDelegate();

	private const int animationColorIn = 120;

	private const int animationColorOut = 150;

	private bool isLeftMouseHeld;

	private bool isRightMouseHeld;

	public ShowCheckDelegate showCheck;

	public int Uuid = ModBase.GetUuid();


	public double Progress
	{
		get;
		set
		{
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			if (field != value)
			{
				field = value;
				if (value < 0.0001)
				{
					PanProgress.Visibility = Visibility.Collapsed;
					return;
				}
				PanProgress.Visibility = Visibility.Visible;
				RectProgress.Rect = new Rect(0.0, 40.0 * (1.0 - value), 40.0, 40.0 * value);
			}
		}
	}

	public string Logo
	{
		get;
		set
		{
			if (!((value ?? "") == (field ?? "")))
			{
				field = value;
				Path.Data = (Geometry)new GeometryConverter().ConvertFromString(value);
				SvgIconControlHelper.ApplyVisibility(Path, ShapeSvgIcon, IsUsingSvgIcon);
			}
		}
	} = "";

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
					Visibility = Visibility.Visible;
					ModAnimation.AniStart(new ModAnimation.AniData[3]
					{
						ModAnimation.AaScaleTransform(this, 0.3 - ((ScaleTransform)RenderTransform).ScaleX, 500, 60, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
						ModAnimation.AaScaleTransform(this, 0.7, 500, 60, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
						ModAnimation.AaHeight(this, 50.0 - Height, 200, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
					}, "MyExtraButton MainScale " + Uuid);
				}
				else
				{
					ModAnimation.AniStart(new ModAnimation.AniData[3]
					{
						ModAnimation.AaScaleTransform(this, 0.0 - ((ScaleTransform)RenderTransform).ScaleX, 100, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
						ModAnimation.AaHeight(this, 0.0 - Height, 400, 100, new ModAnimation.AniEaseOutFluent()),
						ModAnimation.AaCode(() =>
						{
							Visibility = Visibility.Collapsed;
						}, 0, after: true)
					}, "MyExtraButton MainScale " + Uuid);
				}
				IsHitTestVisible = value;
			});
		}
	}

	public bool CanRightClick { get; set; }

	public event ClickEventHandler? Click;

	public event RightClickEventHandler? RightClick;

	public MyExtraButton()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected Obj, but got Unknown
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshColor();
		};
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
		InitializeComponent();
		PanClick.MouseLeave += (object _, MouseEventArgs _) =>
		{
			Button_MouseLeave();
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

	private void StartScaleAnimation(double targetScale, double reboundScale, int reboundDuration = 60)
	{
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaScaleTransform(PanScale, targetScale - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
			ModAnimation.AaScaleTransform(PanScale, reboundScale, reboundDuration, 0, new ModAnimation.AniEaseOutFluent())
		}, "MyExtraButton Scale " + Uuid);
	}

	private void RefreshScaleAfterRelease()
	{
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScaleTransform(PanScale, 1.0 - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 300, 0, new ModAnimation.AniEaseOutBack()) }, "MyExtraButton Scale " + Uuid);
	}

	public void ShowRefresh()
	{
		if (showCheck != null)
		{
			Show = showCheck();
		}
	}

	private void Button_LeftMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isLeftMouseHeld)
		{
			object toolTip = ToolTip;
			bool flag = ((toolTip == null || (toolTip is string text && text == "")) ? true : false);
			ModBase.Log("[Control] 按下附加按钮" + (flag ? "" : ("：" + ToolTip)));
			Click?.Invoke(sender, e);
			e.Handled = true;
			Button_LeftMouseUp();
		}
	}

	private void Button_RightMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isRightMouseHeld)
		{
			object toolTip = ToolTip;
			bool flag = ((toolTip == null || (toolTip is string text && text == "")) ? true : false);
			ModBase.Log("[Control] 右键按下附加按钮" + (flag ? "" : ("：" + ToolTip)));
			RightClick?.Invoke(sender, e);
			e.Handled = true;
			Button_RightMouseUp();
		}
	}

	private void Button_LeftMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!isLeftMouseHeld && !isRightMouseHeld)
		{
			StartScaleAnimation(0.85, -0.05);
		}
		isLeftMouseHeld = true;
		Focus();
	}

	private void Button_RightMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (CanRightClick)
		{
			if (!isLeftMouseHeld && !isRightMouseHeld)
			{
				StartScaleAnimation(0.85, -0.05);
			}
			isRightMouseHeld = true;
			Focus();
		}
	}

	private void Button_LeftMouseUp()
	{
		if (!isRightMouseHeld)
		{
			RefreshScaleAfterRelease();
		}
		if (isLeftMouseHeld)
		{
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
		isLeftMouseHeld = false;
		RefreshColor();
	}

	private void Button_RightMouseUp()
	{
		if (CanRightClick)
		{
			if (!isLeftMouseHeld)
			{
				RefreshScaleAfterRelease();
			}
			isRightMouseHeld = false;
			RefreshColor();
		}
	}

	private void Button_MouseLeave()
	{
		isLeftMouseHeld = false;
		isRightMouseHeld = false;
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScaleTransform(PanScale, 1.0 - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 500, 0, new ModAnimation.AniEaseOutFluent()) }, "MyExtraButton Scale " + Uuid);
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
					ModAnimation.AniStart(ModAnimation.AaColor(PanColor, Panel.BackgroundProperty, "ColorBrushGray4", 120), "MyExtraButton Color " + Uuid);
				}
				else if (IsMouseOver)
				{
					ModAnimation.AniStart(ModAnimation.AaColor(PanColor, Panel.BackgroundProperty, "ColorBrush4", 120), "MyExtraButton Color " + Uuid);
				}
				else
				{
					ModAnimation.AniStart(ModAnimation.AaColor(PanColor, Panel.BackgroundProperty, "ColorBrush3", 150), "MyExtraButton Color " + Uuid);
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
				ControlVisualHelpers.AnimateColorOrSetResource(panColor, backgroundProperty, resourceKey, (!IsEnabled || IsMouseOver) ? 120 : 150, "MyExtraButton Color " + Uuid, shouldAnimate: false);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新图标按钮颜色出错");
		}
	}

	public void Ribble()
	{
		ModBase.RunInUi(() =>
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			Border shape = new Border
			{
				CornerRadius = new CornerRadius(1000.0),
				BorderThickness = new Thickness(0.001),
				Opacity = 0.5,
				RenderTransformOrigin = new Point(0.5, 0.5),
				RenderTransform = new ScaleTransform()
			};
			shape.SetResourceReference(Border.BackgroundProperty, "ColorBrush5");
			PanScale.Children.Insert(0, shape);
			ModAnimation.AniStart(new ModAnimation.AniData[3]
			{
				ModAnimation.AaScaleTransform(shape, 13.0, 1000, 0, new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Strong, 0.3)),
				ModAnimation.AaOpacity(shape, 0.0 - shape.Opacity, 1000),
				ModAnimation.AaCode(() =>
				{
					PanScale.Children.Remove(shape);
				}, 0, after: true)
			}, "ExtraButton Ribble " + ModBase.GetUuid());
		});
	}

	private void PanClick_MouseEvent(object sender, MouseEventArgs e)
	{
		RefreshColor();
	}
}
