using System.Collections.Generic;

namespace PCL.Core.App.Region;

public sealed record RegionSignals(string? GeoName, bool GeoNameAvailable, IReadOnlyList<string> PreferredUiLanguages, bool PreferredUiLanguagesAvailable, string? UserLocaleName, bool UserLocaleAvailable, string? TimeZoneId, bool TimeZoneAvailable, int? DeviceRegionGeoId = null, bool DeviceRegionAvailable = false);
