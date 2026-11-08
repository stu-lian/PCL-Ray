using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL;

public class ModJarInJarIndex
{
	public sealed class DepRow
	{
		public string DepId;

		public string Raw;

		public bool Optional;

		public string Loader;
	}

	private sealed class Provider
	{
		public ModLocalComp.LocalCompFile Host;

		public ModLocalComp.LocalCompFile Source;

		public string Version;
	}

	private readonly List<ModLocalComp.LocalCompFile> _allMods;

	private readonly Dictionary<string, List<Provider>> _providers = new Dictionary<string, List<Provider>>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<ModLocalComp.LocalCompFile, List<(string Id, string Version)>> _selfBundled = new Dictionary<ModLocalComp.LocalCompFile, List<(string, string)>>();

	private readonly Dictionary<ModLocalComp.LocalCompFile, List<DepRow>> _deps = new Dictionary<ModLocalComp.LocalCompFile, List<DepRow>>();

	private readonly Dictionary<ModLocalComp.LocalCompFile, List<ModLocalComp.LocalCompFile>> _loadableNodes = new Dictionary<ModLocalComp.LocalCompFile, List<ModLocalComp.LocalCompFile>>();

	private readonly Dictionary<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile> _nodeHosts = new Dictionary<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile>();

	private readonly Dictionary<(ModLocalComp.LocalCompFile Owner, string Id), ModLocalComp.LocalCompFile> _declaredSources = new Dictionary<(ModLocalComp.LocalCompFile, string), ModLocalComp.LocalCompFile>();

