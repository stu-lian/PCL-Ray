using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PCL.Core.App.Cli;

public class DecimalArgument : CommandArgument<decimal>
{
	public override ArgumentValueKind ValueKind => ArgumentValueKind.Decimal;

	public new decimal Value
	{
		get
		{
			return base.Value;
		}
		init
		{
			base.Value = value;
		}
	}

	protected override decimal ParseValueText()
	{
		return decimal.Parse(ValueText);
	}

	public override bool TryCastValue<T>([NotNullWhen(true)] out T value)
	{
		if (base.TryCastValue<T>(out value))
		{
			return true;
		}
		Type typeFromHandle = typeof(T);
		checked
		{
			try
			{
				if (typeFromHandle == typeof(int))
				{
					Unsafe.As<T, int>(ref value) = Convert.ToInt32(Value);
				}
				else if (typeFromHandle == typeof(long))
				{
					Unsafe.As<T, long>(ref value) = Convert.ToInt64(Value);
				}
				else if (typeFromHandle == typeof(double))
				{
					Unsafe.As<T, double>(ref value) = Convert.ToDouble(Value);
				}
				else if (typeFromHandle == typeof(float))
				{
					Unsafe.As<T, float>(ref value) = Convert.ToSingle(Value);
				}
				else if (typeFromHandle == typeof(short))
				{
					Unsafe.As<T, short>(ref value) = Convert.ToInt16(Value);
				}
				else if (typeFromHandle == typeof(sbyte))
				{
					Unsafe.As<T, sbyte>(ref value) = Convert.ToSByte(Value);
				}
				else if (typeFromHandle == typeof(ulong))
				{
					Unsafe.As<T, ulong>(ref value) = Convert.ToUInt64(Value);
				}
				else if (typeFromHandle == typeof(uint))
				{
					Unsafe.As<T, uint>(ref value) = Convert.ToUInt32(Value);
				}
				else if (typeFromHandle == typeof(ushort))
				{
					Unsafe.As<T, ushort>(ref value) = Convert.ToUInt16(Value);
				}
				else if (typeFromHandle == typeof(byte))
				{
					Unsafe.As<T, byte>(ref value) = Convert.ToByte(Value);
				}
				else if (typeFromHandle == typeof(nint))
				{
					Unsafe.As<T, nint>(ref value) = (nint)Convert.ToInt64(Value);
				}
				else
				{
					if (!(typeFromHandle == typeof(nuint)))
					{
						return false;
					}
					Unsafe.As<T, nuint>(ref value) = (nuint)Convert.ToUInt64(Value);
				}
				return true;
			}
			catch (Exception ex) when ((ex is OverflowException || ex is InvalidCastException || ex is FormatException) ? true : false)
			{
				return false;
			}
		}
	}
}
