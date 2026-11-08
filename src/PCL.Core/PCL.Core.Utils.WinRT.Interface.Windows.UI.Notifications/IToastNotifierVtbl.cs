using System;

namespace PCL.Core.Utils.WinRT.Interface.Windows.UI.Notifications;

public struct IToastNotifierVtbl
{
	public unsafe delegate* unmanaged<void*, Guid*, void**, int> QueryInterface;

	public unsafe delegate* unmanaged<void*, uint> AddRef;

	public unsafe delegate* unmanaged<void*, uint> Release;

	public unsafe delegate* unmanaged<void*, uint*, Guid**, int> GetIids;

	public unsafe delegate* unmanaged<void*, nint*, int> GetRuntimeClassName;

	public unsafe delegate* unmanaged<void*, int*, int> GetTrustLevel;

	public unsafe delegate* unmanaged<void*, void*, int> Show;

	public unsafe delegate* unmanaged<void*, void*, int> Hide;

	public unsafe delegate* unmanaged<void*, int, int> get_Setting;

	public unsafe delegate* unmanaged<void*, void*, int> AddToSchedule;

	public unsafe delegate* unmanaged<void*, void*, int> RemoveFromSchedule;

	public unsafe delegate* unmanaged<void*, void**, int> GetScheduledToastNotifications;
}
