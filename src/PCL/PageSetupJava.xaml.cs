using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft;
using PCL.Core.UI;

namespace PCL;

public partial class PageSetupJava : MyPageRight, IComponentConnector
{
	private bool isLoad;

	public ModLoader.LoaderTask<bool, List<JavaEntry>> loader;

	public PageSetupJava()
	{
		InitializeComponent();
		loader = new ModLoader.LoaderTask<bool, List<JavaEntry>>("JavaPageLoader", Load_GetJavaList);
		Loaded += PageSetupLaunch_Loaded;
	}

	private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
	{
		PageLoaderInit(PanLoad, CardLoad, PanMain, null, loader, (ModLoader.LoaderBase _) =>
		{
			OnLoadFinished();
		}, Load_Input);
	}

	private object Load_Input()
	{
		return false;
	}

	private void Load_GetJavaList(ModLoader.LoaderTask<bool, List<JavaEntry>> loader)
	{
		if (loader.input || loader.isForceRestarting)
		{
			JavaService.JavaManager.ScanJavaAsync(true).GetAwaiter().GetResult();
			JavaService.JavaManager.CheckAllAvailability();
		}
		loader.output = ModJava.Javas.GetSortedJavaList();
	}

	private void OnLoadFinished()
	{
		PanContent.Children.Clear();
		MyListItem myListItem = new MyListItem
		{
			Type = MyListItem.CheckType.RadioBox,
			Title = Lang.Text("Setup.Java.AutoSelect.Title"),
			Info = Lang.Text("Setup.Java.AutoSelect.Info")
		};
		myListItem.Check += (object sender, ModBase.RouteEventArgs e) =>
		{
			Config.Launch.SelectedJava = "";
		};
		PanContent.Children.Add(myListItem);
		string selectedJava = Config.Launch.SelectedJava;
		foreach (JavaEntry sortedJava in ModJava.Javas.GetSortedJavaList())
		{
			MyListItem myListItem2 = ItemBuild(sortedJava);
			PanContent.Children.Add(myListItem2);
			if (sortedJava.Installation.JavaExePath == selectedJava)
			{
				myListItem2.SetChecked(value: true, user: false, anime: false);
			}
		}
		if (string.IsNullOrEmpty(selectedJava))
		{
			myListItem.SetChecked(value: true, user: false, anime: false);
		}
	}

