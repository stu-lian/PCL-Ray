using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;

namespace PCL;

public static class ModJarInJar
{
	internal sealed class ScanContext
	{
		private long _bytesRemaining = 2147483648L;

		private readonly Func<bool>? _isCancelled;

		public bool IsCancelled => _isCancelled?.Invoke() ?? false;

		public ScanContext(Func<bool>? isCancelled = null)
		{
			_isCancelled = isCancelled;
		}

		public bool TryConsume(int count)
		{
			if (!IsCancelled)
			{
				return Interlocked.Add(ref _bytesRemaining, -count) >= 0;
			}
			return false;
		}
	}

	private sealed class _ResolveBudget
	{
		public int NodesRemaining = 512;

		public long BytesRemaining = 536870912L;

		public bool Incomplete;

		public ScanContext? Scan;

		public string? PreferredLoader;
	}

	private sealed class _NestedJarInfo
	{
		public string Path;

		public string Identifier;

		public string VersionRange;

		public string ArtifactVersion;
	}

	internal const int MaxDepth = 5;

	internal const int MaxNodes = 512;

	private const long MaxEntryBytes = 268435456L;

	private const long MaxTotalBytes = 536870912L;

	private const long MaxScanBytes = 2147483648L;

	internal static List<ModLocalComp.LocalCompFile> ResolveCached(string modFilePath, ZipArchive jar, out bool complete, bool deferOnMiss = false, long? expectedLastModified = null, long? expectedSize = null, ScanContext? scan = null, string? preferredLoader = null)
	{
		complete = true;
		long lastModified;
		long size;
		try
		{
			FileInfo fileInfo = new FileInfo(modFilePath);
			lastModified = expectedLastModified ?? fileInfo.LastWriteTimeUtc.Ticks;
			size = expectedSize ?? fileInfo.Length;
			if (!_FingerprintMatches(fileInfo, lastModified, size))
			{
				complete = false;
				return null;
			}
		}
		catch
		{
			if (deferOnMiss)
			{
				complete = false;
				return null;
			}
			_ResolveBudget resolveBudget = new _ResolveBudget
			{
				Scan = scan,
				PreferredLoader = preferredLoader
			};
			List<ModLocalComp.LocalCompFile> result = _Resolve(modFilePath, jar, 0, resolveBudget);
			complete = !resolveBudget.Incomplete;
			return result;
		}
		List<EmbeddedModNode> list = ModJarInJarCache.TryGet(modFilePath, lastModified, size);
		if (list != null)
		{
			return _FromNodes(list, modFilePath);
		}
		if (deferOnMiss)
		{
			complete = false;
			return null;
		}
		_ResolveBudget resolveBudget2 = new _ResolveBudget
		{
			Scan = scan,
			PreferredLoader = preferredLoader
		};
		List<ModLocalComp.LocalCompFile> list2 = _Resolve(modFilePath, jar, 0, resolveBudget2);
		complete = !resolveBudget2.Incomplete;
		try
		{
			if (!_FingerprintMatches(new FileInfo(modFilePath), lastModified, size))
			{
				complete = false;
			}
		}
		catch
		{
			complete = false;
		}
		if (complete)
		{
			try
			{
				ModJarInJarCache.Set(modFilePath, lastModified, size, _ToNodes(list2));
			}
			catch (Exception ex)
			{
				complete = false;
				ModBase.Log(ex, "写入 Jar-in-Jar 缓存条目失败", ModBase.LogLevel.Developer);
			}
		}
		return list2;
	}

	private static bool _FingerprintMatches(FileInfo file, long lastModified, long size)
	{
		file.Refresh();
		if (file.Exists && file.LastWriteTimeUtc.Ticks == lastModified)
		{
			return file.Length == size;
		}
		return false;
	}

