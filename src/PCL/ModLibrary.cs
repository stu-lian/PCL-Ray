using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using PCL.Core.App;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Network;

namespace PCL;

public static class ModLibrary
{
	public class McLibToken
	{
		private string _Url;

		public bool IsLocal;

		public bool IsNatives;

		public string LocalPath;

		public string OriginalName;

		public string Sha1;

		public long size;

		public string Url
		{
			get
			{
				return _Url;
			}
			set
			{
				_Url = (string.IsNullOrWhiteSpace(value) ? null : value);
			}
		}

		public string Name
		{
			get
			{
				if (OriginalName == null)
				{
					return null;
				}
				List<string> list = new List<string>(OriginalName.Split(":"));
				list.RemoveAt(2);
				return list.Join(":");
			}
		}

		public override string ToString()
		{
			return (IsNatives ? "[Native] " : "") + ModBase.GetString(size) + " | " + LocalPath;
		}
	}

	private static readonly string osVersion = Environment.OSVersion.Version.ToString();

	public static bool McJsonRuleCheck(JsonNode ruleToken)
	{
		if (ruleToken == null)
		{
			return true;
		}
		bool result = false;
		foreach (JsonNode item in ruleToken.AsArray())
		{
			bool flag = true;
			if (item["os"] != null)
			{
				if (item["os"]["name"] != null)
				{
					string text = item["os"]["name"].ToString();
					if (!(text == "unknown"))
					{
						if (text == "windows")
						{
							if (item["os"]["version"] != null)
							{
								string regex = item["os"]["version"].ToString();
								flag = flag && osVersion.RegexCheck(regex);
							}
						}
						else
						{
							flag = false;
						}
					}
				}
				if (item["os"]["arch"] != null)
				{
					flag = flag && item["os"]["arch"].ToString() == "x86" == SystemInfo.Is32BitSystem;
				}
			}
			if (item["features"] != null)
			{
				flag = flag && item["features"]["is_demo_user"] == null;
				if (item["features"].AsObject().Any((KeyValuePair<string, JsonNode> prop) => prop.Key.Contains("quick_play")))
				{
					flag = false;
				}
			}
			if (item["action"].ToString() == "allow")
			{
				if (flag)
				{
					result = true;
				}
			}
			else if (flag)
			{
				result = false;
			}
		}
		return result;
	}

	public static List<McLibToken> McLibListGet(McInstance mcInstance, bool includeInstanceJar)
	{
		ModBase.Log("[Minecraft] 获取支持库列表：" + mcInstance.Name);
		List<McLibToken> list = McLibListGetWithJson(mcInstance.JsonObject, keepSameNameDifferentVersionResult: false, null, mcInstance);
		if (includeInstanceJar)
		{
			string text = mcInstance.JsonObject["jar"]?.ToString();
			McInstance mcInstance3;
			if (mcInstance.IsHmclFormatJson || text == null)
			{
				McInstance mcInstance2 = mcInstance;
				if ((!mcInstance.Info.HasForge && !mcInstance.Info.HasNeoForge) || mcInstance.Info.Drop < 170)
				{
					while (!string.IsNullOrEmpty(mcInstance2.InheritInstanceName) && !((mcInstance2.InheritInstanceName ?? "") == (mcInstance2.Name ?? "")))
					{
						mcInstance2 = new McInstance(Path.Combine(ModFolder.mcFolderSelected, "versions", mcInstance2.InheritInstanceName));
					}
				}
				mcInstance3 = new McInstance(mcInstance2.PathInstance);
			}
			else
			{
				mcInstance3 = new McInstance(text);
			}
			if (!File.Exists(mcInstance3.PathInstance + mcInstance3.Name + ".json"))
			{
				mcInstance3 = mcInstance;
				ModBase.Log("[Minecraft] 可能缺少前置实例 " + mcInstance3.Name + "，找不到对应的 JSON 文件", ModBase.LogLevel.Debug);
			}
			string url;
			string sha;
			if (mcInstance3.JsonObject["downloads"] != null && mcInstance3.JsonObject["downloads"]["client"] != null)
			{
				url = (string?)mcInstance3.JsonObject["downloads"]["client"]["url"];
				sha = (string?)mcInstance3.JsonObject["downloads"]["client"]["sha1"];
			}
			else
			{
				url = null;
				sha = null;
			}
			list.Add(new McLibToken
			{
				LocalPath = mcInstance3.PathInstance + mcInstance3.Name + ".jar",
				size = 0L,
				IsNatives = false,
				Url = url,
				Sha1 = sha
			});
		}
		return list;
	}

