using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PCL;

public class AnimatedBackgroundGrid : Grid
{
	public static readonly DependencyProperty BackgroundBrushProperty = DependencyProperty.Register("BackgroundBrush", typeof(SolidColorBrush), typeof(AnimatedBackgroundGrid), new PropertyMetadata((object)new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), (PropertyChangedCallback)_BackgroundBrushChanged));

	private readonly DependencyProperty _animatableBrushProperty;

	public readonly int uuid = ModBase.GetUuid();

	protected virtual FrameworkElement AnimatableElement => this;

	protected virtual SolidColorBrush AnimatableBrush
	{
		get
		{
			return (SolidColorBrush)Background;
		}
		set
		{
			Background = value;
		}
	}

	protected bool IsAnimating
	{
		get
		{
			return field;
		}
		private set
		{
			field = value;
		}
	}

	public SolidColorBrush BackgroundBrush
	{
		get
		{
			return (SolidColorBrush)((DependencyObject)this).GetValue(BackgroundBrushProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(BackgroundBrushProperty, (object)value);
		}
	}

	public AnimatedBackgroundGrid(DependencyProperty brushDp)
	{
		_animatableBrushProperty = brushDp;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Init();
		};
	}

	public AnimatedBackgroundGrid()
		: this(Panel.BackgroundProperty)
	{
	}

	private static void _BackgroundBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AnimatedBackgroundGrid grid = (AnimatedBackgroundGrid)(object)d;
		SolidColorBrush brush = (SolidColorBrush)e.NewValue;
		if (!grid.IsLoaded || !grid.IsVisible)
		{
			grid.AnimatableBrush = brush;
			return;
		}
		((DispatcherObject)grid).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			grid.IsAnimating = true;
			ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaColor(grid.AnimatableElement, grid._animatableBrushProperty, new ModBase.MyColor(brush) - grid.AnimatableBrush, 300) }, "MyCard Theme " + grid.uuid);
			await Task.Delay(300);
			grid.AnimatableBrush = brush;
			grid.IsAnimating = false;
		}), Array.Empty<object>());
	}

	private void Init()
	{
		AnimatableBrush = BackgroundBrush;
	}

	static AnimatedBackgroundGrid()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
	}
}