	public ModJarInJarIndex(IEnumerable<ModLocalComp.LocalCompFile> allMods, string mc)
	{
		_allMods = allMods.Where((ModLocalComp.LocalCompFile m) => !m.IsFolder && m.State != ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable).ToList();
		List<ModLocalComp.LocalCompFile> source = _allMods.Where((ModLocalComp.LocalCompFile m) => m.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine).ToList();
		Dictionary<ModLocalComp.LocalCompFile, List<ModLocalComp.LocalCompFile>> dictionary = source.ToDictionary((ModLocalComp.LocalCompFile m) => m, (ModLocalComp.LocalCompFile m) => _CollectLoadableNodes(m.EmbeddedMods, mc));
		Dictionary<string, string> forgeSelections = _SelectForgeJarJarVersions(dictionary.Values.SelectMany((List<ModLocalComp.LocalCompFile> n) => n));
		Dictionary<ModLocalComp.LocalCompFile, List<ModLocalComp.LocalCompFile>> nodesByHost = source.ToDictionary((ModLocalComp.LocalCompFile m) => m, (ModLocalComp.LocalCompFile m) => _CollectLoadableNodes(m.EmbeddedMods, mc, forgeSelections));
		Dictionary<string, string> fabricSelections = _SelectFabricVersions(nodesByHost, mc, forgeSelections);
		foreach (ModLocalComp.LocalCompFile allMod in _allMods)
		{
			List<ModLocalComp.LocalCompFile> list = ((allMod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? _CollectLoadableNodes(allMod.EmbeddedMods, mc, forgeSelections, fabricSelections) : _CollectLoadableNodes(allMod.EmbeddedMods, mc));
			_loadableNodes[allMod] = list;
			foreach (ModLocalComp.LocalCompFile item in list)
			{
				_nodeHosts[item] = allMod;
			}
			List<(string, string)> list2 = new List<(string, string)>();
			if (!string.IsNullOrWhiteSpace(allMod.ModId))
			{
				list2.Add((allMod.ModId, allMod.Version));
			}
			foreach (string item2 in allMod.ProvidedIds.Where((string id) => !string.IsNullOrWhiteSpace(id)))
			{
				list2.Add((item2, allMod.ProvidedVersions.TryGetValue(item2, out string value) ? value : allMod.Version));
			}
			foreach (ModLocalComp.LocalCompFile item3 in list)
			{
				if (!string.IsNullOrWhiteSpace(item3.ModId))
				{
					list2.Add((item3.ModId, item3.Version));
				}
				foreach (string item4 in item3.ProvidedIds.Where((string id) => !string.IsNullOrWhiteSpace(id)))
				{
					list2.Add((item4, item3.ProvidedVersions.TryGetValue(item4, out string value2) ? value2 : item3.Version));
				}
			}
			_selfBundled[allMod] = list2;
			if (!string.IsNullOrEmpty(allMod.ModId))
			{
				_RegisterSource(allMod, allMod.ModId, allMod);
				_AddProvider(allMod.ModId, allMod, allMod, allMod.Version);
			}
			foreach (string providedId in allMod.ProvidedIds)
			{
				bool flag = allMod.ProvidedVersions.TryGetValue(providedId, out string value3);
				ModLocalComp.LocalCompFile source2 = (flag ? _CreateSiblingSource(allMod, providedId, value3) : allMod);
				_RegisterSource(allMod, providedId, source2);
				_AddProvider(providedId, allMod, source2, flag ? value3 : allMod.Version);
			}
			foreach (ModLocalComp.LocalCompFile item5 in list.Where((ModLocalComp.LocalCompFile n) => !string.IsNullOrEmpty(n.ModId)))
			{
				_RegisterSource(item5, item5.ModId, item5);
				_AddProvider(item5.ModId, allMod, item5, item5.Version);
			}
			foreach (ModLocalComp.LocalCompFile item6 in list)
			{
				foreach (string providedId2 in item6.ProvidedIds)
				{
					bool flag2 = item6.ProvidedVersions.TryGetValue(providedId2, out string value4);
					ModLocalComp.LocalCompFile source3 = (flag2 ? _CreateSiblingSource(item6, providedId2, value4) : item6);
					_RegisterSource(item6, providedId2, source3);
					_AddProvider(providedId2, allMod, source3, flag2 ? value4 : item6.Version);
				}
			}
			List<DepRow> list3 = new List<DepRow>();
			foreach (EmbeddedDependency dependencyDeclaration in allMod.DependencyDeclarations)
			{
				list3.Add(new DepRow
				{
					DepId = dependencyDeclaration.Id,
					Raw = dependencyDeclaration.Raw,
					Optional = dependencyDeclaration.Optional,
					Loader = allMod.DetectedLoader
				});
			}
			foreach (ModLocalComp.LocalCompFile item7 in list)
			{
				foreach (EmbeddedDependency dependencyDeclaration2 in item7.DependencyDeclarations)
				{
					list3.Add(new DepRow
					{
						DepId = dependencyDeclaration2.Id,
						Raw = dependencyDeclaration2.Raw,
						Optional = dependencyDeclaration2.Optional,
						Loader = item7.JijLoader
					});
				}
			}
			_deps[allMod] = (from r in list3
				group r by (DepId: r.DepId, Raw: r.Raw, Loader: r.Loader, Optional: r.Optional) into g
				select g.First()).ToList();
		}
	}

	public IReadOnlyList<DepRow> GetDependencies(ModLocalComp.LocalCompFile mod)
	{
		if (!_deps.TryGetValue(mod, out List<DepRow> value))
		{
			return new List<DepRow>();
		}
		return value;
	}

	public IReadOnlyList<ModLocalComp.LocalCompFile> GetLoadableEmbedded(ModLocalComp.LocalCompFile host)
	{
		if (!_loadableNodes.TryGetValue(host, out List<ModLocalComp.LocalCompFile> value))
		{
			return new List<ModLocalComp.LocalCompFile>();
		}
		return value;
	}

	public IReadOnlyList<string> GetProviderVersions(ModLocalComp.LocalCompFile dependent, string depId)
	{
		if (!_providers.TryGetValue(_Norm(depId), out List<Provider> value))
		{
			return Array.Empty<string>();
		}
		return (from provider in value
			where provider.Source != dependent
			select provider.Version into version
			where !string.IsNullOrWhiteSpace(version)
			select version).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	public static List<DepRow> BuildOwnDependencies(ModLocalComp.LocalCompFile mod, string loader)
	{
		return mod.DependencyDeclarations.Select((EmbeddedDependency declaration) => new DepRow
		{
			DepId = declaration.Id,
			Raw = declaration.Raw,
			Optional = declaration.Optional,
			Loader = loader
		}).ToList();
	}

	private static string _Norm(string id)
	{
		return id?.ToLowerInvariant();
	}

	private static bool _VersionSatisfies(DepRow dep, string providerVersion)
	{
		if (dep.Raw == null)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(providerVersion))
		{
			return true;
		}
		string text = McConstraintMatcher.StripV(providerVersion.Trim());
		if (text.Length == 0 || text.Contains("$"))
		{
			return true;
		}
		if (McConstraintMatcher.Satisfies(dep.Raw, dep.Loader, text))
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(dep.Loader))
		{
			return false;
		}
		return !McConstraintMatcher.HasComparableBound(dep.Raw);
	}

