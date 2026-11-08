using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Localization;

public static class LocalizationFontService
{
	private const string PclEnglishFont = "./Resources/#PCL English";

	private static readonly Uri _ApplicationPackUri = new Uri("pack://application:,,,/");

	private static readonly IReadOnlyDictionary<string, LocalizationFontProfile> _ExactCultureProfiles = new Dictionary<string, LocalizationFontProfile>(StringComparer.OrdinalIgnoreCase)
	{
		["zh-CN"] = LocalizationFontProfile.SimplifiedChinese,
		["zh-SG"] = LocalizationFontProfile.SimplifiedChinese,
		["zh-TW"] = LocalizationFontProfile.TraditionalChinese,
		["zh-HK"] = LocalizationFontProfile.TraditionalChinese,
		["zh-MO"] = LocalizationFontProfile.TraditionalChinese
	};

	private static readonly IReadOnlyDictionary<string, LocalizationFontProfile> _ScriptProfiles = new Dictionary<string, LocalizationFontProfile>(StringComparer.OrdinalIgnoreCase)
	{
		["Hans"] = LocalizationFontProfile.SimplifiedChinese,
		["Hant"] = LocalizationFontProfile.TraditionalChinese
	};

	private static readonly IReadOnlyDictionary<string, LocalizationFontProfile> _LanguageProfiles = new Dictionary<string, LocalizationFontProfile>(StringComparer.OrdinalIgnoreCase)
	{
		["zh"] = LocalizationFontProfile.SimplifiedChinese,
		["ja"] = LocalizationFontProfile.Japanese,
		["ko"] = LocalizationFontProfile.Korean,
		["en"] = LocalizationFontProfile.English
	};

	public static FontFamily BuildLaunchFontFamily(string? customFontName = null, LocalizationLanguage? language = null)
	{
		if ((object)language == null)
		{
			language = LocalizationService.CurrentLanguage;
		}
		IReadOnlyList<string> values = (string.IsNullOrWhiteSpace(customFontName) ? _GetDefaultFamilyNames(language.FontProfile) : _GetCustomFamilyNames(customFontName, language.FontProfile));
		return new FontFamily(_ApplicationPackUri, string.Join(", ", values));
	}

	public static FontFamily BuildRepresentativeFontFamily(LocalizationLanguage language)
	{
		return BuildRepresentativeFontFamily(language.FontProfile);
	}

	public static FontFamily BuildRepresentativeFontFamily(LocalizationFontProfile profile)
	{
		return new FontFamily(_ApplicationPackUri, string.Join(", ", _GetDefaultFamilyNames(profile)));
	}

	public static void ApplyLaunchFont(string? customFontName = null, LocalizationLanguage? language = null)
	{
		Application app = Application.Current ?? Lifecycle.CurrentApplication;
		FontFamily fontFamily = BuildLaunchFontFamily(customFontName, language);
		if (!((DispatcherObject)app).Dispatcher.CheckAccess())
		{
			((DispatcherObject)app).Dispatcher.Invoke<object>((Func<object>)(() => app.Resources["LaunchFontFamily"] = fontFamily));
		}
		else
		{
			app.Resources["LaunchFontFamily"] = fontFamily;
		}
	}

	public static LocalizationFontProfile ResolveProfileFromCultureName(string? cultureName)
	{
		string text = _NormalizeCultureCode(cultureName);
		if (string.IsNullOrEmpty(text))
		{
			return LocalizationFontProfile.Other;
		}
		if (_ExactCultureProfiles.TryGetValue(text, out var value))
		{
			return value;
		}
		string[] source = text.Split('-', StringSplitOptions.RemoveEmptyEntries);
		foreach (string item in source.Skip(1))
		{
			if (_ScriptProfiles.TryGetValue(item, out var value2))
			{
				return value2;
			}
		}
		string text2 = source.FirstOrDefault();
		if (text2 == null || !_LanguageProfiles.TryGetValue(text2, out var value3))
		{
			return LocalizationFontProfile.Other;
		}
		return value3;
	}

	private static string _NormalizeCultureCode(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Replace('_', '-').Trim();
		}
		return string.Empty;
	}

	private static IReadOnlyList<string> _GetDefaultFamilyNames(LocalizationFontProfile profile)
	{
		return profile switch
		{
			LocalizationFontProfile.English => (IReadOnlyList<string>)new _003C_003Ez__ReadOnlyArray<string>(new string[3] { "./Resources/#PCL English", "Segoe UI", "Microsoft YaHei UI" }), 
			LocalizationFontProfile.SimplifiedChinese => new _003C_003Ez__ReadOnlyArray<string>(new string[3] { "./Resources/#PCL English", "Microsoft YaHei UI", "Segoe UI" }), 
			LocalizationFontProfile.TraditionalChinese => new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "./Resources/#PCL English", "Microsoft JhengHei UI", "Microsoft YaHei UI", "Segoe UI" }), 
			LocalizationFontProfile.Japanese => new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "./Resources/#PCL English", "Yu Gothic UI", "Microsoft YaHei UI", "Segoe UI" }), 
			LocalizationFontProfile.Korean => new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "./Resources/#PCL English", "Malgun Gothic", "Microsoft YaHei UI", "Segoe UI" }), 
			_ => new _003C_003Ez__ReadOnlyArray<string>(new string[2] { "Segoe UI", "Microsoft YaHei UI" }), 
		};
	}

	private static IReadOnlyList<string> _GetCustomFamilyNames(string customFontName, LocalizationFontProfile profile)
	{
		List<string> list = (from name in _GetDefaultFamilyNames(profile)
			where !string.Equals(name, "./Resources/#PCL English", StringComparison.OrdinalIgnoreCase)
			select name).ToList();
		list.Insert(0, _EscapeFontFamilyName(customFontName));
		return list.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private static string _EscapeFontFamilyName(string name)
	{
		return name.Replace(",", ",,");
	}
}
