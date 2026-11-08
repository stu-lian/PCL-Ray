using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceSavesLeft : MyPageLeft, IRefreshable, IComponentConnector
{
	public static string currentSave;

	private bool isLoad;

	public FormMain.PageSubType pageID;

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		if (!isLoad)
		{
			isLoad = true;
		}
	}

	private void BtnOpenFolder_Click(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ModBase.OpenExplorer(currentSave + "\\");
	}

	public PageInstanceSavesLeft()
	{
		InitializeComponent();
		Loaded += Page_Loaded;
		ItemInfo.Check += PageCheck;
		ItemDatapack.Check += PageCheck;
		BtnOpenFolder.Click += BtnOpenFolder_Click;
	}

	private void PageCheck(object sender, ModBase.RouteEventArgs e)
	{
		if (sender is MyListItem { Tag: not null } myListItem)
		{
			PageChange((FormMain.PageSubType)ModBase.Val(myListItem.Tag));
		}
	}

	public object PageGet(FormMain.PageSubType id = FormMain.PageSubType.Default)
	{
		if (id == (FormMain.PageSubType)(-1))
		{
			id = pageID;
		}
		switch (id)
		{
		case FormMain.PageSubType.Default:
			if (ModMain.frmInstanceSavesInfo == null)
			{
				ModMain.frmInstanceSavesInfo = new PageInstanceSavesInfo();
			}
			return ModMain.frmInstanceSavesInfo;
		case FormMain.PageSubType.DownloadInstall:
			if (ModMain.frmInstanceSavesDatapack == null)
			{
				ModMain.frmInstanceSavesDatapack = new PageInstanceSavesDatapack();
			}
			return ModMain.frmInstanceSavesDatapack;
		default:
			throw new Exception(Lang.Text("Instance.Saves.Left.UnknownSubPage", new object[1] { (int)id }));
		}
	}

	public void PageChange(FormMain.PageSubType id)
	{
		if (pageID == id)
		{
			return;
		}
		ModAnimation.AniControlEnabled++;
		try
		{
			PageChangeRun((MyPageRight)PageGet(id));
			pageID = id;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Saves.Left.SwitchFailed", new object[1] { (int)id }), ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Saves.Left.SwitchFailed", new object[1] { (int)id }));
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

	public void RefreshButton_Click(object sender, EventArgs e)
	{
		Refresh((FormMain.PageSubType)ModBase.Val(((MyIconButton)sender).Tag));
	}

	public void Refresh()
	{
		Refresh(ModMain.frmMain.PageCurrentSub);
	}

	public void Refresh(FormMain.PageSubType subType)
	{
		if (subType == FormMain.PageSubType.DownloadInstall)
		{
			if (ModMain.frmInstanceSavesDatapack == null)
			{
				ModMain.frmInstanceSavesDatapack = new PageInstanceSavesDatapack();
			}
			if (ItemDatapack.Checked)
			{
				ModMain.frmInstanceSavesDatapack.Refresh();
			}
			else
			{
				ItemDatapack.Checked = true;
			}
		}
		HintService.Hint(Lang.Text("Instance.Saves.Left.Refreshing"));
	}

}
