using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Minecraft.Launch.Utils;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;
using PCL.Network;

namespace PCL;

public static class ModLaunch
{
	public class McLaunchOptions
	{
		public List<string> ExtraArgs = new List<string>();

		public McInstance instance;

		public bool IsTest;

		public string SaveBatch;

		public string ServerIp;

		public string WorldName;
	}

	public enum McLoginType
	{
		Legacy = 1,
		Auth,
		Ms
	}

	public abstract class McLoginData
	{
		public McLoginType LoginType;

		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				return obj.GetHashCode() == GetHashCode();
			}
			return false;
		}
	}

	public class McLoginServer : McLoginData
	{
		public string BaseUrl;

		public string Description;

		public bool ForceReselectProfile;

		public bool IsExist;

		public string Password;

		public string UserName;

		public string DiscoveryAddress;

		public ProfileType ProviderType = (ProfileType)2;

		public McLoginServer(McLoginType type)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			LoginType = type;
		}

		public override int GetHashCode()
		{
			string userName = UserName;
			string password = Password;
			string baseUrl = BaseUrl;
			int loginType = (int)LoginType;
			return (int)Math.Round((decimal)ModBase.GetHash(userName + password + baseUrl + loginType) % 2147483647m);
		}
	}

	public class McLoginMs : McLoginData
	{
		public McLoginMs()
		{
			LoginType = McLoginType.Ms;
		}

		public override int GetHashCode()
		{
			return (int)Math.Round((decimal)ModBase.GetHash(LoginType.ToString()) % 2147483647m);
		}
	}

	public class McLoginLegacy : McLoginData
	{
		public string SkinName;

		public int SkinType;

		public string UserName;

		public string Uuid;

		public McLoginLegacy()
		{
			LoginType = McLoginType.Legacy;
		}

		public override int GetHashCode()
		{
			string userName = UserName;
			string text = SkinType.ToString();
			string skinName = SkinName;
			int loginType = (int)LoginType;
			return (int)Math.Round((decimal)ModBase.GetHash(userName + text + skinName + loginType) % 2147483647m);
		}
	}

	public struct McLoginResult
	{
		public string Name;

		public string Uuid;

		public string AccessToken;

		public string Type;

		public string ClientToken;

		public string ProfileJson;
	}

	public class LaunchArgument
	{
		private readonly List<string> _features = new List<string>();

		public LaunchArgument(McInstance minecraft)
		{
			_ = string.Empty;
			if (minecraft.IsOldJson)
			{
				_features = minecraft.JsonObject["minecraftArguments"].ToString().Split(' ').ToList();
				return;
			}
			foreach (JsonNode item in minecraft.JsonObject["arguments"]["game"].AsArray())
			{
				if (item.GetValueKind() == JsonValueKind.String)
				{
					_features.Add(item.ToString());
				}
				else
				{
					if (item.GetValueKind() != JsonValueKind.Object)
					{
						continue;
					}
					JsonNode jsonNode = item["value"];
					if (jsonNode.GetValueKind() == JsonValueKind.Array)
					{
						_features.AddRange(from x in jsonNode.AsArray()
							select x.ToString());
					}
					else if (jsonNode.GetValueKind() == JsonValueKind.String)
					{
						_features.Add(jsonNode.ToString());
					}
				}
			}
		}

		public object HasArguments(string key)
		{
			return _features.Contains(key);
		}
	}

	public const string mesaLoaderWindowsVersion = "26.0.4";

	public static bool isLaunching;

	public static McLaunchOptions currentLaunchOptions;

	public static ModLoader.LoaderTask<McLaunchOptions, object> mcLaunchLoader = new ModLoader.LoaderTask<McLaunchOptions, object>("Loader Launch", McLaunchStart)
	{
		OnStateChanged = (ModLoader.LoaderBase a) =>
		{
			ModLaunch.McLaunchState((dynamic)a);
		}
	};

	public static ModLoader.LoaderCombo<object> mcLaunchLoaderReal;

	public static Process mcLaunchProcess;

	public static ModWatcher.Watcher mcLaunchWatcher;

	private static string abortHint;

	public static ModLoader.LoaderTask<McLoginData, McLoginResult> mcLoginLoader = new ModLoader.LoaderTask<McLoginData, McLoginResult>(Lang.Text("Minecraft.Launch.Stage.Login"), McLoginStart, McLoginInput, ThreadPriority.BelowNormal)
	{
		reloadTimeout = 1,
		ProgressWeight = 15.0,
		block = false
	};

	public static ModLoader.LoaderTask<McLoginMs, McLoginResult> mcLoginMsLoader = new ModLoader.LoaderTask<McLoginMs, McLoginResult>("Loader Login Ms", McLoginMsStart)
	{
		reloadTimeout = 1
	};

	public static ModLoader.LoaderTask<McLoginLegacy, McLoginResult> mcLoginLegacyLoader = new ModLoader.LoaderTask<McLoginLegacy, McLoginResult>("Loader Login Legacy", McLoginLegacyStart);

	public static ModLoader.LoaderTask<McLoginServer, McLoginResult> mcLoginAuthLoader = new ModLoader.LoaderTask<McLoginServer, McLoginResult>("Loader Login Auth", McLoginServerStartNew)
	{
		reloadTimeout = 600000
	};

	public static JavaEntry mcLaunchJavaSelected;

	private static string mcLaunchArgument;

	private static readonly object extractJavaWrapperLock = new object();

	private static readonly object extractLinkDLock = new object();

	private static void McLaunchPrecheck()
	{
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Invalid comparison between Unknown and I4
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Invalid comparison between Unknown and I4
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Invalid comparison between Unknown and I4
		if (Config.Debug.AddRandomDelay)
		{
			Thread.Sleep(RandomUtils.NextInt(100, 2000));
		}
		if (ModInstanceList.McMcInstanceSelected.PathIndie.Contains("!") || ModInstanceList.McMcInstanceSelected.PathIndie.Contains(";"))
		{
			throw new Exception(Lang.Text("Minecraft.Launch.Precheck.InvalidPathChars", new object[1] { ModInstanceList.McMcInstanceSelected.PathIndie }));
		}
		if (ModInstanceList.McMcInstanceSelected.PathInstance.Contains("!") || ModInstanceList.McMcInstanceSelected.PathInstance.Contains(";"))
		{
			throw new Exception(Lang.Text("Minecraft.Launch.Precheck.InvalidPathChars", new object[1] { ModInstanceList.McMcInstanceSelected.PathInstance }));
		}
		if (ModBase.IsUtf8CodePage() && !States.Hint.NonAsciiGamePath && !ModInstanceList.McMcInstanceSelected.PathInstance.IsASCII())
		{
			switch (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Precheck.NonAsciiPath.Message", new object[1] { ModInstanceList.McMcInstanceSelected.Name }), Lang.Text("Minecraft.Launch.Precheck.NonAsciiPath.Title"), Lang.Text("Minecraft.Launch.Precheck.NonAsciiPath.Continue"), Lang.Text("Minecraft.Launch.Precheck.NonAsciiPath.Back"), Lang.Text("Common.Hint.DoNotShowAgain")))
			{
			case 2:
				throw new Exception("$$");
			case 3:
				States.Hint.NonAsciiGamePath = true;
				break;
			}
		}
		if ((object)ModInstanceList.McMcInstanceSelected == null)
		{
			throw new Exception(Lang.Text("Minecraft.Launch.Precheck.NoInstance"));
		}
		ModInstanceList.McMcInstanceSelected.Load();
		if (ModInstanceList.McMcInstanceSelected.state == McInstanceState.Error)
		{
			throw new Exception(Lang.Text("Minecraft.Launch.Precheck.InstanceError", new object[1] { ModInstanceList.McMcInstanceSelected.Desc }));
		}
		string checkResult = "";
		ModBase.RunInUiWait(() => checkResult = ProfileUi.IsProfileValid());
		McProfile current = ProfileService.Current;
		if (current == null)
		{
			checkResult = Lang.Text("Minecraft.Launch.Precheck.NoProfile");
		}
		else if (ModInstanceList.McMcInstanceSelected.Info.HasLabyMod || ((ParameterizedProperty<object, int>)(object)Config.InstanceAuth.LoginRequirementSolution)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance] == 1)
		{
			if ((int)((SafeProfile)current).ProfileType != 0)
			{
				checkResult = Lang.Text("Minecraft.Launch.Precheck.RequireMicrosoft");
			}
		}
		else if (((ParameterizedProperty<object, int>)(object)Config.InstanceAuth.LoginRequirementSolution)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance] == 2)
		{
			ProfileType profileType = ((SafeProfile)current).ProfileType;
			if ((int)profileType - 2 > 1)
			{
				checkResult = Lang.Text("Minecraft.Launch.Precheck.RequireThirdParty");
			}
			else if (current.Server?.BeforeLast("/authserver") != ((ParameterizedProperty<object, string>)(object)Config.InstanceAuth.AuthServerAddress)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance])
			{
				checkResult = Lang.Text("Minecraft.Launch.Precheck.AuthServerMismatch");
			}
		}
		else if (((ParameterizedProperty<object, int>)(object)Config.InstanceAuth.LoginRequirementSolution)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance] == 3)
		{
			if ((int)((SafeProfile)current).ProfileType == 1)
			{
				checkResult = Lang.Text("Minecraft.Launch.Precheck.RequireMicrosoftOrThirdParty");
			}
			else
			{
				ProfileType profileType = ((SafeProfile)current).ProfileType;
				bool flag = (int)profileType - 2 <= 1;
				if (flag && current.Server?.BeforeLast("/authserver") != ((ParameterizedProperty<object, string>)(object)Config.InstanceAuth.AuthServerAddress)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance])
				{
					checkResult = Lang.Text("Minecraft.Launch.Precheck.AuthServerMismatch");
				}
			}
		}
		if (!string.IsNullOrEmpty(checkResult))
		{
			throw new ArgumentException(checkResult);
		}
		if (ProfileService.HasMicrosoftProfile)
		{
			return;
		}
		if (Lang.IsFeaturesUnrestricted)
		{
			if (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.PurchaseHint.Message"), Lang.Text("Minecraft.Launch.PurchaseHint.Title"), Lang.Text("Minecraft.Launch.PurchaseHint.Purchase"), Lang.Text("Minecraft.Launch.PurchaseHint.Later")) == 1)
			{
				ModBase.OpenWebsite("https://www.xbox.com/zh-cn/games/store/minecraft-java-bedrock-edition-for-pc/9nxp44l49shj");
			}
			return;
		}
		switch (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.AccountVerification.Message"), Lang.Text("Minecraft.Launch.AccountVerification.Title"), Lang.Text("Minecraft.Launch.AccountVerification.Purchase"), Lang.Text("Minecraft.Launch.AccountVerification.Demo"), Lang.Text("Minecraft.Launch.AccountVerification.Back"), isWarn: false, highLight: true, forceWait: false, () =>
		{
			ModBase.OpenWebsite("https://www.xbox.com/zh-cn/games/store/minecraft-java-bedrock-edition-for-pc/9nxp44l49shj");
		}))
		{
		case 2:
			HintService.Hint(Lang.Text("Minecraft.Launch.DemoMode"), HintType.Error);
			currentLaunchOptions.ExtraArgs.Add("--demo");
			break;
		case 3:
			throw new Exception("$$");
		}
	}

	public static bool McLaunchStart(McLaunchOptions options = null)
	{
		isLaunching = true;
		currentLaunchOptions = options ?? new McLaunchOptions();
		if (!ModBase.RunInUi())
		{
			throw new Exception("McLaunchStart 必须在 UI 线程调用！");
		}
		if (mcLaunchLoader.State == ModBase.LoadState.Loading)
		{
			HintService.Hint(Lang.Text("Minecraft.Launch.Error.AlreadyLaunching"), HintType.Error);
			isLaunching = false;
			return false;
		}
		if ((object)currentLaunchOptions.instance != null && ModInstanceList.McMcInstanceSelected != currentLaunchOptions.instance)
		{
			McLaunchLog("在启动前切换到实例 " + currentLaunchOptions.instance.Name);
			currentLaunchOptions.instance.Load();
			if (currentLaunchOptions.instance.state == McInstanceState.Error)
			{
				HintService.Hint(Lang.Text("Minecraft.Launch.Error.CannotLaunch", new object[1] { currentLaunchOptions.instance.Desc }), HintType.Error);
				isLaunching = false;
				return false;
			}
			ModInstanceList.McMcInstanceSelected = currentLaunchOptions.instance;
			States.Game.SelectedInstance = ModInstanceList.McMcInstanceSelected.Name;
			ModMain.frmLaunchLeft.RefreshButtonsUI();
			ModMain.frmLaunchLeft.RefreshPage(anim: false);
		}
		ModMain.frmMain.AprilGiveup();
		ModMain.frmMain.pageStack = ModMain.frmMain.pageStack.Where((FormMain.PageStackData p) => p.page != FormMain.PageType.InstanceSelect).ToList();
		mcLaunchLoader.Start(options, isForceRestart: true);
		return true;
	}

	public static void McLaunchLog(string text)
	{
		text = McLogFilter.FilterUserName(McLogFilter.FilterAccessToken(text, '*'), '*');
		ModBase.RunInUi(() =>
		{
			TextBlock labLog = ModMain.frmLaunchRight.LabLog;
			labLog.Text = labLog.Text + "\r\n[" + TimeUtils.GetTimeNow() + "] " + text;
		});
		ModBase.Log("[Launch] " + text);
	}

	private static void McLaunchState(ModLoader.LoaderTask<McLaunchOptions, object> loader)
	{
		switch (mcLaunchLoader.State)
		{
		case ModBase.LoadState.Waiting:
		case ModBase.LoadState.Finished:
		case ModBase.LoadState.Failed:
		case ModBase.LoadState.Aborted:
			ModMain.frmLaunchLeft.PageChangeToLogin();
			break;
		case ModBase.LoadState.Loading:
			ModMain.frmLaunchRight.LabLog.Text = "";
			break;
		}
	}

	private static void McLaunchStart(ModLoader.LoaderTask<McLaunchOptions, object> loader)
	{
		ModBase.RunInUiWait(ModMain.frmLaunchLeft.PageChangeToLaunching);
		try
		{
			McLaunchPrecheck();
			McLaunchLog("预检测已通过");
		}
		catch (Exception ex)
		{
			if (!ex.Message.StartsWithF("$$"))
			{
				HintService.Hint(Lang.Text("Minecraft.Launch.Precheck.Failed.WithDetail", new object[1] { ex.Message }), HintType.Error);
			}
			throw;
		}
		try
		{
			bool isRestricted = RestrictedFeaturePolicy.IsRestricted;
			if (isRestricted && !ModDownload.McClientFilesAvailableLocally(ModInstanceList.McMcInstanceSelected))
			{
				RestrictedFeaturePolicy.Check("minecraft-launch-missing-files");
				throw new ModBase.CancelledException();
			}
			List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>
			{
				new ModLoader.LoaderTask<int, int>(Lang.Text("Minecraft.Launch.Stage.GetJava"), McLaunchJava)
				{
					ProgressWeight = 4.0,
					block = false
				},
				mcLoginLoader,
				new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Launch.Stage.CompleteFiles"), isRestricted ? new List<ModLoader.LoaderBase>() : ModDownload.DlClientFix(ModInstanceList.McMcInstanceSelected, checkAssetsHash: false, ModDownload.AssetsIndexExistsBehaviour.DownloadInBackground))
				{
					ProgressWeight = 15.0,
					show = false
				},
				new ModLoader.LoaderTask<string, List<ModLibrary.McLibToken>>(Lang.Text("Minecraft.Launch.Stage.GetArguments"), McLaunchArgumentMain)
				{
					ProgressWeight = 2.0
				},
				new ModLoader.LoaderTask<List<ModLibrary.McLibToken>, int>(Lang.Text("Minecraft.Launch.Stage.ExtractNatives"), McLaunchNatives)
				{
					ProgressWeight = 2.0
				},
				new ModLoader.LoaderTask<int, int>(Lang.Text("Minecraft.Launch.Stage.PreLaunch"), (ModLoader.LoaderTask<int, int> _) =>
				{
					McLaunchPrerun();
				})
				{
					ProgressWeight = 1.0
				},
				new ModLoader.LoaderTask<int, int>(Lang.Text("Minecraft.Launch.Stage.CustomCommand"), McLaunchCustom)
				{
					ProgressWeight = 1.0
				},
				new ModLoader.LoaderTask<int, Process>(Lang.Text("Minecraft.Launch.Stage.StartProcess"), McLaunchRun)
				{
					ProgressWeight = 2.0
				},
				new ModLoader.LoaderTask<Process, int>(Lang.Text("Minecraft.Launch.Stage.WaitWindow"), McLaunchWait)
				{
					ProgressWeight = 1.0
				},
				new ModLoader.LoaderTask<int, int>(Lang.Text("Minecraft.Launch.Stage.End"), (ModLoader.LoaderTask<int, int> _) =>
				{
					McLaunchEnd();
				})
				{
					ProgressWeight = 1.0
				}
			};
			ModLoader.LoaderCombo<object> loaderCombo = new ModLoader.LoaderCombo<object>(Lang.Text("Minecraft.Launch.Stage.Root"), loaders)
			{
				show = false
			};
			if (mcLoginLoader.State == ModBase.LoadState.Finished)
			{
				mcLoginLoader.State = ModBase.LoadState.Waiting;
			}
			mcLaunchLoaderReal = loaderCombo;
			abortHint = null;
			loaderCombo.Start();
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			while (loaderCombo.State == ModBase.LoadState.Loading)
			{
				((DispatcherObject)ModMain.frmLaunchLeft).Dispatcher.Invoke((Action)ModMain.frmLaunchLeft.LaunchingRefresh);
				Thread.Sleep(100);
			}
			((DispatcherObject)ModMain.frmLaunchLeft).Dispatcher.Invoke((Action)ModMain.frmLaunchLeft.LaunchingRefresh);
			switch (loaderCombo.State)
			{
			case ModBase.LoadState.Finished:
				HintService.Hint(Lang.Text("Minecraft.Launch.Success", new object[1] { ModInstanceList.McMcInstanceSelected.Name }), HintType.Success);
				break;
			case ModBase.LoadState.Aborted:
				if (abortHint == null)
				{
					HintService.Hint((currentLaunchOptions?.SaveBatch == null) ? Lang.Text("Minecraft.Launch.Cancelled") : Lang.Text("Minecraft.Launch.ExportScript.Cancelled"));
				}
				else
				{
					HintService.Hint(abortHint, HintType.Success);
				}
				break;
			case ModBase.LoadState.Failed:
				throw loaderCombo.Error;
			default:
				throw new Exception(Lang.Text("Minecraft.Launch.Error.InvalidState", new object[1] { ModBase.GetStringFromEnum(loaderCombo.State) }));
			}
			isLaunching = false;
		}
		catch (Exception ex2)
		{
			for (Exception ex3 = ex2; ex3 != null; ex3 = ex3.InnerException)
			{
				if (ex3.Message.StartsWithF("$"))
				{
					if (ex3.Message != "$$")
					{
						ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Error.SpecialMessage.WithDetail", new object[1] { ex3.Message.TrimStart('$') }), (currentLaunchOptions?.SaveBatch == null) ? Lang.Text("Launch.Error.Title") : Lang.Text("Launch.Error.ExportScriptTitle"));
					}
					throw;
				}
				if (ex3.InnerException == null)
				{
					break;
				}
			}
			McLaunchLog("错误：" + ex2);
			ModBase.Log(ex2, (currentLaunchOptions?.SaveBatch == null) ? "Minecraft launch failed" : "Export script failed", ModBase.LogLevel.Msgbox, (currentLaunchOptions?.SaveBatch == null) ? Lang.Text("Launch.Error.Title") : Lang.Text("Launch.Error.ExportScriptTitle"), (currentLaunchOptions?.SaveBatch == null) ? Lang.Text("Minecraft.Launch.Error.LaunchFailed") : Lang.Text("Minecraft.Launch.Error.ExportScriptFailed"));
			throw;
		}
	}

	public static McLoginData McLoginInput()
	{
		McLoginData result = null;
		try
		{
			result = ProfileUi.GetLoginData();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Minecraft.Launch.Login.Error.Input"), ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Login.Error.Input"));
		}
		return result;
	}

	private static void McLoginStart(ModLoader.LoaderTask<McLoginData, McLoginResult> data)
	{
		ModBase.Log("[Profile] 开始加载选定档案");
		string text = ProfileUi.IsProfileValid();
		if (!string.IsNullOrEmpty(text))
		{
			throw new ArgumentException(text);
		}
		ModLoader.LoaderBase loaderBase = null;
		switch (data.input.LoginType)
		{
		case McLoginType.Ms:
			loaderBase = mcLoginMsLoader;
			break;
		case McLoginType.Legacy:
			loaderBase = mcLoginLegacyLoader;
			break;
		case McLoginType.Auth:
			loaderBase = mcLoginAuthLoader;
			break;
		}
		loaderBase.WaitForExit(data.input, mcLoginLoader, data.isForceRestarting);
		data.output = (McLoginResult)((dynamic)loaderBase).output;
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: false);
		});
		ModBase.Log("[Profile] 选定档案加载完成");
	}

	private static void McLoginMsStart(ModLoader.LoaderTask<McLoginMs, McLoginResult> data)
	{
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected Obj, but got Unknown
		McProfile current = ProfileService.Current;
		McProfile val = ((current != null && (int)((SafeProfile)current).ProfileType == 0) ? ProfileService.Current : null);
		LogWrapper.Info("Profile", "验证方式：正版（" + ((val == null) ? "尚未登录" : ((SafeProfile)val).UserName) + "）");
		data.Progress = 0.05;
		McProfile result;
		IdentityModelAuthenticationException ex = default;
		try
		{
			result = ProfileService.AuthenticateAsync((ProfileType)0, new AuthenticationRequest
			{
				ForceRefresh = data.isForceRestarting,
				DeviceCodeHandler = ProfileUi.ShowDeviceCodeLoginAsync,
				RefreshFailureHandler = _ConfirmMicrosoftRefreshFailureAsync
			}, val, true, data.AbortedToken).GetAwaiter().GetResult();
		}
		catch (Exception obj) when (obj is IdentityModelAuthenticationException)
		{
			ex = (IdentityModelAuthenticationException)obj;
			if (ProfileUi.HandleMicrosoftXstsError(ex) || ProfileUi.HandleMicrosoftNotOwnedError(ex) || ProfileUi.HandleMicrosoftCreateProfileError(ex))
			{
				throw new IdentityModelException("$$", ex);
			}
			throw;
		}
		ThrowIfAborted(data);
		ProfileService.IsCreatingProfile = false;
		data.output = new McLoginResult
		{
			AccessToken = ((SafeProfile)result).AccessToken,
			Name = ((SafeProfile)result).UserName,
			Uuid = ((SafeProfile)result).Uuid,
			Type = "Microsoft",
			ClientToken = result.ClientToken,
			ProfileJson = result.RawJson
		};
		data.Progress = 0.98;
		LogWrapper.Info("Profile", "正版验证完成");
	}

	private static void ThrowIfAborted(ModLoader.LoaderTask<McLoginMs, McLoginResult> data)
	{
		if (data.IsAborted)
		{
			throw new ThreadInterruptedException();
		}
	}

	private static Task<bool> _ConfirmMicrosoftRefreshFailureAsync(Exception exception, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		LogWrapper.Info("Profile", "获取正版 OAuth Token 失败：" + exception);
		bool reuseCachedProfile = false;
		ModBase.RunInUiWait(() =>
		{
			if (isLaunching)
			{
				reuseCachedProfile = ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.RefreshAccountFailed.Message"), Lang.Text("Minecraft.Launch.Login.RefreshAccountFailed.Title"), Lang.Text("Minecraft.Launch.Login.Continue"), Lang.Text("Common.Action.Cancel")) == 1;
			}
		});
		return Task.FromResult(reuseCachedProfile);
	}

	private static void McLoginServerStartNew(ModLoader.LoaderTask<McLoginServer, McLoginResult> data)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Invalid comparison between Unknown and I4
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Expected Obj, but got Unknown
		McLoginServer input = data.input;
		LogWrapper.Info("Profile", "验证方式：" + input.Description);
		data.Progress = 0.1;
		McProfile val = ((input.IsExist && !ProfileService.IsCreatingProfile) ? ProfileService.Current : null);
		McProfile result = ProfileService.AuthenticateAsync(input.ProviderType, new AuthenticationRequest
		{
			Server = input.BaseUrl,
			DiscoveryAddress = input.DiscoveryAddress,
			Username = input.UserName,
			Password = input.Password,
			ForceRefresh = data.isForceRestarting,
			ForceReselectProfile = input.ForceReselectProfile,
			DeviceCodeHandler = (((int)input.ProviderType == 3) ? new Func<DeviceCodeAuthenticationContext, CancellationToken, Task<AuthorizeResult>>(ProfileUi.ShowDeviceCodeLoginAsync) : null),
			ProfileSelector = (IReadOnlyList<AuthenticationCandidate> candidates, CancellationToken _) => Task.FromResult<AuthenticationCandidate>(_SelectAuthProfile(candidates))
		}, val, true, data.AbortedToken).GetAwaiter().GetResult();
		if (data.IsAborted)
		{
			throw new ThreadInterruptedException();
		}
		data.output = new McLoginResult
		{
			AccessToken = ((SafeProfile)result).AccessToken,
			ClientToken = result.ClientToken,
			Uuid = ((SafeProfile)result).Uuid,
			Name = ((SafeProfile)result).UserName,
			Type = "Auth"
		};
		ProfileService.IsCreatingProfile = false;
		data.Progress = 0.98;
		LogWrapper.Info("Profile", "第三方验证完成");
	}

	private static AuthenticationCandidate? _SelectAuthProfile(IReadOnlyList<AuthenticationCandidate> candidates)
	{
		if (candidates.Count == 0)
		{
			return null;
		}
		if (candidates.Count == 1)
		{
			return candidates[0];
		}
		AuthenticationCandidate selected = null;
		ModBase.RunInUiWait(() =>
		{
			int? num = ModMain.MyMsgBoxSelect(((IEnumerable<AuthenticationCandidate>)candidates).Select((Func<AuthenticationCandidate, IMyRadio>)((AuthenticationCandidate item) => new MyRadioBox
			{
				Text = item.Name
			})).ToList(), Lang.Text("Minecraft.Launch.Login.Auth.SelectProfile"));
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault >= 0 && valueOrDefault < 100000)
				{
					selected = candidates[num.Value];
				}
			}
		});
		return selected;
	}

	private static void McLoginLegacyStart(ModLoader.LoaderTask<McLoginLegacy, McLoginResult> data)
	{
		McLoginLegacy input = data.input;
		LogWrapper.Info("Profile", $"验证方式：离线（{input.UserName}, {input.Uuid}）");
		data.Progress = 0.1;
		ref McLoginResult output = ref data.output;
		output.Name = input.UserName;
		output.Uuid = input.Uuid;
		output.Type = "Legacy";
		data.output.AccessToken = data.output.Uuid;
		data.output.ClientToken = data.output.Uuid;
	}

	private static void McLaunchJava(ModLoader.LoaderTask<int, int> task)
	{
		Version minVer = new Version(0, 0, 0, 0);
		Version maxVer = new Version(999, 999, 999, 999);
		if ((!ModInstanceList.McMcInstanceSelected.Info.Valid && ModInstanceList.McMcInstanceSelected.releaseTime >= new DateTime(2024, 4, 2)) || (ModInstanceList.McMcInstanceSelected.Info.Valid && ModInstanceList.McMcInstanceSelected.Info.vanilla >= new Version(20, 0, 5)))
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] MC 1.20.5+ (24w14a+) 要求至少 Java 21");
			}
			minVer = new Version(21, 0, 0, 0);
		}
		else if ((!ModInstanceList.McMcInstanceSelected.Info.Valid && ModInstanceList.McMcInstanceSelected.releaseTime >= new DateTime(2021, 11, 16)) || (ModInstanceList.McMcInstanceSelected.Info.Valid && ModInstanceList.McMcInstanceSelected.Info.vanilla.Major >= 18))
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] MC 1.18 pre2+ 要求至少 Java 17");
			}
			minVer = new Version(17, 0, 0, 0);
		}
		else if ((!ModInstanceList.McMcInstanceSelected.Info.Valid && ModInstanceList.McMcInstanceSelected.releaseTime >= new DateTime(2021, 5, 11)) || (ModInstanceList.McMcInstanceSelected.Info.Valid && ModInstanceList.McMcInstanceSelected.Info.vanilla.Major >= 17))
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] MC 1.17+ (21w19a+) 要求至少 Java 16");
			}
			minVer = new Version(16, 0, 0, 0);
		}
		else if (ModInstanceList.McMcInstanceSelected.releaseTime.Year >= 2017)
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] MC 1.12+ 要求至少 Java 8");
			}
			minVer = new Version(1, 8, 0, 0);
		}
		else if (ModInstanceList.McMcInstanceSelected.releaseTime <= new DateTime(2013, 5, 1) && ModInstanceList.McMcInstanceSelected.releaseTime.Year >= 2001)
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] MC 1.5.2- 要求最高 Java 12");
			}
			maxVer = new Version(1, 8, 999, 999);
		}
		string text = null;
		JsonObject jsonObject = ModInstanceList.McMcInstanceSelected.JsonObject;
		int? num;
		if (jsonObject == null)
		{
			num = null;
		}
		else
		{
			JsonNode? jsonNode = jsonObject["javaVersion"];
			if (jsonNode == null)
			{
				num = null;
			}
			else
			{
				JsonNode? jsonNode2 = jsonNode["majorVersion"];
				num = ((jsonNode2 != null) ? new int?(JsonCompat.ToObject<int>(jsonNode2)) : ((int?)null));
			}
		}
		int? num2 = num;
		int valueOrDefault;
		if (!num2.HasValue)
		{
			JsonObject jsonVersion = ModInstanceList.McMcInstanceSelected.JsonVersion;
			int? num3;
			if (jsonVersion == null)
			{
				num3 = null;
			}
			else
			{
				JsonNode? jsonNode3 = jsonVersion["java_version"];
				num3 = ((jsonNode3 != null) ? new int?(JsonCompat.ToObject<int>(jsonNode3)) : ((int?)null));
			}
			int? num4 = num3;
			valueOrDefault = num4.GetValueOrDefault();
		}
		else
		{
			valueOrDefault = num2.GetValueOrDefault();
		}
		int num5 = valueOrDefault;
		if (num5 >= 22)
		{
			McLaunchLog("Mojang 要求至少使用 Java " + num5);
			minVer = new Version(num5, 0, 0, 0);
			text = ModInstanceList.McMcInstanceSelected.JsonObject?["javaVersion"]?["component"]?.ToString() ?? ModInstanceList.McMcInstanceSelected.JsonVersion?["java_component"]?.ToString();
			if (string.IsNullOrEmpty(text))
			{
				text = null;
			}
		}
		if (ModInstanceList.McMcInstanceSelected.Info.HasOptiFine && ModInstanceList.McMcInstanceSelected.Info.Valid)
		{
			if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major < 7)
			{
				maxVer = new Version(1, 8, 999, 999);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major >= 8 && ModInstanceList.McMcInstanceSelected.Info.vanilla.Major < 12)
			{
				minVer = new Version(1, 8, 0, 0);
				maxVer = new Version(1, 8, 999, 999);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major == 12)
			{
				maxVer = new Version(1, 8, 999, 999);
			}
		}
		if (ModInstanceList.McMcInstanceSelected.Info.HasForge)
		{
			if (ModInstanceList.McMcInstanceSelected.Info.vanilla >= new Version(6, 0, 1) && ModInstanceList.McMcInstanceSelected.Info.vanilla <= new Version(7, 0, 2))
			{
				minVer = ((new Version(1, 7, 0, 0) > minVer) ? new Version(1, 7, 0, 0) : minVer);
				maxVer = ((new Version(1, 7, 999, 999) < maxVer) ? new Version(1, 7, 999, 999) : maxVer);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major <= 12 || !ModInstanceList.McMcInstanceSelected.Info.Valid)
			{
				maxVer = new Version(1, 8, 999, 999);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major <= 14)
			{
				minVer = ((new Version(1, 8, 0, 0) > minVer) ? new Version(1, 8, 0, 0) : minVer);
				maxVer = ((new Version(1, 10, 999, 999) < maxVer) ? new Version(1, 10, 999, 999) : maxVer);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major == 15)
			{
				minVer = ((new Version(1, 8, 0, 0) > minVer) ? new Version(1, 8, 0, 0) : minVer);
				maxVer = ((new Version(1, 15, 999, 999) < maxVer) ? new Version(1, 15, 999, 999) : maxVer);
			}
			else if (McVersionComparer.CompareVersionGe(ModInstanceList.McMcInstanceSelected.Info.Forge, "34.0.0") && McVersionComparer.CompareVersionGe("36.2.25", ModInstanceList.McMcInstanceSelected.Info.Forge))
			{
				maxVer = ((new Version(1, 8, 0, 320) < maxVer) ? new Version(1, 8, 0, 321) : maxVer);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major >= 18 && ModInstanceList.McMcInstanceSelected.Info.vanilla.Major < 19 && ModInstanceList.McMcInstanceSelected.Info.HasOptiFine)
			{
				maxVer = ((new Version(1, 18, 999, 999) < maxVer) ? new Version(1, 18, 999, 999) : maxVer);
			}
		}
		if (ModInstanceList.McMcInstanceSelected.Info.HasCleanroom)
		{
			if (!Version.TryParse(ModInstanceList.McMcInstanceSelected.Info.Cleanroom.Split('-')[0], out Version result))
			{
				throw new FormatException("无法解析 Cleanroom 版本号：" + ModInstanceList.McMcInstanceSelected.Info.Cleanroom);
			}
			if (result < new Version(0, 5, 0, 0))
			{
				if (ModBase.modeDebug)
				{
					ModBase.Log("[Launch] [Debug] Cleanroom 版本低于 0.5，要求至少 Java 21");
				}
				minVer = ((new Version(21, 0, 0, 0) > minVer) ? new Version(21, 0, 0, 0) : minVer);
			}
			else
			{
				if (ModBase.modeDebug)
				{
					ModBase.Log("[Launch] [Debug] Cleanroom 版本高于 0.5，要求至少 Java 25");
				}
				minVer = ((new Version(25, 0, 0, 0) > minVer) ? new Version(25, 0, 0, 0) : minVer);
			}
		}
		if (ModInstanceList.McMcInstanceSelected.Info.HasFabric && ModInstanceList.McMcInstanceSelected.Info.Valid)
		{
			if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major >= 15 && ModInstanceList.McMcInstanceSelected.Info.vanilla.Major <= 16)
			{
				minVer = ((new Version(1, 8, 0, 0) > minVer) ? new Version(1, 8, 0, 0) : minVer);
			}
			else if (ModInstanceList.McMcInstanceSelected.Info.vanilla.Major >= 18)
			{
				minVer = ((new Version(1, 17, 0, 0) > minVer) ? new Version(1, 17, 0, 0) : minVer);
			}
		}
		if (ModInstanceList.McMcInstanceSelected.Info.HasLiteLoader && ModInstanceList.McMcInstanceSelected.Info.Valid)
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] LiteLoader 要求最高 Java 8");
			}
			maxVer = ((new Version(8, 999, 999, 999) < maxVer) ? new Version(8, 999, 999, 999) : maxVer);
		}
		if (ModInstanceList.McMcInstanceSelected.Info.HasLabyMod)
		{
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] LabyMod 要求至少 Java 21");
			}
			minVer = ((new Version(21, 0, 0, 0) > minVer) ? new Version(21, 0, 0, 0) : minVer);
			maxVer = new Version(999, 999, 999, 999);
		}
		if (ModInstanceList.McMcInstanceSelected.JsonObject["javaVersion"] != null)
		{
			double num6 = ModBase.Val(ModInstanceList.McMcInstanceSelected.JsonObject["javaVersion"]["majorVersion"]?.ToString());
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Launch] [Debug] JSON 中参数要求至少 Java " + num6);
			}
			if (num6 <= 8.0)
			{
				minVer = ((new Version(1, (int)Math.Round(num6), 0, 0) > minVer) ? new Version(1, (int)Math.Round(num6), 0, 0) : minVer);
			}
			else
			{
				minVer = ((new Version((int)Math.Round(num6), 0, 0, 0) > minVer) ? new Version((int)Math.Round(num6), 0, 0, 0) : minVer);
			}
			if (maxVer < minVer)
			{
				maxVer = new Version(999, 999, 999, 999);
			}
		}
		IReadOnlySet<int> readOnlySet = null;
		if (ModInstanceList.McMcInstanceSelected.JsonObject["compatibleJavaMajors"] is JsonArray { Count: >0 } jsonArray)
		{
			List<int> list = (from major in (from major in jsonArray
					select (int)Math.Round(ModBase.Val(major?.ToString())) into major
					where major > 0
					select major).Distinct()
				orderby major
				select major).ToList();
			if (list.Count > 0)
			{
				Version version = ToCompatibleVersion(list.Min(), isMax: false);
				Version version2 = ToCompatibleVersion(list.Max(), isMax: true);
				if (ModBase.modeDebug)
				{
					ModBase.Log("[Launch] [Debug] JSON 中要求的兼容 Java 主版本：" + string.Join(", ", list));
				}
				Version version3 = ((version > minVer) ? version : minVer);
				Version version4 = ((version2 < maxVer) ? version2 : maxVer);
				if (version4 < version3)
				{
					version3 = version;
					version4 = version2;
				}
				minVer = version3;
				maxVer = version4;
				readOnlySet = list.ToHashSet();
			}
		}
		lock (ModJava.javaLock)
		{
			McLaunchLog("Java 版本需求：最低 " + minVer?.ToString() + "，最高 " + maxVer?.ToString() + ((readOnlySet == null) ? "" : ("，兼容主版本：" + string.Join(", ", readOnlySet.OrderBy((int major) => major)))));
			mcLaunchJavaSelected = ModJava.JavaSelect("$$", minVer, maxVer, ModInstanceList.McMcInstanceSelected, enforceVersionRange: false, readOnlySet);
			if (task.IsAborted)
			{
				return;
			}
			if (mcLaunchJavaSelected != null)
			{
				McLaunchLog("选择的 Java：" + (object)mcLaunchJavaSelected);
			}
			else
			{
				if (task.IsAborted)
				{
					return;
				}
				McLaunchLog("无合适的 Java，需要确认是否自动下载");
				int? num7 = readOnlySet?.Where((int major) =>
				{
					Version version5 = ((major <= 8) ? new Version(1, major, 0, 0) : new Version(major, 0, 0, 0));
					return ((major <= 8) ? new Version(1, major, 999, 999) : new Version(major, 999, 999, 999)) >= minVer && version5 <= maxVer;
				}).OrderBy((int major) => major).FirstOrDefault();
				string text2;
				if (num7 > 0)
				{
					text2 = num7.Value.ToString();
				}
				else if (minVer >= new Version(1, 9))
				{
					text2 = minVer.Major.ToString();
				}
				else
				{
					if (maxVer < new Version(1, 8))
					{
						if (ModInstanceList.McMcInstanceSelected.Info.HasForge)
						{
							ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Java.NeedLegacyJavaFixerOrJava7"), Lang.Text("Minecraft.Launch.Java.NotFound.Title"));
						}
						else
						{
							ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Java.NeedJava7"), Lang.Text("Minecraft.Launch.Java.NotFound.Title"));
						}
						throw new Exception("$$");
					}
					if (minVer > new Version(1, 8, 0, 140) && maxVer < new Version(1, 8, 0, 321))
					{
						ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Java.NeedJava8U141ToU320"), Lang.Text("Minecraft.Launch.Java.NotFound.Title"));
						throw new Exception("$$");
					}
					if (minVer > new Version(1, 8, 0, 140))
					{
						ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Java.NeedJava8U141OrLater"), Lang.Text("Minecraft.Launch.Java.NotFound.Title"));
						throw new Exception("$$");
					}
					text2 = 8.ToString();
				}
				if (!ModJava.JavaDownloadConfirm("Java " + text2))
				{
					throw new Exception("$$");
				}
				ModLoader.LoaderCombo<string> javaDownloadLoader = ModJava.GetJavaDownloadLoader();
				try
				{
					javaDownloadLoader.Start(text ?? text2, isForceRestart: true);
					while (javaDownloadLoader.State == ModBase.LoadState.Loading && !task.IsAborted)
					{
						task.Progress = javaDownloadLoader.Progress;
						Thread.Sleep(10);
					}
				}
				finally
				{
					javaDownloadLoader.Abort();
				}
				mcLaunchJavaSelected = ModJava.JavaSelect("$$", minVer, maxVer, ModInstanceList.McMcInstanceSelected, enforceVersionRange: false, readOnlySet);
				if (!task.IsAborted)
				{
					if (mcLaunchJavaSelected == null)
					{
						HintService.Hint(Lang.Text("Minecraft.Launch.Error.NoJava"), HintType.Error);
						throw new Exception("$$");
					}
					McLaunchLog("选择的 Java：" + (object)mcLaunchJavaSelected);
				}
			}
		}
		static Version ToCompatibleVersion(int major, bool isMax)
		{
			if (major > 8)
			{
				return new Version(major, isMax ? 999 : 0, isMax ? 999 : 0, isMax ? 999 : 0);
			}
			return new Version(1, major, isMax ? 999 : 0, isMax ? 999 : 0);
		}
	}

	internal static void SecretLaunchJvmArgs(ref List<string> dataList)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		string text = ((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		dataList.Insert(0, string.IsNullOrEmpty(text) ? Config.Launch.JvmArgs : text);
		JvmPreferredIpStack preferredIpStack = Config.Launch.PreferredIpStack;
		if ((int)preferredIpStack != 0)
		{
			if ((int)preferredIpStack == 2)
			{
				dataList.Add("-Djava.net.preferIPv6Stack=true");
				dataList.Add("-Djava.net.preferIPv6Addresses=true");
			}
		}
		else
		{
			dataList.Add("-Djava.net.preferIPv4Stack=true");
			dataList.Add("-Djava.net.preferIPv4Addresses=true");
		}
		McLaunchLog("当前剩余内存：" + ((double)KernelInterop.GetAvailablePhysicalMemoryBytes() / 1073741824.0).ToString("N1", CultureInfo.InvariantCulture) + "G");
		double num = PageInstanceSetup.GetRam(ModInstanceList.McMcInstanceSelected) * 1024.0;
		string text2 = Math.Floor(num).ToString(CultureInfo.InvariantCulture);
		dataList.Add("-Xmn" + Math.Floor(num * 0.15).ToString(CultureInfo.InvariantCulture) + "m");
		dataList.Add("-Xmx" + text2 + "m");
		if (!dataList.Any((string d) => d.Contains("-Xms", StringComparison.OrdinalIgnoreCase)))
		{
			if (Config.Launch.LockMemory)
			{
				dataList.Add("-Xms" + text2 + "m");
			}
			else
			{
				double? initialRam = PageInstanceSetup.GetInitialRam(ModInstanceList.McMcInstanceSelected);
				if (initialRam.HasValue)
				{
					dataList.Add("-Xms" + Math.Floor(initialRam.Value * 1024.0).ToString(CultureInfo.InvariantCulture) + "m");
				}
			}
		}
		if (!dataList.Any((string d) => d.Contains("-Dlog4j2.formatMsgNoLookups=true")))
		{
			dataList.Add("-Dlog4j2.formatMsgNoLookups=true");
		}
	}

	public static string ExtractJavaWrapper()
	{
		string text = Path.Combine(ModBase.pathPure, "JavaWrapper.jar");
		ModBase.Log("[Java] 选定的 Java Wrapper 路径：" + text);
		lock (extractJavaWrapperLock)
		{
			try
			{
				WriteJavaWrapper(text);
			}
			catch (Exception ex)
			{
				if (!File.Exists(text))
				{
					throw new FileNotFoundException("释放 Java Wrapper 失败", ex);
				}
				ModBase.Log(ex, "Java Wrapper 文件释放失败，但文件已存在，将在删除后尝试重新生成", ModBase.LogLevel.Developer);
				try
				{
					File.Delete(text);
					WriteJavaWrapper(text);
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, "Java Wrapper 文件重新释放失败，将尝试更换文件名重新生成", ModBase.LogLevel.Developer);
					text = Path.Combine(ModBase.pathPure, "JavaWrapper2.jar");
					try
					{
						WriteJavaWrapper(text);
						goto end_IL_0060;
					}
					catch (Exception innerException)
					{
						throw new FileNotFoundException("释放 Java Wrapper 最终尝试失败", innerException);
					}
					end_IL_0060:;
				}
			}
		}
		return text;
	}

	private static void WriteJavaWrapper(string path)
	{
		ModBase.WriteFile(path, ModBase.GetResourceStream("Resources/java-wrapper.jar"));
	}

	public static string ExtractLinkD()
	{
		string text = Path.Combine(ModBase.pathPure, "linkd.exe");
		lock (extractLinkDLock)
		{
			try
			{
				WriteLinkD(text);
			}
			catch (Exception ex)
			{
				if (!File.Exists(text))
				{
					throw new FileNotFoundException("释放 linkd 失败", ex);
				}
				ModBase.Log(ex, "linkd 文件释放失败，但文件已存在，将在删除后尝试重新生成", ModBase.LogLevel.Developer);
				try
				{
					File.Delete(text);
					WriteLinkD(text);
				}
				catch (Exception innerException)
				{
					throw new FileNotFoundException("释放 linkd 失败", innerException);
				}
			}
		}
		return text;
	}

	private static void WriteLinkD(string path)
	{
		ModBase.WriteFile(path, ModBase.GetResourceStream("Resources/linkd.exe"));
	}

	private static bool McLaunchNeedsLegacyFix(McInstance mc)
	{
		if (Config.Launch.DisableLF || ((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableLF)[(object)mc.PathInstance])
		{
			ModBase.Log("[Launch] LegacyFix 已被禁用");
			return false;
		}
		if (mc.releaseTime < new DateTime(2013, 6, 25) && mc.releaseTime.Year > 2000)
		{
			return true;
		}
		return false;
	}

	private static string McLaunchGetLwjglVersion(McInstance mc)
	{
		foreach (ModLibrary.McLibToken item in ModLibrary.McLibListGet(mc, includeInstanceJar: false))
		{
			if (!string.IsNullOrWhiteSpace(item.OriginalName))
			{
				string[] array = item.OriginalName.Split(':');
				if (array.Length >= 3 && array[0].Equals("org.lwjgl", StringComparison.OrdinalIgnoreCase) && array[1].Equals("lwjgl", StringComparison.OrdinalIgnoreCase))
				{
					return array[2];
				}
			}
		}
		return null;
	}

	private static bool McLaunchUsesLwjglUnsafeAgent(McInstance mc)
	{
		if (McLaunchGetLwjglVersion(mc) == "3.4.1")
		{
			bool disableLwjglUnsafeAgent = Config.Launch.DisableLwjglUnsafeAgent;
			bool flag = ((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableLwjglUnsafeAgent)[(object)mc.PathInstance];
			if (!disableLwjglUnsafeAgent)
			{
				return !flag;
			}
			return false;
		}
		return false;
	}

	private static void McLaunchArgumentMain(ModLoader.LoaderTask<string, List<ModLibrary.McLibToken>> loader)
	{
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		McLaunchLog("开始获取 Minecraft 启动参数");
		string text;
		if (ModInstanceList.McMcInstanceSelected.JsonObject["arguments"] != null && ModInstanceList.McMcInstanceSelected.JsonObject["arguments"]["jvm"] != null)
		{
			McLaunchLog("获取新版 JVM 参数");
			text = McLaunchArgumentsJvmNew(ModInstanceList.McMcInstanceSelected);
			McLaunchLog("新版 JVM 参数获取成功：");
			McLaunchLog(text);
		}
		else
		{
			McLaunchLog("获取旧版 JVM 参数");
			text = McLaunchArgumentsJvmOld(ModInstanceList.McMcInstanceSelected);
			McLaunchLog("旧版 JVM 参数获取成功：");
			McLaunchLog(text);
		}
		if (!string.IsNullOrEmpty((string?)ModInstanceList.McMcInstanceSelected.JsonObject["minecraftArguments"]))
		{
			McLaunchLog("获取旧版 Game 参数");
			text = text + " " + McLaunchArgumentsGameOld(ModInstanceList.McMcInstanceSelected);
			McLaunchLog("旧版 Game 参数获取成功");
		}
		if (ModInstanceList.McMcInstanceSelected.JsonObject["arguments"] != null && ModInstanceList.McMcInstanceSelected.JsonObject["arguments"]["game"] != null)
		{
			McLaunchLog("获取新版 Game 参数");
			text = text + " " + McLaunchArgumentsGameNew(ModInstanceList.McMcInstanceSelected);
			McLaunchLog("新版 Game 参数获取成功");
		}
		if (mcLaunchJavaSelected.Installation.MajorVersion > 8)
		{
			if (!text.Contains("-Dstdout.encoding="))
			{
				text = "-Dstdout.encoding=UTF-8 " + text;
			}
			if (!text.Contains("-Dstderr.encoding="))
			{
				text = "-Dstderr.encoding=UTF-8 " + text;
			}
		}
		if (mcLaunchJavaSelected.Installation.MajorVersion >= 18 && !text.Contains("-Dfile.encoding="))
		{
			text = "-Dfile.encoding=COMPAT " + text;
		}
		text = text.Replace(" -Dos.name=Windows 10", " -Dos.name=\"Windows 10\"");
		if ((int)Config.Launch.GameWindowMode == 0)
		{
			text += " --fullscreen";
		}
		foreach (string extraArg in currentLaunchOptions.ExtraArgs)
		{
			text = text + " " + extraArg.Trim();
		}
		string text2 = ((ParameterizedProperty<object, string>)(object)Config.Instance.GameArgs)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		text = text + " " + (string.IsNullOrEmpty(text2) ? Config.Launch.GameArgs : text2);
		Dictionary<string, string> dictionary = McLaunchArgumentsReplace(ModInstanceList.McMcInstanceSelected, ref loader);
		if (string.IsNullOrWhiteSpace(dictionary["${version_type}"]))
		{
			text = text.Replace(" --versionType ${version_type}", "");
			dictionary["${version_type}"] = "\"\"";
		}
		string text3 = "";
		string[] array = text.Split(" ");
		for (int i = 0; i < array.Length; i++)
		{
			string text4 = array[i];
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				text4 = text4.Replace(item.Key, item.Value);
			}
			if ((text4.Contains(" ") || text4.Contains(":\\")) && !text4.EndsWithF("\""))
			{
				text4 = "\"" + text4 + "\"";
			}
			text3 = text3 + text4 + " ";
		}
		text3 = text3.TrimEnd();
		string worldName = currentLaunchOptions.WorldName;
		if (worldName != null)
		{
			text3 = text3 + " --quickPlaySingleplayer \"" + worldName + "\"";
		}
		string text5 = (string.IsNullOrEmpty(currentLaunchOptions.ServerIp) ? ((ParameterizedProperty<object, string>)(object)Config.Instance.ServerToEnter)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance] : currentLaunchOptions.ServerIp);
		if (string.IsNullOrWhiteSpace(worldName) && !string.IsNullOrWhiteSpace(text5))
		{
			if (ModInstanceList.McMcInstanceSelected.releaseTime > new DateTime(2023, 4, 4))
			{
				text3 = text3 + " --quickPlayMultiplayer \"" + text5 + "\"";
			}
			else
			{
				text3 = ((!text5.Contains(":")) ? (text3 + " --server " + text5 + " --port 25565") : (text3 + " --server " + text5.Split(":")[0] + " --port " + text5.Split(":")[1]));
				if (ModInstanceList.McMcInstanceSelected.Info.HasOptiFine)
				{
					HintService.Hint(Lang.Text("Minecraft.Launch.Error.OptiFineAutoJoinWarning"), HintType.Error);
				}
			}
		}
		McLaunchLog("Minecraft 启动参数：");
		McLaunchLog(text3);
		mcLaunchArgument = text3;
	}

	private static string McLaunchArgumentsJvmOld(McInstance instance)
	{
		List<string> list = new List<string>();
		list.Add("-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump");
		string text = ((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		if (string.IsNullOrEmpty(text))
		{
			text = Config.Launch.JvmArgs;
		}
		if (!text.Contains("-Dlog4j2.formatMsgNoLookups=true"))
		{
			text += " -Dlog4j2.formatMsgNoLookups=true";
		}
		text = text.Replace(" -XX:MaxDirectMemorySize=256M", "");
		list.Insert(0, text);
		list.Add("-Xmn" + Math.Floor(PageInstanceSetup.GetRam(ModInstanceList.McMcInstanceSelected, !mcLaunchJavaSelected.Installation.Is64Bit) * 1024.0 * 0.15) + "m");
		double num = Math.Floor(PageInstanceSetup.GetRam(ModInstanceList.McMcInstanceSelected, !mcLaunchJavaSelected.Installation.Is64Bit) * 1024.0);
		list.Add("-Xmx" + num + "m");
		if (!list.Any((string d) => d.Contains("-Xms", StringComparison.OrdinalIgnoreCase)))
		{
			if (Config.Launch.LockMemory)
			{
				list.Add("-Xms" + num + "m");
			}
			else
			{
				double? initialRam = PageInstanceSetup.GetInitialRam(ModInstanceList.McMcInstanceSelected, !mcLaunchJavaSelected.Installation.Is64Bit);
				if (initialRam.HasValue)
				{
					list.Add("-Xms" + Math.Floor(initialRam.Value * 1024.0).ToString(CultureInfo.InvariantCulture) + "m");
				}
			}
		}
		list.Add("\"-Djava.library.path=" + GetNativesFolder() + "\"");
		list.Add("-cp ${classpath}");
		if (mcLoginLoader.output.Type == "Auth")
		{
			if (mcLaunchJavaSelected.Installation.MajorVersion >= 6)
			{
				list.Add("-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT");
			}
			string text2 = mcLoginAuthLoader.input.BaseUrl.Replace("/authserver", "");
			try
			{
				string s = Requester.FetchString(text2);
				list.Insert(0, "-javaagent:\"" + Path.Combine(ModBase.pathPure, "authlib-injector.jar") + "\"=" + text2 + " -Dauthlibinjector.side=client -Dauthlibinjector.yggdrasil.prefetched=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s)));
			}
			catch (WebException ex)
			{
				throw new Exception(Lang.Text("Minecraft.Launch.Error.CannotConnectAuthServerWithDetail", new object[1] { text2 ?? null }) + ex.InnerException, ex);
			}
			catch (Exception innerException)
			{
				throw new Exception(Lang.Text("Minecraft.Launch.Error.CannotConnectAuthServer", new object[1] { text2 ?? null }), innerException);
			}
		}
		if (((ParameterizedProperty<object, bool>)(object)Config.Instance.UseDebugLof4j2Config)[(object)instance.PathIndie])
		{
			if (ModInstanceList.McMcInstanceSelected.releaseTime.Year >= 2017)
			{
				list.Insert(0, "-Dlog4j.configurationFile=\"" + LaunchEnvUtils.ExtractDebugLog4j2Config() + "\"");
			}
			else
			{
				list.Insert(0, "-Dlog4j.configurationFile=\"" + LaunchEnvUtils.ExtractLegacyDebugLog4j2Config() + "\"");
			}
		}
		int num2 = 0;
		int num3 = ((ParameterizedProperty<object, int>)(object)Config.Instance.Renderer)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		num2 = ((num3 == 0) ? Config.Launch.Renderer : (num3 - 1));
		string text3 = Path.Combine(ModBase.pathPure, "mesa-loader-windows", "26.0.4", "Loader.jar");
		if (num2 != 0)
		{
			list.Insert(0, "-javaagent:\"" + text3 + "\"=" + num2 switch
			{
				2 => "d3d12", 
				1 => "llvmpipe", 
				_ => "zink", 
			});
		}
		if (((ParameterizedProperty<object, bool>)(object)Config.Instance.UseProxy)[(object)instance.PathIndie] && Config.Network.HttpProxy.Type.Equals(2) && !string.IsNullOrWhiteSpace(Config.Network.HttpProxy.CustomAddress))
		{
			try
			{
				Uri uri = new Uri(Config.Network.HttpProxy.CustomAddress);
				list.Add("-D" + (uri.Scheme.StartsWithF("https:") ? "https" : "http") + ".proxyHost=" + uri.AbsoluteUri);
				list.Add($"-D{(uri.Scheme.StartsWithF("https:") ? "https" : "http")}.proxyPort={uri.Port}");
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, Lang.Text("Minecraft.Launch.Error.Proxy"), ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.Proxy"));
			}
		}
		if (McLaunchNeedsLegacyFix(instance))
		{
			string text4 = Path.Combine(ModBase.pathPure, "legacyfix.jar");
			list.Add("-javaagent:\"" + text4 + "\"");
			if (instance.releaseTime < new DateTime(2011, 5, 25))
			{
				list.Add("-Djava.util.Arrays.useLegacyMergeSort=true");
			}
		}
		if (ModBase.IsUtf8CodePage() && !Config.Launch.DisableJlw && !((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableJlw)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance])
		{
			if (mcLaunchJavaSelected.Installation.MajorVersion >= 9)
			{
				list.Add("--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED");
			}
			list.Add("-Doolloo.jlw.tmpdir=\"" + ModBase.pathPure.TrimEnd('\\') + "\"");
			list.Add("-jar \"" + ExtractJavaWrapper() + "\"");
		}
		if (instance.JsonObject["mainClass"] == null)
		{
			throw new Exception(Lang.Text("Minecraft.Launch.Error.MissingMainClass"));
		}
		list.Add((string?)instance.JsonObject["mainClass"]);
		return list.Join(" ");
	}

	private static string McLaunchArgumentsJvmNew(McInstance instance)
	{
		List<string> dataList = new List<string>();
		McInstance mcInstance = instance;
		while (true)
		{
			if (mcInstance.JsonObject["arguments"] != null && mcInstance.JsonObject["arguments"]["jvm"] != null)
			{
				foreach (JsonNode item in mcInstance.JsonObject["arguments"]["jvm"].AsArray())
				{
					if (item.GetValueKind() == JsonValueKind.String)
					{
						dataList.Add(item.ToString());
					}
					else
					{
						if (!ModLibrary.McJsonRuleCheck(item["rules"]))
						{
							continue;
						}
						if (item["value"].GetValueKind() == JsonValueKind.String)
						{
							dataList.Add(item["value"].ToString());
							continue;
						}
						foreach (JsonNode item2 in item["value"].AsArray())
						{
							dataList.Add(item2.ToString());
						}
					}
				}
			}
			if (string.IsNullOrEmpty(mcInstance.InheritInstanceName))
			{
				break;
			}
			mcInstance = new McInstance(mcInstance.InheritInstanceName);
		}
		SecretLaunchJvmArgs(ref dataList);
		if (mcLoginLoader.output.Type == "Auth")
		{
			if (mcLaunchJavaSelected.Installation.MajorVersion >= 6)
			{
				dataList.Add("-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT");
			}
			string text = mcLoginAuthLoader.input.BaseUrl.Replace("/authserver", "");
			try
			{
				string s = ModNet.NetGetCodeByRequestRetry(text, Encoding.UTF8)?.ToString();
				dataList.Insert(0, "-javaagent:\"" + Path.Combine(ModBase.pathPure, "authlib-injector.jar") + "\"=" + text + " -Dauthlibinjector.side=client -Dauthlibinjector.yggdrasil.prefetched=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s)));
			}
			catch (Exception innerException)
			{
				throw new Exception(Lang.Text("Minecraft.Launch.Error.CannotConnectAuthServer", new object[1] { text ?? null }), innerException);
			}
		}
		if (McLaunchUsesLwjglUnsafeAgent(ModInstanceList.McMcInstanceSelected))
		{
			ModBase.Log("获取到的 LWJGL 版本：" + McLaunchGetLwjglVersion(ModInstanceList.McMcInstanceSelected));
			dataList.Insert(0, "-javaagent:\"" + ModBase.pathPure + "lwjgl-unsafe-agent.jar\"");
		}
		if (((ParameterizedProperty<object, bool>)(object)Config.Instance.UseDebugLof4j2Config)[(object)instance.PathIndie])
		{
			if (ModInstanceList.McMcInstanceSelected.releaseTime.Year >= 2017)
			{
				dataList.Insert(0, "-Dlog4j.configurationFile=\"" + LaunchEnvUtils.ExtractDebugLog4j2Config() + "\"");
			}
			else
			{
				dataList.Insert(0, "-Dlog4j.configurationFile=\"" + LaunchEnvUtils.ExtractLegacyDebugLog4j2Config() + "\"");
			}
		}
		int num = 0;
		int num2 = ((ParameterizedProperty<object, int>)(object)Config.Instance.Renderer)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		num = ((num2 == 0) ? Config.Launch.Renderer : (num2 - 1));
		string text2 = Path.Combine(ModBase.pathPure, "mesa-loader-windows", "26.0.4", "Loader.jar");
		if (num != 0)
		{
			dataList.Insert(0, "-javaagent:\"" + text2 + "\"=" + num switch
			{
				2 => "d3d12", 
				1 => "llvmpipe", 
				_ => "zink", 
			});
		}
		if (((ParameterizedProperty<object, bool>)(object)Config.Instance.UseProxy)[(object)instance.PathIndie] && Config.Network.HttpProxy.Type.Equals(2) && !string.IsNullOrWhiteSpace(Config.Network.HttpProxy.CustomAddress))
		{
			try
			{
				Uri uri = new Uri(Config.Network.HttpProxy.CustomAddress);
				dataList.Add("-D" + (uri.Scheme.StartsWithF("https:") ? "https" : "http") + ".proxyHost=" + uri.AbsoluteUri);
				dataList.Add($"-D{(uri.Scheme.StartsWithF("https:") ? "https" : "http")}.proxyPort={uri.Port}");
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Minecraft.Launch.Error.Proxy"), ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.Proxy"));
			}
		}
		if (ModBase.IsUtf8CodePage() && !Config.Launch.DisableJlw && !((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableJlw)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance])
		{
			if (mcLaunchJavaSelected.Installation.MajorVersion >= 9)
			{
				dataList.Add("--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED");
			}
			dataList.Add("-Doolloo.jlw.tmpdir=\"" + ModBase.pathPure.TrimEnd('\\') + "\"");
			dataList.Add("-jar \"" + ExtractJavaWrapper() + "\"");
		}
		List<string> list = new List<string>();
		int i = 0;
		for (int num3 = dataList.Count - 1; i <= num3; i++)
		{
			string text3 = dataList[i];
			if (dataList[i].StartsWithF("-"))
			{
				while (i < dataList.Count - 1 && !dataList[i + 1].StartsWithF("-"))
				{
					i++;
					text3 = text3 + " " + dataList[i];
				}
			}
			list.Add(text3.Trim().Replace("McEmu= ", "McEmu="));
		}
		list.Remove("-XX:MaxDirectMemorySize=256M");
		string text4 = list.Distinct().ToList().Join(" ");
		if (instance.JsonObject["mainClass"] == null)
		{
			throw new Exception(Lang.Text("Minecraft.Launch.Error.MissingMainClass"));
		}
		return text4 + " " + instance.JsonObject["mainClass"];
	}

	private static string McLaunchArgumentsGameOld(McInstance version)
	{
		List<string> list = new List<string>();
		string text = version.JsonObject["minecraftArguments"].ToString();
		if (!text.Contains("--height") && version.releaseTime >= new DateTime(2013, 6, 17))
		{
			text += " --height ${resolution_height} --width ${resolution_width}";
		}
		list.Add(text);
		string text2 = list.Join(" ");
		if ((version.Info.HasForge || version.Info.HasLiteLoader) && version.Info.HasOptiFine)
		{
			if (text2.Contains("--tweakClass optifine.OptiFineForgeTweaker"))
			{
				ModBase.Log("[Launch] 发现正确的 OptiFineForge TweakClass，目前参数：" + text2);
				text2 = text2.Replace(" --tweakClass optifine.OptiFineForgeTweaker", "").Replace("--tweakClass optifine.OptiFineForgeTweaker ", "") + " --tweakClass optifine.OptiFineForgeTweaker";
			}
			if (text2.Contains("--tweakClass optifine.OptiFineTweaker"))
			{
				ModBase.Log("[Launch] 发现错误的 OptiFineForge TweakClass，目前参数：" + text2);
				text2 = text2.Replace(" --tweakClass optifine.OptiFineTweaker", "").Replace("--tweakClass optifine.OptiFineTweaker ", "") + " --tweakClass optifine.OptiFineForgeTweaker";
				try
				{
					ModBase.WriteFile(Path.Combine(version.PathInstance, version.Name + ".json"), ModBase.ReadFile(Path.Combine(version.PathInstance, version.Name + ".json")).Replace("optifine.OptiFineTweaker", "optifine.OptiFineForgeTweaker"));
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "替换 OptiFineForge TweakClass 失败");
				}
			}
		}
		return text2;
	}

	private static string McLaunchArgumentsGameNew(McInstance instance)
	{
		string text = null;
		List<string> list = new List<string>();
		McInstance mcInstance = instance;
		while (true)
		{
			if (mcInstance.JsonObject["arguments"] != null && mcInstance.JsonObject["arguments"]["game"] != null)
			{
				foreach (JsonNode item in mcInstance.JsonObject["arguments"]["game"].AsArray())
				{
					if (item.GetValueKind() == JsonValueKind.String)
					{
						list.Add(item.ToString());
					}
					else
					{
						if (!ModLibrary.McJsonRuleCheck(item["rules"]))
						{
							continue;
						}
						if (item["value"].GetValueKind() == JsonValueKind.String)
						{
							list.Add(item["value"].ToString());
							continue;
						}
						foreach (JsonNode item2 in item["value"].AsArray())
						{
							list.Add(item2.ToString());
						}
					}
				}
			}
			if (string.IsNullOrEmpty(mcInstance.InheritInstanceName))
			{
				break;
			}
			mcInstance = new McInstance(mcInstance.InheritInstanceName);
		}
		List<string> list2 = new List<string>();
		int i = 0;
		for (int num = list.Count - 1; i <= num; i++)
		{
			string text2 = list[i];
			if (list[i].StartsWithF("-"))
			{
				while (i < list.Count - 1 && !list[i + 1].StartsWithF("-"))
				{
					i++;
					text2 = text2 + " " + list[i];
				}
			}
			list2.Add(text2);
		}
		text = list2.Distinct().ToList().Join(" ");
		if ((instance.Info.HasForge || instance.Info.HasLiteLoader) && instance.Info.HasOptiFine)
		{
			if (text.Contains("--tweakClass optifine.OptiFineForgeTweaker"))
			{
				ModBase.Log("[Launch] 发现正确的 OptiFineForge TweakClass，目前参数：" + text);
				text = text.Replace(" --tweakClass optifine.OptiFineForgeTweaker", "").Replace("--tweakClass optifine.OptiFineForgeTweaker ", "") + " --tweakClass optifine.OptiFineForgeTweaker";
			}
			if (text.Contains("--tweakClass optifine.OptiFineTweaker"))
			{
				ModBase.Log("[Launch] 发现错误的 OptiFineForge TweakClass，目前参数：" + text);
				text = text.Replace(" --tweakClass optifine.OptiFineTweaker", "").Replace("--tweakClass optifine.OptiFineTweaker ", "") + " --tweakClass optifine.OptiFineForgeTweaker";
				try
				{
					ModBase.WriteFile(Path.Combine(instance.PathInstance, instance.Name + ".json"), ModBase.ReadFile(Path.Combine(instance.PathInstance, instance.Name + ".json")).Replace("optifine.OptiFineTweaker", "optifine.OptiFineForgeTweaker"));
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "替换 OptiFineForge TweakClass 失败");
				}
			}
		}
		return text;
	}

	private static Dictionary<string, string> McLaunchArgumentsReplace(McInstance instance, ref ModLoader.LoaderTask<string, List<ModLibrary.McLibToken>> loader)
	{
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Invalid comparison between Unknown and I4
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Invalid comparison between Unknown and I4
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("${classpath_separator}", ";");
		dictionary.Add("${natives_directory}", ModBase.ShortenPath(GetNativesFolder()));
		dictionary.Add("${library_directory}", ModBase.ShortenPath(ModFolder.mcFolderSelected + "libraries"));
		dictionary.Add("${libraries_directory}", ModBase.ShortenPath(ModFolder.mcFolderSelected + "libraries"));
		dictionary.Add("${launcher_name}", "PCLCE");
		dictionary.Add("${launcher_version}", ModBase.versionCode.ToString());
		dictionary.Add("${version_name}", instance.Name);
		string text = ((ParameterizedProperty<object, string>)(object)Config.Instance.TypeInfo)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		dictionary.Add("${version_type}", string.IsNullOrEmpty(text) ? Config.Launch.TypeInfo : text);
		string pathIndie = ModInstanceList.McMcInstanceSelected.PathIndie;
		dictionary.Add("${game_directory}", ModBase.ShortenPath(pathIndie.Substring(0, pathIndie.Length - 1)));
		dictionary.Add("${assets_root}", ModBase.ShortenPath(ModFolder.mcFolderSelected + "assets"));
		dictionary.Add("${user_properties}", "{}");
		dictionary.Add("${auth_player_name}", mcLoginLoader.output.Name);
		dictionary.Add("${auth_uuid}", mcLoginLoader.output.Uuid);
		dictionary.Add("${auth_access_token}", mcLoginLoader.output.AccessToken);
		dictionary.Add("${access_token}", mcLoginLoader.output.AccessToken);
		dictionary.Add("${auth_session}", mcLoginLoader.output.AccessToken);
		dictionary.Add("${user_type}", "msa");
		GameWindowSizeMode gameWindowMode = Config.Launch.GameWindowMode;
		Size val;
		if ((int)gameWindowMode != 2)
		{
			val = (((int)gameWindowMode != 3) ? new Size(854.0, 480.0) : new Size(Math.Max(100.0, Config.Launch.GameWindowWidth), Math.Max(100.0, Config.Launch.GameWindowHeight)));
		}
		else
		{
			Size result = default;
			ModBase.RunInUiWait(() =>
			{
				//IL_0029: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0030: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				return result = new Size(ModBase.GetPixelSize(ModMain.frmMain.PanForm.ActualWidth), ModBase.GetPixelSize(ModMain.frmMain.PanForm.ActualHeight));
			});
			val = result;
			val.Height -= 29.5 * (double)ModBase.dpi / 96.0;
		}
		if (ModInstanceList.McMcInstanceSelected.Info.Drop <= 120 && mcLaunchJavaSelected.Installation.MajorVersion <= 8 && mcLaunchJavaSelected.Installation.Version.Revision >= 200 && mcLaunchJavaSelected.Installation.Version.Revision <= 321 && !ModInstanceList.McMcInstanceSelected.Info.HasOptiFine && !ModInstanceList.McMcInstanceSelected.Info.HasForge)
		{
			McLaunchLog($"已应用窗口大小过大修复（{mcLaunchJavaSelected.Installation.Version.Revision}）");
			val.Width /= (double)ModBase.dpi / 96.0;
			val.Height /= (double)ModBase.dpi / 96.0;
		}
		dictionary.Add("${resolution_width}", Math.Round(val.Width).ToString(CultureInfo.InvariantCulture));
		dictionary.Add("${resolution_height}", Math.Round(val.Height).ToString(CultureInfo.InvariantCulture));
		dictionary.Add("${game_assets}", ModBase.ShortenPath(ModFolder.mcFolderSelected + "assets\\virtual\\legacy"));
		dictionary.Add("${assets_index_name}", ModAssets.McAssetsGetIndexName(instance));
		List<ModLibrary.McLibToken> list = ModLibrary.McLibListGet(instance, includeInstanceJar: true);
		loader.output = list;
		List<string> list2 = new List<string>();
		string text2 = null;
		if (McLaunchNeedsLegacyFix(instance))
		{
			string filePath = Path.Combine(ModBase.pathPure, "legacyfix.jar");
			try
			{
				ModBase.WriteFile(filePath, ModBase.GetResourceStream("Resources/legacyfix.jar"));
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "LegacyFix 释放失败");
			}
		}
		if (McLaunchUsesLwjglUnsafeAgent(instance))
		{
			string text3 = Path.Combine(ModBase.pathPure, "lwjgl-unsafe-agent.jar");
			try
			{
				ModBase.WriteFile(text3, ModBase.GetResourceStream("Resources/lwjgl-unsafe-agent.jar"));
				list2.Add(text3);
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "LWJGL Unsafe Agent 释放失败");
			}
		}
		foreach (ModLibrary.McLibToken item in list)
		{
			if (!item.IsNatives && (!ModInstanceList.McMcInstanceSelected.Info.HasCleanroom || item.OriginalName == null || (!item.OriginalName.Contains("org.lwjgl.lwjgl:lwjgl:2.9.4") && !item.OriginalName.Contains("net.java.dev.jna:platform:3.4.0") && !item.OriginalName.Contains("com.ibm.icu:icu4j-core-mojang:51.2"))))
			{
				if (item.Name != null && item.Name == "optifine:OptiFine")
				{
					text2 = item.LocalPath;
				}
				else
				{
					list2.Add(item.LocalPath);
				}
			}
		}
		string[] array = ((ParameterizedProperty<object, string>)(object)Config.Instance.ClasspathHead)[(object)instance.PathInstance].Split(";");
		foreach (string text4 in array)
		{
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list2.Insert(0, text4);
			}
		}
		if (text2 != null)
		{
			list2.Insert(list2.Count - 2, text2);
		}
		dictionary.Add("${classpath}", list2.Select((string c) => ModBase.ShortenPath(c)).Join(";"));
		return dictionary;
	}

	private static void McLaunchNatives(ModLoader.LoaderTask<List<ModLibrary.McLibToken>, int> loader)
	{
		string text = GetNativesFolder() + "\\";
		Directory.CreateDirectory(text);
		McLaunchLog("正在解压 Natives 文件");
		List<string> list = new List<string>();
		foreach (ModLibrary.McLibToken item in loader.input)
		{
			if (!item.IsNatives)
			{
				continue;
			}
			ZipArchive zipArchive;
			try
			{
				zipArchive = new ZipArchive(new FileStream(item.LocalPath, FileMode.Open));
			}
			catch (InvalidDataException ex)
			{
				ModBase.Log(ex, "打开 Natives 文件失败（" + item.LocalPath + "）");
				File.Delete(item.LocalPath);
				throw new Exception(Lang.Text("Minecraft.Launch.Error.NativesCorrupted", new object[1] { item.LocalPath }));
			}
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				string fullName = entry.FullName;
				if (!fullName.EndsWithF(".dll", ignoreCase: true))
				{
					continue;
				}
				string text2 = text + fullName;
				list.Add(text2);
				FileInfo fileInfo = new FileInfo(text2);
				if (fileInfo.Exists)
				{
					if (fileInfo.Length == entry.Length)
					{
						if (ModBase.modeDebug)
						{
							McLaunchLog("无需解压：" + text2);
						}
						continue;
					}
					try
					{
						File.Delete(text2);
					}
					catch (UnauthorizedAccessException ex2)
					{
						McLaunchLog("删除原 dll 访问被拒绝，这通常代表有一个 MC 正在运行，跳过解压：" + text2);
						McLaunchLog("实际的错误信息：" + ex2);
						break;
					}
				}
				ModBase.WriteFile(text2, entry.Open());
				McLaunchLog("已解压：" + text2);
			}
			zipArchive?.Dispose();
		}
		string[] files = Directory.GetFiles(text);
		foreach (string text3 in files)
		{
			if (!list.Contains(text3))
			{
				try
				{
					McLaunchLog("删除：" + text3);
					File.Delete(text3);
				}
				catch (UnauthorizedAccessException ex3)
				{
					McLaunchLog("删除多余文件访问被拒绝，跳过删除步骤");
					McLaunchLog("实际的错误信息：" + ex3);
					break;
				}
			}
		}
	}

	private static string GetNativesFolder()
	{
		string text = Path.Combine(ModInstanceList.McMcInstanceSelected.PathInstance, ModInstanceList.McMcInstanceSelected.Name + "-natives");
		if (SystemInfo.IsGBKEncoding || text.IsASCII())
		{
			return text;
		}
		text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "bin", "natives");
		if (text.IsASCII())
		{
			return text;
		}
		return Path.Combine(SystemPaths.DriveLetter, "ProgramData", "PCL", "natives");
	}

	private static void McLaunchPrerun()
	{
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Invalid comparison between Unknown and I4
		string text = mcLaunchJavaSelected.Installation.JavawExePath ?? mcLaunchJavaSelected.Installation.JavaExePath;
		try
		{
			ModMain.SetGPUPreference(text, Config.Launch.SetGpuPreference);
		}
		catch (Exception ex)
		{
			if (ProcessInterop.IsAdmin() || !Config.Launch.SetGpuPreference)
			{
				ModBase.Log(ex, "直接调整显卡设置失败");
			}
			else
			{
				ModBase.Log(ex, "直接调整显卡设置失败，将以管理员权限重启 PCL 再次尝试");
				try
				{
					if (ProcessInterop.StartAsAdmin("--gpu \"" + text + "\"", (string)null).ExitCode != 5)
					{
						throw new Exception("调整过程中出现异常");
					}
					McLaunchLog("以管理员权限重启 PCL 并调整显卡设置成功");
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, Lang.Text("Minecraft.Launch.Error.GpuSet"), ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.GpuSet"));
				}
			}
		}
		try
		{
			if (!(mcLoginLoader.output.Type != "Microsoft"))
			{
				ModFolder.McFolderLauncherProfilesJsonCreate(ModFolder.mcFolderSelected);
				JsonObject jsonObject = (JsonObject)ModBase.GetJson("\n            {\n              \"authenticationDatabase\": {\n                \"00000111112222233333444445555566\": {\n                  \"username\": \"" + mcLoginLoader.output.Name.Replace("\"", "-") + "\",\n                  \"profiles\": {\n                    \"66666555554444433333222221111100\": {\n                        \"displayName\": \"" + mcLoginLoader.output.Name + "\"\n                    }\n                  }\n                }\n              },\n              \"clientToken\": \"" + mcLoginLoader.output.ClientToken + "\",\n              \"selectedUser\": {\n                \"account\": \"00000111112222233333444445555566\", \n                \"profile\": \"66666555554444433333222221111100\"\n              }\n            }");
				JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(ModFolder.mcFolderSelected + "launcher_profiles.json"));
				JsonCompat.Merge(jsonObject2, (JsonNode)jsonObject);
				ModBase.WriteFile(ModFolder.mcFolderSelected + "launcher_profiles.json", jsonObject2.ToString(), append: false, Encoding.GetEncoding("GB18030"));
				McLaunchLog("已更新 launcher_profiles.json");
			}
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, "更新 launcher_profiles.json 失败，将在删除文件后重试");
			try
			{
				File.Delete(ModFolder.mcFolderSelected + "launcher_profiles.json");
				ModFolder.McFolderLauncherProfilesJsonCreate(ModFolder.mcFolderSelected);
				JsonObject jsonObject3 = (JsonObject)ModBase.GetJson("\n                    {\n                      \"authenticationDatabase\": {\n                        \"00000111112222233333444445555566\": {\n                          \"username\": \"" + mcLoginLoader.output.Name.Replace("\"", "-") + "\",\n                          \"profiles\": {\n                            \"66666555554444433333222221111100\": {\n                                \"displayName\": \"" + mcLoginLoader.output.Name + "\"\n                            }\n                          }\n                        }\n                      },\n                      \"clientToken\": \"" + mcLoginLoader.output.ClientToken + "\",\n                      \"selectedUser\": {\n                        \"account\": \"00000111112222233333444445555566\", \n                        \"profile\": \"66666555554444433333222221111100\"\n                      }\n                    }");
				JsonObject jsonObject4 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(ModFolder.mcFolderSelected + "launcher_profiles.json"));
				JsonCompat.Merge(jsonObject4, (JsonNode)jsonObject3);
				ModBase.WriteFile(ModFolder.mcFolderSelected + "launcher_profiles.json", jsonObject4.ToString(), append: false, Encoding.GetEncoding("GB18030"));
				McLaunchLog("已在删除后更新 launcher_profiles.json");
			}
			catch (Exception ex4)
			{
				ModBase.Log(ex4, "更新 launcher_profiles.json 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Error.UpdateProfilesFailed"));
			}
		}
		string text2 = Path.Combine(ModInstanceList.McMcInstanceSelected.PathIndie, "options.txt");
		if (Config.Tool.AutoChangeLanguage)
		{
			if (!File.Exists(text2))
			{
				string text3 = Path.Combine(ModInstanceList.McMcInstanceSelected.PathIndie, "config", "yosbr", "options.txt");
				if (File.Exists(text3))
				{
					McLaunchLog("将修改 Yosbr Mod 中的 options.txt");
					text2 = text3;
					ModBase.WriteIni(text2, "lang", "none");
				}
			}
			try
			{
				string text4 = ModBase.ReadIni(text2, "lang", "none");
				bool flag = string.Equals(text4, "none", StringComparison.OrdinalIgnoreCase);
				bool flag2 = Directory.Exists(Path.Combine(ModInstanceList.McMcInstanceSelected.PathIndie, "saves"));
				bool shouldUseLauncherLanguage = flag || !flag2;
				string text5 = _ResolveMinecraftLanguage(text4, shouldUseLauncherLanguage, ModInstanceList.McMcInstanceSelected.releaseTime);
				if (text4 == text5)
				{
					McLaunchLog($"需要的语言为 {text5}，当前语言为 {text4}，无需修改");
				}
				else
				{
					ModBase.WriteIni(text2, "lang", "-");
					ModBase.WriteIni(text2, "lang", text5);
					McLaunchLog("已将语言从 " + text4 + " 修改为 " + text5);
				}
				if ((flag || !flag2) && _ShouldEnableForceUnicodeFont())
				{
					ModBase.WriteIni(text2, "forceUnicodeFont", "true");
					McLaunchLog("已开启 forceUnicodeFont，确保当前启动器语言字体正常显示");
				}
			}
			catch (Exception ex5)
			{
				ModBase.Log(ex5, "更新 options.txt 失败", ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.UpdateOptionsFailed"));
			}
		}
		GameWindowSizeMode gameWindowMode = Config.Launch.GameWindowMode;
		if ((int)gameWindowMode != 0)
		{
			if ((int)gameWindowMode != 1)
			{
				ModBase.WriteIni(text2, "fullscreen", "false");
			}
		}
		else
		{
			ModBase.WriteIni(text2, "fullscreen", "true");
		}
	}

	private static string _ResolveMinecraftLanguage(string? currentLanguage, bool shouldUseLauncherLanguage, DateTime? mcReleaseTime)
	{
		if (_IsMinecraftVersionUnder1Dot1(mcReleaseTime))
		{
			return "none";
		}
		bool useLegacyRegionCase = _ShouldUseLegacyMinecraftLanguageCode(mcReleaseTime);
		return _NormalizeMinecraftLanguageCode(shouldUseLauncherLanguage ? LocalizationService.CurrentLanguage.Code : currentLanguage, useLegacyRegionCase);
	}

	private static string _NormalizeMinecraftLanguageCode(string? languageCode, bool useLegacyRegionCase)
	{
		string text = (string.IsNullOrWhiteSpace(languageCode) ? "none" : languageCode.Replace('-', '_').Trim());
		if (string.Equals(text, "none", StringComparison.OrdinalIgnoreCase))
		{
			return "none";
		}
		string[] array = text.Split('_', 2, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 2)
		{
			return text.ToLowerInvariant();
		}
		string text2 = array[0].ToLowerInvariant();
		string text3 = (useLegacyRegionCase ? array[1].ToUpperInvariant() : array[1].ToLowerInvariant());
		return text2 + "_" + text3;
	}

	private static bool _IsMinecraftVersionUnder1Dot1(DateTime? releaseTime)
	{
		if (releaseTime.HasValue && releaseTime.Value > new DateTime(2000, 1, 1))
		{
			return releaseTime.Value <= new DateTime(2011, 11, 18);
		}
		return false;
	}

	private static bool _ShouldUseLegacyMinecraftLanguageCode(DateTime? releaseTime)
	{
		if (releaseTime.HasValue && releaseTime.Value >= new DateTime(2012, 1, 12))
		{
			return releaseTime.Value <= new DateTime(2016, 6, 8);
		}
		return false;
	}

	private static bool _ShouldEnableForceUnicodeFont()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Invalid comparison between Unknown and I4
		LocalizationFontProfile fontProfile = LocalizationService.CurrentLanguage.FontProfile;
		if ((int)fontProfile - 1 <= 3)
		{
			return true;
		}
		return false;
	}

	private static void McLaunchCustom(ModLoader.LoaderTask<int, int> loader)
	{
		string text = Config.Launch.PreLaunchCommand;
		if (!string.IsNullOrEmpty(text))
		{
			text = ArgumentReplace(text, replaceTime: true);
		}
		string text2 = ((ParameterizedProperty<object, string>)(object)Config.Instance.PreLaunchCommand)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		if (!string.IsNullOrEmpty(text2))
		{
			text2 = ArgumentReplace(text2, replaceTime: true);
		}
		try
		{
			string raw = ((mcLaunchJavaSelected.Installation.MajorVersion > 8) ? "chcp 65001>nul\r\n" : "") + "@echo off\r\ntitle 启动 - " + ModInstanceList.McMcInstanceSelected.Name + "\r\necho 游戏正在启动，请稍候。\r\ncd /D \"" + ModBase.ShortenPath(ModInstanceList.McMcInstanceSelected.PathIndie) + "\"\r\n" + text + "\r\n" + text2 + "\r\n\"" + mcLaunchJavaSelected.Installation.JavaExePath + "\" " + mcLaunchArgument + "\r\necho 游戏已退出。\r\npause";
			ModBase.WriteFile(currentLaunchOptions.SaveBatch ?? (ModBase.exePath + "PCL\\LatestLaunch.bat"), McLogFilter.FilterAccessToken(raw, 'F'), append: false, (mcLaunchJavaSelected.Installation.MajorVersion > 8) ? Encoding.UTF8 : Encoding.Default);
			if (currentLaunchOptions.SaveBatch != null)
			{
				McLaunchLog("导出启动脚本完成，强制结束启动过程");
				abortHint = Lang.Text("Minecraft.Launch.ExportScript.Success");
				ModBase.OpenExplorer(currentLaunchOptions.SaveBatch);
				loader.parent.Abort();
				return;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "输出启动脚本失败");
			if (currentLaunchOptions.SaveBatch != null)
			{
				throw;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			McLaunchLog("正在执行全局自定义命令：" + text);
			Process process = new Process();
			try
			{
				process.StartInfo.FileName = "cmd.exe";
				process.StartInfo.Arguments = "/c \"" + text + "\"";
				process.StartInfo.WorkingDirectory = ModBase.ShortenPath(ModFolder.mcFolderSelected);
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.Start();
				if (Config.Launch.PreLaunchCommandWait)
				{
					while (!process.HasExited && !loader.IsAborted)
					{
						Thread.Sleep(10);
					}
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, Lang.Text("Minecraft.Launch.Error.CustomCommand"), ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.CustomCommand"));
			}
			finally
			{
				if (!process.HasExited && loader.IsAborted)
				{
					McLaunchLog("由于取消启动，已强制结束自定义命令 CMD 进程");
					process.Kill();
				}
			}
		}
		if (string.IsNullOrEmpty(text2))
		{
			return;
		}
		McLaunchLog("正在执行实例自定义命令：" + text2);
		Process process2 = new Process();
		try
		{
			process2.StartInfo.FileName = "cmd.exe";
			process2.StartInfo.Arguments = "/c \"" + text2 + "\"";
			process2.StartInfo.WorkingDirectory = ModBase.ShortenPath(ModFolder.mcFolderSelected);
			process2.StartInfo.UseShellExecute = false;
			process2.StartInfo.CreateNoWindow = true;
			process2.Start();
			if (((ParameterizedProperty<object, bool>)(object)Config.Instance.PreLaunchCommandWait)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance])
			{
				while (!process2.HasExited && !loader.IsAborted)
				{
					Thread.Sleep(10);
				}
			}
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, Lang.Text("Minecraft.Launch.Error.CustomCommand"), ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Launch.Error.CustomCommand"));
		}
		finally
		{
			if (!process2.HasExited && loader.IsAborted)
			{
				McLaunchLog("由于取消启动，已强制结束自定义命令 CMD 进程");
				process2.Kill();
			}
		}
	}

	private static void McLaunchRun(ModLoader.LoaderTask<int, Process> loader)
	{
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Expected I4, but got Unknown
		bool flag = Config.Launch.NoJavaw && mcLaunchJavaSelected.Installation.JavawExePath != null;
		Process process = new Process();
		ProcessStartInfo processStartInfo = new ProcessStartInfo(flag ? mcLaunchJavaSelected.Installation.JavaExePath : mcLaunchJavaSelected.Installation.JavawExePath);
		List<string> list = new List<string>(processStartInfo.EnvironmentVariables["Path"].Split(";"));
		list.Add(ModBase.ShortenPath(mcLaunchJavaSelected.Installation.JavaFolder));
		processStartInfo.EnvironmentVariables["Path"] = list.Distinct().ToList().Join(";");
		processStartInfo.EnvironmentVariables["appdata"] = ModBase.ShortenPath(ModFolder.mcFolderSelected);
		processStartInfo.WorkingDirectory = ModBase.ShortenPath(ModInstanceList.McMcInstanceSelected.PathIndie);
		processStartInfo.UseShellExecute = false;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;
		processStartInfo.CreateNoWindow = flag;
		processStartInfo.Arguments = mcLaunchArgument;
		process.StartInfo = processStartInfo;
		process.Start();
		McLaunchLog("已启动游戏进程：" + processStartInfo.FileName);
		if (loader.IsAborted)
		{
			McLaunchLog("由于取消启动，已强制结束游戏进程");
			process.Kill();
			return;
		}
		loader.output = process;
		mcLaunchProcess = process;
		try
		{
			process.PriorityBoostEnabled = true;
			GameProcessPriority processPriority = Config.Launch.ProcessPriority;
			switch ((int)processPriority)
			{
			case 4:
				process.PriorityClass = ProcessPriorityClass.RealTime;
				break;
			case 3:
				process.PriorityClass = ProcessPriorityClass.High;
				break;
			case 0:
				process.PriorityClass = ProcessPriorityClass.AboveNormal;
				break;
			case 2:
				process.PriorityClass = ProcessPriorityClass.BelowNormal;
				break;
			case 1:
				break;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Minecraft.Launch.Error.PrioritySet"), ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Error.PrioritySet"));
		}
	}

	private static void McLaunchWait(ModLoader.LoaderTask<Process, int> loader)
	{
		McLaunchLog("");
		McLaunchLog("~ 基础参数 ~");
		McLaunchLog("PCL 版本：" + ModBase.versionBaseName + " (" + ModBase.versionCode + ")");
		McLaunchLog($"游戏版本：{ModInstanceList.McMcInstanceSelected.Info.VanillaName}（{ModInstanceList.McMcInstanceSelected.Info.vanilla}，Drop {ModInstanceList.McMcInstanceSelected.Info.Drop}{(ModInstanceList.McMcInstanceSelected.Info.Reliable ? "" : "，无法完全确定")}）");
		McLaunchLog("资源版本：" + ModAssets.McAssetsGetIndexName(ModInstanceList.McMcInstanceSelected));
		McLaunchLog("实例继承：" + (string.IsNullOrEmpty(ModInstanceList.McMcInstanceSelected.InheritInstanceName) ? "无" : ModInstanceList.McMcInstanceSelected.InheritInstanceName));
		double ram = PageInstanceSetup.GetRam(ModInstanceList.McMcInstanceSelected, !mcLaunchJavaSelected.Installation.Is64Bit);
		McLaunchLog("分配的内存：" + ram.ToString("N1", CultureInfo.InvariantCulture) + " GiB（" + Math.Round(ram * 1024.0).ToString("N0", CultureInfo.InvariantCulture) + " MiB）");
		McLaunchLog("MC 文件夹：" + ModFolder.mcFolderSelected);
		McLaunchLog("实例文件夹：" + ModInstanceList.McMcInstanceSelected.PathInstance);
		McLaunchLog("版本隔离：" + ((ModInstanceList.McMcInstanceSelected.PathIndie ?? "") == (ModInstanceList.McMcInstanceSelected.PathInstance ?? "")));
		McLaunchLog("HMCL 格式：" + ModInstanceList.McMcInstanceSelected.IsHmclFormatJson);
		McLaunchLog("Java 信息：" + (object)mcLaunchJavaSelected.Installation);
		McLaunchLog("Natives 文件夹：" + GetNativesFolder());
		McLaunchLog("");
		McLaunchLog("~ 档案参数 ~");
		McLaunchLog("玩家用户名：" + mcLoginLoader.output.Name);
		McLaunchLog("AccessToken：" + mcLoginLoader.output.AccessToken);
		McLaunchLog("ClientToken：" + mcLoginLoader.output.ClientToken);
		McLaunchLog("UUID：" + mcLoginLoader.output.Uuid);
		McLaunchLog("验证方式：" + mcLoginLoader.output.Type);
		McLaunchLog("");
		string text = ((ParameterizedProperty<object, string>)(object)Config.Instance.Title)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance];
		if (string.IsNullOrEmpty(text) && !((ParameterizedProperty<object, bool>)(object)Config.Instance.UseGlobalTitle)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance])
		{
			text = Config.Launch.Title;
		}
		text = ArgumentReplace(text, replaceTime: false);
		string text2 = Path.Combine(mcLaunchJavaSelected.Installation.JavaFolder, "jstack.exe");
		ModWatcher.Watcher watcher = (mcLaunchWatcher = new ModWatcher.Watcher(loader, ModInstanceList.McMcInstanceSelected, text, File.Exists(text2) ? text2 : "", currentLaunchOptions.IsTest));
		if (currentLaunchOptions.IsTest)
		{
			if (ModMain.frmLogLeft == null)
			{
				ModBase.RunInUiWait(() => ModMain.frmLogLeft = new PageLogLeft());
			}
			if (ModMain.frmLogRight == null)
			{
				ModBase.RunInUiWait(() =>
				{
					ModAnimation.AniControlEnabled++;
					ModMain.frmLogRight = new PageLogRight();
					ModAnimation.AniControlEnabled--;
				});
			}
			ModMain.frmLogLeft.Add(watcher);
			McLaunchLog("已显示游戏实时日志");
		}
		while (watcher.State == ModWatcher.Watcher.MinecraftState.Loading)
		{
			Thread.Sleep(100);
		}
		if (watcher.State == ModWatcher.Watcher.MinecraftState.Crashed)
		{
			throw new Exception("$$");
		}
	}

	private static void McLaunchEnd()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected I4, but got Unknown
		McLaunchLog("开始启动结束处理");
		if (Config.Preference.Music.StopInGame)
		{
			ModBase.RunInUi(() =>
			{
				if (ModMusic.MusicPause())
				{
					ModBase.Log("[Music] 已根据设置，在启动后暂停音乐播放");
				}
			});
		}
		else if (Config.Preference.Music.StartInGame)
		{
			ModBase.RunInUi(() =>
			{
				if (ModMusic.MusicResume())
				{
					ModBase.Log("[Music] 已根据设置，在启动后开始音乐播放");
				}
			});
		}
		ModVideoBack.IsGaming = true;
		ModVideoBack.VideoPause();
		McLaunchLog("启动器可见性：" + ((object)Config.Launch.LauncherVisibility/*cast due to constrained. prefix*/).ToString());
		LauncherVisibility launcherVisibility = Config.Launch.LauncherVisibility;
		switch ((int)launcherVisibility)
		{
		case 0:
			McLaunchLog("已根据设置，在启动后关闭启动器");
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.EndProgram(sendWarning: false);
			});
			break;
		case 2:
		case 3:
			McLaunchLog("已根据设置，在启动后隐藏启动器");
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.Hidden = true;
			});
			break;
		case 4:
			McLaunchLog("已根据设置，在启动后最小化启动器");
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.WindowState = WindowState.Minimized;
			});
			break;
		}
		States.SystemStatesGroup system = States.System;
		system.LaunchCount += 1;
		((ParameterizedProperty<object, int>)(object)States.Instance.LaunchCount)[(object)ModInstanceList.McMcInstanceSelected.PathInstance] = ((ParameterizedProperty<object, int>)(object)States.Instance.LaunchCount)[(object)ModInstanceList.McMcInstanceSelected.PathInstance] + 1;
	}

	private static string ArgumentReplace(string text, bool replaceTime, Func<string, string> escapeHandler = null)
	{
		if (text == null)
		{
			return null;
		}
		text = text.Replace("{pcl_version}", replacer(ModBase.versionBaseName));
		text = text.Replace("{pcl_version_code}", replacer(ModBase.versionCode.ToString()));
		text = text.Replace("{pcl_version_branch}", replacer("Publish"));
		text = text.Replace("{identify}", replacer(Identify.LauncherId));
		text = text.Replace("{path}", replacer(Basics.CurrentDirectory));
		text = text.Replace("{path_with_name}", replacer(Basics.ExecutablePath));
		text = text.Replace("{path_temp}", replacer(ModBase.pathTemp));
		if (replaceTime)
		{
			text = text.Replace("{date}", replacer(Lang.Date(DateTime.Now, "d")));
			text = text.Replace("{time}", replacer(Lang.Date(DateTime.Now, "T")));
		}
		string text2 = text;
		JavaEntry val = mcLaunchJavaSelected;
		text = text2.Replace("{java}", replacer((val != null) ? val.Installation.JavaFolder : null));
		text = text.Replace("{minecraft}", replacer(ModFolder.mcFolderSelected));
		McInstance mcMcInstanceSelected = ModInstanceList.McMcInstanceSelected;
		if ((object)mcMcInstanceSelected != null && mcMcInstanceSelected.IsLoaded)
		{
			text = text.Replace("{version_path}", replacer(ModInstanceList.McMcInstanceSelected.PathInstance));
			text = text.Replace("{verpath}", replacer(ModInstanceList.McMcInstanceSelected.PathInstance));
			text = text.Replace("{version_indie}", replacer(ModInstanceList.McMcInstanceSelected.PathIndie));
			text = text.Replace("{verindie}", replacer(ModInstanceList.McMcInstanceSelected.PathIndie));
			text = text.Replace("{name}", replacer(ModInstanceList.McMcInstanceSelected.Name));
			text = ((!new ReadOnlySpan<string>(new string[3] { "unknown", "old", "pending" }).Contains(ModInstanceList.McMcInstanceSelected.Info.VanillaName.ToLower())) ? text.Replace("{version}", replacer(ModInstanceList.McMcInstanceSelected.Info.VanillaName)) : text.Replace("{version}", replacer(ModInstanceList.McMcInstanceSelected.Name)));
		}
		else
		{
			text = text.Replace("{version_path}", replacer(null));
			text = text.Replace("{verpath}", replacer(null));
			text = text.Replace("{version_indie}", replacer(null));
			text = text.Replace("{verindie}", replacer(null));
			text = text.Replace("{name}", replacer(null));
			text = text.Replace("{version}", replacer(null));
		}
		if (mcLoginLoader.State == ModBase.LoadState.Finished)
		{
			text = text.Replace("{user}", replacer(mcLoginLoader.output.Name));
			text = text.Replace("{uuid}", replacer(mcLoginLoader.output.Uuid?.ToLower()));
			switch (mcLoginLoader.input.LoginType)
			{
			case McLoginType.Legacy:
				text = text.Replace("{login}", replacer("离线"));
				break;
			case McLoginType.Ms:
				text = text.Replace("{login}", replacer("正版"));
				break;
			case McLoginType.Auth:
				text = text.Replace("{login}", replacer("Authlib-Injector"));
				break;
			}
		}
		else
		{
			text = text.Replace("{user}", replacer(null));
			text = text.Replace("{uuid}", replacer(null));
			text = text.Replace("{login}", replacer(null));
		}
		return text;
		string replacer(string s)
		{
			if (s == null)
			{
				return "";
			}
			if (escapeHandler == null)
			{
				return s;
			}
			if (s.Contains(":\\"))
			{
				s = ModBase.ShortenPath(s);
			}
			return escapeHandler(s);
		}
	}
}
