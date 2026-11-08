using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PCL.Core.App.Configuration;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Localization;

[LifecycleScope("localization", "本地化", false)]
[LifecycleService(LifecycleState.Loaded, Priority = 114514)]
public sealed class LocalizationService : ILifecycleService
{
	public const string Auto = "auto";

	public const string FormatCultureFollowLanguage = "ui-language";

	public const string DefaultLanguageCode = "zh-CN";

	private const string AssemblyResourcePrefix = "/PCL.Core;component/App/Localization/Languages/";

	private static readonly LocalizationLanguage _DefaultLanguage = new LocalizationLanguage("zh-CN", "简体中文（中国大陆）", "zh-CN", LocalizationFontProfile.SimplifiedChinese);

	private static ResourceDictionary? _baseLanguageDictionary;

	private static ResourceDictionary? _currentLanguageDictionary;

	private static CultureInfo _systemFormatCulture = CultureInfo.CurrentCulture;

	private static CultureInfo _systemUiCulture = CultureInfo.CurrentUICulture;

	public static LocalizationLanguage CurrentLanguage { get; private set; } = _DefaultLanguage;

	public static CultureInfo CurrentFormatCulture { get; private set; } = CultureInfo.CurrentCulture;

	public static IReadOnlyList<LocalizationLanguage> SupportedLanguages { get; } = new _003C_003Ez__ReadOnlyArray<LocalizationLanguage>(new LocalizationLanguage[7]
	{
		_DefaultLanguage,
		new LocalizationLanguage("zh-TW", "繁體中文（台灣）", "zh-TW", LocalizationFontProfile.TraditionalChinese),
		new LocalizationLanguage("en-US", "English (US)", "en-US", LocalizationFontProfile.English),
		new LocalizationLanguage("en-GB", "English (United Kingdom)", "en-GB", LocalizationFontProfile.English),
		new LocalizationLanguage("ja-JP", "日本語（日本）", "ja-JP", LocalizationFontProfile.Japanese),
		new LocalizationLanguage("fr-FR", "Français (France)", "fr-FR", LocalizationFontProfile.Other),
		new LocalizationLanguage("es-ES", "Español (España)", "es-ES", LocalizationFontProfile.Other)
	});

	[RegisterConfigEvent]
	public static ConfigEventRegistry OnLanguageConfigChanged => new ConfigEventRegistry(new _003C_003Ez__ReadOnlyArray<IConfigScope>(new IConfigScope[2]
	{
		Config.Preference.Localization.LanguageConfig,
		Config.Preference.Localization.FormatCultureConfig
	}), (ConfigEventArgs _) =>
	{
		ApplyFromConfig();
	}, ConfigEvent.Update);

	public string Identifier => "localization";

	public string Name => "本地化";

