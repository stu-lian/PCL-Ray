using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PCL.Core.App.Cli;

public class BoolArgument : CommandArgument<bool>
{
	public override ArgumentValueKind ValueKind => ArgumentValueKind.Bool;

	protected override bool ParseValueText()
	{
		string text = ValueText.ToLowerInvariant().Trim();
		bool flag = ((text == "0" || text == "false") ? true : false);
		return !flag;
	}

	public override bool TryCastValue<T>([NotNullWhen(true)] out T value)
	{
		if (base.TryCastValue<T>(out value))
		{
			return true;
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle != typeof(sbyte) && typeFromHandle != typeof(byte) && typeFromHandle != typeof(short) && typeFromHandle != typeof(ushort) && typeFromHandle != typeof(int) && typeFromHandle != typeof(uint) && typeFromHandle != typeof(long) && typeFromHandle != typeof(ulong) && typeFromHandle != typeof(nint) && typeFromHandle != typeof(nuint))
		{
			return false;
		}
		bool source = Value;
		Unsafe.As<T, byte>(ref value) = Unsafe.As<bool, byte>(ref source);
		return true;
	}
}
