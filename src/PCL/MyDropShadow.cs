using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCL;

public class MyDropShadow : Decorator
{
	private enum Placement
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Center,
		Right,
		BottomLeft,
		Bottom,
		BottomRight
	}

	public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(MyDropShadow), (PropertyMetadata)(object)new FrameworkPropertyMetadata(Color.FromArgb(113, 0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender, ClearBrushes));

	public static readonly DependencyProperty ShadowRadiusProperty = DependencyProperty.Register("ShadowRadius", typeof(double), typeof(MyDropShadow), (PropertyMetadata)(object)new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender, ClearBrushes));

	public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(MyDropShadow), (PropertyMetadata)(object)new FrameworkPropertyMetadata(default(CornerRadius), FrameworkPropertyMetadataOptions.AffectsRender, ClearBrushes), (ValidateValueCallback)IsCornerRadiusValid);

	private static Brush[] _commonBrushes;

	private static CornerRadius _commonCornerRadius;

	private static readonly object _resourceAccess = new object();

	private Brush[] _brushes;

	public Color Color
	{
		get
		{
			return (Color)((DependencyObject)this).GetValue(ColorProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ColorProperty, (object)value);
		}
	}

	public double ShadowRadius
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(ShadowRadiusProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ShadowRadiusProperty, (object)value);
		}
	}

	public CornerRadius CornerRadius
	{
		get
		{
			return (CornerRadius)((DependencyObject)this).GetValue(CornerRadiusProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(CornerRadiusProperty, (object)value);
		}
	}

	private static bool IsCornerRadiusValid(object value)
	{
		CornerRadius cornerRadius = (CornerRadius)value;
		if (!(cornerRadius.TopLeft < 0.0) && !(cornerRadius.TopRight < 0.0) && !(cornerRadius.BottomLeft < 0.0) && !(cornerRadius.BottomRight < 0.0) && !double.IsNaN(cornerRadius.TopLeft) && !double.IsNaN(cornerRadius.TopRight) && !double.IsNaN(cornerRadius.BottomLeft) && !double.IsNaN(cornerRadius.BottomRight) && !double.IsInfinity(cornerRadius.TopLeft) && !double.IsInfinity(cornerRadius.TopRight) && !double.IsInfinity(cornerRadius.BottomLeft))
		{
			return !double.IsInfinity(cornerRadius.BottomRight);
		}
		return false;
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_056f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_059d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0689: Unknown result type (might be due to invalid IL or missing references)
		//IL_062a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0649: Unknown result type (might be due to invalid IL or missing references)
		//IL_0668: Unknown result type (might be due to invalid IL or missing references)
		CornerRadius cornerRadius = CornerRadius;
		Size renderSize = RenderSize;
		double width = renderSize.Width;
		renderSize = RenderSize;
		Rect val = new Rect(0.0, 0.0, width, renderSize.Height);
		Color color = Color;
		if (!(val.Width > 0.0) || !(val.Height > 0.0) || color.A <= 0)
		{
			return;
		}
		double num = val.Right - val.Left - 2.0 * ShadowRadius;
		double num2 = val.Bottom - val.Top - 2.0 * ShadowRadius;
		double val2 = Math.Min(num * 0.5, num2 * 0.5);
		cornerRadius.TopLeft = Math.Min(cornerRadius.TopLeft, val2);
		cornerRadius.TopRight = Math.Min(cornerRadius.TopRight, val2);
		cornerRadius.BottomLeft = Math.Min(cornerRadius.BottomLeft, val2);
		cornerRadius.BottomRight = Math.Min(cornerRadius.BottomRight, val2);
		Brush[] brushes = GetBrushes(color, cornerRadius);
		double num3 = val.Top + ShadowRadius;
		double num4 = val.Left + ShadowRadius;
		double num5 = val.Right - ShadowRadius;
		double num6 = val.Bottom - ShadowRadius;
		double[] array = new double[6]
		{
			num4,
			num4 + cornerRadius.TopLeft,
			num5 - cornerRadius.TopRight,
			num4 + cornerRadius.BottomLeft,
			num5 - cornerRadius.BottomRight,
			num5
		};
		double[] array2 = new double[6]
		{
			num3,
			num3 + cornerRadius.TopLeft,
			num3 + cornerRadius.TopRight,
			num6 - cornerRadius.BottomLeft,
			num6 - cornerRadius.BottomRight,
			num6
		};
		drawingContext.PushGuidelineSet(new GuidelineSet(array, array2));
		cornerRadius.TopLeft += ShadowRadius;
		cornerRadius.TopRight += ShadowRadius;
		cornerRadius.BottomLeft += ShadowRadius;
		cornerRadius.BottomRight += ShadowRadius;
		drawingContext.DrawRectangle(rectangle: new Rect(val.Left, val.Top, cornerRadius.TopLeft, cornerRadius.TopLeft), brush: brushes[0], pen: null);
		double num7 = array[2] - array[1];
		if (num7 > 0.0)
		{
			drawingContext.DrawRectangle(rectangle: new Rect(array[1], val.Top, num7, ShadowRadius), brush: brushes[1], pen: null);
		}
		drawingContext.DrawRectangle(rectangle: new Rect(array[2], val.Top, cornerRadius.TopRight, cornerRadius.TopRight), brush: brushes[2], pen: null);
		double num8 = array2[3] - array2[1];
		if (num8 > 0.0)
		{
			drawingContext.DrawRectangle(rectangle: new Rect(val.Left, array2[1], ShadowRadius, num8), brush: brushes[3], pen: null);
		}
		double num9 = array2[4] - array2[2];
		if (num9 > 0.0)
		{
			drawingContext.DrawRectangle(rectangle: new Rect(array[5], array2[2], ShadowRadius, num9), brush: brushes[5], pen: null);
		}
		drawingContext.DrawRectangle(rectangle: new Rect(val.Left, array2[3], cornerRadius.BottomLeft, cornerRadius.BottomLeft), brush: brushes[6], pen: null);
		double num10 = array[4] - array[3];
		if (num10 > 0.0)
		{
			drawingContext.DrawRectangle(rectangle: new Rect(array[3], array2[5], num10, ShadowRadius), brush: brushes[7], pen: null);
		}
		drawingContext.DrawRectangle(rectangle: new Rect(array[4], array2[4], cornerRadius.BottomRight, cornerRadius.BottomRight), brush: brushes[8], pen: null);
		if (cornerRadius.TopLeft == ShadowRadius && cornerRadius.TopLeft == cornerRadius.TopRight && cornerRadius.TopLeft == cornerRadius.BottomLeft && cornerRadius.TopLeft == cornerRadius.BottomRight)
		{
			drawingContext.DrawRectangle(rectangle: new Rect(array[0], array2[0], num, num2), brush: brushes[4], pen: null);
		}
		else
		{
			PathFigure pathFigure = new PathFigure();
			if (cornerRadius.TopLeft > ShadowRadius)
			{
				pathFigure.StartPoint = new Point(array[1], array2[0]);
				pathFigure.Segments.Add(new LineSegment(new Point(array[1], array2[1]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[0], array2[1]), isStroked: true));
			}
			else
			{
				pathFigure.StartPoint = new Point(array[0], array2[0]);
			}
			if (cornerRadius.BottomLeft > ShadowRadius)
			{
				pathFigure.Segments.Add(new LineSegment(new Point(array[0], array2[3]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[3], array2[3]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[3], array2[5]), isStroked: true));
			}
			else
			{
				pathFigure.Segments.Add(new LineSegment(new Point(array[0], array2[5]), isStroked: true));
			}
			if (cornerRadius.BottomRight > ShadowRadius)
			{
				pathFigure.Segments.Add(new LineSegment(new Point(array[4], array2[5]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[4], array2[4]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[5], array2[4]), isStroked: true));
			}
			else
			{
				pathFigure.Segments.Add(new LineSegment(new Point(array[5], array2[5]), isStroked: true));
			}
			if (cornerRadius.TopRight > ShadowRadius)
			{
				pathFigure.Segments.Add(new LineSegment(new Point(array[5], array2[2]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[2], array2[2]), isStroked: true));
				pathFigure.Segments.Add(new LineSegment(new Point(array[2], array2[0]), isStroked: true));
			}
			else
			{
				pathFigure.Segments.Add(new LineSegment(new Point(array[5], array2[0]), isStroked: true));
			}
			pathFigure.IsClosed = true;
			((Freezable)pathFigure).Freeze();
			PathGeometry pathGeometry = new PathGeometry();
			pathGeometry.Figures.Add(pathFigure);
			((Freezable)pathGeometry).Freeze();
			drawingContext.DrawGeometry(brushes[4], null, pathGeometry);
		}
		drawingContext.Pop();
	}

	private static void ClearBrushes(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{
		((MyDropShadow)(object)o)._brushes = null;
	}

	private GradientStopCollection CreateStops(Color c, double cornerRadius)
	{
		double num = 1.0 / (ShadowRadius + cornerRadius);
		GradientStopCollection gradientStopCollection = new GradientStopCollection();
		Color color = c;
		gradientStopCollection.Add(new GradientStop(color, (ShadowRadius * 0.1 + cornerRadius) * num));
		color.A = (byte)Math.Round(0.74336 * (double)(int)c.A);
		gradientStopCollection.Add(new GradientStop(color, (ShadowRadius * 0.3 + cornerRadius) * num));
		color.A = (byte)Math.Round(0.38053 * (double)(int)c.A);
		gradientStopCollection.Add(new GradientStop(color, (ShadowRadius * 0.5 + cornerRadius) * num));
		color.A = (byte)Math.Round(0.12389 * (double)(int)c.A);
		gradientStopCollection.Add(new GradientStop(color, (ShadowRadius * 0.7 + cornerRadius) * num));
		color.A = (byte)Math.Round(0.02654 * (double)(int)c.A);
		gradientStopCollection.Add(new GradientStop(color, (ShadowRadius * 0.9 + cornerRadius) * num));
		color.A = 0;
		gradientStopCollection.Add(new GradientStop(color, (ShadowRadius + cornerRadius) * num));
		((Freezable)gradientStopCollection).Freeze();
		return gradientStopCollection;
	}

	private Brush[] CreateBrushes(Color c, CornerRadius cornerRadius)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
		Brush[] array = new Brush[9]
		{
			null,
			null,
			null,
			null,
			new SolidColorBrush(c),
			null,
			null,
			null,
			null
		};
		((Freezable)array[4]).Freeze();
		GradientStopCollection gradientStopCollection = CreateStops(c, 0.0);
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush(gradientStopCollection, new Point(0.0, 1.0), new Point(0.0, 0.0));
		((Freezable)linearGradientBrush).Freeze();
		array[1] = linearGradientBrush;
		LinearGradientBrush linearGradientBrush2 = new LinearGradientBrush(gradientStopCollection, new Point(1.0, 0.0), new Point(0.0, 0.0));
		((Freezable)linearGradientBrush2).Freeze();
		array[3] = linearGradientBrush2;
		LinearGradientBrush linearGradientBrush3 = new LinearGradientBrush(gradientStopCollection, new Point(0.0, 0.0), new Point(1.0, 0.0));
		((Freezable)linearGradientBrush3).Freeze();
		array[5] = linearGradientBrush3;
		LinearGradientBrush linearGradientBrush4 = new LinearGradientBrush(gradientStopCollection, new Point(0.0, 0.0), new Point(0.0, 1.0));
		((Freezable)linearGradientBrush4).Freeze();
		array[7] = linearGradientBrush4;
		GradientStopCollection gradientStopCollection2 = ((cornerRadius.TopLeft != 0.0) ? CreateStops(c, cornerRadius.TopLeft) : gradientStopCollection);
		RadialGradientBrush radialGradientBrush = new RadialGradientBrush(gradientStopCollection2)
		{
			RadiusX = 1.0,
			RadiusY = 1.0,
			Center = new Point(1.0, 1.0),
			GradientOrigin = new Point(1.0, 1.0)
		};
		((Freezable)radialGradientBrush).Freeze();
		array[0] = radialGradientBrush;
		GradientStopCollection gradientStopCollection3;
		if (cornerRadius.TopRight == 0.0)
		{
			gradientStopCollection3 = gradientStopCollection;
		}
		else
		{
			gradientStopCollection3 = ((cornerRadius.TopRight != cornerRadius.TopLeft) ? CreateStops(c, cornerRadius.TopRight) : gradientStopCollection2);
		}
		RadialGradientBrush radialGradientBrush2 = new RadialGradientBrush(gradientStopCollection3)
		{
			RadiusX = 1.0,
			RadiusY = 1.0,
			Center = new Point(0.0, 1.0),
			GradientOrigin = new Point(0.0, 1.0)
		};
		((Freezable)radialGradientBrush2).Freeze();
		array[2] = radialGradientBrush2;
		GradientStopCollection gradientStopCollection4;
		if (cornerRadius.BottomLeft == 0.0)
		{
			gradientStopCollection4 = gradientStopCollection;
		}
		else if (cornerRadius.BottomLeft == cornerRadius.TopLeft)
		{
			gradientStopCollection4 = gradientStopCollection2;
		}
		else
		{
			gradientStopCollection4 = ((cornerRadius.BottomLeft != cornerRadius.TopRight) ? CreateStops(c, cornerRadius.BottomLeft) : gradientStopCollection3);
		}
		RadialGradientBrush radialGradientBrush3 = new RadialGradientBrush(gradientStopCollection4)
		{
			RadiusX = 1.0,
			RadiusY = 1.0,
			Center = new Point(1.0, 0.0),
			GradientOrigin = new Point(1.0, 0.0)
		};
		((Freezable)radialGradientBrush3).Freeze();
		array[6] = radialGradientBrush3;
		GradientStopCollection gradientStopCollection5;
		if (cornerRadius.BottomRight == 0.0)
		{
			gradientStopCollection5 = gradientStopCollection;
		}
		else if (cornerRadius.BottomRight == cornerRadius.TopLeft)
		{
			gradientStopCollection5 = gradientStopCollection2;
		}
		else if (cornerRadius.BottomRight == cornerRadius.TopRight)
		{
			gradientStopCollection5 = gradientStopCollection3;
		}
		else
		{
			gradientStopCollection5 = ((cornerRadius.BottomRight != cornerRadius.BottomLeft) ? CreateStops(c, cornerRadius.BottomRight) : gradientStopCollection4);
		}
		RadialGradientBrush radialGradientBrush4 = new RadialGradientBrush(gradientStopCollection5)
		{
			RadiusX = 1.0,
			RadiusY = 1.0,
			Center = new Point(0.0, 0.0),
			GradientOrigin = new Point(0.0, 0.0)
		};
		((Freezable)radialGradientBrush4).Freeze();
		array[8] = radialGradientBrush4;
		return array;
	}

	private Brush[] GetBrushes(Color c, CornerRadius cornerRadius)
	{
		if (_commonBrushes == null)
		{
			lock (_resourceAccess)
			{
				if (_commonBrushes == null)
				{
					_commonBrushes = CreateBrushes(c, cornerRadius);
					_commonCornerRadius = cornerRadius;
				}
			}
		}
		if (c == ((SolidColorBrush)_commonBrushes[4]).Color && cornerRadius == _commonCornerRadius)
		{
			_brushes = null;
			return _commonBrushes;
		}
		if (_brushes == null)
		{
			_brushes = CreateBrushes(c, cornerRadius);
		}
		return _brushes;
	}

	static MyDropShadow()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected Obj, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected Obj, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected Obj, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected Obj, but got Unknown
	}
}
