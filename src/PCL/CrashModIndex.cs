using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PCL.Core.Logging;

namespace PCL;

internal sealed class CrashModIndex
{
	private readonly List<CrashModInfo> _mods = new List<CrashModInfo>();

	public static CrashModIndex Create(CrashLogSet logs, McInstance? instance)
	{
		CrashModIndex crashModIndex = new CrashModIndex();
		crashModIndex._ReadCrashReport(logs.CrashReport?.Text);
		crashModIndex._ReadForgeDebugLog(logs.Debug?.Text);
		crashModIndex._ReadLoaderMessages(logs.Game?.Text);
		if ((object)instance != null)
		{
			crashModIndex._ReadInstanceModFiles(instance);
		}
		crashModIndex._NormalizeAndDeduplicate();
		return crashModIndex;
	}

	public IEnumerable<CrashModInfo> ResolveMany(IEnumerable<string> hints)
	{
		List<CrashModInfo> list = new List<CrashModInfo>();
		foreach (string hint in hints)
		{
			foreach (CrashModInfo mod in Resolve(hint))
			{
				if (!list.Any((CrashModInfo item) => _SameDisplay(item, mod)))
				{
					list.Add(mod);
				}
			}
		}
		return list;
	}

	public IEnumerable<CrashModInfo> Resolve(string? hint)
	{
		if (string.IsNullOrWhiteSpace(hint))
		{
			return Array.Empty<CrashModInfo>();
		}
		string normalizedHint = _Normalize(hint);
		if (string.IsNullOrWhiteSpace(normalizedHint))
		{
			return Array.Empty<CrashModInfo>();
		}
		List<CrashModInfo> list = _mods.Where((CrashModInfo mod) => string.Equals(_Normalize(mod.ModId), normalizedHint, StringComparison.Ordinal) || string.Equals(_Normalize(mod.FileName), normalizedHint, StringComparison.Ordinal) || string.Equals(_Normalize(mod.DisplayName), normalizedHint, StringComparison.Ordinal)).ToList();
		if (list.Count != 0)
		{
			return list;
		}
		return _mods.Where((CrashModInfo mod) => _Normalize(mod.ModId).Contains(normalizedHint, StringComparison.Ordinal) || _Normalize(mod.FileName).Contains(normalizedHint, StringComparison.Ordinal) || _Normalize(mod.DisplayName).Contains(normalizedHint, StringComparison.Ordinal)).ToList();
	}

	public List<string> ResolveToDisplayNames(IEnumerable<string> hints)
	{
		List<string> list = (from item in hints
			where !string.IsNullOrWhiteSpace(item)
			select item.Trim()).Distinct().ToList();
		List<string> list2 = (from mod in ResolveMany(list)
			select mod.DisplayNameOrFileName into item
			where !string.IsNullOrWhiteSpace(item)
			select item).Distinct(StringComparer.Ordinal).ToList();
		if (list2.Count == 0)
		{
			return list;
		}
		return list2;
	}