	private MyListItem ItemBuild(JavaEntry j)
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		MyListItem item = new MyListItem();
		string versionTypeDesc = (j.Installation.IsJre ? "JRE" : "JDK");
		string text = j.Installation.MajorVersion.ToString();
		item.Title = versionTypeDesc + " " + text;
		item.Info = j.Installation.JavaFolder;
		List<string> list = new List<string>();
		string displayBits = (j.Installation.Is64Bit ? "64 Bit" : "32 Bit");
		list.Add(displayBits);
		string displayBrand = ((object)j.Installation.Brand/*cast due to constrained. prefix*/).ToString();
		list.Add(displayBrand);
		item.Tags = list;
		item.Type = MyListItem.CheckType.RadioBox;
		item.Check += (object sender, ModBase.RouteEventArgs e) =>
		{
			if (!j.Installation.IsStillAvailable)
			{
				HintService.Hint(Lang.Text("Setup.Java.Unavailable"));
			}
			else if (j.IsEnabled)
			{
				Config.Launch.SelectedJava = j.Installation.JavaExePath;
			}
			else
			{
				HintService.Hint(Lang.Text("Setup.Java.EnableBeforeSelect"));
				e.handled = true;
			}
		};
		MyIconButton myIconButton = new MyIconButton();
		myIconButton.SvgIcon = "lucide/folder-open";
		myIconButton.ToolTip = Lang.Text("Common.Action.Open");
		myIconButton.Click += (object sender, EventArgs e) =>
		{
			if (!j.Installation.IsStillAvailable)
			{
				HintService.Hint(Lang.Text("Setup.Java.Unavailable"));
			}
			else
			{
				ModBase.OpenExplorer(j.Installation.JavaFolder);
			}
		};
		MyIconButton myIconButton2 = new MyIconButton();
		myIconButton2.SvgIcon = "lucide/info";
		myIconButton2.ToolTip = Lang.Text("Setup.Java.Detail.ToolTip");
		myIconButton2.Click += (object sender, EventArgs e) =>
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			if (!j.Installation.IsStillAvailable)
			{
				HintService.Hint(Lang.Text("Setup.Java.Unavailable"));
			}
			else
			{
				ModMain.MyMsgBox(Lang.Text("Setup.Java.Info.Format", new object[6]
				{
					versionTypeDesc,
					j.Installation.Version.ToString(),
					((object)j.Installation.Architecture/*cast due to constrained. prefix*/).ToString(),
					displayBits,
					displayBrand,
					j.Installation.JavaFolder
				}), Lang.Text("Setup.Java.Info.Title"));
			}
		};
		MyIconButton btnEnableSwitch = new MyIconButton();
		item.Buttons = new _003C_003Ez__ReadOnlyArray<MyIconButton>(new MyIconButton[3] { myIconButton, myIconButton2, btnEnableSwitch });
		btnEnableSwitch.Click += (object _, EventArgs _) =>
		{
			try
			{
				JavaEntry val = ModJava.Javas.AddOrGet(j.Installation.JavaExePath);
				if (val == null)
				{
					HintService.Hint(Lang.Text("Setup.Java.Unavailable"));
				}
				else if (val.IsEnabled && Config.Launch.SelectedJava == val.Installation.JavaExePath)
				{
					HintService.Hint(Lang.Text("Setup.Java.DeselectBeforeDisable"));
				}
				else
				{
					val.IsEnabled = !val.IsEnabled;
					UpdateEnableStyle(val.IsEnabled);
					ModJava.Javas.SaveConfig();
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Setup.Java.EnableFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Setup.Java.EnableFailed"));
			}
		};
		UpdateEnableStyle(j.IsEnabled);
		return item;
		void UpdateEnableStyle(bool isCurEnable)
		{
			if (!j.Installation.IsStillAvailable)
			{
				HintService.Hint(Lang.Text("Setup.Java.Unavailable"));
			}
			else if (isCurEnable)
			{
				item.LabTitle.TextDecorations = null;
				item.LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1");
				btnEnableSwitch.SvgIcon = "lucide/circle-minus";
				btnEnableSwitch.ToolTip = Lang.Text("Setup.Java.Disable");
			}
			else
			{
				item.LabTitle.TextDecorations = TextDecorations.Strikethrough;
				item.LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray4");
				btnEnableSwitch.SvgIcon = "lucide/circle-check";
				btnEnableSwitch.ToolTip = Lang.Text("Setup.Java.Enable");
			}
		}
	}

	private void BtnAdd_Click(object sender, ModBase.RouteEventArgs e)
	{
		string ret = SystemDialogs.SelectFile(Lang.Text("Setup.Java.SelectFile.Filter"), Lang.Text("Setup.Java.SelectFile.Title"), (string)null);
		if (string.IsNullOrEmpty(ret) || !File.Exists(ret))
		{
			return;
		}
		if (ModJava.Javas.Exist(ret))
		{
			HintService.Hint(Lang.Text("Setup.Java.AlreadyExists"));
			return;
		}
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)(async () =>
		{
			await Task.Run(() =>
			{
				ModJava.Javas.AddOrGet(ret);
				ModJava.Javas.SaveConfig();
			});
			if (ModJava.Javas.Exist(ret))
			{
				HintService.Hint(Lang.Text("Setup.Java.Added"), HintType.Success);
				loader.Start(true, isForceRestart: true);
			}
			else
			{
				HintService.Hint(Lang.Text("Setup.Java.AddFailed"), HintType.Error);
			}
		}), Array.Empty<object>());
	}

}
