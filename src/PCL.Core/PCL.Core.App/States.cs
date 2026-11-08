using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.App.Configuration;

namespace PCL.Core.App;

public static class States
{
	[ConfigGroup("Hint", ConfigSource.Shared)]
	public sealed class HintStatesGroup : IConfigScope
	{
		public static readonly HintStatesGroup SINGLE_INSTANCE = new HintStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("HintDownloadThread", false, ConfigSource.Shared)]
		public bool LargeDownloadThread
		{
			get
			{
				return LargeDownloadThreadConfig.GetValue();
			}
			set
			{
				LargeDownloadThreadConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintRenderer", false, ConfigSource.Shared)]
		public bool Renderer
		{
			get
			{
				return RendererConfig.GetValue();
			}
			set
			{
				RendererConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintDebugLog4j2Config", false, ConfigSource.Shared)]
		public bool DebugLog4j2Config
		{
			get
			{
				return DebugLog4j2ConfigConfig.GetValue();
			}
			set
			{
				DebugLog4j2ConfigConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintInstallBack", false, ConfigSource.Shared)]
		public bool InstallPageBack
		{
			get
			{
				return InstallPageBackConfig.GetValue();
			}
			set
			{
				InstallPageBackConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintHide", false, ConfigSource.Shared)]
		public bool HideGameInstance
		{
			get
			{
				return HideGameInstanceConfig.GetValue();
			}
			set
			{
				HideGameInstanceConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintHandInstall", false, ConfigSource.Shared)]
		public bool ManualInstall
		{
			get
			{
				return ManualInstallConfig.GetValue();
			}
			set
			{
				ManualInstallConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("HintClearRubbish", 0, ConfigSource.Shared)]
		public int CleanJunkFile
		{
			get
			{
				return CleanJunkFileConfig.GetValue();
			}
			set
			{
				CleanJunkFileConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintUpdateMod", false, ConfigSource.Shared)]
		public bool UpdateMod
		{
			get
			{
				return UpdateModConfig.GetValue();
			}
			set
			{
				UpdateModConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintCustomCommand", false, ConfigSource.Shared)]
		public bool HomepageCommand
		{
			get
			{
				return HomepageCommandConfig.GetValue();
			}
			set
			{
				HomepageCommandConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintCustomWarn", false, ConfigSource.Shared)]
		public bool UntrustedHomepage
		{
			get
			{
				return UntrustedHomepageConfig.GetValue();
			}
			set
			{
				UntrustedHomepageConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintMoreAdvancedSetup", false, ConfigSource.Shared)]
		public bool MoreInstanceSetup
		{
			get
			{
				return MoreInstanceSetupConfig.GetValue();
			}
			set
			{
				MoreInstanceSetupConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintIndieSetup", false, ConfigSource.Shared)]
		public bool IndieSetup
		{
			get
			{
				return IndieSetupConfig.GetValue();
			}
			set
			{
				IndieSetupConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintProfileSelect", false, ConfigSource.Shared)]
		public bool LaunchWithProfile
		{
			get
			{
				return LaunchWithProfileConfig.GetValue();
			}
			set
			{
				LaunchWithProfileConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintExportConfig", false, ConfigSource.Shared)]
		public bool ExportConfig
		{
			get
			{
				return ExportConfigConfig.GetValue();
			}
			set
			{
				ExportConfigConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintMaxLog", false, ConfigSource.Shared)]
		public bool MaxGameLog
		{
			get
			{
				return MaxGameLogConfig.GetValue();
			}
			set
			{
				MaxGameLogConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintDisableGamePathCheckTip", false, ConfigSource.Shared)]
		public bool NonAsciiGamePath
		{
			get
			{
				return NonAsciiGamePathConfig.GetValue();
			}
			set
			{
				NonAsciiGamePathConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("UiLauncherCEHint", true, ConfigSource.Shared)]
		public bool CEMessage
		{
			get
			{
				return CEMessageConfig.GetValue();
			}
			set
			{
				CEMessageConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("UiSchematicFirstTimeHintShown", false, ConfigSource.Shared)]
		public bool SchematicFirstTime
		{
			get
			{
				return SchematicFirstTimeConfig.GetValue();
			}
			set
			{
				SchematicFirstTimeConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("SystemSystemAnnouncement", "", ConfigSource.Shared)]
		public string ShowedAnnouncements
		{
			get
			{
				return ShowedAnnouncementsConfig.GetValue();
			}
			set
			{
				ShowedAnnouncementsConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("HintDatapackUpdate", false, ConfigSource.Shared)]
		public bool FunctionDatapackUpdate
		{
			get
			{
				return FunctionDatapackUpdateConfig.GetValue();
			}
			set
			{
				FunctionDatapackUpdateConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> LargeDownloadThreadConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintDownloadThread"));

		public ConfigItem<bool> RendererConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintRenderer"));

		public ConfigItem<bool> DebugLog4j2ConfigConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintDebugLog4j2Config"));

		public ConfigItem<bool> InstallPageBackConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintInstallBack"));

		public ConfigItem<bool> HideGameInstanceConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintHide"));

		public ConfigItem<bool> ManualInstallConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintHandInstall"));

		public ConfigItem<int> CleanJunkFileConfig => field ?? (field = ConfigService.GetConfigItem<int>("HintClearRubbish"));

		public ConfigItem<bool> UpdateModConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintUpdateMod"));

		public ConfigItem<bool> HomepageCommandConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintCustomCommand"));

		public ConfigItem<bool> UntrustedHomepageConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintCustomWarn"));

		public ConfigItem<bool> MoreInstanceSetupConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintMoreAdvancedSetup"));

		public ConfigItem<bool> IndieSetupConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintIndieSetup"));

		public ConfigItem<bool> LaunchWithProfileConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintProfileSelect"));

		public ConfigItem<bool> ExportConfigConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintExportConfig"));

		public ConfigItem<bool> MaxGameLogConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintMaxLog"));

		public ConfigItem<bool> NonAsciiGamePathConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintDisableGamePathCheckTip"));

		public ConfigItem<bool> CEMessageConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiLauncherCEHint"));

		public ConfigItem<bool> SchematicFirstTimeConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiSchematicFirstTimeHintShown"));

		public ConfigItem<string> ShowedAnnouncementsConfig => field ?? (field = ConfigService.GetConfigItem<string>("SystemSystemAnnouncement"));

		public ConfigItem<bool> FunctionDatapackUpdateConfig => field ?? (field = ConfigService.GetConfigItem<bool>("HintDatapackUpdate"));

		private HintStatesGroup()
		{
			_InnerScopes = new IConfigScope[20]
			{
				LargeDownloadThreadConfig, RendererConfig, DebugLog4j2ConfigConfig, InstallPageBackConfig, HideGameInstanceConfig, ManualInstallConfig, CleanJunkFileConfig, UpdateModConfig, HomepageCommandConfig, UntrustedHomepageConfig,
				MoreInstanceSetupConfig, IndieSetupConfig, LaunchWithProfileConfig, ExportConfigConfig, MaxGameLogConfig, NonAsciiGamePathConfig, CEMessageConfig, SchematicFirstTimeConfig, ShowedAnnouncementsConfig, FunctionDatapackUpdateConfig
			};
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	[ConfigGroup("Game", ConfigSource.Shared)]
	public sealed class GameStatesGroup : IConfigScope
	{
		public static readonly GameStatesGroup SINGLE_INSTANCE = new GameStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<int>("CacheJavaListVersion", 0, ConfigSource.Shared)]
		public int JavaListVersion
		{
			get
			{
				return JavaListVersionConfig.GetValue();
			}
			set
			{
				JavaListVersionConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("CacheDrops", "", ConfigSource.Shared)]
		public string Drops
		{
			get
			{
				return DropsConfig.GetValue();
			}
			set
			{
				DropsConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchInstanceSelect", "", ConfigSource.Local)]
		public string SelectedInstance
		{
			get
			{
				return SelectedInstanceConfig.GetValue();
			}
			set
			{
				SelectedInstanceConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchFolderSelect", "", ConfigSource.Local)]
		public string SelectedFolder
		{
			get
			{
				return SelectedFolderConfig.GetValue();
			}
			set
			{
				SelectedFolderConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchFolders", "", ConfigSource.Shared)]
		public string Folders
		{
			get
			{
				return FoldersConfig.GetValue();
			}
			set
			{
				FoldersConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchArgumentJavaUser", "[]", ConfigSource.Shared)]
		public string JavaList
		{
			get
			{
				return JavaListConfig.GetValue();
			}
			set
			{
				JavaListConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("CompFavorites", "[]", ConfigSource.Shared)]
		public string CompFavorites
		{
			get
			{
				return CompFavoritesConfig.GetValue();
			}
			set
			{
				CompFavoritesConfig.SetValue(value);
			}
		}

		public ConfigItem<int> JavaListVersionConfig => field ?? (field = ConfigService.GetConfigItem<int>("CacheJavaListVersion"));

		public ConfigItem<string> DropsConfig => field ?? (field = ConfigService.GetConfigItem<string>("CacheDrops"));

		public ConfigItem<string> SelectedInstanceConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchInstanceSelect"));

		public ConfigItem<string> SelectedFolderConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchFolderSelect"));

		public ConfigItem<string> FoldersConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchFolders"));

		public ConfigItem<string> JavaListConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchArgumentJavaUser"));

		public ConfigItem<string> CompFavoritesConfig => field ?? (field = ConfigService.GetConfigItem<string>("CompFavorites"));

		private GameStatesGroup()
		{
			_InnerScopes = new IConfigScope[7] { JavaListVersionConfig, DropsConfig, SelectedInstanceConfig, SelectedFolderConfig, FoldersConfig, JavaListConfig, CompFavoritesConfig };
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	[ConfigGroup("UI", ConfigSource.Shared)]
	public sealed class UiStatesGroup : IConfigScope
	{
		public static readonly UiStatesGroup SINGLE_INSTANCE = new UiStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<string>("CacheSavedPageUrl", "", ConfigSource.Shared)]
		public string SavedHomepageUrl
		{
			get
			{
				return SavedHomepageUrlConfig.GetValue();
			}
			set
			{
				SavedHomepageUrlConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("CacheSavedPageVersion", "", ConfigSource.Shared)]
		public string SavedHomepageVersion
		{
			get
			{
				return SavedHomepageVersionConfig.GetValue();
			}
			set
			{
				SavedHomepageVersionConfig.SetValue(value);
			}
		}

		[ConfigItem<double>("WindowHeight", 550.0, ConfigSource.Local)]
		public double WindowHeight
		{
			get
			{
				return WindowHeightConfig.GetValue();
			}
			set
			{
				WindowHeightConfig.SetValue(value);
			}
		}

		[ConfigItem<double>("WindowWidth", 900.0, ConfigSource.Local)]
		public double WindowWidth
		{
			get
			{
				return WindowWidthConfig.GetValue();
			}
			set
			{
				WindowWidthConfig.SetValue(value);
			}
		}

		public ConfigItem<string> SavedHomepageUrlConfig => field ?? (field = ConfigService.GetConfigItem<string>("CacheSavedPageUrl"));

		public ConfigItem<string> SavedHomepageVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("CacheSavedPageVersion"));

		public ConfigItem<double> WindowHeightConfig => field ?? (field = ConfigService.GetConfigItem<double>("WindowHeight"));

		public ConfigItem<double> WindowWidthConfig => field ?? (field = ConfigService.GetConfigItem<double>("WindowWidth"));

		private UiStatesGroup()
		{
			_InnerScopes = new IConfigScope[4] { SavedHomepageUrlConfig, SavedHomepageVersionConfig, WindowHeightConfig, WindowWidthConfig };
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	[ConfigGroup("System", ConfigSource.Shared)]
	public sealed class SystemStatesGroup : IConfigScope
	{
		public static readonly SystemStatesGroup SINGLE_INSTANCE = new SystemStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<string>("LaunchUuid", "", ConfigSource.Shared)]
		public string LaunchUuid
		{
			get
			{
				return LaunchUuidConfig.GetValue();
			}
			set
			{
				LaunchUuidConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("CacheExportConfig", "", ConfigSource.Shared)]
		public string ExportConfigPath
		{
			get
			{
				return ExportConfigPathConfig.GetValue();
			}
			set
			{
				ExportConfigPathConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("SystemEula", false, ConfigSource.Shared)]
		public bool LauncherEula
		{
			get
			{
				return LauncherEulaConfig.GetValue();
			}
			set
			{
				LauncherEulaConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemCount", 0, ConfigSource.SharedEncrypt)]
		public int StartupCount
		{
			get
			{
				return StartupCountConfig.GetValue();
			}
			set
			{
				StartupCountConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemLaunchCount", 0, ConfigSource.SharedEncrypt)]
		public int LaunchCount
		{
			get
			{
				return LaunchCountConfig.GetValue();
			}
			set
			{
				LaunchCountConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemLastVersionReg", 0, ConfigSource.SharedEncrypt)]
		public int LastVersion
		{
			get
			{
				return LastVersionConfig.GetValue();
			}
			set
			{
				LastVersionConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemHighestBetaVersionReg", 0, ConfigSource.SharedEncrypt)]
		public int LastBetaVersion
		{
			get
			{
				return LastBetaVersionConfig.GetValue();
			}
			set
			{
				LastBetaVersionConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemHighestAlphaVersionReg", 0, ConfigSource.SharedEncrypt)]
		public int LastAlphaVersion
		{
			get
			{
				return LastAlphaVersionConfig.GetValue();
			}
			set
			{
				LastAlphaVersionConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemSetupVersionReg", 1, ConfigSource.Shared)]
		public int SetupVersionGlobal
		{
			get
			{
				return SetupVersionGlobalConfig.GetValue();
			}
			set
			{
				SetupVersionGlobalConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemSetupVersionIni", 1, ConfigSource.Local)]
		public int SetupVersionLocal
		{
			get
			{
				return SetupVersionLocalConfig.GetValue();
			}
			set
			{
				SetupVersionLocalConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemSystemActivity", 0, ConfigSource.Local)]
		public int AnnounceSolution
		{
			get
			{
				return AnnounceSolutionConfig.GetValue();
			}
			set
			{
				AnnounceSolutionConfig.SetValue(value);
			}
		}

		public ConfigItem<string> LaunchUuidConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchUuid"));

		public ConfigItem<string> ExportConfigPathConfig => field ?? (field = ConfigService.GetConfigItem<string>("CacheExportConfig"));

		public ConfigItem<bool> LauncherEulaConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemEula"));

		public ConfigItem<int> StartupCountConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemCount"));

		public ConfigItem<int> LaunchCountConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemLaunchCount"));

		public ConfigItem<int> LastVersionConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemLastVersionReg"));

		public ConfigItem<int> LastBetaVersionConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemHighestBetaVersionReg"));

		public ConfigItem<int> LastAlphaVersionConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemHighestAlphaVersionReg"));

		public ConfigItem<int> SetupVersionGlobalConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemSetupVersionReg"));

		public ConfigItem<int> SetupVersionLocalConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemSetupVersionIni"));

		public ConfigItem<int> AnnounceSolutionConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemSystemActivity"));

		private SystemStatesGroup()
		{
			_InnerScopes = new IConfigScope[11]
			{
				LaunchUuidConfig, ExportConfigPathConfig, LauncherEulaConfig, StartupCountConfig, LaunchCountConfig, LastVersionConfig, LastBetaVersionConfig, LastAlphaVersionConfig, SetupVersionGlobalConfig, SetupVersionLocalConfig,
				AnnounceSolutionConfig
			};
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	[ConfigGroup("Tool", ConfigSource.Shared)]
	public sealed class ToolStatesGroup : IConfigScope
	{
		public static readonly ToolStatesGroup SINGLE_INSTANCE = new ToolStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<string>("CacheDownloadFolder", "", ConfigSource.Shared)]
		public string DownloadFolder
		{
			get
			{
				return DownloadFolderConfig.GetValue();
			}
			set
			{
				DownloadFolderConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("ToolDownloadCustomUserAgent", "", ConfigSource.Shared)]
		public string DownloadUserAgent
		{
			get
			{
				return DownloadUserAgentConfig.GetValue();
			}
			set
			{
				DownloadUserAgentConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("ToolUpdateReleaseLast", "", ConfigSource.Shared)]
		public string LastRelease
		{
			get
			{
				return LastReleaseConfig.GetValue();
			}
			set
			{
				LastReleaseConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("ToolUpdateSnapshotLast", "", ConfigSource.Shared)]
		public string LastSnapshot
		{
			get
			{
				return LastSnapshotConfig.GetValue();
			}
			set
			{
				LastSnapshotConfig.SetValue(value);
			}
		}

		public ConfigItem<string> DownloadFolderConfig => field ?? (field = ConfigService.GetConfigItem<string>("CacheDownloadFolder"));

		public ConfigItem<string> DownloadUserAgentConfig => field ?? (field = ConfigService.GetConfigItem<string>("ToolDownloadCustomUserAgent"));

		public ConfigItem<string> LastReleaseConfig => field ?? (field = ConfigService.GetConfigItem<string>("ToolUpdateReleaseLast"));

		public ConfigItem<string> LastSnapshotConfig => field ?? (field = ConfigService.GetConfigItem<string>("ToolUpdateSnapshotLast"));

		private ToolStatesGroup()
		{
			_InnerScopes = new IConfigScope[4] { DownloadFolderConfig, DownloadUserAgentConfig, LastReleaseConfig, LastSnapshotConfig };
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	[ConfigGroup("Link", ConfigSource.Shared)]
	public sealed class LinkStatesGroup : IConfigScope
	{
		public static readonly LinkStatesGroup SINGLE_INSTANCE = new LinkStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("LinkEula", false, ConfigSource.Shared)]
		public bool LinkEula
		{
			get
			{
				return LinkEulaConfig.GetValue();
			}
			set
			{
				LinkEulaConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LinkAnnounceCache", "", ConfigSource.SharedEncrypt)]
		public string AnnounceCache
		{
			get
			{
				return AnnounceCacheConfig.GetValue();
			}
			set
			{
				AnnounceCacheConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LinkAnnounceCacheVer", 0, ConfigSource.Shared)]
		public int AnnounceCacheVer
		{
			get
			{
				return AnnounceCacheVerConfig.GetValue();
			}
			set
			{
				AnnounceCacheVerConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LinkNaidRefreshToken", "", ConfigSource.SharedEncrypt)]
		public string NaidRefreshToken
		{
			get
			{
				return NaidRefreshTokenConfig.GetValue();
			}
			set
			{
				NaidRefreshTokenConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LinkNaidRefreshExpiresAt", "", ConfigSource.SharedEncrypt)]
		public string NaidRefreshExpireTime
		{
			get
			{
				return NaidRefreshExpireTimeConfig.GetValue();
			}
			set
			{
				NaidRefreshExpireTimeConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LinkFirstTimeNetTest", true, ConfigSource.SharedEncrypt)]
		public bool DoFirstTimeNetTest
		{
			get
			{
				return DoFirstTimeNetTestConfig.GetValue();
			}
			set
			{
				DoFirstTimeNetTestConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> LinkEulaConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LinkEula"));

		public ConfigItem<string> AnnounceCacheConfig => field ?? (field = ConfigService.GetConfigItem<string>("LinkAnnounceCache"));

		public ConfigItem<int> AnnounceCacheVerConfig => field ?? (field = ConfigService.GetConfigItem<int>("LinkAnnounceCacheVer"));

		public ConfigItem<string> NaidRefreshTokenConfig => field ?? (field = ConfigService.GetConfigItem<string>("LinkNaidRefreshToken"));

		public ConfigItem<string> NaidRefreshExpireTimeConfig => field ?? (field = ConfigService.GetConfigItem<string>("LinkNaidRefreshExpiresAt"));

		public ConfigItem<bool> DoFirstTimeNetTestConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LinkFirstTimeNetTest"));

		private LinkStatesGroup()
		{
			_InnerScopes = new IConfigScope[6] { LinkEulaConfig, AnnounceCacheConfig, AnnounceCacheVerConfig, NaidRefreshTokenConfig, NaidRefreshExpireTimeConfig, DoFirstTimeNetTestConfig };
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	[ConfigGroup("Instance", ConfigSource.GameInstance)]
	public sealed class InstanceStatesGroup : IConfigScope
	{
		private readonly ArgConfig<int> ACCESSOR_LaunchCount;

		private readonly ArgConfig<bool> ACCESSOR_Starred;

		private readonly ArgConfig<int> ACCESSOR_CardType;

		private readonly ArgConfig<string> ACCESSOR_LogoPath;

		private readonly ArgConfig<bool> ACCESSOR_IsLogoCustom;

		private readonly ArgConfig<string> ACCESSOR_CustomInfo;

		private readonly ArgConfig<string> ACCESSOR_Info;

		private readonly ArgConfig<string> ACCESSOR_ReleaseTime;

		private readonly ArgConfig<int> ACCESSOR_State;

		private readonly ArgConfig<string> ACCESSOR_FabricVersion;

		private readonly ArgConfig<string> ACCESSOR_LegacyFabricVersion;

		private readonly ArgConfig<string> ACCESSOR_QuiltVersion;

		private readonly ArgConfig<string> ACCESSOR_LabyModVersion;

		private readonly ArgConfig<string> ACCESSOR_OptiFineVersion;

		private readonly ArgConfig<bool> ACCESSOR_HasLiteLoader;

		private readonly ArgConfig<string> ACCESSOR_ForgeVersion;

		private readonly ArgConfig<string> ACCESSOR_NeoForgeVersion;

		private readonly ArgConfig<string> ACCESSOR_CleanroomVersion;

		private readonly ArgConfig<string> ACCESSOR_VanillaVersionName;

		private readonly ArgConfig<string> ACCESSOR_VanillaVersion;

		private readonly ArgConfig<string> ACCESSOR_ModpackVersion;

		private readonly ArgConfig<string> ACCESSOR_ModpackSource;

		private readonly ArgConfig<string> ACCESSOR_ModpackId;

		public static readonly InstanceStatesGroup SINGLE_INSTANCE = new InstanceStatesGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<int>("VersionLaunchCount", 0, ConfigSource.Shared)]
		public ArgConfig<int> LaunchCount => ACCESSOR_LaunchCount;

		[ConfigItem<bool>("IsStar", false, ConfigSource.Shared)]
		public ArgConfig<bool> Starred => ACCESSOR_Starred;

		[ConfigItem<int>("DisplayType", 0, ConfigSource.Shared)]
		public ArgConfig<int> CardType => ACCESSOR_CardType;

		[ConfigItem<string>("Logo", "", ConfigSource.Shared)]
		public ArgConfig<string> LogoPath => ACCESSOR_LogoPath;

		[ConfigItem<bool>("LogoCustom", false, ConfigSource.Shared)]
		public ArgConfig<bool> IsLogoCustom => ACCESSOR_IsLogoCustom;

		[ConfigItem<string>("CustomInfo", "", ConfigSource.Shared)]
		public ArgConfig<string> CustomInfo => ACCESSOR_CustomInfo;

		[ConfigItem<string>("Info", "", ConfigSource.Shared)]
		public ArgConfig<string> Info => ACCESSOR_Info;

		[ConfigItem<string>("ReleaseTime", "", ConfigSource.Shared)]
		public ArgConfig<string> ReleaseTime => ACCESSOR_ReleaseTime;

		[ConfigItem<int>("State", 0, ConfigSource.Shared)]
		public ArgConfig<int> State => ACCESSOR_State;

		[ConfigItem<string>("VersionFabric", "", ConfigSource.Shared)]
		public ArgConfig<string> FabricVersion => ACCESSOR_FabricVersion;

		[ConfigItem<string>("VersionLegacyFabric", "", ConfigSource.Shared)]
		public ArgConfig<string> LegacyFabricVersion => ACCESSOR_LegacyFabricVersion;

		[ConfigItem<string>("VersionQuilt", "", ConfigSource.Shared)]
		public ArgConfig<string> QuiltVersion => ACCESSOR_QuiltVersion;

		[ConfigItem<string>("VersionLabyMod", "", ConfigSource.Shared)]
		public ArgConfig<string> LabyModVersion => ACCESSOR_LabyModVersion;

		[ConfigItem<string>("VersionOptiFine", "", ConfigSource.Shared)]
		public ArgConfig<string> OptiFineVersion => ACCESSOR_OptiFineVersion;

		[ConfigItem<bool>("VersionLiteLoader", false, ConfigSource.Shared)]
		public ArgConfig<bool> HasLiteLoader => ACCESSOR_HasLiteLoader;

		[ConfigItem<string>("VersionForge", "", ConfigSource.Shared)]
		public ArgConfig<string> ForgeVersion => ACCESSOR_ForgeVersion;

		[ConfigItem<string>("VersionNeoForge", "", ConfigSource.Shared)]
		public ArgConfig<string> NeoForgeVersion => ACCESSOR_NeoForgeVersion;

		[ConfigItem<string>("VersionCleanroom", "", ConfigSource.Shared)]
		public ArgConfig<string> CleanroomVersion => ACCESSOR_CleanroomVersion;

		[ConfigItem<string>("VersionVanillaName", "Unknown", ConfigSource.Shared)]
		public ArgConfig<string> VanillaVersionName => ACCESSOR_VanillaVersionName;

		[ConfigItem<string>("VersionVanilla", "0.0.0", ConfigSource.Shared)]
		public ArgConfig<string> VanillaVersion => ACCESSOR_VanillaVersion;

		[ConfigItem<string>("VersionModpackVersion", "", ConfigSource.Shared)]
		public ArgConfig<string> ModpackVersion => ACCESSOR_ModpackVersion;

		[ConfigItem<string>("VersionModpackSource", "", ConfigSource.Shared)]
		public ArgConfig<string> ModpackSource => ACCESSOR_ModpackSource;

		[ConfigItem<string>("VersionModpackId", "", ConfigSource.Shared)]
		public ArgConfig<string> ModpackId => ACCESSOR_ModpackId;

		public ConfigItem<int> LaunchCountConfig => field ?? (field = ConfigService.GetConfigItem<int>("VersionLaunchCount"));

		public ConfigItem<bool> StarredConfig => field ?? (field = ConfigService.GetConfigItem<bool>("IsStar"));

		public ConfigItem<int> CardTypeConfig => field ?? (field = ConfigService.GetConfigItem<int>("DisplayType"));

		public ConfigItem<string> LogoPathConfig => field ?? (field = ConfigService.GetConfigItem<string>("Logo"));

		public ConfigItem<bool> IsLogoCustomConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LogoCustom"));

		public ConfigItem<string> CustomInfoConfig => field ?? (field = ConfigService.GetConfigItem<string>("CustomInfo"));

		public ConfigItem<string> InfoConfig => field ?? (field = ConfigService.GetConfigItem<string>("Info"));

		public ConfigItem<string> ReleaseTimeConfig => field ?? (field = ConfigService.GetConfigItem<string>("ReleaseTime"));

		public ConfigItem<int> StateConfig => field ?? (field = ConfigService.GetConfigItem<int>("State"));

		public ConfigItem<string> FabricVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionFabric"));

		public ConfigItem<string> LegacyFabricVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionLegacyFabric"));

		public ConfigItem<string> QuiltVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionQuilt"));

		public ConfigItem<string> LabyModVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionLabyMod"));

		public ConfigItem<string> OptiFineVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionOptiFine"));

		public ConfigItem<bool> HasLiteLoaderConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionLiteLoader"));

		public ConfigItem<string> ForgeVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionForge"));

		public ConfigItem<string> NeoForgeVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionNeoForge"));

		public ConfigItem<string> CleanroomVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionCleanroom"));

		public ConfigItem<string> VanillaVersionNameConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionVanillaName"));

		public ConfigItem<string> VanillaVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionVanilla"));

		public ConfigItem<string> ModpackVersionConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionModpackVersion"));

		public ConfigItem<string> ModpackSourceConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionModpackSource"));

		public ConfigItem<string> ModpackIdConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionModpackId"));

		private InstanceStatesGroup()
		{
			_InnerScopes = new IConfigScope[23]
			{
				LaunchCountConfig, StarredConfig, CardTypeConfig, LogoPathConfig, IsLogoCustomConfig, CustomInfoConfig, InfoConfig, ReleaseTimeConfig, StateConfig, FabricVersionConfig,
				LegacyFabricVersionConfig, QuiltVersionConfig, LabyModVersionConfig, OptiFineVersionConfig, HasLiteLoaderConfig, ForgeVersionConfig, NeoForgeVersionConfig, CleanroomVersionConfig, VanillaVersionNameConfig, VanillaVersionConfig,
				ModpackVersionConfig, ModpackSourceConfig, ModpackIdConfig
			};
			ACCESSOR_LaunchCount = new ArgConfig<int>((object? arg) => LaunchCountConfig.GetValue(arg), (object? arg, int value) =>
			{
				LaunchCountConfig.SetValue(value, arg);
			});
			ACCESSOR_Starred = new ArgConfig<bool>((object? arg) => StarredConfig.GetValue(arg), (object? arg, bool value) =>
			{
				StarredConfig.SetValue(value, arg);
			});
			ACCESSOR_CardType = new ArgConfig<int>((object? arg) => CardTypeConfig.GetValue(arg), (object? arg, int value) =>
			{
				CardTypeConfig.SetValue(value, arg);
			});
			ACCESSOR_LogoPath = new ArgConfig<string>((object? arg) => LogoPathConfig.GetValue(arg), (object? arg, string value) =>
			{
				LogoPathConfig.SetValue(value, arg);
			});
			ACCESSOR_IsLogoCustom = new ArgConfig<bool>((object? arg) => IsLogoCustomConfig.GetValue(arg), (object? arg, bool value) =>
			{
				IsLogoCustomConfig.SetValue(value, arg);
			});
			ACCESSOR_CustomInfo = new ArgConfig<string>((object? arg) => CustomInfoConfig.GetValue(arg), (object? arg, string value) =>
			{
				CustomInfoConfig.SetValue(value, arg);
			});
			ACCESSOR_Info = new ArgConfig<string>((object? arg) => InfoConfig.GetValue(arg), (object? arg, string value) =>
			{
				InfoConfig.SetValue(value, arg);
			});
			ACCESSOR_ReleaseTime = new ArgConfig<string>((object? arg) => ReleaseTimeConfig.GetValue(arg), (object? arg, string value) =>
			{
				ReleaseTimeConfig.SetValue(value, arg);
			});
			ACCESSOR_State = new ArgConfig<int>((object? arg) => StateConfig.GetValue(arg), (object? arg, int value) =>
			{
				StateConfig.SetValue(value, arg);
			});
			ACCESSOR_FabricVersion = new ArgConfig<string>((object? arg) => FabricVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				FabricVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_LegacyFabricVersion = new ArgConfig<string>((object? arg) => LegacyFabricVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				LegacyFabricVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_QuiltVersion = new ArgConfig<string>((object? arg) => QuiltVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				QuiltVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_LabyModVersion = new ArgConfig<string>((object? arg) => LabyModVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				LabyModVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_OptiFineVersion = new ArgConfig<string>((object? arg) => OptiFineVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				OptiFineVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_HasLiteLoader = new ArgConfig<bool>((object? arg) => HasLiteLoaderConfig.GetValue(arg), (object? arg, bool value) =>
			{
				HasLiteLoaderConfig.SetValue(value, arg);
			});
			ACCESSOR_ForgeVersion = new ArgConfig<string>((object? arg) => ForgeVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				ForgeVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_NeoForgeVersion = new ArgConfig<string>((object? arg) => NeoForgeVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				NeoForgeVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_CleanroomVersion = new ArgConfig<string>((object? arg) => CleanroomVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				CleanroomVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_VanillaVersionName = new ArgConfig<string>((object? arg) => VanillaVersionNameConfig.GetValue(arg), (object? arg, string value) =>
			{
				VanillaVersionNameConfig.SetValue(value, arg);
			});
			ACCESSOR_VanillaVersion = new ArgConfig<string>((object? arg) => VanillaVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				VanillaVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_ModpackVersion = new ArgConfig<string>((object? arg) => ModpackVersionConfig.GetValue(arg), (object? arg, string value) =>
			{
				ModpackVersionConfig.SetValue(value, arg);
			});
			ACCESSOR_ModpackSource = new ArgConfig<string>((object? arg) => ModpackSourceConfig.GetValue(arg), (object? arg, string value) =>
			{
				ModpackSourceConfig.SetValue(value, arg);
			});
			ACCESSOR_ModpackId = new ArgConfig<string>((object? arg) => ModpackIdConfig.GetValue(arg), (object? arg, string value) =>
			{
				ModpackIdConfig.SetValue(value, arg);
			});
		}

		public IEnumerable<string> CheckScope(IReadOnlySet<string> keys)
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				IEnumerable<string> enumerable2 = innerScopes[i].CheckScope(keys);
				if (enumerable2.Any())
				{
					enumerable = enumerable.Concat(enumerable2);
				}
			}
			return enumerable;
		}

		public bool Reset(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].Reset(argument))
				{
					result = false;
				}
			}
			return result;
		}

		public bool IsDefault(object? argument = null)
		{
			bool result = true;
			IConfigScope[] innerScopes = _InnerScopes;
			for (int i = 0; i < innerScopes.Length; i++)
			{
				if (!innerScopes[i].IsDefault(argument))
				{
					result = false;
				}
			}
			return result;
		}
	}

	public static readonly HintStatesGroup Hint;

	public static readonly GameStatesGroup Game;

	public static readonly UiStatesGroup UI;

	public static readonly SystemStatesGroup System;

	public static readonly ToolStatesGroup Tool;

	public static readonly LinkStatesGroup Link;

	public static readonly InstanceStatesGroup Instance;

	[ConfigItem<string>("Identify", "", ConfigSource.Shared)]
	public static string Identifier
	{
		get
		{
			return IdentifierConfig.GetValue();
		}
		set
		{
			IdentifierConfig.SetValue(value);
		}
	}

	[AnyConfigItem<Dictionary<string, string>>("CustomVariables", ConfigSource.Local)]
	public static Dictionary<string, string> CustomVariables
	{
		get
		{
			return CustomVariablesConfig.GetValue();
		}
		set
		{
			CustomVariablesConfig.SetValue(value);
		}
	}

	public static ConfigItem<string> IdentifierConfig => field ?? (field = ConfigService.GetConfigItem<string>("Identify"));

	public static ConfigItem<Dictionary<string, string>> CustomVariablesConfig => field ?? (field = ConfigService.GetConfigItem<Dictionary<string, string>>("CustomVariables"));

	static States()
	{
		IdentifierConfig = null;
		CustomVariablesConfig = null;
		Hint = HintStatesGroup.SINGLE_INSTANCE;
		Game = GameStatesGroup.SINGLE_INSTANCE;
		UI = UiStatesGroup.SINGLE_INSTANCE;
		System = SystemStatesGroup.SINGLE_INSTANCE;
		Tool = ToolStatesGroup.SINGLE_INSTANCE;
		Link = LinkStatesGroup.SINGLE_INSTANCE;
		Instance = InstanceStatesGroup.SINGLE_INSTANCE;
	}
}
