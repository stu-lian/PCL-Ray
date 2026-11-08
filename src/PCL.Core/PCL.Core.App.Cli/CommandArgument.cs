using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PCL.Core.App.Cli;

public abstract class CommandArgument
{
	public required string Key { get; init; }

	public required string ValueText { get; init; }

	public abstract ArgumentValueKind ValueKind { get; }

	public abstract bool TryCastValue<T>([NotNullWhen(true)] out T? value);

	public T? CastValue<T>()
	{
		if (!TryCastValue<T>(out T value))
		{
			throw new InvalidCastException("Value type mismatch or cannot cast");
		}
		return value;
	}
}
public abstract class CommandArgument<TValue> : CommandArgument
{
	private bool _isValueParsed;

	public TValue Value
	{
		get
		{
			if (_isValueParsed)
			{
				return field;
			}
			_isValueParsed = true;
			return field = ParseValueText();
		}
		protected init
		{
			field = value;
			_isValueParsed = true;
		}
	}

	protected abstract TValue ParseValueText();

	public override bool TryCastValue<T>([NotNullWhen(true)] out T value)
	{
		TValue value2 = Value;
		if (value2 is T val)
		{
			value = val;
			return true;
		}
		value = default;
		if (typeof(T) == typeof(string))
		{
			Unsafe.As<T, string>(ref value) = ValueText;
			return true;
		}
		return false;
	}
}
