using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PCL.Core.UI.Effects;

namespace PCL.Core.UI.Controls;

public class BlurBorder : Border
{
	internal struct Radii
	{
		internal double LeftTop;

		internal double TopLeft;

		internal double TopRight;

		internal double RightTop;

		internal double RightBottom;

		internal double BottomRight;

		internal double BottomLeft;

		internal double LeftBottom;

		internal Radii(CornerRadius radii, Thickness borders, bool outer)
		{
			double num = 0.5 * borders.Left;
			double num2 = 0.5 * borders.Top;
			double num3 = 0.5 * borders.Right;
			double num4 = 0.5 * borders.Bottom;
			if (outer)
			{
				if (_IsZero(radii.TopLeft))
				{
					LeftTop = (TopLeft = 0.0);
				}
				else
				{
					LeftTop = radii.TopLeft + num;
					TopLeft = radii.TopLeft + num2;
				}
				if (_IsZero(radii.TopRight))
				{
					TopRight = (RightTop = 0.0);
				}
				else
				{
					TopRight = radii.TopRight + num2;
					RightTop = radii.TopRight + num3;
				}
				if (_IsZero(radii.BottomRight))
				{
					RightBottom = (BottomRight = 0.0);
				}
				else
				{
					RightBottom = radii.BottomRight + num3;
					BottomRight = radii.BottomRight + num4;
				}
				if (_IsZero(radii.BottomLeft))
				{
					BottomLeft = (LeftBottom = 0.0);
					return;
				}
				BottomLeft = radii.BottomLeft + num4;
				LeftBottom = radii.BottomLeft + num;
			}
			else
			{
				LeftTop = Math.Max(0.0, radii.TopLeft - num);
				TopLeft = Math.Max(0.0, radii.TopLeft - num2);
				TopRight = Math.Max(0.0, radii.TopRight - num2);
				RightTop = Math.Max(0.0, radii.TopRight - num3);
				RightBottom = Math.Max(0.0, radii.BottomRight - num3);
				BottomRight = Math.Max(0.0, radii.BottomRight - num4);
				BottomLeft = Math.Max(0.0, radii.BottomLeft - num4);
				LeftBottom = Math.Max(0.0, radii.BottomLeft - num);
			}
		}
	}

	private const double DoubleEpsilon = 2.220446049250313E-16;

	private readonly Stack<UIElement> _panelStack = new Stack<UIElement>();

