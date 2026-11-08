using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace PCL.Core.UI.Controls.SvgIcon;

internal static class SvgIconParser
{
	public static SvgIconModel Parse(string svg)
	{
		using StringReader input = new StringReader(svg);
		using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null
		});
		XElement? xElement = XDocument.Load(reader, LoadOptions.None).Root ?? throw new FormatException("SVG 文件缺少根节点。");
		(double MinX, double MinY, double Width, double Height) tuple = _ReadViewBox(xElement);
		double item = tuple.MinX;
		double item2 = tuple.MinY;
		double item3 = tuple.Width;
		double item4 = tuple.Height;
		List<SvgIconElement> list = new List<SvgIconElement>();
		_ReadElements(xElement, list, new SvgIconStyle());
		return new SvgIconModel
		{
			MinX = item,
			MinY = item2,
			Width = ((item3 <= 0.0) ? 24.0 : item3),
			Height = ((item4 <= 0.0) ? 24.0 : item4),
			Elements = list
		};
	}

	private static void _ReadElements(XElement parent, ICollection<SvgIconElement> result, SvgIconStyle inheritedStyle)
	{
		SvgIconStyle svgIconStyle = inheritedStyle.Merge(parent);
		foreach (XElement item in parent.Elements())
		{
			string localName = item.Name.LocalName;
			if ((localName == "g" || localName == "svg") ? true : false)
			{
				_ReadElements(item, result, svgIconStyle);
				continue;
			}
			SvgIconStyle style = svgIconStyle.Merge(item);
			SvgIconElement svgIconElement = _CreateElement(localName, item, style);
			if (svgIconElement != null)
			{
				result.Add(svgIconElement);
			}
		}
	}

	private static SvgIconElement? _CreateElement(string name, XElement element, SvgIconStyle style)
	{
		try
		{
			return name switch
			{
				"path" => _CreatePath(element, style), 
				"line" => _CreateLine(element, style), 
				"circle" => _CreateCircle(element, style), 
				"ellipse" => _CreateEllipse(element, style), 
				"rect" => _CreateRect(element, style), 
				"polyline" => _CreatePolyline(element, style), 
				"polygon" => _CreatePolygon(element, style), 
				_ => null, 
			};
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static SvgIconElement? _CreatePath(XElement element, SvgIconStyle style)
	{
		string text = _Attr(element, "d");
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Geometry geometry = SvgPathGeometryParser.Parse(text);
		_ApplyFillRule(geometry, style);
		_TryFreeze((Freezable)(object)geometry);
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Path,
			Geometry = geometry,
			Style = style
		};
	}

	private static SvgIconElement _CreateLine(XElement element, SvgIconStyle style)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		LineGeometry lineGeometry = new LineGeometry(new Point(_Number(element, "x1"), _Number(element, "y1")), new Point(_Number(element, "x2"), _Number(element, "y2")));
		_TryFreeze((Freezable)(object)lineGeometry);
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Line,
			Geometry = lineGeometry,
			Style = style
		};
	}

	private static SvgIconElement _CreateCircle(XElement element, SvgIconStyle style)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		EllipseGeometry ellipseGeometry = new EllipseGeometry(new Point(_Number(element, "cx"), _Number(element, "cy")), _Number(element, "r"), _Number(element, "r"));
		_TryFreeze((Freezable)(object)ellipseGeometry);
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Circle,
			Geometry = ellipseGeometry,
			Style = style
		};
	}

	private static SvgIconElement _CreateEllipse(XElement element, SvgIconStyle style)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		EllipseGeometry ellipseGeometry = new EllipseGeometry(new Point(_Number(element, "cx"), _Number(element, "cy")), _Number(element, "rx"), _Number(element, "ry"));
		_TryFreeze((Freezable)(object)ellipseGeometry);
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Ellipse,
			Geometry = ellipseGeometry,
			Style = style
		};
	}

	private static SvgIconElement _CreateRect(XElement element, SvgIconStyle style)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		double num = _Number(element, "x");
		double num2 = _Number(element, "y");
		double num3 = _Number(element, "width");
		double num4 = _Number(element, "height");
		double num5 = _Number(element, "rx", _Number(element, "ry"));
		RectangleGeometry rectangleGeometry = new RectangleGeometry(radiusY: _Number(element, "ry", num5), rect: new Rect(num, num2, num3, num4), radiusX: num5);
		_TryFreeze((Freezable)(object)rectangleGeometry);
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Rect,
			Geometry = rectangleGeometry,
			Style = style
		};
	}

	private static SvgIconElement? _CreatePolyline(XElement element, SvgIconStyle style)
	{
		StreamGeometry streamGeometry = _CreatePointsGeometry(_Attr(element, "points"), close: false);
		if (streamGeometry == null)
		{
			return null;
		}
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Polyline,
			Geometry = streamGeometry,
			Style = style
		};
	}

	private static SvgIconElement? _CreatePolygon(XElement element, SvgIconStyle style)
	{
		StreamGeometry streamGeometry = _CreatePointsGeometry(_Attr(element, "points"), close: true);
		if (streamGeometry == null)
		{
			return null;
		}
		return new SvgIconElement
		{
			Kind = SvgIconElementKind.Polygon,
			Geometry = streamGeometry,
			Style = style
		};
	}

	private static StreamGeometry? _CreatePointsGeometry(string? points, bool close)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		double[] array = SvgNumberParser.ParseNumberList(points);
		if (array.Length < 4)
		{
			return null;
		}
		StreamGeometry streamGeometry = new StreamGeometry
		{
			FillRule = (close ? FillRule.Nonzero : FillRule.EvenOdd)
		};
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(new Point(array[0], array[1]), close, close);
			for (int i = 2; i + 1 < array.Length; i += 2)
			{
				streamGeometryContext.LineTo(new Point(array[i], array[i + 1]), isStroked: true, isSmoothJoin: false);
			}
		}
		_TryFreeze((Freezable)(object)streamGeometry);
		return streamGeometry;
	}

	private static (double MinX, double MinY, double Width, double Height) _ReadViewBox(XElement root)
	{
		double[] array = SvgNumberParser.ParseNumberList(_Attr(root, "viewBox"));
		if (array.Length == 4)
		{
			return (MinX: array[0], MinY: array[1], Width: array[2], Height: array[3]);
		}
		double item = SvgNumberParser.TryParseNullable(_Attr(root, "width")) ?? 24.0;
		double item2 = SvgNumberParser.TryParseNullable(_Attr(root, "height")) ?? 24.0;
		return (MinX: 0.0, MinY: 0.0, Width: item, Height: item2);
	}

	private static double _Number(XElement element, string name, double fallback = 0.0)
	{
		return SvgNumberParser.TryParse(_Attr(element, name), fallback);
	}

	private static string? _Attr(XElement element, string name)
	{
		return element.Attribute(name)?.Value;
	}

	private static void _ApplyFillRule(Geometry geometry, SvgIconStyle style)
	{
		FillRule fillRule = ((!(style.FillRule?.Trim().ToLowerInvariant() == "evenodd")) ? FillRule.Nonzero : FillRule.EvenOdd);
		FillRule fillRule2 = fillRule;
		if (!(geometry is StreamGeometry streamGeometry))
		{
			if (geometry is PathGeometry pathGeometry)
			{
				pathGeometry.FillRule = fillRule2;
			}
		}
		else
		{
			streamGeometry.FillRule = fillRule2;
		}
	}

	private static void _TryFreeze(Freezable freezable)
	{
		if (freezable.CanFreeze)
		{
			freezable.Freeze();
		}
	}
}
