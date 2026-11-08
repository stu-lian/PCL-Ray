using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Humanizer;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public static class ModDownload
{
	public enum AssetsIndexExistsBehaviour
	{
		DontDownload,
		DownloadInBackground,
		AlwaysDownload
	}

	public struct DlClientListResult
	{
		public string SourceName;

		public bool IsOfficial;

		public JsonObject Value;
	}

	public struct DlOptiFineListResult
	{
		public string sourceName;

		public bool isOfficial;

		public List<DlOptiFineListEntry> Value;
	}

	public class DlOptiFineListEntry
	{
		public string DisplayName;

		public bool IsPreview;

		public string NameFile;

		public string NameVersion;

		public string ReleaseTime;

		public string RequiredForgeVersion;

		public string Inherit
		{
			get
			{
				return field;
			}
			set
			{
				if (value.EndsWithF(".0"))
				{
					value = value.Substring(0, value.Length - 2);
				}
				field = value;
			}
		}
	}

	public struct DlForgeListResult
	{
		public string sourceName;

		public bool isOfficial;

		public List<string> Value;
	}

	public abstract class DlForgelikeEntry : IComparable<DlForgelikeEntry>
	{
		public enum ForgelikeType
		{
			Forge,
			NeoForge,
			Cleanroom
		}

		public ForgelikeType forgeType;

		public string Inherit;

		public Version version;

		public string VersionName;

		public string LoaderName => forgeType.ToString();

		public string FileExtension
		{
			get
			{
				if (forgeType == ForgelikeType.Forge)
				{
					if (!(((DlForgeVersionEntry)this).Category == "installer"))
					{
						return "zip";
					}
					return "jar";
				}
				return "jar";
			}
		}

		public bool IsLegacy
		{
			get
			{
				if (forgeType == ForgelikeType.Cleanroom)
				{
					return false;
				}
				return version.Major < 20;
			}
		}

		public int CompareTo(DlForgelikeEntry other)
		{
			if (version != other.version)
			{
				return version.CompareTo(other.version);
			}
			return McVersionComparer.CompareVersion(VersionName, other.VersionName);
		}
	}

	public class DlForgeVersionEntry : DlForgelikeEntry
	{
		public string Category;

		public string FileVersion;

		public string Hash;

		public bool IsRecommended;

		public string ReleaseTime;

		public DlForgeVersionEntry(string version, string branch, string inherit)
		{
			switch (version)
			{
			case "11.15.1.2318":
			case "11.15.1.1902":
			case "11.15.1.1890":
				branch = "1.8.9";
				break;
			}
			if (branch == null && inherit == "1.7.10" && double.Parse(version.Split(".")[3]) >= 1300.0)
			{
				branch = "1.7.10";
			}
			forgeType = ForgelikeType.Forge;
			VersionName = version;
			base.version = new Version(version);
			Inherit = inherit;
			FileVersion = version + ((branch == null) ? "" : ("-" + branch));
		}
	}

	public struct DlNeoForgeListResult
	{
		public string sourceName;

		public bool isOfficial;

		public List<DlNeoForgeListEntry> Value;
	}

	public class DlNeoForgeListEntry : DlForgelikeEntry
	{
		public string ApiName;

		public bool IsBeta;

		public string UrlBase
		{
			get
			{
				string value = (IsLegacy ? "forge" : "neoforge");
				return $"https://maven.neoforged.net/releases/net/neoforged/{value}/{ApiName}/{value}-{ApiName}";
			}
		}

		public DlNeoForgeListEntry(string apiName)
		{
			forgeType = ForgelikeType.NeoForge;
			ApiName = apiName;
			IsBeta = apiName.Contains("beta") || apiName.Contains("alpha");
			if (apiName.Contains("1.20.1"))
			{
				VersionName = apiName.Replace("1.20.1-", "");
				version = new Version("19." + VersionName);
				Inherit = "1.20.1";
				return;
			}
			if (apiName.StartsWith("0."))
			{
				VersionName = apiName;
				string[] array = apiName.BeforeFirst("-").Split('.');
				version = new Version(0, 0, int.Parse(array.Last()));
				Inherit = array[1];
				return;
			}
			VersionName = apiName;
			version = new Version(apiName.BeforeFirst("-"));
			if (version.Major >= 24)
			{
				Inherit = $"{version.Major}.{version.Minor}{((version.Build > 0) ? $".{version.Build}" : "")}";
			}
			else
			{
				Inherit = "1." + version.Major + ((version.Minor > 0) ? ("." + version.Minor) : "");
			}
			if (VersionName.Contains("+"))
			{
				Inherit = Inherit + "-" + VersionName.AfterFirst("+");
			}
		}
	}

	public struct DlCleanroomListResult
	{
		public string sourceName;

		public bool isOfficial;

		public List<DlCleanroomListEntry> Value;
	}

	public class DlCleanroomListEntry : DlForgelikeEntry
	{
		public string ApiName;

		public bool IsBeta;

		public string UrlBase => "https://github.com/CleanroomMC/Cleanroom/releases/download/" + ApiName + "/cleanroom-" + ApiName;

		public DlCleanroomListEntry(string apiName)
		{
			forgeType = ForgelikeType.Cleanroom;
			ApiName = apiName;
			IsBeta = apiName.Contains("alpha");
			VersionName = apiName;
			version = new Version(apiName.BeforeFirst("-"));
			Inherit = "1.12.2";
		}
	}

	public struct DlLiteLoaderListResult
	{
		public string sourceName;

		public bool isOfficial;

		public List<DlLiteLoaderListEntry> Value;

		public Exception officialError;
	}

	public class DlLiteLoaderListEntry
	{
		public string FileName;

		public string Inherit;

		public bool IsLegacy;

		public bool IsPreview;

		public JsonNode jsonToken;

		public string MD5;

		public string ReleaseTime;
	}

	public struct DlFabricListResult
	{
		public string sourceName;

		public bool isOfficial;

		public JsonObject Value;
	}

	public struct DlLabyModListResult
	{
		public JsonObject Value;
	}

	public struct DlLegacyFabricListResult
	{
		public string sourceName;

		public bool isOfficial;

		public JsonObject Value;
	}

	private static readonly object _allDropsLock = new object();

	public static ModLoader.LoaderTask<string, DlClientListResult> dlClientListLoader = new ModLoader.LoaderTask<string, DlClientListResult>("DlClientList Main", DlClientListMain);

	public static ModLoader.LoaderTask<string, DlClientListResult> dlClientListMojangLoader = new ModLoader.LoaderTask<string, DlClientListResult>("DlClientList Mojang", DlClientListMojangMain);

	private static bool isNewClientVersionHinted = false;

	private static bool _DlClientListMojangMain_IsHinted;

	public static ModLoader.LoaderTask<string, DlClientListResult> dlClientListBmclapiLoader = new ModLoader.LoaderTask<string, DlClientListResult>("DlClientList Bmclapi", DlClientListBmclapiMain);

	public static ModLoader.LoaderTask<int, DlOptiFineListResult> dlOptiFineListLoader = new ModLoader.LoaderTask<int, DlOptiFineListResult>("DlOptiFineList Main", DlOptiFineListMain);

	public static ModLoader.LoaderTask<int, DlOptiFineListResult> dlOptiFineListOfficialLoader = new ModLoader.LoaderTask<int, DlOptiFineListResult>("DlOptiFineList Official", DlOptiFineListOfficialMain);

	public static ModLoader.LoaderTask<int, DlOptiFineListResult> dlOptiFineListBmclapiLoader = new ModLoader.LoaderTask<int, DlOptiFineListResult>("DlOptiFineList Bmclapi", DlOptiFineListBmclapiMain);

	public static ModLoader.LoaderTask<int, DlForgeListResult> dlForgeListLoader = new ModLoader.LoaderTask<int, DlForgeListResult>("DlForgeList Main", DlForgeListMain);

	public static ModLoader.LoaderTask<int, DlForgeListResult> dlForgeListOfficialLoader = new ModLoader.LoaderTask<int, DlForgeListResult>("DlForgeList Official", DlForgeListOfficialMain);

	public static ModLoader.LoaderTask<int, DlForgeListResult> dlForgeListBmclapiLoader = new ModLoader.LoaderTask<int, DlForgeListResult>("DlForgeList Bmclapi", DlForgeListBmclapiMain);

	public static ModLoader.LoaderTask<int, DlNeoForgeListResult> dlNeoForgeListLoader = new ModLoader.LoaderTask<int, DlNeoForgeListResult>("DlNeoForgeList Main", DlNeoForgeListMain);

	public static ModLoader.LoaderTask<int, DlNeoForgeListResult> dlNeoForgeListOfficialLoader = new ModLoader.LoaderTask<int, DlNeoForgeListResult>("DlNeoForgeList Official", DlNeoForgeListOfficialMain);

	public static ModLoader.LoaderTask<int, DlNeoForgeListResult> dlNeoForgeListBmclapiLoader = new ModLoader.LoaderTask<int, DlNeoForgeListResult>("DlNeoForgeList Bmclapi", DlNeoForgeListBmclapiMain);

	public static ModLoader.LoaderTask<int, DlCleanroomListResult> dlCleanroomListLoader = new ModLoader.LoaderTask<int, DlCleanroomListResult>("DlCleanroomList Main", DlCleanroomListMain);

	public static ModLoader.LoaderTask<int, DlCleanroomListResult> dlCleanroomListOfficialLoader = new ModLoader.LoaderTask<int, DlCleanroomListResult>("DlCleanroomList Official", DlCleanroomListOfficialMain);

	public static ModLoader.LoaderTask<int, DlLiteLoaderListResult> dlLiteLoaderListLoader = new ModLoader.LoaderTask<int, DlLiteLoaderListResult>("DlLiteLoaderList Main", DlLiteLoaderListMain);

	public static ModLoader.LoaderTask<int, DlLiteLoaderListResult> dlLiteLoaderListOfficialLoader = new ModLoader.LoaderTask<int, DlLiteLoaderListResult>("DlLiteLoaderList Official", DlLiteLoaderListOfficialMain);

	public static ModLoader.LoaderTask<int, DlLiteLoaderListResult> dlLiteLoaderListBmclapiLoader = new ModLoader.LoaderTask<int, DlLiteLoaderListResult>("DlLiteLoaderList Bmclapi", DlLiteLoaderListBmclapiMain);

	public static ModLoader.LoaderTask<int, DlFabricListResult> dlFabricListLoader = new ModLoader.LoaderTask<int, DlFabricListResult>("DlFabricList Main", DlFabricListMain);

	public static ModLoader.LoaderTask<int, DlFabricListResult> dlFabricListOfficialLoader = new ModLoader.LoaderTask<int, DlFabricListResult>("DlFabricList Official", DlFabricListOfficialMain);

	public static ModLoader.LoaderTask<int, DlFabricListResult> dlFabricListBmclapiLoader = new ModLoader.LoaderTask<int, DlFabricListResult>("DlFabricList Bmclapi", DlFabricListBmclapiMain);

	public static ModLoader.LoaderTask<int, List<ModComp.CompFile>> dlFabricApiLoader = new ModLoader.LoaderTask<int, List<ModComp.CompFile>>("Fabric API List Loader", (ModLoader.LoaderTask<int, List<ModComp.CompFile>> task) =>
	{
		task.output = ModComp.CompFilesGet("fabric-api", fromCurseForge: false);
	});

	public static ModLoader.LoaderTask<int, List<ModComp.CompFile>> dlOptiFabricLoader = new ModLoader.LoaderTask<int, List<ModComp.CompFile>>("OptiFabric List Loader", (ModLoader.LoaderTask<int, List<ModComp.CompFile>> task) =>
	{
		task.output = ModComp.CompFilesGet("322385", fromCurseForge: true);
	});

	public static ModLoader.LoaderTask<int, DlLabyModListResult> dlLabyModListLoader = new ModLoader.LoaderTask<int, DlLabyModListResult>("DlLabyModList Main", DlLabyModListMain);

	public static ModLoader.LoaderTask<int, DlLabyModListResult> dlLabyModListOfficialLoader = new ModLoader.LoaderTask<int, DlLabyModListResult>("DlLabyModList Official", DlLabyModListOfficialMain);

	private static bool dlPreferMojang;

	public static ModLoader.LoaderTask<int, DlLegacyFabricListResult> dlLegacyFabricListLoader = new ModLoader.LoaderTask<int, DlLegacyFabricListResult>("DlLegacyFabricList Main", DlLegacyFabricListMain);

	public static ModLoader.LoaderTask<int, DlLegacyFabricListResult> dlLegacyFabricListOfficialLoader = new ModLoader.LoaderTask<int, DlLegacyFabricListResult>("DlLegacyFabricList Official", DlLegacyFabricListOfficialMain);

	public static ModLoader.LoaderTask<int, List<ModComp.CompFile>> dlLegacyFabricApiLoader = new ModLoader.LoaderTask<int, List<ModComp.CompFile>>("Legacy Fabric API List Loader", (ModLoader.LoaderTask<int, List<ModComp.CompFile>> task) =>
	{
		task.output = ModComp.CompFilesGet("legacy-fabric-api", fromCurseForge: false);
	});

	public static List<int> AllDrops
	{
		get
		{
			lock (_allDropsLock)
			{
				if (field == null)
				{
					string drops = States.Game.Drops;
					if (string.IsNullOrEmpty(drops))
					{
						field = new List<int>();
					}
					else
					{
						field = (from d in drops.Split(new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries)
							select (int)Math.Round(ModBase.Val(d))).ToList();
					}
				}
				return (field.Count != 0) ? field : null;
			}
		}
		set
		{
			lock (_allDropsLock)
			{
				field = value;
				States.Game.Drops = value.Join(",");
			}
		}
	}

	public static bool DlSourcePreferMojang
	{
		get
		{
			if (Config.Download.FileSource != 2)
			{
				if (Config.Download.FileSource == 1)
				{
					return dlPreferMojang;
				}
				return false;
			}
			return true;
		}
	}

	public static bool DlVersionListPreferMojang
	{
		get
		{
			if (Config.Download.VersionListSource != 2)
			{
				if (Config.Download.VersionListSource == 1)
				{
					return dlPreferMojang;
				}
				return false;
			}
			return true;
		}
	}

	public static DownloadFile DlClientJarGet(McInstance version, bool returnNothingOnFileUseable)
	{
		try
		{
			while (!string.IsNullOrEmpty(version.InheritInstanceName))
			{
				version = new McInstance(version.InheritInstanceName);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取底层继承实例失败");
		}
		if (version.JsonObject["downloads"] == null || version.JsonObject["downloads"]["client"] == null || version.JsonObject["downloads"]["client"]["url"] == null)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.NoJarDownloadInfo", new object[1] { version.Name }));
		}
		ModBase.FileChecker fileChecker = new ModBase.FileChecker(1024L, (long)(version.JsonObject["downloads"]["client"]["size"] ?? ((JsonNode)(-1))), (string?)version.JsonObject["downloads"]["client"]["sha1"]);
		if (returnNothingOnFileUseable && fileChecker.Check(version.PathInstance + version.Name + ".jar") == null)
		{
			return null;
		}
		return new DownloadFile(DlSourceLauncherOrMetaGet((string?)version.JsonObject["downloads"]["client"]["url"]), version.PathInstance + version.Name + ".jar", fileChecker);
	}

	public static DownloadFile DlClientAssetIndexGet(McInstance version)
	{
		while (!string.IsNullOrEmpty(version.InheritInstanceName))
		{
			version = new McInstance(version.InheritInstanceName);
		}
		JsonNode jsonNode = ModAssets.McAssetsGetIndex(version, returnLegacyOnError: true, checkURLEmpty: true);
		string localPath = Path.Combine(ModFolder.mcFolderSelected, "assets", "indexes", jsonNode["id"]?.ToString() + ".json");
		ModBase.Log("[Download] 实例 " + version.Name + " 对应的资源文件索引为 " + jsonNode["id"]);
		string text = (string?)(jsonNode["url"] ?? ((JsonNode?)""));
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		long actualSize = ((jsonNode["size"] == null) ? (-1) : ((long)jsonNode["size"]));
		return new DownloadFile(DlSourceLauncherOrMetaGet(text), localPath, new ModBase.FileChecker(-1L, actualSize, null, canUseExistsFile: false));
	}

	public static List<ModLoader.LoaderBase> DlClientFix(McInstance version, bool checkAssetsHash, AssetsIndexExistsBehaviour assetsIndexBehaviour)
	{
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		if (ModLibrary.ShouldIgnoreFileCheck(version))
		{
			ModBase.Log("[Download] 已跳过所有 Libraries 检查");
		}
		else
		{
			List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>
			{
				new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeMissingLibraries"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
				{
					task.output = ModLibrary.McLibNetFilesFromInstance(version);
				})
				{
					ProgressWeight = 1.0
				},
				new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLibraries"), new List<DownloadFile>())
				{
					ProgressWeight = 15.0
				}
			};
			list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.DownloadLibraries.MainLoader"), loaders)
			{
				block = false,
				show = false,
				ProgressWeight = 16.0
			});
		}
		if (ModLibrary.ShouldIgnoreFileCheck(version))
		{
			ModBase.Log("[Download] 已跳过所有 Assets 检查");
		}
		else
		{
			List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
			list2.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeAssetsIndex"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				try
				{
					DownloadFile downloadFile = DlClientAssetIndexGet(version);
					new FileInfo(downloadFile.LocalPath);
					if (assetsIndexBehaviour != AssetsIndexExistsBehaviour.AlwaysDownload && downloadFile.Check.Check(downloadFile.LocalPath) == null)
					{
						task.output = new List<DownloadFile>();
					}
					else
					{
						task.output = new List<DownloadFile> { downloadFile };
					}
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.AssetIndexAnalysisFailed"), innerException);
				}
			})
			{
				ProgressWeight = 0.5,
				show = false
			});
			list2.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssetsIndex"), new List<DownloadFile>())
			{
				ProgressWeight = 2.0
			});
			if (assetsIndexBehaviour == AssetsIndexExistsBehaviour.DownloadInBackground)
			{
				List<ModLoader.LoaderBase> list3 = new List<ModLoader.LoaderBase>();
				string tempAddress = null;
				string realAddress = null;
				list3.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeAssetsIndex.Background"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
				{
					DownloadFile downloadFile = DlClientAssetIndexGet(version);
					realAddress = downloadFile.LocalPath;
					tempAddress = ModBase.pathTemp + "Cache\\" + downloadFile.LocalName;
					downloadFile.LocalPath = tempAddress;
					task.output = new List<DownloadFile> { downloadFile };
					if (File.Exists(realAddress) && Math.Abs((File.GetLastWriteTime(realAddress).Date - DateTime.Now.Date).TotalDays) < 1.0)
					{
						ModBase.Log("[Download] 无需更新资源文件索引，取消");
						task.Abort();
					}
				}));
				list3.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssetsIndex.Background"), new List<DownloadFile>()));
				list3.Add(new ModLoader.LoaderTask<List<DownloadFile>, string>(Lang.Text("Minecraft.Download.Stage.CopyAssetsIndex.Background"), (ModLoader.LoaderTask<List<DownloadFile>, string> task) =>
				{
					ModBase.CopyFile(tempAddress, realAddress);
					ModLaunch.McLaunchLog("后台更新资源文件索引成功：" + tempAddress);
				}));
				ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.UpdateAssetsIndex.Background"), list3);
				ModBase.Log("[Download] 开始后台检查资源文件索引");
				loaderCombo.Start();
			}
			list2.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeMissingAssets"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				ModLoader.LoaderBase progressFeed = task;
				task.output = ModAssets.McAssetsFixList(version, checkAssetsHash, ref progressFeed);
				task = (ModLoader.LoaderTask<string, List<DownloadFile>>)progressFeed;
			})
			{
				ProgressWeight = 3.0
			});
			list2.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssets"), new List<DownloadFile>())
			{
				ProgressWeight = 25.0
			});
			list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.DownloadAssets.MainLoader"), list2)
			{
				block = false,
				show = false,
				ProgressWeight = 30.5
			});
		}
		return list;
	}

	public static bool McClientFilesAvailableLocally(McInstance instance)
	{
		try
		{
			if (!instance.IsLoaded)
			{
				instance.Load();
			}
			if (!File.Exists(Path.Combine(instance.PathInstance, instance.Name + ".json")))
			{
				return false;
			}
			foreach (ModLibrary.McLibToken item in ModLibrary.McLibListGet(instance, includeInstanceJar: true))
			{
				if (!item.IsLocal && new ModBase.FileChecker(1L, (item.size > 0) ? item.size : (-1), item.Sha1).Check(item.LocalPath) != null)
				{
					return false;
				}
			}
			return ModAssets.McAssetsListGet(instance).All((ModAssets.McAssetsToken asset) => File.Exists(asset.localPath) && new FileInfo(asset.localPath).Length > 0 && (asset.size <= 0 || new FileInfo(asset.localPath).Length == asset.size));
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "本地检查 Minecraft 文件失败");
			return false;
		}
	}

	private static void DlClientListMain(ModLoader.LoaderTask<string, DlClientListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>(dlClientListBmclapiLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>(dlClientListMojangLoader, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>(dlClientListMojangLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>(dlClientListBmclapiLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>(dlClientListMojangLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<string, DlClientListResult>, int>(dlClientListBmclapiLoader, 120)
			}, loader.isForceRestarting);
			break;
		}
		List<int> list = new List<int>();
		foreach (JsonObject item in loader.output.Value["versions"].AsArray())
		{
			list.Add(McInstanceInfo.VersionToDrop((string?)item["id"]));
		}
		AllDrops = (from d in list.Distinct()
			orderby d descending
			select d).ToList();
	}

	private static void DlClientListMojangMain(ModLoader.LoaderTask<string, DlClientListResult> loader)
	{
		long timeTick = TimeUtils.GetTimeTick();
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://launchermeta.mojang.com/mc/game/version_manifest.json");
		try
		{
			JsonArray jsonArray = (JsonArray)jsonObject["versions"];
			if (jsonArray.Count < 200)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Mojang", jsonObject }));
			}
			string text = ModBase.pathTemp + "Cache\\uvmc-download.json";
			if (!File.Exists(text))
			{
				try
				{
					JsonObject jsonObject2 = (JsonObject)Requester.FetchJson("https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto/version_manifest.json");
					File.WriteAllText(text, jsonObject2.ToString());
				}
				catch (Exception ex)
				{
					ModBase.Log("[Download] 未列出的版本官方源下载失败: " + ex.Message);
				}
			}
			try
			{
				JsonObject jsonObject3 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(text));
				JsonCompat.Merge(jsonArray, jsonObject3["versions"]);
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[Download] UVMC 列表加载失败，忽略列表内容");
			}
			if (!dlPreferMojang)
			{
				long num = TimeUtils.GetTimeTick() - timeTick;
				dlPreferMojang = num < 4000;
				ModBase.Log($"[Download] Mojang 官方源加载耗时：{num}ms，{(dlPreferMojang ? "可优先使用官方源" : "不优先使用官方源")}");
			}
			loader.output = new DlClientListResult
			{
				IsOfficial = true,
				SourceName = Lang.Text("Download.Source.MojangOfficial"),
				Value = jsonObject
			};
			string text2 = (string?)jsonObject["latest"]["snapshot"];
			if (Config.Tool.SnapshotNotification && States.Tool.LastSnapshot != "" && States.Tool.LastSnapshot != text2 && !_DlClientListMojangMain_IsHinted)
			{
				_DlClientListMojangMain_IsHinted = true;
				McDownloadClientUpdateHint(text2, jsonObject);
			}
			States.Tool.LastSnapshot = text2 ?? "Nothing";
			text2 = (string?)jsonObject["latest"]["release"];
			if (Config.Tool.ReleaseNotification && States.Tool.LastRelease != "" && States.Tool.LastRelease != text2 && !_DlClientListMojangMain_IsHinted)
			{
				_DlClientListMojangMain_IsHinted = true;
				McDownloadClientUpdateHint(text2, jsonObject);
			}
			States.Tool.LastRelease = text2;
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Mojang", "" }), innerException);
		}
	}

	private static void DlClientListBmclapiMain(ModLoader.LoaderTask<string, DlClientListResult> loader)
	{
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://bmclapi2.bangbang93.com/mc/game/version_manifest.json");
		try
		{
			JsonArray jsonArray = (JsonArray)jsonObject["versions"];
			if (jsonArray.Count < 200)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "BMCLAPI", jsonObject }));
			}
			string text = ModBase.pathTemp + "Cache\\uvmc-download.json";
			if (!File.Exists(text))
			{
				try
				{
					JsonObject jsonObject2 = (JsonObject)Requester.FetchJson("https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto/version_manifest.json");
					File.WriteAllText(text, jsonObject2.ToString());
				}
				catch (Exception ex)
				{
					ModBase.Log("[Download] 未列出的版本镜像源下载失败: " + ex.Message);
				}
			}
			try
			{
				JsonObject jsonObject3 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(text));
				JsonCompat.Merge(jsonArray, jsonObject3["versions"]);
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[Download] UVMC 列表加载失败，忽略列表内容");
			}
			if (!string.IsNullOrEmpty(loader.input))
			{
				string id = loader.input;
				if (dlClientListLoader.output.Value != null && !dlClientListLoader.output.Value["versions"].AsArray().Any((JsonNode v) => (string?)v["id"] == id))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.BmclapiMissingTargetVersion", new object[1] { id }));
				}
			}
			loader.output = new DlClientListResult
			{
				IsOfficial = false,
				SourceName = "BMCLAPI",
				Value = jsonObject
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "BMCLAPI", jsonObject }), innerException);
		}
	}

	public static object DlClientListGet(string id)
	{
		try
		{
			id = id.Replace("_", "-");
			if (id != "1.0" && id.EndsWithF(".0"))
			{
				id = id.Substring(0, id.Length - 2);
			}
			switch (dlClientListLoader.State)
			{
			case ModBase.LoadState.Finished:
				foreach (JsonObject item in dlClientListLoader.output.Value["versions"].AsArray())
				{
					if ((string?)item["id"] == id)
					{
						return item["url"].ToString();
					}
				}
				dlClientListLoader.WaitForExit(id, null, isForceRestart: true);
				break;
			case ModBase.LoadState.Loading:
				dlClientListLoader.WaitForExit(id);
				break;
			case ModBase.LoadState.Waiting:
			case ModBase.LoadState.Failed:
			case ModBase.LoadState.Aborted:
				dlClientListLoader.WaitForExit(id, null, isForceRestart: true);
				break;
			}
			foreach (JsonObject item2 in dlClientListLoader.output.Value["versions"].AsArray())
			{
				if ((string?)item2["id"] == id)
				{
					return item2["url"].ToString();
				}
			}
			ModBase.Log($"未发现版本 {id} 的 json 下载地址，版本列表返回为：{"\r\n"}{dlClientListLoader.output.Value}", ModBase.LogLevel.Debug);
			return null;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取版本 " + id + " 的 json 下载地址失败");
			return null;
		}
	}

	private static void DlOptiFineListMain(ModLoader.LoaderTask<int, DlOptiFineListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>(dlOptiFineListBmclapiLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>(dlOptiFineListOfficialLoader, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>(dlOptiFineListOfficialLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>(dlOptiFineListBmclapiLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>(dlOptiFineListOfficialLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<int, DlOptiFineListResult>, int>(dlOptiFineListBmclapiLoader, 120)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlOptiFineListOfficialMain(ModLoader.LoaderTask<int, DlOptiFineListResult> loader)
	{
		string text = "";
		using HttpResponseMessage httpResponseMessage = HttpSenderExtension.SendAsync(HttpHeaderHandler.WithHeader(HttpHeaderHandler.WithHeader(HttpHeaderHandler.WithHeader(HttpRequest.Create("https://optifine.net/downloads"), "Accept", "application/json, text/javascript, */*; q=0.01"), "Accept-Language", "en-US,en;q=0.5"), "X-Requested-With", "XMLHttpRequest"), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult();
		httpResponseMessage.EnsureSuccessStatusCode();
		text = HttpResponseExtension.AsString(httpResponseMessage);
		if (text.Length < 200)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "OptiFine", text }));
		}
		try
		{
			List<string> list = text.RegexSearch("(?<=colForge'>)[^<]*");
			List<string> list2 = text.RegexSearch("(?<=colDate'>)[^<]+");
			List<string> list3 = text.RegexSearch("(?<=OptiFine_)[0-9A-Za-z_.]+(?=.jar\")");
			if (list2.Count != list3.Count)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.OptiFineTimeDataMismatch"));
			}
			if (list.Count != list3.Count)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.OptiFineForgeCompatMismatch"));
			}
			if (list2.Count < 10)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "OptiFine", text }));
			}
			List<DlOptiFineListEntry> list4 = new List<DlOptiFineListEntry>();
			int i = 0;
			for (int num = list2.Count - 1; i <= num; i++)
			{
				list3[i] = list3[i].Replace("_", " ");
				DateTime dateTime = DateTime.ParseExact(list2[i], new string[4] { "d.M.yyyy", "dd.M.yyyy", "d.MM.yyyy", "dd.MM.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None);
				DlOptiFineListEntry dlOptiFineListEntry = new DlOptiFineListEntry
				{
					DisplayName = list3[i].Replace("HD U ", "").Replace(".0 ", " "),
					ReleaseTime = Lang.Date(dateTime, "d"),
					IsPreview = list3[i].ContainsF("pre", ignoreCase: true),
					Inherit = list3[i].Split(" ")[0],
					NameFile = (list3[i].ContainsF("pre", ignoreCase: true) ? "preview_" : "") + "OptiFine_" + list3[i].Replace(" ", "_") + ".jar",
					RequiredForgeVersion = list[i].Replace("Forge ", "").Replace("#", "")
				};
				if (dlOptiFineListEntry.RequiredForgeVersion.Contains("N/A"))
				{
					dlOptiFineListEntry.RequiredForgeVersion = null;
				}
				dlOptiFineListEntry.NameVersion = dlOptiFineListEntry.Inherit + "-OptiFine_" + list3[i].Replace(" ", "_").Replace(dlOptiFineListEntry.Inherit + "_", "");
				list4.Add(dlOptiFineListEntry);
			}
			loader.output = new DlOptiFineListResult
			{
				isOfficial = true,
				sourceName = Lang.Text("Download.Source.OptiFineOfficial"),
				Value = list4
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "OptiFine", text }), innerException);
		}
	}

	private static void DlOptiFineListBmclapiMain(ModLoader.LoaderTask<int, DlOptiFineListResult> loader)
	{
		JsonArray jsonArray = (JsonArray)Requester.FetchJson("https://bmclapi2.bangbang93.com/optifine/versionList");
		try
		{
			List<DlOptiFineListEntry> list = new List<DlOptiFineListEntry>();
			foreach (JsonObject item in jsonArray)
			{
				DlOptiFineListEntry dlOptiFineListEntry = new DlOptiFineListEntry
				{
					DisplayName = (item["mcversion"]?.ToString() + item["type"].ToString().Replace("HD_U", "").Replace("_", " ") + " " + item["patch"]).Replace(".0 ", " "),
					ReleaseTime = "",
					IsPreview = item["patch"].ToString().ContainsF("pre", ignoreCase: true),
					Inherit = item["mcversion"].ToString(),
					NameFile = item["filename"].ToString(),
					RequiredForgeVersion = (item["forge"] ?? ((JsonNode?)"")).ToString().Replace("Forge ", "").Replace("#", "")
				};
				if (dlOptiFineListEntry.RequiredForgeVersion.Contains("N/A"))
				{
					dlOptiFineListEntry.RequiredForgeVersion = null;
				}
				dlOptiFineListEntry.NameVersion = dlOptiFineListEntry.Inherit + "-OptiFine_" + (item["type"]?.ToString() + " " + item["patch"]).Replace(".0 ", " ").Replace(" ", "_").Replace(dlOptiFineListEntry.Inherit + "_", "");
				list.Add(dlOptiFineListEntry);
			}
			loader.output = new DlOptiFineListResult
			{
				isOfficial = false,
				sourceName = "BMCLAPI",
				Value = list
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "OptiFine BMCLAPI", jsonArray }), innerException);
		}
	}

	private static void DlForgeListMain(ModLoader.LoaderTask<int, DlForgeListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>(dlForgeListBmclapiLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>(dlForgeListOfficialLoader, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>(dlForgeListOfficialLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>(dlForgeListBmclapiLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>(dlForgeListOfficialLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<int, DlForgeListResult>, int>(dlForgeListBmclapiLoader, 120)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlForgeListOfficialMain(ModLoader.LoaderTask<int, DlForgeListResult> loader)
	{
		string text = Requester.FetchString("https://files.minecraftforge.net/maven/net/minecraftforge/forge/index_1.2.4.html", new RequestParam
		{
			Encoding = Encoding.Default,
			UseBrowserUserAgent = true
		});
		if (text.Length < 200)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge", text }));
		}
		List<string> list = text.RegexSearch("(?<=a href=\"index_)[0-9.]+(_pre[0-9]?)?(?=.html)");
		list.Add("1.2.4");
		if (list.Count < 10)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge", text }));
		}
		loader.output = new DlForgeListResult
		{
			isOfficial = true,
			sourceName = Lang.Text("Download.Source.ForgeOfficial"),
			Value = list
		};
	}

	private static void DlForgeListBmclapiMain(ModLoader.LoaderTask<int, DlForgeListResult> loader)
	{
		string text = Requester.FetchJson("https://bmclapi2.bangbang93.com/forge/minecraft", new RequestParam
		{
			Encoding = Encoding.Default
		})?.ToString() ?? "";
		if (text.Length < 200)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge BMCLAPI", text }));
		}
		List<string> list = text.RegexSearch("[0-9.]+(_pre[0-9]?)?");
		if (list.Count < 10)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge BMCLAPI", text }));
		}
		loader.output = new DlForgeListResult
		{
			isOfficial = false,
			sourceName = "BMCLAPI",
			Value = list
		};
	}

	public static void DlForgeVersionMain(ModLoader.LoaderTask<string, List<DlForgeVersionEntry>> loader)
	{
		ModLoader.LoaderTask<string, List<DlForgeVersionEntry>> key = new ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>("DlForgeVersion Official", DlForgeVersionOfficialMain);
		ModLoader.LoaderTask<string, List<DlForgeVersionEntry>> key2 = new ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>("DlForgeVersion Bmclapi", DlForgeVersionBmclapiMain);
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>(key2, 30),
				new KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>(key, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>(key, 5),
				new KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>(key2, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>(key, 60),
				new KeyValuePair<ModLoader.LoaderTask<string, List<DlForgeVersionEntry>>, int>(key2, 120)
			}, loader.isForceRestarting);
			break;
		}
	}

	public static void DlForgeVersionOfficialMain(ModLoader.LoaderTask<string, List<DlForgeVersionEntry>> loader)
	{
		string text;
		try
		{
			text = Requester.FetchString("https://files.minecraftforge.net/maven/net/minecraftforge/forge/index_" + loader.input.Replace("-", "_") + ".html", new RequestParam
			{
				UseBrowserUserAgent = true
			});
		}
		catch (WebException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			if (ex2.Message.Contains("(404)"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.NotFound"));
			}
			throw;
		}
		if (text.Length < 1000)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge", text }));
		}
		List<DlForgeVersionEntry> list = new List<DlForgeVersionEntry>();
		try
		{
			string[] array = text.Substring(0, text.LastIndexOfF("</table>")).Split("<td class=\"download-version");
			int i = 1;
			for (int num = array.Count() - 1; i <= num; i++)
			{
				string text2 = array[i];
				try
				{
					string text3 = text2.RegexSeek("(?<=[^(0-9)]+)[0-9\\.]+");
					bool isRecommended = text2.Contains("fa promo-recommended");
					string input = loader.input;
					string text4 = text2.RegexSeek("(?<=-" + text3 + "-)[^-\"]+(?=-[a-z]+.[a-z]{3})");
					if (string.IsNullOrWhiteSpace(text4))
					{
						text4 = null;
					}
					string releaseTime = Lang.Date(DateTime.Parse(text2.RegexSeek("(?<=\"download-time\" title=\")[^\"]+"), null, DateTimeStyles.AssumeUniversal).ToLocalTime(), "g");
					string text5;
					string category;
					if (text2.Contains("classifier-installer\""))
					{
						text2 = text2.Substring(text2.IndexOfF("installer.jar"));
						text5 = text2.RegexSeek("(?<=MD5:</strong> )[^<]+");
						category = "installer";
						goto IL_01fb;
					}
					if (text2.Contains("classifier-universal\""))
					{
						text2 = text2.Substring(text2.IndexOfF("universal.zip"));
						text5 = text2.RegexSeek("(?<=MD5:</strong> )[^<]+");
						category = "universal";
						goto IL_01fb;
					}
					if (text2.Contains("client.zip"))
					{
						text2 = text2.Substring(text2.IndexOfF("client.zip"));
						text5 = text2.RegexSeek("(?<=MD5:</strong> )[^<]+");
						category = "client";
						goto IL_01fb;
					}
					continue;
					IL_01fb:
					list.Add(new DlForgeVersionEntry(text3, text4, input)
					{
						Category = category,
						IsRecommended = isRecommended,
						Hash = text5.Trim(new char[2] { '\r', '\n' }),
						ReleaseTime = releaseTime
					});
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge", text2 }), innerException);
				}
			}
		}
		catch (Exception innerException2)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge", text }), innerException2);
		}
		if (!list.Any())
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.NotFound"));
		}
		loader.output = list;
	}

	public static void DlForgeVersionBmclapiMain(ModLoader.LoaderTask<string, List<DlForgeVersionEntry>> loader)
	{
		JsonArray jsonArray = (JsonArray)Requester.FetchJson("https://bmclapi2.bangbang93.com/forge/minecraft/" + loader.input.Replace("-", "_"));
		List<DlForgeVersionEntry> list = new List<DlForgeVersionEntry>();
		try
		{
			string text = ModDownloadLib.McDownloadForgeRecommendedGet(loader.input);
			foreach (JsonObject item in jsonArray)
			{
				string hash = null;
				string category = "unknown";
				int num = -1;
				foreach (JsonObject item2 in item["files"].AsArray())
				{
					switch (item2["category"].ToString() ?? "")
					{
					case "installer":
						if (item2["format"].ToString() == "jar")
						{
							hash = (string?)item2["hash"];
							category = "installer";
							num = 2;
						}
						break;
					case "universal":
						if (num <= 1 && item2["format"].ToString() == "zip")
						{
							hash = (string?)item2["hash"];
							category = "universal";
							num = 1;
						}
						break;
					case "client":
						if (num <= 0 && item2["format"].ToString() == "zip")
						{
							hash = (string?)item2["hash"];
							category = "client";
							num = 0;
						}
						break;
					}
				}
				string branch = (string?)item["branch"];
				string text2 = (string?)item["version"];
				DlForgeVersionEntry dlForgeVersionEntry = new DlForgeVersionEntry(text2, branch, loader.input)
				{
					Hash = hash,
					Category = category,
					IsRecommended = ((text ?? "") == (text2 ?? ""))
				};
				item["modified"].ToString().Split('-', 'T', ':', '.', ' ', '/');
				dlForgeVersionEntry.ReleaseTime = Lang.Date(JsonCompat.ToObject<DateTime>(item["modified"]).ToLocalTime(), "g");
				list.Add(dlForgeVersionEntry);
			}
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Forge BMCLAPI", jsonArray }), innerException);
		}
		if (!list.Any())
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.NotFound"));
		}
		loader.output = list;
	}

	private static void DlNeoForgeListMain(ModLoader.LoaderTask<int, DlNeoForgeListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>(dlNeoForgeListBmclapiLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>(dlNeoForgeListOfficialLoader, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>(dlNeoForgeListOfficialLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>(dlNeoForgeListBmclapiLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>(dlNeoForgeListOfficialLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<int, DlNeoForgeListResult>, int>(dlNeoForgeListBmclapiLoader, 120)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlNeoForgeListOfficialMain(ModLoader.LoaderTask<int, DlNeoForgeListResult> loader)
	{
		string text = Requester.FetchJson("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge", new RequestParam
		{
			UseBrowserUserAgent = true
		}).ToString();
		string text2 = Requester.FetchJson("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/forge", new RequestParam
		{
			UseBrowserUserAgent = true
		}).ToString();
		if (text.Length < 100 || text2.Length < 100)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2]
			{
				"NeoForge",
				text + "\r\n\r\n" + text2
			}));
		}
		try
		{
			loader.output = new DlNeoForgeListResult
			{
				isOfficial = true,
				sourceName = Lang.Text("Download.Source.NeoForgeOfficial"),
				Value = GetNeoForgeEntries(text, text2)
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2]
			{
				"NeoForge",
				text + "\r\n\r\n" + text2
			}), innerException);
		}
	}

	public static void DlNeoForgeListBmclapiMain(ModLoader.LoaderTask<int, DlNeoForgeListResult> loader)
	{
		string text = Requester.FetchJson("https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/neoforge", new RequestParam
		{
			UseBrowserUserAgent = true
		}).ToString();
		string text2 = Requester.FetchJson("https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/forge", new RequestParam
		{
			UseBrowserUserAgent = true
		}).ToString();
		if (text.Length < 100 || text2.Length < 100)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2]
			{
				"NeoForge BMCLAPI",
				text + "\r\n\r\n" + text2
			}));
		}
		try
		{
			loader.output = new DlNeoForgeListResult
			{
				isOfficial = true,
				sourceName = "BMCLAPI",
				Value = GetNeoForgeEntries(text, text2)
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2]
			{
				"NeoForge BMCLAPI",
				text + "\r\n\r\n" + text2
			}), innerException);
		}
	}

	private static List<DlNeoForgeListEntry> GetNeoForgeEntries(string latestJson, string latestLegacyJson)
	{
		List<DlNeoForgeListEntry> list = (from name in (latestLegacyJson + latestJson).RegexSearch(RegexPatterns.DlNeoForgeVersion)
			where name != "47.1.82"
			select new DlNeoForgeListEntry(name) into a
			orderby a descending
			select a).ToList();
		if (!list.Any())
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.NotFound"));
		}
		return list;
	}

	private static void DlCleanroomListMain(ModLoader.LoaderTask<int, DlCleanroomListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlCleanroomListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlCleanroomListResult>, int>(dlCleanroomListOfficialLoader, 30)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlCleanroomListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlCleanroomListResult>, int>(dlCleanroomListOfficialLoader, 5)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlCleanroomListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlCleanroomListResult>, int>(dlCleanroomListOfficialLoader, 60)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlCleanroomListOfficialMain(ModLoader.LoaderTask<int, DlCleanroomListResult> loader)
	{
		string text = Requester.FetchJson("https://api.github.com/repos/CleanroomMC/Cleanroom/releases", new RequestParam
		{
			UseBrowserUserAgent = true
		}).ToString();
		if (text.Length < 100)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Cleanroom", text }));
		}
		try
		{
			loader.output = new DlCleanroomListResult
			{
				isOfficial = true,
				sourceName = Lang.Text("Download.Source.CleanroomOfficial"),
				Value = GetCleanroomEntries(text)
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Cleanroom", text }), innerException);
		}
	}

	private static List<DlCleanroomListEntry> GetCleanroomEntries(string latestJson)
	{
		List<DlCleanroomListEntry> list = new List<DlCleanroomListEntry>();
		foreach (JsonObject item in JsonNode.Parse(latestJson).AsArray())
		{
			list.Add(new DlCleanroomListEntry(item["tag_name"].ToString())
			{
				forgeType = DlForgelikeEntry.ForgelikeType.Cleanroom
			});
		}
		if (!list.Any())
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.NoAvailableVersion"));
		}
		return list.OrderByDescending((DlCleanroomListEntry a) => a.version).ToList();
	}

	private static void DlLiteLoaderListMain(ModLoader.LoaderTask<int, DlLiteLoaderListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>(dlLiteLoaderListBmclapiLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>(dlLiteLoaderListOfficialLoader, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>(dlLiteLoaderListOfficialLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>(dlLiteLoaderListBmclapiLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>(dlLiteLoaderListOfficialLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<int, DlLiteLoaderListResult>, int>(dlLiteLoaderListBmclapiLoader, 120)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlLiteLoaderListOfficialMain(ModLoader.LoaderTask<int, DlLiteLoaderListResult> loader)
	{
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://dl.liteloader.com/versions/versions.json");
		try
		{
			JsonObject jsonObject2 = (JsonObject)jsonObject["versions"];
			List<DlLiteLoaderListEntry> list = new List<DlLiteLoaderListEntry>();
			foreach (KeyValuePair<string, JsonNode> item in jsonObject2)
			{
				if (!item.Key.StartsWithF("1.6") && !item.Key.StartsWithF("1.5"))
				{
					JsonNode jsonNode = (item.Value["artefacts"] ?? item.Value["snapshots"])["com.mumfrey:liteloader"]["latest"];
					list.Add(new DlLiteLoaderListEntry
					{
						Inherit = item.Key,
						IsLegacy = (double.Parse(item.Key.Split(".")[1]) < 8.0),
						IsPreview = (jsonNode["stream"].ToString().ToLower() == "snapshot"),
						FileName = "liteloader-installer-" + item.Key + ((item.Key == "1.8" || item.Key == "1.9") ? ".0" : "") + "-00-SNAPSHOT.jar",
						MD5 = (string?)jsonNode["md5"],
						ReleaseTime = TimeUtils.FormatUnixTimestamp(long.Parse(jsonNode["timestamp"].ToString())),
						jsonToken = jsonNode
					});
				}
			}
			loader.output = new DlLiteLoaderListResult
			{
				isOfficial = true,
				sourceName = Lang.Text("Download.Source.LiteLoaderOfficial"),
				Value = list
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "LiteLoader", jsonObject }), innerException);
		}
	}

	private static void DlLiteLoaderListBmclapiMain(ModLoader.LoaderTask<int, DlLiteLoaderListResult> loader)
	{
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/versions.json");
		try
		{
			JsonObject jsonObject2 = (JsonObject)jsonObject["versions"];
			List<DlLiteLoaderListEntry> list = new List<DlLiteLoaderListEntry>();
			foreach (KeyValuePair<string, JsonNode> item in jsonObject2)
			{
				if (!item.Key.StartsWithF("1.6") && !item.Key.StartsWithF("1.5"))
				{
					JsonNode jsonNode = (item.Value["artefacts"] ?? item.Value["snapshots"])["com.mumfrey:liteloader"]["latest"];
					list.Add(new DlLiteLoaderListEntry
					{
						Inherit = item.Key,
						IsLegacy = (double.Parse(item.Key.Split(".")[1]) < 8.0),
						IsPreview = (jsonNode["stream"].ToString().ToLower() == "snapshot"),
						FileName = "liteloader-installer-" + item.Key + ((item.Key == "1.8" || item.Key == "1.9") ? ".0" : "") + "-00-SNAPSHOT.jar",
						MD5 = (string?)jsonNode["md5"],
						ReleaseTime = TimeUtils.FormatUnixTimestamp(long.Parse((string?)jsonNode["timestamp"])),
						jsonToken = jsonNode
					});
				}
			}
			loader.output = new DlLiteLoaderListResult
			{
				isOfficial = false,
				sourceName = "BMCLAPI",
				Value = list
			};
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "LiteLoader BMCLAPI", jsonObject }), innerException);
		}
	}

	private static void DlFabricListMain(ModLoader.LoaderTask<int, DlFabricListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>(dlFabricListBmclapiLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>(dlFabricListOfficialLoader, 90)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>(dlFabricListOfficialLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>(dlFabricListBmclapiLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>(dlFabricListOfficialLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<int, DlFabricListResult>, int>(dlFabricListBmclapiLoader, 120)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlFabricListOfficialMain(ModLoader.LoaderTask<int, DlFabricListResult> loader)
	{
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://meta.fabricmc.net/v2/versions");
		try
		{
			DlFabricListResult output = new DlFabricListResult
			{
				isOfficial = true,
				sourceName = Lang.Text("Download.Source.FabricOfficial"),
				Value = jsonObject
			};
			if (output.Value["game"] == null || output.Value["loader"] == null || output.Value["installer"] == null)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Fabric", jsonObject }));
			}
			loader.output = output;
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Fabric", jsonObject }), innerException);
		}
	}

	private static void DlFabricListBmclapiMain(ModLoader.LoaderTask<int, DlFabricListResult> loader)
	{
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://bmclapi2.bangbang93.com/fabric-meta/v2/versions");
		try
		{
			DlFabricListResult output = new DlFabricListResult
			{
				isOfficial = false,
				sourceName = "BMCLAPI",
				Value = jsonObject
			};
			if (output.Value["game"] == null || output.Value["loader"] == null || output.Value["installer"] == null)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Fabric BMCLAPI", jsonObject }));
			}
			loader.output = output;
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "Fabric BMCLAPI", jsonObject }), innerException);
		}
	}

	private static void DlLabyModListMain(ModLoader.LoaderTask<int, DlLabyModListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>(dlLabyModListOfficialLoader, 30),
				new KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>(dlLabyModListOfficialLoader, 60)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>(dlLabyModListOfficialLoader, 5),
				new KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>(dlLabyModListOfficialLoader, 35)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>(dlLabyModListOfficialLoader, 60),
				new KeyValuePair<ModLoader.LoaderTask<int, DlLabyModListResult>, int>(dlLabyModListOfficialLoader, 60)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlLabyModListOfficialMain(ModLoader.LoaderTask<int, DlLabyModListResult> loader)
	{
		JsonObject value;
		using (HttpResponseMessage httpResponseMessage = HttpSenderExtension.SendAsync(HttpBasicExtension.WithHttpVersionOption(HttpRequest.Create("https://releases.r2.labymod.net/api/v1/manifest/production/latest.json"), HttpVersion.Version20), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult())
		{
			value = (JsonObject)ModBase.GetJson(HttpResponseExtension.AsString(httpResponseMessage));
		}
		JsonObject value2;
		using (HttpResponseMessage httpResponseMessage2 = HttpSenderExtension.SendAsync(HttpBasicExtension.WithHttpVersionOption(HttpRequest.Create("https://releases.r2.labymod.net/api/v1/manifest/snapshot/latest.json"), HttpVersion.Version20), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult())
		{
			httpResponseMessage2.EnsureSuccessStatusCode();
			value2 = (JsonObject)ModBase.GetJson(HttpResponseExtension.AsString(httpResponseMessage2));
		}
		JsonObject jsonObject = new JsonObject();
		jsonObject.Add("production", value);
		jsonObject.Add("snapshot", value2);
		try
		{
			DlLabyModListResult output = new DlLabyModListResult
			{
				Value = jsonObject
			};
			if (output.Value["production"]["labyModVersion"] == null || output.Value["snapshot"]["labyModVersion"] == null)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "LabyMod", jsonObject }));
			}
			loader.output = output;
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "LabyMod", jsonObject }), innerException);
		}
	}

	public static string DlModRequest(string url)
	{
		return DlModRequest<string>(url);
	}

	public static T DlModRequest<T>(string url)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		string text = DlSourceModGet(url);
		if ((text ?? "") != (url ?? ""))
		{
			switch (Config.Download.Comp.CompSourceSolution)
			{
			case 0:
				list.Add(new KeyValuePair<string, int>(text, 5));
				list.Add(new KeyValuePair<string, int>(text, 10));
				list.Add(new KeyValuePair<string, int>(url, 15));
				break;
			case 1:
				list.Add(new KeyValuePair<string, int>(url, 5));
				list.Add(new KeyValuePair<string, int>(text, 5));
				list.Add(new KeyValuePair<string, int>(url, 15));
				list.Add(new KeyValuePair<string, int>(text, 10));
				break;
			default:
				list.Add(new KeyValuePair<string, int>(url, 5));
				list.Add(new KeyValuePair<string, int>(url, 15));
				list.Add(new KeyValuePair<string, int>(text, 10));
				break;
			}
		}
		string text2 = "";
		foreach (KeyValuePair<string, int> item in list)
		{
			try
			{
				string text3 = Requester.FetchString(item.Key, new RequestParam
				{
					Timeout = item.Value * 1000,
					UseBrowserUserAgent = true
				});
				if (typeof(T) == typeof(string))
				{
					return (T)(object)text3;
				}
				return (T)(object)ModBase.GetJson(text3);
			}
			catch (Exception ex)
			{
				if (!ex.Message.ContainsF("mcimirror"))
				{
					text2 = text2 + ex.Message + "\r\n";
				}
			}
		}
		throw new Exception(text2);
	}

	public static string DlModRequest(string url, string method, string data, string contentType, bool allowMirror = false)
	{
		return DlModRequest<string>(url, method, data, contentType, allowMirror);
	}

	public static T DlModRequest<T>(string url, string method, string data, string contentType, bool allowMirror = false)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		string text = DlSourceModGet(url);
		if ((text ?? "") != (url ?? ""))
		{
			switch (allowMirror ? Config.Download.Comp.CompSourceSolution : 2)
			{
			case 0:
				list.Add(new KeyValuePair<string, int>(text, 5));
				list.Add(new KeyValuePair<string, int>(text, 10));
				list.Add(new KeyValuePair<string, int>(url, 15));
				break;
			case 1:
				list.Add(new KeyValuePair<string, int>(url, 5));
				list.Add(new KeyValuePair<string, int>(text, 5));
				list.Add(new KeyValuePair<string, int>(url, 15));
				list.Add(new KeyValuePair<string, int>(text, 10));
				break;
			default:
				list.Add(new KeyValuePair<string, int>(url, 5));
				list.Add(new KeyValuePair<string, int>(url, 15));
				list.Add(new KeyValuePair<string, int>(text, 10));
				break;
			}
		}
		string text2 = "";
		foreach (KeyValuePair<string, int> item in list)
		{
			try
			{
				string text3 = Requester.Fetch(item.Key, new FetchParam
				{
					Method = method,
					Content = data,
					ContentType = contentType,
					Timeout = item.Value * 1000
				});
				if (typeof(T) == typeof(string))
				{
					return (T)(object)text3;
				}
				return (T)(object)ModBase.GetJson(text3);
			}
			catch (Exception ex)
			{
				if (!ex.Message.ContainsF("mcimirror"))
				{
					text2 = text2 + ex.Message + "\r\n";
				}
			}
		}
		throw new Exception(text2);
	}

	public static IEnumerable<string> DlSourceOrder(IEnumerable<string> officialUrls, IEnumerable<string> mirrorUrls)
	{
		if (!DlSourcePreferMojang)
		{
			return mirrorUrls.Union(officialUrls);
		}
		return officialUrls.Union(mirrorUrls);
	}

	public static IEnumerable<string> DlVersionListOrder(IEnumerable<string> officialUrls, IEnumerable<string> mirrorUrls)
	{
		if (!DlVersionListPreferMojang)
		{
			return mirrorUrls.Union(officialUrls);
		}
		return officialUrls.Union(mirrorUrls);
	}

	public static IEnumerable<string> DlSourceAssetsGet(string original)
	{
		original = original.Replace("http://resources.download.minecraft.net", "https://resources.download.minecraft.net");
		return DlSourceOrder(new string[1] { original }, new string[1] { original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/assets").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/assets").Replace("https://resources.download.minecraft.net", "https://bmclapi2.bangbang93.com/assets") });
	}

	public static IEnumerable<string> DlSourceLibraryGet(string original)
	{
		if (new string[3] { "minecraftforge", "fabricmc", "neoforged" }.Any((string k) => original.Contains(k)))
		{
			return new string[2]
			{
				original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/maven").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/maven").Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven")
					.Replace("https://zkitefly.github.io/unlisted-versions-of-minecraft", "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto"),
				original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/libraries").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/libraries").Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/libraries")
					.Replace("https://zkitefly.github.io/unlisted-versions-of-minecraft", "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto")
			};
		}
		return DlSourceOrder(new string[1] { original }, new string[3]
		{
			original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/maven").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/maven").Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven")
				.Replace("https://zkitefly.github.io/unlisted-versions-of-minecraft", "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto"),
			original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/libraries").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/libraries").Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/libraries")
				.Replace("https://zkitefly.github.io/unlisted-versions-of-minecraft", "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto"),
			original
		});
	}

	public static IEnumerable<string> DlSourceLauncherOrMetaGet(string original)
	{
		if (original == null)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.NoJsonDownloadAddress"));
		}
		return DlSourceOrder(new string[1] { original }, new string[2]
		{
			original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com").Replace("https://launcher.mojang.com", "https://bmclapi2.bangbang93.com")
				.Replace("https://launchermeta.mojang.com", "https://bmclapi2.bangbang93.com")
				.Replace("https://zkitefly.github.io/unlisted-versions-of-minecraft", "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto"),
			original
		});
	}

	public static string DlSourceModGet(string original)
	{
		return original.Replace("https://api.modrinth.com", "https://mod.mcimirror.top/modrinth").Replace("https://api.curseforge.com", "https://mod.mcimirror.top/curseforge");
	}

	public static List<string> DlSourceModDownloadGet(string original)
	{
		List<string> list = new List<string>();
		string item = original.Replace("https://cdn.modrinth.com", "https://mod.mcimirror.top").Replace("https://edge.forgecdn.net", "https://mod.mcimirror.top");
		switch (Config.Download.Comp.CompSourceSolution)
		{
		case 0:
			list.Add(item);
			list.Add(item);
			break;
		case 1:
			list.Add(original);
			list.Add(item);
			break;
		case 2:
			list.Add(original);
			list.Add(original);
			break;
		default:
			Config.Download.Comp.CompSourceSolution = 1;
			list.Add(original);
			break;
		}
		list.Add(original);
		return list;
	}

	private static void DlSourceLoader<InputType, OutputType>(ModLoader.LoaderTask<InputType, OutputType> mainLoader, List<KeyValuePair<ModLoader.LoaderTask<InputType, OutputType>, int>> loaderList, bool isForceRestart = false)
	{
		int num = 0;
		do
		{
			bool flag = true;
			foreach (KeyValuePair<ModLoader.LoaderTask<InputType, OutputType>, int> loader in loaderList)
			{
				if (num == 0)
				{
					if (isForceRestart || ((loader.Key.input == null) ^ (mainLoader.input == null)))
					{
						continue;
					}
					if (loader.Key.input != null)
					{
						ref InputType input = ref loader.Key.input;
						object obj = mainLoader.input;
						if (!input.Equals(obj))
						{
							continue;
						}
					}
				}
				if (loader.Key.State != ModBase.LoadState.Failed)
				{
					flag = false;
				}
				if (loader.Key.State == ModBase.LoadState.Finished)
				{
					mainLoader.output = loader.Key.output;
					DlSourceLoaderAbort(loaderList);
					return;
				}
				if (flag && num < loader.Value * 100)
				{
					num = loader.Value * 100;
				}
			}
			if (num == 0)
			{
				loaderList.First().Key.Start(mainLoader.input, isForceRestart);
				foreach (KeyValuePair<ModLoader.LoaderTask<InputType, OutputType>, int> item in loaderList.Skip(1))
				{
					item.Key.State = ModBase.LoadState.Waiting;
				}
			}
			int i = 0;
			for (int num2 = loaderList.Count - 1; i <= num2; i++)
			{
				if (num != loaderList[i].Value * 100)
				{
					continue;
				}
				if (i < loaderList.Count - 1 && !loaderList.All((KeyValuePair<ModLoader.LoaderTask<InputType, OutputType>, int> l) => l.Key.State == ModBase.LoadState.Failed))
				{
					loaderList[i + 1].Key.Start(mainLoader.input, isForceRestart);
					break;
				}
				Exception ex = null;
				int num3 = 0;
				for (int num4 = loaderList.Count - 1; num3 <= num4; num3++)
				{
					loaderList[num3].Key.input = default;
					if (loaderList[num3].Key.Error != null && (ex == null || loaderList[num3].Key.Error.Message.Contains(Lang.Text("Minecraft.Download.Error.NotFound"))))
					{
						ex = loaderList[num3].Key.Error;
					}
				}
				if (ex == null)
				{
					ex = new TimeoutException(Lang.Text("Minecraft.Download.Error.Timeout"));
				}
				DlSourceLoaderAbort(loaderList);
				throw ex;
			}
			Thread.Sleep(10);
			num++;
		}
		while (!mainLoader.IsAborted);
		DlSourceLoaderAbort(loaderList);
	}

	private static void DlSourceLoaderAbort<InputType, OutputType>(List<KeyValuePair<ModLoader.LoaderTask<InputType, OutputType>, int>> loaderList)
	{
		foreach (KeyValuePair<ModLoader.LoaderTask<InputType, OutputType>, int> loader in loaderList)
		{
			if (loader.Key.State == ModBase.LoadState.Loading)
			{
				loader.Key.Abort();
			}
		}
	}

	private static void DlLegacyFabricListMain(ModLoader.LoaderTask<int, DlLegacyFabricListResult> loader)
	{
		switch (Config.Download.VersionListSource)
		{
		case 0:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLegacyFabricListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLegacyFabricListResult>, int>(dlLegacyFabricListOfficialLoader, 30)
			}, loader.isForceRestarting);
			break;
		case 1:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLegacyFabricListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLegacyFabricListResult>, int>(dlLegacyFabricListOfficialLoader, 5)
			}, loader.isForceRestarting);
			break;
		default:
			DlSourceLoader(loader, new List<KeyValuePair<ModLoader.LoaderTask<int, DlLegacyFabricListResult>, int>>
			{
				new KeyValuePair<ModLoader.LoaderTask<int, DlLegacyFabricListResult>, int>(dlLegacyFabricListOfficialLoader, 60)
			}, loader.isForceRestarting);
			break;
		}
	}

	private static void DlLegacyFabricListOfficialMain(ModLoader.LoaderTask<int, DlLegacyFabricListResult> loader)
	{
		JsonObject jsonObject = (JsonObject)Requester.FetchJson("https://meta.legacyfabric.net/v2/versions");
		try
		{
			DlLegacyFabricListResult output = new DlLegacyFabricListResult
			{
				isOfficial = true,
				sourceName = Lang.Text("Download.Source.LegacyFabricOfficial"),
				Value = jsonObject
			};
			if (output.Value["game"] == null || output.Value["loader"] == null || output.Value["installer"] == null)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "LegacyFabric", jsonObject }));
			}
			loader.output = output;
		}
		catch (Exception innerException)
		{
			throw new Exception(Lang.Text("Minecraft.Download.Error.VersionListOperationFailed", new object[2] { "LegacyFabric", jsonObject }), innerException);
		}
	}

	public static void McDownloadClientUpdateHint(string versionName, JsonObject json)
	{
		try
		{
			JsonNode version = null;
			foreach (JsonNode item in json["versions"].AsArray())
			{
				if (item["id"] != null && (item["id"].ToString() ?? "") == (versionName ?? ""))
				{
					version = item;
					break;
				}
			}
			if (version == null)
			{
				return;
			}
			DateTime dateTime = JsonCompat.ToObject<DateTime>(version["releaseTime"]);
			if (ModMain.MyMsgBox(Lang.Text("Minecraft.Update.NewVersion", new object[1] { versionName }) + "\r\n" + (((DateTime.Now - dateTime).TotalDays > 1.0) ? (Lang.Text("Minecraft.Update.UpdateTime") + Lang.Date(dateTime, "G")) : (Lang.Text("Minecraft.Update.UpdatedAt") + Lang.TimeSpan(dateTime - DateTime.Now, 2, true, (TimeUnit)7, (TimeUnit)3))), Lang.Text("Minecraft.Update.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Download"), ((DateTime.Now - dateTime).TotalHours > 3.0) ? Lang.Text("Common.Action.UpdateLog") : "", isWarn: false, highLight: true, forceWait: false, null, null, () =>
			{
				ModDownloadLib.McUpdateLogShow(version);
			}) == 2)
			{
				ModBase.RunInUi(() =>
				{
					PageDownloadInstall.mcVersionWaitingForSelect = versionName;
					ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall);
				});
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Minecraft.Error.UpdateNotify", new object[1] { versionName ?? "Nothing" }), ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Error.UpdateNotify", new object[1] { versionName ?? "Nothing" }));
		}
	}
}
