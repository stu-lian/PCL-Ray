using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.Utils.Diff;

public class ItemDiff
{
	public static ItemDiffResult<T> ComputeDiff<T, TKey>(IEnumerable<T> oldItems, IEnumerable<T> newItems, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
	{
		ArgumentNullException.ThrowIfNull(oldItems, "oldItems");
		ArgumentNullException.ThrowIfNull(newItems, "newItems");
		ArgumentNullException.ThrowIfNull(keySelector, "keySelector");
		IEqualityComparer<TKey> comparer = keyComparer ?? EqualityComparer<TKey>.Default;
		Dictionary<TKey, T> oldDict = oldItems.ToDictionary(keySelector, comparer);
		Dictionary<TKey, T> newDict = newItems.ToDictionary(keySelector, comparer);
		HashSet<TKey> hashSet = new HashSet<TKey>(oldDict.Keys, comparer);
		HashSet<TKey> hashSet2 = new HashSet<TKey>(newDict.Keys, comparer);
		HashSet<TKey> source = new HashSet<TKey>(hashSet2.Except(hashSet, comparer), comparer);
		HashSet<TKey> source2 = new HashSet<TKey>(hashSet.Except(hashSet2, comparer), comparer);
		HashSet<TKey> source3 = new HashSet<TKey>(hashSet.Intersect(hashSet2, comparer), comparer);
		List<T> added = source.Select((TKey k) => newDict[k]).ToList();
		List<T> removed = source2.Select((TKey k) => oldDict[k]).ToList();
		List<T> unchanged = source3.Select((TKey k) => newDict[k]).ToList();
		return new ItemDiffResult<T>(added, removed, unchanged);
	}
}
