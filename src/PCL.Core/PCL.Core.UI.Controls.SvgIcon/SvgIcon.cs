using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using PCL.Core.UI.Animation;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.UI.Controls.SvgIcon;

public class SvgIcon : FrameworkElement
{
	public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(SvgIcon), (PropertyMetadata)(object)new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, _OnIconChanged));

	public static readonly DependencyProperty DefaultPackProperty = DependencyProperty.Register("DefaultPack", typeof(string), typeof(SvgIcon), (PropertyMetadata)(object)new FrameworkPropertyMetadata("default", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, _OnIconChanged));

	public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register("IconBrush", typeof(Brush), typeof(SvgIcon), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(SvgIcon), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)2.0, FrameworkPropertyMetadataOptions.AffectsRender), (ValidateValueCallback)((object value) => value is double num && !double.IsNaN(num) && num >= 0.0));

	public static readonly DependencyProperty UseOriginalColorProperty = DependencyProperty.Register("UseOriginalColor", typeof(bool), typeof(SvgIcon), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)false, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(SvgIcon), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

	private SvgIconModel? _model;

	private bool _modelLoaded;

	public string Icon
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(IconProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(IconProperty, (object)value);
		}
	}

	public string DefaultPack
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(DefaultPackProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(DefaultPackProperty, (object)value);
		}
	}

	public Brush IconBrush
	{
		get
		{
			return (Brush)((DependencyObject)this).GetValue(IconBrushProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(IconBrushProperty, (object)value);
		}
	}

	public double StrokeThickness
	{
		get
		{
			return (double)((DependencyObject)this).GetValue(StrokeThicknessProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(StrokeThicknessProperty, (object)value);
		}
	}

	public bool UseOriginalColor
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(UseOriginalColorProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(UseOriginalColorProperty, (object)value);
		}
	}

	public Stretch Stretch
	{
		get
		{
			return (Stretch)((DependencyObject)this).GetValue(StretchProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(StretchProperty, (object)value);
		}
	}

	public IAnimation AnimateIconBrushTo(NColor color, TimeSpan? duration = null, IEasing? easing = null, string? animationKey = null)
	{
		_EnsureAnimatableIconBrush();
		return new NColorFromToAnimation
		{
			Name = (animationKey ?? $"SvgIconColor {RuntimeHelpers.GetHashCode(this)}"),
			To = color,
			Duration = (duration ?? TimeSpan.FromMilliseconds(120L)),
			Easing = (easing ?? CubicEaseOut.Shared)
		}.RunFireAndForget(new WpfAnimatable((DependencyObject)(object)this, IconBrushProperty));
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		SvgIconModel svgIconModel = _GetModel();
		Size result = ((svgIconModel == null) ? new Size(24.0, 24.0) : new Size(svgIconModel.Width, svgIconModel.Height));
		if (double.IsInfinity(availableSize.Width) && double.IsInfinity(availableSize.Height))
		{
			return result;
		}
		if (double.IsInfinity(availableSize.Width))
		{
			return new Size(result.Width * availableSize.Height / result.Height, availableSize.Height);
		}
		if (double.IsInfinity(availableSize.Height))
		{
			return new Size(availableSize.Width, result.Height * availableSize.Width / result.Width);
		}
		return availableSize;
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		base.OnRender(drawingContext);
		SvgIconModel svgIconModel = _GetModel();
		if (svgIconModel == null || svgIconModel.Elements.Count == 0)
		{
			return;
		}
		Size renderSize = RenderSize;
		if (renderSize.Width <= 0.0)
		{
			return;
		}
		renderSize = RenderSize;
		if (renderSize.Height <= 0.0)
		{
			return;
		}
		Rect val = _CalculateTargetRect(new Size(svgIconModel.Width, svgIconModel.Height), RenderSize, Stretch);
		if (val.Width <= 0.0 || val.Height <= 0.0)
		{
			return;
		}
		double scaleX = val.Width / svgIconModel.Width;
		double scaleY = val.Height / svgIconModel.Height;
		drawingContext.PushTransform(new TranslateTransform(val.X, val.Y));
		drawingContext.PushTransform(new ScaleTransform(scaleX, scaleY));
		drawingContext.PushTransform(new TranslateTransform(0.0 - svgIconModel.MinX, 0.0 - svgIconModel.MinY));
		SvgIconPaintOptions options = new SvgIconPaintOptions(IconBrush, StrokeThickness, UseOriginalColor);
		foreach (SvgIconElement element in svgIconModel.Elements)
		{
			element.Draw(drawingContext, options);
		}
		drawingContext.Pop();
		drawingContext.Pop();
		drawingContext.Pop();
	}

	private SvgIconModel? _GetModel()
	{
		if (_modelLoaded)
		{
			return _model;
		}
		_model = SvgIconLoader.Load(Icon, DefaultPack);
		_modelLoaded = true;
		return _model;
	}

	private static void _OnIconChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		SvgIcon svgIcon = (SvgIcon)(object)dependencyObject;
		svgIcon._model = null;
		svgIcon._modelLoaded = false;
	}

	private void _EnsureAnimatableIconBrush()
	{
		Brush iconBrush = IconBrush;
		if (!(iconBrush is SolidColorBrush) || ((Freezable)iconBrush).IsFrozen)
		{
			SolidColorBrush iconBrush2 = ((!(IconBrush is SolidColorBrush solidColorBrush)) ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(solidColorBrush.Color));
			IconBrush = iconBrush2;
		}
	}

	private static Rect _CalculateTargetRect(Size sourceSize, Size renderSize, Stretch stretch)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		if (sourceSize.Width <= 0.0 || sourceSize.Height <= 0.0)
		{
			sourceSize = new Size(24.0, 24.0);
		}
		if (stretch == Stretch.None)
		{
			double num = (renderSize.Width - sourceSize.Width) / 2.0;
			double num2 = (renderSize.Height - sourceSize.Height) / 2.0;
			return new Rect(num, num2, sourceSize.Width, sourceSize.Height);
		}
		double val = renderSize.Width / sourceSize.Width;
		double val2 = renderSize.Height / sourceSize.Height;
		double num3 = stretch switch
		{
			Stretch.Fill => double.NaN, 
			Stretch.UniformToFill => Math.Max(val, val2), 
			_ => Math.Min(val, val2), 
		};
		double num4 = ((stretch == Stretch.Fill) ? renderSize.Width : (sourceSize.Width * num3));
		double num5 = ((stretch == Stretch.Fill) ? renderSize.Height : (sourceSize.Height * num3));
		double num6 = (renderSize.Width - num4) / 2.0;
		double num7 = (renderSize.Height - num5) / 2.0;
		return new Rect(num6, num7, num4, num5);
	}

	static SvgIcon()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected Obj, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected Obj, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected Obj, but got Unknown
	}
}
