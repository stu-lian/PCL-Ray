using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageSetupLeft : MyPageLeft, IComponentConnector
{
	private bool isLoad;

	private bool isPageSwitched;

	public FormMain.PageSubType pageID;

	private void PageSetupLeft_Loaded(object sender, RoutedEventArgs e)
	{
		bool flag = false;
		Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
		if (ItemLaunch.Checked && hide.SetupLaunch)
		{
			flag = true;
		}
		if (ItemJava.Checked && hide.SetupJava)
		{
			flag = true;
		}
		if (ItemGameManage.Checked && hide.SetupGameManage)
		{
			flag = true;
		}
		if (ItemUI.Checked && hide.SetupUi)
		{
			flag = true;
		}
		if (ItemLauncherLanguage.Checked && hide.SetupLauncherLanguage)
		{
			flag = true;
		}
		if (ItemLauncherMisc.Checked && hide.SetupLauncherMisc)
		{
			flag = true;
		}
		if (ItemAbout.Checked && hide.SetupAbout)
		{
			flag = true;
		}
		if (ItemUpdate.Checked && hide.SetupUpdate)
		{
			flag = true;
		}
		if (ItemFeedback.Checked && hide.SetupFeedback)
		{
			flag = true;
		}
		if (ItemLog.Checked && hide.SetupLog)
		{
			flag = true;
		}
		if (PageSetupUI.HiddenForceShow)
		{
			flag = false;
		}
		if (isLoad && !flag)
		{
			return;
		}
		isLoad = true;
		PageSetupUI.HiddenRefresh();
		if (!isPageSwitched)
		{
			Config.PreferenceConfigGroup.HideConfigGroup hide2 = Config.Preference.Hide;
			if (!hide2.SetupLaunch)
			{
				ItemLaunch.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupJava)
			{
				ItemJava.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupGameManage)
			{
				ItemGameManage.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupUi)
			{
				ItemUI.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupLauncherLanguage)
			{
				ItemLauncherLanguage.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupLauncherMisc)
			{
				ItemLauncherMisc.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupAbout)
			{
				ItemAbout.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupUpdate)
			{
				ItemUpdate.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupFeedback)
			{
				ItemFeedback.SetChecked(value: true, user: false, anime: false);
			}
			else if (!hide2.SetupLog)
			{
				ItemLog.SetChecked(value: true, user: false, anime: false);
			}
			else
			{
				ItemLaunch.SetChecked(value: true, user: false, anime: false);
			}
		}
	}

	private void PageOtherLeft_Unloaded(object sender, RoutedEventArgs e)
	{
		isPageSwitched = false;
	}

	public void Reset(object sender, EventArgs e)
	{
		double num = ModBase.Val(((MyIconButton)sender).Tag);
		if (num != 0.0)
		{
			if (num != 1.0)
			{
				if (num != 2.0)
				{
					if (num != 7.0)
					{
						if (num != 11.0)
						{
							if (num == 10.0 && ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Misc.Message"), Lang.Text("Setup.Left.Reset.Title"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
							{
								if (ModMain.frmSetupLauncherMisc == null)
								{
									ModMain.frmSetupLauncherMisc = new PageSetupLauncherMisc();
								}
								ModMain.frmSetupLauncherMisc.Reset();
								ItemLauncherMisc.Checked = true;
							}
						}
						else if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Language.Message"), Lang.Text("Setup.Left.Reset.Title"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
						{
							if (ModMain.frmSetupLauncherLanguage == null)
							{
								ModMain.frmSetupLauncherLanguage = new PageSetupLauncherLanguage();
							}
							ModMain.frmSetupLauncherLanguage.Reset();
							ItemLauncherLanguage.Checked = true;
						}
					}
				}
				else if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.GameManage.Message"), Lang.Text("Setup.Left.Reset.Title"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
				{
					if (ModMain.frmSetupGameManage == null)
					{
						ModMain.frmSetupGameManage = new PageSetupGameManage();
					}
					ModMain.frmSetupGameManage.Reset();
					ItemGameManage.Checked = true;
				}
			}
			else if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Ui.Message"), Lang.Text("Setup.Left.Reset.Title"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
			{
				if (ModMain.frmSetupUI == null)
				{
					ModMain.frmSetupUI = new PageSetupUI();
				}
				ModMain.frmSetupUI.Reset();
				ItemUI.Checked = true;
			}
		}
		else if (ModMain.MyMsgBox(Lang.Text("Setup.Left.Reset.Launch.Message"), Lang.Text("Setup.Left.Reset.Title"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
		{
			if (ModMain.frmSetupLaunch == null)
			{
				ModMain.frmSetupLaunch = new PageSetupLaunch();
			}
			ModMain.frmSetupLaunch.Reset();
			ItemLaunch.Checked = true;
		}
	}

	public static void TryFeedback()
	{
		ModBase.RunInNewThread(() =>
		{
			if (ModBase.CanFeedback(showHint: true))
			{
				switch (ModMain.MyMsgBox(Lang.Text("Setup.Left.Feedback.Message"), Lang.Text("Setup.Left.Feedback.Title"), Lang.Text("Setup.Left.Feedback.SubmitNew"), Lang.Text("Setup.Left.Feedback.ViewList"), Lang.Text("Common.Action.Cancel")))
				{
				case 1:
					ModBase.Feedback();
					break;
				case 2:
					ModBase.OpenWebsite("https://github.com/stu-lian/PCL-Ray/issues/");
					break;
				}
			}
		});
	}

	public void Refresh(object sender, EventArgs e)
	{
		double num = ModBase.Val(((MyIconButton)sender).Tag);
		if (num != 6.0)
		{
			if (num == 9.0)
			{
				if (ModMain.frmSetupJava != null)
				{
					ModMain.frmSetupJava.loader.Start(null, isForceRestart: true);
				}
				ItemJava.Checked = true;
			}
		}
		else
		{
			if (ModMain.frmSetupFeedback != null)
			{
				ModMain.frmSetupFeedback.Loader.Start(null, isForceRestart: true);
			}
			ItemFeedback.Checked = true;
		}
		HintService.Hint(Lang.Text("Setup.Left.Refreshing"), HintType.Info, log: false);
	}

	public PageSetupLeft()
	{
		InitializeComponent();
		Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
		if (!hide.SetupLaunch)
		{
			pageID = FormMain.PageSubType.Default;
		}
		else if (!hide.SetupJava)
		{
			pageID = FormMain.PageSubType.DownloadClient;
		}
		else if (!hide.SetupGameManage)
		{
			pageID = FormMain.PageSubType.DownloadMod;
		}
		else if (!hide.SetupUi)
		{
			pageID = FormMain.PageSubType.DownloadInstall;
		}
		else if (!hide.SetupLauncherLanguage)
		{
			pageID = FormMain.PageSubType.DownloadForge;
		}
		else if (!hide.SetupLauncherMisc)
		{
			pageID = FormMain.PageSubType.DownloadOptiFine;
		}
		else if (!hide.SetupAbout)
		{
			pageID = FormMain.PageSubType.DownloadDataPack;
		}
		else if (!hide.SetupUpdate)
		{
			pageID = FormMain.PageSubType.DownloadCompFavorites;
		}
		else if (!hide.SetupFeedback)
		{
			pageID = FormMain.PageSubType.DownloadShader;
		}
		else if (!hide.SetupLog)
		{
			pageID = FormMain.PageSubType.DownloadResourcePack;
		}
		else
		{
			pageID = FormMain.PageSubType.Default;
		}
		AnimatedControl = PanItem;
		Loaded += PageSetupLeft_Loaded;
		Unloaded += PageOtherLeft_Unloaded;
	}

	private void PageCheck(object senderRaw, ModBase.RouteEventArgs e)
	{
		MyListItem myListItem = (MyListItem)senderRaw;
		if (myListItem.Tag != null)
		{
			PageChange((FormMain.PageSubType)ModBase.Val(myListItem.Tag));
		}
	}

	public object PageGet(FormMain.PageSubType? id = null)
	{
		FormMain.PageSubType? pageSubType = id;
		if (!pageSubType.HasValue)
		{
			_ = pageID;
		}
		else
		{
			pageSubType.GetValueOrDefault();
		}
		switch (id)
		{
		case FormMain.PageSubType.Default:
			if (ModMain.frmSetupLaunch == null)
			{
				ModMain.frmSetupLaunch = new PageSetupLaunch();
			}
			return ModMain.frmSetupLaunch;
		case FormMain.PageSubType.DownloadInstall:
			if (ModMain.frmSetupUI == null)
			{
				ModMain.frmSetupUI = new PageSetupUI();
			}
			return ModMain.frmSetupUI;
		case FormMain.PageSubType.DownloadMod:
			if (ModMain.frmSetupGameManage == null)
			{
				ModMain.frmSetupGameManage = new PageSetupGameManage();
			}
			return ModMain.frmSetupGameManage;
		case FormMain.PageSubType.DownloadCompFavorites:
			if (ModMain.frmSetupUpdate == null)
			{
				ModMain.frmSetupUpdate = new PageSetupUpdate();
			}
			return ModMain.frmSetupUpdate;
		case FormMain.PageSubType.DownloadDataPack:
			if (ModMain.frmSetupAbout == null)
			{
				ModMain.frmSetupAbout = new PageSetupAbout();
			}
			return ModMain.frmSetupAbout;
		case FormMain.PageSubType.DownloadResourcePack:
			if (ModMain.frmSetupLog == null)
			{
				ModMain.frmSetupLog = new PageSetupLog();
			}
			return ModMain.frmSetupLog;
		case FormMain.PageSubType.DownloadShader:
			if (ModMain.frmSetupFeedback == null)
			{
				ModMain.frmSetupFeedback = new PageSetupFeedback();
			}
			return ModMain.frmSetupFeedback;
		case FormMain.PageSubType.DownloadWorld:
			if (ModMain.frmSetupGameLink == null)
			{
				ModMain.frmSetupGameLink = new PageSetupGameLink();
			}
			return ModMain.frmSetupGameLink;
		case FormMain.PageSubType.DownloadForge:
			if (ModMain.frmSetupLauncherLanguage == null)
			{
				ModMain.frmSetupLauncherLanguage = new PageSetupLauncherLanguage();
			}
			return ModMain.frmSetupLauncherLanguage;
		case FormMain.PageSubType.DownloadOptiFine:
			if (ModMain.frmSetupLauncherMisc == null)
			{
				ModMain.frmSetupLauncherMisc = new PageSetupLauncherMisc();
			}
			return ModMain.frmSetupLauncherMisc;
		case FormMain.PageSubType.DownloadClient:
			if (ModMain.frmSetupJava == null)
			{
				ModMain.frmSetupJava = new PageSetupJava();
			}
			return ModMain.frmSetupJava;
		default:
			throw new Exception("未知的设置子页面种类：" + (int)id.Value);
		}
	}

	public void PageChange(FormMain.PageSubType id)
	{
		if (pageID == id)
		{
			return;
		}
		ModAnimation.AniControlEnabled++;
		isPageSwitched = true;
		try
		{
			PageChangeRun((MyPageRight)PageGet(id));
			pageID = id;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, $"切换分页面失败（ID {(int)id}）", ModBase.LogLevel.Feedback, null, Lang.Text("Setup.Error.OperationFailed"));
		}
		finally
		{
			ModAnimation.AniControlEnabled--;
		}
	}

	private static void PageChangeRun(MyPageRight target)
	{
		ModAnimation.AniStop("FrmMain PageChangeRight");
		if (target.Parent != null)
		{
			((DependencyObject)target).SetValue(ContentPresenter.ContentProperty, (object)null);
		}
		ModMain.frmMain.pageRight = target;
		((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnExit();
		ModAnimation.AniStart(new ModAnimation.AniData[2]
		{
			ModAnimation.AaCode(() =>
			{
				((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnForceExit();
				ModMain.frmMain.PanMainRight.Child = ModMain.frmMain.pageRight;
				ModMain.frmMain.pageRight.Opacity = 0.0;
			}, 130),
			ModAnimation.AaCode(() =>
			{
				ModMain.frmMain.pageRight.Opacity = 1.0;
				ModMain.frmMain.pageRight.PageOnEnter();
			}, 30, after: true)
		}, "PageLeft PageChange");
	}

}
