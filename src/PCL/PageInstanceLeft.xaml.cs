using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceLeft : MyPageLeft, IRefreshable, IComponentConnector
{
	public static McInstance McInstance;

	public FormMain.PageSubType pageID;

	public PageInstanceLeft()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshModDisabled();
		};
	}

	public void Refresh()
	{
		Refresh(ModMain.frmMain.PageCurrentSub);
	}

	public void RefreshModDisabled()
	{
		Config.PreferenceConfigGroup.HideConfigGroup hide = Config.Preference.Hide;
		if ((object)McInstance != null && McInstance.Modable)
		{
			ItemMod.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceMod) ? Visibility.Collapsed : Visibility.Visible);
			ItemModDisabled.Visibility = Visibility.Collapsed;
		}
		else
		{
			ItemMod.Visibility = Visibility.Collapsed;
			ItemModDisabled.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceMod) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (!PageSetupUI.HiddenForceShow)
		{
			int num = 0;
			if (hide.InstanceSave)
			{
				num++;
			}
			if (hide.InstanceScreenshot)
			{
				num++;
			}
			if (hide.InstanceMod)
			{
				num++;
			}
			if (hide.InstanceResourcePack)
			{
				num++;
			}
			if (hide.InstanceShader)
			{
				num++;
			}
			if (hide.InstanceSchematic)
			{
				num++;
			}
			if (hide.InstanceServer)
			{
				num++;
			}
			if (num == 7)
			{
				TextResource.Visibility = Visibility.Collapsed;
			}
			else
			{
				TextResource.Visibility = Visibility.Visible;
			}
		}
		else
		{
			TextResource.Visibility = Visibility.Visible;
		}
		ItemInstall.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceEdit) ? Visibility.Collapsed : Visibility.Visible);
		ItemExport.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceExport) ? Visibility.Collapsed : Visibility.Visible);
		ItemWorld.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceSave) ? Visibility.Collapsed : Visibility.Visible);
		ItemScreenshot.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceScreenshot) ? Visibility.Collapsed : Visibility.Visible);
		ItemResourcePack.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceResourcePack) ? Visibility.Collapsed : Visibility.Visible);
		ItemShader.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceShader) ? Visibility.Collapsed : Visibility.Visible);
		ItemSchematic.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceSchematic) ? Visibility.Collapsed : Visibility.Visible);
		ItemServer.Visibility = ((!PageSetupUI.HiddenForceShow && hide.InstanceServer) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void RefreshButton_Click(object sender, EventArgs e)
	{
		Refresh((FormMain.PageSubType)ModBase.Val(((MyIconButton)sender).Tag));
	}

	public void Refresh(FormMain.PageSubType subType)
	{
		switch (subType)
		{
		case FormMain.PageSubType.DownloadResourcePack:
			PageInstanceCompResource.Refresh(ModComp.CompType.Mod);
			break;
		case FormMain.PageSubType.DownloadDataPack:
			PageInstanceScreenshot.RefreshAsync();
			break;
		case FormMain.PageSubType.DownloadPack:
			PageInstanceSaves.Refresh();
			break;
		case FormMain.PageSubType.DownloadWorld:
			PageInstanceCompResource.Refresh(ModComp.CompType.ResourcePack);
			break;
		case FormMain.PageSubType.DownloadCompFavorites:
			PageInstanceCompResource.Refresh(ModComp.CompType.Shader);
			break;
		case FormMain.PageSubType.DownloadClient:
			PageInstanceCompResource.Refresh(ModComp.CompType.Schematic);
			break;
		case FormMain.PageSubType.DownloadOptiFine:
			ModDownload.dlClientListLoader.Start(null, isForceRestart: true);
			ModDownload.dlOptiFineListLoader.Start(null, isForceRestart: true);
			ModDownload.dlForgeListLoader.Start(null, isForceRestart: true);
			ModDownload.dlNeoForgeListLoader.Start(null, isForceRestart: true);
			ModDownload.dlLiteLoaderListLoader.Start(null, isForceRestart: true);
			ModDownload.dlFabricListLoader.Start(null, isForceRestart: true);
			ModDownload.dlFabricApiLoader.Start(null, isForceRestart: true);
			ModDownload.dlOptiFabricLoader.Start(null, isForceRestart: true);
			ModDownload.dlLabyModListLoader.Start(null, isForceRestart: true);
			ItemInstall.Checked = true;
			ModMain.frmInstanceInstall.GetCurrentInfo();
			break;
		case FormMain.PageSubType.DownloadMod:
			if (ModMain.frmInstanceExport != null)
			{
				ModMain.frmInstanceExport.RefreshAll();
			}
			ItemExport.Checked = true;
			break;
		case FormMain.PageSubType.DownloadForge:
			if (ModMain.frmInstanceServer != null)
			{
				ModMain.frmInstanceServer.RefreshServers();
			}
			ItemServer.Checked = true;
			break;
		case FormMain.PageSubType.DownloadShader:
			break;
		}
	}

	public void Reset(object sender, EventArgs e)
	{
		if (ModMain.MyMsgBox(Lang.Text("Instance.Left.InitializeSettings.ConfirmMessage"), Lang.Text("Instance.Left.InitializeSettings.ConfirmTitle"), null, Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 1)
		{
			if (ModMain.frmInstanceSetup == null)
			{
				ModMain.frmInstanceSetup = new PageInstanceSetup();
			}
			ModMain.frmInstanceSetup.Reset();
			ItemSetup.Checked = true;
		}
	}

	private void PageCheck(object sender, ModBase.RouteEventArgs e)
	{
		if (sender is MyListItem { Tag: not null } myListItem)
		{
			PageChange((FormMain.PageSubType)ModBase.Val(myListItem.Tag));
		}
	}

	public object PageGet(FormMain.PageSubType id)
	{
		if (id == (FormMain.PageSubType)(-1))
		{
			id = pageID;
		}
		switch (id)
		{
		case FormMain.PageSubType.Default:
			if (ModMain.frmInstanceOverall == null)
			{
				ModMain.frmInstanceOverall = new PageInstanceOverall();
			}
			return ModMain.frmInstanceOverall;
		case FormMain.PageSubType.DownloadResourcePack:
			if (ModMain.frmInstanceMod == null)
			{
				ModMain.frmInstanceMod = new PageInstanceCompResource(ModComp.CompType.Mod);
			}
			return ModMain.frmInstanceMod;
		case FormMain.PageSubType.DownloadShader:
			if (ModMain.frmInstanceModDisabled == null)
			{
				ModMain.frmInstanceModDisabled = new PageInstanceModDisabled();
			}
			return ModMain.frmInstanceModDisabled;
		case FormMain.PageSubType.DownloadNeoForge:
			if (ModMain.frmInstanceModJarInJar == null)
			{
				ModMain.frmInstanceModJarInJar = new PageInstanceCompJarInJar();
			}
			return ModMain.frmInstanceModJarInJar;
		case FormMain.PageSubType.DownloadInstall:
			if (ModMain.frmInstanceSetup == null)
			{
				ModMain.frmInstanceSetup = new PageInstanceSetup();
			}
			return ModMain.frmInstanceSetup;
		case FormMain.PageSubType.DownloadPack:
			if (ModMain.frmInstanceSaves == null)
			{
				ModMain.frmInstanceSaves = new PageInstanceSaves();
			}
			return ModMain.frmInstanceSaves;
		case FormMain.PageSubType.DownloadDataPack:
			if (ModMain.frmInstanceScreenshot == null)
			{
				ModMain.frmInstanceScreenshot = new PageInstanceScreenshot();
			}
			return ModMain.frmInstanceScreenshot;
		case FormMain.PageSubType.DownloadWorld:
			if (ModMain.frmInstanceResourcePack == null)
			{
				ModMain.frmInstanceResourcePack = new PageInstanceCompResource(ModComp.CompType.ResourcePack);
			}
			return ModMain.frmInstanceResourcePack;
		case FormMain.PageSubType.DownloadCompFavorites:
			if (ModMain.frmInstanceShader == null)
			{
				ModMain.frmInstanceShader = new PageInstanceCompResource(ModComp.CompType.Shader);
			}
			return ModMain.frmInstanceShader;
		case FormMain.PageSubType.DownloadClient:
			if (ModMain.frmInstanceSchematic == null)
			{
				ModMain.frmInstanceSchematic = new PageInstanceCompResource(ModComp.CompType.Schematic);
			}
			return ModMain.frmInstanceSchematic;
		case FormMain.PageSubType.DownloadOptiFine:
			if (ModMain.frmInstanceInstall == null)
			{
				ModMain.frmInstanceInstall = new PageInstanceInstall();
			}
			return ModMain.frmInstanceInstall;
		case FormMain.PageSubType.DownloadMod:
			if (ModMain.frmInstanceExport == null)
			{
				ModMain.frmInstanceExport = new PageInstanceExport();
			}
			return ModMain.frmInstanceExport;
		case FormMain.PageSubType.DownloadForge:
			if (ModMain.frmInstanceServer == null)
			{
				ModMain.frmInstanceServer = new PageInstanceServer();
			}
			return ModMain.frmInstanceServer;
		default:
		{
			int num = (int)id;
			throw new Exception("未知的实例设置子页面种类：" + num);
		}
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
			ModBase.Log(ex, "切换分页面失败（ID " + num + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Error.OperationFailed"));
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