	private bool _SelfBundleSatisfies(ModLocalComp.LocalCompFile mod, DepRow dep)
	{
		if (_selfBundled.TryGetValue(mod, out List<(string, string)> value) && value.Any(((string Id, string Version) x) => string.Equals(_Norm(x.Id), _Norm(dep.DepId), StringComparison.OrdinalIgnoreCase) && _VersionSatisfies(dep, x.Version)))
		{
			return true;
		}
		return false;
	}

	public JijDepStatus Analyze(ModLocalComp.LocalCompFile mod, DepRow dep)
	{
		if (_SelfBundleSatisfies(mod, dep) || (_nodeHosts.TryGetValue(mod, out ModLocalComp.LocalCompFile value) && _SelfBundleSatisfies(value, dep)))
		{
			return JijDepStatus.Bundled;
		}
		_providers.TryGetValue(_Norm(dep.DepId), out List<Provider> value2);
		List<Provider> list = value2?.Where((Provider p) => p.Source != mod).ToList() ?? new List<Provider>();
		List<Provider> list2 = list.Where((Provider p) => _VersionSatisfies(dep, p.Version)).ToList();
		if (list2.Any((Provider p) => p.Host.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine))
		{
			return JijDepStatus.Installed;
		}
		if (list2.Count > 0)
		{
			return JijDepStatus.Disabled;
		}
		if (list.Count > 0)
		{
			return JijDepStatus.VersionMismatch;
		}
		return JijDepStatus.Missing;
	}

