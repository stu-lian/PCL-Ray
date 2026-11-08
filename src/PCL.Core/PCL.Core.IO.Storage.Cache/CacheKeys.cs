using System;
using System.Security.Cryptography;
using System.Text;

namespace PCL.Core.IO.Storage.Cache;

public class CacheKeys
{
	public static string InstanceMeta(string instancePath)
	{
		return _Build("instance", "meta", _HashSegment(instancePath));
	}

	public static string InstanceManifest(string instancePath, string versionId)
	{
		return _Build("instance", "manifest", _HashSegment(instancePath), versionId);
	}

	public static string InstanceComponents(string instancePath, string compType)
	{
		return _Build("instance", "components", _HashSegment(instancePath), compType);
	}

	public static string InstanceComponentFile(string instancePath, string fileHash)
	{
		return _Build("instance", "component", _HashSegment(instancePath), fileHash);
	}

	public static string Download(string url)
	{
		return _Build("download", _HashSegment(url));
	}

	public static string Library(string mavenGroup, string artifact, string version)
	{
		return _Build("library", mavenGroup, artifact, version);
	}

	public static string AssetIndex(string url)
	{
		return _Build("assets", "index", _HashSegment(url));
	}

	public static string AssetObject(string assetHash)
	{
		return _Build("assets", "object", assetHash);
	}

	public static string ApiResponse(string source, string url)
	{
		return _Build("http", source, _HashSegment(url));
	}

	public static string ApiResponseMeta(string source, string url)
	{
		return _Build("http", "meta", source, _HashSegment(url));
	}

	public static string CompSearch(string source, string query, int page)
	{
		return _Build("comp", "search", source, _HashSegment(query), page.ToString());
	}

	public static string Skin(string url)
	{
		return _Build("skin", _HashSegment(url));
	}

	public static string Image(string url)
	{
		return _Build("image", _HashSegment(url));
	}

	public static string News(string url)
	{
		return _Build("news", _HashSegment(url));
	}

	public static string AuthToken(string accountId)
	{
		return _Build("auth", "token", accountId);
	}

	public static string AccountList()
	{
		return "accounts:list";
	}

	private static string _Build(params string[] segments)
	{
		return string.Join(':', segments);
	}

	private static string _HashSegment(string raw)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant().Substring(0, 32);
	}
}
