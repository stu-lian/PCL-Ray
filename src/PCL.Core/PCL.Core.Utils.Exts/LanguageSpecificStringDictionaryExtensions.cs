using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL.Core.Utils.Exts;

public static class LanguageSpecificStringDictionaryExtensions
{
	public static string GetForCurrentUiCulture(this LanguageSpecificStringDictionary dict, string? fallback = null)
	{
		CultureInfo currentUICulture = CultureInfo.CurrentUICulture;
		if (TryFromTag(dict, currentUICulture.IetfLanguageTag, out var value))
		{
			return value;
		}
		string text = currentUICulture.IetfLanguageTag;
		for (int num = text.LastIndexOf('-'); num > 0; num = text.LastIndexOf('-'))
		{
			text = text.Substring(0, num);
			if (TryFromTag(dict, text, out value))
			{
				return value;
			}
		}
		if (!currentUICulture.IsNeutralCulture && TryFromTag(dict, currentUICulture.Parent.IetfLanguageTag, out value))
		{
			return value;
		}
		if (dict.Count > 0)
		{
			return dict.Values.First();
		}
		return fallback ?? string.Empty;
		static bool TryFromTag(LanguageSpecificStringDictionary d, string ietf, out string value2)
		{
			return d.TryGetValue(XmlLanguage.GetLanguage(ietf), out value2);
		}
	}
}
