using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PCL.Core.App;
using PCL.Core.UI.Controls.SvgIcon;
using PCL.Core.UI.Theme;

namespace PCL;

public partial class MyToast : Border, IComponentConnector
{
	public int Uuid = ModBase.GetUuid();

	private const double DragDeadzone = 4.0;

	private const double DragOpacityFloor = 0.35;

	private const double DismissThresholdRatio = 0.12;

	private const double DismissThresholdMin = 24.0;

	private const double MinRemainingMs = 300.0;

	private const int ReturnAnimationMs = 150;

	private bool _dragPending;

	private bool _isDragging;

	private Point _dragStartPoint;

	private double _dragStartTranslateX;

	private FrameworkElement? _dragReference;

	private double _progressStartWidth;

	private double _progressTotalMs;

	private double _targetHeight;


	public string Context
	{
		get
		{
			return TitleText.Text;
		}
		set
		{
			TitleText.Text = value;
		}
	}

	public string Icon { get; set; } = "lucide/info";

	public HintType ToastType { get; set; }

	public double DisplayDuration { get; set; } = 5000.0;

	public bool IsDismissing { get; private set; }

	public MyToast()
	{
		InitializeComponent();
		BtnClose.Click += (object _, EventArgs _) =>
		{
			Dismiss();
		};
		PreviewMouseLeftButtonDown += Toast_PreviewMouseLeftButtonDown;
		PreviewMouseMove += Toast_PreviewMouseMove;
		PreviewMouseLeftButtonUp += Toast_PreviewMouseLeftButtonUp;
		LostMouseCapture += Toast_LostMouseCapture;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Expected Obj, but got Unknown
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected Obj, but got Unknown
			UpdateColors();
			ThemeService.ColorModeChanged += OnThemeChanged;
			ThemeService.ColorThemeChanged += OnColorThemeChanged;
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected Obj, but got Unknown
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Expected Obj, but got Unknown
			ThemeService.ColorModeChanged -= OnThemeChanged;
			ThemeService.ColorThemeChanged -= OnColorThemeChanged;
			ModAnimation.AniStop($"Toast Show {Uuid}");
			ModAnimation.AniStop($"Toast Hide {Uuid}");
			ModAnimation.AniStop($"Toast Dismiss {Uuid}");
			ModAnimation.AniStop($"Toast Emphasize {Uuid}");
			ModAnimation.AniStop($"Toast Drag Return {Uuid}");
			ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, null);
		};
	}

	private void OnThemeChanged(bool isDarkMode, ColorTheme theme)
	{
		UpdateColors();
	}

	private void OnColorThemeChanged(ColorTheme theme)
	{
		UpdateColors();
	}

	public void Show()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if (Parent is Panel)
		{
			if (System.Windows.Application.Current.MainWindow != null)
			{
				MaxWidth = System.Windows.Application.Current.MainWindow.ActualWidth * 0.9;
			}
			Margin = new Thickness(0.0, 0.0, 16.0, 4.0);
			Opacity = 0.0;
			Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Size desiredSize = DesiredSize;
			double width = desiredSize.Width;
			desiredSize = DesiredSize;
			Arrange(new Rect(0.0, 0.0, width, desiredSize.Height));
			_targetHeight = Math.Max(ActualHeight, 45.0);
			Height = 0.0;
			RenderTransform = new TranslateTransform(60.0, 0.0);
			ModAnimation.AniStop($"Toast Drag Return {Uuid}");
			ModAnimation.AniStart(new List<ModAnimation.AniData>
			{
				ModAnimation.AaTranslateX(this, -60.0, 400, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaHeight(this, _targetHeight, 150, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaOpacity(this, 1.0, 100)
			}, $"Toast Show {Uuid}");
			RestartHideAnimation();
		}
	}

	public void Emphasize()
	{
		ModAnimation.AniStop($"Toast Show {Uuid}");
		ModAnimation.AniStop($"Toast Hide {Uuid}");
		ModAnimation.AniStop($"Toast Emphasize {Uuid}");
		ModAnimation.AniStop($"Toast Drag Return {Uuid}");
		ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, null);
		if (RenderTransform is TranslateTransform translateTransform)
		{
			translateTransform.X = 0.0;
		}
		Opacity = 1.0;
		Height = _targetHeight;
		ModAnimation.AniStart(new List<ModAnimation.AniData>
		{
			ModAnimation.AaTranslateX(this, -8.0, 70, 0, new ModAnimation.AniEaseOutFluent()),
			ModAnimation.AaTranslateX(this, 16.0, 70, 0, null, after: true),
			ModAnimation.AaTranslateX(this, -8.0, 60, 0, new ModAnimation.AniEaseOutFluent(), after: true),
			ModAnimation.AaCode(RestartHideAnimation, 0, after: true)
		}, $"Toast Emphasize {Uuid}");
	}

	private void RestartHideAnimation()
	{
		StartHideAnimation(DisplayDuration);
		StartProgressAnimation(DisplayDuration);
	}

	private void StartHideAnimation(double delayMs)
	{
		int delay = (int)Math.Round(delayMs);
		ModAnimation.AniStart(new List<ModAnimation.AniData>
		{
			ModAnimation.AaTranslateX(this, 60.0, 200, delay, new ModAnimation.AniEaseInFluent()),
			ModAnimation.AaOpacity(this, -1.0, 150, delay),
			ModAnimation.AaHeight(this, 0.0 - _targetHeight, 100, 0, new ModAnimation.AniEaseOutFluent(), after: true),
			ModAnimation.AaCode(() =>
			{
				if (Parent is Panel panel)
				{
					panel.Children.Remove(this);
				}
			}, 0, after: true)
		}, $"Toast Hide {Uuid}");
	}

	public void Dismiss()
	{
		if (IsDismissing)
		{
			return;
		}
		IsDismissing = true;
		_isDragging = false;
		_dragPending = false;
		if (IsMouseCaptured)
		{
			ReleaseMouseCapture();
		}
		ModAnimation.AniStop($"Toast Show {Uuid}");
		ModAnimation.AniStop($"Toast Hide {Uuid}");
		ModAnimation.AniStop($"Toast Emphasize {Uuid}");
		ModAnimation.AniStop($"Toast Drag Return {Uuid}");
		ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, null);
		ModAnimation.AniStart(new List<ModAnimation.AniData>
		{
			ModAnimation.AaTranslateX(this, 60.0, 150, 0, new ModAnimation.AniEaseInFluent()),
			ModAnimation.AaOpacity(this, -1.0, 100),
			ModAnimation.AaCode(() =>
			{
				if (Parent is Panel panel)
				{
					panel.Children.Remove(this);
				}
			}, 0, after: true)
		}, $"Toast Dismiss {Uuid}");
	}

	private void StartProgressAnimation(double duration)
	{
		int num = (int)Math.Round(duration);
		if (num > 0)
		{
			double num2 = ProgressBar.ActualWidth;
			if (num2 <= 0.0)
			{
				num2 = 300.0;
			}
			ProgressBar.HorizontalAlignment = HorizontalAlignment.Left;
			ProgressBar.Width = num2;
			_progressStartWidth = num2;
			_progressTotalMs = num;
			DoubleAnimation animation = new DoubleAnimation(num2, 0.0, TimeSpan.FromMilliseconds(num));
			ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, animation);
		}
	}

	private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		RootGrid.Clip = new RectangleGeometry(new Rect(0.0, 0.0, RootGrid.ActualWidth, RootGrid.ActualHeight), 8.0, 8.0);
	}

	private void UpdateColors()
	{
		double sH = ToastType switch
		{
			HintType.Success => 145.0, 
			HintType.Error => 355.0, 
			HintType.Warning => 40.0, 
			_ => 210.0, 
		};
		ResourceDictionary resources = System.Windows.Application.Current.Resources;
		ModBase.MyColor myColor = new ModBase.MyColor().FromHSL2(sH, 75.0, 60.0);
		Brush brush = (ThemeService.IsDarkMode ? new SolidColorBrush((Color)LabColor.FromLch(0.35, 0.0, 0.0, 1.0)) : ((Brush)resources["ColorBrushBackground"]));
		SolidColorBrush foreground = (SolidColorBrush)resources["ColorBrushGray1"];
		SolidColorBrush solidColorBrush = new SolidColorBrush(myColor);
		Root.Background = brush;
		Root.BorderBrush = brush;
		TitleText.Foreground = foreground;
		ProgressBar.Fill = solidColorBrush;
		BtnClose.Foreground = foreground;
		ToastIcon.Icon = Icon;
		ToastIcon.IconBrush = solidColorBrush;
		ToastIcon.StrokeThickness = 0.0;
	}

	private void Toast_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		_dragPending = false;
		if (IsDismissing)
		{
			return;
		}
		object originalSource = e.OriginalSource;
		if (!IsDescendantOf((DependencyObject?)((originalSource is DependencyObject) ? originalSource : null), (DependencyObject)(object)BtnClose))
		{
			_dragReference = Parent as FrameworkElement;
			if (_dragReference != null)
			{
				_dragPending = true;
				_isDragging = false;
				_dragStartPoint = e.GetPosition(_dragReference);
			}
		}
	}

	private void Toast_PreviewMouseMove(object sender, MouseEventArgs e)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (_isDragging)
		{
			if (Mouse.LeftButton != MouseButtonState.Pressed || _dragReference == null)
			{
				_isDragging = false;
				_dragPending = false;
				if (IsMouseCaptured)
				{
					ReleaseMouseCapture();
				}
				ReturnFromDrag();
			}
			else
			{
				Point position = e.GetPosition(_dragReference);
				UpdateDragPosition(position.X - _dragStartPoint.X);
				e.Handled = true;
			}
		}
		else
		{
			if (!_dragPending)
			{
				return;
			}
			if (Mouse.LeftButton != MouseButtonState.Pressed || _dragReference == null)
			{
				_dragPending = false;
				return;
			}
			Point position2 = e.GetPosition(_dragReference);
			double num = position2.X - _dragStartPoint.X;
			if (!(num < 4.0))
			{
				BeginDrag(num);
			}
		}
	}

	private void Toast_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_dragPending && !_isDragging)
		{
			_dragPending = false;
		}
		else if (_isDragging)
		{
			_isDragging = false;
			_dragPending = false;
			e.Handled = true;
			if (IsMouseCaptured)
			{
				ReleaseMouseCapture();
			}
			if (((RenderTransform as TranslateTransform)?.X ?? 0.0) - _dragStartTranslateX >= GetDismissThreshold())
			{
				Dismiss();
			}
			else
			{
				ReturnFromDrag();
			}
		}
	}

	private void Toast_LostMouseCapture(object sender, MouseEventArgs e)
	{
		if (_isDragging)
		{
			_isDragging = false;
			_dragPending = false;
			ReturnFromDrag();
		}
	}

	private void BeginDrag(double initialDelta)
	{
		_isDragging = true;
		_dragPending = false;
		ModAnimation.AniStop($"Toast Show {Uuid}");
		ModAnimation.AniStop($"Toast Hide {Uuid}");
		ModAnimation.AniStop($"Toast Emphasize {Uuid}");
		ModAnimation.AniStop($"Toast Drag Return {Uuid}");
		PauseProgress();
		Height = _targetHeight;
		_dragStartTranslateX = (RenderTransform as TranslateTransform)?.X ?? 0.0;
		CaptureMouse();
		UpdateDragPosition(initialDelta);
	}

	private void UpdateDragPosition(double delta)
	{
		double num = _dragStartTranslateX + ApplyDragResistance(delta);
		if (RenderTransform is TranslateTransform translateTransform)
		{
			translateTransform.X = num;
		}
		Opacity = GetDragOpacity(num);
	}

	private void ReturnFromDrag()
	{
		if (Parent == null || IsDismissing)
		{
			return;
		}
		double num = (RenderTransform as TranslateTransform)?.X ?? 0.0;
		double opacity = Opacity;
		double progressRemainingMs = GetProgressRemainingMs();
		if (progressRemainingMs < 300.0)
		{
			Dismiss();
			return;
		}
		ResumeProgress(progressRemainingMs);
		ModAnimation.AniStart(new List<ModAnimation.AniData>
		{
			ModAnimation.AaTranslateX(this, 0.0 - num, 150, 0, new ModAnimation.AniEaseOutFluent()),
			ModAnimation.AaOpacity(this, 1.0 - opacity, 150),
			ModAnimation.AaCode(() =>
			{
				StartHideAnimation(GetProgressRemainingMs());
			}, 0, after: true)
		}, $"Toast Drag Return {Uuid}");
	}

	private static double ApplyDragResistance(double delta)
	{
		return Math.Max(0.0, delta);
	}

	private double GetDragOpacity(double translateX)
	{
		if (translateX <= 0.0)
		{
			return 1.0;
		}
		double num = ((ActualWidth > 0.0) ? ActualWidth : 1.0);
		return Math.Max(0.35, 1.0 - translateX / num * 0.65);
	}

	private double GetDismissThreshold()
	{
		return Math.Max(24.0, ActualWidth * 0.12);
	}

	private static bool IsDescendantOf(DependencyObject? descendant, DependencyObject ancestor)
	{
		while (descendant != null)
		{
			if (descendant == ancestor)
			{
				return true;
			}
			descendant = VisualTreeHelper.GetParent(descendant);
		}
		return false;
	}

	private void PauseProgress()
	{
		double width = ProgressBar.Width;
		ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, null);
		ProgressBar.Width = width;
	}

	private void ResumeProgress(double remainingMs)
	{
		if (!(remainingMs <= 0.0))
		{
			double width = ProgressBar.Width;
			if (!(width <= 0.0))
			{
				DoubleAnimation animation = new DoubleAnimation(width, 0.0, TimeSpan.FromMilliseconds(remainingMs));
				ProgressBar.BeginAnimation(FrameworkElement.WidthProperty, animation);
			}
		}
	}

	private double GetProgressRemainingMs()
	{
		if (_progressStartWidth <= 0.0)
		{
			return 0.0;
		}
		double width = ProgressBar.Width;
		return _progressTotalMs * (width / _progressStartWidth);
	}

}
