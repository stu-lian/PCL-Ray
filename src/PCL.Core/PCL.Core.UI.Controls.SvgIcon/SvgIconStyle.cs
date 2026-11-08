using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace PCL.Core.UI.Controls.SvgIcon;

internal sealed record SvgIconStyle
{
	public string? Fill { get; init; }

	public string? Stroke { get; init; }

	public string? StrokeLineCap { get; init; }

	public string? StrokeLineJoin { get; init; }

	public string? FillRule { get; init; }

	public double? StrokeWidth { get; init; }

	public double Opacity { get; init; } = 1.0;

	public double FillOpacity { get; init; } = 1.0;

	public double StrokeOpacity { get; init; } = 1.0;

	public SvgIconStyle Merge(XElement element)
	{
		Dictionary<string, string> style = _ParseStyle(element.Attribute("style")?.Value);
		string value = _GetAttributeOrStyle(element, style, "opacity");
		string value2 = _GetAttributeOrStyle(element, style, "fill-opacity");
		string value3 = _GetAttributeOrStyle(element, style, "stroke-opacity");
		return new SvgIconStyle
		{
			Fill = (_GetAttributeOrStyle(element, style, "fill") ?? Fill),
			Stroke = (_GetAttributeOrStyle(element, style, "stroke") ?? Stroke),
			StrokeLineCap = (_GetAttributeOrStyle(element, style, "stroke-linecap") ?? StrokeLineCap),
			StrokeLineJoin = (_GetAttributeOrStyle(element, style, "stroke-linejoin") ?? StrokeLineJoin),
			FillRule = (_GetAttributeOrStyle(element, style, "fill-rule") ?? FillRule),
			StrokeWidth = (SvgNumberParser.TryParseNullable(_GetAttributeOrStyle(element, style, "stroke-width")) ?? StrokeWidth),
			Opacity = Opacity * SvgNumberParser.TryParse(value, 1.0),
			FillOpacity = (SvgNumberParser.TryParseNullable(value2) ?? FillOpacity),
			StrokeOpacity = (SvgNumberParser.TryParseNullable(value3) ?? StrokeOpacity)
		};
	}

	private static string? _GetAttributeOrStyle(XElement element, IReadOnlyDictionary<string, string> style, string name)
	{
		string text = element.Attribute(name)?.Value;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		if (!style.TryGetValue(name, out string value) || string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		return value.Trim();
	}

	private static Dictionary<string, string> _ParseStyle(string? value)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(value))
		{
			return dictionary;
		}
		string[] array = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(':', 2, StringSplitOptions.TrimEntries);
			if (array2 != null && array2.Length == 2)
			{
				string text = array2[0];
				if (text != null && text.Length > 0)
				{
					dictionary[array2[0]] = array2[1];
				}
			}
		}
		return dictionary;
	}
}
