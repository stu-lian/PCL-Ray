using System;
using System.Collections.Generic;

namespace PCL.Core.Utils.Exts;

public static class SortUtils
{
	private class StableComparer<T>(Func<T, T, bool> comparison) : IComparer<T>
	{
		private readonly Func<T, T, bool> _comparison = comparison ?? throw new ArgumentNullException("comparison");

		public int Compare(T? x, T? y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			bool flag = _comparison(x, y);
			bool flag2 = _comparison(y, x);
			if (!flag && !flag2)
			{
				return 0;
			}
			if (!flag)
			{
				return 1;
			}
			return -1;
		}
	}

	public static List<T> Sort<T>(this IList<T> list, Func<T, T, bool> comparison)
	{
		List<T> list2 = new List<T>(list);
		list2.Sort(new StableComparer<T>(comparison));
		return list2;
	}
}
