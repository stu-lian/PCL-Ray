using System;
using System.Windows;
using System.Windows.Media;

namespace PCL.Core.UI.Controls.SvgIcon;

internal sealed class SvgIconElement
{
	public required SvgIconElementKind Kind { get; init; }

	public required Geometry Geometry { get; init; }

	public required SvgIconStyle Style { get; init; }

	public bool PreferStrokeByDefault
	{
		get
		{
			SvgIconElementKind kind = Kind;
			if (kind == SvgIconElementKind.Line || kind == SvgIconElementKind.Polyline)
			{
				return true;
			}
			return false;
		}
	}

	public void Draw(DrawingContext context, SvgIconPaintOptions options)
	{
		if (!(Style.Opacity <= 0.0))
		{
			Brush brush = _ResolveFill(options);
			Pen pen = _ResolvePen(options);
			if (brush != null || pen != null)
			{
				context.DrawGeometry(brush, pen, Geometry);
			}
		}
	}

	private Brush? _ResolveFill(SvgIconPaintOptions options)
	{
		bool flag = _HasPaint(Style.Fill);
		bool flag2 = _HasPaint(Style.Stroke);
		bool flag3 = _IsNone(Style.Fill);
		Brush brush;
		if (!options.UseOriginalColor)
		{
			if (flag3)
			{
				return null;
			}
			if (!flag && (flag2 || PreferStrokeByDefault))
			{
				return null;
			}
			brush = options.IconBrush;
		}
		else
		{
			if (flag3)
			{
				return null;
			}
			if (flag)
			{
				brush = SvgPaintParser.ParseBrush(Style.Fill, options.IconBrush);
			}
			else
			{
				if (flag2 || PreferStrokeByDefault)
				{
					return null;
				}
				brush = Brushes.Black;
			}
		}
		return _ApplyOpacity(brush, Style.Opacity * Style.FillOpacity);
	}

	private Pen? _ResolvePen(SvgIconPaintOptions options)
	{
		bool flag = _HasPaint(Style.Stroke);
		bool flag2 = _IsNone(Style.Stroke);
		Brush brush;
		if (!options.UseOriginalColor)
		{
			if (flag2)
			{
				return null;
			}
			if (!flag && !PreferStrokeByDefault)
			{
				return null;
			}
			brush = options.IconBrush;
		}
		else
		{
			if (flag2)
			{
				return null;
			}
			if (flag)
			{
				brush = SvgPaintParser.ParseBrush(Style.Stroke, options.IconBrush);
			}
			else
			{
				if (!PreferStrokeByDefault)
				{
					return null;
				}
				brush = Brushes.Black;
			}
		}
		return _CreatePen(_ApplyOpacity(brush, Style.Opacity * Style.StrokeOpacity), Style.StrokeWidth ?? options.StrokeThickness);
	}

	private Pen? _CreatePen(Brush? brush, double thickness)
	{
		if (brush == null || thickness <= 0.0)
		{
			return null;
		}
		return new Pen(brush, thickness)
		{
			StartLineCap = _ParseLineCap(Style.StrokeLineCap),
			EndLineCap = _ParseLineCap(Style.StrokeLineCap),
			LineJoin = _ParseLineJoin(Style.StrokeLineJoin)
		};
	}

	private static Brush? _ApplyOpacity(Brush? brush, double opacity)
	{
		if (brush == null)
		{
			return null;
		}
		opacity = Math.Clamp(opacity, 0.0, 1.0);
		if (opacity <= 0.0)
		{
			return null;
		}
		if (Math.Abs(opacity - 1.0) < 0.0001)
		{
			return brush;
		}
		Brush brush2 = brush.CloneCurrentValue();
		brush2.Opacity *= opacity;
		if (((Freezable)brush2).CanFreeze)
		{
			((Freezable)brush2).Freeze();
		}
		return brush2;
	}

	private static bool _HasPaint(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return !_IsNone(value);
		}
		return false;
	}

	private static bool _IsNone(string? value)
	{
		return string.Equals(value?.Trim(), "none", StringComparison.OrdinalIgnoreCase);
	}

	private static PenLineCap _ParseLineCap(string? value)
	{
		return value?.Trim().ToLowerInvariant() switch
		{
			"butt" => PenLineCap.Flat, 
			"square" => PenLineCap.Square, 
			"round" => PenLineCap.Round, 
			_ => PenLineCap.Round, 
		};
	}

	private static PenLineJoin _ParseLineJoin(string? value)
	{
		return value?.Trim().ToLowerInvariant() switch
		{
			"miter" => PenLineJoin.Miter, 
			"bevel" => PenLineJoin.Bevel, 
			"round" => PenLineJoin.Round, 
			_ => PenLineJoin.Round, 
		};
	}
}
