using System;
using System.Collections.Generic;
using PCL.Core.App.Configuration;
using PCL.Core.App.Database;
using PCL.Core.App.Essentials;
using PCL.Core.App.EventBus;
using PCL.Core.App.Localization;
using PCL.Core.App.Tools;
using PCL.Core.IO.Net;
using PCL.Core.IO.Storage.Cache;
using PCL.Core.Logging;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.Profile;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Theme;

namespace PCL.Core.App.IoC;

public static class LifecycleServiceTypes
{
	public static readonly Type[] BeforeLoading = new Type[7]
	{
		typeof(StartupService),
		typeof(Lifecycle),
		typeof(UpdateService),
		typeof(EventBusService),
		typeof(PromoteService),
		typeof(SingleInstanceService),
		typeof(ApplicationService)
	};

	public static readonly Type[] Loaded = new Type[4]
	{
		typeof(LocalizationService),
		typeof(RpcService),
		typeof(JavaService),
		typeof(ProfileService)
	};

	public static readonly Type[] Loading = new Type[5]
	{
		typeof(LogService),
		typeof(ConfigService),
		typeof(CacheServiceManager),
		typeof(DatabaseService),
		typeof(NetworkService)
	};

	public static readonly Type[] Running = new Type[2]
	{
		typeof(TelemetryService),
		typeof(DependencyCheckService)
	};

	public static readonly Type[] WindowCreating = new Type[3]
	{
		typeof(MainWindowService),
		typeof(AnimationService),
		typeof(ThemeService)
	};

	public static Type[] GetServiceTypes(LifecycleState state)
	{
		return state switch
		{
			LifecycleState.BeforeLoading => BeforeLoading, 
			LifecycleState.Loaded => Loaded, 
			LifecycleState.Loading => Loading, 
			LifecycleState.Running => Running, 
			LifecycleState.WindowCreating => WindowCreating, 
			_ => new Type[0], 
		};
	}

	public static Dictionary<LifecycleState, Type[]> GetAllServiceTypes()
	{
		return new Dictionary<LifecycleState, Type[]>
		{
			[LifecycleState.BeforeLoading] = BeforeLoading,
			[LifecycleState.Loaded] = Loaded,
			[LifecycleState.Loading] = Loading,
			[LifecycleState.Running] = Running,
			[LifecycleState.WindowCreating] = WindowCreating
		};
	}

	public static (int StateCount, int TotalServices) GetStatistics()
	{
		return (StateCount: 5, TotalServices: 21);
	}
}
