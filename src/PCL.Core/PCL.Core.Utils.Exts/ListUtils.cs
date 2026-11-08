using System;
using System.Collections.Generic;

namespace PCL.Core.Utils.Exts;

public static class ListUtils
{
	extension<T>(IEnumerable<T> source)
	{
		public T? MaxOrDefault<C>(Func<T, C> selector) where C : IComparable<C>
		{
			using IEnumerator<T> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				return default;
			}
			T current = enumerator.Current;
			C other = selector(current);
			while (enumerator.MoveNext())
			{
				C val = selector(enumerator.Current);
				if (val.CompareTo(other) > 0)
				{
					current = enumerator.Current;
					other = val;
				}
			}
			return current;
		}

		public T? MinOrDefault<C>(Func<T, C> selector) where C : IComparable<C>
		{
			using IEnumerator<T> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				return default;
			}
			T current = enumerator.Current;
			C other = selector(current);
			while (enumerator.MoveNext())
			{
				C val = selector(enumerator.Current);
				if (val.CompareTo(other) < 0)
				{
					current = enumerator.Current;
					other = val;
				}
			}
			return current;
		}
	}
}
