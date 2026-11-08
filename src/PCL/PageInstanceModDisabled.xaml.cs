using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using PCL.Core.App;

namespace PCL;

public partial class PageInstanceModDisabled : MyPageRight, IComponentConnector
{
	public PageInstanceModDisabled()
	{
		InitializeComponent();
		BtnDownload.Click += BtnDownload_Click;
		BtnVersion.Click += BtnVersion_Click;
		BtnDownload.Loaded += BtnDownload_Loaded;
	}

	private void BtnDownload_Click(object sender, EventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall);
	}

	private void BtnVersion_Click(object sender, EventArgs e)
	{
		ModMain.frmMain.PageChange(FormMain.PageType.Launch);
		ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
	}

	public void BtnDownload_Loaded(object? sender = null, RoutedEventArgs? e = null)
	{
		Visibility visibility = (((Config.Preference.Hide.PageDownload && !PageSetupUI.HiddenForceShow) || (ModMain.frmSelectRight != null && ModMain.frmSelectRight.showHidden)) ? Visibility.Collapsed : Visibility.Visible);
		if (BtnDownload.Visibility != visibility)
		{
			BtnDownload.Visibility = visibility;
			PanMain.TriggerForceResize();
		}
	}

}
