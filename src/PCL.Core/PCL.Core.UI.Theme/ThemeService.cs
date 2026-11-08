using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.IoC;

namespace PCL.Core.UI.Theme;

[LifecycleScope("theme", "主题", false)]
[LifecycleService(LifecycleState.WindowCreating)]
public sealed class ThemeService : ILifecycleService
{
	[AnyConfigItem<ToneProfileConfig>("UiToneProfiles", ConfigSource.Local)]
	public static ToneProfileConfig ToneProfiles
	{
		get
		{
			return ToneProfilesConfig.GetValue();
		}
		set
		{
			ToneProfilesConfig.SetValue(value);
		}
	}

	[RegisterConfigEvent]
	public static ConfigEventRegistry OnColorModeConfigChanged => new ConfigEventRegistry(Config.Preference.Theme.ColorModeConfig, (ConfigEventArgs _) =>
	{
		RefreshColorMode();
	}, ConfigEvent.Update);

	[RegisterConfigEvent]
	public static ConfigEventRegistry OnColorThemeConfigChanged => new ConfigEventRegistry(new _003C_003Ez__ReadOnlyArray<IConfigScope>(new IConfigScope[2]
	{
		Config.Preference.Theme.DarkColorConfig,
		Config.Preference.Theme.LightColorConfig
	}), (ConfigEventArgs e) =>
	{
		if (e.OldValue != e.Value)
		{
			if (IsDarkMode)
			{
				if (e.Item == Config.Preference.Theme.LightColorConfig)
				{
					return;
				}
			}
			else if (e.Item == Config.Preference.Theme.DarkColorConfig)
			{
				return;
			}
			if (Lifecycle.CurrentState > LifecycleState.Loading)
			{
				((DispatcherObject)Lifecycle.CurrentApplication).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
				{
					ApplyColorResources();
					ColorThemeChanged?.Invoke(CurrentTheme);
					_AprilFoolLogic();
				}), Array.Empty<object>());
			}
		}
	}, ConfigEvent.Update);

	public static bool IsDarkMode { get; private set; }

	public static ToneProfile CurrentTone
	{
		get
		{
			if (!IsDarkMode)
			{
				return ToneProfiles.Light;
			}
			return ToneProfiles.Dark;
		}
	}

	public static ColorTheme CurrentTheme
	{
		get
		{
			Config.PreferenceConfigGroup.ThemeConfigGroup theme = Config.Preference.Theme;
			if (!IsDarkMode)
			{
				return theme.LightColor;
			}
			return theme.DarkColor;
		}
		set
		{
			Config.PreferenceConfigGroup.ThemeConfigGroup theme = Config.Preference.Theme;
			(IsDarkMode ? theme.DarkColorConfig : theme.LightColorConfig).SetValue(value);
		}
	}

	private static CatColorResource[] LightGrayCache
	{
		get
		{
			return field ?? (field = _CalculateGrays(ToneProfiles.Light));
		}
		set;
	}

	private static CatColorResource[] DarkGrayCache
	{
		get
		{
			return field ?? (field = _CalculateGrays(ToneProfiles.Dark));
		}
		set;
	}

	public static ConfigItem<ToneProfileConfig> ToneProfilesConfig => field ?? (field = ConfigService.GetConfigItem<ToneProfileConfig>("UiToneProfiles"));

	public string Identifier => "theme";

	public string Name => "主题";

	public bool SupportAsync => false;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	}

	private static ILifecycleService Service => Context.ServiceInstance;

	public static event ColorModeChangedEvent? ColorModeChanged;

	public static event ColorThemeChangedEvent? ColorThemeChanged;

	[LifecycleStart]
	private static void _Start()
	{
		IsDarkMode = _IsDarkMode();
		_LogStatus();
		_RefreshAll();
	}

	private static void _LogStatus()
	{
		Context.Debug($"当前状态: {(IsDarkMode ? "Dark" : "Light")}, {CurrentTheme}");
	}

	private static bool _IsDarkMode()
	{
		return Config.Preference.Theme.ColorMode switch
		{
			ColorMode.Light => false, 
			ColorMode.Dark => true, 
			ColorMode.System => SystemThemeHelper.IsSystemInDarkMode(), 
			_ => false, 
		};
	}

	private static void _RefreshAll()
	{
		ApplyGrayResources();
		ApplyColorResources();
		ColorModeChanged?.Invoke(IsDarkMode, CurrentTheme);
		_AprilFoolLogic();
	}

	private static void _AprilFoolLogic()
	{
		if (Basics.IsAprilFool)
		{
			Config.Preference.WindowTitleTypeConfig.TriggerEvent(ConfigEvent.Changed, null);
		}
		else if (CurrentTheme == ColorTheme.HmclBlue)
		{
			CurrentTheme = ColorTheme.CatBlue;
		}
	}

	public static void RefreshColorMode()
	{
		bool flag = _IsDarkMode();
		if (IsDarkMode != flag)
		{
			Context.Info("正在更改配色模式");
			IsDarkMode = flag;
			_LogStatus();
			if (Lifecycle.CurrentState > LifecycleState.Loading)
			{
				((DispatcherObject)Lifecycle.CurrentApplication).Dispatcher.BeginInvoke((Delegate)new Action(_RefreshAll), Array.Empty<object>());
			}
		}
	}

	public static (int Hue, double LightAdjust, double ChromaAdjust) GetCurrentThemeArgs()
	{
		ColorTheme currentTheme = CurrentTheme;
		return currentTheme switch
		{
			ColorTheme.SkyBlue => (Hue: 235, LightAdjust: 0.36, ChromaAdjust: 0.2), 
			ColorTheme.CatBlue => (Hue: 255, LightAdjust: 0.0, ChromaAdjust: -0.2), 
			ColorTheme.DeathBlue => (Hue: 268, LightAdjust: -0.05, ChromaAdjust: -0.1), 
			ColorTheme.HmclBlue => (Hue: 275, LightAdjust: -0.03, ChromaAdjust: -0.35), 
			_ => throw new IndexOutOfRangeException($"Invalid theme index: {(int)currentTheme}"), 
		};
	}

	private static double _AdjustLinear(double value, double adjustment)
	{
		if (adjustment == 0.0)
		{
			return value;
		}
		value = Math.Clamp(value, 0.0, 1.0);
		adjustment = Math.Clamp(adjustment, -1.0, 1.0);
		if (adjustment > 0.0)
		{
			return value + (1.0 - value) * adjustment;
		}
		return value + value * adjustment;
	}

	private static CatColorResource[] _CalculateGrays(ToneProfile tone)
	{
		return new CatColorResource[17]
		{
			LabColor.FromLch(tone.L1).ToCatColor("Gray1"),
			LabColor.FromLch(tone.L2).ToCatColor("Gray2"),
			LabColor.FromLch(tone.L3).ToCatColor("Gray3"),
			LabColor.FromLch(tone.L4).ToCatColor("Gray4"),
			LabColor.FromLch(tone.L5).ToCatColor("Gray5"),
			LabColor.FromLch(tone.L6).ToCatColor("Gray6"),
			LabColor.FromLch(tone.L7).ToCatColor("Gray7"),
			LabColor.FromLch(tone.L8).ToCatColor("Gray8"),
			LabColor.FromLch(tone.LWhite, 0.0, 0.0, tone.AHalfWhite).ToCatColor("HalfWhite", applyToColor: false),
			LabColor.FromLch(tone.LWhite, 0.0, 0.0, tone.ASemiWhite).ToCatColor("SemiWhite", applyToColor: false),
			LabColor.FromLch(tone.LWhite).ToCatColor("White", applyToColor: false),
			LabColor.FromLch(tone.LWhite, 0.0, 0.0, tone.ATransparent).ToCatColor("Transparent", applyToColor: false),
			LabColor.FromLch(tone.LBackground, 0.0, 0.0, tone.ABackground).ToCatColor("TransparentBackground", applyToColor: false),
			LabColor.FromLch(tone.LBackground).ToCatColor("Background", applyToColor: false),
			LabColor.FromLch(tone.LBackground, 0.0, 0.0, tone.AToolTip).ToCatColor("ToolTip", applyToColor: false),
			LabColor.FromLch(tone.L7, 0.25, 30.0, tone.AHalfTransparent).ToCatColor("RedBack", applyToColor: false),
			LabColor.FromLch(tone.LForeground).ToCatColor("Memory", applyToColor: false)
		};
	}

	private static CatColorResource[] _CalculateColors(ToneProfile tone, (int hue, double lightAdj, double chromaAdj) args)
	{
		return new CatColorResource[11]
		{
			LabColor.FromLch(_AdjustLinear(tone.L1, args.lightAdj * 0.1), _AdjustLinear(tone.C1, args.chromaAdj * 0.25), args.hue).ToCatColor("1"),
			LabColor.FromLch(_AdjustLinear(tone.L2, args.lightAdj), _AdjustLinear(tone.C2, args.chromaAdj), args.hue).ToCatColor("2"),
			LabColor.FromLch(_AdjustLinear(tone.L3, args.lightAdj), _AdjustLinear(tone.C3, args.chromaAdj), args.hue).ToCatColor("3"),
			LabColor.FromLch(_AdjustLinear(tone.L4, args.lightAdj), _AdjustLinear(tone.C4, args.chromaAdj), args.hue).ToCatColor("4"),
			LabColor.FromLch(_AdjustLinear(tone.L5, args.lightAdj), _AdjustLinear(tone.C5, args.chromaAdj), args.hue).ToCatColor("5"),
			LabColor.FromLch(_AdjustLinear(tone.L6, args.lightAdj), _AdjustLinear(tone.C6, args.chromaAdj), args.hue).ToCatColor("6"),
			LabColor.FromLch(_AdjustLinear(tone.L7, args.lightAdj), _AdjustLinear(tone.C7, args.chromaAdj), args.hue).ToCatColor("7"),
			LabColor.FromLch(_AdjustLinear(tone.L8, args.lightAdj), _AdjustLinear(tone.C8, args.chromaAdj), args.hue).ToCatColor("8"),
			LabColor.FromLch(_AdjustLinear(tone.L8, args.lightAdj), _AdjustLinear(tone.C8, args.chromaAdj), args.hue, tone.ASemiTransparent).ToCatColor("SemiTransparent", applyToColor: false),
			LabColor.FromLch(_AdjustLinear(tone.L5, args.lightAdj), _AdjustLinear(tone.C5, args.chromaAdj), args.hue).ToCatColor("Bg0"),
			LabColor.FromLch(_AdjustLinear(tone.L7, args.lightAdj), _AdjustLinear(tone.C7, args.chromaAdj), args.hue, tone.ASemiWhite).ToCatColor("Bg1")
		};
	}

	public static void InvalidateGrayCache()
	{
		LightGrayCache = null;
		DarkGrayCache = null;
	}

	public static void ApplyGrayResources()
	{
		CatColorResource[] array = (IsDarkMode ? DarkGrayCache : LightGrayCache);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Apply();
		}
	}

	public static void ApplyColorResources()
	{
		CatColorResource[] array = _CalculateColors(CurrentTone, GetCurrentThemeArgs());
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Apply();
		}
	}

	public ThemeService()
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
