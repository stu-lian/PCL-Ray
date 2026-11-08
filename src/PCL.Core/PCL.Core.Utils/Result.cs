using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PCL.Core.Utils;

[DebuggerDisplay("{_DebuggerDisplay}")]
public sealed class Result<TOk, TErr>
{
	private readonly TOk? _value;

	private readonly TErr? _error;

	private readonly bool _isSuccess;

	public bool IsSuccess => _isSuccess;

	public bool IsFailure => !_isSuccess;

	public TOk Value
	{
		get
		{
			if (!IsSuccess)
			{
				throw new InvalidOperationException("Cannot access Value of a failed Result.");
			}
			return _value;
		}
	}

	public TErr Error
	{
		get
		{
			if (!IsFailure)
			{
				throw new InvalidOperationException("Cannot access Error of a successful Result.");
			}
			return _error;
		}
	}

	private string _DebuggerDisplay
	{
		get
		{
			if (IsSuccess)
			{
				return $"Ok({_value})";
			}
			return $"Err({_error})";
		}
	}

	private Result(TOk value)
	{
		_value = value;
		_isSuccess = true;
	}

	private Result(TErr error)
	{
		_error = error;
		_isSuccess = false;
	}

	public static Result<TOk, TErr> Ok(TOk value)
	{
		return new Result<TOk, TErr>(value);
	}

	public static Result<TOk, TErr> Err(TErr error)
	{
		return new Result<TOk, TErr>(error);
	}

	public TR Match<TR>(Func<TOk, TR> onSuccess, Func<TErr, TR> onError)
	{
		if (!IsSuccess)
		{
			return onError(_error);
		}
		return onSuccess(_value);
	}

	public void IfSuccess(Action<TOk> action)
	{
		if (IsSuccess)
		{
			action(_value);
		}
	}

	public void IfFailure(Action<TErr> action)
	{
		if (IsFailure)
		{
			action(_error);
		}
	}

	public Result<T2, TErr> Map<T2>(Func<TOk, T2> mapper)
	{
		if (!IsSuccess)
		{
			return Result<T2, TErr>.Err(_error);
		}
		return Result<T2, TErr>.Ok(mapper(_value));
	}

	public Result<TOk, TErr2> MapError<TErr2>(Func<TErr, TErr2> mapper)
	{
		if (!IsSuccess)
		{
			return Result<TOk, TErr2>.Err(mapper(_error));
		}
		return Result<TOk, TErr2>.Ok(_value);
	}

	public Result<T2, TErr> Bind<T2>(Func<TOk, Result<T2, TErr>> mapper)
	{
		if (!IsSuccess)
		{
			return Result<T2, TErr>.Err(_error);
		}
		return mapper(_value);
	}

	public Result<TOk, TErr2> BindError<TErr2>(Func<TErr, Result<TOk, TErr2>> mapper)
	{
		if (!IsFailure)
		{
			return Result<TOk, TErr2>.Ok(_value);
		}
		return mapper(_error);
	}

	public TOk OrElse(TOk defaultValue)
	{
		if (!IsSuccess)
		{
			return defaultValue;
		}
		return _value;
	}

	public TOk OrElse(Func<TOk> defaultValueFactory)
	{
		if (!IsSuccess)
		{
			return defaultValueFactory();
		}
		return _value;
	}

	public TOk Expect(string message)
	{
		if (!IsSuccess)
		{
			throw new InvalidOperationException(message);
		}
		return _value;
	}

	public TOk UnwrapOrThrow<TException>(Func<TErr, TException> exceptionFactory) where TException : Exception
	{
		if (!IsSuccess)
		{
			throw exceptionFactory(_error);
		}
		return _value;
	}

	public override string ToString()
	{
		return _DebuggerDisplay;
	}

	public override bool Equals(object? obj)
	{
		if (!(obj is Result<TOk, TErr> result))
		{
			return false;
		}
		if (IsSuccess != result.IsSuccess)
		{
			return false;
		}
		if (!IsSuccess)
		{
			return EqualityComparer<TErr>.Default.Equals(_error, result._error);
		}
		return EqualityComparer<TOk>.Default.Equals(_value, result._value);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(IsSuccess, _value, _error);
	}
}
