using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Validate;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public static class ModModpack
{
	public class MMCPackInfo
	{
		public JsonObject additionalJson = new JsonObject();

		public bool isCleanroomOverrided;

		public bool isFabricOverrided;

		public bool isForgeOverrided;

		public bool isMcArgsEdited;

		public bool isMinecraftOverrided;

		public bool isNeoForgeOverrided;

		public JsonArray jvmArgs = new JsonArray();

		public JsonArray libraries = new JsonArray();

		public JsonObject overridedJson = new JsonObject();

		public string tweakers;
	}

	public static void ModpackInstall()
	{
		if (!RestrictedFeaturePolicy.Check("modpack-install"))
		{
			return;
		}
		string file = SystemDialogs.SelectFile(Lang.Text("Minecraft.Download.Modpack.FileDialog.Filter"), Lang.Text("Minecraft.Download.Modpack.FileDialog.Title"), (string)null);
		if (string.IsNullOrEmpty(file))
		{
			return;
		}
		ModBase.RunInThread(() =>
		{
			try
			{
				ModpackInstall(file);
			}
			catch (ModBase.CancelledException)
			{
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "手动安装整合包失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Minecraft.Download.Modpack.Error.OperationFailed"));
			}
		});
	}

	public static ModLoader.LoaderCombo<string> ModpackInstall(string file, string instanceName = null, string logo = null, string resourceId = null, bool isOnlineInstall = false)
	{
		RestrictedFeaturePolicy.ThrowIfRestricted("modpack-install");
		ModBase.Log("[ModPack] 整合包安装请求：" + (file ?? "null"));
		ZipArchive zipArchive = null;
		string archiveBaseFolder = "";
		try
		{
			string text = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\";
			if (text.Contains("!") || text.Contains(";"))
			{
				HintService.Hint(Lang.Text("Minecraft.Download.Modpack.InvalidGamePathChars", new object[1] { text }), HintType.Error);
				throw new ModBase.CancelledException();
			}
			int num = -1;
			try
			{
				zipArchive = new ZipArchive(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
				if (zipArchive.Entries.Any((ZipArchiveEntry e) => e.IsEncrypted))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Modpack.EncryptedArchiveUnsupported"));
				}
				if (zipArchive.GetEntry("mcbbs.packmeta") != null)
				{
					num = 3;
				}
				else if (zipArchive.GetEntry("mmc-pack.json") != null)
				{
					num = 2;
				}
				else if (zipArchive.GetEntry("modrinth.index.json") != null)
				{
					num = 4;
				}
				else if (zipArchive.GetEntry("manifest.json") != null)
				{
					num = ((((JsonObject)ModBase.GetJson(ModBase.ReadFile(zipArchive.GetEntry("manifest.json").Open(), Encoding.UTF8)))["addons"] != null) ? 3 : 0);
				}
				else if (zipArchive.GetEntry("modpack.json") != null)
				{
					num = 1;
				}
				else if (zipArchive.GetEntry("modpack.zip") != null || zipArchive.GetEntry("modpack.mrpack") != null)
				{
					num = 9;
				}
				else
				{
					bool flag = false;
					foreach (ZipArchiveEntry entry in zipArchive.Entries)
					{
						string[] array = entry.FullName.Split("/");
						archiveBaseFolder = array[0] + "/";
						if (array.Count() != 2)
						{
							continue;
						}
						if (array[1] == "mcbbs.packmeta")
						{
							num = 3;
							flag = true;
							break;
						}
						if (array[1] == "mmc-pack.json")
						{
							num = 2;
							flag = true;
							break;
						}
						if (array[1] == "modrinth.index.json")
						{
							num = 4;
							flag = true;
							break;
						}
						if (array[1] == "manifest.json")
						{
							if (((JsonObject)ModBase.GetJson(ModBase.ReadFile(entry.Open(), Encoding.UTF8)))["addons"] == null)
							{
								num = 0;
								flag = true;
							}
							else
							{
								num = 3;
								archiveBaseFolder = "overrides/";
								flag = true;
							}
							break;
						}
						if (array[1] == "modpack.json")
						{
							num = 1;
							flag = true;
							break;
						}
						if (array[1] == "modpack.zip" || array[1] == "modpack.mrpack")
						{
							num = 9;
							flag = true;
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("Error.WinIOError"))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Modpack.OpenFailed"), ex);
				}
				if (file.EndsWithF(".rar", ignoreCase: true))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Modpack.RarUnsupported"), ex);
				}
				throw new Exception(Lang.Text("Minecraft.Download.Modpack.UnsupportedArchive"), ex);
			}
			switch (num)
			{
			case 0:
				ModBase.Log("[ModPack] 整合包种类：CurseForge");
				return InstallPackCurseForge(file, zipArchive, archiveBaseFolder, instanceName, logo, resourceId, isOnlineInstall);
			case 1:
				ModBase.Log("[ModPack] 整合包种类：HMCL");
				return InstallPackHMCL(file, zipArchive, archiveBaseFolder);
			case 2:
				ModBase.Log("[ModPack] 整合包种类：MMC");
				return InstallPackMMC(file, zipArchive, archiveBaseFolder);
			case 3:
				ModBase.Log("[ModPack] 整合包种类：MCBBS");
				return InstallPackMCBBS(file, zipArchive, archiveBaseFolder, instanceName);
			case 4:
				ModBase.Log("[ModPack] 整合包种类：Modrinth");
				return InstallPackModrinth(file, zipArchive, archiveBaseFolder, instanceName, logo, resourceId, isOnlineInstall);
			case 9:
				ModBase.Log("[ModPack] 整合包种类：带启动器的压缩包");
				return InstallPackLauncherPack(file, zipArchive, archiveBaseFolder);
			default:
				ModBase.Log("[ModPack] 整合包种类：未能识别，假定为压缩包");
				return InstallPackCompress(file, zipArchive);
			}
		}
		finally
		{
			zipArchive?.Dispose();
		}
	}

	private static void ExtractModpackFiles(string installTemp, string fileAddress, ModLoader.LoaderBase loader, double progressIncrement)
	{
		int num = 1;
		Encoding encode = Encoding.GetEncoding("GB18030");
		double progress = loader.Progress;
		while (num <= 5)
		{
			try
			{
				loader.Progress = progress;
				ModBase.DeleteDirectory(installTemp);
				ModBase.ExtractFile(fileAddress, installTemp, encode, (double delta) =>
				{
					loader.Progress += delta * progressIncrement;
				});
				loader.Progress = progress + progressIncrement;
				break;
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, $"第 {num} 次解压尝试失败");
				if (ex is ArgumentException || ex is IOException)
				{
					encode = Encoding.UTF8;
					ModBase.Log("[ModPack] 已切换压缩包解压编码为 UTF8");
				}
				if (loader != null && loader.LoadingState != MyLoading.MyLoadingState.Run)
				{
					break;
				}
				num++;
				if (num <= 5)
				{
					Thread.Sleep((num - 1) * 2000);
					continue;
				}
				throw new Exception("解压整合包文件失败", ex);
			}
		}
	}

	private static void CopyOverrideDirectory(string overridesFolder, string versionFolder, ModLoader.LoaderBase loader, double progressIncrement)
	{
		if (!overridesFolder.EndsWithF("\\"))
		{
			overridesFolder += "\\";
		}
		if (!versionFolder.EndsWithF("\\"))
		{
			versionFolder += "\\";
		}
		if (Directory.Exists(overridesFolder))
		{
			ModBase.Log("[ModPack] 处理整合包覆写文件夹：" + overridesFolder + " → " + versionFolder);
			ModBase.CopyDirectory(overridesFolder, versionFolder, (double delta) =>
			{
				loader.Progress += delta * progressIncrement;
			});
		}
		else
		{
			ModBase.Log("[ModPack] 整合包中没有覆写文件夹：" + overridesFolder);
			loader.Progress += progressIncrement;
		}
		string text = overridesFolder + "PCL\\Setup.ini";
		string text2 = versionFolder + "PCL\\Setup.ini";
		if (File.Exists(text))
		{
			ModBase.WriteIni(text, "VersionArgumentIndie", 1.ToString());
			ModBase.WriteIni(text, "VersionArgumentIndieV2", true.ToString());
			ModBase.CopyFile(text, text2);
		}
		else
		{
			ModBase.WriteIni(text2, "VersionArgumentIndie", 1.ToString());
			ModBase.WriteIni(text2, "VersionArgumentIndieV2", true.ToString());
		}
		ModBase.IniClearCache(text2);
	}

	private static ModLoader.LoaderCombo<string> InstallPackCurseForge(string fileAddress, ZipArchive archive, string archiveBaseFolder, string instanceName = null, string logo = null, string resourceId = null, bool isOnlineInstall = false)
	{
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Expected Obj, but got Unknown
		JsonObject json;
		try
		{
			json = (JsonObject)ModBase.GetJson(ModBase.ReadFile(archive.GetEntry(archiveBaseFolder + "manifest.json").Open()));
		}
		catch (Exception innerException)
		{
			throw new Exception("CurseForge 整合包安装信息存在问题", innerException);
		}
		if (json["minecraft"] == null || json["minecraft"]["version"] == null)
		{
			throw new Exception("CurseForge 整合包未提供 Minecraft 版本信息");
		}
		if (instanceName == null)
		{
			instanceName = (string?)(json["name"] ?? ((JsonNode?)""));
			FolderNameValidator val = new FolderNameValidator(Path.Combine(ModFolder.mcFolderSelected, "versions"), true, true, false);
			if (!((AbstractValidator<string>)(object)val).Validate(instanceName).IsValid)
			{
				instanceName = "";
			}
			if (string.IsNullOrEmpty(instanceName))
			{
				instanceName = ModMain.MyMsgBoxInput(Lang.Text("Minecraft.Download.Modpack.InputInstanceName"), "", "", new Collection<IValidator<string>> { (IValidator<string>)(object)val });
			}
			if (string.IsNullOrEmpty(instanceName))
			{
				throw new ModBase.CancelledException();
			}
		}
		string forgeVersion = null;
		string neoForgeVersion = null;
		string fabricVersion = null;
		ModComp.CompLoaderType modLoader = ModComp.CompLoaderType.Any;
		foreach (dynamic item in (dynamic)(((object)json["minecraft"]["modLoaders"]) ?? ((object)Array.Empty<JsonNode>())))
		{
			string text = (item["id"] ?? "").ToString().ToLower();
			if (text.StartsWithF("forge-"))
			{
				if (text.Contains("recommended"))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Modpack.TooOldUnsupported"));
				}
				ModBase.Log("[ModPack] 整合包 Forge 版本：" + text);
				forgeVersion = text.Replace("forge-", "");
				modLoader = ModComp.CompLoaderType.Forge;
				continue;
			}
			if (text.StartsWithF("neoforge-"))
			{
				ModBase.Log("[ModPack] 整合包 NeoForge 版本：" + text);
				neoForgeVersion = text.Replace("neoforge-", "");
				modLoader = ModComp.CompLoaderType.NeoForge;
				continue;
			}
			if (text.StartsWithF("fabric-"))
			{
				try
				{
					ModBase.Log("[ModPack] 整合包 Fabric 版本：" + text);
					fabricVersion = text.Replace("fabric-", "");
					modLoader = ModComp.CompLoaderType.Fabric;
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "读取整合包 Fabric 版本失败：" + text);
					continue;
				}
				break;
			}
			if (!text.StartsWithF("quilt-"))
			{
				continue;
			}
			throw new Exception(Lang.Text("Minecraft.Download.Modpack.QuiltUnsupported"));
		}
		string installTemp = ModMain.RequestTaskTempFolder();
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		string overrideHome = (string?)(json["overrides"] ?? ((JsonNode?)""));
		if (!string.IsNullOrEmpty(overrideHome))
		{
			list.Add(new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractModpack"), (ModLoader.LoaderTask<string, int> task) =>
			{
				ExtractModpackFiles(installTemp, fileAddress, task, 0.6);
				CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, (overrideHome == "." || overrideHome == "./") ? "" : overrideHome), ModFolder.mcFolderSelected + "versions\\" + instanceName, task, 0.4);
			})
			{
				ProgressWeight = (double)new FileInfo(fileAddress).Length / 1024.0 / 1024.0 / 6.0,
				block = false
			});
		}
		List<int> modList = new List<int>();
		List<int> modOptionalList = new List<int>();
		foreach (dynamic item2 in (dynamic)(((object)json["files"]) ?? ((object)Array.Empty<JsonNode>())))
		{
			if ((object)item2["projectID"] == null || (object)item2["fileID"] == null)
			{
				HintService.Hint(Lang.Text("Minecraft.Download.Modpack.ModMissingRequiredInfoSkipped", item2));
				continue;
			}
			modList.Add((int)item2["fileID"]);
			if (item2["required"] is JsonNode jsonNode && !JsonCompat.ToObject<bool>(jsonNode))
			{
				modOptionalList.Add((int)item2["fileID"]);
			}
		}
		if (modList.Any())
		{
			List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
			list2.Add(new ModLoader.LoaderTask<int, JsonArray>(Lang.Text("Minecraft.Download.Modpack.Stage.PrepareModsDownloadInfo"), (ModLoader.LoaderTask<int, JsonArray> task) =>
			{
				bool flag = true;
				int num = 0;
				do
				{
					num++;
					JsonArray jsonArray = (JsonArray)((JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.curseforge.com/v1/mods/files", "POST", "{\"fileIds\": [" + modList.Join(",") + "]}", "application/json", flag)))["data"];
					if (modList.Count <= jsonArray.Count)
					{
						ModBase.Log("[Modpack] 已获取到的模组数量足够，开始进行下一步");
						task.output = jsonArray;
						return;
					}
					flag = false;
					ModBase.Log($"[Modpack] 获取模组数量不达标，设置镜像源允许状态为: {flag}");
				}
				while (num <= 3);
				throw new Exception(Lang.Text("Minecraft.Download.Modpack.SomeModsDeleted"));
			})
			{
				ProgressWeight = (double)modList.Count / 10.0
			});
			list2.Add(new ModLoader.LoaderTask<JsonArray, List<DownloadFile>>(Lang.Text("Minecraft.Download.Modpack.Stage.BuildModsDownloadInfo"), (ModLoader.LoaderTask<JsonArray, List<DownloadFile>> task) =>
			{
				Dictionary<int, DownloadFile> dictionary = new Dictionary<int, DownloadFile>();
				foreach (JsonNode item3 in task.input)
				{
					int num = JsonCompat.ToObject<int>(item3["id"]);
					if (!dictionary.ContainsKey(num) && (!modOptionalList.Contains(num) || ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Modpack.OptionalFile.Message", new object[1] { item3["displayName"] }), Lang.Text("Minecraft.Download.Modpack.OptionalFile.Title"), Lang.Text("Minecraft.Download.Modpack.OptionalFile.Download"), Lang.Text("Minecraft.Download.Modpack.OptionalFile.Skip")) != 2))
					{
						string value;
						ModComp.CompType defaultType;
						if (item3["modules"].AsArray().Any())
						{
							List<string> list5 = ((JsonArray)item3["modules"]).Select((JsonNode l) => l["name"].ToString()).ToList();
							if (list5.Contains("META-INF") || list5.Contains("mcmod.info") || item3?["FileName"]?.ToString()?.EndsWithF(".jar", ignoreCase: true) == true)
							{
								value = "mods";
								defaultType = ModComp.CompType.Mod;
							}
							else if (list5.Contains("pack.mcmeta"))
							{
								value = "resourcepacks";
								defaultType = ModComp.CompType.ResourcePack;
							}
							else if (list5.Contains("level.dat"))
							{
								value = "saves";
								defaultType = ModComp.CompType.World;
							}
							else
							{
								value = "shaderpacks";
								defaultType = ModComp.CompType.Shader;
							}
						}
						else
						{
							value = "mods";
							defaultType = ModComp.CompType.Mod;
						}
						ModComp.CompFile compFile = new ModComp.CompFile((JsonObject)item3, defaultType);
						if (compFile.Available)
						{
							dictionary.Add(num, compFile.ToNetFile($"{ModFolder.mcFolderSelected}versions\\{instanceName}\\{value}\\", ModComp.DownloadReason.ModPack, json["minecraft"]["version"].ToString(), modLoader));
							task.Progress += 1.0 / (double)(1 + modList.Count);
						}
					}
				}
				task.output = dictionary.Values.ToList();
			})
			{
				ProgressWeight = (double)modList.Count / 200.0,
				show = false
			});
			list2.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Modpack.Stage.DownloadMods"), new List<DownloadFile>())
			{
				ProgressWeight = (double)modList.Count * 1.5
			});
			list.Add(new ModLoader.LoaderCombo<int>(Lang.Text("Minecraft.Download.Modpack.Stage.DownloadMods.MainLoader"), list2)
			{
				show = false,
				ProgressWeight = list2.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
			});
		}
		ModDownloadLib.McInstallRequest mcInstallRequest = new ModDownloadLib.McInstallRequest
		{
			targetInstanceName = instanceName,
			targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\",
			minecraftName = json["minecraft"]["version"].ToString(),
			forgeVersion = forgeVersion,
			neoForgeVersion = neoForgeVersion,
			fabricVersion = fabricVersion
		};
		List<ModLoader.LoaderBase> list3 = ModDownloadLib.McInstallLoader(mcInstallRequest);
		List<ModLoader.LoaderBase> list4 = new List<ModLoader.LoaderBase>();
		list4.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ModpackInstall"), list)
		{
			show = false,
			block = false,
			ProgressWeight = list.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		list4.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.GameInstall"), list3)
		{
			show = false,
			ProgressWeight = list3.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		list4.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Minecraft.Download.Modpack.Stage.FinalizeFiles"), (ModLoader.LoaderTask<string, string> task) =>
		{
			string text2 = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\";
			if (logo != null && File.Exists(logo))
			{
				File.Copy(logo, Path.Combine(text2, "PCL", "Logo.png"), overwrite: true);
				((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)text2] = "PCL\\Logo.png";
				((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)text2] = true;
				ModBase.Log("[ModPack] 已设置整合包 Logo：" + logo);
			}
			string[] array = new string[2]
			{
				Path.Combine(text2, "原始整合包.zip"),
				Path.Combine(text2, "原始整合包.mrpack")
			};
			foreach (string text3 in array)
			{
				if (File.Exists(text3))
				{
					ModBase.Log("[ModPack] 删除原始整合包文件：" + text3);
					File.Delete(text3);
				}
			}
			if (File.Exists(fileAddress) && ModBase.GetFileNameWithoutExtentionFromPath(fileAddress) == "modpack")
			{
				ModBase.Log("[ModPack] 删除安装整合包文件：" + fileAddress);
				File.Delete(fileAddress);
			}
			if (json["version"] != null)
			{
				((ParameterizedProperty<object, string>)(object)States.Instance.ModpackVersion)[(object)text2] = json["version"].ToString();
			}
			((ParameterizedProperty<object, string>)(object)States.Instance.ModpackSource)[(object)text2] = "CurseForge";
			((ParameterizedProperty<object, string>)(object)States.Instance.ModpackId)[(object)text2] = resourceId;
			try
			{
				int i = 1;
				List<string> list5 = new List<string>(i);
				CollectionsMarshal.SetCount(list5, i);
				CollectionsMarshal.AsSpan(list5)[0] = resourceId;
				List<ModComp.CompProject> compProjectsByIds = ModComp.CompRequest.GetCompProjectsByIds(list5);
				if (compProjectsByIds.Count != 0)
				{
					((ParameterizedProperty<object, string>)(object)States.Instance.CustomInfo)[(object)text2] = compProjectsByIds.First().Description;
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[ModPack] 获取整合包描述文本失败");
			}
		})
		{
			ProgressWeight = 0.1,
			show = false
		});
		string loaderName = Lang.Text("Minecraft.Download.Modpack.Task.CurseForgeInstall", new object[1] { instanceName });
		if (ModLoader.loaderTaskbar.Any((ModLoader.LoaderBase l) => (l.name ?? "") == (loaderName ?? "")))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.Installing"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(loaderName, list4);
		loaderCombo.OnStateChanged = ModDownloadLib.McInstallState;
		loaderCombo.Start(mcInstallRequest.targetInstanceFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		if (!isOnlineInstall)
		{
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
			});
		}
		return loaderCombo;
	}

	private static ModLoader.LoaderCombo<string> InstallPackModrinth(string fileAddress, ZipArchive archive, string archiveBaseFolder, string instanceName = null, string logo = null, string resourceId = null, bool isOnlineInstall = false)
	{
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Expected Obj, but got Unknown
		JsonObject json;
		try
		{
			json = (JsonObject)ModBase.GetJson(ModBase.ReadFile(archive.GetEntry(archiveBaseFolder + "modrinth.index.json").Open()));
		}
		catch (Exception innerException)
		{
			throw new Exception("Modrinth 整合包安装信息存在问题", innerException);
		}
		if (json["dependencies"] == null || json["dependencies"]["minecraft"] == null)
		{
			throw new Exception("Modrinth 整合包未提供 Minecraft 版本信息");
		}
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		ModComp.CompLoaderType modLoader = ModComp.CompLoaderType.Any;
		foreach (KeyValuePair<string, JsonNode> item in json["dependencies"]?.AsObject() ?? new JsonObject())
		{
			switch (item.Key.ToLower() ?? "")
			{
			case "minecraft":
			{
				JsonNode value3 = item.Value;
				text = ((value3 != null) ? JsonCompat.ToObject<string>(value3) : null);
				break;
			}
			case "forge":
			{
				JsonNode value2 = item.Value;
				text2 = ((value2 != null) ? JsonCompat.ToObject<string>(value2) : null);
				modLoader = ModComp.CompLoaderType.Forge;
				ModBase.Log("[ModPack] 整合包 Forge 版本：" + text2);
				break;
			}
			case "neoforge":
			case "neo-forge":
			{
				JsonNode value4 = item.Value;
				text3 = ((value4 != null) ? JsonCompat.ToObject<string>(value4) : null);
				modLoader = ModComp.CompLoaderType.NeoForge;
				ModBase.Log("[ModPack] 整合包 NeoForge 版本：" + text3);
				break;
			}
			case "fabric-loader":
			{
				JsonNode value = item.Value;
				text4 = ((value != null) ? JsonCompat.ToObject<string>(value) : null);
				modLoader = ModComp.CompLoaderType.Fabric;
				ModBase.Log("[ModPack] 整合包 Fabric 版本：" + text4);
				break;
			}
			case "quilt-loader":
				throw new Exception(Lang.Text("Minecraft.Download.Modpack.QuiltUnsupported"));
			default:
				HintService.Hint(Lang.Text("Minecraft.Download.Modpack.UnknownLoader", new object[2] { item.Key, item.Value }), HintType.Error);
				break;
			}
		}
		if (instanceName == null)
		{
			instanceName = (string?)(json["name"] ?? ((JsonNode?)""));
			FolderNameValidator val = new FolderNameValidator(Path.Combine(ModFolder.mcFolderSelected, "versions"), true, true, false);
			if (!((AbstractValidator<string>)(object)val).Validate(instanceName).IsValid)
			{
				instanceName = "";
			}
			if (string.IsNullOrEmpty(instanceName))
			{
				instanceName = ModMain.MyMsgBoxInput(Lang.Text("Minecraft.Download.Modpack.InputInstanceName"), "", "", new Collection<IValidator<string>> { (IValidator<string>)(object)val });
			}
			if (string.IsNullOrEmpty(instanceName))
			{
				throw new ModBase.CancelledException();
			}
		}
		string installTemp = ModMain.RequestTaskTempFolder();
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		list.Add(new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractModpack"), (ModLoader.LoaderTask<string, int> task) =>
		{
			ExtractModpackFiles(installTemp, fileAddress, task, 0.5);
			CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, "overrides"), Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName), task, 0.4);
			CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, "client-overrides"), Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName), task, 0.1);
		})
		{
			ProgressWeight = (double)new FileInfo(fileAddress).Length / 1024.0 / 1024.0 / 6.0,
			block = false
		});
		List<DownloadFile> list2 = new List<DownloadFile>();
		foreach (dynamic item2 in (dynamic)(((object)json["files"]) ?? ((object)Array.Empty<JsonNode>())))
		{
			if ((object)item2["env"] != null)
			{
				switch ((item2["env"]["client"].ToString() ?? "") as string)
				{
				case "optional":
					if (ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Modpack.OptionalFile.Message", ModBase.GetFileNameFromPath(item2["path"].ToString())), Lang.Text("Minecraft.Download.Modpack.OptionalFile.Title"), Lang.Text("Minecraft.Download.Modpack.OptionalFile.Download"), Lang.Text("Minecraft.Download.Modpack.OptionalFile.Skip")) == 2)
					{
						continue;
					}
					break;
				case "unsupported":
					continue;
				}
			}
			List<string> source = (from x in ((JsonArray)item2["downloads"]).OfType<JsonNode>()
				select ModComp.CompFile.HandleCurseForgeDownloadUrls(x.ToString())).ToList();
			source = source.SelectMany((string x) => ModDownload.DlSourceModDownloadGet(x)).ToList();
			string text5 = string.Format("{0}versions\\{1}\\{2}", ModFolder.mcFolderSelected, instanceName, (object?)item2["path"]);
			if (!Path.GetFullPath(text5).StartsWithF(ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\", ignoreCase: true))
			{
				ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Modpack.PathOutsideInstance.Message", new object[1] { text5 }), Lang.Text("Minecraft.Download.Modpack.PathOutsideInstance.Title"), null, "", "", isWarn: true);
				throw new ModBase.CancelledException();
			}
			list2.Add(new DownloadFile(ModComp.CompFile.HandleModrinthDownloadUrls(source, ModComp.DownloadReason.ModPack, text, modLoader), text5, new ModBase.FileChecker(actualSize: JsonCompat.ToObject<long>((JsonNode)item2["fileSize"]), hash: item2["hashes"]["sha1"].ToString()), useBrowserUserAgent: true));
		}
		if (list2.Any())
		{
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Modpack.Stage.DownloadAdditions"), list2)
			{
				ProgressWeight = (double)list2.Count * 1.5
			});
		}
		ModDownloadLib.McInstallRequest mcInstallRequest = new ModDownloadLib.McInstallRequest
		{
			targetInstanceName = instanceName,
			targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\",
			minecraftName = text,
			forgeVersion = text2,
			neoForgeVersion = text3,
			fabricVersion = text4
		};
		List<ModLoader.LoaderBase> list3 = ModDownloadLib.McInstallLoader(mcInstallRequest);
		List<ModLoader.LoaderBase> list4 = new List<ModLoader.LoaderBase>();
		list4.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ModpackInstall"), list)
		{
			show = false,
			block = false,
			ProgressWeight = list.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		list4.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.GameInstall"), list3)
		{
			show = false,
			ProgressWeight = list3.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		list4.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Minecraft.Download.Modpack.Stage.FinalizeFiles"), (ModLoader.LoaderTask<string, string> task) =>
		{
			string text6 = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\";
			if (logo != null && File.Exists(logo))
			{
				File.Copy(logo, Path.Combine(text6, "PCL", "Logo.png"), overwrite: true);
				((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)text6] = "PCL\\Logo.png";
				((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)text6] = true;
				ModBase.Log("[ModPack] 已设置整合包 Logo：" + logo);
			}
			string[] array = new string[2]
			{
				Path.Combine(text6, "原始整合包.zip"),
				Path.Combine(text6, "原始整合包.mrpack")
			};
			foreach (string text7 in array)
			{
				if (File.Exists(text7))
				{
					ModBase.Log("[ModPack] 删除原始整合包文件：" + text7);
					File.Delete(text7);
				}
			}
			if (File.Exists(fileAddress) && ModBase.GetFileNameWithoutExtentionFromPath(fileAddress) == "modpack")
			{
				ModBase.Log("[ModPack] 删除安装整合包文件：" + fileAddress);
				File.Delete(fileAddress);
			}
			if (json["versionId"] != null)
			{
				((ParameterizedProperty<object, string>)(object)States.Instance.ModpackVersion)[(object)text6] = json["versionId"].ToString();
			}
			((ParameterizedProperty<object, string>)(object)States.Instance.ModpackSource)[(object)text6] = "Modrinth";
			((ParameterizedProperty<object, string>)(object)States.Instance.ModpackId)[(object)text6] = resourceId;
			try
			{
				int i = 1;
				List<string> list5 = new List<string>(i);
				CollectionsMarshal.SetCount(list5, i);
				CollectionsMarshal.AsSpan(list5)[0] = resourceId;
				List<ModComp.CompProject> compProjectsByIds = ModComp.CompRequest.GetCompProjectsByIds(list5);
				if (compProjectsByIds.Count != 0)
				{
					((ParameterizedProperty<object, string>)(object)States.Instance.CustomInfo)[(object)text6] = compProjectsByIds.First().Description;
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[ModPack] 获取整合包描述文本失败");
			}
		})
		{
			ProgressWeight = 0.1,
			show = false
		});
		string loaderName = Lang.Text("Minecraft.Download.Modpack.Task.ModrinthInstall", new object[1] { instanceName });
		if (ModLoader.loaderTaskbar.Any((ModLoader.LoaderBase l) => (l.name ?? "") == (loaderName ?? "")))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.Installing"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(loaderName, list4);
		loaderCombo.OnStateChanged = ModDownloadLib.McInstallState;
		loaderCombo.Start(mcInstallRequest.targetInstanceFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		if (!isOnlineInstall)
		{
			ModBase.RunInUi(() =>
			{
				ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
			});
		}
		return loaderCombo;
	}

	private static ModLoader.LoaderCombo<string> InstallPackHMCL(string fileAddress, ZipArchive archive, string archiveBaseFolder)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected Obj, but got Unknown
		JsonObject jsonObject;
		try
		{
			jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(archive.GetEntry(archiveBaseFolder + "modpack.json").Open(), Encoding.UTF8));
		}
		catch (Exception innerException)
		{
			throw new Exception("HMCL 整合包安装信息存在问题", innerException);
		}
		string instanceName = (string?)(jsonObject["name"] ?? ((JsonNode?)""));
		FolderNameValidator val = new FolderNameValidator(Path.Combine(ModFolder.mcFolderSelected, "versions"), true, true, false);
		if (!((AbstractValidator<string>)(object)val).Validate(instanceName).IsValid)
		{
			instanceName = "";
		}
		if (string.IsNullOrEmpty(instanceName))
		{
			instanceName = ModMain.MyMsgBoxInput(Lang.Text("Minecraft.Download.Modpack.InputInstanceName"), "", "", new Collection<IValidator<string>> { (IValidator<string>)(object)val });
		}
		if (string.IsNullOrEmpty(instanceName))
		{
			throw new ModBase.CancelledException();
		}
		string installTemp = ModMain.RequestTaskTempFolder();
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		list.Add(new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractModpack"), (ModLoader.LoaderTask<string, int> task) =>
		{
			ExtractModpackFiles(installTemp, fileAddress, task, 0.6);
			CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, "minecraft"), Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName), task, 0.4);
		})
		{
			ProgressWeight = (double)new FileInfo(fileAddress).Length / 1024.0 / 1024.0 / 6.0,
			block = false
		});
		if (jsonObject["gameVersion"] == null)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Modpack.MissingGameVersion.Hmcl"));
		}
		ModDownloadLib.McInstallRequest mcInstallRequest = new ModDownloadLib.McInstallRequest
		{
			targetInstanceName = instanceName,
			targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\",
			minecraftName = jsonObject["gameVersion"].ToString()
		};
		List<ModLoader.LoaderBase> list2 = ModDownloadLib.McInstallLoader(mcInstallRequest);
		List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>
		{
			new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ModpackInstall"), list)
			{
				show = false,
				block = false,
				ProgressWeight = list.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
			},
			new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.GameInstall"), list2)
			{
				show = false,
				ProgressWeight = list2.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
			}
		};
		string loaderName = Lang.Text("Minecraft.Download.Modpack.Task.HmclInstall", new object[1] { instanceName });
		if (ModLoader.loaderTaskbar.Any((ModLoader.LoaderBase l) => (l.name ?? "") == (loaderName ?? "")))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.Installing"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(loaderName, loaders);
		loaderCombo.OnStateChanged = ModDownloadLib.McInstallState;
		loaderCombo.Start(mcInstallRequest.targetInstanceFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModBase.RunInUi(() =>
		{
			ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
		});
		return loaderCombo;
	}

	private static string HandleLaunchArguments(JsonNode? argumentNode)
	{
		IEnumerable<string> enumerable;
		if (argumentNode is JsonArray source)
		{
			enumerable = from argument in source.OfType<JsonValue>()
				where argument.GetValueKind() == JsonValueKind.String
				select argument.GetValue<string>();
		}
		else
		{
			enumerable = ((!(argumentNode is JsonValue jsonValue) || jsonValue.GetValueKind() != JsonValueKind.String) ? ((IEnumerable<string>)Array.Empty<string>()) : ((IEnumerable<string>)new _003C_003Ez__ReadOnlySingleElementList<string>(jsonValue.GetValue<string>())));
		}
		IEnumerable<string> source2 = enumerable;
		return string.Join(" ", source2.Where((string argument) => !string.IsNullOrWhiteSpace(argument)));
	}

	private static ModLoader.LoaderCombo<string> InstallPackMCBBS(string fileAddress, ZipArchive archive, string archiveBaseFolder, string instanceName = null)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected Obj, but got Unknown
		JsonObject json;
		try
		{
			using Stream stream = (archive.GetEntry(archiveBaseFolder + "mcbbs.packmeta") ?? archive.GetEntry(archiveBaseFolder + "manifest.json")).Open();
			json = (JsonObject)ModBase.GetJson(ModBase.ReadFile(stream, Encoding.UTF8));
		}
		catch (Exception innerException)
		{
			throw new Exception("MCBBS 整合包安装信息存在问题", innerException);
		}
		if (instanceName == null)
		{
			instanceName = json["name"]?.ToString() ?? "";
			FolderNameValidator val = new FolderNameValidator(Path.Combine(ModFolder.mcFolderSelected, "versions"), true, true, false);
			if (!((AbstractValidator<string>)(object)val).Validate(instanceName).IsValid)
			{
				instanceName = "";
			}
			if (string.IsNullOrEmpty(instanceName))
			{
				instanceName = ModMain.MyMsgBoxInput(Lang.Text("Minecraft.Download.Modpack.InputInstanceName"), "", "", new Collection<IValidator<string>> { (IValidator<string>)(object)val });
			}
			if (string.IsNullOrEmpty(instanceName))
			{
				throw new ModBase.CancelledException();
			}
		}
		string installTemp = ModMain.RequestTaskTempFolder();
		string versionFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName;
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		ModLoader.LoaderTask<string, int> loaderTask = new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractModpack"), (ModLoader.LoaderTask<string, int> task) =>
		{
			ExtractModpackFiles(installTemp, fileAddress, task, 0.6);
			CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, "overrides"), Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName), task, 0.4);
			if (json["launchInfo"] != null)
			{
				JsonObject jsonObject2 = (JsonObject)json["launchInfo"];
				((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)versionFolder] = HandleLaunchArguments(jsonObject2["javaArgument"]);
				((ParameterizedProperty<object, string>)(object)Config.Instance.GameArgs)[(object)versionFolder] = HandleLaunchArguments(jsonObject2["launchArgument"]);
			}
			if (json["version"] != null)
			{
				((ParameterizedProperty<object, string>)(object)States.Instance.ModpackVersion)[(object)versionFolder] = json["version"].ToString();
			}
		});
		loaderTask.ProgressWeight = (double)new FileInfo(fileAddress).Length / 1024.0 / 1024.0 / 6.0;
		loaderTask.block = false;
		list.Add(loaderTask);
		if (json["addons"] == null)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Modpack.MissingGameVersion.McbbsAddons"));
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (JsonNode item in json["addons"].AsArray())
		{
			JsonObject jsonObject = item.AsObject();
			dictionary.Add(jsonObject["id"].ToString(), jsonObject["version"].ToString());
		}
		if (!dictionary.ContainsKey("game"))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.MissingGameVersion.Generic"), HintType.Error);
			return null;
		}
		if (dictionary.ContainsKey("quilt"))
		{
			throw new Exception(Lang.Text("Minecraft.Download.Modpack.QuiltUnsupported"));
		}
		ModDownloadLib.McInstallRequest mcInstallRequest = new ModDownloadLib.McInstallRequest
		{
			targetInstanceName = instanceName,
			targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\",
			minecraftName = dictionary["game"],
			optiFineVersion = (dictionary.ContainsKey("optifine") ? dictionary["optifine"] : null),
			forgeVersion = (dictionary.ContainsKey("forge") ? dictionary["forge"] : null),
			neoForgeVersion = (dictionary.ContainsKey("neoforge") ? dictionary["neoforge"] : null),
			fabricVersion = (dictionary.ContainsKey("fabric") ? dictionary["fabric"] : null)
		};
		List<ModLoader.LoaderBase> list2 = ModDownloadLib.McInstallLoader(mcInstallRequest);
		List<ModLoader.LoaderBase> list3 = new List<ModLoader.LoaderBase>();
		list3.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ModpackInstall"), list)
		{
			show = false,
			block = false,
			ProgressWeight = list.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		list3.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.GameInstall"), list2)
		{
			show = false,
			ProgressWeight = list2.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		string loaderName = Lang.Text("Minecraft.Download.Modpack.Task.McbbsInstall", new object[1] { instanceName });
		if (ModLoader.loaderTaskbar.Any((ModLoader.LoaderBase l) => l.name == loaderName))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.Installing"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(loaderName, list3);
		loaderCombo.OnStateChanged = ModDownloadLib.McInstallState;
		loaderCombo.Start(mcInstallRequest.targetInstanceFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModBase.RunInUi(() =>
		{
			ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
		});
		return loaderCombo;
	}

	private static ModLoader.LoaderCombo<string> InstallPackLauncherPack(string fileAddress, ZipArchive archive, string archiveBaseFolder)
	{
		ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Modpack.SelectEmptyFolder.Message"), Lang.Text("Common.Action.Install"), Lang.Text("Common.Action.Continue"), "", "", isWarn: false, highLight: true, forceWait: true);
		string targetFolder = SystemDialogs.SelectFolder(Lang.Text("Minecraft.Download.Modpack.SelectTargetFolder.Title"), (string)null);
		if (string.IsNullOrEmpty(targetFolder))
		{
			throw new ModBase.CancelledException();
		}
		if (Directory.GetFileSystemEntries(targetFolder).Length != 0)
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.TargetFolderMustBeEmpty"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractArchive"), new ModLoader.LoaderTask<string, int>[1]
		{
			new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractArchive"), (ModLoader.LoaderTask<string, int> task) =>
			{
				ExtractModpackFiles(targetFolder, fileAddress, task, 0.9);
				Thread.Sleep(400);
				string text = null;
				string[] files = Directory.GetFiles(targetFolder, "*.exe", SearchOption.TopDirectoryOnly);
				foreach (string text2 in files)
				{
					FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(text2);
					ModBase.Log("[Modpack] 文件 " + text2 + " 的产品名标识为 " + versionInfo.ProductName);
					if (versionInfo.ProductName == "Plain Craft Launcher")
					{
						text = text2;
						ModBase.Log("[Modpack] 发现整合包附带的 PCL 启动器：" + text2);
					}
					else if ((versionInfo.ProductName.ContainsF("Launcher", ignoreCase: true) || versionInfo.ProductName.ContainsF("启动", ignoreCase: true)) && !(versionInfo.ProductName == "Plain Craft Launcher Admin Manager") && text == null)
					{
						text = text2;
						ModBase.Log("[Modpack] 发现整合包附带的疑似第三方启动器：" + text2);
					}
				}
				task.Progress = 0.95;
				if (text != null)
				{
					ModBase.Log("[Modpack] 找到压缩包中附带的启动器：" + text);
					if (ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Modpack.BundledLauncher.Message", new object[1] { text }), Lang.Text("Minecraft.Download.Modpack.BundledLauncher.Title"), Lang.Text("Minecraft.Download.Modpack.BundledLauncher.UseBundled"), Lang.Text("Minecraft.Download.Modpack.BundledLauncher.DoNotUse")) == 1)
					{
						ModBase.OpenExplorer(targetFolder);
						ModBase.ShellOnly(text, "--wait");
						ModBase.Log("[Modpack] 为换用整合包中的启动器启动，强制结束程序");
						ModMain.frmMain.EndProgram(sendWarning: false);
						return;
					}
				}
				else
				{
					ModBase.Log("[Modpack] 未找到压缩包中附带的启动器");
				}
				ModBase.OpenExplorer(targetFolder);
				string folderNameFromPath = ModBase.GetFolderNameFromPath(targetFolder);
				Directory.CreateDirectory(Path.Combine(targetFolder, ".minecraft"));
				PageSelectLeft.AddFolder(Path.Combine(targetFolder, ".minecraft", archiveBaseFolder.Replace("/", "\\").TrimStart('\\')), folderNameFromPath, showHint: false);
				string text3 = Directory.GetFiles(targetFolder, "modpack.*", SearchOption.AllDirectories).First();
				ModBase.Log("[Modpack] 调用 modpack 文件继续安装：" + text3);
				ModpackInstall(text3);
			})
		});
		loaderCombo.Start(targetFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
		return loaderCombo;
	}

	private static ModLoader.LoaderCombo<string> InstallPackCompress(string fileAddress, ZipArchive archive)
	{
		Match match = null;
		Regex regex = new Regex("^.*\\/(?=versions\\/(?<ver>[^\\/]+)\\/(\\k<ver>)\\.json$)", RegexOptions.IgnoreCase);
		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			Match match2 = regex.Match("/" + entry.FullName);
			if (match2.Success)
			{
				match = match2;
				break;
			}
		}
		if (match == null)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Modpack.UnknownArchiveStructure"));
		}
		string archiveBaseFolder = match.Value.Replace("/", "\\").TrimStart('\\');
		string value = match.Groups[1].Value;
		ModBase.Log("[ModPack] 检测到压缩包的 .minecraft 根目录：" + archiveBaseFolder + "，命中的实例名：" + value);
		ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Modpack.SelectEmptyFolder.Message"), Lang.Text("Common.Action.Install"), Lang.Text("Common.Action.Continue"), "", "", isWarn: false, highLight: true, forceWait: true);
		string targetFolder = SystemDialogs.SelectFolder(Lang.Text("Minecraft.Download.Modpack.SelectTargetFolder.Title"), (string)null);
		if (string.IsNullOrEmpty(targetFolder))
		{
			throw new ModBase.CancelledException();
		}
		if (targetFolder.Contains("!") || targetFolder.Contains(";"))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.InvalidGamePathChars", new object[1] { targetFolder }), HintType.Error);
			throw new ModBase.CancelledException();
		}
		if (Directory.GetFileSystemEntries(targetFolder).Length != 0)
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.TargetFolderMustBeEmpty"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractArchive"), new ModLoader.LoaderTask<string, int>[1]
		{
			new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractArchive"), (ModLoader.LoaderTask<string, int> task) =>
			{
				ExtractModpackFiles(targetFolder, fileAddress, task, 0.95);
				PageSelectLeft.AddFolder(Path.Combine(targetFolder, archiveBaseFolder), ModBase.GetFolderNameFromPath(targetFolder), showHint: false);
				Thread.Sleep(400);
				ModBase.RunInUi(() =>
				{
					ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
				});
			})
		});
		loaderCombo.OnStateChanged = ModDownloadLib.McInstallState;
		loaderCombo.Start(targetFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
		return loaderCombo;
	}

	private static ModLoader.LoaderCombo<string> InstallPackMMC(string fileAddress, ZipArchive archive, string archiveBaseFolder)
	{
		//IL_0c71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c78: Expected Obj, but got Unknown
		MMCPackInfo mMCPackInfo = null;
		JsonObject jsonObject;
		string str;
		try
		{
			jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(archive.GetEntry(archiveBaseFolder + "mmc-pack.json").Open(), Encoding.UTF8));
			ZipArchiveEntry entry = archive.GetEntry(archiveBaseFolder + "instance.cfg");
			str = ((entry == null) ? "" : ModBase.ReadFile(entry.Open(), Encoding.UTF8));
			try
			{
				if (archive.Entries.Any((ZipArchiveEntry e) => !e.FullName.EndsWith("/") && e.FullName.StartsWith(archiveBaseFolder + "patches/", StringComparison.OrdinalIgnoreCase)))
				{
					ModBase.Log("[ModPack] 安装的 MultiMC 整合包存在 JSON Patches");
					List<KeyValuePair<JsonObject, int>> list = new List<KeyValuePair<JsonObject, int>>();
					foreach (ZipArchiveEntry entry2 in archive.Entries)
					{
						if (!entry2.FullName.EndsWith("/") && entry2.FullName.StartsWith(archiveBaseFolder + "patches/", StringComparison.OrdinalIgnoreCase))
						{
							JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(archive.GetEntry(entry2.FullName).Open(), Encoding.UTF8));
							list.Add(new KeyValuePair<JsonObject, int>(jsonObject2, (int)((jsonObject2["order"] != null) ? jsonObject2["order"] : ((JsonNode)0))));
						}
					}
					JsonArray source = (JsonArray)jsonObject["components"];
					HashSet<string> componentUids = source.Select((JsonNode c) => c["uid"]?.ToString()).ToHashSet();
					list = (from p in list
						where componentUids.Contains(p.Key["uid"]?.ToString())
						orderby p.Value
						select p).ToList();
					mMCPackInfo = new MMCPackInfo();
					string value = null;
					JsonObject jsonObject3 = null;
					string text = null;
					List<int> javaMajorsSet = null;
					JsonArray jsonArray = null;
					JsonArray jsonArray2 = new JsonArray();
					JsonArray jsonArray3 = new JsonArray();
					JsonArray jsonArray4 = new JsonArray();
					new JsonArray();
					foreach (KeyValuePair<JsonObject, int> item in list)
					{
						JsonObject key = item.Key;
						if ((string?)key["uid"] == "net.minecraft")
						{
							mMCPackInfo.isMinecraftOverrided = true;
						}
						else if ((string?)key["uid"] == "net.minecraftforge")
						{
							if (key["version"].ToString().StartsWithF("0."))
							{
								mMCPackInfo.isCleanroomOverrided = true;
							}
							else
							{
								mMCPackInfo.isForgeOverrided = true;
							}
						}
						else if ((string?)key["uid"] == "net.neoforged")
						{
							mMCPackInfo.isNeoForgeOverrided = true;
						}
						else if ((string?)key["uid"] == "net.fabricmc.fabric-loader")
						{
							mMCPackInfo.isFabricOverrided = true;
						}
						else if ((string?)key["uid"] == "org.quiltmc.quilt-loader")
						{
							throw new Exception(Lang.Text("Minecraft.Download.Modpack.QuiltUnsupported"));
						}
						if (key["+jvmArgs"] != null)
						{
							JsonCompat.Merge(jsonArray3, key["+jvmArgs"]);
							ModBase.Log($"[ModPack] 已应用 JSON-Patch {key["uid"]} 的 JVM 参数");
						}
						if (key["libraries"] != null || key["+libraries"] != null)
						{
							JsonArray jsonArray5 = new JsonArray();
							if (key["libraries"] != null)
							{
								foreach (JsonNode item2 in key["libraries"].AsArray())
								{
									if (item2 is JsonObject jsonObject4)
									{
										JsonObject jsonObject5 = jsonObject4.DeepClone().AsObject();
										if (jsonObject5["MMC-hint"] != null)
										{
											jsonObject5.Add("hint", jsonObject5["MMC-hint"]?.DeepClone());
											jsonObject5.Remove("MMC-hint");
										}
										jsonArray5.Add(jsonObject5);
									}
								}
							}
							if (key["+libraries"] != null)
							{
								foreach (JsonNode item3 in key["+libraries"].AsArray())
								{
									if (item3 is JsonObject jsonObject6)
									{
										JsonObject jsonObject7 = jsonObject6.DeepClone().AsObject();
										if (jsonObject7["MMC-hint"] != null)
										{
											jsonObject7.Add("hint", jsonObject7["MMC-hint"]?.DeepClone());
											jsonObject7.Remove("MMC-hint");
										}
										jsonArray5.Add(jsonObject7);
									}
								}
							}
							JsonCompat.Merge(jsonArray4, (JsonNode)jsonArray5);
							ModBase.Log($"[ModPack] 已应用 JSON-Patch {key["uid"]} 的 Libraries");
						}
						if (key["+tweakers"] != null)
						{
							value = (string?)key["+tweakers"][0];
							ModBase.Log($"[ModPack] 已应用 JSON-Patch {key["uid"]} 的 Tweakers");
						}
						if (key["assetIndex"] != null)
						{
							jsonObject3 = key["assetIndex"]?.DeepClone().AsObject();
							ModBase.Log($"[ModPack] 已应用 JSON-Patch {key["uid"]} 的 AssetIndex");
						}
						if (key["minecraftArguments"] != null)
						{
							string[] array = key["minecraftArguments"].ToString().Split(" ");
							foreach (string value2 in array)
							{
								jsonArray2.Add(value2);
							}
							mMCPackInfo.isMcArgsEdited = true;
							ModBase.Log($"[ModPack] 已应用 JSON-Patch {key["uid"]} 的 minecraftArguments 至 arguments.game");
						}
						if (key["mainClass"] != null)
						{
							text = (string?)key["mainClass"];
							ModBase.Log($"[ModPack] 已应用 JSON-Patch {key["uid"]} 的 mainClass");
						}
						if (!(key["compatibleJavaMajors"] is JsonArray { Count: >0 } jsonArray6))
						{
							continue;
						}
						List<int> list2 = (from major in (from Java in jsonArray6
								select (int)Math.Round(ModBase.Val(Java?.ToString())) into major
								where major > 0
								select major).Distinct()
							orderby major
							select major).ToList();
						if (list2.Count <= 0)
						{
							continue;
						}
						if (javaMajorsSet != null)
						{
							List<int> list3 = list2.Where((int major) => javaMajorsSet.Contains(major)).ToList();
							if (list3.Count == 0)
							{
								ModBase.Log($"[ModPack] JSON-Patch {key["uid"]} 要求的 Java 主版本（{string.Join(", ", list2)}）与此前的要求（{string.Join(", ", javaMajorsSet)}）无交集，已使用当前补丁的要求");
								javaMajorsSet = list2;
							}
							else
							{
								javaMajorsSet = list3;
							}
						}
						else
						{
							javaMajorsSet = list2;
						}
						ModBase.Log($"[ModPack] JSON-Patch {key["uid"]} 兼容的 Java 主版本：" + string.Join(", ", list2));
					}
					JsonObject jsonObject8 = null;
					if (javaMajorsSet != null && javaMajorsSet.Count > 0)
					{
						int num2 = javaMajorsSet.Min();
						string text2 = num2 switch
						{
							8 => "jre-legacy", 
							17 => "java-runtime-gamma", 
							21 => "java-runtime-delta", 
							25 => "java-runtime-epsilon", 
							_ => null, 
						};
						jsonObject8 = new JsonObject { 
						{
							"majorVersion",
							(JsonNode)num2
						} };
						if (text2 != null)
						{
							jsonObject8.Add("component", text2);
						}
						jsonArray = new JsonArray();
						foreach (int item4 in javaMajorsSet)
						{
							jsonArray.Add(item4);
						}
						ModBase.Log("[ModPack] 整合包要求的 Java 主版本集合：" + string.Join(", ", javaMajorsSet));
					}
					JsonObject jsonObject9 = null;
					if (!string.IsNullOrWhiteSpace(value))
					{
						jsonArray2.Add("--tweakClass");
						jsonArray2.Add(value);
					}
					if (jsonArray2 != null || jsonArray3 != null)
					{
						jsonArray3.Insert(0, "-Djava.library.path=${natives_directory}");
						jsonArray3.Insert(1, "-Dminecraft.launcher.brand=${launcher_name}");
						jsonArray3.Insert(2, "-Dminecraft.launcher.version=${launcher_version}");
						jsonArray3.Insert(3, "-cp");
						jsonArray3.Insert(4, "${classpath}");
						jsonObject9 = new JsonObject
						{
							{ "game", jsonArray2 },
							{ "jvm", jsonArray3 }
						};
					}
					mMCPackInfo.overridedJson = new JsonObject();
					if (jsonObject9 != null)
					{
						mMCPackInfo.overridedJson.Add("arguments", jsonObject9);
					}
					if (text != null)
					{
						mMCPackInfo.overridedJson.Add("mainClass", text);
					}
					if (jsonObject3 != null)
					{
						mMCPackInfo.overridedJson.Add("assetIndex", jsonObject3);
					}
					if (jsonObject8 != null)
					{
						mMCPackInfo.overridedJson.Add("javaVersion", jsonObject8);
					}
					if (jsonArray != null)
					{
						mMCPackInfo.overridedJson.Add("compatibleJavaMajors", jsonArray);
					}
					if (jsonArray4 != null)
					{
						mMCPackInfo.overridedJson.Add("libraries", jsonArray4);
					}
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("应用 MMC JSON-Patches 失败", innerException);
			}
		}
		catch (Exception innerException2)
		{
			throw new Exception("MMC 整合包安装信息存在问题", innerException2);
		}
		string instanceName = str.RegexSeek("(?<=\\nname\\=)[^\\n]+") ?? "";
		FolderNameValidator val = new FolderNameValidator(Path.Combine(ModFolder.mcFolderSelected, "versions"), true, true, false);
		if (!((AbstractValidator<string>)(object)val).Validate(instanceName).IsValid)
		{
			instanceName = "";
		}
		if (string.IsNullOrEmpty(instanceName))
		{
			instanceName = ModMain.MyMsgBoxInput(Lang.Text("Minecraft.Download.Modpack.InputInstanceName"), "", "", new Collection<IValidator<string>> { (IValidator<string>)(object)val });
		}
		if (string.IsNullOrEmpty(instanceName))
		{
			throw new ModBase.CancelledException();
		}
		string installTemp = ModMain.RequestTaskTempFolder();
		string versionFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\";
		List<ModLoader.LoaderBase> list4 = new List<ModLoader.LoaderBase>();
		list4.Add(new ModLoader.LoaderTask<string, int>(Lang.Text("Minecraft.Download.Modpack.Stage.ExtractModpack"), (ModLoader.LoaderTask<string, int> task) =>
		{
			//IL_045d: Unknown result type (might be due to invalid IL or missing references)
			ExtractModpackFiles(installTemp, fileAddress, task, 0.55);
			CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, "libraries"), Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName, "libraries"), task, 0.2);
			CopyOverrideDirectory(Path.Combine(installTemp, archiveBaseFolder, ".minecraft"), Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName), task, 0.2);
			try
			{
				string text3 = Path.Combine(installTemp, archiveBaseFolder, "instance.cfg");
				if (File.Exists(text3))
				{
					List<string> list7 = new List<string>();
					string[] array2 = ModBase.ReadFile(text3).Split(new string[2] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string text4 in array2)
					{
						if (text4.Contains("="))
						{
							list7.Add(text4.BeforeFirst("=") + ":" + text4.AfterFirst("="));
						}
					}
					ModBase.WriteFile(text3, list7.Join("\r\n"));
					if (Convert.ToBoolean(ModBase.ReadIni(text3, "OverrideCommands", false.ToString())))
					{
						string text5 = ModBase.ReadIni(text3, "PreLaunchCommand");
						if (!string.IsNullOrEmpty(text5))
						{
							text5 = text5.Replace("\\\"", "\"").Replace("$INST_JAVA", "{java}java.exe").Replace("$INST_MC_DIR\\", "{minecraft}")
								.Replace("$INST_MC_DIR", "{minecraft}")
								.Replace("$INST_DIR\\", "{verpath}")
								.Replace("$INST_DIR", "{verpath}")
								.Replace("$INST_ID", "{name}")
								.Replace("$INST_NAME", "{name}");
							((ParameterizedProperty<object, string>)(object)Config.Instance.PreLaunchCommand)[(object)versionFolder] = text5;
							ModBase.Log("[ModPack] 迁移 MultiMC 实例独立设置：启动前执行命令：" + text5);
						}
					}
					if (Convert.ToBoolean(ModBase.ReadIni(text3, "JoinServerOnLaunch", false.ToString())))
					{
						string text6 = ModBase.ReadIni(text3, "JoinServerOnLaunchAddress").Replace("\\\"", "\"");
						((ParameterizedProperty<object, string>)(object)Config.Instance.ServerToEnter)[(object)versionFolder] = text6;
						ModBase.Log("[ModPack] 迁移 MultiMC 实例独立设置：自动进入服务器：" + text6);
					}
					if (Convert.ToBoolean(ModBase.ReadIni(text3, "IgnoreJavaCompatibility", false.ToString())))
					{
						((ParameterizedProperty<object, bool>)(object)Config.Instance.IgnoreJavaCompatibility)[(object)versionFolder] = true;
						ModBase.Log("[ModPack] 迁移 MultiMC 实例独立设置：忽略 Java 兼容性警告");
					}
					string fileName = Path.GetFileName(ModBase.ReadIni(text3, "iconKey"));
					if (!string.IsNullOrEmpty(fileName) && File.Exists(installTemp + archiveBaseFolder + fileName + ".png"))
					{
						((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)versionFolder] = true;
						((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)versionFolder] = "PCL\\Logo.png";
						ModBase.CopyFile(installTemp + archiveBaseFolder + fileName + ".png", ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\PCL\\Logo.png");
						ModBase.Log("[ModPack] 迁移 MultiMC 实例独立设置：实例图标（" + fileName + ".png）");
					}
					string text7 = ModBase.ReadIni(text3, "JvmArgs");
					if (!string.IsNullOrEmpty(text7))
					{
						if (Convert.ToBoolean(ModBase.ReadIni(text3, "OverrideJavaArgs", false.ToString())))
						{
							((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)versionFolder] = text7;
							ModBase.Log("[ModPack] 迁移 MultiMC 实例独立设置：JVM 参数（覆盖）：" + text7);
						}
						else
						{
							text7 = text7 + " " + Config.Launch.JvmArgs;
							((ParameterizedProperty<object, string>)(object)Config.Instance.JvmArgs)[(object)versionFolder] = text7;
							ModBase.Log("[ModPack] 迁移 MultiMC 实例独立设置：JVM 参数（追加）：" + text7);
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "读取 MMC 配置文件失败（" + installTemp + archiveBaseFolder + "instance.cfg）");
			}
			((DynamicCacheConfigStorage)ConfigService.GetProvider((ConfigSource)3)).InvalidateCache((object)versionFolder);
		})
		{
			ProgressWeight = (double)new FileInfo(fileAddress).Length / 1024.0 / 1024.0 / 6.0,
			block = false
		});
		if (jsonObject["components"] == null)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Modpack.MissingGameVersion.Generic"));
		}
		ModDownloadLib.McInstallRequest mcInstallRequest = new ModDownloadLib.McInstallRequest
		{
			targetInstanceName = instanceName,
			targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + instanceName + "\\"
		};
		foreach (JsonNode item5 in jsonObject["components"].AsArray())
		{
			switch ((item5["uid"] ?? ((JsonNode?)"")).ToString() ?? "")
			{
			case "org.lwjgl":
				ModBase.Log("[ModPack] 已跳过 LWJGL 项");
				break;
			case "net.minecraft":
				mcInstallRequest.minecraftName = (string?)item5["version"];
				break;
			case "net.minecraftforge":
				if (item5["version"].ToString().StartsWithF("0."))
				{
					mcInstallRequest.cleanroomVersion = (string?)item5["version"];
				}
				else
				{
					mcInstallRequest.forgeVersion = (string?)item5["version"];
				}
				break;
			case "net.neoforged":
				mcInstallRequest.neoForgeVersion = (string?)item5["version"];
				break;
			case "net.fabricmc.fabric-loader":
				mcInstallRequest.fabricVersion = (string?)item5["version"];
				break;
			case "org.quiltmc.quilt-loader":
				throw new Exception(Lang.Text("Minecraft.Download.Modpack.QuiltUnsupported"));
			}
		}
		if (mMCPackInfo != null)
		{
			mcInstallRequest.mmcPackInfo = mMCPackInfo;
		}
		List<ModLoader.LoaderBase> list5 = ModDownloadLib.McInstallLoader(mcInstallRequest);
		List<ModLoader.LoaderBase> list6 = new List<ModLoader.LoaderBase>();
		list6.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.ModpackInstall"), list4)
		{
			show = false,
			block = false,
			ProgressWeight = list4.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		list6.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Modpack.Stage.GameInstall"), list5)
		{
			show = false,
			ProgressWeight = list5.Sum((ModLoader.LoaderBase l) => l.ProgressWeight)
		});
		string loaderName = Lang.Text("Minecraft.Download.Modpack.Task.MmcInstall", new object[1] { instanceName });
		if (ModLoader.loaderTaskbar.Any((ModLoader.LoaderBase l) => (l.name ?? "") == (loaderName ?? "")))
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Modpack.Installing"), HintType.Error);
			throw new ModBase.CancelledException();
		}
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(loaderName, list6);
		loaderCombo.OnStateChanged = ModDownloadLib.McInstallState;
		loaderCombo.Start(mcInstallRequest.targetInstanceFolder);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModBase.RunInUi(() =>
		{
			ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
		});
		return loaderCombo;
	}
}
