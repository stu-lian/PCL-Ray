using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI.Theme;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App.Configuration;

[LifecycleService(LifecycleState.Loading, Priority = 1919810)]
[LifecycleScope("config", "配置", true)]
public sealed class ConfigService : ILifecycleService
{
	private static readonly Dictionary<string, ConfigItem> _Items = new Dictionary<string, ConfigItem>();

	private static readonly HashSet<string> _KeySet = new HashSet<string>();

	private static ConfigStorage? _sharedConfigProvider;

	private static ConfigStorage? _sharedEncryptedConfigProvider;

	private static ConfigStorage? _localConfigProvider;

	private static ConfigStorage? _instanceConfigProvider;

	private static bool _isProvidersInitialized = false;

	private static bool _isConfigItemsInitialized = false;

	public static IReadOnlySet<string> KeySet => _KeySet;

	[ConfigItem<int>("FileVersion", 1, ConfigSource.Shared)]
	public static int SharedVersion
	{
		get
		{
			return SharedVersionConfig.GetValue();
		}
		set
		{
			SharedVersionConfig.SetValue(value);
		}
	}

	[ConfigItem<int>("LocalFileVersion", 1, ConfigSource.Local)]
	public static int LocalVersion
	{
		get
		{
			return LocalVersionConfig.GetValue();
		}
		set
		{
			LocalVersionConfig.SetValue(value);
		}
	}

	public static string SharedConfigPath { get; } = Path.Combine(Paths.SharedData, "config.v1.json");

	public static string LocalConfigPath { get; } = Path.Combine(Paths.Data, "config.v1.yml");

	public static bool IsInitialized { get; private set; } = false;

	[RegisterConfigEvent]
	public static ConfigEventRegistry SharedVersionInit => new ConfigEventRegistry(SharedVersionConfig, (ConfigEventArgs e) =>
	{
		_UpdateConfigVersion(SharedVersionConfig, "全局", (int)e.NewValue);
	}, ConfigEvent.Init);

	[RegisterConfigEvent]
	public static ConfigEventRegistry LocalVersionInit => new ConfigEventRegistry(LocalVersionConfig, (ConfigEventArgs e) =>
	{
		_UpdateConfigVersion(LocalVersionConfig, "本地", (int)e.NewValue);
	}, ConfigEvent.Init);

	public static ConfigItem<int> SharedVersionConfig => field ?? (field = GetConfigItem<int>("FileVersion"));

	public static ConfigItem<int> LocalVersionConfig => field ?? (field = GetConfigItem<int>("LocalFileVersion"));

	public string Identifier => "config";

	public string Name => "配置";

