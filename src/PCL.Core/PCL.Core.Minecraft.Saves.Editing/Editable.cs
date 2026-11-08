using System;

namespace PCL.Core.Minecraft.Saves.Editing;

public readonly record struct Editable<T> where T : struct
{
	private readonly T _value;

	private readonly bool _hasValue;

	public bool HasValue => _hasValue;

	public T Value
	{
		get
		{
			if (!_hasValue)
			{
				throw new InvalidOperationException("Editable 没有值。");
			}
			return _value;
		}
	}

	public Editable(T value)
	{
		_value = value;
		_hasValue = true;
	}

	public T GetValueOrDefault(T defaultValue)
	{
		if (!_hasValue)
		{
			return defaultValue;
		}
		return _value;
	}

	public bool TryGetValue(out T value)
	{
		value = _value;
		return _hasValue;
	}

	public override string ToString()
	{
		if (!_hasValue)
		{
			return "<unspecified>";
		}
		return _value.ToString() ?? "";
	}
}