	private static readonly DependencyPropertyKey ContentClipPropertyKey = DependencyProperty.RegisterReadOnly("ContentClip", typeof(Geometry), typeof(BlurBorder), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)null));

	public static readonly DependencyProperty ContentClipProperty = ContentClipPropertyKey.DependencyProperty;

	public static readonly DependencyProperty MaxDepthProperty = BackgroundPresenter.MaxDepthProperty.AddOwner(typeof(BlurBorder));

	public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register("BlurRadius", typeof(double), typeof(BlurBorder), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)16.0, (PropertyChangedCallback)OnRenderPropertyChanged));

	public static readonly DependencyProperty BlurKernelTypeProperty = DependencyProperty.Register("BlurKernelType", typeof(KernelType), typeof(BlurBorder), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)KernelType.Gaussian, (PropertyChangedCallback)OnRenderPropertyChanged));

	public static readonly DependencyProperty BlurRenderingBiasProperty = DependencyProperty.Register("BlurRenderingBias", typeof(RenderingBias), typeof(BlurBorder), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)RenderingBias.Performance, (PropertyChangedCallback)OnRenderPropertyChanged));

	public static readonly DependencyProperty BlurSamplingRateProperty = DependencyProperty.Register("BlurSamplingRate", typeof(double), typeof(BlurBorder), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)0.9, (PropertyChangedCallback)OnRenderPropertyChanged));

	public Geometry? ContentClip
	{
		get
		{
			return (Geometry)((DependencyObject)this).GetValue(ContentClipProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ContentClipProperty, (object)value);
		}
	}

	public int MaxDepth
	{
		get
		{
			return (int)((DependencyObject)this).GetValue(MaxDepthProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(MaxDepthProperty, (object)value);
		}
	}

	public double BlurRadius
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(BlurRadiusProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(BlurRadiusProperty, (object)value);
		}
	}

	public KernelType BlurKernelType
	{
		get
		{
			return (KernelType)((DependencyObject)this).GetValue(BlurKernelTypeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(BlurKernelTypeProperty, (object)value);
		}
	}

	public RenderingBias BlurRenderingBias
	{
		get
		{
			return (RenderingBias)((DependencyObject)this).GetValue(BlurRenderingBiasProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(BlurRenderingBiasProperty, (object)value);
		}
	}

	public double BlurSamplingRate
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(BlurSamplingRateProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(BlurSamplingRateProperty, (object)Math.Max(0.1, Math.Min(1.0, value)));
		}
	}

	private static bool _IsZero(double value)
	{
		return Math.Abs(value) < 2.220446049250313E-15;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((DependencyObject)this).SetValue(ContentClipPropertyKey, (object)CalculateContentClip(this));
		return base.ArrangeOverride(finalSize);
	}

	protected override Geometry? GetLayoutClip(Size layoutSlotSize)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (!ClipToBounds)
		{
			return null;
		}
		return CalculateLayoutClip(layoutSlotSize, BorderThickness, CornerRadius);
	}

	protected override void OnVisualParentChanged(DependencyObject oldParentObject)
	{
		if (oldParentObject is UIElement uIElement)
		{
			uIElement.LayoutUpdated -= ParentLayoutUpdated;
		}
		if (Parent is UIElement uIElement2)
		{
			uIElement2.LayoutUpdated += ParentLayoutUpdated;
		}
	}

	private void ParentLayoutUpdated(object? sender, EventArgs e)
	{
		BackgroundPresenter.ForceRender(this);
	}

	private static Geometry? CalculateContentClip(Border border)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		Thickness borderThickness = border.BorderThickness;
		CornerRadius cornerRadius = border.CornerRadius;
		Size renderSize = border.RenderSize;
		double num = renderSize.Width - borderThickness.Left - borderThickness.Right;
		double num2 = renderSize.Height - borderThickness.Top - borderThickness.Bottom;
		if (num > 0.0 && num2 > 0.0)
		{
			Rect rect = new Rect(0.0, 0.0, num, num2);
			Radii radii = new Radii(cornerRadius, borderThickness, outer: false);
			StreamGeometry streamGeometry = new StreamGeometry();
			using StreamGeometryContext ctx = streamGeometry.Open();
			GenerateGeometry(ctx, rect, radii);
			((Freezable)streamGeometry).Freeze();
			return streamGeometry;
		}
		return null;
	}

	protected override void OnRender(DrawingContext dc)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		bool flag = BlurRadius == 0.0 || Opacity == 0.0;
		if (!flag)
		{
			Visibility visibility = Visibility;
			bool flag2 = visibility - 1 <= Visibility.Hidden;
			flag = flag2;
		}
		if (flag)
		{
			base.OnRender(dc);
			return;
		}
		DrawingVisual drawingVisual = new DrawingVisual();
		Size renderSize = RenderSize;
		double width = renderSize.Width;
		renderSize = RenderSize;
		drawingVisual.Clip = new RectangleGeometry(new Rect(0.0, 0.0, width, renderSize.Height));
		drawingVisual.Effect = CreateOptimizedBlurEffect();
		DrawingVisual drawingVisual2 = drawingVisual;
		using (DrawingContext drawingContext = drawingVisual2.RenderOpen())
		{
			BackgroundPresenter.DrawBackground(drawingContext, this, _panelStack, MaxDepth, throwExceptionIfParentArranging: false);
		}
		if (drawingVisual2.Drawing != null)
		{
			Geometry geometry = CalculateLayoutClip(RenderSize, BorderThickness, CornerRadius);
			if (geometry != null)
			{
				dc.PushClip(geometry);
			}
			BackgroundPresenter.DrawVisual(dc, drawingVisual2, default);
			if (geometry != null)
			{
				dc.Pop();
			}
		}
		base.OnRender(dc);
	}

	private Effect CreateOptimizedBlurEffect()
	{
		if (BlurRadius <= 2.0 || BlurSamplingRate >= 0.95)
		{
			return new BlurEffect
			{
				Radius = BlurRadius,
				KernelType = BlurKernelType,
				RenderingBias = BlurRenderingBias
			};
		}
		if (BlurRadius >= 50.0 && BlurSamplingRate <= 0.3)
		{
			OptimizedBlurEffect optimizedBlurEffect = OptimizedBlurFactory.CreateRealTimePreview(BlurRadius);
			optimizedBlurEffect.SamplingRate = Math.Max(0.1, BlurSamplingRate);
			optimizedBlurEffect.RenderingBias = RenderingBias.Performance;
			return optimizedBlurEffect.GetEffectInstance();
		}
		OptimizedBlurEffect optimizedBlurEffect2 = OptimizedBlurFactory.CreateAdaptive(BlurRadius);
		optimizedBlurEffect2.SamplingRate = BlurSamplingRate;
		optimizedBlurEffect2.RenderingBias = BlurRenderingBias;
		optimizedBlurEffect2.KernelType = BlurKernelType;
		return optimizedBlurEffect2.GetEffectInstance();
	}

	private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is UIElement target)
		{
			BackgroundPresenter.ForceRender(target);
		}
	}

	internal static void GenerateGeometry(StreamGeometryContext ctx, Rect rect, Radii radii)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		Point val = new Point(radii.LeftTop, 0.0);
		Point val2 = new Point(rect.Width - radii.RightTop, 0.0);
		Point val3 = new Point(rect.Width, radii.TopRight);
		Point val4 = new Point(rect.Width, rect.Height - radii.BottomRight);
		Point val5 = new Point(rect.Width - radii.RightBottom, rect.Height);
		Point val6 = new Point(radii.LeftBottom, rect.Height);
		Point val7 = new Point(0.0, rect.Height - radii.BottomLeft);
		Point val8 = new Point(0.0, radii.TopLeft);
		if (val.X > val2.X)
		{
			double x = (val.X = radii.LeftTop / (radii.LeftTop + radii.RightTop) * rect.Width);
			val2.X = x;
		}
		if (val3.Y > val4.Y)
		{
			double y = (val3.Y = radii.TopRight / (radii.TopRight + radii.BottomRight) * rect.Height);
			val4.Y = y;
		}
		if (val5.X < val6.X)
		{
			double x2 = (val5.X = radii.LeftBottom / (radii.LeftBottom + radii.RightBottom) * rect.Width);
			val6.X = x2;
		}
		if (val7.Y < val8.Y)
		{
			double y2 = (val7.Y = radii.TopLeft / (radii.TopLeft + radii.BottomLeft) * rect.Height);
			val8.Y = y2;
		}
		Point val9 = rect.TopLeft;
		double x3 = val9.X;
		val9 = rect.TopLeft;
		Vector val10 = new Vector(x3, val9.Y);
		val += val10;
		val2 += val10;
		val3 += val10;
		val4 += val10;
		val5 += val10;
		val6 += val10;
		val7 += val10;
		val8 += val10;
		ctx.BeginFigure(val, isFilled: true, isClosed: true);
		ctx.LineTo(val2, isStroked: true, isSmoothJoin: false);
		val9 = rect.TopRight;
		double num5 = val9.X - val2.X;
		double y3 = val3.Y;
		val9 = rect.TopRight;
		double num6 = y3 - val9.Y;
		if (!_IsZero(num5) || !_IsZero(num6))
		{
			ctx.ArcTo(val3, new Size(num5, num6), 0.0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
		}
		ctx.LineTo(val4, isStroked: true, isSmoothJoin: false);
		val9 = rect.BottomRight;
		num5 = val9.X - val5.X;
		val9 = rect.BottomRight;
		num6 = val9.Y - val4.Y;
		if (!_IsZero(num5) || !_IsZero(num6))
		{
			ctx.ArcTo(val5, new Size(num5, num6), 0.0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
		}
		ctx.LineTo(val6, isStroked: true, isSmoothJoin: false);
		double x4 = val6.X;
		val9 = rect.BottomLeft;
		num5 = x4 - val9.X;
		val9 = rect.BottomLeft;
		num6 = val9.Y - val7.Y;
		if (!_IsZero(num5) || !_IsZero(num6))
		{
			ctx.ArcTo(val7, new Size(num5, num6), 0.0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
		}
		ctx.LineTo(val8, isStroked: true, isSmoothJoin: false);
		double x5 = val.X;
		val9 = rect.TopLeft;
		num5 = x5 - val9.X;
		double y4 = val8.Y;
		val9 = rect.TopLeft;
		num6 = y4 - val9.Y;
		if (!_IsZero(num5) || !_IsZero(num6))
		{
			ctx.ArcTo(val, new Size(num5, num6), 0.0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
		}
	}

	internal static Geometry? CalculateLayoutClip(Size layoutSlotSize, Thickness borderThickness, CornerRadius cornerRadius)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (layoutSlotSize.Width <= 0.0 || layoutSlotSize.Height <= 0.0)
		{
			return new RectangleGeometry(new Rect(0.0, 0.0, 0.0, 0.0));
		}
		Rect rect = new Rect(0.0, 0.0, layoutSlotSize.Width, layoutSlotSize.Height);
		Radii radii = new Radii(cornerRadius, borderThickness, outer: true);
		StreamGeometry streamGeometry = new StreamGeometry();
		using StreamGeometryContext ctx = streamGeometry.Open();
		GenerateGeometry(ctx, rect, radii);
		((Freezable)streamGeometry).Freeze();
		return streamGeometry;
	}

	static BlurBorder()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected Obj, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected Obj, but got Unknown
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected Obj, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected Obj, but got Unknown
	}
}
