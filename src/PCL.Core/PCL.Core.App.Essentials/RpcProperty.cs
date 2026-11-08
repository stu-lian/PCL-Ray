using System;

namespace PCL.Core.App.Essentials;

public class RpcProperty
{
	public delegate void GetValueDelegate(out string? outValue);

	public delegate void SetValueDelegate(string? value, ref bool success);

	public readonly string Name;

	public readonly bool Settable = true;

	public string? Value
	{
		get
		{
			GetValue(out var outValue);
			return outValue;
		}
		set
		{
			bool success = true;
			SetValue?.Invoke(value, ref success);
			if (!success)
			{
				throw new RpcPropertyOperationFailedException();
			}
		}
	}

	public event GetValueDelegate GetValue;

	public event SetValueDelegate? SetValue;

	public RpcProperty(string name, Func<string?> onGetValue, Action<string?>? onSetValue = null, bool settable = false)
	{
		Name = name;
		GetValue += delegate(out string? outValue)
		{
			outValue = onGetValue();
		};
		if (onSetValue != null)
		{
			SetValue += delegate(string? value, ref bool _)
			{
				onSetValue(value);
			};
		}
		else if (!settable)
		{
			Settable = false;
			SetValue += delegate(string? _, ref bool success)
			{
				success = false;
			};
		}
	}
}
