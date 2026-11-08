using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PCL.Core.Utils.Exts;

public static class ConcurrentDictionaryExtension
{
	extension<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dict) where TKey : notnull
	{
		public bool CompareAndRemove(TKey key, TValue comparison)
		{
			return ((ICollection<KeyValuePair<TKey, TValue>>)dict).Remove(new KeyValuePair<TKey, TValue>(key, comparison));
		}

		public TValue? UpdateAndGetPrevious(TKey key, TValue value)
		{
			TValue prevValue = default;
			dict.AddOrUpdate(key, (TKey _) =>
			{
				prevValue = default;
				return value;
			}, (TKey _, TValue existingValue) =>
			{
				prevValue = existingValue;
				return value;
			});
			return prevValue;
		}
	}
}
