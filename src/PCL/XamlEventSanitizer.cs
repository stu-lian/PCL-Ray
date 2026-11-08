using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace PCL;

public static class XamlEventSanitizer
{
	public class SanitizeResult
	{
		public string SanitizedXaml { get; set; } = "";

		public List<string> UnsupportedTypesFound { get; set; } = new List<string>();

		public List<string> UnrecognizedTypes { get; set; } = new List<string>();
	}

	private static Regex EventTypeAttributeRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__EventTypeAttributeRegex_0.Instance;
	}

	private static Regex EventTypePropertyElementRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__EventTypePropertyElementRegex_1.Instance;
	}

	private static Regex CustomEventTypePropertyElementRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__CustomEventTypePropertyElementRegex_2.Instance;
	}

	private static Regex LocalCustomEventTypeAttributeRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventTypeAttributeRegex_3.Instance;
	}

	private static Regex LocalCustomEventSelfClosingRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventSelfClosingRegex_4.Instance;
	}

	private static Regex LocalCustomEventOpenTagRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__LocalCustomEventOpenTagRegex_5.Instance;
	}

	private static Regex SetterEventTypePropertyFirstRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__SetterEventTypePropertyFirstRegex_6.Instance;
	}

	private static Regex SetterEventTypeValueFirstRegex()
	{
		return _003CRegexGenerator_g_003EF2B7BE1F3330BFC5DAA272F5C549A19C58ADE43553DD06A701030F889B806E6A4__SetterEventTypeValueFirstRegex_7.Instance;
	}

	public static SanitizeResult Sanitize(string xaml)
	{
		SanitizeResult result = new SanitizeResult();
		xaml = EventTypeAttributeRegex().Replace(xaml, (Match match) =>
		{
			string value = match.Groups[2].Value;
			return ReplaceEventType(match, value, result);
		});
		xaml = EventTypePropertyElementRegex().Replace(xaml, (Match match) =>
		{
			string rawValue = match.Groups[2].Value.Trim();
			return ReplaceEventType(match, rawValue, result);
		});
		xaml = CustomEventTypePropertyElementRegex().Replace(xaml, (Match match) =>
		{
			string rawValue = match.Groups[2].Value.Trim();
			return ReplaceOrRemovePropertyElement(match, rawValue, result);
		});
		xaml = LocalCustomEventTypeAttributeRegex().Replace(xaml, (Match match) =>
		{
			string value = match.Groups[2].Value;
			return ReplaceEventType(match, value, result);
		});
		xaml = LocalCustomEventSelfClosingRegex().Replace(xaml, (Match match) =>
		{
			string value = match.Groups[1].Value;
			return _RemoveBadCustomEventElement(match, value, result);
		});
		xaml = LocalCustomEventOpenTagRegex().Replace(xaml, (Match match) =>
		{
			if (_IsBadType(match.Groups[1].Value, result))
			{
				string elementName = "local:CustomEvent";
				string text = xaml.Substring(match.Index + match.Length);
				int num = FindMatchingCloseTag(text, elementName);
				if (num < 0)
				{
					return match.Value;
				}
				return text.Substring(num);
			}
			return match.Value;
		});
		xaml = SetterEventTypePropertyFirstRegex().Replace(xaml, (Match match) => _ReplaceSetter(match, match.Groups[1].Value, result));
		xaml = SetterEventTypeValueFirstRegex().Replace(xaml, (Match match) => _ReplaceSetter(match, match.Groups[1].Value, result));
		RemoveElementsForTypes(result.UnsupportedTypesFound, ref xaml);
		RemoveElementsForTypes(result.UnrecognizedTypes, ref xaml);
		result.UnsupportedTypesFound = result.UnsupportedTypesFound.Distinct().ToList();
		result.UnrecognizedTypes = result.UnrecognizedTypes.Distinct().ToList();
		result.SanitizedXaml = xaml;
		return result;
	}

	private static bool _IsBadType(string rawValue, SanitizeResult result)
	{
		if (EventTypeMapper.IsUnsupportedType(rawValue))
		{
			result.UnsupportedTypesFound.Add(rawValue);
			return true;
		}
		if (!Enum.TryParse<EventType>(rawValue, ignoreCase: true, out var _) && !EventTypeMapper.TryToEnglish(rawValue, out string _))
		{
			result.UnrecognizedTypes.Add(rawValue);
			return true;
		}
		return false;
	}

	private static string _RemoveBadCustomEventElement(Match match, string rawValue, SanitizeResult result)
	{
		if (!_IsBadType(rawValue, result))
		{
			return match.Value;
		}
		return "";
	}

	private static void RemoveElementsForTypes(List<string> types, ref string sanitized)
	{
		foreach (string item in types.ToList())
		{
			sanitized = RemoveElementsWithEventType(sanitized, item, types);
		}
	}

	private static string ReplaceEventType(Match match, string rawValue, SanitizeResult result)
	{
		if (EventTypeMapper.TryToEnglish(rawValue, out string englishName))
		{
			return match.Groups[1].Value + englishName + match.Groups[3].Value;
		}
		if (Enum.TryParse<EventType>(rawValue, ignoreCase: true, out var _))
		{
			return match.Value;
		}
		if (EventTypeMapper.IsUnsupportedType(rawValue))
		{
			result.UnsupportedTypesFound.Add(rawValue);
			return match.Value;
		}
		result.UnrecognizedTypes.Add(rawValue);
		return match.Value;
	}

	private static string ReplaceOrRemovePropertyElement(Match match, string rawValue, SanitizeResult result)
	{
		if (_IsBadType(rawValue, result))
		{
			return "";
		}
		return ReplaceEventType(match, rawValue, result);
	}

	private static string _ReplaceSetter(Match match, string rawValue, SanitizeResult result)
	{
		if (_IsBadType(rawValue, result))
		{
			return "";
		}
		if (EventTypeMapper.TryToEnglish(rawValue, out string englishName))
		{
			return match.Value.Replace("Value=\"" + rawValue + "\"", "Value=\"" + englishName + "\"");
		}
		return match.Value;
	}

	private static string RemoveElementsWithEventType(string xaml, string eventTypeValue, List<string> trackingList)
	{
		string text = Regex.Escape(eventTypeValue);
		string pattern = "<[\\w:]+[^>]*\\s+local:CustomEventService\\.EventType\\s*=\\s*\"" + text + "\"[^>]*/\\s*>";
		xaml = Regex.Replace(xaml, pattern, (Match match) =>
		{
			trackingList.Add(eventTypeValue);
			return "";
		}, RegexOptions.Compiled);
		string pattern2 = "<[\\w:]+[^>]*\\s+local:CustomEventService\\.EventType\\s*=\\s*\"" + text + "\"[^>]*>";
		xaml = Regex.Replace(xaml, pattern2, (Match match) =>
		{
			string value = Regex.Match(match.Value, "<([\\w:]+)").Groups[1].Value;
			string text2 = xaml.Substring(match.Index + match.Length);
			int num = FindMatchingCloseTag(text2, value);
			if (num < 0)
			{
				return match.Value;
			}
			trackingList.Add(eventTypeValue);
			return text2.Substring(num);
		}, RegexOptions.Compiled);
		string pattern3 = "<local:CustomEventService\\.EventType\\s*>\\s*" + text + "\\s*</local:CustomEventService\\.EventType\\s*>";
		xaml = Regex.Replace(xaml, pattern3, (Match match) =>
		{
			Match match2 = Regex.Match(xaml.Substring(0, match.Index), "<([\\w:]+)[^>]*>\\s*$", RegexOptions.RightToLeft);
			if (!match2.Success)
			{
				return match.Value;
			}
			string value = match2.Groups[1].Value;
			if (!Regex.Match(xaml.Substring(match.Index + match.Length), "</" + value + "\\s*>").Success)
			{
				return match.Value;
			}
			trackingList.Add(eventTypeValue);
			return "";
		}, RegexOptions.Compiled);
		return xaml;
	}

	private static int FindMatchingCloseTag(string text, string elementName)
	{
		string pattern = "</" + elementName + "\\s*>";
		Match match = Regex.Match(text, pattern);
		if (!match.Success)
		{
			return -1;
		}
		return match.Index + match.Length;
	}
}
