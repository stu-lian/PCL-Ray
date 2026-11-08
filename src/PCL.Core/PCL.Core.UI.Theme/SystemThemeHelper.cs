using System;
using System.IO;
using System.Security;
using Microsoft.Win32;
using PCL.Core.Logging;

namespace PCL.Core.UI.Theme;

public static class SystemThemeHelper
{
	private const string ThemeRegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

	private const string AppsUseLightThemeKey = "AppsUseLightTheme";

	public static bool IsSystemInDarkMode()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			if (registryKey == null)
			{
				LogWrapper.Warn("注册表键 Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize 不存在");
				return false;
			}
			return registryKey.GetValue("AppsUseLightTheme") as int? == 0;
		}
		catch (Exception ex) when ((ex is SecurityException || ex is IOException) ? true : false)
		{
			LogWrapper.Warn(ex, "无法访问注册表键 Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			return false;
		}
	}

	[Obsolete("Use ThemeService.IsDarkMode instead")]
	public static bool IsDarkMode()
	{
		return ThemeService.IsDarkMode;
	}
}
