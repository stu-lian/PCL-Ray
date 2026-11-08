using System;

namespace PCL.Core.UI.Animation.Animatable;

public sealed class ClrAnimatable<TOwner, T> : IAnimatable
{
	private readonly TOwner _owner;

	private readonly Func<TOwner, T> _getter;

	private readonly Action<TOwner, T> _setter;

	public ClrAnimatable(TOwner owner, Func<TOwner, T> getter, Action<TOwner, T> setter)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("owner");
		}
		_owner = owner;
		_getter = getter ?? throw new ArgumentNullException("getter");
		_setter = setter ?? throw new ArgumentNullException("setter");
	}

	public T GetValue()
	{
		return _getter(_owner);
	}

	public void SetValue(T value)
	{
		_setter(_owner, value);
	}

	object? IAnimatable.GetValue()
	{
		return GetValue();
	}

	void IAnimatable.SetValue(object? value)
	{
		SetValue((T)value);
	}

	void IAnimatable.SetValue<TValue>(TValue value)
	{
		SetValue((T)(object)value);
	}
}
