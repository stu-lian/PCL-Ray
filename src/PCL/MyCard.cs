using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.UI.Controls;

namespace PCL;

public class MyCard : AnimatedBackgroundGrid
{
	public delegate void PreviewSwapEventHandler(object sender, ModBase.RouteEventArgs e);

	public delegate void SwapEventHandler(object sender, ModBase.RouteEventArgs e);

	private const double dropShadowIdleOpacity = 0.07;

	private const double dropShadowHoverOpacity = 0.4;

	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(MyCard), new PropertyMetadata((object)""));

	private readonly BlurBorder mainBorder;

	private readonly Grid mainGrid;

	private TextBlock _MainTextBlock;

	private bool isLoad;

	private bool isHeightAnimating;

	private double actualUsedHeight;

	public UIElement SwapControl;

	private bool isSwapMouseDown;

	private bool isCustomMouseDown;

	public const int SwapedHeight = 40;

	public MyDropShadow MainChrome { get; }

	public UIElement BorderChild
	{
		get
		{
			return ((Decorator)(object)mainBorder).Child;
		}
		set
		{
			((Decorator)(object)mainBorder).Child = value;
		}
	}

	public TextBlock MainTextBlock
	{
		get
		{
			Init();
			return _MainTextBlock;
		}
		set
		{
			_MainTextBlock = value;
		}
	}

	public Path MainSwap
	{
		get
		{
			Init();
			return field;
		}
		set
		{
			field = value;
		}
	}

	public InlineCollection Inlines => MainTextBlock.Inlines;

	public CornerRadius CornerRadius
	{
		get
		{
			return MainChrome.CornerRadius;
		}
		set
		{
			MainChrome.CornerRadius = value;
			((Border)(object)mainBorder).CornerRadius = value;
		}
	}

	public string Title
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TitleProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TitleProperty, (object)value);
			if (_MainTextBlock != null)
			{
				MainTextBlock.Text = value;
			}
		}
	}

	protected override SolidColorBrush AnimatableBrush
	{
		get
		{
			return (SolidColorBrush)((Border)(object)mainBorder).Background;
		}
		set
		{
			((Border)(object)mainBorder).Background = value;
		}
	}

	protected override FrameworkElement AnimatableElement => (FrameworkElement)(object)mainBorder;

	public bool HasMouseAnimation { get; set; } = true;

	public bool UseAnimation { get; set; } = true;

	public bool CanSwap { get; set; }

	public Action<StackPanel> InstallMethod { get; set; }

	public bool IsSwapped
	{
		get
		{
			return field;
		}
		set
		{
			if (field == value)
			{
				return;
			}
			field = value;
			if (SwapControl != null)
			{
				if (!IsSwapped && SwapControl is StackPanel)
				{
					StackPanel stack = (StackPanel)SwapControl;
					StackInstall(ref stack, InstallMethod);
					SwapControl = stack;
				}
				if (IsLoaded)
				{
					SwapControl.Visibility = Visibility.Visible;
					TriggerForceResize();
					ModAnimation.AniStart(ModAnimation.AaRotateTransform(MainSwap, (double)((!field) ? 180 : (SwapLogoRight ? 270 : 0)) - ((RotateTransform)MainSwap.RenderTransform).Angle, 250, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)), "MyCard Swap " + uuid, refreshTime: true);
				}
			}
		}
	}

	[Obsolete("请使用 IsSwapped 属性，IsSwaped 存在拼写错误")]
	public bool IsSwaped
	{
		get
		{
			return IsSwapped;
		}
		set
		{
			IsSwapped = value;
		}
	}

	public bool SwapLogoRight { get; set; }

	public event PreviewSwapEventHandler? PreviewSwap;

	public event SwapEventHandler? Swap;

	public MyCard()
		: base(Border.BackgroundProperty)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected Obj, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected Obj, but got Unknown
		//IL_00e1: Expected Obj, but got Unknown
		MainChrome = new MyDropShadow
		{
			Margin = new Thickness(-3.0, -3.0, -3.0, -3.0 - ModBase.GetWPFSize(1.0)),
			ShadowRadius = 3.0,
			Opacity = 0.07,
			CornerRadius = new CornerRadius(5.0)
		};
		MainChrome.SetResourceReference(MyDropShadow.ColorProperty, "ColorObject1");
		Children.Insert(0, MainChrome);
		BlurBorder val = new BlurBorder();
		((Border)val).CornerRadius = new CornerRadius(5.0);
		((UIElement)val).IsHitTestVisible = false;
		mainBorder = val;
		Children.Insert(1, (UIElement)(object)mainBorder);
		mainGrid = new Grid();
		Children.Add(mainGrid);
		SetResourceReference(AnimatedBackgroundGrid.BackgroundBrushProperty, "ColorBrushTransparentBackground");
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
		MouseEnter += MyCard_MouseEnter;
		MouseLeave += MyCard_MouseLeave;
		SizeChanged += MySizeChanged;
		MouseLeftButtonDown += MyCard_MouseLeftButtonDown;
		MouseLeftButtonUp += MyCard_MouseLeftButtonUp;
		MouseLeave += MyCard_MouseLeave_Swap;
	}

	private void Init()
	{
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		if (isLoad)
		{
			return;
		}
		isLoad = true;
		if (MainTextBlock == null)
		{
			MainTextBlock = new TextBlock
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(15.0, 12.0, 0.0, 0.0),
				FontWeight = FontWeights.Bold,
				FontSize = 13.0,
				IsHitTestVisible = false
			};
			MainTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1");
			MainTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Title")
			{
				Source = this,
				Mode = BindingMode.OneWay
			});
			mainGrid.Children.Add(MainTextBlock);
		}
		if (CanSwap || SwapControl != null)
		{
			if (SwapControl == null && Children.Count > 3)
			{
				SwapControl = Children[3];
			}
			MainSwap = new Path
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				Stretch = Stretch.Uniform,
				Height = 6.0,
				Width = 10.0,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 17.0, 16.0, 0.0),
				Data = (Geometry)new GeometryConverter().ConvertFromString("M2,4 l-2,2 10,10 10,-10 -2,-2 -8,8 -8,-8 z"),
				RenderTransform = new RotateTransform(180.0),
				RenderTransformOrigin = new Point(0.5, 0.5)
			};
			MainSwap.SetResourceReference(Shape.FillProperty, "ColorBrush1");
			mainGrid.Children.Add(MainSwap);
		}
		if (IsSwapped && SwapControl != null)
		{
			MainSwap.RenderTransform = new RotateTransform(SwapLogoRight ? 270 : 0);
			SwapControl.Visibility = Visibility.Collapsed;
			bool rawUseAnimation = UseAnimation;
			UseAnimation = false;
			Height = 40.0;
			ModAnimation.AniStop("MyCard Height " + uuid);
			isHeightAnimating = false;
			ModBase.RunInUi(() =>
			{
				UseAnimation = rawUseAnimation;
			}, forceWaitUntilLoaded: true);
		}
	}

	public void StackInstall()
	{
		StackPanel stack = (StackPanel)SwapControl;
		StackInstall(ref stack, InstallMethod);
		SwapControl = stack;
		TriggerForceResize();
	}

	public static void StackInstall(ref StackPanel stack, Action<StackPanel> installMethod)
	{
		if (stack.Tag != null)
		{
			try
			{
				installMethod(stack);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[MyCard] InstallMethod 调用失败");
			}
			stack.Children.Add(new FrameworkElement
			{
				Height = 18.0
			});
			stack.Tag = null;
		}
	}

	private void MyCard_MouseEnter(object sender, MouseEventArgs e)
	{
		if (HasMouseAnimation)
		{
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
			if (MainTextBlock != null)
			{
				list.Add(ModAnimation.AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush2", 90));
			}
			if (MainSwap != null)
			{
				list.Add(ModAnimation.AaColor(MainSwap, Shape.FillProperty, "ColorBrush2", 90));
			}
			list.AddRange(new ModAnimation.AniData[2]
			{
				ModAnimation.AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject4", 90),
				ModAnimation.AaOpacity(MainChrome, 0.4 - MainChrome.Opacity, 90)
			});
			if (!IsAnimating)
			{
				ModAnimation.AniStart(list, "MyCard Mouse " + uuid);
			}
		}
	}

	private void MyCard_MouseLeave(object sender, MouseEventArgs e)
	{
		if (HasMouseAnimation)
		{
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
			if (MainTextBlock != null)
			{
				list.Add(ModAnimation.AaColor(MainTextBlock, TextBlock.ForegroundProperty, "ColorBrush1", 90));
			}
			if (MainSwap != null)
			{
				list.Add(ModAnimation.AaColor(MainSwap, Shape.FillProperty, "ColorBrush1", 90));
			}
			list.AddRange(new ModAnimation.AniData[2]
			{
				ModAnimation.AaColor(MainChrome, MyDropShadow.ColorProperty, "ColorObject1", 90),
				ModAnimation.AaOpacity(MainChrome, 0.07 - MainChrome.Opacity, 90)
			});
			if (!IsAnimating)
			{
				ModAnimation.AniStart(list, "MyCard Mouse " + uuid);
			}
		}
	}

	private void MySizeChanged(object sender, SizeChangedEventArgs e)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (UseAnimation)
		{
			Size val;
			double num;
			if (!IsSwapped)
			{
				val = e.NewSize;
				num = val.Height;
			}
			else
			{
				num = 40.0;
			}
			val = e.PreviousSize;
			double num2 = num - val.Height;
			val = e.PreviousSize;
			if (val.Height != 0.0 && !isHeightAnimating && !(Math.Abs(num2) < 1.0) && ActualHeight != 0.0)
			{
				val = e.PreviousSize;
				StartHeightAnimation(num2, val.Height, isLoadAnimation: false);
			}
		}
	}

	private void StartHeightAnimation(double delta, double previousHeight, bool isLoadAnimation)
	{
		if (isHeightAnimating || ModMain.frmMain == null)
		{
			return;
		}
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		double num = Math.Abs(delta);
		if (num <= 800.0)
		{
			list.Add(ModAnimation.AaHeight(this, delta, 150, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)));
		}
		else
		{
			int num2 = 0;
			int num3;
			int num4;
			if (delta < 0.0 && num - (double)num2 > 500.0)
			{
				num2 = 200;
				num3 = 150;
				num4 = (int)Math.Round((num - (double)num2) / 0.1);
			}
			else if (delta > 0.0 && num - (double)num2 > 3000.0)
			{
				num4 = 5000;
				num2 = (int)Math.Round(num - (double)num4 * 0.3);
				num3 = 400;
			}
			else
			{
				num2 = 150;
				num3 = 200;
				num4 = 4000;
			}
			list.Add(ModAnimation.AaHeight(this, (num - (double)num2) * (double)Math.Sign(delta), (int)Math.Round((num - (double)num2) / (double)num4 * 1000.0)));
			list.Add(ModAnimation.AaHeight(this, num2 * Math.Sign(delta), num3, 0, new ModAnimation.AniEaseOutFluentWithInitial(num4, (double)num3 / 1000.0, num2), after: true));
		}
		list.Add(ModAnimation.AaCode(() =>
		{
			isHeightAnimating = false;
			Height = actualUsedHeight;
			if (IsSwapped && SwapControl != null)
			{
				SwapControl.Visibility = Visibility.Collapsed;
			}
		}, 0, after: true));
		ModAnimation.AniStart(list, "MyCard Height " + uuid);
		isHeightAnimating = true;
		actualUsedHeight = (IsSwapped ? 40.0 : Height);
		Height = previousHeight;
	}

	public void TriggerForceResize()
	{
		Height = (IsSwapped ? 40.0 : double.NaN);
		ModAnimation.AniStop("MyCard Height " + uuid);
		isHeightAnimating = false;
	}

	private void MyCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Point position = Mouse.GetPosition(this);
		double y = position.Y;
		if (IsSwapped || (!(y > (double)(IsSwapped ? 40 : 34)) && (y != 0.0 || IsMouseDirectlyOver)))
		{
			isCustomMouseDown = true;
			if (IsSwapped || (SwapControl != null && !(y > (double)(IsSwapped ? 40 : 34)) && (y != 0.0 || IsMouseDirectlyOver)))
			{
				isSwapMouseDown = true;
			}
		}
	}

	private void MyCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!isCustomMouseDown)
		{
			return;
		}
		isCustomMouseDown = false;
		ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		if (!isSwapMouseDown)
		{
			return;
		}
		isSwapMouseDown = false;
		Point position = Mouse.GetPosition(this);
		double y = position.Y;
		if (IsSwapped || (SwapControl != null && !(y > (double)(IsSwapped ? 40 : 34)) && (y != 0.0 || IsMouseDirectlyOver)))
		{
			ModBase.RouteEventArgs e2 = new ModBase.RouteEventArgs(raiseByMouse: true);
			PreviewSwap?.Invoke(this, e2);
			if (e2.handled)
			{
				isSwapMouseDown = false;
				return;
			}
			IsSwapped = !IsSwapped;
			ModBase.Log("[Control] " + (IsSwapped ? "折叠卡片" : "展开卡片") + ((Title == null) ? "" : ("：" + Title)));
			Swap?.Invoke(this, e2);
		}
	}

	private void MyCard_MouseLeave_Swap(object sender, MouseEventArgs e)
	{
		isSwapMouseDown = false;
	}

	static MyCard()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected Obj, but got Unknown
	}
}
