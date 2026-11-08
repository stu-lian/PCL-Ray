using System;
using System.Linq;
using System.Windows;
using PCL.Core.UI;

namespace PCL;

public static class HintService
{
	private struct HintMessage
	{
		public string Text;

		public HintType Type;

		public bool Log;
	}

	private static ModBase.SafeList<HintMessage> HintWaiting
	{
		get
		{
			return field ?? (field = new ModBase.SafeList<HintMessage>());
		}
		set;
	}

	public static void Hint(string? text, HintType type = HintType.Info, bool log = true)
	{
		string normalized = (text ?? "").Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
		if (!HintWaiting.Any((HintMessage h) => h.Text == normalized && h.Type == type))
		{
			HintWaiting.Add(new HintMessage
			{
				Text = normalized,
				Type = type,
				Log = log
			});
		}
	}

	public static void HintWrapper_OnShow(string message, HintTheme messageTheme)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected I4, but got Unknown
		Hint(message, ((int)messageTheme - 1) switch
		{
			0 => HintType.Success, 
			1 => HintType.Error, 
			2 => HintType.Warning, 
			_ => HintType.Info, 
		});
	}

	internal static void Tick()
	{
		try
		{
			ModMain.frmMain.PanHint.HorizontalAlignment = HorizontalAlignment.Right;
			ModMain.frmMain.PanHint.VerticalAlignment = VerticalAlignment.Bottom;
			double actualHeight = ModMain.frmMain.PanExtraButtons.ActualHeight;
			ModMain.frmMain.PanHint.Margin = new Thickness(0.0, 0.0, 0.0, (actualHeight > 0.0) ? (actualHeight + 20.0) : 20.0);
			if (!HintWaiting.Any())
			{
				return;
			}
			HintMessage currentHint = HintWaiting[0];
			MyToast myToast = ModMain.frmMain.PanHint.Children.OfType<MyToast>().FirstOrDefault((MyToast t) => !t.IsDismissing && t.Context == currentHint.Text && t.ToastType == currentHint.Type);
			if (myToast != null)
			{
				myToast.Emphasize();
				HintWaiting.RemoveAt(0);
				return;
			}
			if (ModMain.frmMain.PanHint.Children.OfType<MyToast>().Count((MyToast t) => !t.IsDismissing) >= 5)
			{
				ModMain.frmMain.PanHint.Children.OfType<MyToast>().FirstOrDefault((MyToast t) => !t.IsDismissing)?.Dismiss();
				return;
			}
			MyToast myToast2 = new MyToast();
			myToast2.Context = currentHint.Text;
			myToast2.ToastType = currentHint.Type;
			MyToast myToast3 = myToast2;
			myToast3.Icon = currentHint.Type switch
			{
				HintType.Success => "lucide/circle-check", 
				HintType.Error => "lucide/circle-minus", 
				HintType.Warning => "lucide/triangle-alert", 
				_ => "lucide/info", 
			};
			myToast2.DisplayDuration = (800.0 + ModBase.MathClamp(currentHint.Text.Length, 5.0, 23.0) * 180.0) * ModAnimation.aniSpeed;
			MyToast myToast4 = myToast2;
			ModMain.frmMain.PanHint.Children.Add(myToast4);
			myToast4.Show();
			if (currentHint.Log)
			{
				ModBase.Log("[UI] 弹出提示：" + currentHint.Text);
			}
			HintWaiting.RemoveAt(0);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "显示弹出提示失败", ModBase.LogLevel.Normal);
		}
	}

	public static void HideAll()
	{
		foreach (MyToast item in ModMain.frmMain.PanHint.Children.OfType<MyToast>().ToList())
		{
			item.Dismiss();
		}
	}
}
