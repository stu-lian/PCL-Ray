using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.Utils;

public static class RandomUtils
{
	private static readonly Random _SharedRandom = Random.Shared;

	public static T PickRandom<T>(ICollection<T> collection)
	{
		if (collection.Count == 0)
		{
			throw new ArgumentException("集合不能为空", "collection");
		}
		int num = _SharedRandom.Next(collection.Count);
		if (collection is IList<T> list)
		{
			return list[num];
		}
		return collection.Skip(num).First();
	}

	public static int NextInt(int min, int max)
	{
		if (min <= max)
		{
			return _SharedRandom.Next(min, max + 1);
		}
		throw new ArgumentOutOfRangeException("min", "最小值不能大于最大值");
	}

	public static List<T> Shuffle<T>(IList<T> list)
	{
		List<T> list2 = new List<T>(list);
		for (int num = list2.Count - 1; num > 0; num--)
		{
			int num2 = _SharedRandom.Next(0, num + 1);
			List<T> list3 = list2;
			int index = num;
			List<T> list4 = list2;
			int index2 = num2;
			T value = list2[num2];
			T value2 = list2[num];
			list3[index] = value;
			list4[index2] = value2;
		}
		return list2;
	}

	public static void ShuffleInPlace<T>(IList<T> list)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int num2 = _SharedRandom.Next(0, num + 1);
			int index = num;
			int index2 = num2;
			T value = list[num2];
			T value2 = list[num];
			list[index] = value;
			list[index2] = value2;
		}
	}
}
