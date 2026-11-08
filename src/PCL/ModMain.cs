using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using FluentValidation;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;

namespace PCL;

public static class ModMain
{
	public class MyMsgBoxConverter
	{
		public string Button1 = "";

		public Action Button1Action;

		public string Button2 = "";

		public Action Button2Action;

		public string Button3 = "";

		public Action Button3Action;

		public object Content;

		public Func<JsonObject, CancellationToken, Task<AuthorizeResult?>> DeviceCodePoll;

		public Func<AuthorizeResult, CancellationToken, Task>? LoginResultHandler;

		public Action<object>? CompletionHandler;

		public bool ForceWait;

		public bool MultiSelect;

		public bool HighLight;

		public string HintText = "";

		public bool IsExited;

		public bool IsWarn;

		public object Result;

		public string Text;

		public string Title;

		public MyMsgBoxType Type;

		public Collection<IValidator<string>> ValidateRules;

		public DispatcherFrame WaitFrame = new DispatcherFrame(true);

		public string AuthServerDefault = "";

		public IReadOnlyDictionary<string, string>? AuthServerPresets;

		public MyMsgBoxConverter()
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Expected Obj, but got Unknown
		}
	}

	public enum MyMsgBoxType
	{
		Text,
		Select,
		Input,
		AuthServer,
		Login,
		Markdown
	}

	public static FormMain? frmMain;

	public static SplashScreen? frmStart;

	public static PageLaunchLeft? frmLaunchLeft;

	public static PageLaunchRight? frmLaunchRight;

	public static PageLogLeft? frmLogLeft;

	public static PageLogRight? frmLogRight;

	public static PageSelectLeft? frmSelectLeft;

	public static PageSelectRight? frmSelectRight;

	public static PageSpeedLeft? frmSpeedLeft;

	public static PageSpeedRight? frmSpeedRight;

	public static PageToolsLeft? frmToolsLeft;

	public static PageToolsGameLink? frmToolsGameLink;

	public static PageToolsTest? frmToolsTest;

	public static PageToolsMapViewer? frmToolsMapViewer;

	public static PageToolsModTranslate? frmToolsModTranslate;

	public static PageToolsLogAnalyse? frmToolsLogAnalyse;

	public static PageToolsColorCode? frmToolsColorCode;

	public static PageToolsHeadGenerator? frmToolsHeadGenerator;
	public static PageToolsSummon? frmToolsSummon;
	public static PageToolsShield? frmToolsShield;
	public static PageToolsFirework? frmToolsFirework;
	public static PageToolsBeacon? frmToolsBeacon;
	public static PageToolsArmorTrim? frmToolsArmorTrim;
	public static PageToolsTitle? frmToolsTitle;
	public static PageToolsPlayerData? frmToolsPlayerData;
	public static PageToolsHardcoreReviver? frmToolsHardcoreReviver;
	public static PageToolsNbtEditor? frmToolsNbtEditor;


	public static PageDownloadLeft? frmDownloadLeft;

	public static PageDownloadInstall? frmDownloadInstall;

	public static PageDownloadClient? frmDownloadClient;

	public static PageDownloadOptiFine? frmDownloadOptiFine;

	public static PageDownloadLiteLoader? frmDownloadLiteLoader;

	public static PageDownloadForge? frmDownloadForge;

	public static PageDownloadNeoForge? frmDownloadNeoForge;

	public static PageDownloadCleanroom? frmDownloadCleanroom;

	public static PageDownloadFabric? frmDownloadFabric;

	public static PageDownloadLabyMod? frmDownloadLabyMod;

	public static PageDownloadLegacyFabric? frmDownloadLegacyFabric;

	public static PageDownloadMod? frmDownloadMod;

	public static PageDownloadPack? frmDownloadPack;

	public static PageDownloadDataPack? frmDownloadDataPack;

	public static PageDownloadShader? frmDownloadShader;

	public static PageDownloadResourcePack? frmDownloadResourcePack;

	public static PageDownloadWorld? frmDownloadWorld;

	public static PageDownloadCompFavorites? frmDownloadCompFavorites;

	public static PageSetupLeft? frmSetupLeft;

	public static PageSetupLaunch? frmSetupLaunch;

	public static PageSetupUI? frmSetupUI;

	public static PageSetupGameManage? frmSetupGameManage;

	public static PageSetupUpdate? frmSetupUpdate;

	public static PageSetupJava? frmSetupJava;

	public static PageSetupAbout? frmSetupAbout;

	public static PageSetupLog? frmSetupLog;

	public static PageSetupFeedback? frmSetupFeedback;

	public static PageSetupGameLink? frmSetupGameLink;

	public static PageSetupLauncherLanguage? frmSetupLauncherLanguage;

	public static PageSetupLauncherMisc? frmSetupLauncherMisc;

	public static PageLoginAuth? frmLoginAuth;

	public static PageLoginMs? frmLoginMs;

	public static PageLoginProfile? frmLoginProfile;

	public static PageLoginProfileSkin? frmLoginProfileSkin;

	public static PageLoginOffline? frmLoginOffline;

	public static PageInstanceLeft? frmInstanceLeft;

	public static PageInstanceOverall? frmInstanceOverall;

	public static PageInstanceCompResource? frmInstanceMod;

	public static PageInstanceModDisabled? frmInstanceModDisabled;

	public static PageInstanceCompJarInJar? frmInstanceModJarInJar;

	public static PageInstanceScreenshot? frmInstanceScreenshot;

	public static PageInstanceSaves? frmInstanceSaves;

	public static PageInstanceCompResource? frmInstanceShader;

	public static PageInstanceCompResource? frmInstanceSchematic;

	public static PageInstanceCompResource? frmInstanceResourcePack;

	public static PageInstanceSetup? frmInstanceSetup;

	public static PageInstanceInstall? frmInstanceInstall;

	public static PageInstanceExport? frmInstanceExport;

	public static PageInstanceServer? frmInstanceServer;

	public static PageInstanceSavesLeft? frmInstanceSavesLeft;

	public static PageInstanceSavesInfo? frmInstanceSavesInfo;

	public static PageInstanceSavesDatapack? frmInstanceSavesDatapack;

	public static PageDownloadCompDetail? frmDownloadCompDetail;

	public static PageHomepageNewsView? frmHomepageNews;

	public static MySlider? dragControl = null;

	private static int timer4Count;

	private static int timer150Count;

	public static bool isAprilEnabled = DateTime.Now.Month == 4 && DateTime.Now.Day == 1;

	public static bool isAprilGiveup = false;

	private static Vector aprilSpeed = new Vector(0.0, 0.0);

	private static int aprilIdieCount;

	private static Point aprilMousePosLast = new Point(0.0, 0.0);

	private static int aprilDistance;

	private static bool isTaskTempCleared;

	private static bool isTaskTempClearing;

	public static List<MyMsgBoxConverter> WaitingMyMsgBox { get; } = new List<MyMsgBoxConverter>();

	private static void TimerMain()
	{
		try
		{
			HintService.Tick();
			MyMsgBoxTick();
			frmMain.DragTick();
			ModLoader.LoaderTaskbarProgressRefresh();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "短程主时钟执行异常", ModBase.LogLevel.Critical, null, Lang.Text("Main.Error.OperationFailed"));
		}
		timer4Count++;
		if (timer4Count == 4)
		{
			timer4Count = 0;
		}
		timer150Count++;
		if (timer150Count != 150)
		{
			return;
		}
		timer150Count = 0;
		try
		{
			if (frmMain.BtnExtraApril_ShowCheck() && aprilDistance != 0)
			{
				frmMain.BtnExtraApril.Ribble();
			}
			ModBase.RunInUi(() =>
			{
				if (!frmMain.Hidden)
				{
					if (frmMain.Top < -9000.0)
					{
						frmMain.Top = 100.0;
					}
					if (frmMain.Left < -9000.0)
					{
						frmMain.Left = 100.0;
					}
				}
			});
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "长程主时钟执行异常", ModBase.LogLevel.Critical, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	public static void TimerMainStart()
	{
		ModBase.RunInNewThread(() =>
		{
			try
			{
				while (true)
				{
					ModBase.RunInUiWait(TimerMain);
					Thread.Sleep((int)Math.Round(49.0));
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "程序主时钟出错", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
			}
		}, "Timer Main");
		if (!isAprilEnabled)
		{
			return;
		}
		ModBase.RunInNewThread(() =>
		{
			try
			{
				int tickCount = Environment.TickCount;
				while (true)
				{
					if (tickCount != Environment.TickCount)
					{
						tickCount = Environment.TickCount;
						ModBase.RunInUiWait(TimerFool);
					}
					Thread.Sleep(1);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "愚人节主时钟出错", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
			}
		}, "Timer Main Fool");
	}

	private static string GetDefaultDialogTitle()
	{
		return Lang.Text("Common.Dialog.Title");
	}

	private static string GetDefaultConfirmText()
	{
		return Lang.Text("Common.Action.Confirm");
	}

	private static string GetDefaultCancelText()
	{
		return Lang.Text("Common.Action.Cancel");
	}

	public static int MyMsgBox(string caption, string? title = null, string? button1 = null, string? button2 = "", string? button3 = "", bool isWarn = false, bool highLight = true, bool forceWait = false, Action button1Action = null, Action button2Action = null, Action button3Action = null)
	{
		if (title == null)
		{
			title = GetDefaultDialogTitle();
		}
		if (button1 == null)
		{
			button1 = GetDefaultConfirmText();
		}
		if (button2 == null)
		{
			button2 = "";
		}
		if (button3 == null)
		{
			button3 = "";
		}
		MyMsgBoxConverter myMsgBoxConverter = new MyMsgBoxConverter
		{
			Type = MyMsgBoxType.Text,
			Button1 = button1,
			Button2 = button2,
			Button3 = button3,
			Text = caption,
			IsWarn = isWarn,
			Title = title,
			HighLight = highLight,
			ForceWait = true,
			Button1Action = button1Action,
			Button2Action = button2Action,
			Button3Action = button3Action
		};
		WaitingMyMsgBox.Add(myMsgBoxConverter);
		if (ModBase.RunInUi())
		{
			MyMsgBoxTick();
		}
		if ((button2.Length > 0) | forceWait)
		{
			if (frmMain == null || (frmMain.PanMsg == null && ModBase.RunInUi()))
			{
				WaitingMyMsgBox.Remove(myMsgBoxConverter);
				if (button2.Length > 0)
				{
					switch (Interaction.MsgBox(caption, (MsgBoxStyle)(((button3.Length > 0) ? 3 : 4) + (isWarn ? 16 : 32)), title))
					{
					case MsgBoxResult.Yes:
						myMsgBoxConverter.Result = 1;
						break;
					case MsgBoxResult.No:
						myMsgBoxConverter.Result = 2;
						break;
					case MsgBoxResult.Cancel:
						myMsgBoxConverter.Result = 3;
						break;
					}
				}
				else
				{
					Interaction.MsgBox(caption, isWarn ? MsgBoxStyle.Critical : MsgBoxStyle.Question, title);
					myMsgBoxConverter.Result = 1;
				}
				ModBase.Log("[Control] 主窗体加载完成前出现意料外的等待弹窗：" + button1 + "," + button2 + "," + button3, ModBase.LogLevel.Debug);
			}
			else
			{
				try
				{
					frmMain.DragStop();
					ComponentDispatcher.PushModal();
					Dispatcher.PushFrame(myMsgBoxConverter.WaitFrame);
				}
				finally
				{
					ComponentDispatcher.PopModal();
				}
			}
			ModBase.Log($"[Control] 普通弹框返回：{myMsgBoxConverter.Result ?? "null"}");
			return (int)myMsgBoxConverter.Result;
		}
		return 1;
	}

	public static int MyMsgBoxMarkdown(string caption, string? title = null, string? button1 = null, string? button2 = "", string? button3 = "", bool isWarn = false, bool highLight = true, bool forceWait = false, Action button1Action = null, Action button2Action = null, Action button3Action = null)
	{
		if (title == null)
		{
			title = GetDefaultDialogTitle();
		}
		if (button1 == null)
		{
			button1 = GetDefaultConfirmText();
		}
		if (button2 == null)
		{
			button2 = "";
		}
		if (button3 == null)
		{
			button3 = "";
		}
		MyMsgBoxConverter myMsgBoxConverter = new MyMsgBoxConverter
		{
			Type = MyMsgBoxType.Markdown,
			Button1 = button1,
			Button2 = button2,
			Button3 = button3,
			Text = caption,
			IsWarn = isWarn,
			Title = title,
			HighLight = highLight,
			ForceWait = true,
			Button1Action = button1Action,
			Button2Action = button2Action,
			Button3Action = button3Action
		};
		WaitingMyMsgBox.Add(myMsgBoxConverter);
		if (ModBase.RunInUi())
		{
			MyMsgBoxTick();
		}
		if ((button2.Length > 0) | forceWait)
		{
			if (frmMain == null || (frmMain.PanMsg == null && ModBase.RunInUi()))
			{
				WaitingMyMsgBox.Remove(myMsgBoxConverter);
				if (button2.Length > 0)
				{
					switch (Interaction.MsgBox(caption, (MsgBoxStyle)(((button3.Length > 0) ? 3 : 4) + (isWarn ? 16 : 32)), title))
					{
					case MsgBoxResult.Yes:
						myMsgBoxConverter.Result = 1;
						break;
					case MsgBoxResult.No:
						myMsgBoxConverter.Result = 2;
						break;
					case MsgBoxResult.Cancel:
						myMsgBoxConverter.Result = 3;
						break;
					}
				}
				else
				{
					Interaction.MsgBox(caption, isWarn ? MsgBoxStyle.Critical : MsgBoxStyle.Question, title);
					myMsgBoxConverter.Result = 1;
				}
				ModBase.Log("[Control] 主窗体加载完成前出现意料外的等待弹窗：" + button1 + "," + button2 + "," + button3, ModBase.LogLevel.Debug);
			}
			else
			{
				try
				{
					frmMain.DragStop();
					ComponentDispatcher.PushModal();
					Dispatcher.PushFrame(myMsgBoxConverter.WaitFrame);
				}
				finally
				{
					ComponentDispatcher.PopModal();
				}
			}
			ModBase.Log($"[Control] 普通弹框返回：{myMsgBoxConverter.Result ?? "null"}");
			return (int)myMsgBoxConverter.Result;
		}
		return 1;
	}

	public static string MyMsgBoxInput(string title, string text = "", string defaultInput = "", Collection<IValidator<string>>? validateRules = null, string hintText = "", string? button1 = null, string? button2 = null, bool isWarn = false)
	{
		if (button1 == null)
		{
			button1 = GetDefaultConfirmText();
		}
		if (button2 == null)
		{
			button2 = GetDefaultCancelText();
		}
		MyMsgBoxConverter myMsgBoxConverter = new MyMsgBoxConverter
		{
			Text = text,
			HintText = hintText,
			Type = MyMsgBoxType.Input,
			ValidateRules = (validateRules ?? new Collection<IValidator<string>>()),
			Button1 = button1,
			Button2 = button2,
			Content = defaultInput,
			IsWarn = isWarn,
			Title = title
		};
		WaitingMyMsgBox.Add(myMsgBoxConverter);
		try
		{
			frmMain?.DragStop();
			ComponentDispatcher.PushModal();
			Dispatcher.PushFrame(myMsgBoxConverter.WaitFrame);
		}
		finally
		{
			ComponentDispatcher.PopModal();
		}
		ModBase.Log($"[Control] 输入弹框返回：{myMsgBoxConverter.Result}");
		return myMsgBoxConverter.Result?.ToString();
	}

	public static string MyMsgBoxAuthServer(string defaultServer, IReadOnlyDictionary<string, string> presets, string? title = null)
	{
		MyMsgBoxConverter myMsgBoxConverter = new MyMsgBoxConverter
		{
			Type = MyMsgBoxType.AuthServer,
			Title = (title ?? Lang.Text("Launch.Account.Auth.SelectServer")),
			Button1 = GetDefaultConfirmText(),
			Button2 = GetDefaultCancelText(),
			AuthServerDefault = defaultServer,
			AuthServerPresets = presets,
			ForceWait = true
		};
		WaitingMyMsgBox.Add(myMsgBoxConverter);
		try
		{
			frmMain?.DragStop();
			ComponentDispatcher.PushModal();
			Dispatcher.PushFrame(myMsgBoxConverter.WaitFrame);
		}
		finally
		{
			ComponentDispatcher.PopModal();
		}
		return myMsgBoxConverter.Result?.ToString() ?? string.Empty;
	}

	public static int? MyMsgBoxSelect(List<IMyRadio> selections, string? title = null, string? button1 = null, string? button2 = "", bool isWarn = false)
	{
		if (title == null)
		{
			title = GetDefaultDialogTitle();
		}
		if (button1 == null)
		{
			button1 = GetDefaultConfirmText();
		}
		if (button2 == null)
		{
			button2 = "";
		}
		MyMsgBoxConverter myMsgBoxConverter = new MyMsgBoxConverter
		{
			Type = MyMsgBoxType.Select,
			Button1 = button1,
			Button2 = button2,
			Content = selections,
			IsWarn = isWarn,
			Title = title
		};
		WaitingMyMsgBox.Add(myMsgBoxConverter);
		try
		{
			if (frmMain != null)
			{
				frmMain.DragStop();
			}
			ComponentDispatcher.PushModal();
			Dispatcher.PushFrame(myMsgBoxConverter.WaitFrame);
		}
		finally
		{
			ComponentDispatcher.PopModal();
		}
		ModBase.Log($"[Control] 选择弹框返回：{myMsgBoxConverter.Result ?? "null"}");
		return (int?)myMsgBoxConverter.Result;
	}

	public static List<int>? MyMsgBoxMultiSelect(List<MyListItem> selections, string? title = null, string? button1 = null, string? button2 = "", bool isWarn = false)
	{
		if (title == null)
		{
			title = GetDefaultDialogTitle();
		}
		if (button1 == null)
		{
			button1 = GetDefaultConfirmText();
		}
		if (button2 == null)
		{
			button2 = "";
		}
		MyMsgBoxConverter myMsgBoxConverter = new MyMsgBoxConverter
		{
			Type = MyMsgBoxType.Select,
			MultiSelect = true,
			Button1 = button1,
			Button2 = button2,
			Content = selections,
			IsWarn = isWarn,
			Title = title
		};
		WaitingMyMsgBox.Add(myMsgBoxConverter);
		try
		{
			if (frmMain != null)
			{
				frmMain.DragStop();
			}
			ComponentDispatcher.PushModal();
			Dispatcher.PushFrame(myMsgBoxConverter.WaitFrame);
		}
		finally
		{
			ComponentDispatcher.PopModal();
		}
		ModBase.Log($"[Control] 多选弹框返回：{myMsgBoxConverter.Result ?? "null"}");
		return (List<int>)myMsgBoxConverter.Result;
	}

	public static void MyMsgBoxTick()
	{
		try
		{
			if (frmMain == null || frmMain.PanMsg == null || frmMain.WindowState == WindowState.Minimized)
			{
				return;
			}
			if (frmMain.PanMsg.Children.Count > 0)
			{
				((UIElement)(object)frmMain.PanMsgBackground).Visibility = Visibility.Visible;
			}
			else if (WaitingMyMsgBox.Any())
			{
				((UIElement)(object)frmMain.PanMsgBackground).Visibility = Visibility.Visible;
				switch (WaitingMyMsgBox[0].Type)
				{
				case MyMsgBoxType.Input:
					frmMain.PanMsg.Children.Add(new MyMsgInput(WaitingMyMsgBox[0]));
					break;
				case MyMsgBoxType.Select:
					frmMain.PanMsg.Children.Add(new MyMsgSelect(WaitingMyMsgBox[0]));
					break;
				case MyMsgBoxType.AuthServer:
					frmMain.PanMsg.Children.Add(new MyMsgAuthServer(WaitingMyMsgBox[0]));
					break;
				case MyMsgBoxType.Text:
					frmMain.PanMsg.Children.Add(new MyMsgText(WaitingMyMsgBox[0]));
					break;
				case MyMsgBoxType.Login:
					frmMain.PanMsg.Children.Add(new MyMsgLogin(WaitingMyMsgBox[0]));
					break;
				case MyMsgBoxType.Markdown:
					frmMain.PanMsg.Children.Add(new MyMsgMarkdown(WaitingMyMsgBox[0]));
					break;
				}
				WaitingMyMsgBox.RemoveAt(0);
			}
			else if (((UIElement)(object)frmMain.PanMsgBackground).Visibility != Visibility.Collapsed)
			{
				((UIElement)(object)frmMain.PanMsgBackground).Visibility = Visibility.Collapsed;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "处理等待中的弹窗失败", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	public static void MsgBoxWrapper_OnShow(string message, string caption, ICollection<MsgBoxButtonInfo> buttons, MsgBoxTheme theme, bool block, ref int result)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Invalid comparison between Unknown and I4
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Invalid comparison between Unknown and I4
		string button = ((buttons.Count < 1) ? GetDefaultConfirmText() : buttons.ElementAt(0).Context);
		Action button1Action = ((buttons.Count < 1) ? null : buttons.ElementAt(0).OnClick);
		string button2 = ((buttons.Count < 2) ? GetDefaultCancelText() : buttons.ElementAt(1).Context);
		Action button2Action = ((buttons.Count < 2) ? null : buttons.ElementAt(1).OnClick);
		string button3 = ((buttons.Count < 3) ? "" : buttons.ElementAt(2).Context);
		Action button3Action = ((buttons.Count < 3) ? null : buttons.ElementAt(2).OnClick);
		bool isWarn = (int)theme == 1 || (int)theme == 2;
		result = MyMsgBox(message, caption, button, button2, button3, isWarn, highLight: true, block, button1Action, button2Action, button3Action);
	}

	private static void TimerFool()
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_067f: Unknown result type (might be due to invalid IL or missing references)
		//IL_068d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0692: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06db: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e0: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (frmLaunchLeft == null || frmLaunchLeft.AprilPosTrans == null || frmMain.lastMouseArg == null || isAprilGiveup || frmMain.pageCurrent != FormMain.PageType.Launch || ModAnimation.AniControlEnabled != 0 || !frmLaunchLeft.BtnLaunch.IsLoaded)
			{
				return;
			}
			Point position = frmMain.lastMouseArg.GetPosition(frmMain);
			if (position == aprilMousePosLast)
			{
				aprilIdieCount++;
			}
			else
			{
				aprilMousePosLast = position;
				aprilIdieCount = 0;
			}
			double num = frmLaunchLeft.BtnLaunch.ActualWidth / 2.0;
			double num2 = frmLaunchLeft.BtnLaunch.ActualHeight / 2.0;
			Vector val = (Vector)(frmMain.lastMouseArg.GetPosition(frmLaunchLeft.BtnLaunch) - new Vector(num, num2));
			Vector val2 = new Vector(val.X, val.Y);
			val2.Normalize();
			Vector val3 = -val2;
			Vector val4 = new Vector(Math.Max(0.0, Math.Abs(val.X) - num), Math.Max(0.0, Math.Abs(val.Y) - num2));
			double length = val4.Length;
			double num3 = Math.Sin((double)timer150Count / 37.5 * Math.PI);
			Vector val5 = Math.Max(0.0, num3 * 0.25 - 0.65 - Math.Log((length + 0.4) / 200.0)) * val3;
			if (aprilIdieCount >= 320)
			{
				Vector val6 = (Vector)(frmMain.lastMouseArg.GetPosition(frmMain.PanMain) - new Vector(num, frmMain.PanMain.ActualHeight - num2 * 3.0));
				Vector val7 = new Vector(frmLaunchLeft.AprilPosTrans.X, frmLaunchLeft.AprilPosTrans.Y);
				if (val6.Length > 250.0 && val7.Length > 0.4)
				{
					val5 -= val7 * 0.0005;
					val7.Normalize();
					val5 -= val7 * 0.15;
				}
			}
			Point val8 = frmLaunchLeft.BtnLaunch.TranslatePoint(new Point(0.0, 0.0), frmMain.PanForm);
			if (val8.X < (0.0 - num) * 2.0)
			{
				frmLaunchLeft.AprilPosTrans.X += frmMain.PanForm.ActualWidth + num * 2.0;
				aprilSpeed.X -= 80.0;
				if (val8.Y < 0.0)
				{
					frmLaunchLeft.AprilPosTrans.Y += num2 * 2.5;
				}
				else if (val8.Y > frmMain.PanForm.ActualHeight - num2 * 2.0)
				{
					frmLaunchLeft.AprilPosTrans.Y -= num2 * 2.5;
				}
			}
			else if (val8.X > frmMain.PanForm.ActualWidth)
			{
				frmLaunchLeft.AprilPosTrans.X -= frmMain.PanForm.ActualWidth + num * 2.0;
				aprilSpeed.X += 80.0;
				if (val8.Y < 0.0)
				{
					frmLaunchLeft.AprilPosTrans.Y += num2 * 2.5;
				}
				else if (val8.Y > frmMain.PanForm.ActualHeight - num2 * 2.0)
				{
					frmLaunchLeft.AprilPosTrans.Y -= num2 * 2.5;
				}
			}
			else if (val8.Y < (0.0 - num2) * 2.0)
			{
				frmLaunchLeft.AprilPosTrans.Y += frmMain.PanForm.ActualHeight + num2 * 2.0;
				aprilSpeed.Y -= 25.0;
				if (val8.X < 0.0)
				{
					frmLaunchLeft.AprilPosTrans.X += num * 2.0;
				}
				else if (val8.X > frmMain.PanForm.ActualWidth - num * 2.0)
				{
					frmLaunchLeft.AprilPosTrans.X -= num * 2.0;
				}
			}
			else if (val8.Y > frmMain.PanForm.ActualHeight)
			{
				frmLaunchLeft.AprilPosTrans.Y -= frmMain.PanForm.ActualHeight + num2 * 2.0;
				aprilSpeed.Y += 25.0;
				if (val8.X < 0.0)
				{
					frmLaunchLeft.AprilPosTrans.X += num * 2.0;
				}
				else if (val8.X > frmMain.PanForm.ActualWidth - num * 2.0)
				{
					frmLaunchLeft.AprilPosTrans.X -= num * 2.0;
				}
			}
			aprilSpeed = aprilSpeed * 0.8 + val5;
			double num4 = Math.Min(60.0, aprilSpeed.Length);
			if (num4 < 0.01)
			{
				return;
			}
			aprilSpeed.Normalize();
			aprilSpeed *= num4;
			aprilDistance = (int)Math.Round((double)aprilDistance + num4);
			frmLaunchLeft.AprilPosTrans.X += aprilSpeed.X;
			frmLaunchLeft.AprilPosTrans.Y += aprilSpeed.Y;
			frmLaunchLeft.AprilScaleTrans.ScaleX = ModBase.MathClamp(1.0 - (Math.Abs(val3.X) - Math.Abs(val3.Y)) * (num4 / 160.0), 0.2, 1.8);
			frmLaunchLeft.AprilScaleTrans.ScaleY = ModBase.MathClamp(1.0 - (Math.Abs(val3.Y) - Math.Abs(val3.X)) * (num4 / 100.0), 0.2, 1.8);
			if (aprilDistance > 4000)
			{
				aprilDistance = -4000;
				switch (RandomUtils.NextInt(0, 3))
				{
				case 0:
					HintService.Hint("放弃吧！只需要点一下右下角的小白旗……");
					break;
				case 1:
					HintService.Hint("看到右下角的那面小白旗了吗？");
					break;
				case 2:
					HintService.Hint("这里建议点一下右下角的小白旗投降呢.jpg");
					break;
				case 3:
					HintService.Hint("右下角的小白旗永远等着你……");
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "愚人节移动出错", ModBase.LogLevel.Feedback, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	public static void ShowWindowToTop(nint handle)
	{
		try
		{
			PostMessage(handle, 6402u, 0L, 0L);
			SetForegroundWindow(handle);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "设置窗口置顶失败", ModBase.LogLevel.Hint, null, Lang.Text("Main.Error.OperationFailed"));
		}
	}

	[DllImport("user32", EntryPoint = "FindWindowA")]
	public static extern nint FindWindow(string className, string windowName);

	[DllImport("user32")]
	public static extern int SetForegroundWindow(nint hWnd);

	[DllImport("user32", EntryPoint = "PostMessageA")]
	private static extern bool PostMessage(nint hWnd, uint msg, long wParam, long lParam);

	public static void SetGPUPreference(string executeable, bool wantHighPerformance = true)
	{
		bool flag = false;
		using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\DirectX\\UserGpuPreferences", writable: false))
		{
			if (registryKey != null)
			{
				if ("GpuPreference=2;" == (registryKey.GetValue(executeable)?.ToString() ?? ""))
				{
					flag = true;
				}
			}
			else
			{
				ModBase.Log("[System] 需要创建显卡设置的父级键");
				Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\DirectX\\UserGpuPreferences");
			}
		}
		ModBase.Log($"[System] 当前程序 ({executeable}) 的显卡设置为高性能: {flag}");
		if (flag ^ wantHighPerformance)
		{
			using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\DirectX\\UserGpuPreferences", writable: true))
			{
				registryKey2.SetValue(executeable, wantHighPerformance ? "GpuPreference=2;" : "GpuPreference=0;");
				ModBase.Log($"[System] 已调整程序 ({executeable}) 显卡设置: {wantHighPerformance}");
			}
		}
	}

	public static string ArgumentReplace(string text, Func<string, string> escapeHandler = null, bool replaceTime = true)
	{
		if (text == null)
		{
			return null;
		}
		Func<string, string> replacer = (string s) =>
		{
			if (s == null)
			{
				return "";
			}
			if (escapeHandler == null)
			{
				return s;
			}
			if (s.Contains(":\\"))
			{
				s = ModBase.ShortenPath(s);
			}
			return escapeHandler(s);
		};
		text = text.Replace("{pcl_version}", replacer(ModBase.versionBaseName));
		text = text.Replace("{pcl_version_code}", replacer(ModBase.versionCode.ToString()));
		text = text.Replace("{pcl_version_branch}", replacer("Publish"));
		text = text.Replace("{pcl_branch}", replacer("Publish"));
		text = text.Replace("{identify}", replacer(Identify.LauncherId));
		text = text.Replace("{path}", replacer(Basics.ExecutableDirectory));
		text = text.Replace("{path_with_name}", replacer(Basics.ExecutableName));
		text = text.Replace("{path_temp}", replacer(ModBase.pathTemp));
		if (replaceTime)
		{
			text = text.Replace("{date}", replacer(Lang.Date(DateTime.Now, "d")));
			text = text.Replace("{time}", replacer(Lang.Date(DateTime.Now, "T")));
		}
		string text2 = text;
		Func<string, string> func = replacer;
		JavaEntry mcLaunchJavaSelected = ModLaunch.mcLaunchJavaSelected;
		text = text2.Replace("{java}", func((mcLaunchJavaSelected != null) ? mcLaunchJavaSelected.Installation.JavaFolder : null));
		text = text.Replace("{minecraft}", replacer(ModFolder.mcFolderSelected));
		if ((object)ModInstanceList.McMcInstanceSelected != null)
		{
			text = text.Replace("{version_path}", replacer(ModInstanceList.McMcInstanceSelected.PathInstance));
			text = text.Replace("{verpath}", replacer(ModInstanceList.McMcInstanceSelected.PathInstance));
			text = text.Replace("{version_indie}", replacer(ModInstanceList.McMcInstanceSelected.PathIndie));
			text = text.Replace("{verindie}", replacer(ModInstanceList.McMcInstanceSelected.PathIndie));
			text = text.Replace("{name}", replacer(ModInstanceList.McMcInstanceSelected.Name));
			text = ((!new ReadOnlySpan<string>(new string[3] { "unknown", "old", "pending" }).Contains(ModInstanceList.McMcInstanceSelected.Info.VanillaName)) ? text.Replace("{version}", replacer(ModInstanceList.McMcInstanceSelected.Info.VanillaName)) : text.Replace("{version}", replacer(ModInstanceList.McMcInstanceSelected.Name)));
		}
		else
		{
			text = text.Replace("{version_path}", replacer(null));
			text = text.Replace("{verpath}", replacer(null));
			text = text.Replace("{version_indie}", replacer(null));
			text = text.Replace("{verindie}", replacer(null));
			text = text.Replace("{name}", replacer(null));
			text = text.Replace("{version}", replacer(null));
		}
		if (ModLaunch.mcLoginLoader.State == ModBase.LoadState.Finished)
		{
			text = text.Replace("{user}", replacer(ModLaunch.mcLoginLoader.output.Name));
			text = text.Replace("{uuid}", replacer(ModLaunch.mcLoginLoader.output.Uuid.ToLower()));
			switch (ModLaunch.mcLoginLoader.input.LoginType)
			{
			case ModLaunch.McLoginType.Legacy:
				text = text.Replace("{login}", replacer("离线"));
				break;
			case ModLaunch.McLoginType.Ms:
				text = text.Replace("{login}", replacer("正版"));
				break;
			case ModLaunch.McLoginType.Auth:
				text = text.Replace("{login}", replacer("Authlib-Injector"));
				break;
			}
		}
		else
		{
			text = text.Replace("{user}", replacer(null));
			text = text.Replace("{uuid}", replacer(null));
			text = text.Replace("{login}", replacer(null));
		}
		text = text.RegexReplaceEach("\\{hint\\}", (Match m) => replacer(PageToolsTest.GetRandomHint()));
		text = text.RegexReplaceEach("\\{cave\\}", (Match m) => replacer(PageToolsTest.GetRandomCave()));
		text = text.RegexReplaceEach("\\{setup:([a-zA-Z0-9]+)\\}", (Match m) =>
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Invalid comparison between Unknown and I4
			ConfigItem val = default;
			return (ConfigService.TryGetConfigItemNoType(m.Groups[1].Value, out val) && (int)val.Source != 1) ? replacer(val.GetValueNoType((object)ModInstanceList.McMcInstanceSelected?.PathInstance)?.ToString() ?? "") : replacer("");
		});
		text = text.RegexReplaceEach("\\{varible:([^:\\}]+)(?::([^\\}]+))?\\}", (Match m) => replacer(CustomEvent.GetCustomVariable(m.Groups[1].Value, m.Groups[2].Value)));
		text = text.RegexReplaceEach("\\{variable:([^:\\}]+)(?::([^\\}]+))?\\}", (Match m) => replacer(CustomEvent.GetCustomVariable(m.Groups[1].Value, m.Groups[2].Value)));
		return text;
	}

	public static void TryClearTaskTemp()
	{
		if (!isTaskTempCleared)
		{
			isTaskTempCleared = true;
			isTaskTempClearing = true;
			try
			{
				ModBase.Log("[System] 开始清理任务缓存文件夹");
				ModBase.DeleteDirectory(Path.Combine(SystemPaths.DriveLetter, "ProgramData", "PCL", "TaskTemp"));
				ModBase.DeleteDirectory(ModBase.pathTemp + "TaskTemp\\");
				ModBase.Log("[System] 已清理任务缓存文件夹");
				return;
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "清理任务缓存文件夹失败");
				return;
			}
			finally
			{
				isTaskTempClearing = false;
			}
		}
		if (isTaskTempClearing)
		{
			while (isTaskTempClearing)
			{
				Thread.Sleep(1);
			}
		}
	}

	public static string RequestTaskTempFolder(bool requireNonSpace = false)
	{
		TryClearTaskTemp();
		string text;
		try
		{
			text = $"{ModBase.pathTemp}TaskTemp\\{ModBase.GetUuid()}-{RandomUtils.NextInt(0, 1000000)}\\";
			if (!requireNonSpace || !text.Contains(" "))
			{
				Directory.CreateDirectory(text);
				ModBase.CheckPermissionWithException(text);
				return text;
			}
		}
		catch
		{
		}
		InlineArray5<string> buffer = default;
		buffer[0] = SystemPaths.DriveLetter;
		buffer[1] = "ProgramData";
		buffer[2] = "PCL";
		buffer[3] = "TaskTemp";
		buffer[4] = $"{ModBase.GetUuid()}-{RandomUtils.NextInt(0, 1000000)}";
		text = Path.Combine(buffer);
		Directory.CreateDirectory(text);
		ModBase.CheckPermission(text);
		return text;
	}

	public static void RaiseCustomEvent(DependencyObject control)
	{
		List<CustomEvent> events = CustomEventService.GetEvents(control).ToList();
		EventType eventType = CustomEventService.GetEventType(control);
		if (eventType != EventType.None)
		{
			events.Add(new CustomEvent(eventType, CustomEventService.GetEventData(control)));
		}
		if (!events.Any())
		{
			return;
		}
		ModBase.RunInNewThread(() =>
		{
			foreach (CustomEvent item in events)
			{
				item.Raise();
			}
		}, $"执行自定义事件 {ModBase.GetUuid()}");
	}

	static ModMain()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
	}
}
