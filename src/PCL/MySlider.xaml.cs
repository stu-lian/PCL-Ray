using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using PCL.Core.App.Localization;

namespace PCL;

public partial class MySlider : Border, IComponentConnector
{
	public delegate void ChangeEventHandler(object sender, bool user);

	public delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);

	private int _Value;

	private bool changeByKey;

	public Delegate getHintText;

	public int Uuid = ModBase.GetUuid();

	public int MaxValue
	{
		get
		{
			return field;
		}
		set
		{
			if (value != field)
			{
				field = value;
				RefreshWidth(null, null);
			}
		}
	} = 100;

	public int Value
	{
		get
		{
			return _Value;
		}
		set
		{
			try
			{
				value = (int)Math.Round(ModBase.MathClamp(value, 0.0, MaxValue));
				if (_Value == value)
				{
					return;
				}
				int value2 = _Value;
				_Value = value;
				if (ModAnimation.AniControlEnabled == 0)
				{
					ModBase.RouteEventArgs e = new ModBase.RouteEventArgs();
					PreviewChange?.Invoke(this, e);
					if (e.handled)
					{
						_Value = value2;
						DragStop();
						return;
					}
				}
				if (IsLoaded && ModAnimation.AniControlEnabled == 0)
				{
					if (ActualWidth < ShapeDot.Width)
					{
						return;
					}
					double num = (double)_Value / (double)MaxValue * (ActualWidth - ShapeDot.Width);
					double num2 = Math.Abs(LineFore.Width / (ActualWidth - ShapeDot.Width) - (double)_Value / (double)MaxValue);
					double num3 = (1.0 - Math.Pow(1.0 - num2, 3.0)) * 300.0 + (double)(changeByKey ? 100 : 0);
					ModAnimation.AniStart(new ModAnimation.AniData[3]
					{
						ModAnimation.AaWidth(LineFore, Math.Max(0.0, num + ((num < 0.5) ? 0.0 : 0.5)) - LineFore.Width, (int)Math.Round(num3), 0, (num3 > 50.0) ? ((ModAnimation.AniEase)new ModAnimation.AniEaseOutFluent()) : ((ModAnimation.AniEase)new ModAnimation.AniEaseLinear())),
						ModAnimation.AaWidth(LineBack, Math.Max(0.0, ActualWidth - ShapeDot.Width - num + ((ActualWidth - ShapeDot.Width - num < 0.5) ? 0.0 : 0.5)) - LineBack.Width, (int)Math.Round(num3), 0, (num3 > 50.0) ? ((ModAnimation.AniEase)new ModAnimation.AniEaseOutFluent()) : ((ModAnimation.AniEase)new ModAnimation.AniEaseLinear())),
						ModAnimation.AaX(ShapeDot, num - ShapeDot.Margin.Left, (int)Math.Round(num3), 0, (num3 > 50.0) ? ((ModAnimation.AniEase)new ModAnimation.AniEaseOutFluent()) : ((ModAnimation.AniEase)new ModAnimation.AniEaseLinear()))
					}, "MySlider Progress " + Uuid);
				}
				else
				{
					RefreshWidth(null, null);
				}
				if (ModAnimation.AniControlEnabled == 0)
				{
					Change?.Invoke(this, user: false);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "滑动条进度改变出错", ModBase.LogLevel.Hint, null, Lang.Text("Application.Control.Error.OperationFailed"));
			}
		}
	}

	public uint ValueByKey { get; set; } = 1u;

	public event ChangeEventHandler? Change;

	public event PreviewChangeEventHandler? PreviewChange;

	public MySlider()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected Obj, but got Unknown
		InitializeComponent();
		SizeChanged += RefreshWidth;
		MouseLeftButtonDown += DragStart;
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
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			MySlider_MouseEnter();
		};
		KeyDown += MySlider_KeyDown;
	}

	private void RefreshWidth(object sender, SizeChangedEventArgs? e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (e != null)
		{
			Grid panMain = PanMain;
			Size newSize = e.NewSize;
			panMain.Width = newSize.Width;
		}
		ModAnimation.AniStop("MySlider Progress " + Uuid);
		double num = (double)_Value / (double)MaxValue * (ActualWidth - ShapeDot.Width);
		LineFore.Width = Math.Max(0.0, num + ((num < 0.5) ? 0.0 : 0.5));
		LineBack.Width = Math.Max(0.0, ActualWidth - ShapeDot.Width - num + ((ActualWidth - ShapeDot.Width - num < 0.5) ? 0.0 : 0.5));
		ModBase.SetLeft(ShapeDot, num);
	}

	private void DragStart(object sender, MouseButtonEventArgs e)
	{
		CaptureMouse();
		MouseMove += OnDragMouseMove;
		e.Handled = true;
		ModMain.dragControl = this;
		RefreshColor();
		ModMain.frmMain.DragDoing();
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(ShapeDot, 1.3 - ((ScaleTransform)ShapeDot.RenderTransform).ScaleX, 40, 0, new ModAnimation.AniEaseOutFluent()), "MySlider Scale " + Uuid);
		RefreshPopup();
		ModAnimation.AniStop("MySlider KeyPopup " + Uuid);
	}

	public void DragDoing()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Point position = Mouse.GetPosition(PanMain);
		int num = (int)Math.Round(ModBase.MathClamp((position.X - ShapeDot.Width / 2.0) / (ActualWidth - ShapeDot.Width), 0.0, 1.0) * (double)MaxValue);
		if (num != Value)
		{
			Value = num;
		}
		RefreshPopup();
	}

	private void OnDragMouseMove(object sender, MouseEventArgs e)
	{
		DragDoing();
	}

	public void DragStop()
	{
		MouseMove -= OnDragMouseMove;
		if (IsMouseCaptured)
		{
			ReleaseMouseCapture();
		}
		RefreshColor();
		ModAnimation.AniStart(ModAnimation.AaScaleTransform(ShapeDot, 1.0 - ((ScaleTransform)ShapeDot.RenderTransform).ScaleX, 200, 0, new ModAnimation.AniEaseOutFluent()), "MySlider Scale " + Uuid);
		Popup.IsOpen = false;
	}

	public void RefreshPopup()
	{
		if ((object)getHintText != null)
		{
			Popup.IsOpen = true;
			TextHint.Text = getHintText.DynamicInvoke(Value)?.ToString() ?? "";
			Typeface typeface = new Typeface(TextHint.FontFamily, TextHint.FontStyle, TextHint.FontWeight, TextHint.FontStretch);
			FormattedText formattedText = new FormattedText(TextHint.Text, Thread.CurrentThread.CurrentCulture, TextHint.FlowDirection, typeface, TextHint.FontSize, TextHint.Foreground, ModBase.dpi);
			TextHint.Width = formattedText.Width;
		}
	}

	private void RefreshColor()
	{
		try
		{
			string text;
			string text2;
			int time;
			if (IsEnabled)
			{
				if ((ModMain.dragControl != null && ((object)ModMain.dragControl).Equals((object?)this)) || IsMouseOver)
				{
					text = "ColorBrush3";
					text2 = "ColorBrush3";
					time = 40;
				}
				else
				{
					text = "ColorBrushBg0";
					text2 = "ColorBrushBg0";
					time = 100;
				}
			}
			else
			{
				text = "ColorBrushGray5";
				text2 = "ColorBrushGray5";
				time = 200;
			}
			if (IsLoaded && ModAnimation.AniControlEnabled == 0)
			{
				ModAnimation.AniStart(new ModAnimation.AniData[2]
				{
					ModAnimation.AaColor(this, Border.BorderBrushProperty, text, time),
					ModAnimation.AaColor(ShapeDot, Shape.FillProperty, text2, time)
				}, "MySlider Color " + Uuid);
			}
			else
			{
				ModAnimation.AniStop("MySlider Color " + Uuid);
				SetResourceReference(Border.BorderBrushProperty, text);
				ShapeDot.SetResourceReference(Shape.FillProperty, text2);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "滑动条颜色改变出错");
		}
	}

	private void MySlider_MouseEnter()
	{
		Focus();
	}

	private void MySlider_KeyDown(object sender, KeyEventArgs e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		if (this == ModMain.dragControl)
		{
			return;
		}
		if ((int)e.Key == 23)
		{
			changeByKey = true;
			Value = (int)(Value - ValueByKey);
			changeByKey = false;
			e.Handled = true;
		}
		else
		{
			if ((int)e.Key != 25)
			{
				return;
			}
			changeByKey = true;
			Value = (int)(Value + ValueByKey);
			changeByKey = false;
			e.Handled = true;
		}
		if ((object)getHintText != null)
		{
			RefreshPopup();
			ModAnimation.AniStop("MySlider KeyPopup " + Uuid);
			ModAnimation.AniStart(ModAnimation.AaCode(() =>
			{
				Popup.IsOpen = false;
			}, (int)Math.Round(700.0 * ModAnimation.aniSpeed)), "MySlider KeyPopup " + Uuid);
		}
	}
}
