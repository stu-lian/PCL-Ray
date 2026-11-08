using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.UI.Theme;
using PCL.Core.Utils.Exts;
using PCL.Network;

namespace PCL;

public class ModSetup
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static ConfigEventHandler _003C_003E9__0_0;

		public static ConfigEventHandler _003C_003E9__0_1;

		public static ConfigEventHandler _003C_003E9__0_2;

		public static ConfigEventHandler _003C_003E9__0_3;

		public static ConfigEventHandler _003C_003E9__0_4;

		public static ConfigEventHandler _003C_003E9__0_5;

		public static ConfigEventHandler _003C_003E9__0_6;

		public static ConfigEventHandler _003C_003E9__0_7;

		public static ConfigEventHandler _003C_003E9__0_8;

		public static ConfigEventHandler _003C_003E9__0_9;

		public static ConfigEventHandler _003C_003E9__0_10;

		public static ConfigEventHandler _003C_003E9__0_11;

		public static ConfigEventHandler _003C_003E9__0_12;

		public static ConfigEventHandler _003C_003E9__0_13;

		public static ConfigEventHandler _003C_003E9__0_14;

		public static ConfigEventHandler _003C_003E9__0_15;

		public static ConfigEventHandler _003C_003E9__0_16;

		public static ConfigEventHandler _003C_003E9__0_17;

		public static ConfigEventHandler _003C_003E9__0_18;

		public static ConfigEventHandler _003C_003E9__0_19;

		public static ConfigEventHandler _003C_003E9__0_20;

		public static ConfigEventHandler _003C_003E9__0_21;

		public static ConfigEventHandler _003C_003E9__0_22;

		public static ConfigEventHandler _003C_003E9__0_23;

		public static ConfigEventHandler _003C_003E9__0_24;

		public static ConfigEventHandler _003C_003E9__0_25;

		public static ConfigEventHandler _003C_003E9__0_26;

		public static ConfigEventHandler _003C_003E9__0_27;

		public static ConfigEventHandler _003C_003E9__0_28;

		public static ConfigEventHandler _003C_003E9__0_29;

		public static ConfigEventHandler _003C_003E9__0_30;

		public static ConfigEventHandler _003C_003E9__0_31;

		internal void _003C_002Ector_003Eb__0_0(ConfigEventArgs _)
		{
			PageSetupUI.HiddenRefresh();
		}

		internal void _003C_002Ector_003Eb__0_1(ConfigEventArgs e)
		{
			LaunchRamType((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_2(ConfigEventArgs e)
		{
			LaunchFolderSelect((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_3(ConfigEventArgs e)
		{
			LaunchInstanceSelect((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_4(ConfigEventArgs e)
		{
			ToolDownloadThread((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_5(ConfigEventArgs e)
		{
			ToolDownloadFileConnection((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_6(ConfigEventArgs e)
		{
			ToolDownloadSpeed((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_7(ConfigEventArgs e)
		{
			UiLauncherTransparent((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_8(ConfigEventArgs e)
		{
			UiLauncherTheme((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_9(ConfigEventArgs e)
		{
			UiBackgroundColorful((bool)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_10(ConfigEventArgs e)
		{
			UiLockWindowSize((bool)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_11(ConfigEventArgs e)
		{
			UiAutoPauseVideo((bool)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_12(ConfigEventArgs e)
		{
			UiBackgroundOpacity((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_13(ConfigEventArgs e)
		{
			UiBackgroundBlur((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_14(ConfigEventArgs e)
		{
			UiBackgroundSuit((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_15(ConfigEventArgs e)
		{
			UiFont((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_16(ConfigEventArgs e)
		{
			UiCustomType((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_17(ConfigEventArgs e)
		{
			UiBlur((bool)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_18(ConfigEventArgs e)
		{
			UiBlurValue((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_19(ConfigEventArgs e)
		{
			UiBlurSamplingRate((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_20(ConfigEventArgs e)
		{
			UiBlurType((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_21(ConfigEventArgs e)
		{
			UiLogoType((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_22(ConfigEventArgs e)
		{
			UiLogoText((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_23(ConfigEventArgs e)
		{
			UiLogoLeft((bool)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_24(ConfigEventArgs e)
		{
			SystemDebugMode((bool)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_25(ConfigEventArgs e)
		{
			SystemDebugAnim((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_26(ConfigEventArgs e)
		{
			SystemHttpProxy((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_27(ConfigEventArgs e)
		{
			SystemHttpProxyType((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_28(ConfigEventArgs e)
		{
			SystemHttpProxyCustomUsername((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_29(ConfigEventArgs e)
		{
			SystemHttpProxyCustomPassword((string)(e.Value ?? ""));
		}

		internal void _003C_002Ector_003Eb__0_30(ConfigEventArgs e)
		{
			VersionRamType((int)e.Value);
		}

		internal void _003C_002Ector_003Eb__0_31(ConfigEventArgs e)
		{
			VersionServerLogin((int)e.Value);
		}
	}

	public ModSetup()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected Obj, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected Obj, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected Obj, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected Obj, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected Obj, but got Unknown
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected Obj, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected Obj, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected Obj, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected Obj, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected Obj, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected Obj, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected Obj, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Expected Obj, but got Unknown
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Expected Obj, but got Unknown
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected Obj, but got Unknown
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Expected Obj, but got Unknown
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Expected Obj, but got Unknown
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected Obj, but got Unknown
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Expected Obj, but got Unknown
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Expected Obj, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Expected Obj, but got Unknown
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Expected Obj, but got Unknown
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Expected Obj, but got Unknown
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Expected Obj, but got Unknown
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Expected Obj, but got Unknown
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Expected Obj, but got Unknown
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Expected Obj, but got Unknown
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Expected Obj, but got Unknown
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Expected Obj, but got Unknown
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Expected Obj, but got Unknown
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Expected Obj, but got Unknown
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Expected Obj, but got Unknown
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Expected Obj, but got Unknown
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Expected Obj, but got Unknown
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Expected Obj, but got Unknown
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Expected Obj, but got Unknown
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Expected Obj, but got Unknown
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Expected Obj, but got Unknown
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Expected Obj, but got Unknown
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0464: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Expected Obj, but got Unknown
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Expected Obj, but got Unknown
		//IL_049a: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Expected Obj, but got Unknown
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e6: Expected Obj, but got Unknown
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Expected Obj, but got Unknown
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_051c: Expected Obj, but got Unknown
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_050b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Expected Obj, but got Unknown
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_0552: Expected Obj, but got Unknown
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0541: Unknown result type (might be due to invalid IL or missing references)
		//IL_0547: Expected Obj, but got Unknown
		//IL_057e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0588: Expected Obj, but got Unknown
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Expected Obj, but got Unknown
		//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05be: Expected Obj, but got Unknown
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b3: Expected Obj, but got Unknown
		//IL_05ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f9: Expected Obj, but got Unknown
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ee: Expected Obj, but got Unknown
		//IL_062a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0634: Expected Obj, but got Unknown
		//IL_061e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Expected Obj, but got Unknown
		//IL_0665: Unknown result type (might be due to invalid IL or missing references)
		//IL_066f: Expected Obj, but got Unknown
		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
		//IL_065e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0664: Expected Obj, but got Unknown
		//IL_06a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06aa: Expected Obj, but got Unknown
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
		//IL_069f: Expected Obj, but got Unknown
		//IL_06d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e0: Expected Obj, but got Unknown
		//IL_06ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d5: Expected Obj, but got Unknown
		//IL_070c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0716: Expected Obj, but got Unknown
		//IL_0700: Unknown result type (might be due to invalid IL or missing references)
		//IL_0705: Unknown result type (might be due to invalid IL or missing references)
		//IL_070b: Expected Obj, but got Unknown
		Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
		ConfigEventHandler val = _003C_003Ec._003C_003E9__0_0;
		if (val == null)
		{
			ConfigEventHandler val2 = (ConfigEventArgs _) =>
			{
				PageSetupUI.HiddenRefresh();
			};
			_003C_003Ec._003C_003E9__0_0 = val2;
			val = val2;
		}
		ConfigService.RegisterObserver((IConfigScope)(object)hide, new ConfigObserver((ConfigEvent)13, val, false));
		ConfigItem<int> memoryAllocationModeConfig = Config.Launch.MemoryAllocationModeConfig;
		ConfigEventHandler val3 = _003C_003Ec._003C_003E9__0_1;
		if (val3 == null)
		{
			ConfigEventHandler val4 = (ConfigEventArgs e) =>
			{
				LaunchRamType((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_1 = val4;
			val3 = val4;
		}
		memoryAllocationModeConfig.Observe(new ConfigObserver((ConfigEvent)13, val3, false));
		ConfigItem<string> selectedFolderConfig = States.Game.SelectedFolderConfig;
		ConfigEventHandler val5 = _003C_003Ec._003C_003E9__0_2;
		if (val5 == null)
		{
			ConfigEventHandler val6 = (ConfigEventArgs e) =>
			{
				LaunchFolderSelect((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_2 = val6;
			val5 = val6;
		}
		selectedFolderConfig.Observe(new ConfigObserver((ConfigEvent)13, val5, false));
		ConfigItem<string> selectedInstanceConfig = States.Game.SelectedInstanceConfig;
		ConfigEventHandler val7 = _003C_003Ec._003C_003E9__0_3;
		if (val7 == null)
		{
			ConfigEventHandler val8 = (ConfigEventArgs e) =>
			{
				LaunchInstanceSelect((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_3 = val8;
			val7 = val8;
		}
		selectedInstanceConfig.Observe(new ConfigObserver((ConfigEvent)13, val7, false));
		ConfigItem<int> threadLimitConfig = Config.Download.ThreadLimitConfig;
		ConfigEventHandler val9 = _003C_003Ec._003C_003E9__0_4;
		if (val9 == null)
		{
			ConfigEventHandler val10 = (ConfigEventArgs e) =>
			{
				ToolDownloadThread((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_4 = val10;
			val9 = val10;
		}
		threadLimitConfig.Observe(new ConfigObserver((ConfigEvent)13, val9, false));
		ConfigItem<int> fileConnectionLimitConfig = Config.Download.FileConnectionLimitConfig;
		ConfigEventHandler val11 = _003C_003Ec._003C_003E9__0_5;
		if (val11 == null)
		{
			ConfigEventHandler val12 = (ConfigEventArgs e) =>
			{
				ToolDownloadFileConnection((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_5 = val12;
			val11 = val12;
		}
		fileConnectionLimitConfig.Observe(new ConfigObserver((ConfigEvent)13, val11, false));
		ConfigItem<int> speedLimitConfig = Config.Download.SpeedLimitConfig;
		ConfigEventHandler val13 = _003C_003Ec._003C_003E9__0_6;
		if (val13 == null)
		{
			ConfigEventHandler val14 = (ConfigEventArgs e) =>
			{
				ToolDownloadSpeed((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_6 = val14;
			val13 = val14;
		}
		speedLimitConfig.Observe(new ConfigObserver((ConfigEvent)13, val13, false));
		ConfigItem<int> windowOpacityConfig = Config.Preference.Theme.WindowOpacityConfig;
		ConfigEventHandler val15 = _003C_003Ec._003C_003E9__0_7;
		if (val15 == null)
		{
			ConfigEventHandler val16 = (ConfigEventArgs e) =>
			{
				UiLauncherTransparent((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_7 = val16;
			val15 = val16;
		}
		windowOpacityConfig.Observe(new ConfigObserver((ConfigEvent)13, val15, false));
		ConfigItem<int> themeSelectedConfig = Config.Preference.Theme.ThemeSelectedConfig;
		ConfigEventHandler val17 = _003C_003Ec._003C_003E9__0_8;
		if (val17 == null)
		{
			ConfigEventHandler val18 = (ConfigEventArgs e) =>
			{
				UiLauncherTheme((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_8 = val18;
			val17 = val18;
		}
		themeSelectedConfig.Observe(new ConfigObserver((ConfigEvent)13, val17, false));
		ConfigItem<bool> backgroundColorfulConfig = Config.Preference.Background.BackgroundColorfulConfig;
		ConfigEventHandler val19 = _003C_003Ec._003C_003E9__0_9;
		if (val19 == null)
		{
			ConfigEventHandler val20 = (ConfigEventArgs e) =>
			{
				UiBackgroundColorful((bool)e.Value);
			};
			_003C_003Ec._003C_003E9__0_9 = val20;
			val19 = val20;
		}
		backgroundColorfulConfig.Observe(new ConfigObserver((ConfigEvent)13, val19, false));
		ConfigItem<bool> lockWindowSizeConfig = Config.Preference.LockWindowSizeConfig;
		ConfigEventHandler val21 = _003C_003Ec._003C_003E9__0_10;
		if (val21 == null)
		{
			ConfigEventHandler val22 = (ConfigEventArgs e) =>
			{
				UiLockWindowSize((bool)e.Value);
			};
			_003C_003Ec._003C_003E9__0_10 = val22;
			val21 = val22;
		}
		lockWindowSizeConfig.Observe(new ConfigObserver((ConfigEvent)13, val21, false));
		ConfigItem<bool> autoPauseVideoConfig = Config.Preference.Background.AutoPauseVideoConfig;
		ConfigEventHandler val23 = _003C_003Ec._003C_003E9__0_11;
		if (val23 == null)
		{
			ConfigEventHandler val24 = (ConfigEventArgs e) =>
			{
				UiAutoPauseVideo((bool)e.Value);
			};
			_003C_003Ec._003C_003E9__0_11 = val24;
			val23 = val24;
		}
		autoPauseVideoConfig.Observe(new ConfigObserver((ConfigEvent)13, val23, false));
		ConfigItem<int> wallpaperOpacityConfig = Config.Preference.Background.WallpaperOpacityConfig;
		ConfigEventHandler val25 = _003C_003Ec._003C_003E9__0_12;
		if (val25 == null)
		{
			ConfigEventHandler val26 = (ConfigEventArgs e) =>
			{
				UiBackgroundOpacity((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_12 = val26;
			val25 = val26;
		}
		wallpaperOpacityConfig.Observe(new ConfigObserver((ConfigEvent)13, val25, false));
		ConfigItem<int> wallpaperBlurRadiusConfig = Config.Preference.Background.WallpaperBlurRadiusConfig;
		ConfigEventHandler val27 = _003C_003Ec._003C_003E9__0_13;
		if (val27 == null)
		{
			ConfigEventHandler val28 = (ConfigEventArgs e) =>
			{
				UiBackgroundBlur((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_13 = val28;
			val27 = val28;
		}
		wallpaperBlurRadiusConfig.Observe(new ConfigObserver((ConfigEvent)13, val27, false));
		ConfigItem<int> wallpaperSuitModeConfig = Config.Preference.Background.WallpaperSuitModeConfig;
		ConfigEventHandler val29 = _003C_003Ec._003C_003E9__0_14;
		if (val29 == null)
		{
			ConfigEventHandler val30 = (ConfigEventArgs e) =>
			{
				UiBackgroundSuit((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_14 = val30;
			val29 = val30;
		}
		wallpaperSuitModeConfig.Observe(new ConfigObserver((ConfigEvent)13, val29, false));
		ConfigItem<string> fontConfig = Config.Preference.FontConfig;
		ConfigEventHandler val31 = _003C_003Ec._003C_003E9__0_15;
		if (val31 == null)
		{
			ConfigEventHandler val32 = (ConfigEventArgs e) =>
			{
				UiFont((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_15 = val32;
			val31 = val32;
		}
		fontConfig.Observe(new ConfigObserver((ConfigEvent)13, val31, false));
		ConfigItem<int> typeConfig = Config.Preference.Homepage.TypeConfig;
		ConfigEventHandler val33 = _003C_003Ec._003C_003E9__0_16;
		if (val33 == null)
		{
			ConfigEventHandler val34 = (ConfigEventArgs e) =>
			{
				UiCustomType((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_16 = val34;
			val33 = val34;
		}
		typeConfig.Observe(new ConfigObserver((ConfigEvent)13, val33, false));
		ConfigItem<bool> isEnabledConfig = Config.Preference.Blur.IsEnabledConfig;
		ConfigEventHandler val35 = _003C_003Ec._003C_003E9__0_17;
		if (val35 == null)
		{
			ConfigEventHandler val36 = (ConfigEventArgs e) =>
			{
				UiBlur((bool)e.Value);
			};
			_003C_003Ec._003C_003E9__0_17 = val36;
			val35 = val36;
		}
		isEnabledConfig.Observe(new ConfigObserver((ConfigEvent)13, val35, false));
		ConfigItem<int> radiusConfig = Config.Preference.Blur.RadiusConfig;
		ConfigEventHandler val37 = _003C_003Ec._003C_003E9__0_18;
		if (val37 == null)
		{
			ConfigEventHandler val38 = (ConfigEventArgs e) =>
			{
				UiBlurValue((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_18 = val38;
			val37 = val38;
		}
		radiusConfig.Observe(new ConfigObserver((ConfigEvent)13, val37, false));
		ConfigItem<int> samplingRateConfig = Config.Preference.Blur.SamplingRateConfig;
		ConfigEventHandler val39 = _003C_003Ec._003C_003E9__0_19;
		if (val39 == null)
		{
			ConfigEventHandler val40 = (ConfigEventArgs e) =>
			{
				UiBlurSamplingRate((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_19 = val40;
			val39 = val40;
		}
		samplingRateConfig.Observe(new ConfigObserver((ConfigEvent)13, val39, false));
		ConfigItem<int> kernelTypeConfig = Config.Preference.Blur.KernelTypeConfig;
		ConfigEventHandler val41 = _003C_003Ec._003C_003E9__0_20;
		if (val41 == null)
		{
			ConfigEventHandler val42 = (ConfigEventArgs e) =>
			{
				UiBlurType((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_20 = val42;
			val41 = val42;
		}
		kernelTypeConfig.Observe(new ConfigObserver((ConfigEvent)13, val41, false));
		ConfigItem<LauncherTitleType> windowTitleTypeConfig = Config.Preference.WindowTitleTypeConfig;
		ConfigEventHandler val43 = _003C_003Ec._003C_003E9__0_21;
		if (val43 == null)
		{
			ConfigEventHandler val44 = (ConfigEventArgs e) =>
			{
				UiLogoType((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_21 = val44;
			val43 = val44;
		}
		windowTitleTypeConfig.Observe(new ConfigObserver((ConfigEvent)13, val43, false));
		ConfigItem<string> windowTitleCustomTextConfig = Config.Preference.WindowTitleCustomTextConfig;
		ConfigEventHandler val45 = _003C_003Ec._003C_003E9__0_22;
		if (val45 == null)
		{
			ConfigEventHandler val46 = (ConfigEventArgs e) =>
			{
				UiLogoText((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_22 = val46;
			val45 = val46;
		}
		windowTitleCustomTextConfig.Observe(new ConfigObserver((ConfigEvent)13, val45, false));
		ConfigItem<bool> topBarLeftAlignConfig = Config.Preference.TopBarLeftAlignConfig;
		ConfigEventHandler val47 = _003C_003Ec._003C_003E9__0_23;
		if (val47 == null)
		{
			ConfigEventHandler val48 = (ConfigEventArgs e) =>
			{
				UiLogoLeft((bool)e.Value);
			};
			_003C_003Ec._003C_003E9__0_23 = val48;
			val47 = val48;
		}
		topBarLeftAlignConfig.Observe(new ConfigObserver((ConfigEvent)13, val47, false));
		ConfigItem<bool> enabledConfig = Config.Debug.EnabledConfig;
		ConfigEventHandler val49 = _003C_003Ec._003C_003E9__0_24;
		if (val49 == null)
		{
			ConfigEventHandler val50 = (ConfigEventArgs e) =>
			{
				SystemDebugMode((bool)e.Value);
			};
			_003C_003Ec._003C_003E9__0_24 = val50;
			val49 = val50;
		}
		enabledConfig.Observe(new ConfigObserver((ConfigEvent)13, val49, false));
		ConfigItem<int> animationSpeedConfig = Config.Debug.AnimationSpeedConfig;
		ConfigEventHandler val51 = _003C_003Ec._003C_003E9__0_25;
		if (val51 == null)
		{
			ConfigEventHandler val52 = (ConfigEventArgs e) =>
			{
				SystemDebugAnim((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_25 = val52;
			val51 = val52;
		}
		animationSpeedConfig.Observe(new ConfigObserver((ConfigEvent)13, val51, false));
		ConfigItem<string> customAddressConfig = Config.Network.HttpProxy.CustomAddressConfig;
		ConfigEventHandler val53 = _003C_003Ec._003C_003E9__0_26;
		if (val53 == null)
		{
			ConfigEventHandler val54 = (ConfigEventArgs e) =>
			{
				SystemHttpProxy((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_26 = val54;
			val53 = val54;
		}
		customAddressConfig.Observe(new ConfigObserver((ConfigEvent)13, val53, false));
		ConfigItem<int> typeConfig2 = Config.Network.HttpProxy.TypeConfig;
		ConfigEventHandler val55 = _003C_003Ec._003C_003E9__0_27;
		if (val55 == null)
		{
			ConfigEventHandler val56 = (ConfigEventArgs e) =>
			{
				SystemHttpProxyType((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_27 = val56;
			val55 = val56;
		}
		typeConfig2.Observe(new ConfigObserver((ConfigEvent)13, val55, false));
		ConfigItem<string> customUsernameConfig = Config.Network.HttpProxy.CustomUsernameConfig;
		ConfigEventHandler val57 = _003C_003Ec._003C_003E9__0_28;
		if (val57 == null)
		{
			ConfigEventHandler val58 = (ConfigEventArgs e) =>
			{
				SystemHttpProxyCustomUsername((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_28 = val58;
			val57 = val58;
		}
		customUsernameConfig.Observe(new ConfigObserver((ConfigEvent)13, val57, false));
		ConfigItem<string> customPasswordConfig = Config.Network.HttpProxy.CustomPasswordConfig;
		ConfigEventHandler val59 = _003C_003Ec._003C_003E9__0_29;
		if (val59 == null)
		{
			ConfigEventHandler val60 = (ConfigEventArgs e) =>
			{
				SystemHttpProxyCustomPassword((string)(e.Value ?? ""));
			};
			_003C_003Ec._003C_003E9__0_29 = val60;
			val59 = val60;
		}
		customPasswordConfig.Observe(new ConfigObserver((ConfigEvent)13, val59, false));
		ConfigItem<int> memorySolutionConfig = Config.Instance.MemorySolutionConfig;
		ConfigEventHandler val61 = _003C_003Ec._003C_003E9__0_30;
		if (val61 == null)
		{
			ConfigEventHandler val62 = (ConfigEventArgs e) =>
			{
				VersionRamType((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_30 = val62;
			val61 = val62;
		}
		memorySolutionConfig.Observe(new ConfigObserver((ConfigEvent)13, val61, false));
		ConfigItem<int> loginRequirementSolutionConfig = Config.InstanceAuth.LoginRequirementSolutionConfig;
		ConfigEventHandler val63 = _003C_003Ec._003C_003E9__0_31;
		if (val63 == null)
		{
			ConfigEventHandler val64 = (ConfigEventArgs e) =>
			{
				VersionServerLogin((int)e.Value);
			};
			_003C_003Ec._003C_003E9__0_31 = val64;
			val63 = val64;
		}
		loginRequirementSolutionConfig.Observe(new ConfigObserver((ConfigEvent)13, val63, false));
	}

	public static void ApplyAll()
	{
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected I4, but got Unknown
		LaunchRamType(Config.Launch.MemoryAllocationMode);
		ToolDownloadThread(Config.Download.ThreadLimit);
		ToolDownloadFileConnection(Config.Download.FileConnectionLimit);
		ToolDownloadSpeed(Config.Download.SpeedLimit);
		UiLauncherTransparent(Config.Preference.Theme.WindowOpacity);
		UiLauncherTheme(Config.Preference.Theme.ThemeSelected);
		UiBackgroundColorful(Config.Preference.Background.BackgroundColorful);
		UiLockWindowSize(Config.Preference.LockWindowSize);
		UiAutoPauseVideo(Config.Preference.Background.AutoPauseVideo);
		UiBackgroundOpacity(Config.Preference.Background.WallpaperOpacity);
		UiBackgroundBlur(Config.Preference.Background.WallpaperBlurRadius);
		UiBackgroundSuit(Config.Preference.Background.WallpaperSuitMode);
		UiFont(Config.Preference.Font);
		UiCustomType(Config.Preference.Homepage.Type);
		if (Config.Preference.Blur.IsEnabled)
		{
			UiBlurValue(Config.Preference.Blur.Radius);
			UiBlurSamplingRate(Config.Preference.Blur.SamplingRate);
			UiBlurType(Config.Preference.Blur.KernelType);
		}
		else
		{
			UiBlurValue(0);
		}
		UiBlur(Config.Preference.Blur.IsEnabled);
		UiLogoType((int)Config.Preference.WindowTitleType);
		UiLogoText(Config.Preference.WindowTitleCustomText);
		UiLogoLeft(Config.Preference.TopBarLeftAlign);
		PageSetupUI.HiddenRefresh();
		SystemDebugMode(Config.Debug.Enabled);
		SystemDebugAnim(Config.Debug.AnimationSpeed);
		SystemHttpProxy(Config.Network.HttpProxy.CustomAddress);
		SystemHttpProxyType(Config.Network.HttpProxy.Type);
		SystemHttpProxyCustomUsername(Config.Network.HttpProxy.CustomUsername);
		SystemHttpProxyCustomPassword(Config.Network.HttpProxy.CustomPassword);
	}

	public static void LaunchInstanceSelect(string value)
	{
		ModBase.Log("[Setup] 当前选择的 Minecraft 版本：" + value);
		ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", value);
	}

	public static void LaunchFolderSelect(string value)
	{
		ModBase.Log("[Setup] 当前选择的 Minecraft 文件夹：" + value.Replace("$", ModBase.exePath));
		ModFolder.mcFolderSelected = value.Replace("$", ModBase.exePath);
	}

	public static void LaunchRamType(int type)
	{
		if (ModMain.frmSetupLaunch != null)
		{
			ModMain.frmSetupLaunch.RamType(type);
		}
	}

	public static void ToolDownloadThread(int value)
	{
		ModNet.NetTaskConnectionLimit = Math.Clamp(value + 1, 1, 256);
		DownloadResourceManager.OnConnectionLimitChanged();
	}

	public static void ToolDownloadFileConnection(int value)
	{
		ModNet.NetTaskSingleFileConnectionLimit = Math.Clamp(value + 1, 1, 8);
	}

	public static void ToolDownloadSpeed(int value)
	{
		long netTaskSpeedLimitHigh;
		if (value <= 31)
		{
			netTaskSpeedLimitHigh = ((value > 14) ? ((long)Math.Round((double)(value - 11) * 0.5 * 1024.0 * 1024.0)) : ((long)Math.Round((double)(value + 1) * 0.1 * 1024.0 * 1024.0)));
		}
		else
		{
			netTaskSpeedLimitHigh = ((value > 41) ? (-1) : ((long)((value - 21) * 1024) * 1024L));
		}
		ModNet.NetTaskSpeedLimitHigh = netTaskSpeedLimitHigh;
	}

	public static void UiLauncherTransparent(int value)
	{
		ModMain.frmMain.Opacity = (double)value / 1000.0 + 0.4;
	}

	public static void UiLauncherTheme(int value)
	{
		ThemeManager.ThemeRefresh(value);
	}

	public static void UiBackgroundColorful(bool value)
	{
		ThemeManager.ThemeRefresh();
	}

	public static void UiLockWindowSize(bool value)
	{
		if (value)
		{
			ModMain.frmMain.RemoveResizer();
		}
		else
		{
			ModMain.frmMain.AddResizer();
		}
	}

	public static void UiAutoPauseVideo(bool value)
	{
		if (!value)
		{
			ModVideoBack.ForcePlay = true;
			ModVideoBack.VideoPlay();
			return;
		}
		ModVideoBack.ForcePlay = false;
		if (ModVideoBack.IsGaming)
		{
			ModVideoBack.VideoPause();
		}
	}

	public static void UiBackgroundOpacity(int value)
	{
		ModMain.frmMain.ImgBack.Opacity = (double)value / 1000.0;
	}

	public static void UiBackgroundBlur(int value)
	{
		ModMain.frmMain.ImgBack.Effect = ((value == 0) ? null : new BlurEffect
		{
			Radius = value + 1
		});
		ModMain.frmMain.ImgBack.Margin = new Thickness((double)(-(value + 1)) / 1.8);
	}

	public static void UiBackgroundSuit(int value)
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		if (ModMain.frmMain.ImgBack.Background != null)
		{
			double width = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width;
			double height = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height;
			if (value == 0)
			{
				value = ((!(width < ModMain.frmMain.PanMain.ActualWidth / 2.0) || !(height < ModMain.frmMain.PanMain.ActualHeight / 2.0)) ? 2 : 4);
			}
			((ImageBrush)ModMain.frmMain.ImgBack.Background).TileMode = TileMode.None;
			((ImageBrush)ModMain.frmMain.ImgBack.Background).Viewport = new Rect(0.0, 0.0, 1.0, 1.0);
			((ImageBrush)ModMain.frmMain.ImgBack.Background).ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
			switch (value)
			{
			case 1:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Center;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Center;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.None;
				ModMain.frmMain.ImgBack.Width = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width;
				ModMain.frmMain.ImgBack.Height = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height;
				break;
			case 2:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Stretch;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Stretch;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.UniformToFill;
				ModMain.frmMain.ImgBack.Width = double.NaN;
				ModMain.frmMain.ImgBack.Height = double.NaN;
				break;
			case 3:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Stretch;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Stretch;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.Fill;
				ModMain.frmMain.ImgBack.Width = double.NaN;
				ModMain.frmMain.ImgBack.Height = double.NaN;
				break;
			case 4:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Stretch;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Stretch;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.None;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).TileMode = TileMode.Tile;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Viewport = new Rect(0.0, 0.0, ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width, ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height);
				((ImageBrush)ModMain.frmMain.ImgBack.Background).ViewportUnits = BrushMappingMode.Absolute;
				ModMain.frmMain.ImgBack.Width = double.NaN;
				ModMain.frmMain.ImgBack.Height = double.NaN;
				break;
			case 5:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Left;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Top;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.None;
				ModMain.frmMain.ImgBack.Width = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width;
				ModMain.frmMain.ImgBack.Height = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height;
				break;
			case 6:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Right;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Top;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.None;
				ModMain.frmMain.ImgBack.Width = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width;
				ModMain.frmMain.ImgBack.Height = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height;
				break;
			case 7:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Left;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Bottom;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.None;
				ModMain.frmMain.ImgBack.Width = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width;
				ModMain.frmMain.ImgBack.Height = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height;
				break;
			case 8:
				ModMain.frmMain.ImgBack.HorizontalAlignment = HorizontalAlignment.Right;
				ModMain.frmMain.ImgBack.VerticalAlignment = VerticalAlignment.Bottom;
				((ImageBrush)ModMain.frmMain.ImgBack.Background).Stretch = Stretch.None;
				ModMain.frmMain.ImgBack.Width = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Width;
				ModMain.frmMain.ImgBack.Height = ((ImageBrush)ModMain.frmMain.ImgBack.Background).ImageSource.Height;
				break;
			}
		}
	}

	public static void UiFont(string value)
	{
		try
		{
			ModBase.SetLaunchFont(value);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "字体加载失败", ModBase.LogLevel.Hint, null, Lang.Text("Setup.Error.OperationFailed"));
		}
	}

	public static void UiCustomType(int value)
	{
		if (ModMain.frmSetupUI != null)
		{
			switch (value)
			{
			case 0:
				ModMain.frmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.HintCustom.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.HintCustomWarn.Visibility = Visibility.Collapsed;
				break;
			case 1:
				ModMain.frmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanCustomLocal.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.HintCustom.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.HintCustomWarn.Visibility = (States.Hint.UntrustedHomepage ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupUI.HintCustom.Text = Lang.Text("Setup.Ui.Homepage.LocalFile.Hint");
				CustomEventService.SetEventType((DependencyObject)(object)ModMain.frmSetupUI.HintCustom, EventType.None);
				break;
			case 2:
				ModMain.frmSetupUI.PanCustomPreset.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanCustomNet.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.HintCustom.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.HintCustomWarn.Visibility = (States.Hint.UntrustedHomepage ? Visibility.Collapsed : Visibility.Visible);
				ModMain.frmSetupUI.HintCustom.Text = Lang.Text("Setup.Ui.Homepage.NetUpdate.Hint");
				CustomEventService.SetEventType((DependencyObject)(object)ModMain.frmSetupUI.HintCustom, EventType.OpenUrl);
				CustomEventService.SetEventData((DependencyObject)(object)ModMain.frmSetupUI.HintCustom, "https://github.com/stu-lian/PCL-Ray/discussions");
				break;
			case 3:
				ModMain.frmSetupUI.PanCustomPreset.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.PanCustomLocal.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanCustomNet.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.HintCustom.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.HintCustomWarn.Visibility = Visibility.Collapsed;
				break;
			}
			ModMain.frmSetupUI.CardCustom.TriggerForceResize();
		}
	}

	public static void UiBlur(bool value)
	{
		if (ModMain.frmSetupUI != null)
		{
			ModMain.frmSetupUI.PanBlurValue.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
			UiBlurValue(value ? Config.Preference.Blur.Radius : 0);
		}
	}

	public static void UiBlurValue(int value)
	{
		System.Windows.Application.Current.Resources["BlurRadius"] = (double)value * 1.0;
	}

	public static void UiBlurSamplingRate(int value)
	{
		System.Windows.Application.Current.Resources["BlurSamplingRate"] = (double)value * 0.01;
	}

	public static void UiBlurType(int value)
	{
		System.Windows.Application.Current.Resources["BlurType"] = (KernelType)value;
	}

	public static void UiLogoType(int value)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		if ((int)ThemeService.CurrentTheme == 3)
		{
			value = 4;
		}
		switch (value)
		{
		case 0:
			ModMain.frmMain.ShapeTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.BtnTitleHelp.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ShapeHMCLTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.LabTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ImageTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.RayLogo.Visibility = Visibility.Collapsed;
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.CheckLogoLeft.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.PanLogoText.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed;
			}
			break;
		case 1:
			ModMain.frmMain.ShapeTitleLogo.Visibility = Visibility.Visible;
			ModMain.frmMain.BtnTitleHelp.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ShapeHMCLTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.LabTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ImageTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.RayLogo.Visibility = Visibility.Visible;
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoText.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed;
			}
			break;
		case 2:
			ModMain.frmMain.ShapeTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.BtnTitleHelp.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ShapeHMCLTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.LabTitleLogo.Visibility = Visibility.Visible;
			ModMain.frmMain.ImageTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.RayLogo.Visibility = Visibility.Visible;
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoText.Visibility = Visibility.Visible;
				ModMain.frmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed;
			}
			_ = Config.Preference.WindowTitleCustomText;
			break;
		case 3:
			ModMain.frmMain.ShapeTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.BtnTitleHelp.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ShapeHMCLTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.LabTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ImageTitleLogo.Visibility = Visibility.Visible;
			ModMain.frmMain.RayLogo.Visibility = Visibility.Visible;
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoText.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoChange.Visibility = Visibility.Visible;
			}
			try
			{
				ModMain.frmMain.ImageTitleLogo.Source = ModBase.exePath + "PCL\\Logo.png";
			}
			catch (Exception ex)
			{
				ModMain.frmMain.ImageTitleLogo.Source = null;
				ModBase.Log(ex, "显示标题栏图片失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Setup.Error.OperationFailed"));
			}
			break;
		case 4:
			ModMain.frmMain.ShapeTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ShapeHMCLTitleLogo.Visibility = Visibility.Visible;
			ModMain.frmMain.LabTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.ImageTitleLogo.Visibility = Visibility.Collapsed;
			ModMain.frmMain.BtnTitleHelp.Visibility = Visibility.Visible;
			if (ModMain.frmSetupUI != null)
			{
				ModMain.frmSetupUI.CheckLogoLeft.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoText.Visibility = Visibility.Collapsed;
				ModMain.frmSetupUI.PanLogoChange.Visibility = Visibility.Collapsed;
			}
			break;
		}
		UiLogoLeft(Config.Preference.TopBarLeftAlign);
		if (ModMain.frmSetupUI != null)
		{
			ModMain.frmSetupUI.CardLogo.TriggerForceResize();
		}
	}

	public static void UiLogoText(string value)
	{
		ModMain.frmMain.LabTitleLogo.Text = value;
	}

	public static void UiLogoLeft(bool value)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		ModMain.frmMain.PanTitleMain.ColumnDefinitions[0].Width = new GridLength((!value || (int)Config.Preference.WindowTitleType != 0) ? 1 : 0, GridUnitType.Star);
	}

	public static void SystemDebugMode(bool value)
	{
		ModBase.modeDebug = value;
	}

	public static void SystemDebugAnim(int value)
	{
		ModAnimation.aniSpeed = ((value >= 30) ? 200.0 : ModBase.MathClamp((double)value * 0.1 + 0.1, 0.1, 3.0));
	}

	public static void SystemHttpProxy(string value)
	{
		if (StringExtension.IsNullOrWhiteSpace(value))
		{
			return;
		}
		try
		{
			HttpProxyManager.Instance.CustomProxyAddress = new Uri(value);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "HTTP 代理应用出错");
		}
	}

	public static void SystemHttpProxyType(int value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		HttpProxyManager.ProxyMode val = (HttpProxyManager.ProxyMode)value;
		HttpProxyManager.Instance.Mode = (Enum.IsDefined<HttpProxyManager.ProxyMode>(val) ? val : HttpProxyManager.Instance.Mode);
	}

	public static void SystemHttpProxyCustomUsername(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			string customPassword = Config.Network.HttpProxy.CustomPassword;
			HttpProxyManager.Instance.Credentials = new NetworkCredential(value, customPassword);
		}
		else
		{
			HttpProxyManager.Instance.Credentials = null;
		}
	}

	public static void SystemHttpProxyCustomPassword(string value)
	{
		string customUsername = Config.Network.HttpProxy.CustomUsername;
		HttpProxyManager.Instance.Credentials = ((!string.IsNullOrEmpty(customUsername)) ? new NetworkCredential(customUsername, value) : null);
	}

	public static void VersionRamType(int type)
	{
		if (ModMain.frmInstanceSetup != null)
		{
			ModMain.frmInstanceSetup.RamType(type);
		}
	}

	public static void VersionServerLogin(int type)
	{
		if (ModMain.frmInstanceSetup != null)
		{
			ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
			if ((object)PageInstanceLeft.McInstance != null)
			{
				PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
			}
		}
	}
}
