using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PCL;

public class MyCollapseBar : StackPanel
{
	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(MyCollapseBar), new PropertyMetadata((object)"", (PropertyChangedCallback)((DependencyObject d, DependencyPropertyChangedEventArgs e) =>
	{
		((MyCollapseBar)(object)d)._titleBlock.Text = (string)e.NewValue;
	})));

	private const double HeaderHeight = 30.0;

	private readonly int _uuid = ModBase.GetUuid();

	private readonly TextBlock _titleBlock;

	private readonly Path _triangle;

	private readonly StackPanel _contentPanel;

	private (MyCard card, bool useAnimation)? _parentCardState;

	public string Title
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TitleProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TitleProperty, (object)value);
		}
	}

	public StackPanel ContentPanel => _contentPanel;

	public bool IsCollapsed
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}
			field = value;
			double num = (value ? 0.0 : 180.0);
			if (IsLoaded)
			{
				ModAnimation.AniStart(ModAnimation.AaRotateTransform(_triangle, num - ((RotateTransform)_triangle.RenderTransform).Angle, 250, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)), "MyCollapseBar " + _uuid, refreshTime: true);
			}
			else
			{
				((RotateTransform)_triangle.RenderTransform).Angle = num;
			}
			if (IsLoaded && ActualHeight > 0.0)
			{
				if (value)
				{
					CollapseWithAnimation();
				}
				else
				{
					ExpandWithAnimation();
				}
			}
			else
			{
				_contentPanel.Visibility = (value ? Visibility.Collapsed : Visibility.Visible);
			}
			Toggled?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler? Toggled;

	public MyCollapseBar()
	{
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		Orientation = Orientation.Vertical;
		ClipToBounds = true;
		_titleBlock = new TextBlock
		{
			FontSize = 14.0,
			FontWeight = FontWeights.Bold,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(6.0, 0.0, 0.0, 0.0),
			IsHitTestVisible = false
		};
		_titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1");
		_triangle = new Path
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			Stretch = Stretch.Uniform,
			Height = 6.0,
			Width = 10.0,
			Margin = new Thickness(0.0, 0.0, 12.0, 0.0),
			IsHitTestVisible = false,
			Data = (Geometry)new GeometryConverter().ConvertFromString("M2,4 l-2,2 10,10 10,-10 -2,-2 -8,8 -8,-8 z"),
			RenderTransform = new RotateTransform(180.0),
			RenderTransformOrigin = new Point(0.5, 0.5)
		};
		_triangle.SetResourceReference(Shape.FillProperty, "ColorBrush1");
		Grid grid = new Grid
		{
			Height = 30.0,
			Background = Brushes.Transparent,
			Cursor = Cursors.Hand
		};
		grid.Children.Add(_titleBlock);
		grid.Children.Add(_triangle);
		grid.MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			IsCollapsed = !IsCollapsed;
		};
		_contentPanel = new StackPanel
		{
			Margin = new Thickness(6.0, 2.0, 0.0, 0.0)
		};
		Children.Add(grid);
		Children.Add(_contentPanel);
	}

	private void CollapseWithAnimation()
	{
		ModAnimation.AniStop("MyCollapseBar Height " + _uuid);
		RestoreParentCardOnInterrupt();
		SilenceParentCard();
		double num = (Height = ActualHeight);
		ModAnimation.AniStart(new List<ModAnimation.AniData>
		{
			ModAnimation.AaHeight(this, 30.0 - num, 200, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
			ModAnimation.AaCode(() =>
			{
				_contentPanel.Visibility = Visibility.Collapsed;
				Height = double.NaN;
				RestoreParentCard();
			}, 0, after: true)
		}, "MyCollapseBar Height " + _uuid);
	}

	private void ExpandWithAnimation()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		ModAnimation.AniStop("MyCollapseBar Height " + _uuid);
		RestoreParentCardOnInterrupt();
		SilenceParentCard();
		_contentPanel.Visibility = Visibility.Visible;
		Height = double.NaN;
		Measure(new Size(ActualWidth, double.PositiveInfinity));
		Size desiredSize = DesiredSize;
		double height = desiredSize.Height;
		Height = 30.0;
		ModAnimation.AniStart(new List<ModAnimation.AniData>
		{
			ModAnimation.AaHeight(this, height - 30.0, 200, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
			ModAnimation.AaCode(() =>
			{
				Height = double.NaN;
				RestoreParentCard();
			}, 0, after: true)
		}, "MyCollapseBar Height " + _uuid);
	}

	private void RestoreParentCardOnInterrupt()
	{
		(MyCard, bool)? parentCardState = _parentCardState;
		if (parentCardState.HasValue)
		{
			(MyCard, bool) valueOrDefault = parentCardState.GetValueOrDefault();
			valueOrDefault.Item1.UseAnimation = valueOrDefault.Item2;
			_parentCardState = null;
		}
	}

	private void SilenceParentCard()
	{
		(MyCard, bool)? parentCardState = _parentCardState;
		if (parentCardState.HasValue)
		{
			return;
		}
		for (FrameworkElement frameworkElement = Parent as FrameworkElement; frameworkElement != null; frameworkElement = frameworkElement.Parent as FrameworkElement)
		{
			if (frameworkElement is MyCard myCard)
			{
				_parentCardState = (myCard, myCard.UseAnimation);
				myCard.UseAnimation = false;
				break;
			}
		}
	}

	private void RestoreParentCard()
	{
		(MyCard, bool)? parentCardState = _parentCardState;
		if (parentCardState.HasValue)
		{
			(MyCard, bool) valueOrDefault = parentCardState.GetValueOrDefault();
			valueOrDefault.Item1.UseAnimation = valueOrDefault.Item2;
			_parentCardState = null;
		}
	}

	static MyCollapseBar()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected Obj, but got Unknown
	}
}
