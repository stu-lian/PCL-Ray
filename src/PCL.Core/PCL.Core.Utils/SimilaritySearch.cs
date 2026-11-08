using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.Utils;

public static class SimilaritySearch
{
	private const double LengthPowerBase = 1.4;

	private const double LengthWeightOffset = 3.6;

	private const double PositionBonusFactor = 0.3;

	private const int MaxPositionBonusDistance = 3;

	private const double SourceLengthImpactFactor = 3.0;

	private const double SourceLengthSmoothing = 15.0;

	private const double ShortQueryBonusFactor = 2.0;

	private static double _SearchSimilarity(string source, string query)
	{
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
		{
			return 0.0;
		}
		ReadOnlySpan<char> readOnlySpan = source.ToLower().Replace(" ", "");
		ReadOnlySpan<char> readOnlySpan2 = query.ToLower().Replace(" ", "");
		if (readOnlySpan.IsEmpty || readOnlySpan2.IsEmpty)
		{
			return 0.0;
		}
		bool[] array = new bool[readOnlySpan.Length];
		double num = 0.0;
		int num2;
		for (int i = 0; i < readOnlySpan2.Length; i += Math.Max(1, num2))
		{
			num2 = 0;
			int num3 = -1;
			for (int j = 0; j < readOnlySpan.Length; j++)
			{
				int k;
				for (k = 0; i + k < readOnlySpan2.Length && j + k < readOnlySpan.Length && !array[j + k] && readOnlySpan[j + k] == readOnlySpan2[i + k]; k++)
				{
				}
				if (k > num2)
				{
					num2 = k;
					num3 = j;
				}
			}
			if (num2 > 0)
			{
				for (int l = 0; l < num2; l++)
				{
					array[num3 + l] = true;
				}
				double num4 = Math.Pow(1.4, 3 + num2) - 3.6;
				int num5 = Math.Abs(i - num3);
				double num6 = 1.0 + 0.3 * (double)Math.Max(0, 3 - num5);
				num4 *= num6;
				num += num4;
			}
		}
		double num7 = num / (double)readOnlySpan2.Length;
		double num8 = 3.0 / Math.Sqrt((double)readOnlySpan.Length + 15.0);
		double num9 = ((query.Length == 1) ? 2.0 : 1.0);
		return num7 * num8 * num9;
	}

	private static double _SearchSimilarityWeighted(List<KeyValuePair<string, double>> source, string query)
	{
		if (source.Count == 0)
		{
			return 0.0;
		}
		double num = source.Sum((KeyValuePair<string, double> pair) => pair.Value);
		if (num == 0.0)
		{
			return 0.0;
		}
		return source.Sum((KeyValuePair<string, double> pair) => _SearchSimilarity(pair.Key, query) * pair.Value) / num;
	}

	private static bool _IsAbsoluteMatch(IEnumerable<KeyValuePair<string, double>> searchSources, string[] queryParts)
	{
		List<string> processedSources = searchSources.Select((KeyValuePair<string, double> s) => s.Key.Replace(" ", "").ToLower()).ToList();
		return queryParts.All((string queryPart) => processedSources.Any((string source) => source.Contains(queryPart)));
	}

	public static List<SearchEntry<T>> Search<T>(List<SearchEntry<T>> entries, string query, int maxBlurCount = 5, double minBlurSimilarity = 0.1)
	{
		if (entries.Count == 0)
		{
			return new List<SearchEntry<T>>();
		}
		if (string.IsNullOrWhiteSpace(query))
		{
			return entries;
		}
		string[] array = (from q in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			select q.ToLower()).ToArray();
		if (array.Length == 0)
		{
			return entries;
		}
		foreach (SearchEntry<T> entry in entries)
		{
			entry.Similarity = _SearchSimilarityWeighted(entry.SearchSource, query);
			entry.AbsoluteRight = _IsAbsoluteMatch(entry.SearchSource, array);
		}
		List<SearchEntry<T>> source = (from e in entries
			orderby e.AbsoluteRight descending, e.Similarity descending
			select e).ToList();
		IEnumerable<SearchEntry<T>> first = source.Where((SearchEntry<T> e) => e.AbsoluteRight);
		IEnumerable<SearchEntry<T>> second = source.Where((SearchEntry<T> e) => !e.AbsoluteRight && e.Similarity >= minBlurSimilarity).Take(maxBlurCount);
		return first.Concat(second).ToList();
	}
}
