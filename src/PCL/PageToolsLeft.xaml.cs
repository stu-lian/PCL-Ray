using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageToolsLeft : MyPageLeft, IComponentConnector
{
	private bool isLoad;

	private bool isPageSwitched;

	public FormMain.PageSubType pageID = FormMain.PageSubType.DownloadPack;

	public PageToolsLeft()
	{
		InitializeComponent();
		AnimatedControl = PanItem;
		Loaded += PageLinkLeft_Loaded;
		Unloaded += PageOtherLeft_Unloaded;
	}

	private void PageLinkLeft_Loaded(object sender, RoutedEventArgs e)
	{
		if (isLoad)
		{
			return;
		}
		isLoad = true;
		PageSetupUI.HiddenRefresh();
		if (!isPageSwitched)
		{
			ItemTest.SetChecked(value: true, user: false, anime: false);
		}
	}

	private void PageOtherLeft_Unloaded(object sender, RoutedEventArgs e)
	{
		isPageSwitched = false;
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
		case FormMain.PageSubType.DownloadPack:
			if (ModMain.frmToolsTest == null)
			{
				ModMain.frmToolsTest = new PageToolsTest();
			}
			return ModMain.frmToolsTest;
		case FormMain.PageSubType.ToolsMapViewer:
			if (ModMain.frmToolsMapViewer == null)
			{
				ModMain.frmToolsMapViewer = new PageToolsMapViewer();
			}
			return ModMain.frmToolsMapViewer;
		case FormMain.PageSubType.ToolsModTranslate:
			if (ModMain.frmToolsModTranslate == null)
			{
				ModMain.frmToolsModTranslate = new PageToolsModTranslate();
			}
			return ModMain.frmToolsModTranslate;
		case FormMain.PageSubType.ToolsLogAnalyse:
			if (ModMain.frmToolsLogAnalyse == null)
			{
				ModMain.frmToolsLogAnalyse = new PageToolsLogAnalyse();
			}
			return ModMain.frmToolsLogAnalyse;
		case FormMain.PageSubType.ToolsColorCode:
			if (ModMain.frmToolsColorCode == null)
			{
				ModMain.frmToolsColorCode = new PageToolsColorCode();
			}
			return ModMain.frmToolsColorCode;
		case FormMain.PageSubType.ToolsHeadGenerator:
			if (ModMain.frmToolsHeadGenerator == null)
			{
				ModMain.frmToolsHeadGenerator = new PageToolsHeadGenerator();
			}
			return ModMain.frmToolsHeadGenerator;
		case FormMain.PageSubType.ToolsSummon:
			if (ModMain.frmToolsSummon == null)
			{
				ModMain.frmToolsSummon = new PageToolsSummon();
			}
			return ModMain.frmToolsSummon;
		case FormMain.PageSubType.ToolsShield:
			if (ModMain.frmToolsShield == null)
			{
				ModMain.frmToolsShield = new PageToolsShield();
			}
			return ModMain.frmToolsShield;
		case FormMain.PageSubType.ToolsFirework:
			if (ModMain.frmToolsFirework == null)
			{
				ModMain.frmToolsFirework = new PageToolsFirework();
			}
			return ModMain.frmToolsFirework;
		case FormMain.PageSubType.ToolsBeacon:
			if (ModMain.frmToolsBeacon == null)
			{
				ModMain.frmToolsBeacon = new PageToolsBeacon();
			}
			return ModMain.frmToolsBeacon;
		case FormMain.PageSubType.ToolsArmorTrim:
			if (ModMain.frmToolsArmorTrim == null)
			{
				ModMain.frmToolsArmorTrim = new PageToolsArmorTrim();
			}
			return ModMain.frmToolsArmorTrim;
		case FormMain.PageSubType.ToolsTitle:
			if (ModMain.frmToolsTitle == null)
			{
				ModMain.frmToolsTitle = new PageToolsTitle();
			}
			return ModMain.frmToolsTitle;
		case FormMain.PageSubType.ToolsPlayerData:
			if (ModMain.frmToolsPlayerData == null)
			{
				ModMain.frmToolsPlayerData = new PageToolsPlayerData();
			}
			return ModMain.frmToolsPlayerData;
		case FormMain.PageSubType.ToolsHardcoreReviver:
			if (ModMain.frmToolsHardcoreReviver == null)
			{
				ModMain.frmToolsHardcoreReviver = new PageToolsHardcoreReviver();
			}
			return ModMain.frmToolsHardcoreReviver;
		case FormMain.PageSubType.ToolsNbtEditor:
			if (ModMain.frmToolsNbtEditor == null)
			{
				ModMain.frmToolsNbtEditor = new PageToolsNbtEditor();
			}
			return ModMain.frmToolsNbtEditor;
		default:
			throw new Exception("未知的更多子页面种类：" + (int)id.Value);
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
			ModBase.Log(ex, $"切换分页面失败（ID {(int)id}）", ModBase.LogLevel.Feedback, null, Lang.Text("Tools.Error.OperationFailed"));
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