	private void _ReadCrashReport(string? text)
	{
		if (string.IsNullOrEmpty(text) || !text.Contains("A detailed walkthrough of the error", StringComparison.Ordinal))
		{
			return;
		}
		string text2 = text.Replace("A detailed walkthrough of the error", "\u00a8");
		bool flag = text2.Contains("Fabric Mods", StringComparison.Ordinal);
		if (flag)
		{
			text2 = text2.Replace("Fabric Mods", "\u00a8");
			LogWrapper.Info("Crash", "崩溃报告中检测到 Fabric Mod 信息格式");
		}
		if (text2.Contains("quilt-loader", StringComparison.Ordinal))
		{
			text2 = text2.Replace("Mod Table Version", "\u00a8");
			LogWrapper.Info("Crash", "崩溃报告中检测到 Quilt Mod 信息格式");
		}
		text2 = CrashText.AfterLast(text2, "\u00a8");
		string[] array = text2.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i].Trim(new char[2] { '\r', '\n' });
			if (!string.IsNullOrWhiteSpace(text3))
			{
				if (flag && text3.StartsWith("\t\t", StringComparison.Ordinal) && !CrashRegex.IsMatch(text3, "\\t\\tfabric[\\w-]*: Fabric"))
				{
					_Add(new CrashModInfo
					{
						ModId = CrashRegex.First(text3, "(?<=\\t\\t)[^:]+"),
						DisplayName = CrashRegex.First(text3, "(?<=: )[^\\n]+(?= [^\\n]+)"),
						Version = CrashRegex.First(text3, "(?<= )[\\w\\.-]+$"),
						Source = "crash-report-fabric"
					});
				}
				else if (text3.Contains(".jar", StringComparison.OrdinalIgnoreCase) && text3.Length - text3.Replace(".jar", "", StringComparison.OrdinalIgnoreCase).Length == 4)
				{
					_Add(new CrashModInfo
					{
						FileName = CrashRegex.First(text3, "(?<=\\()[^\\t]+.jar(?=\\))|(?<=(\\t\\t)|(\\| ))[^\\t\\|]+.jar", RegexOptions.IgnoreCase),
						DisplayName = CrashRegex.First(text3, "(?<=\\t)[^\\t\\|]+(?=\\s+\\()"),
						ModId = CrashRegex.First(text3, "(?<=\\| )[\\w\\.-]+(?= \\|)"),
						Source = "crash-report-forge"
					});
				}
			}
		}
	}

	private void _ReadForgeDebugLog(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (string item in CrashRegex.All(text, "(?<=valid mod file ).*", RegexOptions.Multiline))
		{
			_Add(new CrashModInfo
			{
				FileName = CrashRegex.First(item, ".*(?= with)"),
				ModId = CrashRegex.First(item, "(?<=with \\{)[^\\}]+"),
				Version = CrashRegex.First(item, "(?<=versions \\{)[^\\}]+"),
				Source = "debug-log"
			});
		}
	}

	private void _ReadLoaderMessages(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (string item in CrashRegex.All(text, "(?<=ModID: )[^,\\n]+|(?<=ModId )[^\\n]+(?= for )", RegexOptions.IgnoreCase))
		{
			_Add(new CrashModInfo
			{
				ModId = item.Trim(),
				Source = "loader-message"
			});
		}
	}

	private void _ReadInstanceModFiles(McInstance instance)
	{
		foreach (string item in _GetCandidateModDirectories(instance))
		{
			try
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(item);
				if (!directoryInfo.Exists)
				{
					continue;
				}
				foreach (FileInfo item2 in directoryInfo.EnumerateFiles("*.jar"))
				{
					_Add(new CrashModInfo
					{
						FileName = item2.Name,
						DisplayName = Path.GetFileNameWithoutExtension(item2.Name),
						Source = "instance-mods"
					});
				}
			}
			catch (Exception ex)
			{
				LogWrapper.Warn(ex, "Crash", "读取实例 Mod 文件列表失败（" + item + "）");
			}
		}
	}

	private static IEnumerable<string> _GetCandidateModDirectories(McInstance instance)
	{
		yield return Path.Combine(instance.PathInstance, "mods");
		string text = null;
		try
		{
			text = instance.PathIndie;
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Crash", "读取实例隔离路径失败");
		}
		if (!string.IsNullOrEmpty(text))
		{
			yield return Path.Combine(text, "mods");
		}
	}

	private void _Add(CrashModInfo mod)
	{
		if (!string.IsNullOrWhiteSpace(mod.ModId) || !string.IsNullOrWhiteSpace(mod.DisplayName) || !string.IsNullOrWhiteSpace(mod.FileName))
		{
			_mods.Add(mod);
		}
	}

	private void _NormalizeAndDeduplicate()
	{
		List<CrashModInfo> distinct = new List<CrashModInfo>();
		foreach (CrashModInfo item in _mods.Where((CrashModInfo mod) => !distinct.Any((CrashModInfo item) => _SameDisplay(item, mod))))
		{
			distinct.Add(item);
		}
		_mods.Clear();
		_mods.AddRange(distinct);
		LogWrapper.Info("Crash", "构建 Mod 索引，找到 " + _mods.Count + " 个候选 Mod");
	}

	private static bool _SameDisplay(CrashModInfo a, CrashModInfo b)
	{
		if (string.Equals(_Normalize(a.ModId), _Normalize(b.ModId), StringComparison.Ordinal) && string.Equals(_Normalize(a.FileName), _Normalize(b.FileName), StringComparison.Ordinal))
		{
			return string.Equals(_Normalize(a.DisplayName), _Normalize(b.DisplayName), StringComparison.Ordinal);
		}
		return false;
	}

	private static string _Normalize(string? value)
	{
		return (value ?? string.Empty).ToLowerInvariant().Replace("_", "").Replace("-", "")
			.Replace(" ", "")
			.Replace(".jar", "");
	}
}
