using System;
using System.Runtime.CompilerServices;

namespace PCL.Core.Utils.WinRT;

public readonly record struct HString(nint Value)
{
	public readonly nint Value = Value;

	public bool IsNull => Value == IntPtr.Zero;

	public static implicit operator nint(HString h)
	{
		return h.Value;
	}

	public static explicit operator HString(nint ptr)
	{
		return new HString(ptr);
	}

	[CompilerGenerated]
	public void Deconstruct(out nint Value)
	{
		Value = this.Value;
	}
}
