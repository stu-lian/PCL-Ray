using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL;

public static class UpdateManager
{
	public static bool isUpdateWaitingRestart;

	public static UpdatesWrapperModel remoteServer = new UpdatesWrapperModel(new List<IUpdateSource>
	{
		new UpdatesGitHubModel("stu-lian", "PCL-Ray")
	});

	public static ModLoader.LoaderCombo<JsonObject> updateLoader;

	public static ModLoader.LoaderTask<int, int> serverLoader = new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Service.PclCe"), (ModLoader.LoaderTask<int, int> _) =>
	{
		LoadOnlineInfo();
	}, null, ThreadPriority.BelowNormal);

	public static bool IsCurrentVersionBeta
	{
		get
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			if (ModBase.versionBaseName.Contains("beta"))
			{
				return true;
			}
			return (int)Config.Update.UpdateChannel == 1;
		}
	}

	public static UpdateEnums.VersionStatus GetVersionStatus()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		try
		{
			return (!remoteServer.IsLatest(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64, SemVer.Parse(ModBase.versionBaseName), ModBase.versionCode)) ? UpdateEnums.VersionStatus.NotLatest : UpdateEnums.VersionStatus.Latest;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Update.Check.Failed"), ModBase.LogLevel.Hint, null, Lang.Text("Update.Check.Failed"));
			return UpdateEnums.VersionStatus.Unknown;
		}
	}

	public static void UpdateStart(UpdateEnums.UpdateType type, string receivedKey = null, bool forceValidated = false)
	{
		string dlTargetPath = ModBase.exePath + "PCL\\Plain Craft Launcher Ray Edition.exe";
		ModBase.RunInNewThread(() =>
		{
			try
			{
				VersionDataModel version = remoteServer.GetLatestVersion(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64);
				ModBase.WriteFile(ModBase.pathTemp + "CEUpdateLog.md", version.Changelog);
				ModBase.Log("[Update] 远程最新版本: " + version.VersionName + ", 当前版本: " + ModBase.versionBaseName);
				if (SemVer.Parse(version.VersionName) > SemVer.Parse(ModBase.versionBaseName))
				{
					if (type == UpdateEnums.UpdateType.PromptOnly)
					{
						ModBase.RunInUi(() =>
						{
							if (ModMain.MyMsgBox(Lang.Text("Update.Available", new object[2]
							{
								ModBase.versionBaseName,
								version.VersionName
							}), Lang.Text("Update.Title"), Lang.Text("Update.Action"), Lang.Text("Common.Action.Cancel")) == 1)
							{
								ModMain.frmMain.PageChange(FormMain.PageType.Setup, FormMain.PageSubType.DownloadCompFavorites);
							}
						});
					}
					else
					{
						List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
						list.AddRange(remoteServer.GetDownloadLoader(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64, dlTargetPath));
						list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Task.Check"), (ModLoader.LoaderTask<int, int> _) =>
						{
							// GitHub Release 未提供 digest 时不校验 SHA，避免空值比对导致必然失败。
							if (!string.IsNullOrWhiteSpace(version.Sha256))
							{
								string fileSHA = ModBase.GetFileSHA256(dlTargetPath);
								if ((fileSHA ?? "") != version.Sha256)
								{
									throw new Exception(Lang.Text("Update.Error.Sha256Mismatch", new object[2] { version.Sha256, fileSHA }));
								}
							}
							else
							{
								ModBase.Log("[Update] 发布资产未提供 SHA256，跳过校验");
							}
						}));
						if (type == UpdateEnums.UpdateType.UpdateNow)
						{
							list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Task.Install"), (ModLoader.LoaderTask<int, int> _) =>
							{
								UpdateRestart(triggerRestartAndByEnd: true);
							}));
						}
						else if (type == UpdateEnums.UpdateType.Silent)
						{
							list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Task.Prepare"), (ModLoader.LoaderTask<int, int> _) =>
							{
								isUpdateWaitingRestart = true;
							}));
						}
						else if (type == UpdateEnums.UpdateType.DownloadAndPrompt)
						{
							list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Task.ShowButton"), (ModLoader.LoaderTask<int, int> _) =>
							{
								isUpdateWaitingRestart = true;
								ModBase.RunInUi(() =>
								{
									ModMain.frmMain.BtnExtraUpdateRestart.ToolTip = Lang.Text("Main.Extra.UpdateRestart.ToolTipWithVersion", new object[2]
									{
										ModBase.versionBaseName,
										version.VersionName
									});
									ModMain.frmMain.BtnExtraUpdateRestart.ShowRefresh();
									ModMain.frmMain.BtnExtraUpdateRestart.Ribble();
								});
							})
							{
								show = false
							});
						}
						list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Task.RefreshSettings"), (ModLoader.LoaderTask<int, int> _) =>
						{
							if (ModMain.frmSetupUpdate != null)
							{
								ModBase.RunInUi(() =>
								{
									ModMain.frmSetupUpdate.BtnUpdate.Text = Lang.Text("Update.Task.RestartInstall");
									ModMain.frmSetupUpdate.BtnUpdate.IsEnabled = true;
								});
							}
						})
						{
							show = false
						});
						updateLoader = new ModLoader.LoaderCombo<JsonObject>(Lang.Text("Update.Title"), list);
						updateLoader.Start();
						if (type == UpdateEnums.UpdateType.UpdateNow)
						{
							ModLoader.LoaderTaskbarAdd(updateLoader);
							ModMain.frmMain.BtnExtraDownload.ShowRefresh();
							ModMain.frmMain.BtnExtraDownload.Ribble();
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[Update] 获取启动器更新失败");
				if (type != UpdateEnums.UpdateType.Silent)
				{
					HintService.Hint(Lang.Text("Update.Error.FetchFailed"), HintType.Error);
				}
			}
		});
	}

	public static void UpdateRestart(bool triggerRestartAndByEnd, bool triggerRestart = true)
	{
		try
		{
			string text = ModBase.exePath + "PCL\\Plain Craft Launcher Ray Edition.exe";
			if (!File.Exists(text))
			{
				ModBase.Log("[System] 更新失败：未找到更新文件");
				return;
			}
			string text2 = $"update {Process.GetCurrentProcess().Id} \"{Basics.ExecutablePath}\" \"{text}\" {(triggerRestart ? "true" : "false")}";
			ModBase.Log("[System] 更新程序启动，参数：" + text2);
			Process.Start(new ProcessStartInfo(text)
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				CreateNoWindow = true,
				Arguments = text2
			});
			if (triggerRestartAndByEnd)
			{
				ModMain.frmMain.EndProgram(sendWarning: false, isUpdating: true);
				ModBase.Log("[System] 已由于更新强制结束程序");
			}
		}
		catch (Win32Exception ex)
		{
			ModBase.Log(ex, "自动更新时触发 Win32 错误，疑似被拦截");
			ModMain.MyMsgBox(Lang.Text("Update.Error.UpdateBlockedMessage", new object[1] { ModBase.exePath }), Lang.Text("Update.Error.UpdateBlocked"), Lang.Text("Common.Action.Confirm"), "", "", isWarn: true);
		}
	}

	internal static void DownloadLatestPCL(ModLoader.LoaderBase loaderToSyncProgress = null)
	{
		string text = Path.Combine(ModBase.pathTemp, "CE-Latest.exe");
		VersionDataModel latestVersion = remoteServer.GetLatestVersion(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64);
		if (latestVersion == null)
		{
			throw new Exception(Lang.Text("Update.Error.UnableToGetUpdate"));
		}
		if (File.Exists(text) && !string.IsNullOrWhiteSpace(latestVersion.Sha256) && (ModBase.GetFileSHA256(text) ?? "") == latestVersion.Sha256)
		{
			ModBase.Log("[System] 最新版 PCL 已存在，跳过下载");
			return;
		}
		if (!string.IsNullOrWhiteSpace(latestVersion.Sha256) && (ModBase.GetFileSHA256(Basics.ExecutablePath) ?? "") == latestVersion.Sha256)
		{
			ModBase.CopyFile(Basics.ExecutablePath, text);
			return;
		}
		List<ModLoader.LoaderBase> downloadLoader = remoteServer.GetDownloadLoader(UpdateChannel.stable, SystemInfo.IsArm64System ? UpdateArch.arm64 : UpdateArch.x64, text);
		ModLoader.LoaderCombo<int> loaderCombo = new ModLoader.LoaderCombo<int>(Lang.Text("Update.Task.DownloadLatestStable"), downloadLoader);
		loaderCombo.Start();
		loaderCombo.WaitForExit();
	}

	private static void LoadOnlineInfo()
	{
		ScheduleBasedOnConfig();
		AnnouncementService.Load();
	}

	private static void ScheduleBasedOnConfig()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected I4, but got Unknown
		LauncherAutoUpdateBehavior updateMode = Config.Update.UpdateMode;
		switch ((int)updateMode)
		{
		case 0:
			ModBase.Log("[Update] 更新设置: 自动下载并安装更新");
			if (GetVersionStatus() != UpdateEnums.VersionStatus.Latest)
			{
				UpdateStart(UpdateEnums.UpdateType.Silent);
			}
			break;
		case 1:
			ModBase.Log("[Update] 更新设置: 自动下载并提示更新");
			UpdateStart(UpdateEnums.UpdateType.DownloadAndPrompt);
			break;
		case 2:
			ModBase.Log("[Update] 更新设置: 提示更新");
			UpdateStart(UpdateEnums.UpdateType.PromptOnly);
			break;
		default:
			ModBase.Log("[Update] 更新设置: 不自动检查更新");
			break;
		}
	}

	public static void ShowCEAnnounce()
	{
		ModMain.MyMsgBox(Lang.Text("Update.CommunityNotice.Body"), Lang.Text("Update.CommunityNotice.Title"), Lang.Text("Update.CommunityNotice.Confirm"));
	}
}