	public static List<McLibToken> McLibListGetWithJson(JsonObject jsonObject, bool keepSameNameDifferentVersionResult = false, string customMcFolder = null, McInstance targetMcInstance = null)
	{
		customMcFolder = customMcFolder ?? ModFolder.mcFolderSelected;
		List<McLibToken> list = new List<McLibToken>();
		foreach (JsonNode item3 in (JsonArray)jsonObject["libraries"])
		{
			JsonObject jsonObject2 = item3.AsObject();
			foreach (string item4 in (from p in jsonObject2.Where((KeyValuePair<string, JsonNode> p) =>
				{
					JsonNode value = p.Value;
					return value != null && value.GetValueKind() == JsonValueKind.Null;
				})
				select p.Key).ToList())
			{
				jsonObject2.Remove(item4);
			}
			if (!McJsonRuleCheck(jsonObject2["rules"]))
			{
				continue;
			}
			string text = (string?)jsonObject2["url"];
			if (text != null)
			{
				text += McLibGet((string?)jsonObject2["name"], withHead: false, ignoreLiteLoader: true, customMcFolder).Replace("\\", "/");
			}
			string text2 = (string?)jsonObject2["hint"];
			bool flag = text2 != null && text2 == "local";
			if (jsonObject2["natives"] == null)
			{
				string localPath = ((!flag || (object)targetMcInstance == null) ? McLibGet((string?)jsonObject2["name"], withHead: true, ignoreLiteLoader: false, customMcFolder) : (targetMcInstance.PathInstance + "libraries\\" + jsonObject2["name"].ToString().AfterFirst(":").Replace(":", "-") + ".jar"));
				JsonNode jsonNode = ((jsonObject2["downloads"] != null && jsonObject2["downloads"]["artifact"] != null) ? jsonObject2["downloads"]["artifact"]["path"] : null);
				try
				{
					if (jsonObject2["downloads"] != null && jsonObject2["downloads"]["artifact"] != null)
					{
						McLibToken mcLibToken = new McLibToken();
						string item = (mcLibToken.OriginalName = (string?)jsonObject2["name"]);
						string text3 = text;
						string item2 = (mcLibToken.Url = (string?)((text3 != null) ? ((JsonNode?)text3) : jsonObject2["downloads"]["artifact"]["url"]));
						list.Add((item, item2, mcLibToken.LocalPath = ((jsonNode == null) ? McLibGet((string?)jsonObject2["name"], withHead: true, ignoreLiteLoader: false, customMcFolder) : McLibGetByArtifactPath(jsonNode.ToString(), customMcFolder)), mcLibToken.size = (long)Math.Round(ModBase.Val(jsonObject2["downloads"]["artifact"]["size"].ToString())), mcLibToken.IsNatives = false, mcLibToken.Sha1 = jsonObject2["downloads"]["artifact"]["sha1"]?.ToString(), mcLibToken.IsLocal = flag, mcLibToken).Item8);
					}
					else
					{
						list.Add(new McLibToken
						{
							OriginalName = (string?)jsonObject2["name"],
							Url = text,
							LocalPath = localPath,
							size = 0L,
							IsNatives = false,
							Sha1 = null,
							IsLocal = flag
						});
					}
				}
				catch (Exception ex) when (jsonNode != null && (ex is ArgumentException || ex is IOException))
				{
					ModBase.Log(ex, "支持库下载路径非法，已跳过（无 Natives，" + (jsonObject2["name"] ?? ((JsonNode?)"Nothing"))?.ToString() + "）");
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, "处理实际支持库列表失败（无 Natives，" + (jsonObject2["name"] ?? ((JsonNode?)"Nothing"))?.ToString() + "）");
					list.Add(new McLibToken
					{
						OriginalName = (string?)jsonObject2["name"],
						Url = text,
						LocalPath = localPath,
						size = 0L,
						IsNatives = false,
						Sha1 = null
					});
				}
			}
			else
			{
				if (jsonObject2["natives"]["windows"] == null)
				{
					continue;
				}
				JsonNode jsonNode2 = ((jsonObject2["downloads"] != null && jsonObject2["downloads"]["classifiers"] != null && jsonObject2["downloads"]["classifiers"]["natives-windows"] != null) ? jsonObject2["downloads"]["classifiers"]["natives-windows"]["path"] : null);
				try
				{
					if (jsonObject2["downloads"] != null && jsonObject2["downloads"]["classifiers"] != null && jsonObject2["downloads"]["classifiers"]["natives-windows"] != null)
					{
						McLibToken mcLibToken2 = new McLibToken
						{
							OriginalName = (string?)jsonObject2["name"]
						};
						string item2 = text;
						mcLibToken2.Url = (string?)((item2 != null) ? ((JsonNode?)item2) : jsonObject2["downloads"]["classifiers"]["natives-windows"]["url"]);
						mcLibToken2.LocalPath = ((jsonNode2 == null) ? McLibGet((string?)jsonObject2["name"], withHead: true, ignoreLiteLoader: false, customMcFolder).Replace(".jar", "-" + jsonObject2["natives"]["windows"]?.ToString() + ".jar").Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32") : McLibGetByArtifactPath(jsonNode2.ToString(), customMcFolder));
						mcLibToken2.size = (long)Math.Round(ModBase.Val(jsonObject2["downloads"]["classifiers"]["natives-windows"]["size"].ToString()));
						mcLibToken2.IsNatives = true;
						mcLibToken2.Sha1 = jsonObject2["downloads"]["classifiers"]["natives-windows"]["sha1"].ToString();
						mcLibToken2.IsLocal = flag;
						list.Add(mcLibToken2);
					}
					else
					{
						list.Add(new McLibToken
						{
							OriginalName = (string?)jsonObject2["name"],
							Url = text,
							LocalPath = McLibGet((string?)jsonObject2["name"], withHead: true, ignoreLiteLoader: false, customMcFolder).Replace(".jar", "-" + jsonObject2["natives"]["windows"]?.ToString() + ".jar").Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32"),
							size = 0L,
							IsNatives = true,
							Sha1 = null,
							IsLocal = flag
						});
					}
				}
				catch (Exception ex3) when (jsonNode2 != null && (ex3 is ArgumentException || ex3 is IOException))
				{
					ModBase.Log(ex3, "支持库下载路径非法，已跳过（有 Natives，" + (jsonObject2["name"] ?? ((JsonNode?)"Nothing"))?.ToString() + "）");
				}
				catch (Exception ex4)
				{
					ModBase.Log(ex4, "处理实际支持库列表失败（有 Natives，" + (jsonObject2["name"] ?? ((JsonNode?)"Nothing"))?.ToString() + "）");
					list.Add(new McLibToken
					{
						OriginalName = (string?)jsonObject2["name"],
						Url = text,
						LocalPath = McLibGet((string?)jsonObject2["name"], withHead: true, ignoreLiteLoader: false, customMcFolder).Replace(".jar", "-" + jsonObject2["natives"]["windows"]?.ToString() + ".jar").Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32"),
						size = 0L,
						IsNatives = true,
						Sha1 = null,
						IsLocal = false
					});
				}
			}
		}
		Dictionary<string, McLibToken> dictionary = new Dictionary<string, McLibToken>();
		int num = 0;
		for (int num2 = list.Count - 1; num <= num2; num++)
		{
			string text5 = list[num].Name + list[num].IsNatives;
			if (dictionary.ContainsKey(text5))
			{
				string text6 = GetVersion(list[num]);
				string text7 = GetVersion(dictionary[text5]);
				if (((text6 ?? "") != (text7 ?? "")) & keepSameNameDifferentVersionResult)
				{
					ModBase.Log($"[Minecraft] 发现疑似重复的支持库：{list[num]} ({text6}) 与 {dictionary[text5]} ({text7})");
					dictionary.Add(text5 + ModBase.GetUuid(), list[num]);
					continue;
				}
				ModBase.Log($"[Minecraft] 发现重复的支持库：{list[num]} ({text6}) 与 {dictionary[text5]} ({text7})，已忽略其中之一");
				if (McVersionComparer.CompareVersionGe(text6, text7))
				{
					dictionary[text5] = list[num];
				}
			}
			else
			{
				dictionary.Add(text5, list[num]);
			}
		}
		return dictionary.Values.ToList();
		static string GetVersion(McLibToken token)
		{
			return ModBase.GetFolderNameFromPath(ModBase.GetPathFromFullPath(token.LocalPath));
		}
	}

