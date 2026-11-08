using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace PCL;

public static class ModJarInJarCache
{
	private class _Store
	{
		public string CachePath;

		public Dictionary<string, CacheEntry> Entries;

		public bool Dirty;
	}

	private class CacheEntry
	{
		public long LastModified { get; set; }

		public long Size { get; set; }

		public List<EmbeddedModNode> Tree { get; set; } = new List<EmbeddedModNode>();
	}

	private class CacheFile
	{
		public string Version { get; set; }

		public Dictionary<string, CacheEntry> Entries { get; set; } = new Dictionary<string, CacheEntry>();
	}

	private const string FormatVersion = "8.3";

	private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver
		{
			Modifiers = { (Action<JsonTypeInfo>)_DropEmptyCollections }
		}
	};

	private static readonly object _lock = new object();

	private static readonly Dictionary<string, _Store> _stores = new Dictionary<string, _Store>(StringComparer.OrdinalIgnoreCase);

	[ThreadStatic]
	private static _Store? _current;

	private static void _DropEmptyCollections(JsonTypeInfo info)
	{
		foreach (JsonPropertyInfo property in info.Properties)
		{
			if (typeof(ICollection).IsAssignableFrom(property.PropertyType))
			{
				property.ShouldSerialize = (object _, object? value) => value is ICollection collection && collection.Count > 0;
			}
		}
	}

	public static void UseInstance(string? instancePath)
	{
		lock (_lock)
		{
			if (string.IsNullOrEmpty(instancePath))
			{
				_current = null;
				return;
			}
			string text = instancePath.TrimEnd(new char[2] { '\\', '/' });
			if (!_stores.TryGetValue(text, out _Store value))
			{
				value = new _Store
				{
					CachePath = Path.Combine(text, "PCL", "JarInJar.bin")
				};
				_stores[text] = value;
			}
			_current = value;
		}
	}

	private static string _NormalizeKey(string path)
	{
		if (!path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		return path.Substring(0, path.Length - ".disabled".Length);
	}

	private static void _EnsureLoaded(_Store store)
	{
		if (store.Entries != null)
		{
			return;
		}
		store.Entries = new Dictionary<string, CacheEntry>();
		try
		{
			if (!File.Exists(store.CachePath))
			{
				return;
			}
			using FileStream stream = File.OpenRead(store.CachePath);
			using GZipStream utf8Json = new GZipStream(stream, CompressionMode.Decompress);
			using JsonDocument jsonDocument = JsonDocument.Parse(utf8Json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("Version", out var value) || value.ValueKind != JsonValueKind.String || value.GetString() != "8.3" || !rootElement.TryGetProperty("Entries", out var value2) || value2.ValueKind != JsonValueKind.Object)
			{
				return;
			}
			foreach (JsonProperty item in value2.EnumerateObject())
			{
				try
				{
					CacheEntry cacheEntry = item.Value.Deserialize<CacheEntry>();
					if (cacheEntry != null && _IsValidTree(cacheEntry.Tree))
					{
						store.Entries[item.Name] = cacheEntry;
					}
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "跳过损坏的 Jar-in-Jar 缓存条目：" + item.Name, ModBase.LogLevel.Developer);
				}
			}
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "读取 Jar-in-Jar 缓存失败，已重置", ModBase.LogLevel.Developer);
		}
	}

	private static bool _IsValidTree(List<EmbeddedModNode> tree)
	{
		if (tree == null)
		{
			return false;
		}
		Stack<(List<EmbeddedModNode>, int)> stack = new Stack<(List<EmbeddedModNode>, int)>();
		stack.Push((tree, 1));
		int num = 0;
		while (stack.Count > 0)
		{
			var (list, num2) = stack.Pop();
			if (num2 > 5)
			{
				return false;
			}
			foreach (EmbeddedModNode item in list)
			{
				if (item == null || string.IsNullOrWhiteSpace(item.FileName) || item.DependencyRows == null || item.Conflicts == null || item.ProvidedIds == null || item.ProvidedVersions == null || item.Children == null || item.ProvidedIds.Any(string.IsNullOrWhiteSpace))
				{
					return false;
				}
				if (++num > 512)
				{
					return false;
				}
				if (item.Children.Count > 0)
				{
					stack.Push((item.Children, num2 + 1));
				}
			}
		}
		return true;
	}

	public static List<EmbeddedModNode> TryGet(string path, long lastModified, long size)
	{
		_Store current = _current;
		if (current == null)
		{
			return null;
		}
		lock (_lock)
		{
			_EnsureLoaded(current);
			if (current.Entries.TryGetValue(_NormalizeKey(path), out CacheEntry value) && value.LastModified == lastModified && value.Size == size)
			{
				return value.Tree;
			}
			return null;
		}
	}

	public static void Set(string path, long lastModified, long size, List<EmbeddedModNode> tree)
	{
		_Store current = _current;
		if (current == null)
		{
			return;
		}
		lock (_lock)
		{
			_EnsureLoaded(current);
			current.Entries[_NormalizeKey(path)] = new CacheEntry
			{
				LastModified = lastModified,
				Size = size,
				Tree = tree
			};
			current.Dirty = true;
		}
	}

	public static void Prune(IEnumerable<string> keepPaths)
	{
		_Store current = _current;
		if (current == null)
		{
			return;
		}
		lock (_lock)
		{
			ICollection<string> source = (keepPaths as ICollection<string>) ?? keepPaths.ToList();
			_EnsureLoaded(current);
			HashSet<string> keep = new HashSet<string>(source.Select(_NormalizeKey), StringComparer.OrdinalIgnoreCase);
			List<string> list = current.Entries.Keys.Where((string k) => !keep.Contains(k)).ToList();
			if (list.Count == 0)
			{
				return;
			}
			foreach (string item in list)
			{
				current.Entries.Remove(item);
			}
			current.Dirty = true;
		}
	}

	public static void Flush()
	{
		lock (_lock)
		{
			foreach (_Store value in _stores.Values)
			{
				_FlushStore(value);
			}
		}
	}

	private static void _FlushStore(_Store store)
	{
		if (!store.Dirty || store.Entries == null)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(store.CachePath));
			string text = store.CachePath + ".tmp";
			byte[] bytes;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (GZipStream utf8Json = new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
				{
					JsonSerializer.Serialize(utf8Json, new CacheFile
					{
						Version = "8.3",
						Entries = store.Entries
					}, _jsonOpts);
				}
				bytes = memoryStream.ToArray();
			}
			File.WriteAllBytes(text, bytes);
			File.Move(text, store.CachePath, overwrite: true);
			string path = Path.Combine(Path.GetDirectoryName(store.CachePath), "JarInJar.json");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			store.Dirty = false;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "写入 Jar-in-Jar 缓存失败", ModBase.LogLevel.Developer);
		}
	}
}