	private static List<EmbeddedModNode> _ToNodes(List<ModLocalComp.LocalCompFile> mods)
	{
		return mods.Select((ModLocalComp.LocalCompFile m) => new EmbeddedModNode
		{
			FileName = m.FileName,
			Name = m.Name,
			ModId = m.ModId,
			Version = m.Version,
			Loader = m.JijLoader,
			TargetMcVersion = m.JijTargetMcVersion,
			DependencyRows = m.DependencyDeclarations.Select((EmbeddedDependency d) => new EmbeddedDependency
			{
				Id = d.Id,
				Raw = d.Raw,
				Optional = d.Optional
			}).ToList(),
			Conflicts = m.ConflictDeclarations.Select((EmbeddedConflict c) => new EmbeddedConflict
			{
				DeclarerId = c.DeclarerId,
				Target = c.Target,
				Raw = c.Raw,
				Hard = c.Hard
			}).ToList(),
			ProvidedIds = m.ProvidedIds.ToList(),
			ProvidedVersions = new Dictionary<string, string>(m.ProvidedVersions),
			JijIdentifier = m.JijIdentifier,
			JijVersionRange = m.JijVersionRange,
			JijArtifactVersion = m.JijArtifactVersion,
			Children = _ToNodes(m.EmbeddedMods)
		}).ToList();
	}

	private static List<ModLocalComp.LocalCompFile> _FromNodes(List<EmbeddedModNode> nodes, string parentPath)
	{
		List<ModLocalComp.LocalCompFile> list = new List<ModLocalComp.LocalCompFile>();
		if (nodes == null)
		{
			return list;
		}
		foreach (EmbeddedModNode node in nodes)
		{
			string text = parentPath + "!/" + node.FileName;
			ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(text);
			localCompFile.SetJijMetadata(node.Name, node.ModId, node.Version);
			localCompFile.JijLoader = node.Loader;
			localCompFile.JijTargetMcVersion = node.TargetMcVersion;
			localCompFile.JijIdentifier = node.JijIdentifier;
			localCompFile.JijVersionRange = node.JijVersionRange;
			localCompFile.JijArtifactVersion = node.JijArtifactVersion;
			localCompFile.SetJijDependencies(node.DependencyRows);
			localCompFile.SetJijConflicts(node.Conflicts, node.ProvidedIds, node.ProvidedVersions);
			localCompFile.EmbeddedMods = _FromNodes(node.Children, text);
			list.Add(localCompFile);
		}
		return list;
	}

	private static List<ModLocalComp.LocalCompFile> _Resolve(string parentPath, ZipArchive jar, int depth, _ResolveBudget budget)
	{
		List<ModLocalComp.LocalCompFile> list = new List<ModLocalComp.LocalCompFile>();
		ScanContext? scan = budget.Scan;
		if (scan != null && scan.IsCancelled)
		{
			budget.Incomplete = true;
			return list;
		}
		List<_NestedJarInfo> list2 = new List<_NestedJarInfo>();
		if (!_CollectFabricNestedJars(jar, list2))
		{
			budget.Incomplete = true;
		}
		if (!_CollectQuiltNestedJars(jar, list2))
		{
			budget.Incomplete = true;
		}
		if (!_CollectForgeNestedJars(jar, list2))
		{
			budget.Incomplete = true;
		}
		if (!_CollectManifestEmbeddedJars(jar, list2))
		{
			budget.Incomplete = true;
		}
		list2 = (from g in list2.Where((_NestedJarInfo n) => !string.IsNullOrWhiteSpace(n.Path)).GroupBy((_NestedJarInfo n) => n.Path, StringComparer.Ordinal)
			select g.OrderByDescending((_NestedJarInfo n) => n.Identifier != null).First()).ToList();
		if (depth >= 5)
		{
			if (list2.Count > 0)
			{
				budget.Incomplete = true;
			}
			return list;
		}
		foreach (_NestedJarInfo item in list2)
		{
			ScanContext? scan2 = budget.Scan;
			if (scan2 != null && scan2.IsCancelled)
			{
				budget.Incomplete = true;
				break;
			}
			if (budget.NodesRemaining <= 0)
			{
				budget.Incomplete = true;
				break;
			}
			ZipArchiveEntry entry = jar.GetEntry(item.Path);
			if (entry == null)
			{
				continue;
			}
			try
			{
				ModBase.GetFileNameFromPath(item.Path);
			}
			catch (Exception ex)
			{
				budget.Incomplete = true;
				ModBase.Log(ex, "跳过路径无效的内嵌 Mod（" + parentPath + " -> " + item.Path + "）", ModBase.LogLevel.Developer);
				continue;
			}
			if (entry.Length > 268435456 || entry.Length > budget.BytesRemaining)
			{
				budget.Incomplete = true;
				ModBase.Log("跳过过大的内嵌 Mod（" + parentPath + " -> " + item.Path + "，" + entry.Length + " bytes）", ModBase.LogLevel.Developer);
				continue;
			}
			budget.NodesRemaining--;
			string text = parentPath + "!/" + item.Path;
			ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(text);
			localCompFile.MarkLoaded();
			localCompFile.JijIdentifier = item.Identifier;
			localCompFile.JijVersionRange = item.VersionRange;
			localCompFile.JijArtifactVersion = item.ArtifactVersion;
			list.Add(localCompFile);
			string text2 = null;
			try
			{
				text2 = Path.GetTempFileName();
				using (Stream input = entry.Open())
				{
					using FileStream output = File.Create(text2);
					_CopyWithBudget(input, output, budget);
				}
				using ZipArchive jar2 = ZipFile.OpenRead(text2);
				localCompFile.JijLoader = DetectLoader(jar2, budget.PreferredLoader);
				localCompFile.PreferredLoader = budget.PreferredLoader;
				localCompFile.LookupMetadata(jar2, localCompFile.JijLoader);
				localCompFile.JijTargetMcVersion = (localCompFile.DependencyRaw.TryGetValue("minecraft", out string value) ? value : null);
				localCompFile.EmbeddedMods = _Resolve(text, jar2, depth + 1, budget);
			}
			catch (OperationCanceledException)
			{
				budget.Incomplete = true;
				throw;
			}
			catch (Exception ex3)
			{
				budget.Incomplete = true;
				ModBase.Log(ex3, "解析内嵌 Mod 失败（" + parentPath + " -> " + item.Path + "）", ModBase.LogLevel.Developer);
			}
			finally
			{
				if (text2 != null)
				{
					try
					{
						File.Delete(text2);
					}
					catch
					{
					}
				}
			}
		}
		return list;
	}