	public bool SupportAsync => true;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	}

	private static ILifecycleService Service => Context.ServiceInstance;

	public static bool TryGetConfigItemNoType(string key, [NotNullWhen(true)] out ConfigItem? item)
	{
		return _Items.TryGetValue(key, out item);
	}

	public static bool TryGetConfigItem<TValue>(string key, out ConfigItem<TValue>? item)
	{
		if (!_isConfigItemsInitialized)
		{
			throw new InvalidOperationException("Not initialized");
		}
		bool flag = TryGetConfigItemNoType(key, out ConfigItem item2);
		item = (flag ? (item2 as ConfigItem<TValue>) : null);
		return flag;
	}

	public static ConfigItem<TValue> GetConfigItem<TValue>(string key)
	{
		if (!TryGetConfigItem(key, out ConfigItem<TValue> item))
		{
			throw new KeyNotFoundException("Config key not found: '" + key + "'");
		}
		return item ?? throw new InvalidCastException("Type of '" + key + "' is incompatible with " + typeof(TValue).FullName);
	}

	public static void TrySetValue(string key, object value, object? argument = null)
	{
		if (TryGetConfigItemNoType(key, out ConfigItem item))
		{
			if (item.Type.IsEnum && !(value is string))
			{
				item.SetValueNoType(Enum.ToObject(item.Type, value), argument);
			}
			else
			{
				item.SetValueNoType(value, argument);
			}
		}
	}

	public static void RegisterObserver(IConfigScope scope, ConfigObserver observer)
	{
		foreach (string item in scope.CheckScope(KeySet))
		{
			_Items[item].Observe(observer);
		}
	}

	public static IConfigProvider GetProvider(ConfigSource source)
	{
		if (!_isProvidersInitialized)
		{
			throw new InvalidOperationException("Not initialized");
		}
		return source switch
		{
			ConfigSource.Shared => (IConfigProvider)_sharedConfigProvider, 
			ConfigSource.SharedEncrypt => _sharedEncryptedConfigProvider, 
			ConfigSource.Local => _localConfigProvider, 
			ConfigSource.GameInstance => _instanceConfigProvider, 
			_ => throw new ArgumentException($"Invalid source: {source}"), 
		};
	}

	private static void _InitializeProviders()
	{
		Action[] source = new Action[3]
		{
			() =>
			{
				if (!File.Exists(SharedConfigPath))
				{
					string[] source2 = new string[2]
					{
						Path.Combine(Paths.OldSharedData, "Config.json"),
						Path.Combine(Paths.SharedData, "config.json")
					};
					_TryMigrate(SharedConfigPath, source2.Select((string path) => new ConfigMigration
					{
						From = path,
						To = SharedConfigPath,
						OnMigration = SharedJsonMigration
					}));
				}
				_sharedEncryptedConfigProvider = new EncryptedFileConfigStorage(_sharedConfigProvider = new FileConfigStorage(new JsonFileProvider(SharedConfigPath)));
			},
			() =>
			{
				if (!File.Exists(LocalConfigPath))
				{
					_TryMigrate(LocalConfigPath, new _003C_003Ez__ReadOnlySingleElementList<ConfigMigration>(new ConfigMigration
					{
						From = Path.Combine(Paths.Data, "setup.ini"),
						To = LocalConfigPath,
						OnMigration = CatIniMigration
					}));
				}
				_localConfigProvider = new FileConfigStorage(new YamlFileProvider(LocalConfigPath));
			},
			() =>
			{
				_instanceConfigProvider = new DynamicCacheConfigStorage
				{
					StorageFactory = (object? argument) =>
					{
						ArgumentNullException.ThrowIfNull(argument, "argument");
						string fullPath = Path.GetFullPath(argument.ToString());
						string text = Path.Combine(fullPath, "PCL", "config.v1.yml");
						if (!File.Exists(fullPath))
						{
							_TryMigrate(fullPath, new _003C_003Ez__ReadOnlySingleElementList<ConfigMigration>(new ConfigMigration
							{
								From = Path.Combine(fullPath, "PCL", "setup.ini"),
								To = text,
								OnMigration = CatIniMigration
							}));
						}
						return new FileConfigStorage(new YamlFileProvider(text));
					}
				};
			}
		};
		try
		{
			Task.WaitAll(source.Select(Task.Run).ToArray());
		}
		catch (AggregateException ex)
		{
			throw ex.GetBaseException();
		}
		static void CatIniMigration(string from, string to)
		{
			string[] array = File.ReadAllLines(from);
			YamlFileProvider yamlFileProvider = new YamlFileProvider(to);
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!text.IsNullOrWhiteSpace())
				{
					string[] array3 = text.Split(':', 2);
					if (array3.Length == 2)
					{
						yamlFileProvider.Set(array3[0], array3[1]);
					}
				}
			}
			yamlFileProvider.Sync();
		}
		static void SharedJsonMigration(string from, string to)
		{
			File.Copy(from, to);
		}
	}

	private static void _TryMigrate(string target, IEnumerable<ConfigMigration> migrations)
	{
		Context.Info("Try migrating config: " + target);
		try
		{
			if (!ConfigMigration.Migrate(target, migrations))
			{
				Context.Info("No migration solution available");
			}
		}
		catch (Exception ex)
		{
			Context.Warn("Migration failed", ex);
		}
	}

	[LifecycleStart]
	private static void _Start()
	{
		if (IsInitialized)
		{
			return;
		}
		Context.Info("Config initialization started");
		try
		{
			Context.Trace("Initializing config items...");
			_InitializeConfigItems();
			Context.Debug($"Finished initialize {_Items.Count} item(s)");
			_isConfigItemsInitialized = true;
			Context.Trace("Initializing providers...");
			_InitializeProviders();
			_isProvidersInitialized = true;
			Context.Trace("Initializing observers...");
			_InitializeObservers();
			Context.Info("Invoking init events...");
			foreach (KeyValuePair<string, ConfigItem> item in _Items)
			{
				item.Deconstruct(out var _, out var value);
				value.TriggerEvent(ConfigEvent.Init, null, bypassOldValue: true, fillNewValue: true);
			}
			IsInitialized = true;
		}
		catch (Exception ex)
		{
			string text;
			if (_isConfigItemsInitialized)
			{
				text = "OBSERVER";
			}
			else
			{
				text = (_isProvidersInitialized ? "CONFIG_ITEM" : "PROVIDER");
			}
			string message;
			if (ex is ConfigFileInitException ex2)
			{
				string path = ex2.Path;
				string text2 = ex2.Path + ".failbackup";
				string text3 = ex2.Path + ".bak";
				File.Move(path, text2, overwrite: true);
				if (File.Exists(text3))
				{
					File.Copy(text3, path, overwrite: true);
				}
				message = Lang.Text("Config.Error.InvalidFormat.RecoveryMessage", text, path, text2);
			}
			else
			{
				message = Lang.Text("Config.Error.LoadFailed.Message", text);
			}
			Context.Fatal(message, ex);
		}
	}

	[LifecycleStop]
	private static void _Stop()
	{
		Exception serviceLastException = Lifecycle.GetServiceLastException(Service.Identifier);
		if (serviceLastException != null)
		{
			Context.Fatal(Lang.Text("Config.Error.LoadFailed.Title"), serviceLastException);
			return;
		}
		Context.Info("Saving config...");
		_sharedConfigProvider?.Stop();
		_localConfigProvider?.Stop();
		_instanceConfigProvider?.Stop();
	}

	private static void _UpdateConfigVersion(ConfigItem<int> versionConfig, string name, int fileVersion)
	{
		int defaultValue = versionConfig.DefaultValue;
		bool flag = versionConfig.IsDefault();
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 3);
		defaultInterpolatedStringHandler.AppendFormatted(name);
		defaultInterpolatedStringHandler.AppendLiteral("配置: 文件版本 ");
		defaultInterpolatedStringHandler.AppendFormatted(flag ? "UNSET" : ((object)fileVersion));
		defaultInterpolatedStringHandler.AppendLiteral(", 目标版本 ");
		defaultInterpolatedStringHandler.AppendFormatted(defaultValue);
		LogWrapper.Info(defaultInterpolatedStringHandler.ToStringAndClear());
		if (flag || defaultValue != fileVersion)
		{
			versionConfig.SetValue(defaultValue);
		}
	}

	private static void _InitializeConfigItems()
	{
		(string, ConfigItem)[] array = new (string, ConfigItem)[251]
		{
			("Identify", new ConfigItem<string>("Identify", "", ConfigSource.Shared)),
			("CustomVariables", new ConfigItem<Dictionary<string, string>>("CustomVariables", () => new Dictionary<string, string>(), ConfigSource.Local)),
			("SystemDisableHardwareAcceleration", new ConfigItem<bool>("SystemDisableHardwareAcceleration", defaultValue: false, ConfigSource.Shared)),
			("UiToneProfiles", new ConfigItem<ToneProfileConfig>("UiToneProfiles", () => new ToneProfileConfig(), ConfigSource.Local)),
			("SystemTelemetry", new ConfigItem<bool>("SystemTelemetry", defaultValue: false, ConfigSource.Shared)),
			("HintDownloadThread", new ConfigItem<bool>("HintDownloadThread", defaultValue: false, ConfigSource.Shared)),
			("FileVersion", new ConfigItem<int>("FileVersion", 1, ConfigSource.Shared)),
			("SystemMaxLog", new ConfigItem<int>("SystemMaxLog", 13, ConfigSource.Shared)),
			("LocalFileVersion", new ConfigItem<int>("LocalFileVersion", 1, ConfigSource.Local)),
			("HintRenderer", new ConfigItem<bool>("HintRenderer", defaultValue: false, ConfigSource.Shared)),
			("UiAniFPS", new ConfigItem<int>("UiAniFPS", 59, ConfigSource.Shared)),
			("Profile", new ConfigItem<string>("Profile", "{\"lastUsed\":-1,\"profiles\":[]}", ConfigSource.SharedEncrypt)),
			("HintDebugLog4j2Config", new ConfigItem<bool>("HintDebugLog4j2Config", defaultValue: false, ConfigSource.Shared)),
			("SystemNetEnableDoH", new ConfigItem<bool>("SystemNetEnableDoH", defaultValue: true, ConfigSource.Shared)),
			("HintInstallBack", new ConfigItem<bool>("HintInstallBack", defaultValue: false, ConfigSource.Shared)),
			("SystemHttpProxy", new ConfigItem<string>("SystemHttpProxy", "", ConfigSource.SharedEncrypt)),
			("HintHide", new ConfigItem<bool>("HintHide", defaultValue: false, ConfigSource.Shared)),
			("SystemHttpProxyType", new ConfigItem<int>("SystemHttpProxyType", 1, ConfigSource.Shared)),
			("HintHandInstall", new ConfigItem<bool>("HintHandInstall", defaultValue: false, ConfigSource.Shared)),
			("SystemHttpProxyCustomUsername", new ConfigItem<string>("SystemHttpProxyCustomUsername", "", ConfigSource.Shared)),
			("SystemHttpProxyCustomPassword", new ConfigItem<string>("SystemHttpProxyCustomPassword", "", ConfigSource.Shared)),
			("HintClearRubbish", new ConfigItem<int>("HintClearRubbish", 0, ConfigSource.Shared)),
			("HintUpdateMod", new ConfigItem<bool>("HintUpdateMod", defaultValue: false, ConfigSource.Shared)),
			("SystemDebugMode", new ConfigItem<bool>("SystemDebugMode", defaultValue: false, ConfigSource.Shared)),
			("SystemDebugAnim", new ConfigItem<int>("SystemDebugAnim", 9, ConfigSource.Shared)),
			("HintCustomCommand", new ConfigItem<bool>("HintCustomCommand", defaultValue: false, ConfigSource.Shared)),
			("SystemDebugDelay", new ConfigItem<bool>("SystemDebugDelay", defaultValue: false, ConfigSource.Shared)),
			("HintCustomWarn", new ConfigItem<bool>("HintCustomWarn", defaultValue: false, ConfigSource.Shared)),
			("SystemDebugSkipCopy", new ConfigItem<bool>("SystemDebugSkipCopy", defaultValue: false, ConfigSource.Shared)),
			("SystemDebugAllowRestrictedFeature", new ConfigItem<bool>("SystemDebugAllowRestrictedFeature", defaultValue: false, ConfigSource.Shared)),
			("HintMoreAdvancedSetup", new ConfigItem<bool>("HintMoreAdvancedSetup", defaultValue: false, ConfigSource.Shared)),
			("HintIndieSetup", new ConfigItem<bool>("HintIndieSetup", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadThread", new ConfigItem<int>("ToolDownloadThread", 63, ConfigSource.Shared)),
			("ToolDownloadFileConnection", new ConfigItem<int>("ToolDownloadFileConnection", 7, ConfigSource.Shared)),
			("HintProfileSelect", new ConfigItem<bool>("HintProfileSelect", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadSpeed", new ConfigItem<int>("ToolDownloadSpeed", 42, ConfigSource.Shared)),
			("HintExportConfig", new ConfigItem<bool>("HintExportConfig", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadHttpMode", new ConfigItem<DownloadHttpMode>("ToolDownloadHttpMode", DownloadHttpMode.Auto, ConfigSource.Shared)),
			("ToolDownloadSource", new ConfigItem<int>("ToolDownloadSource", 1, ConfigSource.Shared)),
			("HintMaxLog", new ConfigItem<bool>("HintMaxLog", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadVersion", new ConfigItem<int>("ToolDownloadVersion", 1, ConfigSource.Shared)),
			("HintDisableGamePathCheckTip", new ConfigItem<bool>("HintDisableGamePathCheckTip", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadAutoSelectVersion", new ConfigItem<bool>("ToolDownloadAutoSelectVersion", defaultValue: true, ConfigSource.Shared)),
			("ToolFixAuthlib", new ConfigItem<bool>("ToolFixAuthlib", defaultValue: true, ConfigSource.Shared)),
			("UiLauncherCEHint", new ConfigItem<bool>("UiLauncherCEHint", defaultValue: true, ConfigSource.Shared)),
			("UiSchematicFirstTimeHintShown", new ConfigItem<bool>("UiSchematicFirstTimeHintShown", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadTranslate", new ConfigItem<int>("ToolDownloadTranslate", 0, ConfigSource.Shared)),
			("ToolDownloadTranslateV2", new ConfigItem<int>("ToolDownloadTranslateV2", 1, ConfigSource.Shared)),
			("SystemSystemAnnouncement", new ConfigItem<string>("SystemSystemAnnouncement", "", ConfigSource.Shared)),
			("ToolDownloadIgnoreQuilt", new ConfigItem<bool>("ToolDownloadIgnoreQuilt", defaultValue: true, ConfigSource.Shared)),
			("ToolDownloadAutoInstallDependencies", new ConfigItem<bool>("ToolDownloadAutoInstallDependencies", defaultValue: true, ConfigSource.Shared)),
			("ToolDownloadAutoInstallOptionalDependencies", new ConfigItem<bool>("ToolDownloadAutoInstallOptionalDependencies", defaultValue: false, ConfigSource.Shared)),
			("HintDatapackUpdate", new ConfigItem<bool>("HintDatapackUpdate", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadClipboard", new ConfigItem<bool>("ToolDownloadClipboard", defaultValue: false, ConfigSource.Shared)),
			("ToolDownloadMod", new ConfigItem<int>("ToolDownloadMod", 1, ConfigSource.Shared)),
			("CacheJavaListVersion", new ConfigItem<int>("CacheJavaListVersion", 0, ConfigSource.Shared)),
			("ToolModLocalNameStyle", new ConfigItem<int>("ToolModLocalNameStyle", 0, ConfigSource.Shared)),
			("ToolDownloadQuickBehavior", new ConfigItem<int>("ToolDownloadQuickBehavior", 0, ConfigSource.Shared)),
			("CacheDrops", new ConfigItem<string>("CacheDrops", "", ConfigSource.Shared)),
			("LaunchInstanceSelect", new ConfigItem<string>("LaunchInstanceSelect", "", ConfigSource.Local)),
			("ToolHelpChinese", new ConfigItem<bool>("ToolHelpChinese", defaultValue: true, ConfigSource.Shared)),
			("LaunchFolderSelect", new ConfigItem<string>("LaunchFolderSelect", "", ConfigSource.Local)),
			("ToolUpdateRelease", new ConfigItem<bool>("ToolUpdateRelease", defaultValue: false, ConfigSource.Shared)),
			("ToolUpdateSnapshot", new ConfigItem<bool>("ToolUpdateSnapshot", defaultValue: false, ConfigSource.Shared)),
			("LaunchFolders", new ConfigItem<string>("LaunchFolders", "", ConfigSource.Shared)),
			("LaunchArgumentJavaUser", new ConfigItem<string>("LaunchArgumentJavaUser", "[]", ConfigSource.Shared)),
			("SystemSystemUpdate", new ConfigItem<LauncherAutoUpdateBehavior>("SystemSystemUpdate", LauncherAutoUpdateBehavior.DownloadAndAnnounce, ConfigSource.Local)),
			("CompFavorites", new ConfigItem<string>("CompFavorites", "[]", ConfigSource.Shared)),
			("SystemUpdateChannel", new ConfigItem<UpdateChannel>("SystemUpdateChannel", UpdateChannel.Release, ConfigSource.Local)),
			("CacheSavedPageUrl", new ConfigItem<string>("CacheSavedPageUrl", "", ConfigSource.Shared)),
			("SystemMirrorChyanKey", new ConfigItem<string>("SystemMirrorChyanKey", "", ConfigSource.SharedEncrypt)),
			("CacheSavedPageVersion", new ConfigItem<string>("CacheSavedPageVersion", "", ConfigSource.Shared)),
			("WindowHeight", new ConfigItem<double>("WindowHeight", 550.0, ConfigSource.Local)),
			("LinkUsername", new ConfigItem<string>("LinkUsername", "", ConfigSource.Shared)),
			("WindowWidth", new ConfigItem<double>("WindowWidth", 900.0, ConfigSource.Local)),
			("LinkRelayType", new ConfigItem<LinkRelayBehavior>("LinkRelayType", LinkRelayBehavior.Default, ConfigSource.Shared)),
			("LinkServerType", new ConfigItem<int>("LinkServerType", 1, ConfigSource.Shared)),
			("LaunchUuid", new ConfigItem<string>("LaunchUuid", "", ConfigSource.Shared)),
			("LinkLatencyFirstMode", new ConfigItem<bool>("LinkLatencyFirstMode", defaultValue: true, ConfigSource.Shared)),
			("CacheExportConfig", new ConfigItem<string>("CacheExportConfig", "", ConfigSource.Shared)),
			("LinkRelayServer", new ConfigItem<string>("LinkRelayServer", "", ConfigSource.Shared)),
			("SystemEula", new ConfigItem<bool>("SystemEula", defaultValue: false, ConfigSource.Shared)),
			("SystemCount", new ConfigItem<int>("SystemCount", 0, ConfigSource.SharedEncrypt)),
			("LinkProtocolPreference", new ConfigItem<LinkProtocolPreference>("LinkProtocolPreference", LinkProtocolPreference.Tcp, ConfigSource.Shared)),
			("SystemLaunchCount", new ConfigItem<int>("SystemLaunchCount", 0, ConfigSource.SharedEncrypt)),
			("LinkTryPunchSym", new ConfigItem<bool>("LinkTryPunchSym", defaultValue: true, ConfigSource.Shared)),
			("SystemLastVersionReg", new ConfigItem<int>("SystemLastVersionReg", 0, ConfigSource.SharedEncrypt)),
			("LinkEnableIPv6", new ConfigItem<bool>("LinkEnableIPv6", defaultValue: true, ConfigSource.Shared)),
			("LinkEnableCliOutput", new ConfigItem<bool>("LinkEnableCliOutput", defaultValue: false, ConfigSource.Shared)),
			("SystemHighestBetaVersionReg", new ConfigItem<int>("SystemHighestBetaVersionReg", 0, ConfigSource.SharedEncrypt)),
			("UiLauncherLogo", new ConfigItem<bool>("UiLauncherLogo", defaultValue: true, ConfigSource.Local)),
			("SystemHighestAlphaVersionReg", new ConfigItem<int>("SystemHighestAlphaVersionReg", 0, ConfigSource.SharedEncrypt)),
			("UiLockWindowSize", new ConfigItem<bool>("UiLockWindowSize", defaultValue: false, ConfigSource.Shared)),
			("SystemSetupVersionReg", new ConfigItem<int>("SystemSetupVersionReg", 1, ConfigSource.Shared)),
			("UiShowLaunchingHint", new ConfigItem<bool>("UiShowLaunchingHint", defaultValue: true, ConfigSource.Local)),
			("SystemSetupVersionIni", new ConfigItem<int>("SystemSetupVersionIni", 1, ConfigSource.Local)),
			("UiLogoType", new ConfigItem<LauncherTitleType>("UiLogoType", LauncherTitleType.Default, ConfigSource.Local)),
			("SystemSystemActivity", new ConfigItem<int>("SystemSystemActivity", 0, ConfigSource.Local)),
			("UiLogoText", new ConfigItem<string>("UiLogoText", "", ConfigSource.Local)),
			("CacheDownloadFolder", new ConfigItem<string>("CacheDownloadFolder", "", ConfigSource.Shared)),
			("UiLogoLeft", new ConfigItem<bool>("UiLogoLeft", defaultValue: false, ConfigSource.Local)),
			("ToolDownloadCustomUserAgent", new ConfigItem<string>("ToolDownloadCustomUserAgent", "", ConfigSource.Shared)),
			("UiFont", new ConfigItem<string>("UiFont", "", ConfigSource.Local)),
			("ToolUpdateReleaseLast", new ConfigItem<string>("ToolUpdateReleaseLast", "", ConfigSource.Shared)),
			("UiMotdFont", new ConfigItem<string>("UiMotdFont", "", ConfigSource.Local)),
			("ToolUpdateSnapshotLast", new ConfigItem<string>("ToolUpdateSnapshotLast", "", ConfigSource.Shared)),
			("DetailedInstanceClassification", new ConfigItem<bool>("DetailedInstanceClassification", defaultValue: false, ConfigSource.Local)),
			("LinkEula", new ConfigItem<bool>("LinkEula", defaultValue: false, ConfigSource.Shared)),
			("LinkAnnounceCache", new ConfigItem<string>("LinkAnnounceCache", "", ConfigSource.SharedEncrypt)),
			("UiLanguage", new ConfigItem<string>("UiLanguage", "auto", ConfigSource.Local)),
			("LinkAnnounceCacheVer", new ConfigItem<int>("LinkAnnounceCacheVer", 0, ConfigSource.Shared)),
			("UiFormatCulture", new ConfigItem<string>("UiFormatCulture", "auto", ConfigSource.Local)),
			("LinkNaidRefreshToken", new ConfigItem<string>("LinkNaidRefreshToken", "", ConfigSource.SharedEncrypt)),
			("UiRegion", new ConfigItem<string>("UiRegion", "auto", ConfigSource.Local)),
			("LinkNaidRefreshExpiresAt", new ConfigItem<string>("LinkNaidRefreshExpiresAt", "", ConfigSource.SharedEncrypt)),
			("UiDarkMode", new ConfigItem<ColorMode>("UiDarkMode", ColorMode.System, ConfigSource.Shared)),
			("LinkFirstTimeNetTest", new ConfigItem<bool>("LinkFirstTimeNetTest", defaultValue: true, ConfigSource.SharedEncrypt)),
			("UiDarkColor", new ConfigItem<ColorTheme>("UiDarkColor", ColorTheme.CatBlue, ConfigSource.Shared)),
			("VersionLaunchCount", new ConfigItem<int>("VersionLaunchCount", 0, ConfigSource.GameInstance)),
			("UiLightColor", new ConfigItem<ColorTheme>("UiLightColor", ColorTheme.CatBlue, ConfigSource.Shared)),
			("IsStar", new ConfigItem<bool>("IsStar", defaultValue: false, ConfigSource.GameInstance)),
			("DisplayType", new ConfigItem<int>("DisplayType", 0, ConfigSource.GameInstance)),
			("UiLauncherTransparent", new ConfigItem<int>("UiLauncherTransparent", 600, ConfigSource.Local)),
			("Logo", new ConfigItem<string>("Logo", "", ConfigSource.GameInstance)),
			("LogoCustom", new ConfigItem<bool>("LogoCustom", defaultValue: false, ConfigSource.GameInstance)),
			("UiLauncherHue", new ConfigItem<int>("UiLauncherHue", 180, ConfigSource.Local)),
			("CustomInfo", new ConfigItem<string>("CustomInfo", "", ConfigSource.GameInstance)),
			("Info", new ConfigItem<string>("Info", "", ConfigSource.GameInstance)),
			("UiLauncherSat", new ConfigItem<int>("UiLauncherSat", 80, ConfigSource.Local)),
			("ReleaseTime", new ConfigItem<string>("ReleaseTime", "", ConfigSource.GameInstance)),
			("State", new ConfigItem<int>("State", 0, ConfigSource.GameInstance)),
			("UiLauncherLight", new ConfigItem<int>("UiLauncherLight", 20, ConfigSource.Local)),
			("VersionFabric", new ConfigItem<string>("VersionFabric", "", ConfigSource.GameInstance)),
			("VersionLegacyFabric", new ConfigItem<string>("VersionLegacyFabric", "", ConfigSource.GameInstance)),
			("UiLauncherDelta", new ConfigItem<int>("UiLauncherDelta", 90, ConfigSource.Local)),
			("VersionQuilt", new ConfigItem<string>("VersionQuilt", "", ConfigSource.GameInstance)),
			("VersionLabyMod", new ConfigItem<string>("VersionLabyMod", "", ConfigSource.GameInstance)),
			("UiLauncherTheme", new ConfigItem<int>("UiLauncherTheme", 0, ConfigSource.Local)),
			("VersionOptiFine", new ConfigItem<string>("VersionOptiFine", "", ConfigSource.GameInstance)),
			("VersionLiteLoader", new ConfigItem<bool>("VersionLiteLoader", defaultValue: false, ConfigSource.GameInstance)),
			("VersionForge", new ConfigItem<string>("VersionForge", "", ConfigSource.GameInstance)),
			("UiBackgroundColorful", new ConfigItem<bool>("UiBackgroundColorful", defaultValue: true, ConfigSource.Local)),
			("VersionNeoForge", new ConfigItem<string>("VersionNeoForge", "", ConfigSource.GameInstance)),
			("VersionCleanroom", new ConfigItem<string>("VersionCleanroom", "", ConfigSource.GameInstance)),
			("UiBackgroundOpacity", new ConfigItem<int>("UiBackgroundOpacity", 1000, ConfigSource.Local)),
			("VersionVanillaName", new ConfigItem<string>("VersionVanillaName", "Unknown", ConfigSource.GameInstance)),
			("VersionVanilla", new ConfigItem<string>("VersionVanilla", "0.0.0", ConfigSource.GameInstance)),
			("UiBackgroundCarousel", new ConfigItem<int>("UiBackgroundCarousel", 1000, ConfigSource.Local)),
			("VersionModpackVersion", new ConfigItem<string>("VersionModpackVersion", "", ConfigSource.GameInstance)),
			("VersionModpackSource", new ConfigItem<string>("VersionModpackSource", "", ConfigSource.GameInstance)),
			("UiBackgroundBlur", new ConfigItem<int>("UiBackgroundBlur", 0, ConfigSource.Local)),
			("VersionModpackId", new ConfigItem<string>("VersionModpackId", "", ConfigSource.GameInstance)),
			("UiBackgroundSuit", new ConfigItem<int>("UiBackgroundSuit", 0, ConfigSource.Local)),
			("UiAutoPauseVideo", new ConfigItem<bool>("UiAutoPauseVideo", defaultValue: true, ConfigSource.Local)),
			("UiBlur", new ConfigItem<bool>("UiBlur", defaultValue: false, ConfigSource.Local)),
			("UiBlurValue", new ConfigItem<int>("UiBlurValue", 16, ConfigSource.Local)),
			("UiBlurSamplingRate", new ConfigItem<int>("UiBlurSamplingRate", 70, ConfigSource.Local)),
			("UiBlurType", new ConfigItem<int>("UiBlurType", 0, ConfigSource.Local)),
			("UiCustomType", new ConfigItem<int>("UiCustomType", 0, ConfigSource.Local)),
			("UiCustomPreset", new ConfigItem<int>("UiCustomPreset", 0, ConfigSource.Local)),
			("UiCustomNet", new ConfigItem<string>("UiCustomNet", "", ConfigSource.Local)),
			("UiMusicVolume", new ConfigItem<int>("UiMusicVolume", 500, ConfigSource.Local)),
			("UiMusicStop", new ConfigItem<bool>("UiMusicStop", defaultValue: false, ConfigSource.Local)),
			("UiMusicStart", new ConfigItem<bool>("UiMusicStart", defaultValue: false, ConfigSource.Local)),
			("UiMusicAuto", new ConfigItem<bool>("UiMusicAuto", defaultValue: true, ConfigSource.Local)),
			("UiMusicRandom", new ConfigItem<bool>("UiMusicRandom", defaultValue: true, ConfigSource.Local)),
			("UiMusicSMTC", new ConfigItem<bool>("UiMusicSMTC", defaultValue: true, ConfigSource.Local)),
			("UiHiddenPageDownload", new ConfigItem<bool>("UiHiddenPageDownload", defaultValue: false, ConfigSource.Local)),
			("UiHiddenPageSetup", new ConfigItem<bool>("UiHiddenPageSetup", defaultValue: false, ConfigSource.Local)),
			("UiHiddenPageTools", new ConfigItem<bool>("UiHiddenPageTools", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupLaunch", new ConfigItem<bool>("UiHiddenSetupLaunch", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupUi", new ConfigItem<bool>("UiHiddenSetupUi", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupLauncherLanguage", new ConfigItem<bool>("UiHiddenSetupLauncherLanguage", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupLauncherMisc", new ConfigItem<bool>("UiHiddenSetupLauncherMisc", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupGameManage", new ConfigItem<bool>("UiHiddenSetupGameManage", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupJava", new ConfigItem<bool>("UiHiddenSetupJava", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupUpdate", new ConfigItem<bool>("UiHiddenSetupUpdate", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupGameLink", new ConfigItem<bool>("UiHiddenSetupGameLink", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupAbout", new ConfigItem<bool>("UiHiddenSetupAbout", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupFeedback", new ConfigItem<bool>("UiHiddenSetupFeedback", defaultValue: false, ConfigSource.Local)),
			("UiHiddenSetupLog", new ConfigItem<bool>("UiHiddenSetupLog", defaultValue: false, ConfigSource.Local)),
			("UiHiddenToolsGameLink", new ConfigItem<bool>("UiHiddenToolsGameLink", defaultValue: false, ConfigSource.Local)),
			("UiHiddenToolsTest", new ConfigItem<bool>("UiHiddenToolsTest", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionEdit", new ConfigItem<bool>("UiHiddenVersionEdit", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionExport", new ConfigItem<bool>("UiHiddenVersionExport", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionSave", new ConfigItem<bool>("UiHiddenVersionSave", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionScreenshot", new ConfigItem<bool>("UiHiddenVersionScreenshot", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionMod", new ConfigItem<bool>("UiHiddenVersionMod", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionResourcePack", new ConfigItem<bool>("UiHiddenVersionResourcePack", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionShader", new ConfigItem<bool>("UiHiddenVersionShader", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionSchematic", new ConfigItem<bool>("UiHiddenVersionSchematic", defaultValue: false, ConfigSource.Local)),
			("UiHiddenVersionServer", new ConfigItem<bool>("UiHiddenVersionServer", defaultValue: false, ConfigSource.Local)),
			("UiHiddenFunctionSelect", new ConfigItem<bool>("UiHiddenFunctionSelect", defaultValue: false, ConfigSource.Local)),
			("UiHiddenFunctionModUpdate", new ConfigItem<bool>("UiHiddenFunctionModUpdate", defaultValue: false, ConfigSource.Local)),
			("UiHiddenFunctionHidden", new ConfigItem<bool>("UiHiddenFunctionHidden", defaultValue: false, ConfigSource.Local)),
			("LaunchRamType", new ConfigItem<int>("LaunchRamType", 0, ConfigSource.Local)),
			("LaunchRamCustom", new ConfigItem<int>("LaunchRamCustom", 15, ConfigSource.Local)),
			("LaunchRamCustomInitial", new ConfigItem<int>("LaunchRamCustomInitial", 0, ConfigSource.Local)),
			("LaunchAdvanceLockMemory", new ConfigItem<bool>("LaunchAdvanceLockMemory", defaultValue: false, ConfigSource.Local)),
			("LaunchPreferredIpStack", new ConfigItem<JvmPreferredIpStack>("LaunchPreferredIpStack", JvmPreferredIpStack.Default, ConfigSource.Shared)),
			("LaunchAdvanceJvm", new ConfigItem<string>("LaunchAdvanceJvm", "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow -Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True -Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true", ConfigSource.Local)),
			("LaunchAdvanceGame", new ConfigItem<string>("LaunchAdvanceGame", "", ConfigSource.Local)),
			("LaunchAdvanceRun", new ConfigItem<string>("LaunchAdvanceRun", "", ConfigSource.Local)),
			("LaunchAdvanceRunWait", new ConfigItem<bool>("LaunchAdvanceRunWait", defaultValue: true, ConfigSource.Local)),
			("LaunchAdvanceDisableJLW", new ConfigItem<bool>("LaunchAdvanceDisableJLW", defaultValue: true, ConfigSource.Local)),
			("LaunchAdvanceDisableLF", new ConfigItem<bool>("LaunchAdvanceDisableLF", defaultValue: false, ConfigSource.Local)),
			("LaunchAdvanceGraphicCard", new ConfigItem<bool>("LaunchAdvanceGraphicCard", defaultValue: true, ConfigSource.Shared)),
			("LaunchAdvanceNoJavaw", new ConfigItem<bool>("LaunchAdvanceNoJavaw", defaultValue: false, ConfigSource.Shared)),
			("LaunchAdvanceDisableLwjglUnsafeAgent", new ConfigItem<bool>("LaunchAdvanceDisableLwjglUnsafeAgent", defaultValue: false, ConfigSource.Shared)),
			("LaunchAdvanceDisableCrashAnalysis", new ConfigItem<bool>("LaunchAdvanceDisableCrashAnalysis", defaultValue: false, ConfigSource.Local)),
			("LaunchAdvanceRenderer", new ConfigItem<int>("LaunchAdvanceRenderer", 0, ConfigSource.Local)),
			("LaunchArgumentTitle", new ConfigItem<string>("LaunchArgumentTitle", "", ConfigSource.Local)),
			("LaunchArgumentInfo", new ConfigItem<string>("LaunchArgumentInfo", "PCLCE", ConfigSource.Local)),
			("LaunchArgumentJavaSelect", new ConfigItem<string>("LaunchArgumentJavaSelect", "", ConfigSource.Shared)),
			("LaunchArgumentIndieV2", new ConfigItem<int>("LaunchArgumentIndieV2", 4, ConfigSource.Local)),
			("LaunchArgumentVisible", new ConfigItem<LauncherVisibility>("LaunchArgumentVisible", LauncherVisibility.DoNothing, ConfigSource.Shared)),
			("LaunchArgumentPriority", new ConfigItem<GameProcessPriority>("LaunchArgumentPriority", GameProcessPriority.Normal, ConfigSource.Shared)),
			("LaunchArgumentWindowWidth", new ConfigItem<int>("LaunchArgumentWindowWidth", 854, ConfigSource.Local)),
			("LaunchArgumentWindowHeight", new ConfigItem<int>("LaunchArgumentWindowHeight", 480, ConfigSource.Local)),
			("LaunchArgumentWindowType", new ConfigItem<GameWindowSizeMode>("LaunchArgumentWindowType", GameWindowSizeMode.Default, ConfigSource.Local)),
			("LoginMsAuthType", new ConfigItem<int>("LoginMsAuthType", 1, ConfigSource.Shared)),
			("VersionAdvanceJvm", new ConfigItem<string>("VersionAdvanceJvm", "", ConfigSource.GameInstance)),
			("VersionAdvanceGame", new ConfigItem<string>("VersionAdvanceGame", "", ConfigSource.GameInstance)),
			("VersionAdvanceRenderer", new ConfigItem<int>("VersionAdvanceRenderer", 0, ConfigSource.GameInstance)),
			("VersionAdvanceAssets", new ConfigItem<int>("VersionAdvanceAssets", 0, ConfigSource.GameInstance)),
			("VersionAdvanceAssetsV2", new ConfigItem<bool>("VersionAdvanceAssetsV2", defaultValue: false, ConfigSource.GameInstance)),
			("VersionAdvanceJava", new ConfigItem<bool>("VersionAdvanceJava", defaultValue: false, ConfigSource.GameInstance)),
			("VersionAdvanceDisableJlw", new ConfigItem<bool>("VersionAdvanceDisableJlw", defaultValue: false, ConfigSource.GameInstance)),
			("VersionAdvanceRun", new ConfigItem<string>("VersionAdvanceRun", "", ConfigSource.GameInstance)),
			("VersionAdvanceClasspathHead", new ConfigItem<string>("VersionAdvanceClasspathHead", "", ConfigSource.GameInstance)),
			("VersionAdvanceRunWait", new ConfigItem<bool>("VersionAdvanceRunWait", defaultValue: true, ConfigSource.GameInstance)),
			("VersionAdvanceDisableJLW", new ConfigItem<bool>("VersionAdvanceDisableJLW", defaultValue: false, ConfigSource.GameInstance)),
			("VersionAdvanceDisableLwjglUnsafeAgent", new ConfigItem<bool>("VersionAdvanceDisableLwjglUnsafeAgent", defaultValue: false, ConfigSource.GameInstance)),
			("VersionAdvanceUseProxyV2", new ConfigItem<bool>("VersionAdvanceUseProxyV2", defaultValue: false, ConfigSource.GameInstance)),
			("VersionAdvanceDisableLF", new ConfigItem<bool>("VersionAdvanceDisableLF", defaultValue: false, ConfigSource.GameInstance)),
			("VersionUseDebugLog4j2Config", new ConfigItem<bool>("VersionUseDebugLog4j2Config", defaultValue: false, ConfigSource.GameInstance)),
			("VersionRamType", new ConfigItem<int>("VersionRamType", 2, ConfigSource.GameInstance)),
			("VersionRamCustom", new ConfigItem<int>("VersionRamCustom", 15, ConfigSource.GameInstance)),
			("VersionRamCustomInitial", new ConfigItem<int>("VersionRamCustomInitial", 0, ConfigSource.GameInstance)),
			("VersionArgumentTitle", new ConfigItem<string>("VersionArgumentTitle", "", ConfigSource.GameInstance)),
			("VersionArgumentTitleEmpty", new ConfigItem<bool>("VersionArgumentTitleEmpty", defaultValue: false, ConfigSource.GameInstance)),
			("VersionArgumentInfo", new ConfigItem<string>("VersionArgumentInfo", "", ConfigSource.GameInstance)),
			("VersionArgumentIndie", new ConfigItem<int>("VersionArgumentIndie", -1, ConfigSource.GameInstance)),
			("VersionArgumentIndieV2", new ConfigItem<bool>("VersionArgumentIndieV2", defaultValue: false, ConfigSource.GameInstance)),
			("VersionArgumentJavaSelect", new ConfigItem<string>("VersionArgumentJavaSelect", "使用全局设置", ConfigSource.GameInstance)),
			("VersionServerEnter", new ConfigItem<string>("VersionServerEnter", "", ConfigSource.GameInstance)),
			("VersionServerLoginRequire", new ConfigItem<int>("VersionServerLoginRequire", 0, ConfigSource.GameInstance)),
			("VersionServerAuthRegister", new ConfigItem<string>("VersionServerAuthRegister", "", ConfigSource.GameInstance)),
			("VersionServerAuthName", new ConfigItem<string>("VersionServerAuthName", "", ConfigSource.GameInstance)),
			("VersionServerAuthServer", new ConfigItem<string>("VersionServerAuthServer", "", ConfigSource.GameInstance)),
			("VersionServerLoginLock", new ConfigItem<bool>("VersionServerLoginLock", defaultValue: false, ConfigSource.GameInstance))
		};
		for (int num = 0; num < array.Length; num++)
		{
			var (text, value) = array[num];
			_KeySet.Add(text);
			_Items[text] = value;
		}
	}

	private static void _InitializeObservers()
	{
		ConfigEventRegistry[] array = new ConfigEventRegistry[5]
		{
			ThemeService.OnColorModeConfigChanged,
			ThemeService.OnColorThemeConfigChanged,
			LocalizationService.OnLanguageConfigChanged,
			SharedVersionInit,
			LocalVersionInit
		};
		foreach (ConfigEventRegistry configEventRegistry in array)
		{
			foreach (IConfigScope scope in configEventRegistry.Scopes)
			{
				RegisterObserver(scope, configEventRegistry.ToObserver());
			}
		}
	}

	public ConfigService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		_Stop();
	}

	public async Task StartAsync()
	{
		_Start();
	}

	static ConfigService()
	{
		SharedVersionConfig = null;
		LocalVersionConfig = null;
		Context = null;
	}
}
