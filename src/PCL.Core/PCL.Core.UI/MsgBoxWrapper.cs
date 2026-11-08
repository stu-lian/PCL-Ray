using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PCL.Core.App.Localization;

namespace PCL.Core.UI;

public static class MsgBoxWrapper
{
	public static event MsgBoxHandler? OnShow;

	public static int ShowWithCustomButtons(string message, string? caption, MsgBoxTheme theme, bool block, ICollection<MsgBoxButtonInfo> buttonCollection)
	{
		int result = 0;
		if (buttonCollection.Count == 0)
		{
			int num = 1;
			List<MsgBoxButtonInfo> list = new List<MsgBoxButtonInfo>(num);
			CollectionsMarshal.SetCount(list, num);
			CollectionsMarshal.AsSpan(list)[0] = new MsgBoxButtonInfo(Lang.Text("Common.Action.Confirm"));
			buttonCollection = list;
		}
		OnShow?.Invoke(message, _ResolveCaption(caption, theme), buttonCollection, theme, block, ref result);
		return result;
	}

	public static int ShowWithCustomButtons(string message, string? caption = null, MsgBoxTheme theme = MsgBoxTheme.Info, bool block = true, params MsgBoxButtonInfo[] buttons)
	{
		return ShowWithCustomButtons(message, caption, theme, block, (ICollection<MsgBoxButtonInfo>)buttons);
	}

	public static int Show(string message, string? caption = null, MsgBoxTheme theme = MsgBoxTheme.Info, bool block = true, params string[] buttons)
	{
		int index = 0;
		List<MsgBoxButtonInfo> buttonCollection = buttons.Select((string button) => new MsgBoxButtonInfo(button, ++index)).ToList();
		return ShowWithCustomButtons(message, caption, theme, block, buttonCollection);
	}

	private static string _ResolveCaption(string? caption, MsgBoxTheme theme)
	{
		if (!string.IsNullOrWhiteSpace(caption))
		{
			return caption;
		}
		return theme switch
		{
			MsgBoxTheme.Warning => Lang.Text("Common.Dialog.Warning"), 
			MsgBoxTheme.Error => Lang.Text("SystemDialog.Error.Title"), 
			_ => Lang.Text("Common.Dialog.Title"), 
		};
	}
}
