using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Java;
using PCL.Core.Minecraft.Java.Parser;
using PCL.Core.Minecraft.Java.Scanner;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft;

public class JavaManager
{
	private const string ModuleName = "JavaManager";

	private readonly Dictionary<string, JavaEntry> _javaEntrys = new Dictionary<string, JavaEntry>();

	private readonly IJavaParser _parser;

	private readonly IJavaScanner[] _scanners;

	private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);

	private DateTime _lastScanTime = DateTime.MinValue;

	private static readonly TimeSpan _MinScanInterval = TimeSpan.FromSeconds(13L);

	public JavaManager(IJavaParser parser, params IJavaScanner[] scanners)
	{
		_parser = parser;
		_scanners = scanners;
	}

	public void SaveConfig()
	{
		try
		{
			JavaStorageItem[] value = _javaEntrys.Select((KeyValuePair<string, JavaEntry> x) => new JavaStorageItem
			{
				Path = x.Value.Installation.JavaExePath,
				IsEnable = x.Value.IsEnabled,
				Source = x.Value.Source
			}).ToArray();
			States.Game.JavaList = JsonSerializer.Serialize(value, JsonCompat.SerializerOptions);
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "JavaManager", "保存 Java 配置项失败");
		}
	}

	public void ReadConfig()
	{
		try
		{
			JavaStorageItem[] array = JsonSerializer.Deserialize<JavaStorageItem[]>(States.Game.JavaList, JsonCompat.SerializerOptions);
			if (array == null)
			{
				return;
			}
			List<JavaEntry> list = new List<JavaEntry>();
			JavaStorageItem[] array2 = array;
			foreach (JavaStorageItem javaStorageItem in array2)
			{
				JavaInstallation javaInstallation = _parser.Parse(javaStorageItem.Path);
				if ((object)javaInstallation == null)
				{
					LogWrapper.Trace("JavaManager", "Can not find Java " + javaStorageItem.Path + ", skip");
					continue;
				}
				list.Add(new JavaEntry
				{
					Installation = javaInstallation,
					IsEnabled = javaStorageItem.IsEnable,
					Source = javaStorageItem.Source.GetValueOrDefault()
				});
			}
			lock (_javaEntrys)
			{
				foreach (JavaEntry item in list)
				{
					if (_javaEntrys.TryGetValue(item.Installation.JavaExePath, out JavaEntry value))
					{
						value.IsEnabled = item.IsEnabled;
						value.Source = item.Source;
					}
					else
					{
						_javaEntrys.Add(item.Installation.JavaExePath, item);
					}
				}
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "JavaManager", "无法读取 Java 配置项");
		}
	}

	public async Task ScanJavaAsync(bool force = false)
	{
		if (ShouldSkip() || !(await _scanLock.WaitAsync(TimeSpan.FromSeconds(7L))))
		{
			return;
		}
		try
		{
			if (!ShouldSkip())
			{
				await Task.Run((Action)_ScanInternal);
				_lastScanTime = DateTime.Now;
				SaveConfig();
			}
		}
		finally
		{
			_scanLock.Release();
		}
		bool ShouldSkip()
		{
			if (!force)
			{
				return DateTime.Now - _lastScanTime < _MinScanInterval;
			}
			return false;
		}
	}

	private void _ScanInternal()
	{
		ConcurrentDictionary<string, bool> pathSet = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		Parallel.ForEach(_scanners, (IJavaScanner scanner) =>
		{
			List<string> list2 = new List<string>();
			scanner.Scan(list2);
			foreach (string item in list2)
			{
				string text = _NormalizePath(item);
				if (!_ShouldExcludePath(text))
				{
					pathSet.TryAdd(text, value: true);
				}
			}
		});
		List<JavaEntry> list = (from inst in pathSet.Keys.Select(_parser.Parse)
			where (object)inst != null
			select new JavaEntry
			{
				Installation = inst,
				IsEnabled = (_javaEntrys.TryGetValue(_NormalizePath(inst.JavaExePath), out JavaEntry value) ? value.IsEnabled : _ShouldEnableByDefault(inst)),
				Source = JavaSource.AutoScanned
			}).ToList();
		lock (_javaEntrys)
		{
			foreach (JavaEntry item2 in list)
			{
				_javaEntrys[item2.Installation.JavaExePath] = item2;
			}
		}
	}

	private static bool _ShouldEnableByDefault(JavaInstallation inst)
	{
		string path = Path.Combine(Directory.GetParent(inst.JavaFolder).FullName, "lib");
		bool result = (!inst.IsJre && File.Exists(Path.Combine(path, "jvm.lib"))) || (inst.IsJre && File.Exists(Path.Combine(path, "rt.jar")));
		if ((!inst.IsJre || inst.MajorVersion <= 8) && !(inst.Is64Bit ^ Environment.Is64BitOperatingSystem))
		{
			return result;
		}
		return false;
	}

	public List<JavaEntry> GetSortedJavaList()
	{
		List<JavaEntry> list = _javaEntrys.Values.ToList();
		list.Sort((JavaEntry a, JavaEntry b) =>
		{
			int num = a.Installation.Version.CompareTo(b.Installation.Version);
			return (num != 0) ? num : (a.Installation.Brand - b.Installation.Brand);
		});
		list.Reverse();
		return list;
	}

	public bool Existing64BitJava()
	{
		lock (_javaEntrys)
		{
			return _javaEntrys.Any((KeyValuePair<string, JavaEntry> x) => x.Value.Installation.Is64Bit);
		}
	}

	public bool ExistAnyJava()
	{
		return _javaEntrys.Count != 0;
	}

	public bool Exist(string javaExePath)
	{
		return _javaEntrys.ContainsKey(javaExePath);
	}

	public JavaEntry? AddOrGet(string javaExePath)
	{
		try
		{
			if (javaExePath.IsNullOrWhiteSpace() || !File.Exists(javaExePath))
			{
				return null;
			}
			JavaInstallation javaInstallation = _parser.Parse(javaExePath);
			if ((object)javaInstallation == null)
			{
				return null;
			}
			string key = _NormalizePath(javaInstallation.JavaExePath);
			lock (_javaEntrys)
			{
				if (_javaEntrys.TryGetValue(key, out JavaEntry value))
				{
					return value;
				}
				JavaEntry javaEntry = new JavaEntry
				{
					Installation = javaInstallation,
					IsEnabled = _ShouldEnableByDefault(javaInstallation),
					Source = JavaSource.ManualAdded
				};
				_javaEntrys.Add(key, javaEntry);
				return javaEntry;
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "JavaManager", "Failed to add or get " + javaExePath);
			return null;
		}
	}

	public JavaEntry? Get(string javaExePath)
	{
		try
		{
			if (javaExePath.IsNullOrWhiteSpace() || !File.Exists(javaExePath))
			{
				return null;
			}
			JavaInstallation javaInstallation = _parser.Parse(javaExePath);
			if ((object)javaInstallation == null)
			{
				return null;
			}
			string key = _NormalizePath(javaInstallation.JavaExePath);
			lock (_javaEntrys)
			{
				if (_javaEntrys.TryGetValue(key, out JavaEntry value))
				{
					return value;
				}
				return new JavaEntry
				{
					Installation = javaInstallation,
					IsEnabled = _ShouldEnableByDefault(javaInstallation),
					Source = JavaSource.ManualAdded
				};
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "JavaManager", "Failed to get " + javaExePath);
			return null;
		}
	}

	public async Task<JavaEntry[]> SelectSuitableJavaAsync(Version minVersion, Version maxVersion)
	{
		if (_javaEntrys.Count == 0)
		{
			await ScanJavaAsync();
		}
		lock (_javaEntrys)
		{
			return (from j in _javaEntrys.Values.ToList()
				where j.Installation.IsStillAvailable && j.IsEnabled && IsVersionSuitable(j.Installation.Version, minVersion, maxVersion)
				orderby j.Installation.MajorVersion, j.Installation.IsJre, j.Installation.Brand, j.Installation.Version descending
				select j).ToArray();
		}
	}

	public void CheckAllAvailability()
	{
		lock (_javaEntrys)
		{
			string[] array = (from kv in _javaEntrys
				where !kv.Value.Installation.IsStillAvailable
				select kv.Key).ToArray();
			foreach (string key in array)
			{
				_javaEntrys.Remove(key);
			}
		}
	}

	private static string _NormalizePath(string path)
	{
		return Path.GetFullPath(path.TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		})).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		});
	}

	private static bool _ShouldExcludePath(string path)
	{
		InlineArray2<char> buffer = default;
		buffer[0] = Path.DirectorySeparatorChar;
		buffer[1] = Path.AltDirectorySeparatorChar;
		return path.Split(buffer).Any((string part) => JavaConsts.ExcludeFolderNames.Contains(part, StringComparer.OrdinalIgnoreCase));
	}

	public static Version NormalizeVersion(Version version)
	{
		if (version.Major != 1 || version.Minor < 0)
		{
			return version;
		}
		return new Version(version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));
	}

	public static bool IsVersionSuitable(Version javaVersion, Version minVersion, Version maxVersion)
	{
		Version version = NormalizeVersion(javaVersion);
		Version version2 = NormalizeVersion(minVersion);
		Version version3 = NormalizeVersion(maxVersion);
		if (version >= version2)
		{
			return version <= version3;
		}
		return false;
	}
}
