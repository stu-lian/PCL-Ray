using System;
using System.Runtime.InteropServices;
using System.Security;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.WinRT;
using PCL.Core.Utils.WinRT.Interface;
using PCL.Core.Utils.WinRT.Interface.Windows.Data.Xml.Dom;
using PCL.Core.Utils.WinRT.Interface.Windows.UI.Notifications;

namespace PCL.Core.UI;

public static class ToastNotification
{
	public static void SendToast(string message, string title = "Notice")
	{
		SendToastFromTemplate($"<toast>\n    <visual>\n        <binding template=\"ToastGeneric\">\n            <text>{SecurityElement.Escape(title)}</text>\n            <text>{SecurityElement.Escape(message)}</text>\n        </binding>\n    </visual>\n</toast>");
	}

	public unsafe static void SendToastFromTemplate(string xml)
	{
		HString hString = HStringHelper.ToHString(xml);
		if (!AumidHelper.HasAumid())
		{
			AumidHelper.RegisterAumid();
		}
		HString hString2 = HStringHelper.ToHString("PCLCommunity.PCLCE");
		IInspectable* ptr = null;
		IXmlDocumentIO* ptr2 = null;
		IToastNotificationFactory* ptr3 = null;
		IToastNotificationManagerStatics* ptr4 = null;
		IInspectable* ptr5 = null;
		IToastNotifier* ptr6 = null;
		try
		{
			fixed (Guid* iid = &IXmlDocumentIOInfo.Iid)
			{
				ptr = (IInspectable*)WinRTInterop.ActivateInstance(IXmlDocumentIOInfo.ActivatableClassId);
				Marshal.ThrowExceptionForHR(ptr->lpVtbl->QueryInterface(ptr, iid, (void**)(&ptr2)));
				Marshal.ThrowExceptionForHR(ptr2->lpVtbl->LoadXml(ptr2, (nint)hString));
				ptr3 = (IToastNotificationFactory*)WinRTInterop.GetActivationFactory(IToastNotificationFactoryInfo.ActivatableClassId, IToastNotificationFactoryInfo.Iid);
				Marshal.ThrowExceptionForHR(ptr3->lpVtbl->CreateToastNotification(ptr3, ptr2, (void**)(&ptr5)));
				ptr4 = (IToastNotificationManagerStatics*)WinRTInterop.GetActivationFactory(IToastNotificationManagerStaticsInfo.ActivatableClassId, IToastNotificationManagerStaticsInfo.Iid);
				Marshal.ThrowExceptionForHR(ptr4->lpVtbl->CreateToastNotifierWithId(ptr4, (nint)hString2, (void**)(&ptr6)));
				Marshal.ThrowExceptionForHR(ptr6->lpVtbl->Show(ptr6, ptr5));
			}
		}
		finally
		{
			if (ptr6 != null)
			{
				ptr6->lpVtbl->Release(ptr6);
			}
			if (ptr5 != null)
			{
				ptr5->lpVtbl->Release(ptr5);
			}
			if (ptr4 != null)
			{
				ptr4->lpVtbl->Release(ptr4);
			}
			if (ptr3 != null)
			{
				ptr3->lpVtbl->Release(ptr3);
			}
			if (ptr2 != null)
			{
				ptr2->lpVtbl->Release(ptr2);
			}
			if (ptr != null)
			{
				ptr->lpVtbl->Release(ptr);
			}
			HStringHelper.DeleteHString(hString);
			HStringHelper.DeleteHString(hString2);
		}
	}

	public static void UninstallToasts()
	{
		AumidHelper.UnregisterAumid();
	}
}
