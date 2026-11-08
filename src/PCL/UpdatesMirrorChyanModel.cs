using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public class UpdatesMirrorChyanModel : IUpdateSource
{
	private const string mirrorChyanBaseUrl = "https://mirrorchyan.com/api/resources/{cid}/latest?cdk={cdk}&os=win&arch={arch}&channel={channel}";

	private const string myCid = "PCL2-CE";

	public string SourceName { get; set; } = "MirrorChyan";

	public bool IsAvailable()
	{
		return !string.IsNullOrWhiteSpace(Config.Update.MirrorChyanKey);
	}

	public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
	{
		using HttpResponseMessage httpResponseMessage = HttpSenderExtension.SendAsync(HttpRequest.Create(GetUrl(channel, arch)), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult();
		JsonObject jsonObject = (JsonObject)ModBase.GetJson(HttpResponseExtension.AsString(httpResponseMessage));
		if ((int)jsonObject["code"] != 0)
		{
			throw new Exception("Mirror 酱获取数据不成功");
		}
		JsonNode jsonNode = jsonObject["data"];
		string value = jsonNode["url"]?.ToString();
		if (jsonNode != null && string.IsNullOrWhiteSpace(value))
		{
			throw new Exception("无效 CDK");
		}
		return new VersionDataModel
		{
			Source = SourceName,
			VersionCode = (int)jsonNode["version_number"],
			VersionName = (string?)jsonNode["version_name"],
			Sha256 = (string?)jsonNode["sha256"],
			Changelog = (string?)jsonNode["release_note"]
		};
	}

	public bool RefreshCache()
	{
		return true;
	}

	public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		VersionDataModel latestVersion = GetLatestVersion(channel, arch);
		return currentVersion >= SemVer.Parse(latestVersion.VersionName);
	}

	public VersionAnnouncementDataModel GetAnnouncementList()
	{
		throw new Exception("Mirror 酱无公告系统");
	}

	public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
	{
		return new List<ModLoader.LoaderBase>
		{
			new ModLoader.LoaderTask<int, List<DownloadFile>>(Lang.Text("Update.Task.GetDownloadInfo"), (ModLoader.LoaderTask<int, List<DownloadFile>> load) =>
			{
				string text = ((JsonObject)Requester.FetchJson(GetUrl(channel, arch), RequestParam.WithRetry))["data"]["url"]?.ToString();
				if (text == null)
				{
					throw new Exception("Mirror 酱下载源不可用");
				}
				load.output = new List<DownloadFile>
				{
					new DownloadFile(new string[1] { text }, output)
				};
			}),
			new LoaderDownload(Lang.Text("Update.Task.DownloadUpdateFile"), new List<DownloadFile>())
		};
	}

	private string GetUrl(UpdateChannel channel, UpdateArch arch)
	{
		return "https://mirrorchyan.com/api/resources/{cid}/latest?cdk={cdk}&os=win&arch={arch}&channel={channel}".Replace("{cid}", "PCL2-CE").Replace("{cdk}", Config.Update.MirrorChyanKey).Replace("{arch}", arch.ToString())
			.Replace("{channel}", channel.ToString());
	}
}
