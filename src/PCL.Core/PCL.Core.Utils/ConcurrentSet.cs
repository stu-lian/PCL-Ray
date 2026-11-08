using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PCL.Core.Utils;

public sealed class ConcurrentSet<T> : IProducerConsumerCollection<T>, IEnumerable<T>, IEnumerable, ICollection, ICollection<T> where T : notnull
{
	private readonly ConcurrentDictionary<T, object?> _dictionary = new ConcurrentDictionary<T, object>();

	public bool IsReadOnly => false;

	public int Count => _dictionary.Count;

	object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

	bool ICollection.IsSynchronized => ((ICollection)_dictionary).IsSynchronized;

	public bool IgnoreDuplicated { get; init; }

	public bool TryAdd(T item)
	{
		if (!_dictionary.TryAdd(item, null) && !IgnoreDuplicated)
		{
			return false;
		}
		return true;
	}

	public bool TryTake([UnscopedRef] out T item)
	{
		while (true)
		{
			ICollection<T> keys = _dictionary.Keys;
			if (keys.Count == 0)
			{
				break;
			}
			foreach (T item2 in keys)
			{
				if (_dictionary.TryRemove(item2, out object _))
				{
					item = item2;
					return true;
				}
			}
		}
		item = default;
		return false;
	}

	public bool Remove(T item)
	{
		object value;
		return _dictionary.TryRemove(item, out value);
	}

	public void Add(T item)
	{
		if (!_dictionary.TryAdd(item, null) && !IgnoreDuplicated)
		{
			throw new ArgumentException("ConcurrentSet 中已存在该元素");
		}
	}

	public void Clear()
	{
		_dictionary.Clear();
	}

	public bool Contains(T item)
	{
		return _dictionary.ContainsKey(item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _dictionary.Keys.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void CopyTo(T[] array, int index)
	{
		_dictionary.Keys.CopyTo(array, index);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CopyTo((T[])array, index);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		CopyTo(array, arrayIndex);
	}

	public T[] ToArray()
	{
		return _dictionary.Keys.ToArray();
	}
}
