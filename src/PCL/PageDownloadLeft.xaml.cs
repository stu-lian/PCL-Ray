using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageDownloadLeft : MyPageLeft, IRefreshable, IComponentConnector
{
	public FormMain.PageSubType pageID = FormMain.PageSubType.DownloadInstall;

	public void Refresh()
	{
		Refresh(ModMain.frmMain.PageCurrentSub);
	}

	public void RefreshButton_Click(object sender, EventArgs e)
	{
		Refresh((FormMain.PageSubType)ModBase.Val(((MyIconButton)sender).Tag));
	}

	public void Refresh(FormMain.PageSubType subType)
	{
		switch (subType)
		{
		case FormMain.PageSubType.DownloadInstall:
			ModDownload.dlClientListLoader.Start(null, isForceRestart: true);
			ModDownload.dlOptiFineListLoader.Start(null, isForceRestart: true);
			ModDownload.dlForgeListLoader.Start(null, isForceRestart: true);
			ModDownload.dlNeoForgeListLoader.Start(null, isForceRestart: true);
			ModDownload.dlCleanroomListLoader.Start(null, isForceRestart: true);
			ModDownload.dlLiteLoaderListLoader.Start(null, isForceRestart: true);
			ModDownload.dlFabricListLoader.Start(null, isForceRestart: true);
			ModDownload.dlLegacyFabricListLoader.Start(null, isForceRestart: true);
			ModDownload.dlFabricApiLoader.Start(null, isForceRestart: true);
			ModDownload.dlLegacyFabricApiLoader.Start(null, isForceRestart: true);
			ModDownload.dlOptiFabricLoader.Start(null, isForceRestart: true);
			ModDownload.dlLabyModListLoader.Start(null, isForceRestart: true);
			ItemInstall.Checked = true;
			break;
		case FormMain.PageSubType.DownloadMod:
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			if (ModMain.frmDownloadMod != null)
			{
				ModMain.frmDownloadMod.Content.storage = new ModComp.CompProjectStorage();
				ModMain.frmDownloadMod.Content.page = 0;
				ModMain.frmDownloadMod.PageLoaderRestart();
			}
			ItemMod.Checked = true;
			break;
		case FormMain.PageSubType.DownloadPack:
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			if (ModMain.frmDownloadPack != null)
			{
				ModMain.frmDownloadPack.Content.storage = new ModComp.CompProjectStorage();
				ModMain.frmDownloadPack.Content.page = 0;
				ModMain.frmDownloadPack.PageLoaderRestart();
			}
			ItemPack.Checked = true;
			break;
		case FormMain.PageSubType.DownloadDataPack:
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			if (ModMain.frmDownloadDataPack != null)
			{
				ModMain.frmDownloadDataPack.Content.storage = new ModComp.CompProjectStorage();
				ModMain.frmDownloadDataPack.Content.page = 0;
				ModMain.frmDownloadDataPack.PageLoaderRestart();
			}
			ItemDataPack.Checked = true;
			break;
		case FormMain.PageSubType.DownloadResourcePack:
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			if (ModMain.frmDownloadResourcePack != null)
			{
				ModMain.frmDownloadResourcePack.Content.storage = new ModComp.CompProjectStorage();
				ModMain.frmDownloadResourcePack.Content.page = 0;
				ModMain.frmDownloadResourcePack.PageLoaderRestart();
			}
			ItemResourcePack.Checked = true;
			break;
		case FormMain.PageSubType.DownloadShader:
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			if (ModMain.frmDownloadShader != null)
			{
				ModMain.frmDownloadShader.Content.storage = new ModComp.CompProjectStorage();
				ModMain.frmDownloadShader.Content.page = 0;
				ModMain.frmDownloadShader.PageLoaderRestart();
			}
			ItemShader.Checked = true;
			break;
		case FormMain.PageSubType.DownloadWorld:
			ModComp.compProjectCache.Clear();
			ModComp.compFilesCache.Clear();
			if (ModMain.frmDownloadWorld != null)
			{
				ModMain.frmDownloadWorld.Content.storage = new ModComp.CompProjectStorage();
				ModMain.frmDownloadWorld.Content.page = 0;
				ModMain.frmDownloadWorld.PageLoaderRestart();
			}
			ItemWorld.Checked = true;
			break;
		case FormMain.PageSubType.DownloadClient:
			ModDownload.dlClientListLoader.Start(null, isForceRestart: true);
			ItemClient.Checked = true;
			break;
		case FormMain.PageSubType.DownloadOptiFine:
			ModDownload.dlOptiFineListLoader.Start(null, isForceRestart: true);
			ItemOptiFine.Checked = true;
			break;
		case FormMain.PageSubType.DownloadForge:
			ModDownload.dlForgeListLoader.Start(null, isForceRestart: true);
			ItemForge.Checked = true;
			break;
		case FormMain.PageSubType.DownloadNeoForge:
			ModDownload.dlNeoForgeListLoader.Start(null, isForceRestart: true);
			ItemNeoForge.Checked = true;
			break;
		case FormMain.PageSubType.DownloadCleanroom:
			ModDownload.dlCleanroomListLoader.Start(null, isForceRestart: true);
			ItemCleanroom.Checked = true;
			break;
		case FormMain.PageSubType.DownloadLiteLoader:
			ModDownload.dlLiteLoaderListLoader.Start(null, isForceRestart: true);
			ItemLiteLoader.Checked = true;
			break;
		case FormMain.PageSubType.DownloadFabric:
			ModDownload.dlFabricListLoader.Start(null, isForceRestart: true);
			ItemFabric.Checked = true;
			break;
		case FormMain.PageSubType.DownloadLabyMod:
			ModDownload.dlLabyModListLoader.Start(null, isForceRestart: true);
			ItemLabyMod.Checked = true;
			break;
		case FormMain.PageSubType.DownloadLegacyFabric:
			ModDownload.dlLegacyFabricListLoader.Start(null, isForceRestart: true);
			ItemLegacyFabric.Checked = true;
			break;
		case FormMain.PageSubType.DownloadCompFavorites:
			if (ModMain.frmDownloadCompFavorites != null)
			{
				ModMain.frmDownloadCompFavorites.PageLoaderRestart();
			}
			ItemFavorites.Checked = true;
			break;
		}
		HintService.Hint(Lang.Text("Download.Left.Hint.Refreshing"), HintType.Info, log: false);
	}

	private void ItemInstall_Click(object sender, MouseButtonEventArgs e)
	{
		if (ItemInstall.Checked)
		{
			ModMain.frmDownloadInstall.ExitSelectPage();
		}
	}

	public PageDownloadLeft()
	{
		InitializeComponent();
		ItemInstall.Check += PageCheck;
		ItemMod.Check += PageCheck;
		ItemPack.Check += PageCheck;
		ItemDataPack.Check += PageCheck;
		ItemResourcePack.Check += PageCheck;
		ItemShader.Check += PageCheck;
		ItemWorld.Check += PageCheck;
		ItemFavorites.Check += PageCheck;
		ItemClient.Check += PageCheck;
		ItemOptiFine.Check += PageCheck;
		ItemForge.Check += PageCheck;
		ItemNeoForge.Check += PageCheck;
		ItemLiteLoader.Check += PageCheck;
		ItemFabric.Check += PageCheck;
		ItemLegacyFabric.Check += PageCheck;
		ItemLabyMod.Check += PageCheck;
		AnimatedControl = PanItem;
	}

	private void PageCheck(object sender, ModBase.RouteEventArgs e)
	{
		if (sender is MyListItem { Tag: { } tag })
		{
			PageChange((FormMain.PageSubType)ModBase.Val(tag));
		}
	}

	public object PageGet(FormMain.PageSubType id)
	{
		if (id == FormMain.PageSubType.Default)
		{
			id = pageID;
		}
		switch (id)
		{
		case FormMain.PageSubType.DownloadInstall:
			if (ModMain.frmDownloadInstall == null)
			{
				ModMain.frmDownloadInstall = new PageDownloadInstall();
			}
			return ModMain.frmDownloadInstall;
		case FormMain.PageSubType.DownloadMod:
			if (ModMain.frmDownloadMod == null)
			{
				ModMain.frmDownloadMod = new PageDownloadMod();
			}
			return ModMain.frmDownloadMod;
		case FormMain.PageSubType.DownloadPack:
			if (ModMain.frmDownloadPack == null)
			{
				ModMain.frmDownloadPack = new PageDownloadPack();
			}
			return ModMain.frmDownloadPack;
		case FormMain.PageSubType.DownloadDataPack:
			if (ModMain.frmDownloadDataPack == null)
			{
				ModMain.frmDownloadDataPack = new PageDownloadDataPack();
			}
			return ModMain.frmDownloadDataPack;
		case FormMain.PageSubType.DownloadResourcePack:
			if (ModMain.frmDownloadResourcePack == null)
			{
				ModMain.frmDownloadResourcePack = new PageDownloadResourcePack();
			}
			return ModMain.frmDownloadResourcePack;
		case FormMain.PageSubType.DownloadShader:
			if (ModMain.frmDownloadShader == null)
			{
				ModMain.frmDownloadShader = new PageDownloadShader();
			}
			return ModMain.frmDownloadShader;
		case FormMain.PageSubType.DownloadWorld:
			if (ModMain.frmDownloadWorld == null)
			{
				ModMain.frmDownloadWorld = new PageDownloadWorld();
			}
			return ModMain.frmDownloadWorld;
		case FormMain.PageSubType.DownloadCompFavorites:
			if (ModMain.frmDownloadCompFavorites == null)
			{
				ModMain.frmDownloadCompFavorites = new PageDownloadCompFavorites();
			}
			return ModMain.frmDownloadCompFavorites;
		case FormMain.PageSubType.DownloadClient:
			if (ModMain.frmDownloadClient == null)
			{
				ModMain.frmDownloadClient = new PageDownloadClient();
			}
			return ModMain.frmDownloadClient;
		case FormMain.PageSubType.DownloadOptiFine:
			if (ModMain.frmDownloadOptiFine == null)
			{
				ModMain.frmDownloadOptiFine = new PageDownloadOptiFine();
			}
			return ModMain.frmDownloadOptiFine;
		case FormMain.PageSubType.DownloadForge:
			if (ModMain.frmDownloadForge == null)
			{
				ModMain.frmDownloadForge = new PageDownloadForge();
			}
			return ModMain.frmDownloadForge;
		case FormMain.PageSubType.DownloadNeoForge:
			if (ModMain.frmDownloadNeoForge == null)
			{
				ModMain.frmDownloadNeoForge = new PageDownloadNeoForge();
			}
			return ModMain.frmDownloadNeoForge;
		case FormMain.PageSubType.DownloadCleanroom:
			if (ModMain.frmDownloadCleanroom == null)
			{
				ModMain.frmDownloadCleanroom = new PageDownloadCleanroom();
			}
			return ModMain.frmDownloadCleanroom;
		case FormMain.PageSubType.DownloadLiteLoader:
			if (ModMain.frmDownloadLiteLoader == null)
			{
				ModMain.frmDownloadLiteLoader = new PageDownloadLiteLoader();
			}
			return ModMain.frmDownloadLiteLoader;
		case FormMain.PageSubType.DownloadFabric:
			if (ModMain.frmDownloadFabric == null)
			{
				ModMain.frmDownloadFabric = new PageDownloadFabric();
			}
			return ModMain.frmDownloadFabric;
		case FormMain.PageSubType.DownloadLabyMod:
			if (ModMain.frmDownloadLabyMod == null)
			{
				ModMain.frmDownloadLabyMod = new PageDownloadLabyMod();
			}
			return ModMain.frmDownloadLabyMod;
		case FormMain.PageSubType.DownloadLegacyFabric:
			if (ModMain.frmDownloadLegacyFabric == null)
			{
				ModMain.frmDownloadLegacyFabric = new PageDownloadLegacyFabric();
			}
			return ModMain.frmDownloadLegacyFabric;
		default:
			throw new Exception(Lang.Text("Download.Left.Error.UnknownSubPageType", new object[1] { (int)id }));
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
			int num = (int)id;
			ModBase.Log(ex, "切换分页面失败（ID " + num + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Error.OperationFailed"));
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
