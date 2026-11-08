using System;

namespace PCL.Core.Utils;

public class ParameterizedProperty<TParam, TValue>
{
	public Func<TParam, TValue> GetValue { private get; init; }

	public Action<TParam, TValue> SetValue { private get; init; }

	public TValue this[TParam param]
	{
		get
		{
			return GetValue(param);
		}
		set
		{
			SetValue(param, value);
		}
	}
}
