using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PCL.Core.App.Region;

public static class RegionPolicy
{
	private const int MainlandChinaGeoId = 45;

	private const string DeviceRegionRegistryPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\DeviceRegion";

	private const string DeviceRegionValueName = "DeviceRegion";

	private static readonly RegistryView[] DeviceRegionRegistryViews = new RegistryView[2]
	{
		RegistryView.Registry64,
		RegistryView.Registry32
	};

	private static readonly HashSet<string> ChinaTimeZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "China Standard Time", "Asia/Shanghai", "PRC" };

	public static RegionAssessment Evaluate(RegionSignals signals)
	{
		ArgumentNullException.ThrowIfNull(signals, "signals");
		if (signals.GeoNameAvailable && IsChinaGeoName(signals.GeoName))
		{
			return new RegionAssessment(RegionClassification.Mainland, "geo-cn");
		}
		if (signals.DeviceRegionAvailable && IsChinaDeviceRegion(signals.DeviceRegionGeoId))
		{
			return new RegionAssessment(RegionClassification.Mainland, "device-region-cn");
		}
		bool flag = signals.TimeZoneAvailable && signals.TimeZoneId != null && ChinaTimeZones.Contains(signals.TimeZoneId);
		bool flag2 = signals.UserLocaleAvailable && IsChinaLocale(signals.UserLocaleName);
		bool flag3 = signals.PreferredUiLanguagesAvailable && IsChinaLocale(signals.PreferredUiLanguages.FirstOrDefault());
		if (flag && (flag2 | flag3))
		{
			return new RegionAssessment(RegionClassification.Mainland, "china-time-zone-and-locale");
		}
		if (signals.GeoNameAvailable && IsValidCountryCode(signals.GeoName) && !IsChinaGeoName(signals.GeoName))
		{
			return new RegionAssessment(RegionClassification.Other, "geo-other");
		}
		bool flag4 = (signals.UserLocaleAvailable && IsExplicitOtherLocale(signals.UserLocaleName)) || (signals.PreferredUiLanguagesAvailable && IsExplicitOtherLocale(signals.PreferredUiLanguages.FirstOrDefault()));
		if ((!signals.GeoNameAvailable && signals.TimeZoneAvailable && !flag) & flag4)
		{
			return new RegionAssessment(RegionClassification.Other, "other-time-zone-and-locale");
		}
		if (!signals.GeoNameAvailable && !signals.PreferredUiLanguagesAvailable && !signals.UserLocaleAvailable && !signals.TimeZoneAvailable)
		{
			return new RegionAssessment(RegionClassification.Unknown, "signals-unavailable");
		}
		return new RegionAssessment(RegionClassification.Unknown, "insufficient-signals");
	}

	public static RegionSignals ReadWindowsSignals()
	{
		string geoName = TryReadGeoName(out var available);
		IReadOnlyList<string> preferredUiLanguages = TryReadPreferredUiLanguages(out var available2);
		string userLocaleName = TryReadUserLocale(out var available3);
		int? deviceRegionGeoId = TryReadDeviceRegion(out var available4);
		string text = null;
		bool timeZoneAvailable = false;
		try
		{
			text = TimeZoneInfo.Local.Id;
			timeZoneAvailable = !string.IsNullOrWhiteSpace(text);
		}
		catch
		{
		}
		return new RegionSignals(geoName, available, preferredUiLanguages, available2, userLocaleName, available3, text, timeZoneAvailable, deviceRegionGeoId, available4);
	}

	private static bool IsChinaGeoName(string? name)
	{
		if (!string.Equals(name, "CN", StringComparison.OrdinalIgnoreCase))
		{
			return name == "156";
		}
		return true;
	}

	private static bool IsChinaDeviceRegion(int? geoId)
	{
		return geoId == 45;
	}

	private static bool IsValidCountryCode(string? name)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Length != 2 || !name.All(char.IsAsciiLetter))
		{
			return false;
		}
		try
		{
			new RegionInfo(name);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static bool IsChinaLocale(string? localeName)
	{
		if (string.IsNullOrWhiteSpace(localeName))
		{
			return false;
		}
		try
		{
			return string.Equals(CultureInfo.GetCultureInfo(localeName.Replace('_', '-')).Name, "zh-CN", StringComparison.OrdinalIgnoreCase) || CultureInfo.GetCultureInfo(localeName.Replace('_', '-')).Name.EndsWith("-CN", StringComparison.OrdinalIgnoreCase);
		}
		catch (CultureNotFoundException)
		{
			return false;
		}
	}

	private static bool IsExplicitOtherLocale(string? localeName)
	{
		if (string.IsNullOrWhiteSpace(localeName))
		{
			return false;
		}
		try
		{
			CultureInfo cultureInfo = CultureInfo.GetCultureInfo(localeName.Replace('_', '-'));
			return !cultureInfo.IsNeutralCulture && !string.Equals(new RegionInfo(cultureInfo.Name).TwoLetterISORegionName, "CN", StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception ex) when ((ex is CultureNotFoundException || ex is ArgumentException) ? true : false)
		{
			return false;
		}
	}

	private unsafe static string? TryReadGeoName(out bool available)
	{
		available = false;
		try
		{
			char[] array = new char[16];
			int userDefaultGeoName;
			fixed (char* geoName = array)
			{
				userDefaultGeoName = GetUserDefaultGeoName(geoName, array.Length);
			}
			if (userDefaultGeoName > 1 && userDefaultGeoName <= array.Length)
			{
				available = true;
				return new string(array, 0, userDefaultGeoName - 1);
			}
			return TryReadLegacyGeoName(out available);
		}
		catch
		{
			return TryReadLegacyGeoName(out available);
		}
	}

	private unsafe static string? TryReadLegacyGeoName(out bool available)
	{
		available = false;
		try
		{
			int userGeoID = GetUserGeoID(16);
			if (userGeoID <= 0)
			{
				return null;
			}
			char[] array = new char[16];
			int geoInfoW;
			fixed (char* geoData = array)
			{
				geoInfoW = GetGeoInfoW(userGeoID, 4, geoData, array.Length, 0);
			}
			if (geoInfoW <= 1 || geoInfoW > array.Length)
			{
				return null;
			}
			available = true;
			return new string(array, 0, geoInfoW - 1);
		}
		catch
		{
			return null;
		}
	}

	private unsafe static IReadOnlyList<string> TryReadPreferredUiLanguages(out bool available)
	{
		available = false;
		try
		{
			uint num = 0u;
			uint num2 = 0u;
			GetUserPreferredUILanguages(8u, &num, null, &num2);
			if (num2 == 0 || num2 > 32768)
			{
				return Array.Empty<string>();
			}
			nint num3 = Marshal.AllocHGlobal(checked((int)num2 * 2));
			try
			{
				if (GetUserPreferredUILanguages(8u, &num, (char*)num3, &num2) == 0)
				{
					return Array.Empty<string>();
				}
				string text = new string((char*)num3, 0, checked((int)num2));
				available = true;
				return text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
			}
			finally
			{
				Marshal.FreeHGlobal(num3);
			}
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	private unsafe static string? TryReadUserLocale(out bool available)
	{
		available = false;
		try
		{
			char[] array = new char[85];
			int userDefaultLocaleName;
			fixed (char* localeName = array)
			{
				userDefaultLocaleName = GetUserDefaultLocaleName(localeName, array.Length);
			}
			if (userDefaultLocaleName <= 1 || userDefaultLocaleName > array.Length)
			{
				return null;
			}
			available = true;
			return new string(array, 0, userDefaultLocaleName - 1);
		}
		catch
		{
			return null;
		}
	}

	private static int? TryReadDeviceRegion(out bool available)
	{
		available = false;
		RegistryView[] deviceRegionRegistryViews = DeviceRegionRegistryViews;
		foreach (RegistryView view in deviceRegionRegistryViews)
		{
			try
			{
				using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
				using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Control Panel\\DeviceRegion");
				object obj = registryKey2?.GetValue("DeviceRegion");
				if (obj == null)
				{
					continue;
				}
				int value = Convert.ToInt32(obj, CultureInfo.InvariantCulture);
				available = true;
				return value;
			}
			catch
			{
			}
		}
		return null;
	}

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private unsafe static extern int GetUserDefaultGeoName(char* geoName, int geoNameCount);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private unsafe static extern int GetUserDefaultLocaleName(char* localeName, int localeNameCount);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private unsafe static extern int GetUserPreferredUILanguages(uint flags, uint* languageCount, char* languageBuffer, uint* bufferChars);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern int GetUserGeoID(int geoClass);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private unsafe static extern int GetGeoInfoW(int location, int geoType, char* geoData, int dataSize, int flags);
}
