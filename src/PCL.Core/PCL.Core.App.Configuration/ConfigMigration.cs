using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace PCL.Core.App.Configuration;

public class ConfigMigration
{
	public required string From { get; init; }

	public required string To { get; init; }

	public int Priority { get; init; }

	public required ConfigMigrationHandler OnMigration { get; init; }

	public static bool Migrate(string target, IEnumerable<ConfigMigration> migrations)
	{
		migrations = (migrations as ConfigMigration[]) ?? migrations.ToArray();
		IEnumerable<ConfigMigration> solution = null;
		if (!(from migration in migrations.Reverse()
			let path = migration.From
			where File.Exists(path) && _TryFindShortestPath(path, target, migrations, out solution)
			select path).Any())
		{
			return false;
		}
		foreach (ConfigMigration item in solution)
		{
			item.OnMigration(item.From, item.To);
		}
		return true;
	}

	private static bool _TryFindShortestPath(string start, string end, IEnumerable<ConfigMigration> paths, [NotNullWhen(true)] out IEnumerable<ConfigMigration>? result)
	{
		if (start == end)
		{
			result = Array.Empty<ConfigMigration>();
			return true;
		}
		Dictionary<string, List<ConfigMigration>> dictionary = new Dictionary<string, List<ConfigMigration>>(StringComparer.Ordinal);
		foreach (ConfigMigration path in paths)
		{
			if (!dictionary.TryGetValue(path.From, out var value))
			{
				value = new List<ConfigMigration>();
				dictionary[path.From] = value;
			}
			value.Add(path);
		}
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>(StringComparer.Ordinal);
		Queue<string> queue = new Queue<string>();
		dictionary2[start] = 0;
		queue.Enqueue(start);
		while (queue.Count > 0)
		{
			string key = queue.Dequeue();
			if (!dictionary.TryGetValue(key, out var value2))
			{
				continue;
			}
			int num = dictionary2[key];
			foreach (ConfigMigration item2 in value2)
			{
				string to = item2.To;
				if (!dictionary2.ContainsKey(to))
				{
					dictionary2[to] = num + 1;
					queue.Enqueue(to);
				}
			}
		}
		if (!dictionary2.ContainsKey(end))
		{
			result = null;
			return false;
		}
		Dictionary<int, List<string>> dictionary3 = new Dictionary<int, List<string>>();
		foreach (KeyValuePair<string, int> item3 in dictionary2)
		{
			if (!dictionary3.TryGetValue(item3.Value, out var value3))
			{
				value3 = new List<string>();
				dictionary3[item3.Value] = value3;
			}
			value3.Add(item3.Key);
		}
		Dictionary<string, long> dictionary4 = new Dictionary<string, long>(StringComparer.Ordinal);
		Dictionary<string, string> dictionary5 = new Dictionary<string, string>(StringComparer.Ordinal);
		Dictionary<string, ConfigMigration> dictionary6 = new Dictionary<string, ConfigMigration>(StringComparer.Ordinal);
		dictionary4[start] = 0L;
		int num2 = dictionary2[end];
		for (int i = 0; i < num2; i++)
		{
			if (!dictionary3.TryGetValue(i, out var value4))
			{
				continue;
			}
			foreach (string item4 in value4)
			{
				if (!dictionary4.ContainsKey(item4) || !dictionary.TryGetValue(item4, out var value5))
				{
					continue;
				}
				foreach (ConfigMigration item5 in value5)
				{
					if (dictionary2.TryGetValue(item5.To, out var value6) && value6 == i + 1)
					{
						long num3 = dictionary4[item4] + item5.Priority;
						if (!dictionary4.TryGetValue(item5.To, out var value7) || num3 > value7)
						{
							dictionary4[item5.To] = num3;
							dictionary5[item5.To] = item4;
							dictionary6[item5.To] = item5;
						}
					}
				}
			}
		}
		if (!dictionary6.ContainsKey(end))
		{
			result = null;
			return false;
		}
		List<ConfigMigration> list = new List<ConfigMigration>();
		string text = end;
		while (text != start)
		{
			ConfigMigration item = dictionary6[text];
			list.Add(item);
			text = dictionary5[text];
		}
		list.Reverse();
		result = list;
		return true;
	}
}
