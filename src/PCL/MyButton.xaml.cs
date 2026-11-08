using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL;

[ContentProperty("Inlines")]
public partial class MyButton : Border, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	public enum ColorState
	{
		Normal,
		Highlight,
		Red
	}

	private const int animationColorIn = 100;

	private const int animationColorOut = 200;

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MyButton), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender != null)
		{
			((MyButton)(object)sender).LabText.Text = (string)e.NewValue;
		}
	})));

	public new static readonly DependencyProperty PaddingProperty = DependencyProperty.Register("Padding", typeof(Thickness), typeof(MyButton), new PropertyMetadata((PropertyChangedCallback)((DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		if (sender != null)
		{
			((MyButton)(object)sender).PanFore.Padding = (Thickness)e.NewValue;
		}
	})));

	private ColorState _ColorType;

	public int Uuid = ModBase.GetUuid();

	private bool isMouseDown;

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

	public Thickness TextPadding
	{
		get
		{
			return LabText.Padding;
		}
		set
		{
			LabText.Padding = value;
		}
	}

	public ColorState ColorType
	{
		get
		{
			return _ColorType;
		}
		set
		{
			_ColorType = value;
			RefreshColor();
		}
	}

	public new Thickness Padding
	{
		get
		{
			return PanFore.Padding;
		}
		set
		{
			PanFore.Padding = value;
		}
	}

	public Transform RealRenderTransform
	{
		get
		{
			return PanFore.RenderTransform;
		}
		set
		{
			PanFore.RenderTransform = value;
		}
	}

	public event ClickEventHandler? Click;

	public MyButton()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected Obj, but got Unknown
		InitializeComponent();
		MouseEnter += RefreshColor;
		MouseLeave += RefreshColor;
		Loaded += RefreshColor;
		IsEnabledChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			RefreshColor();
		};
		MouseLeftButtonUp += Button_MouseUp;
		MouseLeftButtonDown += Button_MouseDown;
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			Button_MouseEnter();
		};
		MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			Button_MouseUp();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			Button_MouseLeave();
		};
	}

	private string GetBorderBrushResourceKey()
	{
		return ColorType switch
		{
			ColorState.Normal => IsMouseOver ? "ColorBrush3" : "ColorBrush1", 
			ColorState.Highlight => IsMouseOver ? "ColorBrush3" : "ColorBrush2", 
			ColorState.Red => IsMouseOver ? "ColorBrushRedLight" : "ColorBrushRedDark", 
			_ => "ColorBrush1", 
		};
	}

	private void StartBorderBrushAnimation(string resourceKey, int duration)
	{
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaColor(PanFore, Border.BorderBrushProperty, resourceKey, duration) }, "MyButton Color " + Uuid);
	}

	private void RefreshColor(object obj = null, object e = null)
	{
		try
		{
			if (ControlVisualHelpers.ShouldAnimate(this))
			{
				if (IsEnabled)
				{
					StartBorderBrushAnimation(GetBorderBrushResourceKey(), IsMouseOver ? 100 : 200);
					return;
				}
				ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaColor(PanFore, Border.BorderBrushProperty, ThemeManager.colorGray4 - PanFore.BorderBrush, 200) }, "MyButton Color " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("MyButton Color " + Uuid);
				if (IsEnabled)
				{
					PanFore.SetResourceReference(Border.BorderBrushProperty, GetBorderBrushResourceKey());
				}
				else
				{
					PanFore.BorderBrush = ThemeManager.colorGray4;
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新按钮颜色出错");
		}
	}

	private void Button_MouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isMouseDown)
		{
			ModBase.Log("[Control] 按下按钮：" + Text);
			Click?.Invoke(sender, e);
			ModMain.RaiseCustomEvent((DependencyObject)(object)this);
		}
	}

	private void Button_MouseDown(object sender, MouseButtonEventArgs e)
	{
		isMouseDown = true;
		Focus();
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaScaleTransform(PanFore, 0.955 - ((ScaleTransform)PanFore.RenderTransform).ScaleX, 80, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
			ModAnimation.AaScaleTransform(PanFore, -0.01, 700, 0, new ModAnimation.AniEaseOutFluent())
		}, "MyButton Scale " + Uuid);
	}

	private void Button_MouseEnter()
	{
		ModAnimation.AniStart(ModAnimation.AaColor(PanFore, Border.BackgroundProperty, (_ColorType == ColorState.Red) ? "ColorBrushRedBack" : "ColorBrush7", 100), "MyButton Background " + Uuid);
	}

	private void Button_MouseUp()
	{
		if (isMouseDown)
		{
			isMouseDown = false;
			ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaScaleTransform(PanFore, 1.0 - ((ScaleTransform)PanFore.RenderTransform).ScaleX, 300, 10, new ModAnimation.AniEaseOutFluent()) }, "MyButton Scale " + Uuid);
		}
	}

	private void Button_MouseLeave()
	{
		ModAnimation.AniStart(ModAnimation.AaColor(PanFore, Border.BackgroundProperty, "ColorBrushHalfWhite", 200), "MyButton Background " + Uuid);
		if (isMouseDown)
		{
			isMouseDown = false;
			ModAnimation.AniStart(ModAnimation.AaScaleTransform(PanFore, 1.0 - ((ScaleTransform)PanFore.RenderTransform).ScaleX, 800, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)), "MyButton Scale " + Uuid);
		}
	}

	static MyButton()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected Obj, but got Unknown
	}
}
