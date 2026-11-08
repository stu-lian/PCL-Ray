using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

/// <summary>
/// 从 PCL Ray 自建公告后端（Cloudflare Worker）读取公告。
/// 接口返回格式与 VersionAnnouncementDataModel 一致：{ "content": [ ... ] }
/// </summary>
public class UpdatesNoticeModel : IUpdateSource
{
	private readonly string _url;

	private VersionAnnouncementDataModel _cache;

	public string SourceName { get; set; } = "PCL Ray 公告";

	public UpdatesNoticeModel(string url)
	{
		_url = url;
	}

	public bool IsAvailable()
	{
		return !string.IsNullOrWhiteSpace(_url);
	}

	public bool RefreshCache()
	{
		_cache = Fetch();
		return _cache != null;
	}

	private VersionAnnouncementDataModel Fetch()
	{
		string text = Requester.FetchString(_url, new RequestParam
		{
			Retries = 3,
			Timeout = 15000
		});
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		JsonNode jsonNode = ModBase.GetJson(text);
		return JsonCompat.ToObject<VersionAnnouncementDataModel>(jsonNode);
	}

	public VersionAnnouncementDataModel GetAnnouncementList()
	{
		VersionAnnouncementDataModel result = _cache ?? Fetch();
		if (result == null)
		{
			throw new Exception("无法获取公告内容");
		}
		result.Content ??= new List<VersionAnnouncementContentModel>();
		return result;
	}

	public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
	{
		throw new NotSupportedException("公告源不提供版本信息");
	}

	public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
	{
		throw new NotSupportedException("公告源不提供版本信息");
	}

	public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
	{
		throw new NotSupportedException("公告源不提供版本下载");
	}
}
