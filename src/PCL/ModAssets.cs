using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

public static class ModAssets
{
	public struct McAssetsToken
	{
		public string localPath;

		public string sourcePath;

		public long size;

		public string hash;

		public override string ToString()
		{
			return ModBase.GetString(size) + " | " + localPath;
		}
	}

	public static JsonNode McAssetsGetIndex(McInstance mcInstance, bool returnLegacyOnError = false, bool checkURLEmpty = false)
	{
		try
		{
			while (true)
			{
				JsonNode jsonNode = mcInstance.JsonObject["assetIndex"];
				if (jsonNode != null && jsonNode["id"] != null)
				{
					return jsonNode;
				}
				if (mcInstance.JsonObject["assets"] != null)
				{
					mcInstance.JsonObject["assets"].ToString();
				}
				if (checkURLEmpty && jsonNode["url"] != null)
				{
					return jsonNode;
				}
				if (!string.IsNullOrEmpty(mcInstance.InheritInstanceName))
				{
					mcInstance = new McInstance(Path.Combine(ModFolder.mcFolderSelected, "versions", mcInstance.InheritInstanceName));
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		if (returnLegacyOnError)
		{
			ModBase.Log("[Minecraft] 无法获取资源文件索引下载地址，使用默认的 legacy 下载地址");
			return ModBase.GetJson("{\n                    \"id\": \"legacy\",\n                    \"sha1\": \"c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729\",\n                    \"size\": 134284,\n                    \"url\": \"https://launchermeta.mojang.com/mc-staging/assets/legacy/c0fd82e8ce9fbc93119e40d96d5a4e62cfa3f729/legacy.json\",\n                    \"totalSize\": 111220701\n                }");
		}
		throw new Exception(Lang.Text("Minecraft.Error.NoAssetIndexInfo"));
	}

	public static string McAssetsGetIndexName(McInstance mcInstance)
	{
		try
		{
			while (true)
			{
				if (mcInstance.JsonObject["assetIndex"] != null && mcInstance.JsonObject["assetIndex"]["id"] != null)
				{
					return mcInstance.JsonObject["assetIndex"]["id"].ToString();
				}
				if (mcInstance.JsonObject["assets"] != null)
				{
					return mcInstance.JsonObject["assets"].ToString();
				}
				if (!string.IsNullOrEmpty(mcInstance.InheritInstanceName))
				{
					mcInstance = new McInstance(Path.Combine(ModFolder.mcFolderSelected, "versions", mcInstance.InheritInstanceName));
					continue;
				}
				break;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取资源文件索引名失败");
		}
		return "legacy";
	}

	internal static string McAssetsHashPrefix(string hash)
	{
		return hash.Substring(0, 2);
	}

	internal static string McAssetsUrl(string hash)
	{
		return "https://resources.download.minecraft.net/" + McAssetsHashPrefix(hash) + "/" + hash;
	}

	internal static List<McAssetsToken> McAssetsListGet(McInstance mcInstance)
	{
		string text = McAssetsGetIndexName(mcInstance);
		try
		{
			if (!File.Exists(ModFolder.mcFolderSelected + "assets\\indexes\\" + text + ".json"))
			{
				throw new FileNotFoundException(Lang.Text("Minecraft.Error.AssetIndexNotFound"), Path.Combine(ModFolder.mcFolderSelected, "assets", "indexes", text + ".json"));
			}
			List<McAssetsToken> list = new List<McAssetsToken>();
			JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(ModFolder.mcFolderSelected + "assets\\indexes\\" + text + ".json"));
			foreach (KeyValuePair<string, JsonNode> item in jsonObject["objects"].AsObject())
			{
				string text2 = item.Value["hash"].ToString();
				string localPath;
				if (jsonObject["map_to_resources"] != null && JsonCompat.ToObject<bool>(jsonObject["map_to_resources"]))
				{
					localPath = Path.Combine(mcInstance.PathIndie, "resources", item.Key.Replace("/", "\\"));
				}
				else if (jsonObject["virtual"] != null && JsonCompat.ToObject<bool>(jsonObject["virtual"]))
				{
					InlineArray5<string> buffer = default;
					buffer[0] = ModFolder.mcFolderSelected;
					buffer[1] = "assets";
					buffer[2] = "virtual";
					buffer[3] = "legacy";
					buffer[4] = item.Key.Replace("/", "\\");
					localPath = Path.Combine(buffer);
				}
				else
				{
					InlineArray5<string> buffer2 = default;
					buffer2[0] = ModFolder.mcFolderSelected;
					buffer2[1] = "assets";
					buffer2[2] = "objects";
					buffer2[3] = McAssetsHashPrefix(text2);
					buffer2[4] = text2;
					localPath = Path.Combine(buffer2);
				}
				list.Add(new McAssetsToken
				{
					localPath = localPath,
					sourcePath = item.Key,
					hash = text2,
					size = long.Parse(item.Value["size"].ToString())
				});
			}
			return list;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取资源文件列表失败：" + text);
			throw;
		}
	}

	public static List<DownloadFile> McAssetsFixList(McInstance mcInstance, bool checkHash, [Optional] ref ModLoader.LoaderBase progressFeed)
	{
		if (checkHash)
		{
			return McAssetsListGet(mcInstance).Select((McAssetsToken token) =>
			{
				string hash2 = token.hash;
				return new DownloadFile(ModDownload.DlSourceAssetsGet(McAssetsUrl(hash2)), token.localPath, new ModBase.FileChecker(-1L, (token.size == 0L) ? (-1) : token.size, hash2));
			}).ToList();
		}
		List<DownloadFile> list = new List<DownloadFile>();
		try
		{
			List<McAssetsToken> list2 = McAssetsListGet(mcInstance);
			if (progressFeed != null)
			{
				progressFeed.Progress = 0.04;
			}
			int num = 0;
			for (int num2 = list2.Count - 1; num <= num2; num++)
			{
				McAssetsToken mcAssetsToken = list2[num];
				if (progressFeed != null)
				{
					progressFeed.Progress = 0.05 + 0.94 * (double)num / (double)list2.Count;
				}
				FileInfo fileInfo = new FileInfo(mcAssetsToken.localPath);
				if (!fileInfo.Exists || (mcAssetsToken.size != 0L && mcAssetsToken.size != fileInfo.Length))
				{
					string hash = mcAssetsToken.hash;
					list.Add(new DownloadFile(ModDownload.DlSourceAssetsGet(McAssetsUrl(hash)), mcAssetsToken.localPath, new ModBase.FileChecker(-1L, (mcAssetsToken.size == 0L) ? (-1) : mcAssetsToken.size, hash)));
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取实例缺失的资源文件下载列表失败");
		}
		if (progressFeed != null)
		{
			progressFeed.Progress = 0.99;
		}
		return list;
	}
}