	public bool SupportAsync => false;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	} = null;

	private static ILifecycleService Service => Context.ServiceInstance;

	public static event Action? LanguageChanged;

	[LifecycleStart]
	private static void _Start()
	{
		_systemFormatCulture = CultureInfo.CurrentCulture;
		_systemUiCulture = CultureInfo.CurrentUICulture;
		ApplyFromConfig();
	}

	public static void ApplyFromConfig(bool save = false)
	{
		if (!ConfigService.IsInitialized)
		{
			Apply("auto", "auto", save: false);
		}
		else
		{
			Apply(Config.Preference.Localization.Language, Config.Preference.Localization.FormatCulture, save);
		}
	}

	public static void Apply(string languageCode, string formatCultureCode = "auto", bool save = true)
	{
		string text = _NormalizeConfigValue(languageCode);
		LocalizationLanguage localizationLanguage = ResolveLanguage(text);
		CultureInfo cultureInfo = CultureInfo.GetCultureInfo(localizationLanguage.CultureName);
		CultureInfo cultureInfo2 = _ResolveFormatCulture(formatCultureCode, cultureInfo, out string normalizedCode);
		bool flag = !string.Equals(CurrentLanguage.Code, localizationLanguage.Code, StringComparison.OrdinalIgnoreCase);
		bool flag2 = !string.Equals(CurrentFormatCulture.Name, cultureInfo2.Name, StringComparison.OrdinalIgnoreCase);
		if (_baseLanguageDictionary != null && !flag && !flag2)
		{
			_SaveConfigIfNeeded(save, text, localizationLanguage, normalizedCode);
			return;
		}
		_ApplyCultures(cultureInfo, cultureInfo2);
		_ApplyLanguageResources(localizationLanguage.Code, cultureInfo, cultureInfo2);
		CurrentLanguage = localizationLanguage;
		LocalizationFontService.ApplyLaunchFont(ConfigService.IsInitialized ? Config.Preference.Font : null, localizationLanguage);
		CurrentFormatCulture = cultureInfo2;
		_SaveConfigIfNeeded(save, text, localizationLanguage, normalizedCode);
		_LogInfo("当前 UI 语言: " + localizationLanguage.Code + ", 展示格式: " + cultureInfo2.Name);
		LanguageChanged?.Invoke();
	}

	public static bool IsLanguageSupported(string? languageCode)
	{
		if (string.IsNullOrWhiteSpace(languageCode))
		{
			return true;
		}
		if (string.Equals(languageCode, "auto", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string normalizedCode = _NormalizeCultureCode(languageCode);
		return SupportedLanguages.Any((LocalizationLanguage language) => string.Equals(language.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));
	}

	public static LocalizationLanguage ResolveLanguage(string? languageCode)
	{
		if (string.IsNullOrWhiteSpace(languageCode) || string.Equals(languageCode, "auto", StringComparison.OrdinalIgnoreCase))
		{
			return _ResolveSystemLanguage();
		}
		string normalizedCode = _NormalizeCultureCode(languageCode);
		return SupportedLanguages.FirstOrDefault((LocalizationLanguage language) => string.Equals(language.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)) ?? _DefaultLanguage;
	}

	private static void _SaveConfigIfNeeded(bool save, string normalizedLanguageCode, LocalizationLanguage language, string normalizedFormatCultureCode)
	{
		if (save && ConfigService.IsInitialized)
		{
			string text = (string.Equals(normalizedLanguageCode, "auto", StringComparison.OrdinalIgnoreCase) ? "auto" : language.Code);
			if (Config.Preference.Localization.Language != text)
			{
				Config.Preference.Localization.Language = text;
			}
			if (Config.Preference.Localization.FormatCulture != normalizedFormatCultureCode)
			{
				Config.Preference.Localization.FormatCulture = normalizedFormatCultureCode;
			}
		}
	}

	private static LocalizationLanguage _ResolveSystemLanguage()
	{
		string systemLanguage = _NormalizeCultureCode(_systemUiCulture.Name);
		LocalizationLanguage localizationLanguage = SupportedLanguages.FirstOrDefault((LocalizationLanguage language) => string.Equals(language.Code, systemLanguage, StringComparison.OrdinalIgnoreCase));
		if ((object)localizationLanguage != null)
		{
			return localizationLanguage;
		}
		string neutral = _systemUiCulture.TwoLetterISOLanguageName;
		return SupportedLanguages.FirstOrDefault((LocalizationLanguage language) => language.Code.StartsWith(neutral + "-", StringComparison.OrdinalIgnoreCase)) ?? _DefaultLanguage;
	}

	private static CultureInfo _ResolveFormatCulture(string? formatCultureCode, CultureInfo uiCulture, out string normalizedCode)
	{
		if (string.IsNullOrWhiteSpace(formatCultureCode) || string.Equals(formatCultureCode, "auto", StringComparison.OrdinalIgnoreCase))
		{
			normalizedCode = "auto";
			return _systemFormatCulture;
		}
		if (string.Equals(formatCultureCode, "ui-language", StringComparison.OrdinalIgnoreCase))
		{
			normalizedCode = "ui-language";
			return uiCulture;
		}
		try
		{
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo(formatCultureCode);
			normalizedCode = cultureInfo.Name;
			return cultureInfo;
		}
		catch (CultureNotFoundException)
		{
			_LogWarn("无法识别展示格式区域性: " + formatCultureCode + "，已回退系统区域格式");
			normalizedCode = "auto";
			return _systemFormatCulture;
		}
	}

	private static void _ApplyCultures(CultureInfo uiCulture, CultureInfo formatCulture)
	{
		CultureInfo.CurrentUICulture = uiCulture;
		CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
		Thread.CurrentThread.CurrentUICulture = uiCulture;
		CultureInfo.CurrentCulture = formatCulture;
		CultureInfo.DefaultThreadCurrentCulture = formatCulture;
		Thread.CurrentThread.CurrentCulture = formatCulture;
		Lang.SyncCulture(formatCulture);
	}

	private static void _ApplyLanguageResources(string languageCode, CultureInfo uiCulture, CultureInfo formatCulture)
	{
		Application app = Application.Current ?? Lifecycle.CurrentApplication;
		if (!((DispatcherObject)app).Dispatcher.CheckAccess())
		{
			((DispatcherObject)app).Dispatcher.Invoke((Action)(() =>
			{
				_ApplyCultures(uiCulture, formatCulture);
				_ApplyLanguageResourcesCore(app, languageCode);
			}));
		}
		else
		{
			_ApplyLanguageResourcesCore(app, languageCode);
		}
	}

	private static void _ApplyLanguageResourcesCore(Application app, string languageCode)
	{
		Collection<ResourceDictionary> mergedDictionaries = app.Resources.MergedDictionaries;
		if (_baseLanguageDictionary != null)
		{
			mergedDictionaries.Remove(_baseLanguageDictionary);
		}
		if (_currentLanguageDictionary != null)
		{
			mergedDictionaries.Remove(_currentLanguageDictionary);
		}
		_baseLanguageDictionary = _LoadLanguageDictionary("zh-CN");
		mergedDictionaries.Add(_baseLanguageDictionary);
		if (string.Equals(languageCode, "zh-CN", StringComparison.OrdinalIgnoreCase))
		{
			_currentLanguageDictionary = null;
			return;
		}
		_currentLanguageDictionary = _LoadLanguageDictionary(languageCode);
		mergedDictionaries.Add(_currentLanguageDictionary);
	}

	private static ResourceDictionary _LoadLanguageDictionary(string languageCode)
	{
		return new ResourceDictionary
		{
			Source = new Uri("/PCL.Core;component/App/Localization/Languages/" + languageCode + ".xaml", UriKind.Relative)
		};
	}

	private static string _NormalizeConfigValue(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return _NormalizeCultureCode(value);
		}
		return "auto";
	}

	private static string _NormalizeCultureCode(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value.Replace('_', '-').Trim();
		}
		return "auto";
	}

	private static void _LogInfo(string message)
	{
		try
		{
			Context.Info(message);
		}
		catch (InvalidOperationException)
		{
		}
	}

	private static void _LogWarn(string message)
	{
		try
		{
			Context.Warn(message);
		}
		catch (InvalidOperationException)
		{
		}
	}

	public LocalizationService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
	}

	public async Task StartAsync()
	{
		_Start();
		Context.DeclareStopped();
	}
}
