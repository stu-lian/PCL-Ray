using System;

namespace PCL.Core.Utils.WinRT.Interface;

public struct IInspectableVtbl
{
	public unsafe delegate* unmanaged<void*, Guid*, void**, int> QueryInterface;

	public unsafe delegate* unmanaged<void*, uint> AddRef;

	public unsafe delegate* unmanaged<void*, uint> Release;

	public unsafe delegate* unmanaged<void*, uint*, Guid**, int> GetIids;

	public unsafe delegate* unmanaged<void*, nint*, int> GetRuntimeClassName;

	public unsafe delegate* unmanaged<void*, int*, int> GetTrustLevel;
}
