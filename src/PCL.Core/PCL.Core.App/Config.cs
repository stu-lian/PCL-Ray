using System;
using System.Collections.Generic;
using System.Linq;
using PCL.Core.App.Configuration;

namespace PCL.Core.App;

public static class Config
{
	[ConfigGroup("System", ConfigSource.Shared)]
	public sealed class SystemConfigGroup : IConfigScope
	{
		public static readonly SystemConfigGroup SINGLE_INSTANCE = new SystemConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("SystemDisableHardwareAcceleration", false, ConfigSource.Shared)]
		public bool DisableHardwareAcceleration
		{
			get
			{
				return DisableHardwareAccelerationConfig.GetValue();
			}
			set
			{
				DisableHardwareAccelerationConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("SystemTelemetry", false, ConfigSource.Shared)]
		public bool Telemetry
		{
			get
			{
				return TelemetryConfig.GetValue();
			}
			set
			{
				TelemetryConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemMaxLog", 13, ConfigSource.Shared)]
		public int MaxGameLog
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

		[ConfigItem<int>("UiAniFPS", 59, ConfigSource.Shared)]
		public int AnimationFpsLimit
		{
			get
			{
				return AnimationFpsLimitConfig.GetValue();
			}
			set
			{
				AnimationFpsLimitConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("Profile", "{\"lastUsed\":-1,\"profiles\":[]}", ConfigSource.SharedEncrypt)]
		public string Profiles
		{
			get
			{
				return ProfilesConfig.GetValue();
			}
			set
			{
				ProfilesConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> DisableHardwareAccelerationConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemDisableHardwareAcceleration"));

		public ConfigItem<bool> TelemetryConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemTelemetry"));

		public ConfigItem<int> MaxGameLogConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemMaxLog"));

		public ConfigItem<int> AnimationFpsLimitConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiAniFPS"));

		public ConfigItem<string> ProfilesConfig => field ?? (field = ConfigService.GetConfigItem<string>("Profile"));

		private SystemConfigGroup()
		{
			_InnerScopes = new IConfigScope[5] { DisableHardwareAccelerationConfig, TelemetryConfig, MaxGameLogConfig, AnimationFpsLimitConfig, ProfilesConfig };
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

	[ConfigGroup("Network", ConfigSource.Shared)]
	public sealed class NetworkConfigGroup : IConfigScope
	{
		[ConfigGroup("HttpProxy", ConfigSource.Shared)]
		public sealed class HttpProxyConfigGroup : IConfigScope
		{
			public static readonly HttpProxyConfigGroup SINGLE_INSTANCE = new HttpProxyConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<string>("SystemHttpProxy", "", ConfigSource.SharedEncrypt)]
			public string CustomAddress
			{
				get
				{
					return CustomAddressConfig.GetValue();
				}
				set
				{
					CustomAddressConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("SystemHttpProxyType", 1, ConfigSource.Shared)]
			public int Type
			{
				get
				{
					return TypeConfig.GetValue();
				}
				set
				{
					TypeConfig.SetValue(value);
				}
			}

			[ConfigItem<string>("SystemHttpProxyCustomUsername", "", ConfigSource.Shared)]
			public string CustomUsername
			{
				get
				{
					return CustomUsernameConfig.GetValue();
				}
				set
				{
					CustomUsernameConfig.SetValue(value);
				}
			}

			[ConfigItem<string>("SystemHttpProxyCustomPassword", "", ConfigSource.Shared)]
			public string CustomPassword
			{
				get
				{
					return CustomPasswordConfig.GetValue();
				}
				set
				{
					CustomPasswordConfig.SetValue(value);
				}
			}

			public ConfigItem<string> CustomAddressConfig => field ?? (field = ConfigService.GetConfigItem<string>("SystemHttpProxy"));

			public ConfigItem<int> TypeConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemHttpProxyType"));

			public ConfigItem<string> CustomUsernameConfig => field ?? (field = ConfigService.GetConfigItem<string>("SystemHttpProxyCustomUsername"));

			public ConfigItem<string> CustomPasswordConfig => field ?? (field = ConfigService.GetConfigItem<string>("SystemHttpProxyCustomPassword"));

			private HttpProxyConfigGroup()
			{
				_InnerScopes = new IConfigScope[4] { CustomAddressConfig, TypeConfig, CustomUsernameConfig, CustomPasswordConfig };
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

		public readonly HttpProxyConfigGroup HttpProxy = HttpProxyConfigGroup.SINGLE_INSTANCE;

		public static readonly NetworkConfigGroup SINGLE_INSTANCE = new NetworkConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("SystemNetEnableDoH", true, ConfigSource.Shared)]
		public bool EnableDoH
		{
			get
			{
				return EnableDoHConfig.GetValue();
			}
			set
			{
				EnableDoHConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> EnableDoHConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemNetEnableDoH"));

		private NetworkConfigGroup()
		{
			_InnerScopes = new IConfigScope[2] { EnableDoHConfig, HttpProxy };
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

	[ConfigGroup("Debug", ConfigSource.Shared)]
	public sealed class DebugConfigGroup : IConfigScope
	{
		public static readonly DebugConfigGroup SINGLE_INSTANCE = new DebugConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("SystemDebugMode", false, ConfigSource.Shared)]
		public bool Enabled
		{
			get
			{
				return EnabledConfig.GetValue();
			}
			set
			{
				EnabledConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("SystemDebugAnim", 9, ConfigSource.Shared)]
		public int AnimationSpeed
		{
			get
			{
				return AnimationSpeedConfig.GetValue();
			}
			set
			{
				AnimationSpeedConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("SystemDebugDelay", false, ConfigSource.Shared)]
		public bool AddRandomDelay
		{
			get
			{
				return AddRandomDelayConfig.GetValue();
			}
			set
			{
				AddRandomDelayConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("SystemDebugSkipCopy", false, ConfigSource.Shared)]
		public bool DontCopy
		{
			get
			{
				return DontCopyConfig.GetValue();
			}
			set
			{
				DontCopyConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("SystemDebugAllowRestrictedFeature", false, ConfigSource.Shared)]
		public bool AllowRestrictedFeature
		{
			get
			{
				return AllowRestrictedFeatureConfig.GetValue();
			}
			set
			{
				AllowRestrictedFeatureConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> EnabledConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemDebugMode"));

		public ConfigItem<int> AnimationSpeedConfig => field ?? (field = ConfigService.GetConfigItem<int>("SystemDebugAnim"));

		public ConfigItem<bool> AddRandomDelayConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemDebugDelay"));

		public ConfigItem<bool> DontCopyConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemDebugSkipCopy"));

		public ConfigItem<bool> AllowRestrictedFeatureConfig => field ?? (field = ConfigService.GetConfigItem<bool>("SystemDebugAllowRestrictedFeature"));

		private DebugConfigGroup()
		{
			_InnerScopes = new IConfigScope[5] { EnabledConfig, AnimationSpeedConfig, AddRandomDelayConfig, DontCopyConfig, AllowRestrictedFeatureConfig };
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

	[ConfigGroup("Download", ConfigSource.Shared)]
	public sealed class DownloadConfigGroup : IConfigScope
	{
		[ConfigGroup("Comp", ConfigSource.Shared)]
		public sealed class CompConfigGroup : IConfigScope
		{
			public static readonly CompConfigGroup SINGLE_INSTANCE = new CompConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<int>("ToolDownloadTranslate", 0, ConfigSource.Shared)]
			public int NameFormatV1
			{
				get
				{
					return NameFormatV1Config.GetValue();
				}
				set
				{
					NameFormatV1Config.SetValue(value);
				}
			}

			[ConfigItem<int>("ToolDownloadTranslateV2", 1, ConfigSource.Shared)]
			public int NameFormatV2
			{
				get
				{
					return NameFormatV2Config.GetValue();
				}
				set
				{
					NameFormatV2Config.SetValue(value);
				}
			}

			[ConfigItem<bool>("ToolDownloadIgnoreQuilt", true, ConfigSource.Shared)]
			public bool IgnoreQuilt
			{
				get
				{
					return IgnoreQuiltConfig.GetValue();
				}
				set
				{
					IgnoreQuiltConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("ToolDownloadAutoInstallDependencies", true, ConfigSource.Shared)]
			public bool AutoInstallDependencies
			{
				get
				{
					return AutoInstallDependenciesConfig.GetValue();
				}
				set
				{
					AutoInstallDependenciesConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("ToolDownloadAutoInstallOptionalDependencies", false, ConfigSource.Shared)]
			public bool AutoInstallOptionalDependencies
			{
				get
				{
					return AutoInstallOptionalDependenciesConfig.GetValue();
				}
				set
				{
					AutoInstallOptionalDependenciesConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("ToolDownloadClipboard", false, ConfigSource.Shared)]
			public bool ReadClipboard
			{
				get
				{
					return ReadClipboardConfig.GetValue();
				}
				set
				{
					ReadClipboardConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("ToolDownloadMod", 1, ConfigSource.Shared)]
			public int CompSourceSolution
			{
				get
				{
					return CompSourceSolutionConfig.GetValue();
				}
				set
				{
					CompSourceSolutionConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("ToolModLocalNameStyle", 0, ConfigSource.Shared)]
			public int UiCompNameSolution
			{
				get
				{
					return UiCompNameSolutionConfig.GetValue();
				}
				set
				{
					UiCompNameSolutionConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("ToolDownloadQuickBehavior", 0, ConfigSource.Shared)]
			public int QuickDownloadBehavior
			{
				get
				{
					return QuickDownloadBehaviorConfig.GetValue();
				}
				set
				{
					QuickDownloadBehaviorConfig.SetValue(value);
				}
			}

			public ConfigItem<int> NameFormatV1Config => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadTranslate"));

			public ConfigItem<int> NameFormatV2Config => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadTranslateV2"));

			public ConfigItem<bool> IgnoreQuiltConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolDownloadIgnoreQuilt"));

			public ConfigItem<bool> AutoInstallDependenciesConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolDownloadAutoInstallDependencies"));

			public ConfigItem<bool> AutoInstallOptionalDependenciesConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolDownloadAutoInstallOptionalDependencies"));

			public ConfigItem<bool> ReadClipboardConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolDownloadClipboard"));

			public ConfigItem<int> CompSourceSolutionConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadMod"));

			public ConfigItem<int> UiCompNameSolutionConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolModLocalNameStyle"));

			public ConfigItem<int> QuickDownloadBehaviorConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadQuickBehavior"));

			private CompConfigGroup()
			{
				_InnerScopes = new IConfigScope[9] { NameFormatV1Config, NameFormatV2Config, IgnoreQuiltConfig, AutoInstallDependenciesConfig, AutoInstallOptionalDependenciesConfig, ReadClipboardConfig, CompSourceSolutionConfig, UiCompNameSolutionConfig, QuickDownloadBehaviorConfig };
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

		public readonly CompConfigGroup Comp = CompConfigGroup.SINGLE_INSTANCE;

		public static readonly DownloadConfigGroup SINGLE_INSTANCE = new DownloadConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<int>("ToolDownloadThread", 63, ConfigSource.Shared)]
		public int ThreadLimit
		{
			get
			{
				return ThreadLimitConfig.GetValue();
			}
			set
			{
				ThreadLimitConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("ToolDownloadFileConnection", 7, ConfigSource.Shared)]
		public int FileConnectionLimit
		{
			get
			{
				return FileConnectionLimitConfig.GetValue();
			}
			set
			{
				FileConnectionLimitConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("ToolDownloadSpeed", 42, ConfigSource.Shared)]
		public int SpeedLimit
		{
			get
			{
				return SpeedLimitConfig.GetValue();
			}
			set
			{
				SpeedLimitConfig.SetValue(value);
			}
		}

		[ConfigItem<DownloadHttpMode>("ToolDownloadHttpMode", DownloadHttpMode.Auto, ConfigSource.Shared)]
		public DownloadHttpMode HttpMode
		{
			get
			{
				return HttpModeConfig.GetValue();
			}
			set
			{
				HttpModeConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("ToolDownloadSource", 1, ConfigSource.Shared)]
		public int FileSource
		{
			get
			{
				return FileSourceConfig.GetValue();
			}
			set
			{
				FileSourceConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("ToolDownloadVersion", 1, ConfigSource.Shared)]
		public int VersionListSource
		{
			get
			{
				return VersionListSourceConfig.GetValue();
			}
			set
			{
				VersionListSourceConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("ToolDownloadAutoSelectVersion", true, ConfigSource.Shared)]
		public bool AutoSelectInstance
		{
			get
			{
				return AutoSelectInstanceConfig.GetValue();
			}
			set
			{
				AutoSelectInstanceConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("ToolFixAuthlib", true, ConfigSource.Shared)]
		public bool FixAuthLib
		{
			get
			{
				return FixAuthLibConfig.GetValue();
			}
			set
			{
				FixAuthLibConfig.SetValue(value);
			}
		}

		public ConfigItem<int> ThreadLimitConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadThread"));

		public ConfigItem<int> FileConnectionLimitConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadFileConnection"));

		public ConfigItem<int> SpeedLimitConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadSpeed"));

		public ConfigItem<DownloadHttpMode> HttpModeConfig => field ?? (field = ConfigService.GetConfigItem<DownloadHttpMode>("ToolDownloadHttpMode"));

		public ConfigItem<int> FileSourceConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadSource"));

		public ConfigItem<int> VersionListSourceConfig => field ?? (field = ConfigService.GetConfigItem<int>("ToolDownloadVersion"));

		public ConfigItem<bool> AutoSelectInstanceConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolDownloadAutoSelectVersion"));

		public ConfigItem<bool> FixAuthLibConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolFixAuthlib"));

		private DownloadConfigGroup()
		{
			_InnerScopes = new IConfigScope[9] { ThreadLimitConfig, FileConnectionLimitConfig, SpeedLimitConfig, HttpModeConfig, FileSourceConfig, VersionListSourceConfig, AutoSelectInstanceConfig, FixAuthLibConfig, Comp };
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
	public sealed class ToolConfigGroup : IConfigScope
	{
		public static readonly ToolConfigGroup SINGLE_INSTANCE = new ToolConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("ToolHelpChinese", true, ConfigSource.Shared)]
		public bool AutoChangeLanguage
		{
			get
			{
				return AutoChangeLanguageConfig.GetValue();
			}
			set
			{
				AutoChangeLanguageConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("ToolUpdateRelease", false, ConfigSource.Shared)]
		public bool ReleaseNotification
		{
			get
			{
				return ReleaseNotificationConfig.GetValue();
			}
			set
			{
				ReleaseNotificationConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("ToolUpdateSnapshot", false, ConfigSource.Shared)]
		public bool SnapshotNotification
		{
			get
			{
				return SnapshotNotificationConfig.GetValue();
			}
			set
			{
				SnapshotNotificationConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> AutoChangeLanguageConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolHelpChinese"));

		public ConfigItem<bool> ReleaseNotificationConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolUpdateRelease"));

		public ConfigItem<bool> SnapshotNotificationConfig => field ?? (field = ConfigService.GetConfigItem<bool>("ToolUpdateSnapshot"));

		private ToolConfigGroup()
		{
			_InnerScopes = new IConfigScope[3] { AutoChangeLanguageConfig, ReleaseNotificationConfig, SnapshotNotificationConfig };
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

	[ConfigGroup("Update", ConfigSource.Shared)]
	public sealed class UpdateConfigGroup : IConfigScope
	{
		public static readonly UpdateConfigGroup SINGLE_INSTANCE = new UpdateConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<LauncherAutoUpdateBehavior>("SystemSystemUpdate", LauncherAutoUpdateBehavior.DownloadAndAnnounce, ConfigSource.Local)]
		public LauncherAutoUpdateBehavior UpdateMode
		{
			get
			{
				return UpdateModeConfig.GetValue();
			}
			set
			{
				UpdateModeConfig.SetValue(value);
			}
		}

		[ConfigItem<UpdateChannel>("SystemUpdateChannel", UpdateChannel.Release, ConfigSource.Local)]
		public UpdateChannel UpdateChannel
		{
			get
			{
				return UpdateChannelConfig.GetValue();
			}
			set
			{
				UpdateChannelConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("SystemMirrorChyanKey", "", ConfigSource.SharedEncrypt)]
		public string MirrorChyanKey
		{
			get
			{
				return MirrorChyanKeyConfig.GetValue();
			}
			set
			{
				MirrorChyanKeyConfig.SetValue(value);
			}
		}

		public ConfigItem<LauncherAutoUpdateBehavior> UpdateModeConfig => field ?? (field = ConfigService.GetConfigItem<LauncherAutoUpdateBehavior>("SystemSystemUpdate"));

		public ConfigItem<UpdateChannel> UpdateChannelConfig => field ?? (field = ConfigService.GetConfigItem<UpdateChannel>("SystemUpdateChannel"));

		public ConfigItem<string> MirrorChyanKeyConfig => field ?? (field = ConfigService.GetConfigItem<string>("SystemMirrorChyanKey"));

		private UpdateConfigGroup()
		{
			_InnerScopes = new IConfigScope[3] { UpdateModeConfig, UpdateChannelConfig, MirrorChyanKeyConfig };
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
	public sealed class LinkConfigGroup : IConfigScope
	{
		public static readonly LinkConfigGroup SINGLE_INSTANCE = new LinkConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<string>("LinkUsername", "", ConfigSource.Shared)]
		public string Username
		{
			get
			{
				return UsernameConfig.GetValue();
			}
			set
			{
				UsernameConfig.SetValue(value);
			}
		}

		[ConfigItem<LinkRelayBehavior>("LinkRelayType", LinkRelayBehavior.Default, ConfigSource.Shared)]
		public LinkRelayBehavior RelayType
		{
			get
			{
				return RelayTypeConfig.GetValue();
			}
			set
			{
				RelayTypeConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LinkServerType", 1, ConfigSource.Shared)]
		public int ServerType
		{
			get
			{
				return ServerTypeConfig.GetValue();
			}
			set
			{
				ServerTypeConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LinkLatencyFirstMode", true, ConfigSource.Shared)]
		public bool UseLatencyFirstMode
		{
			get
			{
				return UseLatencyFirstModeConfig.GetValue();
			}
			set
			{
				UseLatencyFirstModeConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LinkRelayServer", "", ConfigSource.Shared)]
		public string CustomRelayServer
		{
			get
			{
				return CustomRelayServerConfig.GetValue();
			}
			set
			{
				CustomRelayServerConfig.SetValue(value);
			}
		}

		[ConfigItem<LinkProtocolPreference>("LinkProtocolPreference", LinkProtocolPreference.Tcp, ConfigSource.Shared)]
		public LinkProtocolPreference ProtocolPreference
		{
			get
			{
				return ProtocolPreferenceConfig.GetValue();
			}
			set
			{
				ProtocolPreferenceConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LinkTryPunchSym", true, ConfigSource.Shared)]
		public bool TryPunchSym
		{
			get
			{
				return TryPunchSymConfig.GetValue();
			}
			set
			{
				TryPunchSymConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LinkEnableIPv6", true, ConfigSource.Shared)]
		public bool EnableIPv6
		{
			get
			{
				return EnableIPv6Config.GetValue();
			}
			set
			{
				EnableIPv6Config.SetValue(value);
			}
		}

		[ConfigItem<bool>("LinkEnableCliOutput", false, ConfigSource.Shared)]
		public bool EnableCliOutput
		{
			get
			{
				return EnableCliOutputConfig.GetValue();
			}
			set
			{
				EnableCliOutputConfig.SetValue(value);
			}
		}

		public ConfigItem<string> UsernameConfig => field ?? (field = ConfigService.GetConfigItem<string>("LinkUsername"));

		public ConfigItem<LinkRelayBehavior> RelayTypeConfig => field ?? (field = ConfigService.GetConfigItem<LinkRelayBehavior>("LinkRelayType"));

		public ConfigItem<int> ServerTypeConfig => field ?? (field = ConfigService.GetConfigItem<int>("LinkServerType"));

		public ConfigItem<bool> UseLatencyFirstModeConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LinkLatencyFirstMode"));

		public ConfigItem<string> CustomRelayServerConfig => field ?? (field = ConfigService.GetConfigItem<string>("LinkRelayServer"));

		public ConfigItem<LinkProtocolPreference> ProtocolPreferenceConfig => field ?? (field = ConfigService.GetConfigItem<LinkProtocolPreference>("LinkProtocolPreference"));

		public ConfigItem<bool> TryPunchSymConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LinkTryPunchSym"));

		public ConfigItem<bool> EnableIPv6Config => field ?? (field = ConfigService.GetConfigItem<bool>("LinkEnableIPv6"));

		public ConfigItem<bool> EnableCliOutputConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LinkEnableCliOutput"));

		private LinkConfigGroup()
		{
			_InnerScopes = new IConfigScope[9] { UsernameConfig, RelayTypeConfig, ServerTypeConfig, UseLatencyFirstModeConfig, CustomRelayServerConfig, ProtocolPreferenceConfig, TryPunchSymConfig, EnableIPv6Config, EnableCliOutputConfig };
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

	[ConfigGroup("Preference", ConfigSource.Shared)]
	public sealed class PreferenceConfigGroup : IConfigScope
	{
		[ConfigGroup("Localization", ConfigSource.Local)]
		public sealed class LocalizationConfigGroup : IConfigScope
		{
			public static readonly LocalizationConfigGroup SINGLE_INSTANCE = new LocalizationConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<string>("UiLanguage", "auto", ConfigSource.Shared)]
			public string Language
			{
				get
				{
					return LanguageConfig.GetValue();
				}
				set
				{
					LanguageConfig.SetValue(value);
				}
			}

			[ConfigItem<string>("UiFormatCulture", "auto", ConfigSource.Shared)]
			public string FormatCulture
			{
				get
				{
					return FormatCultureConfig.GetValue();
				}
				set
				{
					FormatCultureConfig.SetValue(value);
				}
			}

			[ConfigItem<string>("UiRegion", "auto", ConfigSource.Shared)]
			public string Region
			{
				get
				{
					return RegionConfig.GetValue();
				}
				set
				{
					RegionConfig.SetValue(value);
				}
			}

			public ConfigItem<string> LanguageConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiLanguage"));

			public ConfigItem<string> FormatCultureConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiFormatCulture"));

			public ConfigItem<string> RegionConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiRegion"));

			private LocalizationConfigGroup()
			{
				_InnerScopes = new IConfigScope[3] { LanguageConfig, FormatCultureConfig, RegionConfig };
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

		[ConfigGroup("Theme", ConfigSource.Shared)]
		public sealed class ThemeConfigGroup : IConfigScope
		{
			public static readonly ThemeConfigGroup SINGLE_INSTANCE = new ThemeConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<ColorMode>("UiDarkMode", ColorMode.System, ConfigSource.Shared)]
			public ColorMode ColorMode
			{
				get
				{
					return ColorModeConfig.GetValue();
				}
				set
				{
					ColorModeConfig.SetValue(value);
				}
			}

			[ConfigItem<ColorTheme>("UiDarkColor", ColorTheme.CatBlue, ConfigSource.Shared)]
			public ColorTheme DarkColor
			{
				get
				{
					return DarkColorConfig.GetValue();
				}
				set
				{
					DarkColorConfig.SetValue(value);
				}
			}

			[ConfigItem<ColorTheme>("UiLightColor", ColorTheme.CatBlue, ConfigSource.Shared)]
			public ColorTheme LightColor
			{
				get
				{
					return LightColorConfig.GetValue();
				}
				set
				{
					LightColorConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiLauncherTransparent", 600, ConfigSource.Local)]
			public int WindowOpacity
			{
				get
				{
					return WindowOpacityConfig.GetValue();
				}
				set
				{
					WindowOpacityConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiLauncherHue", 180, ConfigSource.Local)]
			public int WindowHue
			{
				get
				{
					return WindowHueConfig.GetValue();
				}
				set
				{
					WindowHueConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiLauncherSat", 80, ConfigSource.Local)]
			public int WindowSat
			{
				get
				{
					return WindowSatConfig.GetValue();
				}
				set
				{
					WindowSatConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiLauncherLight", 20, ConfigSource.Local)]
			public int WindowLight
			{
				get
				{
					return WindowLightConfig.GetValue();
				}
				set
				{
					WindowLightConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiLauncherDelta", 90, ConfigSource.Local)]
			public int WindowDelta
			{
				get
				{
					return WindowDeltaConfig.GetValue();
				}
				set
				{
					WindowDeltaConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiLauncherTheme", 0, ConfigSource.Local)]
			public int ThemeSelected
			{
				get
				{
					return ThemeSelectedConfig.GetValue();
				}
				set
				{
					ThemeSelectedConfig.SetValue(value);
				}
			}

			public ConfigItem<ColorMode> ColorModeConfig => field ?? (field = ConfigService.GetConfigItem<ColorMode>("UiDarkMode"));

			public ConfigItem<ColorTheme> DarkColorConfig => field ?? (field = ConfigService.GetConfigItem<ColorTheme>("UiDarkColor"));

			public ConfigItem<ColorTheme> LightColorConfig => field ?? (field = ConfigService.GetConfigItem<ColorTheme>("UiLightColor"));

			public ConfigItem<int> WindowOpacityConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiLauncherTransparent"));

			public ConfigItem<int> WindowHueConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiLauncherHue"));

			public ConfigItem<int> WindowSatConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiLauncherSat"));

			public ConfigItem<int> WindowLightConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiLauncherLight"));

			public ConfigItem<int> WindowDeltaConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiLauncherDelta"));

			public ConfigItem<int> ThemeSelectedConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiLauncherTheme"));

			private ThemeConfigGroup()
			{
				_InnerScopes = new IConfigScope[9] { ColorModeConfig, DarkColorConfig, LightColorConfig, WindowOpacityConfig, WindowHueConfig, WindowSatConfig, WindowLightConfig, WindowDeltaConfig, ThemeSelectedConfig };
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

		[ConfigGroup("Background", ConfigSource.Shared)]
		public sealed class BackgroundConfigGroup : IConfigScope
		{
			public static readonly BackgroundConfigGroup SINGLE_INSTANCE = new BackgroundConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<bool>("UiBackgroundColorful", true, ConfigSource.Local)]
			public bool BackgroundColorful
			{
				get
				{
					return BackgroundColorfulConfig.GetValue();
				}
				set
				{
					BackgroundColorfulConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBackgroundOpacity", 1000, ConfigSource.Local)]
			public int WallpaperOpacity
			{
				get
				{
					return WallpaperOpacityConfig.GetValue();
				}
				set
				{
					WallpaperOpacityConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBackgroundCarousel", 1000, ConfigSource.Local)]
			public int WallpaperCarousel
			{
				get
				{
					return WallpaperCarouselConfig.GetValue();
				}
				set
				{
					WallpaperCarouselConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBackgroundBlur", 0, ConfigSource.Local)]
			public int WallpaperBlurRadius
			{
				get
				{
					return WallpaperBlurRadiusConfig.GetValue();
				}
				set
				{
					WallpaperBlurRadiusConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBackgroundSuit", 0, ConfigSource.Local)]
			public int WallpaperSuitMode
			{
				get
				{
					return WallpaperSuitModeConfig.GetValue();
				}
				set
				{
					WallpaperSuitModeConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiAutoPauseVideo", true, ConfigSource.Local)]
			public bool AutoPauseVideo
			{
				get
				{
					return AutoPauseVideoConfig.GetValue();
				}
				set
				{
					AutoPauseVideoConfig.SetValue(value);
				}
			}

			public ConfigItem<bool> BackgroundColorfulConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiBackgroundColorful"));

			public ConfigItem<int> WallpaperOpacityConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBackgroundOpacity"));

			public ConfigItem<int> WallpaperCarouselConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBackgroundCarousel"));

			public ConfigItem<int> WallpaperBlurRadiusConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBackgroundBlur"));

			public ConfigItem<int> WallpaperSuitModeConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBackgroundSuit"));

			public ConfigItem<bool> AutoPauseVideoConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiAutoPauseVideo"));

			private BackgroundConfigGroup()
			{
				_InnerScopes = new IConfigScope[6] { BackgroundColorfulConfig, WallpaperOpacityConfig, WallpaperCarouselConfig, WallpaperBlurRadiusConfig, WallpaperSuitModeConfig, AutoPauseVideoConfig };
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

		[ConfigGroup("Blur", ConfigSource.Shared)]
		public sealed class BlurConfigGroup : IConfigScope
		{
			public static readonly BlurConfigGroup SINGLE_INSTANCE = new BlurConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<bool>("UiBlur", false, ConfigSource.Local)]
			public bool IsEnabled
			{
				get
				{
					return IsEnabledConfig.GetValue();
				}
				set
				{
					IsEnabledConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBlurValue", 16, ConfigSource.Local)]
			public int Radius
			{
				get
				{
					return RadiusConfig.GetValue();
				}
				set
				{
					RadiusConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBlurSamplingRate", 70, ConfigSource.Local)]
			public int SamplingRate
			{
				get
				{
					return SamplingRateConfig.GetValue();
				}
				set
				{
					SamplingRateConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiBlurType", 0, ConfigSource.Local)]
			public int KernelType
			{
				get
				{
					return KernelTypeConfig.GetValue();
				}
				set
				{
					KernelTypeConfig.SetValue(value);
				}
			}

			public ConfigItem<bool> IsEnabledConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiBlur"));

			public ConfigItem<int> RadiusConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBlurValue"));

			public ConfigItem<int> SamplingRateConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBlurSamplingRate"));

			public ConfigItem<int> KernelTypeConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiBlurType"));

			private BlurConfigGroup()
			{
				_InnerScopes = new IConfigScope[4] { IsEnabledConfig, RadiusConfig, SamplingRateConfig, KernelTypeConfig };
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

		[ConfigGroup("Homepage", ConfigSource.Shared)]
		public sealed class HomepageConfigGroup : IConfigScope
		{
			public static readonly HomepageConfigGroup SINGLE_INSTANCE = new HomepageConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<int>("UiCustomType", 0, ConfigSource.Local)]
			public int Type
			{
				get
				{
					return TypeConfig.GetValue();
				}
				set
				{
					TypeConfig.SetValue(value);
				}
			}

			[ConfigItem<int>("UiCustomPreset", 0, ConfigSource.Local)]
			public int SelectedPreset
			{
				get
				{
					return SelectedPresetConfig.GetValue();
				}
				set
				{
					SelectedPresetConfig.SetValue(value);
				}
			}

			[ConfigItem<string>("UiCustomNet", "", ConfigSource.Local)]
			public string CustomUrl
			{
				get
				{
					return CustomUrlConfig.GetValue();
				}
				set
				{
					CustomUrlConfig.SetValue(value);
				}
			}

			public ConfigItem<int> TypeConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiCustomType"));

			public ConfigItem<int> SelectedPresetConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiCustomPreset"));

			public ConfigItem<string> CustomUrlConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiCustomNet"));

			private HomepageConfigGroup()
			{
				_InnerScopes = new IConfigScope[3] { TypeConfig, SelectedPresetConfig, CustomUrlConfig };
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

		[ConfigGroup("Music", ConfigSource.Shared)]
		public sealed class MusicConfigGroup : IConfigScope
		{
			public static readonly MusicConfigGroup SINGLE_INSTANCE = new MusicConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<int>("UiMusicVolume", 500, ConfigSource.Local)]
			public int Volume
			{
				get
				{
					return VolumeConfig.GetValue();
				}
				set
				{
					VolumeConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiMusicStop", false, ConfigSource.Local)]
			public bool StopInGame
			{
				get
				{
					return StopInGameConfig.GetValue();
				}
				set
				{
					StopInGameConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiMusicStart", false, ConfigSource.Local)]
			public bool StartInGame
			{
				get
				{
					return StartInGameConfig.GetValue();
				}
				set
				{
					StartInGameConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiMusicAuto", true, ConfigSource.Local)]
			public bool StartOnStartup
			{
				get
				{
					return StartOnStartupConfig.GetValue();
				}
				set
				{
					StartOnStartupConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiMusicRandom", true, ConfigSource.Local)]
			public bool ShufflePlayback
			{
				get
				{
					return ShufflePlaybackConfig.GetValue();
				}
				set
				{
					ShufflePlaybackConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiMusicSMTC", true, ConfigSource.Local)]
			public bool EnableSMTC
			{
				get
				{
					return EnableSMTCConfig.GetValue();
				}
				set
				{
					EnableSMTCConfig.SetValue(value);
				}
			}

			public ConfigItem<int> VolumeConfig => field ?? (field = ConfigService.GetConfigItem<int>("UiMusicVolume"));

			public ConfigItem<bool> StopInGameConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiMusicStop"));

			public ConfigItem<bool> StartInGameConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiMusicStart"));

			public ConfigItem<bool> StartOnStartupConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiMusicAuto"));

			public ConfigItem<bool> ShufflePlaybackConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiMusicRandom"));

			public ConfigItem<bool> EnableSMTCConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiMusicSMTC"));

			private MusicConfigGroup()
			{
				_InnerScopes = new IConfigScope[6] { VolumeConfig, StopInGameConfig, StartInGameConfig, StartOnStartupConfig, ShufflePlaybackConfig, EnableSMTCConfig };
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

		[ConfigGroup("Hide", ConfigSource.Shared)]
		public sealed class HideConfigGroup : IConfigScope
		{
			public static readonly HideConfigGroup SINGLE_INSTANCE = new HideConfigGroup();

			private readonly IConfigScope[] _InnerScopes;

			[ConfigItem<bool>("UiHiddenPageDownload", false, ConfigSource.Local)]
			public bool PageDownload
			{
				get
				{
					return PageDownloadConfig.GetValue();
				}
				set
				{
					PageDownloadConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenPageSetup", false, ConfigSource.Local)]
			public bool PageSetup
			{
				get
				{
					return PageSetupConfig.GetValue();
				}
				set
				{
					PageSetupConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenPageTools", false, ConfigSource.Local)]
			public bool PageTools
			{
				get
				{
					return PageToolsConfig.GetValue();
				}
				set
				{
					PageToolsConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupLaunch", false, ConfigSource.Local)]
			public bool SetupLaunch
			{
				get
				{
					return SetupLaunchConfig.GetValue();
				}
				set
				{
					SetupLaunchConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupUi", false, ConfigSource.Local)]
			public bool SetupUi
			{
				get
				{
					return SetupUiConfig.GetValue();
				}
				set
				{
					SetupUiConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupLauncherLanguage", false, ConfigSource.Local)]
			public bool SetupLauncherLanguage
			{
				get
				{
					return SetupLauncherLanguageConfig.GetValue();
				}
				set
				{
					SetupLauncherLanguageConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupLauncherMisc", false, ConfigSource.Local)]
			public bool SetupLauncherMisc
			{
				get
				{
					return SetupLauncherMiscConfig.GetValue();
				}
				set
				{
					SetupLauncherMiscConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupGameManage", false, ConfigSource.Local)]
			public bool SetupGameManage
			{
				get
				{
					return SetupGameManageConfig.GetValue();
				}
				set
				{
					SetupGameManageConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupJava", false, ConfigSource.Local)]
			public bool SetupJava
			{
				get
				{
					return SetupJavaConfig.GetValue();
				}
				set
				{
					SetupJavaConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupUpdate", false, ConfigSource.Local)]
			public bool SetupUpdate
			{
				get
				{
					return SetupUpdateConfig.GetValue();
				}
				set
				{
					SetupUpdateConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupGameLink", false, ConfigSource.Local)]
			public bool SetupGameLink
			{
				get
				{
					return SetupGameLinkConfig.GetValue();
				}
				set
				{
					SetupGameLinkConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupAbout", false, ConfigSource.Local)]
			public bool SetupAbout
			{
				get
				{
					return SetupAboutConfig.GetValue();
				}
				set
				{
					SetupAboutConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupFeedback", false, ConfigSource.Local)]
			public bool SetupFeedback
			{
				get
				{
					return SetupFeedbackConfig.GetValue();
				}
				set
				{
					SetupFeedbackConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenSetupLog", false, ConfigSource.Local)]
			public bool SetupLog
			{
				get
				{
					return SetupLogConfig.GetValue();
				}
				set
				{
					SetupLogConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenToolsGameLink", false, ConfigSource.Local)]
			public bool ToolsGameLink
			{
				get
				{
					return ToolsGameLinkConfig.GetValue();
				}
				set
				{
					ToolsGameLinkConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenToolsTest", false, ConfigSource.Local)]
			public bool ToolsTest
			{
				get
				{
					return ToolsTestConfig.GetValue();
				}
				set
				{
					ToolsTestConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionEdit", false, ConfigSource.Local)]
			public bool InstanceEdit
			{
				get
				{
					return InstanceEditConfig.GetValue();
				}
				set
				{
					InstanceEditConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionExport", false, ConfigSource.Local)]
			public bool InstanceExport
			{
				get
				{
					return InstanceExportConfig.GetValue();
				}
				set
				{
					InstanceExportConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionSave", false, ConfigSource.Local)]
			public bool InstanceSave
			{
				get
				{
					return InstanceSaveConfig.GetValue();
				}
				set
				{
					InstanceSaveConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionScreenshot", false, ConfigSource.Local)]
			public bool InstanceScreenshot
			{
				get
				{
					return InstanceScreenshotConfig.GetValue();
				}
				set
				{
					InstanceScreenshotConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionMod", false, ConfigSource.Local)]
			public bool InstanceMod
			{
				get
				{
					return InstanceModConfig.GetValue();
				}
				set
				{
					InstanceModConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionResourcePack", false, ConfigSource.Local)]
			public bool InstanceResourcePack
			{
				get
				{
					return InstanceResourcePackConfig.GetValue();
				}
				set
				{
					InstanceResourcePackConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionShader", false, ConfigSource.Local)]
			public bool InstanceShader
			{
				get
				{
					return InstanceShaderConfig.GetValue();
				}
				set
				{
					InstanceShaderConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionSchematic", false, ConfigSource.Local)]
			public bool InstanceSchematic
			{
				get
				{
					return InstanceSchematicConfig.GetValue();
				}
				set
				{
					InstanceSchematicConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenVersionServer", false, ConfigSource.Local)]
			public bool InstanceServer
			{
				get
				{
					return InstanceServerConfig.GetValue();
				}
				set
				{
					InstanceServerConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenFunctionSelect", false, ConfigSource.Local)]
			public bool FunctionSelect
			{
				get
				{
					return FunctionSelectConfig.GetValue();
				}
				set
				{
					FunctionSelectConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenFunctionModUpdate", false, ConfigSource.Local)]
			public bool FunctionModUpdate
			{
				get
				{
					return FunctionModUpdateConfig.GetValue();
				}
				set
				{
					FunctionModUpdateConfig.SetValue(value);
				}
			}

			[ConfigItem<bool>("UiHiddenFunctionHidden", false, ConfigSource.Local)]
			public bool FunctionHidden
			{
				get
				{
					return FunctionHiddenConfig.GetValue();
				}
				set
				{
					FunctionHiddenConfig.SetValue(value);
				}
			}

			public ConfigItem<bool> PageDownloadConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenPageDownload"));

			public ConfigItem<bool> PageSetupConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenPageSetup"));

			public ConfigItem<bool> PageToolsConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenPageTools"));

			public ConfigItem<bool> SetupLaunchConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupLaunch"));

			public ConfigItem<bool> SetupUiConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupUi"));

			public ConfigItem<bool> SetupLauncherLanguageConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupLauncherLanguage"));

			public ConfigItem<bool> SetupLauncherMiscConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupLauncherMisc"));

			public ConfigItem<bool> SetupGameManageConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupGameManage"));

			public ConfigItem<bool> SetupJavaConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupJava"));

			public ConfigItem<bool> SetupUpdateConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupUpdate"));

			public ConfigItem<bool> SetupGameLinkConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupGameLink"));

			public ConfigItem<bool> SetupAboutConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupAbout"));

			public ConfigItem<bool> SetupFeedbackConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupFeedback"));

			public ConfigItem<bool> SetupLogConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenSetupLog"));

			public ConfigItem<bool> ToolsGameLinkConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenToolsGameLink"));

			public ConfigItem<bool> ToolsTestConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenToolsTest"));

			public ConfigItem<bool> InstanceEditConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionEdit"));

			public ConfigItem<bool> InstanceExportConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionExport"));

			public ConfigItem<bool> InstanceSaveConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionSave"));

			public ConfigItem<bool> InstanceScreenshotConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionScreenshot"));

			public ConfigItem<bool> InstanceModConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionMod"));

			public ConfigItem<bool> InstanceResourcePackConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionResourcePack"));

			public ConfigItem<bool> InstanceShaderConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionShader"));

			public ConfigItem<bool> InstanceSchematicConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionSchematic"));

			public ConfigItem<bool> InstanceServerConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenVersionServer"));

			public ConfigItem<bool> FunctionSelectConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenFunctionSelect"));

			public ConfigItem<bool> FunctionModUpdateConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenFunctionModUpdate"));

			public ConfigItem<bool> FunctionHiddenConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiHiddenFunctionHidden"));

			private HideConfigGroup()
			{
				_InnerScopes = new IConfigScope[28]
				{
					PageDownloadConfig, PageSetupConfig, PageToolsConfig, SetupLaunchConfig, SetupUiConfig, SetupLauncherLanguageConfig, SetupLauncherMiscConfig, SetupGameManageConfig, SetupJavaConfig, SetupUpdateConfig,
					SetupGameLinkConfig, SetupAboutConfig, SetupFeedbackConfig, SetupLogConfig, ToolsGameLinkConfig, ToolsTestConfig, InstanceEditConfig, InstanceExportConfig, InstanceSaveConfig, InstanceScreenshotConfig,
					InstanceModConfig, InstanceResourcePackConfig, InstanceShaderConfig, InstanceSchematicConfig, InstanceServerConfig, FunctionSelectConfig, FunctionModUpdateConfig, FunctionHiddenConfig
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

		public readonly LocalizationConfigGroup Localization = LocalizationConfigGroup.SINGLE_INSTANCE;

		public readonly ThemeConfigGroup Theme = ThemeConfigGroup.SINGLE_INSTANCE;

		public readonly BackgroundConfigGroup Background = BackgroundConfigGroup.SINGLE_INSTANCE;

		public readonly BlurConfigGroup Blur = BlurConfigGroup.SINGLE_INSTANCE;

		public readonly HomepageConfigGroup Homepage = HomepageConfigGroup.SINGLE_INSTANCE;

		public readonly MusicConfigGroup Music = MusicConfigGroup.SINGLE_INSTANCE;

		public readonly HideConfigGroup Hide = HideConfigGroup.SINGLE_INSTANCE;

		public static readonly PreferenceConfigGroup SINGLE_INSTANCE = new PreferenceConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<bool>("UiLauncherLogo", true, ConfigSource.Local)]
		public bool ShowStartupLogo
		{
			get
			{
				return ShowStartupLogoConfig.GetValue();
			}
			set
			{
				ShowStartupLogoConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("UiLockWindowSize", false, ConfigSource.Shared)]
		public bool LockWindowSize
		{
			get
			{
				return LockWindowSizeConfig.GetValue();
			}
			set
			{
				LockWindowSizeConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("UiShowLaunchingHint", true, ConfigSource.Local)]
		public bool ShowLaunchingHint
		{
			get
			{
				return ShowLaunchingHintConfig.GetValue();
			}
			set
			{
				ShowLaunchingHintConfig.SetValue(value);
			}
		}

		[ConfigItem<LauncherTitleType>("UiLogoType", LauncherTitleType.Default, ConfigSource.Local)]
		public LauncherTitleType WindowTitleType
		{
			get
			{
				return WindowTitleTypeConfig.GetValue();
			}
			set
			{
				WindowTitleTypeConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("UiLogoText", "", ConfigSource.Local)]
		public string WindowTitleCustomText
		{
			get
			{
				return WindowTitleCustomTextConfig.GetValue();
			}
			set
			{
				WindowTitleCustomTextConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("UiLogoLeft", false, ConfigSource.Local)]
		public bool TopBarLeftAlign
		{
			get
			{
				return TopBarLeftAlignConfig.GetValue();
			}
			set
			{
				TopBarLeftAlignConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("UiFont", "", ConfigSource.Local)]
		public string Font
		{
			get
			{
				return FontConfig.GetValue();
			}
			set
			{
				FontConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("UiMotdFont", "", ConfigSource.Local)]
		public string MotdFont
		{
			get
			{
				return MotdFontConfig.GetValue();
			}
			set
			{
				MotdFontConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("DetailedInstanceClassification", false, ConfigSource.Local)]
		public bool DetailedInstanceClassification
		{
			get
			{
				return DetailedInstanceClassificationConfig.GetValue();
			}
			set
			{
				DetailedInstanceClassificationConfig.SetValue(value);
			}
		}

		public ConfigItem<bool> ShowStartupLogoConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiLauncherLogo"));

		public ConfigItem<bool> LockWindowSizeConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiLockWindowSize"));

		public ConfigItem<bool> ShowLaunchingHintConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiShowLaunchingHint"));

		public ConfigItem<LauncherTitleType> WindowTitleTypeConfig => field ?? (field = ConfigService.GetConfigItem<LauncherTitleType>("UiLogoType"));

		public ConfigItem<string> WindowTitleCustomTextConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiLogoText"));

		public ConfigItem<bool> TopBarLeftAlignConfig => field ?? (field = ConfigService.GetConfigItem<bool>("UiLogoLeft"));

		public ConfigItem<string> FontConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiFont"));

		public ConfigItem<string> MotdFontConfig => field ?? (field = ConfigService.GetConfigItem<string>("UiMotdFont"));

		public ConfigItem<bool> DetailedInstanceClassificationConfig => field ?? (field = ConfigService.GetConfigItem<bool>("DetailedInstanceClassification"));

		private PreferenceConfigGroup()
		{
			_InnerScopes = new IConfigScope[16]
			{
				ShowStartupLogoConfig, LockWindowSizeConfig, ShowLaunchingHintConfig, WindowTitleTypeConfig, WindowTitleCustomTextConfig, TopBarLeftAlignConfig, FontConfig, MotdFontConfig, DetailedInstanceClassificationConfig, Localization,
				Theme, Background, Blur, Homepage, Music, Hide
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

	[ConfigGroup("Launch", ConfigSource.Shared)]
	public sealed class LaunchConfigGroup : IConfigScope
	{
		public static readonly LaunchConfigGroup SINGLE_INSTANCE = new LaunchConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<int>("LaunchRamType", 0, ConfigSource.Local)]
		public int MemoryAllocationMode
		{
			get
			{
				return MemoryAllocationModeConfig.GetValue();
			}
			set
			{
				MemoryAllocationModeConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LaunchRamCustom", 15, ConfigSource.Local)]
		public int CustomMemorySize
		{
			get
			{
				return CustomMemorySizeConfig.GetValue();
			}
			set
			{
				CustomMemorySizeConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LaunchRamCustomInitial", 0, ConfigSource.Local)]
		public int CustomInitialMemorySize
		{
			get
			{
				return CustomInitialMemorySizeConfig.GetValue();
			}
			set
			{
				CustomInitialMemorySizeConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceLockMemory", false, ConfigSource.Local)]
		public bool LockMemory
		{
			get
			{
				return LockMemoryConfig.GetValue();
			}
			set
			{
				LockMemoryConfig.SetValue(value);
			}
		}

		[ConfigItem<JvmPreferredIpStack>("LaunchPreferredIpStack", JvmPreferredIpStack.Default, ConfigSource.Shared)]
		public JvmPreferredIpStack PreferredIpStack
		{
			get
			{
				return PreferredIpStackConfig.GetValue();
			}
			set
			{
				PreferredIpStackConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchAdvanceJvm", "-XX:+UseG1GC -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow -Djdk.lang.Process.allowAmbiguousCommands=true -Dfml.ignoreInvalidMinecraftCertificates=True -Dfml.ignorePatchDiscrepancies=True -Dlog4j2.formatMsgNoLookups=true", ConfigSource.Local)]
		public string JvmArgs
		{
			get
			{
				return JvmArgsConfig.GetValue();
			}
			set
			{
				JvmArgsConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchAdvanceGame", "", ConfigSource.Local)]
		public string GameArgs
		{
			get
			{
				return GameArgsConfig.GetValue();
			}
			set
			{
				GameArgsConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchAdvanceRun", "", ConfigSource.Local)]
		public string PreLaunchCommand
		{
			get
			{
				return PreLaunchCommandConfig.GetValue();
			}
			set
			{
				PreLaunchCommandConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceRunWait", true, ConfigSource.Local)]
		public bool PreLaunchCommandWait
		{
			get
			{
				return PreLaunchCommandWaitConfig.GetValue();
			}
			set
			{
				PreLaunchCommandWaitConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceDisableJLW", true, ConfigSource.Local)]
		public bool DisableJlw
		{
			get
			{
				return DisableJlwConfig.GetValue();
			}
			set
			{
				DisableJlwConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceDisableLF", false, ConfigSource.Local)]
		public bool DisableLF
		{
			get
			{
				return DisableLFConfig.GetValue();
			}
			set
			{
				DisableLFConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceGraphicCard", true, ConfigSource.Shared)]
		public bool SetGpuPreference
		{
			get
			{
				return SetGpuPreferenceConfig.GetValue();
			}
			set
			{
				SetGpuPreferenceConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceNoJavaw", false, ConfigSource.Shared)]
		public bool NoJavaw
		{
			get
			{
				return NoJavawConfig.GetValue();
			}
			set
			{
				NoJavawConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceDisableLwjglUnsafeAgent", false, ConfigSource.Shared)]
		public bool DisableLwjglUnsafeAgent
		{
			get
			{
				return DisableLwjglUnsafeAgentConfig.GetValue();
			}
			set
			{
				DisableLwjglUnsafeAgentConfig.SetValue(value);
			}
		}

		[ConfigItem<bool>("LaunchAdvanceDisableCrashAnalysis", false, ConfigSource.Local)]
		public bool DisableCrashAnalysis
		{
			get
			{
				return DisableCrashAnalysisConfig.GetValue();
			}
			set
			{
				DisableCrashAnalysisConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LaunchAdvanceRenderer", 0, ConfigSource.Local)]
		public int Renderer
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

		[ConfigItem<string>("LaunchArgumentTitle", "", ConfigSource.Local)]
		public string Title
		{
			get
			{
				return TitleConfig.GetValue();
			}
			set
			{
				TitleConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchArgumentInfo", "PCLCE", ConfigSource.Local)]
		public string TypeInfo
		{
			get
			{
				return TypeInfoConfig.GetValue();
			}
			set
			{
				TypeInfoConfig.SetValue(value);
			}
		}

		[ConfigItem<string>("LaunchArgumentJavaSelect", "", ConfigSource.Shared)]
		public string SelectedJava
		{
			get
			{
				return SelectedJavaConfig.GetValue();
			}
			set
			{
				SelectedJavaConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LaunchArgumentIndieV2", 4, ConfigSource.Local)]
		public int IndieSolutionV2
		{
			get
			{
				return IndieSolutionV2Config.GetValue();
			}
			set
			{
				IndieSolutionV2Config.SetValue(value);
			}
		}

		[ConfigItem<LauncherVisibility>("LaunchArgumentVisible", LauncherVisibility.DoNothing, ConfigSource.Shared)]
		public LauncherVisibility LauncherVisibility
		{
			get
			{
				return LauncherVisibilityConfig.GetValue();
			}
			set
			{
				LauncherVisibilityConfig.SetValue(value);
			}
		}

		[ConfigItem<GameProcessPriority>("LaunchArgumentPriority", GameProcessPriority.Normal, ConfigSource.Shared)]
		public GameProcessPriority ProcessPriority
		{
			get
			{
				return ProcessPriorityConfig.GetValue();
			}
			set
			{
				ProcessPriorityConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LaunchArgumentWindowWidth", 854, ConfigSource.Local)]
		public int GameWindowWidth
		{
			get
			{
				return GameWindowWidthConfig.GetValue();
			}
			set
			{
				GameWindowWidthConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LaunchArgumentWindowHeight", 480, ConfigSource.Local)]
		public int GameWindowHeight
		{
			get
			{
				return GameWindowHeightConfig.GetValue();
			}
			set
			{
				GameWindowHeightConfig.SetValue(value);
			}
		}

		[ConfigItem<GameWindowSizeMode>("LaunchArgumentWindowType", GameWindowSizeMode.Default, ConfigSource.Local)]
		public GameWindowSizeMode GameWindowMode
		{
			get
			{
				return GameWindowModeConfig.GetValue();
			}
			set
			{
				GameWindowModeConfig.SetValue(value);
			}
		}

		[ConfigItem<int>("LoginMsAuthType", 1, ConfigSource.Shared)]
		public int LoginMsAuthType
		{
			get
			{
				return LoginMsAuthTypeConfig.GetValue();
			}
			set
			{
				LoginMsAuthTypeConfig.SetValue(value);
			}
		}

		public ConfigItem<int> MemoryAllocationModeConfig => field ?? (field = ConfigService.GetConfigItem<int>("LaunchRamType"));

		public ConfigItem<int> CustomMemorySizeConfig => field ?? (field = ConfigService.GetConfigItem<int>("LaunchRamCustom"));

		public ConfigItem<int> CustomInitialMemorySizeConfig => field ?? (field = ConfigService.GetConfigItem<int>("LaunchRamCustomInitial"));

		public ConfigItem<bool> LockMemoryConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceLockMemory"));

		public ConfigItem<JvmPreferredIpStack> PreferredIpStackConfig => field ?? (field = ConfigService.GetConfigItem<JvmPreferredIpStack>("LaunchPreferredIpStack"));

		public ConfigItem<string> JvmArgsConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchAdvanceJvm"));

		public ConfigItem<string> GameArgsConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchAdvanceGame"));

		public ConfigItem<string> PreLaunchCommandConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchAdvanceRun"));

		public ConfigItem<bool> PreLaunchCommandWaitConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceRunWait"));

		public ConfigItem<bool> DisableJlwConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceDisableJLW"));

		public ConfigItem<bool> DisableLFConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceDisableLF"));

		public ConfigItem<bool> SetGpuPreferenceConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceGraphicCard"));

		public ConfigItem<bool> NoJavawConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceNoJavaw"));

		public ConfigItem<bool> DisableLwjglUnsafeAgentConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceDisableLwjglUnsafeAgent"));

		public ConfigItem<bool> DisableCrashAnalysisConfig => field ?? (field = ConfigService.GetConfigItem<bool>("LaunchAdvanceDisableCrashAnalysis"));

		public ConfigItem<int> RendererConfig => field ?? (field = ConfigService.GetConfigItem<int>("LaunchAdvanceRenderer"));

		public ConfigItem<string> TitleConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchArgumentTitle"));

		public ConfigItem<string> TypeInfoConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchArgumentInfo"));

		public ConfigItem<string> SelectedJavaConfig => field ?? (field = ConfigService.GetConfigItem<string>("LaunchArgumentJavaSelect"));

		public ConfigItem<int> IndieSolutionV2Config => field ?? (field = ConfigService.GetConfigItem<int>("LaunchArgumentIndieV2"));

		public ConfigItem<LauncherVisibility> LauncherVisibilityConfig => field ?? (field = ConfigService.GetConfigItem<LauncherVisibility>("LaunchArgumentVisible"));

		public ConfigItem<GameProcessPriority> ProcessPriorityConfig => field ?? (field = ConfigService.GetConfigItem<GameProcessPriority>("LaunchArgumentPriority"));

		public ConfigItem<int> GameWindowWidthConfig => field ?? (field = ConfigService.GetConfigItem<int>("LaunchArgumentWindowWidth"));

		public ConfigItem<int> GameWindowHeightConfig => field ?? (field = ConfigService.GetConfigItem<int>("LaunchArgumentWindowHeight"));

		public ConfigItem<GameWindowSizeMode> GameWindowModeConfig => field ?? (field = ConfigService.GetConfigItem<GameWindowSizeMode>("LaunchArgumentWindowType"));

		public ConfigItem<int> LoginMsAuthTypeConfig => field ?? (field = ConfigService.GetConfigItem<int>("LoginMsAuthType"));

		private LaunchConfigGroup()
		{
			_InnerScopes = new IConfigScope[26]
			{
				MemoryAllocationModeConfig, CustomMemorySizeConfig, CustomInitialMemorySizeConfig, LockMemoryConfig, PreferredIpStackConfig, JvmArgsConfig, GameArgsConfig, PreLaunchCommandConfig, PreLaunchCommandWaitConfig, DisableJlwConfig,
				DisableLFConfig, SetGpuPreferenceConfig, NoJavawConfig, DisableLwjglUnsafeAgentConfig, DisableCrashAnalysisConfig, RendererConfig, TitleConfig, TypeInfoConfig, SelectedJavaConfig, IndieSolutionV2Config,
				LauncherVisibilityConfig, ProcessPriorityConfig, GameWindowWidthConfig, GameWindowHeightConfig, GameWindowModeConfig, LoginMsAuthTypeConfig
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

	[ConfigGroup("Instance", ConfigSource.GameInstance)]
	public sealed class InstanceConfigGroup : IConfigScope
	{
		private readonly ArgConfig<string> ACCESSOR_JvmArgs;

		private readonly ArgConfig<string> ACCESSOR_GameArgs;

		private readonly ArgConfig<int> ACCESSOR_Renderer;

		private readonly ArgConfig<int> ACCESSOR_AssetVerifySolutionV1;

		private readonly ArgConfig<bool> ACCESSOR_DisableAssetVerifyV2;

		private readonly ArgConfig<bool> ACCESSOR_IgnoreJavaCompatibility;

		private readonly ArgConfig<bool> ACCESSOR_DisableJlwObsolete;

		private readonly ArgConfig<string> ACCESSOR_PreLaunchCommand;

		private readonly ArgConfig<string> ACCESSOR_ClasspathHead;

		private readonly ArgConfig<bool> ACCESSOR_PreLaunchCommandWait;

		private readonly ArgConfig<bool> ACCESSOR_DisableJlw;

		private readonly ArgConfig<bool> ACCESSOR_DisableLwjglUnsafeAgent;

		private readonly ArgConfig<bool> ACCESSOR_UseProxy;

		private readonly ArgConfig<bool> ACCESSOR_DisableLF;

		private readonly ArgConfig<bool> ACCESSOR_UseDebugLof4j2Config;

		private readonly ArgConfig<int> ACCESSOR_MemorySolution;

		private readonly ArgConfig<int> ACCESSOR_CustomMemorySize;

		private readonly ArgConfig<int> ACCESSOR_CustomInitialMemorySize;

		private readonly ArgConfig<string> ACCESSOR_Title;

		private readonly ArgConfig<bool> ACCESSOR_UseGlobalTitle;

		private readonly ArgConfig<string> ACCESSOR_TypeInfo;

		private readonly ArgConfig<int> ACCESSOR_IndieV1;

		private readonly ArgConfig<bool> ACCESSOR_IndieV2;

		private readonly ArgConfig<string> ACCESSOR_SelectedJava;

		private readonly ArgConfig<string> ACCESSOR_ServerToEnter;

		public static readonly InstanceConfigGroup SINGLE_INSTANCE = new InstanceConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<string>("VersionAdvanceJvm", "", ConfigSource.Shared)]
		public ArgConfig<string> JvmArgs => ACCESSOR_JvmArgs;

		[ConfigItem<string>("VersionAdvanceGame", "", ConfigSource.Shared)]
		public ArgConfig<string> GameArgs => ACCESSOR_GameArgs;

		[ConfigItem<int>("VersionAdvanceRenderer", 0, ConfigSource.Shared)]
		public ArgConfig<int> Renderer => ACCESSOR_Renderer;

		[ConfigItem<int>("VersionAdvanceAssets", 0, ConfigSource.Shared)]
		public ArgConfig<int> AssetVerifySolutionV1 => ACCESSOR_AssetVerifySolutionV1;

		[ConfigItem<bool>("VersionAdvanceAssetsV2", false, ConfigSource.Shared)]
		public ArgConfig<bool> DisableAssetVerifyV2 => ACCESSOR_DisableAssetVerifyV2;

		[ConfigItem<bool>("VersionAdvanceJava", false, ConfigSource.Shared)]
		public ArgConfig<bool> IgnoreJavaCompatibility => ACCESSOR_IgnoreJavaCompatibility;

		[ConfigItem<bool>("VersionAdvanceDisableJlw", false, ConfigSource.Shared)]
		public ArgConfig<bool> DisableJlwObsolete => ACCESSOR_DisableJlwObsolete;

		[ConfigItem<string>("VersionAdvanceRun", "", ConfigSource.Shared)]
		public ArgConfig<string> PreLaunchCommand => ACCESSOR_PreLaunchCommand;

		[ConfigItem<string>("VersionAdvanceClasspathHead", "", ConfigSource.Shared)]
		public ArgConfig<string> ClasspathHead => ACCESSOR_ClasspathHead;

		[ConfigItem<bool>("VersionAdvanceRunWait", true, ConfigSource.Shared)]
		public ArgConfig<bool> PreLaunchCommandWait => ACCESSOR_PreLaunchCommandWait;

		[ConfigItem<bool>("VersionAdvanceDisableJLW", false, ConfigSource.Shared)]
		public ArgConfig<bool> DisableJlw => ACCESSOR_DisableJlw;

		[ConfigItem<bool>("VersionAdvanceDisableLwjglUnsafeAgent", false, ConfigSource.Shared)]
		public ArgConfig<bool> DisableLwjglUnsafeAgent => ACCESSOR_DisableLwjglUnsafeAgent;

		[ConfigItem<bool>("VersionAdvanceUseProxyV2", false, ConfigSource.Shared)]
		public ArgConfig<bool> UseProxy => ACCESSOR_UseProxy;

		[ConfigItem<bool>("VersionAdvanceDisableLF", false, ConfigSource.Shared)]
		public ArgConfig<bool> DisableLF => ACCESSOR_DisableLF;

		[ConfigItem<bool>("VersionUseDebugLog4j2Config", false, ConfigSource.Shared)]
		public ArgConfig<bool> UseDebugLof4j2Config => ACCESSOR_UseDebugLof4j2Config;

		[ConfigItem<int>("VersionRamType", 2, ConfigSource.Shared)]
		public ArgConfig<int> MemorySolution => ACCESSOR_MemorySolution;

		[ConfigItem<int>("VersionRamCustom", 15, ConfigSource.Shared)]
		public ArgConfig<int> CustomMemorySize => ACCESSOR_CustomMemorySize;

		[ConfigItem<int>("VersionRamCustomInitial", 0, ConfigSource.Shared)]
		public ArgConfig<int> CustomInitialMemorySize => ACCESSOR_CustomInitialMemorySize;

		[ConfigItem<string>("VersionArgumentTitle", "", ConfigSource.Shared)]
		public ArgConfig<string> Title => ACCESSOR_Title;

		[ConfigItem<bool>("VersionArgumentTitleEmpty", false, ConfigSource.Shared)]
		public ArgConfig<bool> UseGlobalTitle => ACCESSOR_UseGlobalTitle;

		[ConfigItem<string>("VersionArgumentInfo", "", ConfigSource.Shared)]
		public ArgConfig<string> TypeInfo => ACCESSOR_TypeInfo;

		[ConfigItem<int>("VersionArgumentIndie", -1, ConfigSource.Shared)]
		public ArgConfig<int> IndieV1 => ACCESSOR_IndieV1;

		[ConfigItem<bool>("VersionArgumentIndieV2", false, ConfigSource.Shared)]
		public ArgConfig<bool> IndieV2 => ACCESSOR_IndieV2;

		[ConfigItem<string>("VersionArgumentJavaSelect", "使用全局设置", ConfigSource.Shared)]
		public ArgConfig<string> SelectedJava => ACCESSOR_SelectedJava;

		[ConfigItem<string>("VersionServerEnter", "", ConfigSource.Shared)]
		public ArgConfig<string> ServerToEnter => ACCESSOR_ServerToEnter;

		public ConfigItem<string> JvmArgsConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionAdvanceJvm"));

		public ConfigItem<string> GameArgsConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionAdvanceGame"));

		public ConfigItem<int> RendererConfig => field ?? (field = ConfigService.GetConfigItem<int>("VersionAdvanceRenderer"));

		public ConfigItem<int> AssetVerifySolutionV1Config => field ?? (field = ConfigService.GetConfigItem<int>("VersionAdvanceAssets"));

		public ConfigItem<bool> DisableAssetVerifyV2Config => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceAssetsV2"));

		public ConfigItem<bool> IgnoreJavaCompatibilityConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceJava"));

		public ConfigItem<bool> DisableJlwObsoleteConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceDisableJlw"));

		public ConfigItem<string> PreLaunchCommandConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionAdvanceRun"));

		public ConfigItem<string> ClasspathHeadConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionAdvanceClasspathHead"));

		public ConfigItem<bool> PreLaunchCommandWaitConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceRunWait"));

		public ConfigItem<bool> DisableJlwConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceDisableJLW"));

		public ConfigItem<bool> DisableLwjglUnsafeAgentConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceDisableLwjglUnsafeAgent"));

		public ConfigItem<bool> UseProxyConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceUseProxyV2"));

		public ConfigItem<bool> DisableLFConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionAdvanceDisableLF"));

		public ConfigItem<bool> UseDebugLof4j2ConfigConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionUseDebugLog4j2Config"));

		public ConfigItem<int> MemorySolutionConfig => field ?? (field = ConfigService.GetConfigItem<int>("VersionRamType"));

		public ConfigItem<int> CustomMemorySizeConfig => field ?? (field = ConfigService.GetConfigItem<int>("VersionRamCustom"));

		public ConfigItem<int> CustomInitialMemorySizeConfig => field ?? (field = ConfigService.GetConfigItem<int>("VersionRamCustomInitial"));

		public ConfigItem<string> TitleConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionArgumentTitle"));

		public ConfigItem<bool> UseGlobalTitleConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionArgumentTitleEmpty"));

		public ConfigItem<string> TypeInfoConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionArgumentInfo"));

		public ConfigItem<int> IndieV1Config => field ?? (field = ConfigService.GetConfigItem<int>("VersionArgumentIndie"));

		public ConfigItem<bool> IndieV2Config => field ?? (field = ConfigService.GetConfigItem<bool>("VersionArgumentIndieV2"));

		public ConfigItem<string> SelectedJavaConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionArgumentJavaSelect"));

		public ConfigItem<string> ServerToEnterConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionServerEnter"));

		private InstanceConfigGroup()
		{
			_InnerScopes = new IConfigScope[25]
			{
				JvmArgsConfig, GameArgsConfig, RendererConfig, AssetVerifySolutionV1Config, DisableAssetVerifyV2Config, IgnoreJavaCompatibilityConfig, DisableJlwObsoleteConfig, PreLaunchCommandConfig, ClasspathHeadConfig, PreLaunchCommandWaitConfig,
				DisableJlwConfig, DisableLwjglUnsafeAgentConfig, UseProxyConfig, DisableLFConfig, UseDebugLof4j2ConfigConfig, MemorySolutionConfig, CustomMemorySizeConfig, CustomInitialMemorySizeConfig, TitleConfig, UseGlobalTitleConfig,
				TypeInfoConfig, IndieV1Config, IndieV2Config, SelectedJavaConfig, ServerToEnterConfig
			};
			ACCESSOR_JvmArgs = new ArgConfig<string>((object? arg) => JvmArgsConfig.GetValue(arg), (object? arg, string value) =>
			{
				JvmArgsConfig.SetValue(value, arg);
			});
			ACCESSOR_GameArgs = new ArgConfig<string>((object? arg) => GameArgsConfig.GetValue(arg), (object? arg, string value) =>
			{
				GameArgsConfig.SetValue(value, arg);
			});
			ACCESSOR_Renderer = new ArgConfig<int>((object? arg) => RendererConfig.GetValue(arg), (object? arg, int value) =>
			{
				RendererConfig.SetValue(value, arg);
			});
			ACCESSOR_AssetVerifySolutionV1 = new ArgConfig<int>((object? arg) => AssetVerifySolutionV1Config.GetValue(arg), (object? arg, int value) =>
			{
				AssetVerifySolutionV1Config.SetValue(value, arg);
			});
			ACCESSOR_DisableAssetVerifyV2 = new ArgConfig<bool>((object? arg) => DisableAssetVerifyV2Config.GetValue(arg), (object? arg, bool value) =>
			{
				DisableAssetVerifyV2Config.SetValue(value, arg);
			});
			ACCESSOR_IgnoreJavaCompatibility = new ArgConfig<bool>((object? arg) => IgnoreJavaCompatibilityConfig.GetValue(arg), (object? arg, bool value) =>
			{
				IgnoreJavaCompatibilityConfig.SetValue(value, arg);
			});
			ACCESSOR_DisableJlwObsolete = new ArgConfig<bool>((object? arg) => DisableJlwObsoleteConfig.GetValue(arg), (object? arg, bool value) =>
			{
				DisableJlwObsoleteConfig.SetValue(value, arg);
			});
			ACCESSOR_PreLaunchCommand = new ArgConfig<string>((object? arg) => PreLaunchCommandConfig.GetValue(arg), (object? arg, string value) =>
			{
				PreLaunchCommandConfig.SetValue(value, arg);
			});
			ACCESSOR_ClasspathHead = new ArgConfig<string>((object? arg) => ClasspathHeadConfig.GetValue(arg), (object? arg, string value) =>
			{
				ClasspathHeadConfig.SetValue(value, arg);
			});
			ACCESSOR_PreLaunchCommandWait = new ArgConfig<bool>((object? arg) => PreLaunchCommandWaitConfig.GetValue(arg), (object? arg, bool value) =>
			{
				PreLaunchCommandWaitConfig.SetValue(value, arg);
			});
			ACCESSOR_DisableJlw = new ArgConfig<bool>((object? arg) => DisableJlwConfig.GetValue(arg), (object? arg, bool value) =>
			{
				DisableJlwConfig.SetValue(value, arg);
			});
			ACCESSOR_DisableLwjglUnsafeAgent = new ArgConfig<bool>((object? arg) => DisableLwjglUnsafeAgentConfig.GetValue(arg), (object? arg, bool value) =>
			{
				DisableLwjglUnsafeAgentConfig.SetValue(value, arg);
			});
			ACCESSOR_UseProxy = new ArgConfig<bool>((object? arg) => UseProxyConfig.GetValue(arg), (object? arg, bool value) =>
			{
				UseProxyConfig.SetValue(value, arg);
			});
			ACCESSOR_DisableLF = new ArgConfig<bool>((object? arg) => DisableLFConfig.GetValue(arg), (object? arg, bool value) =>
			{
				DisableLFConfig.SetValue(value, arg);
			});
			ACCESSOR_UseDebugLof4j2Config = new ArgConfig<bool>((object? arg) => UseDebugLof4j2ConfigConfig.GetValue(arg), (object? arg, bool value) =>
			{
				UseDebugLof4j2ConfigConfig.SetValue(value, arg);
			});
			ACCESSOR_MemorySolution = new ArgConfig<int>((object? arg) => MemorySolutionConfig.GetValue(arg), (object? arg, int value) =>
			{
				MemorySolutionConfig.SetValue(value, arg);
			});
			ACCESSOR_CustomMemorySize = new ArgConfig<int>((object? arg) => CustomMemorySizeConfig.GetValue(arg), (object? arg, int value) =>
			{
				CustomMemorySizeConfig.SetValue(value, arg);
			});
			ACCESSOR_CustomInitialMemorySize = new ArgConfig<int>((object? arg) => CustomInitialMemorySizeConfig.GetValue(arg), (object? arg, int value) =>
			{
				CustomInitialMemorySizeConfig.SetValue(value, arg);
			});
			ACCESSOR_Title = new ArgConfig<string>((object? arg) => TitleConfig.GetValue(arg), (object? arg, string value) =>
			{
				TitleConfig.SetValue(value, arg);
			});
			ACCESSOR_UseGlobalTitle = new ArgConfig<bool>((object? arg) => UseGlobalTitleConfig.GetValue(arg), (object? arg, bool value) =>
			{
				UseGlobalTitleConfig.SetValue(value, arg);
			});
			ACCESSOR_TypeInfo = new ArgConfig<string>((object? arg) => TypeInfoConfig.GetValue(arg), (object? arg, string value) =>
			{
				TypeInfoConfig.SetValue(value, arg);
			});
			ACCESSOR_IndieV1 = new ArgConfig<int>((object? arg) => IndieV1Config.GetValue(arg), (object? arg, int value) =>
			{
				IndieV1Config.SetValue(value, arg);
			});
			ACCESSOR_IndieV2 = new ArgConfig<bool>((object? arg) => IndieV2Config.GetValue(arg), (object? arg, bool value) =>
			{
				IndieV2Config.SetValue(value, arg);
			});
			ACCESSOR_SelectedJava = new ArgConfig<string>((object? arg) => SelectedJavaConfig.GetValue(arg), (object? arg, string value) =>
			{
				SelectedJavaConfig.SetValue(value, arg);
			});
			ACCESSOR_ServerToEnter = new ArgConfig<string>((object? arg) => ServerToEnterConfig.GetValue(arg), (object? arg, string value) =>
			{
				ServerToEnterConfig.SetValue(value, arg);
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

	[ConfigGroup("InstanceAuth", ConfigSource.GameInstance)]
	public sealed class InstanceAuthConfigGroup : IConfigScope
	{
		private readonly ArgConfig<int> ACCESSOR_LoginRequirementSolution;

		private readonly ArgConfig<string> ACCESSOR_AuthRegisterAddress;

		private readonly ArgConfig<string> ACCESSOR_AuthServerDisplayName;

		private readonly ArgConfig<string> ACCESSOR_AuthServerAddress;

		private readonly ArgConfig<bool> ACCESSOR_AuthLocked;

		public static readonly InstanceAuthConfigGroup SINGLE_INSTANCE = new InstanceAuthConfigGroup();

		private readonly IConfigScope[] _InnerScopes;

		[ConfigItem<int>("VersionServerLoginRequire", 0, ConfigSource.Shared)]
		public ArgConfig<int> LoginRequirementSolution => ACCESSOR_LoginRequirementSolution;

		[ConfigItem<string>("VersionServerAuthRegister", "", ConfigSource.Shared)]
		public ArgConfig<string> AuthRegisterAddress => ACCESSOR_AuthRegisterAddress;

		[ConfigItem<string>("VersionServerAuthName", "", ConfigSource.Shared)]
		public ArgConfig<string> AuthServerDisplayName => ACCESSOR_AuthServerDisplayName;

		[ConfigItem<string>("VersionServerAuthServer", "", ConfigSource.Shared)]
		public ArgConfig<string> AuthServerAddress => ACCESSOR_AuthServerAddress;

		[ConfigItem<bool>("VersionServerLoginLock", false, ConfigSource.Shared)]
		public ArgConfig<bool> AuthLocked => ACCESSOR_AuthLocked;

		public ConfigItem<int> LoginRequirementSolutionConfig => field ?? (field = ConfigService.GetConfigItem<int>("VersionServerLoginRequire"));

		public ConfigItem<string> AuthRegisterAddressConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionServerAuthRegister"));

		public ConfigItem<string> AuthServerDisplayNameConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionServerAuthName"));

		public ConfigItem<string> AuthServerAddressConfig => field ?? (field = ConfigService.GetConfigItem<string>("VersionServerAuthServer"));

		public ConfigItem<bool> AuthLockedConfig => field ?? (field = ConfigService.GetConfigItem<bool>("VersionServerLoginLock"));

		private InstanceAuthConfigGroup()
		{
			_InnerScopes = new IConfigScope[5] { LoginRequirementSolutionConfig, AuthRegisterAddressConfig, AuthServerDisplayNameConfig, AuthServerAddressConfig, AuthLockedConfig };
			ACCESSOR_LoginRequirementSolution = new ArgConfig<int>((object? arg) => LoginRequirementSolutionConfig.GetValue(arg), (object? arg, int value) =>
			{
				LoginRequirementSolutionConfig.SetValue(value, arg);
			});
			ACCESSOR_AuthRegisterAddress = new ArgConfig<string>((object? arg) => AuthRegisterAddressConfig.GetValue(arg), (object? arg, string value) =>
			{
				AuthRegisterAddressConfig.SetValue(value, arg);
			});
			ACCESSOR_AuthServerDisplayName = new ArgConfig<string>((object? arg) => AuthServerDisplayNameConfig.GetValue(arg), (object? arg, string value) =>
			{
				AuthServerDisplayNameConfig.SetValue(value, arg);
			});
			ACCESSOR_AuthServerAddress = new ArgConfig<string>((object? arg) => AuthServerAddressConfig.GetValue(arg), (object? arg, string value) =>
			{
				AuthServerAddressConfig.SetValue(value, arg);
			});
			ACCESSOR_AuthLocked = new ArgConfig<bool>((object? arg) => AuthLockedConfig.GetValue(arg), (object? arg, bool value) =>
			{
				AuthLockedConfig.SetValue(value, arg);
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

	public static readonly SystemConfigGroup System = SystemConfigGroup.SINGLE_INSTANCE;

	public static readonly NetworkConfigGroup Network = NetworkConfigGroup.SINGLE_INSTANCE;

	public static readonly DebugConfigGroup Debug = DebugConfigGroup.SINGLE_INSTANCE;

	public static readonly DownloadConfigGroup Download = DownloadConfigGroup.SINGLE_INSTANCE;

	public static readonly ToolConfigGroup Tool = ToolConfigGroup.SINGLE_INSTANCE;

	public static readonly UpdateConfigGroup Update = UpdateConfigGroup.SINGLE_INSTANCE;

	public static readonly LinkConfigGroup Link = LinkConfigGroup.SINGLE_INSTANCE;

	public static readonly PreferenceConfigGroup Preference = PreferenceConfigGroup.SINGLE_INSTANCE;

	public static readonly LaunchConfigGroup Launch = LaunchConfigGroup.SINGLE_INSTANCE;

	public static readonly InstanceConfigGroup Instance = InstanceConfigGroup.SINGLE_INSTANCE;

	public static readonly InstanceAuthConfigGroup InstanceAuth = InstanceAuthConfigGroup.SINGLE_INSTANCE;
}
