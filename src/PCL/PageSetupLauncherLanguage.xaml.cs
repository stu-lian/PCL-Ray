using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI.Controls;

namespace PCL;

public partial class PageSetupLauncherLanguage : MyPageRight, IComponentConnector
{
	private bool _isLoaded;


	public PageSetupLauncherLanguage()
	{
		InitializeComponent();
		Loaded += PageSetupLauncherLanguage_Loaded;
	}

	private void PageSetupLauncherLanguage_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		if (!_isLoaded)
		{
			_isLoaded = true;
			ModAnimation.AniControlEnabled++;
			Reload();
			ModAnimation.AniControlEnabled--;
		}
	}

	public void Reload()
	{
		ModAnimation.AniControlEnabled++;
		try
		{
			ReloadLanguageCombo();
			ReloadFormatCultureCombo();
		}
		finally
		{
			ModAnimation.AniControlEnabled--;
		}
	}

	public void Reset()
	{
		try
		{
			Config.Preference.Localization.LanguageConfig.SetDefaultValue((object)null, (bool?)null);
			Config.Preference.Localization.FormatCultureConfig.SetDefaultValue((object)null, (bool?)null);
			LocalizationService.ApplyFromConfig(false);
			ModBase.Log("[Setup] 已初始化启动器-语言页设置");
			HintService.Hint(Lang.Text("Setup.Language.Reset.Success"), HintType.Success, log: false);
			Reload();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "初始化启动器-语言页设置失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Error.OperationFailed"));
		}
	}

	private void ReloadLanguageCombo()
	{
		ComboUiLanguage.Items.Clear();
		LocalizationLanguage language = LocalizationService.ResolveLanguage("auto");
		ComboUiLanguage.Items.Add(CreateLanguageComboItem(Lang.Text("Setup.Language.UiLanguage.Auto", new object[1] { GetLanguageDisplay(language) }), "auto", language));
		foreach (LocalizationLanguage supportedLanguage in LocalizationService.SupportedLanguages)
		{
			ComboUiLanguage.Items.Add(CreateLanguageComboItem(GetLanguageDisplay(supportedLanguage), supportedLanguage.Code, supportedLanguage));
		}
		string text = NormalizeConfigValue(Config.Preference.Localization.Language);
		string tag = "auto";
		if (LocalizationService.IsLanguageSupported(text))
		{
			tag = (string.Equals(text, "auto", StringComparison.OrdinalIgnoreCase) ? "auto" : LocalizationService.ResolveLanguage(text).Code);
		}
		SelectComboItem(ComboUiLanguage, tag);
	}

	private void ReloadFormatCultureCombo()
	{
		ComboUiFormatCulture.Items.Clear();
		ComboUiFormatCulture.Items.Add(new MyComboBoxItem
		{
			Content = Lang.Text("Setup.Language.FormatCulture.Auto"),
			Tag = "auto"
		});
		ComboUiFormatCulture.Items.Add(new MyComboBoxItem
		{
			Content = Lang.Text("Setup.Language.FormatCulture.FollowLanguage"),
			Tag = "ui-language"
		});
		foreach (CultureInfo builtInFormatCulture in GetBuiltInFormatCultures())
		{
			ComboUiFormatCulture.Items.Add(new MyComboBoxItem
			{
				Content = GetCultureDisplay(builtInFormatCulture),
				Tag = builtInFormatCulture.Name
			});
		}
		string text = NormalizeConfigValue(Config.Preference.Localization.FormatCulture);
		if (!IsFormatCultureItemExisting(text) && TryGetCulture(text, out CultureInfo culture))
		{
			ComboUiFormatCulture.Items.Add(new MyComboBoxItem
			{
				Content = GetCultureDisplay(culture),
				Tag = culture.Name
			});
		}
		SelectComboItem(ComboUiFormatCulture, IsFormatCultureItemExisting(text) ? text : "auto");
	}

	private void ComboUiLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0 && ComboUiLanguage.SelectedItem is MyComboBoxItem { Tag: var tag })
		{
			string text = tag?.ToString() ?? "auto";
			if (!(Config.Preference.Localization.Language == text))
			{
				Config.Preference.Localization.Language = text;
				HintService.Hint(Lang.Text("Setup.Language.Changed"), HintType.Success, log: false);
				Reload();
			}
		}
	}

	private void ComboUiFormatCulture_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ModAnimation.AniControlEnabled == 0 && ComboUiFormatCulture.SelectedItem is MyComboBoxItem { Tag: var tag })
		{
			string text = tag?.ToString() ?? "auto";
			if (!(Config.Preference.Localization.FormatCulture == text))
			{
				Config.Preference.Localization.FormatCulture = text;
				HintService.Hint(Lang.Text("Setup.Language.Changed"), HintType.Success, log: false);
				Reload();
			}
		}
	}

	private static IEnumerable<CultureInfo> GetBuiltInFormatCultures()
	{
		HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (LocalizationLanguage supportedLanguage in LocalizationService.SupportedLanguages)
		{
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo(supportedLanguage.CultureName);
			if (used.Add(cultureInfo.Name))
			{
				yield return cultureInfo;
			}
		}
	}

	private static MyComboBoxItem CreateLanguageComboItem(string content, string tag, LocalizationLanguage language)
	{
		return new MyComboBoxItem
		{
			Content = content,
			Tag = tag,
			FontFamily = LocalizationFontService.BuildRepresentativeFontFamily(language)
		};
	}

	private static string GetLanguageDisplay(LocalizationLanguage language)
	{
		return language.NativeName ?? "";
	}

	private static string GetCultureDisplay(CultureInfo culture)
	{
		return culture.NativeName ?? "";
	}

	private static string NormalizeConfigValue(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "auto";
	}

	private static bool TryGetCulture(string value, out CultureInfo culture)
	{
		try
		{
			culture = CultureInfo.GetCultureInfo(value);
			return true;
		}
		catch (CultureNotFoundException)
		{
			culture = CultureInfo.InvariantCulture;
			return false;
		}
	}

	private static void SelectComboItem(MyComboBox comboBox, string tag)
	{
		foreach (MyComboBoxItem item in comboBox.Items.OfType<MyComboBoxItem>())
		{
			if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
			{
				comboBox.SelectedItem = item;
				return;
			}
		}
		comboBox.SelectedIndex = ((comboBox.Items.Count <= 0) ? (-1) : 0);
	}

	private bool IsFormatCultureItemExisting(string tag)
	{
		return ComboUiFormatCulture.Items.OfType<MyComboBoxItem>().Any((MyComboBoxItem item) => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
	}

}
