using System;

namespace PCL.Core.Utils.WinRT.Interface.Windows.UI.Notifications;

public struct IToastNotificationFactoryVtbl
{
	public unsafe delegate* unmanaged<void*, Guid*, void**, int> QueryInterface;

	public unsafe delegate* unmanaged<void*, uint> AddRef;

	public unsafe delegate* unmanaged<void*, uint> Release;

	public unsafe delegate* unmanaged<void*, uint*, Guid**, int> GetIids;

	public unsafe delegate* unmanaged<void*, nint*, int> GetRuntimeClassName;

	public unsafe delegate* unmanaged<void*, int*, int> GetTrustLevel;

	public unsafe delegate* unmanaged<void*, void*, void**, int> CreateToastNotification;
}