	private static void _CopyWithBudget(Stream input, Stream output, _ResolveBudget budget)
	{
		byte[] array = new byte[81920];
		long num = 0L;
		while (true)
		{
			int num2 = input.Read(array, 0, array.Length);
			if (num2 <= 0)
			{
				return;
			}
			num += num2;
			budget.BytesRemaining -= num2;
			ScanContext? scan = budget.Scan;
			if (scan != null && scan.IsCancelled)
			{
				budget.Incomplete = true;
				throw new OperationCanceledException("Jar-in-Jar scan cancelled");
			}
			if (num > 268435456 || budget.BytesRemaining < 0 || (budget.Scan != null && !budget.Scan.TryConsume(num2)))
			{
				break;
			}
			output.Write(array, 0, num2);
		}
		budget.Incomplete = true;
		throw new InvalidDataException("内嵌 Mod 解压体积超过安全上限");
	}

	private static bool _CollectFabricNestedJars(ZipArchive jar, List<_NestedJarInfo> found)
	{
		try
		{
			ZipArchiveEntry entry = jar.GetEntry("fabric.mod.json");
			if (entry == null)
			{
				return true;
			}
			if (((JsonObject)ModBase.GetJson(ModBase.ReadFile(entry.Open()))).TryGetPropertyValue("jars", out JsonNode jsonNode) && jsonNode is JsonArray jsonArray)
			{
				foreach (JsonNode item in jsonArray)
				{
					if (item is JsonObject jsonObject && jsonObject.TryGetPropertyValue("file", out JsonNode jsonNode2) && jsonNode2 != null)
					{
						found.Add(new _NestedJarInfo
						{
							Path = jsonNode2.ToString()
						});
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "解析 fabric.mod.json 内嵌清单失败", ModBase.LogLevel.Developer);
			return false;
		}
	}

	private static bool _CollectForgeNestedJars(ZipArchive jar, List<_NestedJarInfo> found)
	{
		try
		{
			ZipArchiveEntry entry = jar.GetEntry("META-INF/jarjar/metadata.json");
			if (entry == null)
			{
				return true;
			}
			if (((JsonObject)ModBase.GetJson(ModBase.ReadFile(entry.Open()))).TryGetPropertyValue("jars", out JsonNode jsonNode) && jsonNode is JsonArray jsonArray)
			{
				foreach (JsonNode item in jsonArray)
				{
					if (item is JsonObject jsonObject && jsonObject.TryGetPropertyValue("path", out JsonNode jsonNode2) && jsonNode2 != null)
					{
						JsonObject obj = jsonObject["identifier"] as JsonObject;
						JsonObject jsonObject2 = jsonObject["version"] as JsonObject;
						string text = obj?["group"]?.ToString();
						string text2 = obj?["artifact"]?.ToString();
						found.Add(new _NestedJarInfo
						{
							Path = jsonNode2.ToString(),
							Identifier = ((string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2)) ? null : (text + ":" + text2)),
							VersionRange = jsonObject2?["range"]?.ToString(),
							ArtifactVersion = jsonObject2?["artifactVersion"]?.ToString()
						});
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "解析 META-INF/jarjar/metadata.json 内嵌清单失败", ModBase.LogLevel.Developer);
			return false;
		}
	}

	private static bool _CollectQuiltNestedJars(ZipArchive jar, List<_NestedJarInfo> found)
	{
		try
		{
			ZipArchiveEntry entry = jar.GetEntry("quilt.mod.json");
			if (entry == null)
			{
				return true;
			}
			if (((JsonObject)ModBase.GetJson(ModBase.ReadFile(entry.Open()))).TryGetPropertyValue("quilt_loader", out JsonNode jsonNode) && jsonNode is JsonObject jsonObject && jsonObject.TryGetPropertyValue("jars", out JsonNode jsonNode2) && jsonNode2 is JsonArray jsonArray)
			{
				foreach (JsonNode item in jsonArray)
				{
					string text = item?.ToString();
					if (!string.IsNullOrEmpty(text))
					{
						found.Add(new _NestedJarInfo
						{
							Path = text
						});
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "解析 quilt.mod.json 内嵌清单失败", ModBase.LogLevel.Developer);
			return false;
		}
	}

	private static bool _CollectManifestEmbeddedJars(ZipArchive jar, List<_NestedJarInfo> found)
	{
		try
		{
			ZipArchiveEntry entry = jar.GetEntry("META-INF/MANIFEST.MF");
			if (entry == null)
			{
				return true;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = ModBase.ReadFile(entry.Open()).Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].TrimEnd('\r');
				if (text.StartsWith(" "))
				{
					stringBuilder.Append(text, 1, text.Length - 1);
				}
				else
				{
					stringBuilder.Append('\n').Append(text);
				}
			}
			array = stringBuilder.ToString().Split('\n');
			foreach (string text2 in array)
			{
				if (text2.StartsWith("Embedded-Dependencies-Mod:", StringComparison.OrdinalIgnoreCase))
				{
					string text3 = text2.Substring("Embedded-Dependencies-Mod:".Length).Trim();
					if (!string.IsNullOrEmpty(text3))
					{
						found.Add(new _NestedJarInfo
						{
							Path = text3
						});
					}
					return true;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "解析 MANIFEST.MF 内嵌声明失败", ModBase.LogLevel.Developer);
			return false;
		}
	}

	public static string? DetectLoader(ZipArchive jar, string? preferredLoader = null)
	{
		bool flag = jar.GetEntry("fabric.mod.json") != null;
		bool flag2 = jar.GetEntry("quilt.mod.json") != null;
		bool flag3 = jar.GetEntry("META-INF/neoforge.mods.toml") != null;
		bool flag4 = jar.GetEntry("META-INF/mods.toml") != null || jar.GetEntry("mcmod.info") != null;
		if (string.Equals(preferredLoader, "NeoForge", StringComparison.OrdinalIgnoreCase) & flag3)
		{
			return "NeoForge";
		}
		if (string.Equals(preferredLoader, "Forge", StringComparison.OrdinalIgnoreCase) & flag4)
		{
			return "Forge";
		}
		if (string.Equals(preferredLoader, "Fabric", StringComparison.OrdinalIgnoreCase) & flag)
		{
			return "Fabric";
		}
		if (string.Equals(preferredLoader, "Quilt", StringComparison.OrdinalIgnoreCase) & flag2)
		{
			return "Quilt";
		}
		if (flag)
		{
			return "Fabric";
		}
		if (flag2)
		{
			return "Quilt";
		}
		if (flag3)
		{
			return "NeoForge";
		}
		if (flag4)
		{
			return "Forge";
		}
		return null;
	}
}
