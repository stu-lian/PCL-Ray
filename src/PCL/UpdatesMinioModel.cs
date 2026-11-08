using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;
using PCL.Core.Utils.Diff;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public class UpdatesMinioModel : IUpdateSource
{
	private class MinioUpdateModel
	{
		[JsonPropertyName("assets")]
		public List<MinioUpdateAsset> Assets { get; set; }
	}

	private class MinioUpdateAsset
	{
		[JsonPropertyName("file_name")]
		public string FileName { get; set; }

		[JsonPropertyName("version")]
		public MinioUpdateAssetVersionInfo Version { get; set; }

		[JsonPropertyName("upd_time")]
		public string UpdTime { get; set; }

		[JsonPropertyName("downloads")]
		public List<string> Downloads { get; set; }

		[JsonPropertyName("patches")]
		public List<string> Patches { get; set; }

		[JsonPropertyName("sha256")]
		public string Sha256 { get; set; }

		[JsonPropertyName("changelog")]
		public string Changelog { get; set; }
	}

	private class MinioUpdateAssetVersionInfo
	{
		[JsonPropertyName("channel")]
		public string Channel { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("code")]
		public int Code { get; set; }
	}

	private readonly string _baseUrl;

	private Dictionary<string, string> _remoteCache;

	public string SourceName { get; set; }

	public UpdatesMinioModel(string baseUrl, string name = "Minio")
	{
		_baseUrl = baseUrl;
		SourceName = name;
	}

	public bool IsAvailable()
	{
		return !string.IsNullOrWhiteSpace(_baseUrl);
	}

	public bool RefreshCache()
	{
		JsonNode json = ModBase.GetJson(Requester.FetchString(_baseUrl + "apiv2/cache.json", RequestParam.WithRetry));
		_remoteCache = JsonCompat.ToObject<Dictionary<string, string>>(json);
		return true;
	}

	public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
	{
		if (_remoteCache == null)
		{
			RefreshCache();
		}
		return GetChannelInfo(channel, arch);
	}

	public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		if (_remoteCache == null)
		{
			RefreshCache();
		}
		VersionDataModel channelInfo = GetChannelInfo(channel, arch);
		return currentVersion >= SemVer.Parse(channelInfo.VersionName);
	}

	public VersionAnnouncementDataModel GetAnnouncementList()
	{
		if (_remoteCache == null)
		{
			RefreshCache();
		}
		JsonNode remoteInfoByName = GetRemoteInfoByName("announcement");
		return ((remoteInfoByName != null) ? JsonCompat.ToObject<VersionAnnouncementDataModel>(remoteInfoByName) : null) ?? throw new NullReferenceException("Can not get remote announcement info!");
	}

	public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
	{
		if (_remoteCache == null)
		{
			RefreshCache();
		}
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		bool patchUpdate = true;
		string tempPath = ModBase.pathTemp + "Cache\\Update\\Download\\";
		list.Add(new ModLoader.LoaderTask<int, List<DownloadFile>>(Lang.Text("Update.Task.GetVersionInfo"), (ModLoader.LoaderTask<int, List<DownloadFile>> load) =>
		{
			string channelName = GetChannelName(channel, arch);
			JsonNode remoteInfoByName = GetRemoteInfoByName("updates-" + channelName, "updates/");
			MinioUpdateAsset minioUpdateAsset = ((remoteInfoByName == null) ? null : JsonCompat.ToObject<MinioUpdateModel>(remoteInfoByName)?.Assets?.FirstOrDefault());
			if (minioUpdateAsset == null)
			{
				throw new Exception("No assets can download!");
			}
			string fileSHA = ModBase.GetFileSHA256(Basics.ExecutablePath);
			string sha = minioUpdateAsset.Sha256;
			string text = fileSHA + "_" + sha + ".patch";
			if (minioUpdateAsset.Patches.Contains(text))
			{
				patchUpdate = true;
				tempPath += text;
				load.output = new List<DownloadFile>
				{
					new DownloadFile(new string[1] { _baseUrl + "static/patch/" + text }, tempPath)
				};
			}
			else
			{
				patchUpdate = false;
				tempPath = tempPath + minioUpdateAsset.Sha256 + ".bin";
				load.output = new List<DownloadFile>
				{
					new DownloadFile(RandomUtils.Shuffle<string>((IList<string>)minioUpdateAsset.Downloads), tempPath)
				};
			}
		}));
		list.Add(new LoaderDownload(Lang.Text("Update.Task.DownloadFile"), new List<DownloadFile>()));
		list.Add(new ModLoader.LoaderTask<string, int>(Lang.Text("Update.Task.ApplyFile"), (ModLoader.LoaderTask<string, int> _) =>
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			if (patchUpdate)
			{
				byte[] result = new BsDiff().ApplyAsync(ModBase.ReadFileBytes(Basics.ExecutablePath), ModBase.ReadFileBytes(tempPath)).GetAwaiter().GetResult();
				ModBase.WriteFile(output, result);
				return;
			}
			using FileStream stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using ZipArchive zipArchive = new ZipArchive(stream);
			ZipArchiveEntry zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry x) => x.Name.Contains("Plain Craft Launcher Ray Edition.exe")) ?? zipArchive.Entries.FirstOrDefault((ZipArchiveEntry x) => x.Name.Contains("Plain Craft Launcher"));
			if (zipArchiveEntry == null)
			{
				zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry x) => x.Name.Contains("Launcher"));
			}
			if (zipArchiveEntry == null)
			{
				zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry x) => x.Name.Contains(".exe"));
			}
			if (zipArchiveEntry == null)
			{
				throw new Exception(Lang.Text("Update.Error.FileNotFound"));
			}
			zipArchiveEntry.ExtractToFile(output, overwrite: true);
		}));
		return list;
	}

	private VersionDataModel GetChannelInfo(UpdateChannel channel, UpdateArch arch)
	{
		string channelName = GetChannelName(channel, arch);
		JsonNode remoteInfoByName = GetRemoteInfoByName("updates-" + channelName, "updates/");
		MinioUpdateAsset minioUpdateAsset = ((remoteInfoByName != null) ? JsonCompat.ToObject<MinioUpdateModel>(remoteInfoByName).Assets.FirstOrDefault() : null);
		if (minioUpdateAsset == null)
		{
			throw new NullReferenceException("Can not get remote update info!");
		}
		return new VersionDataModel
		{
			VersionName = minioUpdateAsset.Version.Name,
			VersionCode = minioUpdateAsset.Version.Code,
			Sha256 = minioUpdateAsset.Sha256,
			Source = SourceName,
			Changelog = minioUpdateAsset.Changelog
		};
	}

	private JsonNode GetRemoteInfoByName(string name, string path = "")
	{
		string filePath = Path.Combine(ModBase.pathTemp, "Cache", "Update", name + ".json");
		JsonNode json;
		if (IsCacheValid(name + ".json", _remoteCache[name]))
		{
			json = ModBase.GetJson(ModBase.ReadFile(filePath));
		}
		else
		{
			string text = HttpResponseExtension.AsString(HttpSenderExtension.SendAsync(HttpRequest.Create($"{_baseUrl}apiv2/{path}{name}.json"), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult());
			json = ModBase.GetJson(text);
			ModBase.WriteFile(filePath, text);
		}
		return json;
	}

	private bool IsCacheValid(string path, string hash)
	{
		string text = Path.Combine(ModBase.pathTemp, "Cache", "Update", path);
		FileInfo fileInfo = new FileInfo(text);
		if (fileInfo.Exists && (DateTime.Now - fileInfo.LastWriteTime).TotalHours < 1.0)
		{
			return (ModBase.GetFileMD5(text) ?? "") == (hash ?? "");
		}
		return false;
	}

	private string GetChannelName(UpdateChannel channel, UpdateArch arch)
	{
		string empty = string.Empty;
		empty = channel switch
		{
			UpdateChannel.stable => empty + "sr", 
			UpdateChannel.beta => empty + "fr", 
			_ => empty + "sr", 
		};
		return arch switch
		{
			UpdateArch.x64 => empty + "x64", 
			UpdateArch.arm64 => empty + "arm64", 
			_ => empty + "x64", 
		};
	}
}