	public static List<DownloadFile> McLibNetFilesFromInstance(McInstance mcInstance)
	{
		if (!mcInstance.IsLoaded)
		{
			mcInstance.Load();
		}
		List<DownloadFile> list = new List<DownloadFile>();
		try
		{
			DownloadFile downloadFile = ModDownload.DlClientJarGet(mcInstance, returnNothingOnFileUseable: true);
			if (downloadFile != null)
			{
				list.Add(downloadFile);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "实例缺失主 Jar 文件所必须的信息", ModBase.LogLevel.Developer);
		}
		list.AddRange(McLibNetFilesFromTokens(McLibListGet(mcInstance, includeInstanceJar: false)));
		string localPath = Path.Combine(ModBase.pathPure, "authlib-injector.jar");
		JsonObject jsonObject = null;
		try
		{
			ModBase.Log("[Minecraft] 开始获取 Authlib-Injector 下载信息");
			jsonObject = (JsonObject)ModBase.GetJson(ModNet.NetGetCodeByLoader(new string[2] { "https://authlib-injector.yushi.moe/artifact/latest.json", "https://bmclapi2.bangbang93.com/mirrors/authlib-injector/artifact/latest.json" }, 45000, isJson: true));
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "获取 Authlib-Injector 下载信息失败");
		}
		if (jsonObject != null && new ModBase.FileChecker(-1L, -1L, jsonObject["checksums"]["sha256"].ToString()).Check(localPath) != null)
		{
			string text = jsonObject["download_url"].ToString().Replace("bmclapi2.bangbang93.com/mirrors/authlib-injector", "authlib-injector.yushi.moe");
			ModBase.Log("[Minecraft] Authlib-Injector 需要更新：" + text, ModBase.LogLevel.Developer);
			list.Add(new DownloadFile(new string[2]
			{
				text,
				text.Replace("authlib-injector.yushi.moe", "bmclapi2.bangbang93.com/mirrors/authlib-injector")
			}, localPath, new ModBase.FileChecker(-1L, -1L, jsonObject["checksums"]["sha256"].ToString())));
		}
		string text2 = Path.Combine(ModBase.pathPure, "mesa-loader-windows", "26.0.4", "Loader.jar");
		int num = -1;
		if ((object)ModInstanceList.McMcInstanceSelected != null)
		{
			num = ((ParameterizedProperty<object, int>)(object)Config.Instance.Renderer)[(object)ModInstanceList.McMcInstanceSelected?.PathInstance] - 1;
		}
		if (num == -1)
		{
			num = Config.Launch.Renderer;
		}
		if (num != 0 && !File.Exists(text2))
		{
			string text3;
			if (SystemInfo.Is32BitSystem)
			{
				text3 = "x86";
			}
			else
			{
				text3 = (SystemInfo.IsArm64System ? "arm64" : "x64");
			}
			string text4 = "https://mirrors.cloud.tencent.com/nexus/repository/maven-public/org/glavo/mesa-loader-windows/26.0.4/mesa-loader-windows-26.0.4-" + text3 + ".jar";
			list.Add(new DownloadFile(new string[1] { text4 }, text2));
		}
		if (mcInstance.Info.HasLabyMod)
		{
			if ((mcInstance.PathIndie ?? "") == (mcInstance.PathInstance ?? ""))
			{
				if (Directory.Exists(Path.Combine(mcInstance.PathInstance, "labymod-neo")))
				{
					Directory.Delete(Path.Combine(mcInstance.PathInstance, "labymod-neo"), recursive: true);
				}
				ModBase.CreateSymbolicLink(Path.Combine(mcInstance.PathInstance, "labymod-neo"), Path.Combine(ModFolder.mcFolderSelected, "labymod-neo"), 2);
			}
			try
			{
				string text5 = mcInstance.JsonObject["labymod_data"]["channelType"].ToString();
				Directory.CreateDirectory(ModFolder.mcFolderSelected + "labymod-neo\\libraries");
				ModBase.Log("[Minecraft] 开始获取 LabyMod 信息");
				JsonObject jsonObject2 = (JsonObject)ModNet.NetGetCodeByRequestRetry("https://releases.r2.labymod.net/api/v1/manifest/" + text5 + "/latest.json", null, "", isJson: true);
				JsonObject jsonObject3 = (JsonObject)jsonObject2["assets"];
				string value = jsonObject2["commitReference"].ToString();
				foreach (KeyValuePair<string, JsonNode> item in jsonObject3)
				{
					string key = item.Key;
					string text6 = item.Value.ToString();
					string localPath2 = ModFolder.mcFolderSelected + "labymod-neo\\assets\\" + key + ".jar";
					string text7 = $"https://releases.r2.labymod.net/api/v1/download/assets/labymod4/{text5}/{value}/{key}/{text6}.jar";
					ModBase.FileChecker fileChecker = new ModBase.FileChecker(-1L, -1L, text6);
					if (fileChecker.Check(localPath2) != null)
					{
						list.Add(new DownloadFile(new string[1] { text7 }, localPath2, fileChecker));
					}
				}
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "获取 LabyMod 信息失败，跳过检查");
			}
		}
		if (ShouldIgnoreFileCheck(mcInstance))
		{
			ModBase.Log("[Minecraft] 用户要求尽量忽略文件检查，这可能会保留有误的文件");
			list = list.Where((DownloadFile f) =>
			{
				if (File.Exists(f.LocalPath))
				{
					ModBase.Log("[Minecraft] 跳过下载的支持库文件：" + f.LocalPath, ModBase.LogLevel.Debug);
					return false;
				}
				return true;
			}).ToList();
		}
		return list;
	}

	public static List<DownloadFile> McLibNetFilesFromTokens(List<McLibToken> libs, string customMcFolder = null)
	{
		customMcFolder = customMcFolder ?? ModFolder.mcFolderSelected;
		List<DownloadFile> list = new List<DownloadFile>();
		foreach (McLibToken lib in libs)
		{
			ModBase.FileChecker fileChecker = new ModBase.FileChecker(-1L, (lib.size == 0L) ? (-1) : lib.size, lib.Sha1);
			if (fileChecker.Check(lib.LocalPath) == null)
			{
				continue;
			}
			if (lib.IsLocal)
			{
				ModBase.Log("[Download] 已跳过被标记为本地文件的支持库: " + lib.OriginalName);
				continue;
			}
			List<string> list2 = new List<string>();
			if (lib.Url == null && lib.Name == "net.minecraftforge:forge:universal")
			{
				lib.Url = "https://maven.minecraftforge.net" + lib.LocalPath.Replace(customMcFolder + "libraries", "").Replace("\\", "/");
			}
			if (lib.Url != null)
			{
				list2.Add(lib.Url);
				if (lib.Url.Contains("launcher.mojang.com/v1/objects") || lib.Url.Contains("client.txt") || lib.Url.Contains(".tsrg"))
				{
					list2.AddRange(ModDownload.DlSourceLauncherOrMetaGet(lib.Url));
				}
				if (lib.Url.Contains("maven"))
				{
					string item = lib.Url.Replace(lib.Url.Substring(0, lib.Url.IndexOfF("maven")), "https://bmclapi2.bangbang93.com/").Replace("maven.fabricmc.net", "maven").Replace("maven.minecraftforge.net", "maven")
						.Replace("maven.neoforged.net/releases", "maven");
					if (ModDownload.DlSourcePreferMojang)
					{
						list2.Add(item);
					}
					else
					{
						list2.Insert(0, item);
					}
				}
			}
			if (lib.LocalPath.Contains("transformer-discovery-service"))
			{
				if (!File.Exists(lib.LocalPath))
				{
					ModBase.WriteFile(lib.LocalPath, ModBase.GetResourceStream("Resources/transformer.jar"));
				}
				ModBase.Log("[Download] 已自动释放 Transformer Discovery Service", ModBase.LogLevel.Developer);
				continue;
			}
			if (lib.LocalPath.Contains("optifine\\OptiFine"))
			{
				string text = lib.LocalPath.Replace(Path.Combine(customMcFolder, "libraries", "optifine", "OptiFine") + "\\", "").Split("_")[0] + "/" + ModBase.GetFileNameFromPath(lib.LocalPath).Replace("-", "_");
				text = "/maven/com/optifine/" + text;
				if (text.Contains("_pre"))
				{
					text = text.Replace("com/optifine/", "com/optifine/preview_");
				}
				list2.Add("https://bmclapi2.bangbang93.com" + text);
			}
			else if (lib.Name.Contains("LabyMod"))
			{
				list2.Add(lib.Url);
				ModBase.Log($"[Download] 获取到 LabyMod 主要库文件的 Size = {lib.size},SHA1 = {lib.Sha1}，由于 LabyMod 乱写 Size，已忽略 Size");
				fileChecker = new ModBase.FileChecker(-1L, -1L, lib.Sha1);
			}
			else if (list2.Count <= 2)
			{
				list2.AddRange(ModDownload.DlSourceLibraryGet("https://libraries.minecraft.net" + lib.LocalPath.Replace(customMcFolder + "libraries", "").Replace("\\", "/")));
			}
			list.Add(new DownloadFile(list2.Distinct(), lib.LocalPath, fileChecker));
		}
		return list.Distinct((DownloadFile a, DownloadFile b) => (a.LocalPath ?? "") == (b.LocalPath ?? ""));
	}

	public static string McLibGet(string original, bool withHead = true, bool ignoreLiteLoader = false, string customMcFolder = null)
	{
		string text = null;
		customMcFolder = customMcFolder ?? ModFolder.mcFolderSelected;
		string[] array = original.Split(":");
		string text2;
		if (!withHead)
		{
			text2 = Path.Combine(array[0].Replace(".", "\\"), array[1], array[2], array[1] + "-" + array[2] + ".jar");
		}
		else
		{
			InlineArray6<string> buffer = default;
			buffer[0] = customMcFolder;
			buffer[1] = "libraries";
			buffer[2] = array[0].Replace(".", "\\");
			buffer[3] = array[1];
			buffer[4] = array[2];
			buffer[5] = array[1] + "-" + array[2] + ".jar";
			text2 = Path.Combine(buffer);
		}
		text = text2;
		if (text.Contains("optifine\\OptiFine\\1.") && array[2].Split(".").Count() > 1)
		{
			int num = (int)Math.Round(ModBase.Val(array[2].Split(".")[1].BeforeFirst("_")));
			int num2 = (int)Math.Round((array[2].Split(".").Count() > 2) ? ModBase.Val(array[2].Split(".")[2].BeforeFirst("_")) : 0.0);
			if ((num == 12 || (num == 20 && num2 >= 4) || num >= 21) && File.Exists($"{customMcFolder}libraries\\{array[0].Replace(".", "\\")}\\{array[1]}\\{array[2]}\\{array[1]}-{array[2]}-installer.jar"))
			{
				ModLaunch.McLaunchLog("已将 " + original + " 替换为对应的 Installer 文件");
				text = text.Replace(".jar", "-installer.jar");
			}
		}
		return text;
	}

	private static string McLibGetByArtifactPath(string artifactPath, string customMcFolder = null)
	{
		customMcFolder = customMcFolder ?? ModFolder.mcFolderSelected;
		if (string.IsNullOrWhiteSpace(artifactPath))
		{
			throw new ArgumentException("支持库下载路径无效", "artifactPath");
		}
		string fullPath = Path.GetFullPath(Path.Combine(customMcFolder, "libraries"));
		string text = artifactPath.Replace('/', Path.DirectorySeparatorChar);
		if (Path.IsPathRooted(text))
		{
			throw new IOException("支持库下载路径不能为绝对路径: " + artifactPath);
		}
		string fullPath2 = Path.GetFullPath(Path.Combine(fullPath, text));
		string value = fullPath.TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}) + Path.DirectorySeparatorChar;
		if (!fullPath2.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("支持库下载路径不能位于 libraries 文件夹外: " + artifactPath);
		}
		return fullPath2;
	}

	public static bool ShouldIgnoreFileCheck(McInstance version)
	{
		if (!((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableAssetVerifyV2)[(object)version.PathInstance])
		{
			return ((ParameterizedProperty<object, int>)(object)Config.Instance.AssetVerifySolutionV1)[(object)version.PathInstance] == 2;
		}
		return true;
	}
}
