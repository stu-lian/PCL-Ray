using Microsoft.Win32;

namespace PCL.Core.Utils.OS;

public static class AumidHelper
{
	public const string Aumid = "PCLCommunity.PCLCE";

	public static bool HasAumid()
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\AppUserModelId\\" + "PCLCommunity.PCLCE");
		return registryKey != null;
	}

	public static void RegisterAumid()
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\AppUserModelId\\" + "PCLCommunity.PCLCE");
		registryKey.SetValue("DisplayName", "Plain Craft Launcher Ray Edition");
		registryKey.SetValue("IconUri", IconHelper.GetIconPath());
		registryKey.SetValue("IconBackgroundColor", "FFDDDD");
	}

	public static void UnregisterAumid()
	{
		Registry.CurrentUser.DeleteSubKey("Software\\Classes\\AppUserModelId\\" + "PCLCommunity.PCLCE", throwOnMissingSubKey: false);
	}
}
