using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace PCL.Core.UI.Controls;

public static class Tooltip
{
	public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(Tooltip), new PropertyMetadata((object)true));

	public static readonly DependencyProperty FollowCursorProperty = DependencyProperty.RegisterAttached("FollowCursor", typeof(bool), typeof(Tooltip), new PropertyMetadata((object)true));

	private const double ScaleClosed = 0.97;

	private const double ShadowBlur = 18.0;

	private const double ShadowAlpha = 0.15;

	private const int MaxContentWidth = 676;

	private const double TipFontSize = 12.5;

	private const double TipLineHeight = 17.0;

	private const int AnimLength = 80;

	private const int AnimExit = 80;

	private static readonly Thickness _InnerPad = new Thickness(12.0, 10.0, 12.0, 10.0);

	private static readonly DropShadowEffect _Shadow = new DropShadowEffect
	{
		Opacity = 0.15,
		BlurRadius = 18.0,
		ShadowDepth = 0.0,
		Color = Colors.Black
	};

	private static readonly DependencyProperty _KeyCombo = DependencyProperty.RegisterAttached("KeyCombo", typeof(bool), typeof(Tooltip), new PropertyMetadata((object)false));

	private static bool _running;

	private static int _gen;

	private static bool _closing;

	private static Point _cursor;

	private static FrameworkElement? _target;

	private static Popup? _flyout;

	private static Border? _shell;

	private static ScaleTransform? _scaler;

	private static Storyboard? _openStory;

	private static Storyboard? _closeStory;

	private static DispatcherTimer? _latch;

	private static HwndSourceHook? _transparentHook;

	private static readonly MouseEventHandler _OnEnterHandler = OnEnter;

	private static readonly MouseEventHandler _OnMoveHandler = OnMove;

	private static readonly MouseEventHandler _OnLeaveHandler = OnLeave;

	private static readonly MouseButtonEventHandler _OnReleaseHandler = OnRelease;

	private static readonly ToolTipEventHandler _OnOpeningHandler = OnOpening;

	private static readonly RoutedEventHandler _OnUnloadedHandler = OnUnloaded;

	private static readonly RoutedEventHandler _OnComboLoadedHandler = OnComboInit;

	private static readonly MouseButtonEventHandler _OnComboMouseDownHandler = OnComboInit;

	public static void SetIsEnabled(DependencyObject element, bool value)
	{
		element.SetValue(IsEnabledProperty, (object)value);
	}

	public static bool GetIsEnabled(DependencyObject element)
	{
		return (bool)element.GetValue(IsEnabledProperty);
	}

	public static void SetFollowCursor(DependencyObject element, bool value)
	{
		element.SetValue(FollowCursorProperty, (object)value);
	}

	public static bool GetFollowCursor(DependencyObject element)
	{
		return (bool)element.GetValue(FollowCursorProperty);
	}

	public static void Enable()
	{
		if (!_running)
		{
			_running = true;
			((Freezable)_Shadow).Freeze();
			_PrebuildStoryboards();
			EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.MouseEnterEvent, _OnEnterHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.MouseMoveEvent, _OnMoveHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.MouseLeaveEvent, _OnLeaveHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(FrameworkElement), UIElement.PreviewMouseUpEvent, _OnReleaseHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(FrameworkElement), ToolTipService.ToolTipOpeningEvent, _OnOpeningHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.UnloadedEvent, _OnUnloadedHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(ComboBox), FrameworkElement.LoadedEvent, _OnComboLoadedHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseDownEvent, _OnComboMouseDownHandler, handledEventsToo: true);
			EventManager.RegisterClassHandler(typeof(Window), UIElement.MouseLeaveEvent, new MouseEventHandler(OnWindowLeave), handledEventsToo: true);
		}
	}

	private static void OnWindowLeave(object s, MouseEventArgs e)
	{
		if (_target != null)
		{
			_WindDown();
		}
	}

	public static void Disable()
	{
		if (_running)
		{
			_running = false;
			_Hush();
		}
	}

	public static void Dismiss()
	{
		_WindDown();
	}

	private static void _PrebuildStoryboards()
	{
		_openStory = new Storyboard();
		_openStory.Children.Add(MakeAnim(1.0, "Opacity", 80));
		_openStory.Children.Add(MakeAnim(1.0, "RenderTransform.ScaleX", 80));
		_openStory.Children.Add(MakeAnim(1.0, "RenderTransform.ScaleY", 80));
		_closeStory = new Storyboard();
		_closeStory.Children.Add(MakeAnim(0.0, "Opacity", 80));
		_closeStory.Children.Add(MakeAnim(0.97, "RenderTransform.ScaleX", 80));
		_closeStory.Children.Add(MakeAnim(0.97, "RenderTransform.ScaleY", 80));
		static DoubleAnimation MakeAnim(double to, string prop, int ms)
		{
			DoubleAnimation doubleAnimation = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms)))
			{
				EasingFunction = new QuadraticEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			Storyboard.SetTargetProperty((DependencyObject)(object)doubleAnimation, new PropertyPath(prop));
			return doubleAnimation;
		}
	}

	private static void OnEnter(object s, MouseEventArgs e)
	{
		if (!_running)
		{
			return;
		}
		FrameworkElement fe = s as FrameworkElement;
		if (fe != null)
		{
			((DispatcherObject)fe).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
			{
				_TryClaim(fe);
			}), Array.Empty<object>());
		}
	}

	private static bool _IsCursorPlaced(FrameworkElement el)
	{
		bool flag = GetFollowCursor((DependencyObject)(object)el);
		if (flag)
		{
			PlacementMode placement = ToolTipService.GetPlacement((DependencyObject)(object)el);
			bool flag2 = (uint)(placement - 7) <= 1u;
			flag = flag2;
		}
		return flag;
	}

	private static void OnMove(object s, MouseEventArgs e)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		if (!_running || !(s is FrameworkElement pivot))
		{
			return;
		}
		if (Mouse.LeftButton == MouseButtonState.Pressed)
		{
			if (_target != null)
			{
				_cursor = Mouse.GetPosition(_target);
				if (_IsCursorPlaced(_target))
				{
					Popup flyout = _flyout;
					if (flyout != null && flyout.IsOpen)
					{
						_PlaceNear(_target, _cursor);
					}
				}
			}
			else
			{
				Popup flyout = _flyout;
				if (flyout != null && flyout.IsOpen && flyout.PlacementTarget is FrameworkElement frameworkElement && _IsCursorPlaced(frameworkElement))
				{
					_cursor = Mouse.GetPosition(frameworkElement);
					_PlaceNear(frameworkElement, _cursor);
				}
			}
			return;
		}
		_TryClaim(pivot);
		if (_target != null)
		{
			_cursor = Mouse.GetPosition(_target);
			if (_IsCursorPlaced(_target))
			{
				Popup flyout = _flyout;
				if (flyout != null && flyout.IsOpen)
				{
					_PlaceNear(_target, _cursor);
				}
			}
		}
		else
		{
			Popup flyout = _flyout;
			if (flyout != null && flyout.IsOpen && flyout.PlacementTarget is FrameworkElement frameworkElement2 && _IsCursorPlaced(frameworkElement2))
			{
				_cursor = Mouse.GetPosition(frameworkElement2);
				_PlaceNear(frameworkElement2, _cursor);
			}
		}
	}

	private static void OnLeave(object s, MouseEventArgs e)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (!_running || !(s is FrameworkElement frameworkElement) || frameworkElement != _target)
		{
			return;
		}
		if (_PointInside(frameworkElement, Mouse.GetPosition(frameworkElement)))
		{
			e.Handled = true;
			return;
		}
		FrameworkElement frameworkElement2 = _SeekOwner(_Over());
		if (frameworkElement2 != null && frameworkElement2 != _target)
		{
			_StartCycle(frameworkElement2, Mouse.GetPosition(frameworkElement2));
		}
		else
		{
			_WindDown();
		}
	}

	private static void OnRelease(object s, MouseButtonEventArgs e)
	{
		if (!_running)
		{
			return;
		}
		FrameworkElement fe = s as FrameworkElement;
		if (fe == null)
		{
			return;
		}
		((DispatcherObject)fe).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			if (_target != null)
			{
				FrameworkElement frameworkElement = _SeekOwner((DependencyObject?)(object)fe);
				if (frameworkElement == null)
				{
					_WindDown();
				}
				else
				{
					_StartCycle(frameworkElement, Mouse.GetPosition(frameworkElement));
				}
			}
		}), (DispatcherPriority)5, Array.Empty<object>());
	}

	private static void OnOpening(object s, ToolTipEventArgs e)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (!_running || !(s is FrameworkElement frameworkElement) || !_Eligible(frameworkElement) || !_FetchContent(frameworkElement))
		{
			return;
		}
		e.Handled = true;
		if (_DragHush(frameworkElement))
		{
			_Hush();
		}
		else if (frameworkElement.IsEnabled)
		{
			if (_target == frameworkElement)
			{
				Popup flyout = _flyout;
				if (flyout != null && flyout.IsOpen)
				{
					return;
				}
			}
			if (_target != frameworkElement)
			{
				_Hush();
			}
			_target = frameworkElement;
			DispatcherTimer? latch = _latch;
			if (latch != null)
			{
				latch.Stop();
			}
			_latch = null;
			_cursor = Mouse.GetPosition(frameworkElement);
			_PopUp(frameworkElement, _cursor);
		}
		else
		{
			if (_target != frameworkElement)
			{
				_Hush();
			}
			_target = frameworkElement;
			DispatcherTimer? latch2 = _latch;
			if (latch2 != null)
			{
				latch2.Stop();
			}
			_cursor = Mouse.GetPosition(frameworkElement);
			_PopUp(frameworkElement, _cursor);
		}
	}

	private static void OnUnloaded(object s, RoutedEventArgs e)
	{
		if (_running && s is FrameworkElement frameworkElement && frameworkElement == _target)
		{
			_WindDown();
		}
	}

	private static void _TryClaim(FrameworkElement pivot)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		FrameworkElement frameworkElement = _SeekOwner(_Over()) ?? pivot;
		if (frameworkElement == pivot && !_PointInside(pivot, Mouse.GetPosition(pivot)))
		{
			return;
		}
		if (!_Eligible(frameworkElement) || !_FetchContent(frameworkElement))
		{
			if (_target != null)
			{
				_WindDown();
			}
		}
		else if (_DragHush(frameworkElement))
		{
			if (_target != null)
			{
				DependencyObject val = _Captured();
				if (val != null && _ShareAncestor(val, (DependencyObject)(object)_target) && _PointInside(_target, Mouse.GetPosition(_target)))
				{
					return;
				}
			}
			_Hush();
		}
		else
		{
			_StartCycle(frameworkElement, Mouse.GetPosition(frameworkElement));
		}
	}

	private static DependencyObject? _Over()
	{
		IInputElement directlyOver = Mouse.DirectlyOver;
		return (DependencyObject?)((directlyOver is DependencyObject) ? directlyOver : null);
	}

	private static DependencyObject? _Captured()
	{
		IInputElement captured = Mouse.Captured;
		return (DependencyObject?)((captured is DependencyObject) ? captured : null);
	}

	private static FrameworkElement? _SeekOwner(DependencyObject? leaf)
	{
		for (DependencyObject val = leaf; val != null; val = ((val is Visual) ? VisualTreeHelper.GetParent(val) : null))
		{
			if (val is FrameworkElement frameworkElement && _Eligible(frameworkElement) && _FetchContent(frameworkElement))
			{
				return frameworkElement;
			}
		}
		return null;
	}

	private static bool _Eligible(FrameworkElement fe)
	{
		if (GetIsEnabled((DependencyObject)(object)fe) && ToolTipService.GetIsEnabled((DependencyObject)(object)fe))
		{
			if (!fe.IsEnabled)
			{
				return ToolTipService.GetShowOnDisabled((DependencyObject)(object)fe);
			}
			return true;
		}
		return false;
	}

	private static bool _FetchContent(FrameworkElement src)
	{
		object toolTip = src.ToolTip;
		if (toolTip == null)
		{
			return false;
		}
		object obj = ((toolTip is ToolTip toolTip2) ? toolTip2.Content : toolTip);
		if (obj != null)
		{
			if (obj is string text)
			{
				return text.Length > 0;
			}
			return true;
		}
		return false;
	}

	private static bool _DragHush(FrameworkElement? candidate)
	{
		if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.Captured == null)
		{
			return false;
		}
		if (candidate == null)
		{
			return true;
		}
		DependencyObject val = _Captured();
		if (val == null)
		{
			return true;
		}
		return !_ShareAncestor(val, (DependencyObject)(object)candidate);
	}

	private static bool _PointInside(FrameworkElement el, Point p)
	{
		if (p.X >= 0.0 && p.Y >= 0.0 && p.X <= el.ActualWidth)
		{
			return p.Y <= el.ActualHeight;
		}
		return false;
	}

	private static bool _ShareAncestor(DependencyObject a, DependencyObject b)
	{
		if (a == b)
		{
			return true;
		}
		if (a is ComboBox comboBox && b is ComboBoxItem container && ItemsControl.ItemsControlFromItemContainer((DependencyObject)(object)container) == comboBox)
		{
			return true;
		}
		if (b is ComboBox comboBox2 && a is ComboBoxItem container2 && ItemsControl.ItemsControlFromItemContainer((DependencyObject)(object)container2) == comboBox2)
		{
			return true;
		}
		for (DependencyObject parent = VisualTreeHelper.GetParent(b); parent != null; parent = VisualTreeHelper.GetParent(parent))
		{
			if (parent == a)
			{
				return true;
			}
		}
		for (DependencyObject parent2 = VisualTreeHelper.GetParent(a); parent2 != null; parent2 = VisualTreeHelper.GetParent(parent2))
		{
			if (parent2 == b)
			{
				return true;
			}
		}
		return false;
	}

	private static void _StartCycle(FrameworkElement target, Point pt)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if (!_Eligible(target) || !_FetchContent(target))
		{
			return;
		}
		if (_DragHush(target))
		{
			_Hush();
			return;
		}
		_Stitch(target as ComboBox);
		Popup flyout;
		if (_target == target)
		{
			_cursor = pt;
			flyout = _flyout;
			if ((flyout == null || !flyout.IsOpen) && _latch == null)
			{
				_KickTimer(target);
			}
			return;
		}
		flyout = _flyout;
		if (flyout != null && flyout.IsOpen)
		{
			_closing = false;
			_target = target;
			_cursor = pt;
			int mark = ++_gen;
			Storyboard sb = _closeStory.Clone();
			sb.Completed += (object? _, EventArgs _) =>
			{
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				if (mark == _gen)
				{
					_flyout.IsOpen = false;
					_PopUp(target, pt);
				}
				sb.Remove(_shell);
			};
			_shell.BeginStoryboard(sb);
		}
		else
		{
			_Hush();
			_target = target;
			_cursor = pt;
			_KickTimer(target);
		}
	}

	private static void _KickTimer(FrameworkElement target)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected Obj, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		DispatcherTimer? latch = _latch;
		if (latch != null)
		{
			latch.Stop();
		}
		int num = Math.Max(0, ToolTipService.GetInitialShowDelay((DependencyObject)(object)target));
		if (num == 0)
		{
			_PopUp(target, _cursor);
			return;
		}
		int mark = ++_gen;
		_latch = new DispatcherTimer(TimeSpan.FromMilliseconds(num), (DispatcherPriority)9, (EventHandler)((object? _, EventArgs _) =>
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			DispatcherTimer? latch2 = _latch;
			if (latch2 != null)
			{
				latch2.Stop();
			}
			if (mark == _gen && _target != null)
			{
				_PopUp(_target, _cursor);
			}
		}), ((DispatcherObject)target).Dispatcher);
	}

	private static void _PopUp(FrameworkElement target, Point pt)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (target == _target)
		{
			if (_flyout == null)
			{
				_BuildUi();
			}
			_flyout.PlacementTarget = target;
			_PlaceNear(target, pt);
			_shell.DataContext = (target.ToolTip as ToolTip)?.DataContext ?? target.DataContext;
			_shell.FlowDirection = target.FlowDirection;
			_RenderInside(target);
			_gen++;
			_shell.BeginStoryboard(_openStory);
			_flyout.IsOpen = true;
		}
	}

	private static void _BuildUi()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		_scaler = new ScaleTransform(0.97, 0.97);
		_shell = new Border
		{
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			MaxWidth = 700.0,
			SnapsToDevicePixels = true,
			UseLayoutRounding = true,
			RenderTransform = _scaler,
			RenderTransformOrigin = new Point(0.0, 0.0),
			Effect = _Shadow
		};
		_shell.SetResourceReference(Border.BackgroundProperty, "ColorBrushWhite");
		_shell.SetResourceReference(Border.BorderBrushProperty, "ColorBrushGray5");
		Grid grid = new Grid
		{
			Margin = new Thickness(19.0),
			SnapsToDevicePixels = true,
			UseLayoutRounding = true
		};
		grid.Children.Add(_shell);
		_flyout = new Popup
		{
			AllowsTransparency = true,
			IsHitTestVisible = false,
			StaysOpen = true,
			PopupAnimation = PopupAnimation.None,
			Placement = PlacementMode.Relative,
			Child = grid
		};
		_transparentHook = delegate(nint _, int msg, nint _, nint _, ref bool handled)
		{
			if (msg == 132)
			{
				handled = true;
				return -1;
			}
			return IntPtr.Zero;
		};
		_flyout.IsOpen = true;
		_AttachTransparentHook();
		_flyout.IsOpen = false;
		_flyout.Opened += (object? _, EventArgs _) =>
		{
			_AttachTransparentHook();
		};
	}

	private static void _AttachTransparentHook()
	{
		if (_transparentHook != null)
		{
			UIElement uIElement = _flyout?.Child;
			if (uIElement != null)
			{
				((HwndSource)PresentationSource.FromVisual(uIElement))?.AddHook(_transparentHook);
			}
		}
	}

	private static void _RenderInside(FrameworkElement owner)
	{
		_shell.Child = null;
		object toolTip = owner.ToolTip;
		ToolTip toolTip2 = toolTip as ToolTip;
		object obj = toolTip2?.Content ?? toolTip;
		if (obj != null && !(obj is string { Length: 0 }))
		{
			bool flag = toolTip2?.ContentTemplate != null || toolTip2?.ContentTemplateSelector != null;
			double maxWidth = ((toolTip2 != null && toolTip2.Width > 0.0 && !double.IsNaN(toolTip2.Width)) ? toolTip2.Width : 676.0);
			if (obj is string text2 && !flag)
			{
				TextBlock textBlock = new TextBlock
				{
					Text = text2,
					TextWrapping = TextWrapping.Wrap,
					Margin = _InnerPad,
					FontSize = 12.5,
					LineHeight = 17.0,
					LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
					MaxWidth = maxWidth
				};
				textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1");
				_shell.Child = textBlock;
			}
			else
			{
				_shell.Child = new ContentPresenter
				{
					Content = obj,
					ContentTemplate = toolTip2?.ContentTemplate,
					ContentTemplateSelector = toolTip2?.ContentTemplateSelector,
					ContentStringFormat = toolTip2?.ContentStringFormat,
					Margin = _InnerPad,
					MaxWidth = maxWidth
				};
			}
		}
	}

	private static void _PlaceNear(FrameworkElement target, Point pt)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		_flyout.PlacementTarget = target;
		PlacementMode placement = ToolTipService.GetPlacement((DependencyObject)(object)target);
		switch (placement)
		{
		case PlacementMode.Mouse:
			_flyout.Placement = PlacementMode.Left;
			_flyout.PlacementRectangle = default;
			_flyout.HorizontalOffset = Math.Round(pt.X + 15.0 + ToolTipService.GetHorizontalOffset((DependencyObject)(object)target));
			_flyout.VerticalOffset = Math.Round(pt.Y + 15.0 + ToolTipService.GetVerticalOffset((DependencyObject)(object)target));
			break;
		case PlacementMode.MousePoint:
			_flyout.Placement = PlacementMode.Relative;
			_flyout.PlacementRectangle = default;
			_flyout.HorizontalOffset = Math.Round(pt.X + ToolTipService.GetHorizontalOffset((DependencyObject)(object)target));
			_flyout.VerticalOffset = Math.Round(pt.Y + ToolTipService.GetVerticalOffset((DependencyObject)(object)target));
			break;
		default:
			_flyout.Placement = placement;
			_flyout.HorizontalOffset = ToolTipService.GetHorizontalOffset((DependencyObject)(object)target);
			_flyout.VerticalOffset = ToolTipService.GetVerticalOffset((DependencyObject)(object)target);
			_flyout.PlacementRectangle = ToolTipService.GetPlacementRectangle((DependencyObject)(object)target);
			break;
		}
	}

	private static void _WindDown()
	{
		if (_closing)
		{
			return;
		}
		_closing = true;
		DispatcherTimer? latch = _latch;
		if (latch != null)
		{
			latch.Stop();
		}
		_latch = null;
		_target = null;
		Popup flyout = _flyout;
		if (flyout == null || !flyout.IsOpen || _shell == null)
		{
			_Hush();
			return;
		}
		int mark = ++_gen;
		Storyboard sb = _closeStory.Clone();
		sb.Completed += (object? _, EventArgs _) =>
		{
			if (mark == _gen)
			{
				_Hush();
			}
			sb.Remove(_shell);
		};
		_shell.BeginStoryboard(sb);
	}

	private static void _Hush()
	{
		DispatcherTimer? latch = _latch;
		if (latch != null)
		{
			latch.Stop();
		}
		_latch = null;
		_closing = false;
		_target = null;
		_gen++;
		if (_flyout != null)
		{
			_flyout.IsOpen = false;
		}
		if (_shell != null)
		{
			_shell.BeginAnimation(UIElement.OpacityProperty, null);
			_shell.Child = null;
		}
		if (_scaler != null)
		{
			_scaler.BeginAnimation(ScaleTransform.ScaleXProperty, null);
			_scaler.BeginAnimation(ScaleTransform.ScaleYProperty, null);
		}
	}

	private static void OnComboInit(object s, RoutedEventArgs e)
	{
		_Stitch(s as ComboBox);
	}

	private static void OnComboInit(object s, MouseButtonEventArgs e)
	{
		_Stitch(s as ComboBox);
	}

	private static void _Stitch(ComboBox? box)
	{
		if (box == null || (bool)((DependencyObject)box).GetValue(_KeyCombo))
		{
			return;
		}
		((DependencyObject)box).SetValue(_KeyCombo, (object)true);
		box.DropDownOpened += (object? _, EventArgs _) =>
		{
			if (_target != null)
			{
				_WindDown();
			}
		};
	}

	static Tooltip()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected Obj, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected Obj, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected Obj, but got Unknown
	}
}