	public List<(ModLocalComp.LocalCompFile A, ModLocalComp.LocalCompFile B, bool Hard)> FindActiveConflicts()
	{
		Dictionary<ModLocalComp.LocalCompFile, int> dictionary = new Dictionary<ModLocalComp.LocalCompFile, int>();
		foreach (ModLocalComp.LocalCompFile allMod in _allMods)
		{
			dictionary[allMod] = dictionary.Count;
			foreach (ModLocalComp.LocalCompFile item in _loadableNodes[allMod])
			{
				if (!dictionary.ContainsKey(item))
				{
					dictionary[item] = dictionary.Count;
				}
			}
		}
		foreach (ModLocalComp.LocalCompFile item2 in (from p in _providers.Values.SelectMany((List<Provider> p) => p)
			select p.Source).Distinct())
		{
			if (!dictionary.ContainsKey(item2))
			{
				dictionary[item2] = dictionary.Count;
			}
		}
		Dictionary<(ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile), bool> dictionary2 = new Dictionary<(ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile), bool>();
		foreach (ModLocalComp.LocalCompFile allMod2 in _allMods)
		{
			if (allMod2.State != ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
			{
				continue;
			}
			_CollectConflicts(allMod2, allMod2.ConflictDeclarations, allMod2.DetectedLoader, dictionary, dictionary2);
			foreach (ModLocalComp.LocalCompFile item3 in _loadableNodes[allMod2])
			{
				_CollectConflicts(item3, item3.ConflictDeclarations, item3.JijLoader, dictionary, dictionary2);
			}
		}
		return dictionary2.Select((KeyValuePair<(ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile), bool> kv) => (kv.Key.Item1, kv.Key.Item2, Value: kv.Value)).ToList();
	}

	private void _CollectConflicts(ModLocalComp.LocalCompFile declarerSource, IEnumerable<EmbeddedConflict> conflicts, string loader, Dictionary<ModLocalComp.LocalCompFile, int> order, Dictionary<(ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile), bool> pairs)
	{
		foreach (EmbeddedConflict conflict in conflicts)
		{
			ModLocalComp.LocalCompFile localCompFile = _ResolveConflictSource(declarerSource, conflict.DeclarerId);
			if (!_providers.TryGetValue(_Norm(conflict.Target), out List<Provider> value))
			{
				continue;
			}
			string raw = conflict.Raw;
			bool hard = conflict.Hard;
			string text = raw;
			foreach (Provider item in value)
			{
				if (item.Source == localCompFile || item.Host.State != ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
				{
					continue;
				}
				if (text != null)
				{
					string text2 = McConstraintMatcher.StripV(item.Version?.Trim() ?? "");
					if (text2.Length == 0 || text2.Contains("$") || !McConstraintMatcher.Satisfies(text, loader, text2))
					{
						continue;
					}
				}
				(ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile) key = ((order[localCompFile] < order[item.Source]) ? (localCompFile, item.Source) : (item.Source, localCompFile));
				pairs[key] = (pairs.TryGetValue(key, out var value2) ? (value2 | hard) : hard);
			}
		}
	}

	public List<ModLocalComp.LocalCompFile> FindAffected(IEnumerable<ModLocalComp.LocalCompFile> targets)
	{
		HashSet<ModLocalComp.LocalCompFile> targetSet = new HashSet<ModLocalComp.LocalCompFile>(targets);
		HashSet<ModLocalComp.LocalCompFile> removal = new HashSet<ModLocalComp.LocalCompFile>(targetSet);
		bool flag;
		do
		{
			flag = false;
			foreach (ModLocalComp.LocalCompFile allMod in _allMods)
			{
				if (allMod.State != ModLocalComp.LocalCompFile.LocalFileStatus.Fine || removal.Contains(allMod))
				{
					continue;
				}
				foreach (DepRow dep in GetDependencies(allMod))
				{
					if (!ModDependencyIds.IsPlatform(dep.DepId) && !dep.Optional && !_SelfBundleSatisfies(allMod, dep) && _providers.TryGetValue(_Norm(dep.DepId), out List<Provider> value))
					{
						List<Provider> list = value.Where((Provider p) => p.Host.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && _VersionSatisfies(dep, p.Version)).ToList();
						if (list.Count != 0 && list.All((Provider p) => removal.Contains(p.Host)))
						{
							removal.Add(allMod);
							flag = true;
						}
					}
				}
			}
		}
		while (flag);
		return removal.Where((ModLocalComp.LocalCompFile m) => !targetSet.Contains(m)).ToList();
	}

	private void _AddProvider(string id, ModLocalComp.LocalCompFile host, ModLocalComp.LocalCompFile source, string version)
	{
		id = _Norm(id);
		if (!_providers.TryGetValue(id, out List<Provider> value))
		{
			value = new List<Provider>();
			_providers[id] = value;
		}
		if (!value.Any((Provider p) => p.Host == host && p.Source == source && p.Version == version))
		{
			value.Add(new Provider
			{
				Host = host,
				Source = source,
				Version = version
			});
		}
	}

	private void _RegisterSource(ModLocalComp.LocalCompFile owner, string id, ModLocalComp.LocalCompFile source)
	{
		if (!string.IsNullOrWhiteSpace(id))
		{
			_declaredSources[(owner, _Norm(id))] = source;
		}
	}

	private ModLocalComp.LocalCompFile _ResolveConflictSource(ModLocalComp.LocalCompFile owner, string declarerId)
	{
		if (string.IsNullOrWhiteSpace(declarerId) || !_declaredSources.TryGetValue((owner, _Norm(declarerId)), out ModLocalComp.LocalCompFile value))
		{
			return owner;
		}
		return value;
	}

	private static ModLocalComp.LocalCompFile _CreateSiblingSource(ModLocalComp.LocalCompFile owner, string id, string version)
	{
		ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(owner.path + "!/@" + id);
		localCompFile.SetJijMetadata(id, id, version);
		localCompFile.JijLoader = owner.JijLoader ?? owner.DetectedLoader;
		return localCompFile;
	}

	private static List<ModLocalComp.LocalCompFile> _CollectLoadableNodes(List<ModLocalComp.LocalCompFile> embedded, string mc, IReadOnlyDictionary<string, string> forgeSelections = null, IReadOnlyDictionary<string, string> fabricSelections = null)
	{
		List<ModLocalComp.LocalCompFile> list = new List<ModLocalComp.LocalCompFile>();
		_Collect(embedded, mc, list, forgeSelections, fabricSelections);
		return list;
	}

	private static void _Collect(List<ModLocalComp.LocalCompFile> embedded, string mc, List<ModLocalComp.LocalCompFile> into, IReadOnlyDictionary<string, string> forgeSelections, IReadOnlyDictionary<string, string> fabricSelections)
	{
		if (embedded == null)
		{
			return;
		}
		foreach (ModLocalComp.LocalCompFile item in embedded)
		{
			if (_NodeLoads(item, mc) && _ForgeNodeSelected(item, forgeSelections) && _FabricNodeSelected(item, fabricSelections))
			{
				into.Add(item);
				_Collect(item.EmbeddedMods, mc, into, forgeSelections, fabricSelections);
			}
		}
	}

	private static bool _FabricNodeSelected(ModLocalComp.LocalCompFile node, IReadOnlyDictionary<string, string> selections)
	{
		if (selections == null || string.IsNullOrWhiteSpace(node.ModId) || (!string.Equals(node.JijLoader, "Fabric", StringComparison.OrdinalIgnoreCase) && !string.Equals(node.JijLoader, "Quilt", StringComparison.OrdinalIgnoreCase)))
		{
			return true;
		}
		if (!selections.TryGetValue(node.ModId, out string value) || value == null)
		{
			return false;
		}
		return McConstraintMatcher.CompareFabricVersions(node.Version, value) == 0;
	}

	private Dictionary<string, string> _SelectFabricVersions(IReadOnlyDictionary<ModLocalComp.LocalCompFile, List<ModLocalComp.LocalCompFile>> nodesByHost, string mc, IReadOnlyDictionary<string, string> forgeSelections)
	{
		List<ModLocalComp.LocalCompFile> list = nodesByHost.Values.SelectMany((List<ModLocalComp.LocalCompFile> nodes) => nodes).ToList();
		List<DepRow> list2 = new List<DepRow>();
		foreach (ModLocalComp.LocalCompFile item in _allMods.Where((ModLocalComp.LocalCompFile m) => m.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine))
		{
			foreach (EmbeddedDependency item2 in item.DependencyDeclarations.Where((EmbeddedDependency d) => !d.Optional))
			{
				list2.Add(new DepRow
				{
					DepId = item2.Id,
					Raw = item2.Raw,
					Loader = item.DetectedLoader
				});
			}
		}
		Dictionary<string, string> dictionary = _ChooseFabricVersions(list, list2);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		int num = list.Count + 1;
		for (int num2 = 0; num2 < num; num2++)
		{
			if (!hashSet.Add(_FabricSelectionKey(dictionary)))
			{
				ModBase.Log("[Mod] Fabric 内嵌版本选择出现循环，保留当前候选并交由依赖检查报告冲突", ModBase.LogLevel.Developer);
				return dictionary;
			}
			List<DepRow> list3 = new List<DepRow>(list2);
			foreach (ModLocalComp.LocalCompFile key in nodesByHost.Keys)
			{
				foreach (ModLocalComp.LocalCompFile item3 in _CollectLoadableNodes(key.EmbeddedMods, mc, forgeSelections, dictionary))
				{
					foreach (EmbeddedDependency item4 in item3.DependencyDeclarations.Where((EmbeddedDependency d) => !d.Optional))
					{
						list3.Add(new DepRow
						{
							DepId = item4.Id,
							Raw = item4.Raw,
							Loader = item3.JijLoader
						});
					}
				}
			}
			Dictionary<string, string> dictionary2 = _ChooseFabricVersions(list, list3);
			if (_SameFabricSelections(dictionary, dictionary2))
			{
				return dictionary2;
			}
			dictionary = dictionary2;
		}
		ModBase.Log("[Mod] Fabric 内嵌版本选择未在限定轮次内收敛，保留当前候选并交由依赖检查报告冲突", ModBase.LogLevel.Developer);
		return dictionary;
	}

	private static Dictionary<string, string> _ChooseFabricVersions(List<ModLocalComp.LocalCompFile> candidates, List<DepRow> requirements)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (IGrouping<string, ModLocalComp.LocalCompFile> group in candidates.Where((ModLocalComp.LocalCompFile n) => !string.IsNullOrWhiteSpace(n.ModId) && (string.Equals(n.JijLoader, "Fabric", StringComparison.OrdinalIgnoreCase) || string.Equals(n.JijLoader, "Quilt", StringComparison.OrdinalIgnoreCase))).GroupBy((ModLocalComp.LocalCompFile n) => n.ModId, StringComparer.OrdinalIgnoreCase))
		{
			List<string> list = (from n in @group
				select n.Version into v
				where !string.IsNullOrWhiteSpace(v)
				select v).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			list.Sort((string a, string b) => McConstraintMatcher.CompareFabricVersions(b, a));
			List<DepRow> needed = requirements.Where((DepRow r) => string.Equals(r.DepId, group.Key, StringComparison.OrdinalIgnoreCase)).ToList();
			dictionary[group.Key] = list.FirstOrDefault((string candidate) => needed.All((DepRow r) => _VersionSatisfies(r, candidate)));
		}
		return dictionary;
	}

	private static bool _SameFabricSelections(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
	{
		if (left.Count == right.Count)
		{
			return left.All((KeyValuePair<string, string> pair) => right.TryGetValue(pair.Key, out string value) && string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	private static string _FabricSelectionKey(IReadOnlyDictionary<string, string> selections)
	{
		return string.Join("\n", from pair in selections.OrderBy((KeyValuePair<string, string> pair) => pair.Key, StringComparer.OrdinalIgnoreCase)
			select pair.Key + "\0" + pair.Value);
	}

	private static bool _ForgeNodeSelected(ModLocalComp.LocalCompFile node, IReadOnlyDictionary<string, string> selections)
	{
		if (selections == null || string.IsNullOrWhiteSpace(node.JijIdentifier))
		{
			return true;
		}
		if (!selections.TryGetValue(node.JijIdentifier, out string value) || value == null)
		{
			return false;
		}
		string text = node.JijArtifactVersion ?? node.Version;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return McConstraintMatcher.CompareMavenVersions(text, value) == 0;
		}
		return false;
	}

	private static Dictionary<string, string> _SelectForgeJarJarVersions(IEnumerable<ModLocalComp.LocalCompFile> nodes)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (IGrouping<string, ModLocalComp.LocalCompFile> group in nodes.Where((ModLocalComp.LocalCompFile n) => !string.IsNullOrWhiteSpace(n.JijIdentifier)).GroupBy((ModLocalComp.LocalCompFile n) => n.JijIdentifier, StringComparer.OrdinalIgnoreCase))
		{
			List<string> list = (from n in @group
				select n.JijArtifactVersion ?? n.Version into v
				where !string.IsNullOrWhiteSpace(v)
				select v).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			list.Sort((string a, string b) => McConstraintMatcher.CompareMavenVersions(b, a));
			dictionary[group.Key] = list.FirstOrDefault((string candidate) => group.All((ModLocalComp.LocalCompFile n) => string.IsNullOrWhiteSpace(n.JijVersionRange) || _JarJarRangeContains(n.JijVersionRange, candidate)));
		}
		return dictionary;
	}

	private static bool _JarJarRangeContains(string range, string candidate)
	{
		string text = range.Trim();
		if (text.Length > 0)
		{
			char c = text[0];
			if (c != '[' && c != '(')
			{
				return McConstraintMatcher.CompareMavenVersions(candidate, text) == 0;
			}
		}
		return McConstraintMatcher.Satisfies(text, "Forge", candidate);
	}

	private static bool _NodeLoads(ModLocalComp.LocalCompFile node, string mc)
	{
		if (string.IsNullOrEmpty(mc))
		{
			return true;
		}
		List<EmbeddedDependency> list = node.DependencyDeclarations.Where((EmbeddedDependency d) => string.Equals(d.Id, "minecraft", StringComparison.OrdinalIgnoreCase)).ToList();
		List<EmbeddedDependency> list2 = list.Where((EmbeddedDependency d) => !d.Optional).ToList();
		if (list2.Count > 0)
		{
			List<EmbeddedDependency> list3 = list2.Where((EmbeddedDependency d) => !string.IsNullOrWhiteSpace(d.Raw)).ToList();
			if (list3.Count == 0 || list3.All((EmbeddedDependency d) => McConstraintMatcher.Satisfies(d.Raw, node.JijLoader, mc)))
			{
				return true;
			}
			if (!McConstraintMatcher.ContainsVersionToken(node.FileName, mc))
			{
				return McConstraintMatcher.ContainsVersionToken(node.Version, mc);
			}
			return true;
		}
		if (list.Count > 0)
		{
			return true;
		}
		string jijTargetMcVersion = node.JijTargetMcVersion;
		if (string.IsNullOrWhiteSpace(jijTargetMcVersion))
		{
			return true;
		}
		if (McConstraintMatcher.Satisfies(jijTargetMcVersion, node.JijLoader, mc))
		{
			return true;
		}
		if (!McConstraintMatcher.ContainsVersionToken(node.FileName, mc))
		{
			return McConstraintMatcher.ContainsVersionToken(node.Version, mc);
		}
		return true;
	}
}
