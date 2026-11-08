using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

/// <summary>
/// Update source backed by this project's own GitHub Releases.
/// </summary>
public class UpdatesGitHubModel : IUpdateSource
{
	private readonly string _owner;

	private readonly string _repo;

	private JsonArray _releases;

	public string SourceName { get; set; } = "GitHub Releases";

	public UpdatesGitHubModel(string owner, string repo)
	{
		_owner = owner;
		_repo = repo;
	}

	public bool IsAvailable()
	{
		return !string.IsNullOrWhiteSpace(_owner) && !string.IsNullOrWhiteSpace(_repo);
	}

	public bool RefreshCache()
	{
		_releases = Requester.FetchJson($"https://api.github.com/repos/{_owner}/{_repo}/releases?per_page=100", new RequestParam
		{
			Retries = 3,
			UseBrowserUserAgent = true
		}) as JsonArray;
		return _releases != null;
	}

	private JsonObject FindRelease(UpdateChannel channel)
	{
		if (_releases == null)
		{
			RefreshCache();
		}
		if (_releases == null)
		{
			throw new Exception("No releases can be fetched!");
		}
		JsonObject best = null;
		SemVer bestVersion = null;
		foreach (JsonNode node in _releases)
		{
			if (node is not JsonObject release)
			{
				continue;
			}
			if (release["draft"]?.GetValue<bool>() == true)
			{
				continue;
			}
			bool prerelease = release["prerelease"]?.GetValue<bool>() == true;
			// Only the stable channel is offered; skip pre-releases.
			if (channel == UpdateChannel.stable && prerelease)
			{
				continue;
			}
			if (release["assets"] is not JsonArray assets || assets.Count == 0)
			{
				continue;
			}
			// 不能依赖 API 返回顺序：按 tag 版本号取最高的一条。
			SemVer version = null;
			try
			{
				version = SemVer.Parse(GetTagName(release));
			}
			catch
			{
				// 无法解析的 tag 仅在没有任何可解析版本时作为兜底。
			}
			if (best == null)
			{
				best = release;
				bestVersion = version;
				continue;
			}
			if (version != null && (bestVersion == null || version > bestVersion))
			{
				best = release;
				bestVersion = version;
			}
		}
		if (best == null)
		{
			throw new Exception(Lang.Text("Update.Task.GetVersionInfoFailed"));
		}
		return best;
	}

	private static string GetAssetUrl(JsonObject release)
	{
		if (release["assets"] is JsonArray assets)
		{
			foreach (JsonNode assetNode in assets)
			{
				if (assetNode is not JsonObject asset)
				{
					continue;
				}
				string name = asset["name"]?.ToString() ?? "";
				if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				{
					return asset["browser_download_url"]?.ToString();
				}
			}
		}
		throw new Exception(Lang.Text("Update.Error.FileNotFound"));
	}

	private static string GetTagName(JsonObject release)
	{
		string tag = release["tag_name"]?.ToString() ?? "";
		if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
		{
			tag = tag.Substring(1);
		}
		return tag;
	}

	public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
	{
		JsonObject release = FindRelease(channel);
		string sha256 = "";
		if (release["assets"] is JsonArray assets)
		{
			foreach (JsonNode assetNode in assets)
			{
				if (assetNode is JsonObject asset && (asset["name"]?.ToString() ?? "").EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				{
					sha256 = asset["digest"]?.ToString() ?? "";
					break;
				}
			}
		}
		return new VersionDataModel
		{
			VersionName = GetTagName(release),
			VersionCode = 0,
			Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256.Replace("sha256:", ""),
			Source = SourceName,
			Changelog = release["body"]?.ToString() ?? ""
		};
	}

	public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		VersionDataModel latestVersion = GetLatestVersion(channel, arch);
		return currentVersion >= SemVer.Parse(latestVersion.VersionName);
	}

	public VersionAnnouncementDataModel GetAnnouncementList()
	{
		throw new Exception("GitHub Releases 无公告系统");
	}

	public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
	{
		JsonObject release = FindRelease(channel);
		string url = GetAssetUrl(release);
		return new List<ModLoader.LoaderBase>
		{
			new ModLoader.LoaderTask<int, List<DownloadFile>>(Lang.Text("Update.Task.GetDownloadInfo"), (ModLoader.LoaderTask<int, List<DownloadFile>> load) =>
			{
				load.output = new List<DownloadFile>
				{
					new DownloadFile(new string[1] { url }, output)
				};
			}),
			new LoaderDownload(Lang.Text("Update.Task.DownloadUpdateFile"), new List<DownloadFile>())
		};
	}
}
