using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using FluentValidation;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Validate;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageInstanceOverall : MyPageRight, IComponentConnector
{
	private ModLoader.LoaderCombo<int> instanceInfoLoader;

	private bool isLoad;

	public MyListItem itemVersion;

	private MyCompItem modpackCompItem;

	public PageInstanceOverall()
	{
		InitializeComponent();
		Loaded += PageSetupLaunch_Loaded;
		LabInfoLoading.Text = Lang.Text("Instance.Overall.Info.Loading");
		ComboDisplayType.SelectionChanged += ComboDisplayType_SelectionChanged;
		BtnDisplayDesc.Click += BtnDisplayDesc_Click;
		BtnDisplayRename.Click += BtnDisplayRename_Click;
		ComboDisplayLogo.SelectionChanged += ComboDisplayLogo_SelectionChanged;
		BtnDisplayStar.Click += BtnDisplayStar_Click;
		BtnFolderVersion.Click += BtnFolderVersion_Click;
		BtnFolderSaves.Click += BtnFolderSaves_Click;
		BtnFolderMods.Click += BtnFolderMods_Click;
		BtnManageScript.Click += BtnManageScript_Click;
		BtnManageCheck.Click += BtnManageCheck_Click;
		BtnManageRestore.Click += BtnManageRestore_Click;
		BtnManageTest.Click += BtnManageTest_Click;
		BtnManageDelete.Click += BtnManageDelete_Click;
		BtnManagePatch.Click += BtnManagePatch_Click;
		BtnManageUpdateModpack.Click += BtnManageUpdateModpack_Click;
	}

	private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		ItemDisplayLogoCustom.Tag = "PCL\\Logo.png";
		Reload();
		if (!isLoad)
		{
			isLoad = true;
			PanDisplay.TriggerForceResize();
		}
	}

	private void Reload()
	{
		ModAnimation.AniControlEnabled++;
		McInstance mcInstance = PageInstanceLeft.McInstance;
		ComboDisplayType.SelectedIndex = ((ParameterizedProperty<object, int>)(object)States.Instance.CardType)[(object)mcInstance.PathInstance];
		BtnDisplayStar.Text = (mcInstance.IsStar ? Lang.Text("Instance.Overall.Unfavorite") : Lang.Text("Instance.Overall.Favorite"));
		BtnFolderMods.Visibility = ((!mcInstance.Modable) ? Visibility.Collapsed : Visibility.Visible);
		PanDisplayItem.Children.Clear();
		itemVersion = PageSelectRight.McVersionListItem(mcInstance);
		itemVersion.IsHitTestVisible = false;
		PanDisplayItem.Children.Add(itemVersion);
		ModMain.frmMain.PageNameRefresh();
		GetInstanceInfo();
		ComboDisplayLogo.SelectedIndex = 0;
		string text = ((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)mcInstance.PathInstance];
		if (((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)mcInstance.PathInstance])
		{
			foreach (MyComboBoxItem item in (IEnumerable)ComboDisplayLogo.Items)
			{
				if (object.Equals(item.Tag, text) || (object.Equals(item.Tag, "PCL\\Logo.png") && text.EndsWith("PCL\\Logo.png")))
				{
					ComboDisplayLogo.SelectedItem = item;
					break;
				}
			}
		}
		ModAnimation.AniControlEnabled--;
	}

	private void GetInstanceInfo()
	{
		modpackCompItem = null;
		ModBase.RunInUi(() =>
		{
			PanInfo.Children.Clear();
			PanInfo.Children.Add(new MyLoading
			{
				Text = Lang.Text("Instance.Overall.Info.Loading"),
				Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
			});
		});
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Overall.Info.LoadModpackInfoTask"), (ModLoader.LoaderTask<int, int> _) =>
		{
			string text = ((ParameterizedProperty<object, string>)(object)States.Instance.ModpackId)[(object)PageInstanceLeft.McInstance.PathInstance];
			if (!string.IsNullOrWhiteSpace(text))
			{
				List<ModComp.CompProject> compProjects = ModComp.CompRequest.GetCompProjectsByIds(new List<string> { text });
				if (compProjects.Count > 0)
				{
					ModBase.RunInUi(() =>
					{
						modpackCompItem = compProjects.First().ToCompItem(showMcVersionDesc: false, showLoaderDesc: false);
						modpackCompItem.Tag = compProjects.First();
					});
				}
			}
		})
		{
			block = true
		});
		list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Overall.Info.LoadInstanceInfoTask"), (ModLoader.LoaderTask<int, int> _) =>
		{
			ModBase.RunInUi(() =>
			{
				McInstance mcInstance = PageInstanceLeft.McInstance;
				McInstanceInfo info = mcInstance.Info;
				List<MyListItem> list2 = new List<MyListItem>();
				if (((ParameterizedProperty<object, int>)(object)States.Instance.LaunchCount)[(object)mcInstance.PathInstance] == 0)
				{
					list2.Add(new MyListItem
					{
						Title = Lang.Text("Instance.Overall.Info.LaunchCount.Title"),
						Info = Lang.Text("Instance.Overall.Info.LaunchCount.Never"),
						Logo = "pack://application:,,,/images/Blocks/RedstoneLampOff.png"
					});
				}
				else
				{
					list2.Add(new MyListItem
					{
						Title = Lang.Text("Instance.Overall.Info.LaunchCount.Title"),
						Info = Lang.Text("Instance.Overall.Info.LaunchCount.Count", new object[1] { ((ParameterizedProperty<object, int>)(object)States.Instance.LaunchCount)[(object)mcInstance.PathInstance] }),
						Logo = "pack://application:,,,/images/Blocks/RedstoneLampOn.png"
					});
				}
				if (!string.IsNullOrWhiteSpace(((ParameterizedProperty<object, string>)(object)States.Instance.ModpackVersion)[(object)mcInstance.PathInstance]))
				{
					list2.Add(new MyListItem
					{
						Title = Lang.Text("Instance.Overall.Info.ModpackVersion"),
						Info = ((ParameterizedProperty<object, string>)(object)States.Instance.ModpackVersion)[(object)mcInstance.PathInstance],
						Logo = "pack://application:,,,/images/Blocks/CommandBlock.png"
					});
				}
				list2.Add(new MyListItem
				{
					Title = "Minecraft",
					Info = info.VanillaName,
					Logo = "pack://application:,,,/images/Blocks/Grass.png"
				});
				if (info.HasForge)
				{
					list2.Add(new MyListItem
					{
						Title = "Forge",
						Info = info.Forge,
						Logo = "pack://application:,,,/images/Blocks/Anvil.png"
					});
				}
				if (info.HasNeoForge)
				{
					list2.Add(new MyListItem
					{
						Title = "NeoForge",
						Info = info.NeoForge,
						Logo = "pack://application:,,,/images/Blocks/NeoForge.png"
					});
				}
				if (info.HasCleanroom)
				{
					list2.Add(new MyListItem
					{
						Title = "Cleanroom",
						Info = info.Cleanroom,
						Logo = "pack://application:,,,/images/Blocks/Cleanroom.png"
					});
				}
				if (info.HasFabric)
				{
					list2.Add(new MyListItem
					{
						Title = "Fabric",
						Info = info.Fabric,
						Logo = "pack://application:,,,/images/Blocks/Fabric.png"
					});
				}
				if (info.HasQuilt)
				{
					list2.Add(new MyListItem
					{
						Title = "Quilt",
						Info = info.Quilt,
						Logo = "pack://application:,,,/images/Blocks/Quilt.png"
					});
				}
				if (info.HasOptiFine)
				{
					list2.Add(new MyListItem
					{
						Title = "OptiFine",
						Info = info.OptiFine,
						Logo = "pack://application:,,,/images/Blocks/GrassPath.png"
					});
				}
				if (info.HasLiteLoader)
				{
					list2.Add(new MyListItem
					{
						Title = "LiteLoader",
						Info = Lang.Text("Instance.Overall.Info.Installed"),
						Logo = "pack://application:,,,/images/Blocks/Egg.png"
					});
				}
				if (info.HasLegacyFabric)
				{
					list2.Add(new MyListItem
					{
						Title = "Legacy Fabric",
						Info = info.LegacyFabric,
						Logo = "pack://application:,,,/images/Blocks/Fabric.png"
					});
				}
				if (info.HasLabyMod)
				{
					list2.Add(new MyListItem
					{
						Title = "LabyMod",
						Info = info.LabyMod,
						Logo = "pack://application:,,,/images/Blocks/LabyMod.png"
					});
				}
				WrapPanel wrapPanel = new WrapPanel
				{
					Margin = new Thickness(0.0, -5.0, -20.0, 7.0)
				};
				foreach (MyListItem item in list2)
				{
					wrapPanel.Children.Add(item);
					wrapPanel.Children.Add(new TextBlock
					{
						Width = 2.0
					});
				}
				PanInfo.Children.Clear();
				if (modpackCompItem != null)
				{
					PanInfo.Children.Add(modpackCompItem);
					PanInfo.Children.Add(new TextBlock());
				}
				PanInfo.Children.Add(wrapPanel);
			});
		}));
		instanceInfoLoader = new ModLoader.LoaderCombo<int>("Instance Info Loader", list)
		{
			show = false
		};
		instanceInfoLoader.Start();
	}

	private void ComboDisplayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!isLoad || ModAnimation.AniControlEnabled != 0)
		{
			return;
		}
		if (ComboDisplayType.SelectedIndex != 1)
		{
			try
			{
				((ParameterizedProperty<object, int>)(object)States.Instance.CardType)[(object)PageInstanceLeft.McInstance.PathInstance] = ComboDisplayType.SelectedIndex;
				PageInstanceLeft.McInstance.displayType = (McInstanceCardType)((ParameterizedProperty<object, int>)(object)States.Instance.CardType)[(object)PageInstanceLeft.McInstance.PathInstance];
				ModMain.frmInstanceLeft.RefreshModDisabled();
				ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "修改实例分类失败（" + PageInstanceLeft.McInstance.Name + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
			}
			Reload();
			return;
		}
		try
		{
			if (!States.Hint.HideGameInstance)
			{
				if (ModMain.MyMsgBox(Lang.Text("Instance.Overall.Hide.ConfirmMessage"), Lang.Text("Instance.Overall.Hide.ConfirmTitle"), null, Lang.Text("Common.Action.Cancel")) != 1)
				{
					ComboDisplayType.SelectedIndex = 0;
					return;
				}
				States.Hint.HideGameInstance = true;
			}
			((ParameterizedProperty<object, int>)(object)States.Instance.CardType)[(object)PageInstanceLeft.McInstance.PathInstance] = 1;
			ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "隐藏实例 " + PageInstanceLeft.McInstance.Name + " 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnDisplayDesc_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			string text = ((ParameterizedProperty<object, string>)(object)States.Instance.CustomInfo)[(object)PageInstanceLeft.McInstance.PathInstance];
			string text2 = ModMain.MyMsgBoxInput(Lang.Text("Instance.Overall.Description.EditTitle"), Lang.Text("Instance.Overall.Description.EditMessage"), text, new Collection<IValidator<string>>(), Lang.Text("Instance.Overall.Description.Default"));
			if (text2 != null && (text ?? "") != (text2 ?? ""))
			{
				((ParameterizedProperty<object, string>)(object)States.Instance.CustomInfo)[(object)PageInstanceLeft.McInstance.PathInstance] = text2;
			}
			PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
			Reload();
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "实例 " + PageInstanceLeft.McInstance.Name + " 描述更改失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnDisplayRename_Click(object sender, MouseButtonEventArgs e)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		try
		{
			string name = PageInstanceLeft.McInstance.Name;
			string pathInstance = PageInstanceLeft.McInstance.PathInstance;
			string text = ModMain.MyMsgBoxInput(Lang.Text("Instance.Overall.Name.EditTitle"), "", name, new Collection<IValidator<string>> { (IValidator<string>)new FolderNameValidator(ModFolder.mcFolderSelected + "versions", true, false, false) });
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string text2 = Path.Combine(ModFolder.mcFolderSelected, "versions", text);
			string text3 = text + "_temp";
			string directory = Path.Combine(ModFolder.mcFolderSelected, "versions", text3);
			bool flag = (text.ToLower() ?? "") == (name.ToLower() ?? "");
			JsonObject jsonObject;
			try
			{
				jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".json"));
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "重命名读取 Json 时失败");
				jsonObject = PageInstanceLeft.McInstance.JsonObject;
			}
			FileSystem.RenameDirectory(pathInstance, text3);
			FileSystem.RenameDirectory(directory, text);
			ModBase.IniClearCache(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "options.txt"));
			if (Directory.Exists(Path.Combine(text2, name + "-natives")))
			{
				if (flag)
				{
					FileSystem.RenameDirectory(Path.Combine(text2, name + "-natives"), name + "natives_temp");
					FileSystem.RenameDirectory(Path.Combine(text2, name + "-natives_temp"), text + "-natives");
				}
				else
				{
					ModBase.DeleteDirectory(Path.Combine(text2, text + "-natives"));
					FileSystem.RenameDirectory(Path.Combine(text2, name + "-natives"), text + "-natives");
				}
			}
			if (File.Exists(Path.Combine(text2, name + ".jar")))
			{
				if (flag)
				{
					FileSystem.RenameFile(Path.Combine(text2, name + ".jar"), name + "_temp.jar");
					FileSystem.RenameFile(Path.Combine(text2, name + "_temp.jar"), text + ".jar");
				}
				else
				{
					File.Delete(Path.Combine(text2, text + ".jar"));
					FileSystem.RenameFile(Path.Combine(text2, name + ".jar"), text + ".jar");
				}
			}
			if (File.Exists(Path.Combine(text2, "PCL", "Setup.ini")))
			{
				ModBase.WriteFile(Path.Combine(text2, "PCL", "Setup.ini"), ModBase.ReadFile(Path.Combine(text2, "PCL", "Setup.ini")).Replace(pathInstance, text2));
			}
			if ((ModBase.ReadIni(ModFolder.mcFolderSelected + "PCL.ini", "Version") ?? "") == (name ?? ""))
			{
				ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", text);
			}
			try
			{
				jsonObject["id"] = text;
				ModBase.WriteFile(Path.Combine(text2, text + ".json"), jsonObject.ToString());
				if (!flag)
				{
					File.Delete(Path.Combine(text2, name + ".json"));
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "重命名实例 Json 失败");
			}
			HintService.Hint(Lang.Text("Instance.Overall.Name.RenameSuccess"), HintType.Success);
			PageInstanceLeft.McInstance = new McInstance(text).Load();
			if ((object)ModInstanceList.McMcInstanceSelected != null && ModInstanceList.McMcInstanceSelected.Equals(PageInstanceLeft.McInstance))
			{
				ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", text);
			}
			Reload();
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, "重命名实例失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void ComboDisplayLogo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!isLoad || ModAnimation.AniControlEnabled != 0)
		{
			return;
		}
		try
		{
			if (ComboDisplayLogo.SelectedItem == ItemDisplayLogoCustom)
			{
				string text = SystemDialogs.SelectFile(Lang.Text("Instance.Overall.Icon.SelectFile.Filter"), Lang.Text("Instance.Overall.Icon.SelectFile.Title"), (string)null);
				if (string.IsNullOrEmpty(text))
				{
					Reload();
					return;
				}
				ModBase.CopyFile(text, PageInstanceLeft.McInstance.PathInstance + "PCL\\Logo.png");
			}
			else
			{
				File.Delete(PageInstanceLeft.McInstance.PathInstance + "PCL\\Logo.png");
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "更改自定义实例图标失败（" + PageInstanceLeft.McInstance.Name + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
		try
		{
			string text2 = ((MyComboBoxItem)ComboDisplayLogo.SelectedItem).Tag?.ToString();
			((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)PageInstanceLeft.McInstance.PathInstance] = text2;
			((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)PageInstanceLeft.McInstance.PathInstance] = !string.IsNullOrEmpty(text2);
			ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
			PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
			Reload();
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "更改实例图标失败（" + PageInstanceLeft.McInstance.Name + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnDisplayStar_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			((ParameterizedProperty<object, bool>)(object)States.Instance.Starred)[(object)PageInstanceLeft.McInstance.PathInstance] = !PageInstanceLeft.McInstance.IsStar;
			PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
			Reload();
			ModInstanceList.mcInstanceListForceRefresh = true;
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "实例 " + PageInstanceLeft.McInstance.Name + " 收藏状态更改失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnFolderVersion_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
	{
		OpenVersionFolder(PageInstanceLeft.McInstance);
	}

	public static void OpenVersionFolder(McInstance version)
	{
		ModBase.OpenExplorer(version.PathInstance);
	}

	private void BtnFolderSaves_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
	{
		string text = PageInstanceLeft.McInstance.PathIndie + "saves\\";
		Directory.CreateDirectory(text);
		ModBase.OpenExplorer(text);
	}

	private void BtnFolderMods_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
	{
		string text = PageInstanceLeft.McInstance.PathIndie + "mods\\";
		Directory.CreateDirectory(text);
		ModBase.OpenExplorer(text);
	}

	private void BtnManageScript_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Invalid comparison between Unknown and I4
		try
		{
			string text = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Overall.Script.SelectSaveTitle"), "启动 " + PageInstanceLeft.McInstance.Name + ".bat", Lang.Text("Instance.Overall.Script.FileFilter"), (string)null);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (ModLaunch.mcLaunchLoader.State == ModBase.LoadState.Loading)
			{
				HintService.Hint(Lang.Text("Instance.Overall.Script.WaitForLaunchTask"), HintType.Error);
			}
			else if (ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
			{
				SaveBatch = text,
				instance = PageInstanceLeft.McInstance
			}))
			{
				McProfile current = ProfileService.Current;
				if (current != null && (int)((SafeProfile)current).ProfileType == 1)
				{
					HintService.Hint(Lang.Text("Instance.Overall.Script.Exporting"));
				}
				else
				{
					HintService.Hint(Lang.Text("Instance.Overall.Script.ExportingWarning"));
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "导出启动脚本失败（" + PageInstanceLeft.McInstance.Name + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnManageCheck_Click(object sender, MouseButtonEventArgs e)
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-repair"))
		{
			return;
		}
		try
		{
			if (ModLibrary.ShouldIgnoreFileCheck(PageInstanceLeft.McInstance))
			{
				HintService.Hint(Lang.Text("Instance.Overall.Repair.DisableVerificationHint"));
				return;
			}
			string taskName = PageInstanceLeft.McInstance.Name + " " + Lang.Text("Instance.Overall.Repair.TaskName");
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar)
			{
				if (!((item.name ?? "") != (taskName ?? "")))
				{
					HintService.Hint(Lang.Text("Instance.Overall.Repair.Processing"), HintType.Error);
					return;
				}
			}
			ModLoader.LoaderCombo<string> loader = new ModLoader.LoaderCombo<string>(taskName, ModDownload.DlClientFix(PageInstanceLeft.McInstance, checkAssetsHash: true, ModDownload.AssetsIndexExistsBehaviour.AlwaysDownload));
			loader.OnStateChanged = (ModLoader.LoaderBase _) =>
			{
				switch (loader.State)
				{
				case ModBase.LoadState.Finished:
					HintService.Hint(Lang.Text("Instance.Overall.Repair.Success.WithTaskName", new object[1] { taskName }), HintType.Success);
					break;
				case ModBase.LoadState.Failed:
					HintService.Hint(Lang.Text("Instance.Overall.Repair.Failed.WithDetail", new object[2]
					{
						taskName,
						loader.Error.ToString()
					}), HintType.Error);
					break;
				case ModBase.LoadState.Aborted:
					HintService.Hint(Lang.Text("Instance.Overall.Repair.Cancelled.WithTaskName", new object[1] { taskName }));
					break;
				}
			};
			loader.Start(PageInstanceLeft.McInstance.Name);
			ModLoader.LoaderTaskbarAdd(loader);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "尝试补全文件失败（" + PageInstanceLeft.McInstance.Name + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnManageRestore_Click(object sender, MouseButtonEventArgs e)
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-reset"))
		{
			return;
		}
		try
		{
			McInstanceInfo info = PageInstanceLeft.McInstance.Info;
			if (info.Drop != 99 && McVersionComparer.CompareVersion(info.VanillaName, "1.5.2") == -1 && info.HasForge)
			{
				HintService.Hint(Lang.Text("Instance.Overall.Reset.NotSupported"));
			}
			else if (info.HasQuilt)
			{
				HintService.Hint(Lang.Text("Instance.Overall.Reset.QuiltUnsupported"));
			}
			else if (ModMain.MyMsgBox(Lang.Text("Instance.Overall.Reset.ConfirmMessage", new object[1] { PageInstanceLeft.McInstance.Name }), Lang.Text("Instance.Overall.Reset.ConfirmTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) != 2)
			{
				ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".json", PageInstanceLeft.McInstance.PathInstance + "PCLInstallBackups\\" + PageInstanceLeft.McInstance.Name + ".json");
				ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar", PageInstanceLeft.McInstance.PathInstance + "PCLInstallBackups\\" + PageInstanceLeft.McInstance.Name + ".jar");
				if (ModDownloadLib.McInstall(new ModDownloadLib.McInstallRequest
				{
					targetInstanceName = PageInstanceLeft.McInstance.Name,
					targetInstanceFolder = ModFolder.mcFolderSelected + "versions\\" + PageInstanceLeft.McInstance.Name + "\\",
					minecraftName = info.VanillaName,
					optiFineEntry = (info.HasOptiFine ? new ModDownload.DlOptiFineListEntry
					{
						Inherit = info.VanillaName,
						DisplayName = info.VanillaName + " " + info.OptiFine
					} : null),
					forgeEntry = (info.HasForge ? new ModDownload.DlForgeVersionEntry(info.Forge, null, info.VanillaName)
					{
						Category = "installer"
					} : null),
					forgeVersion = (info.HasForge ? info.Forge : null),
					neoForgeVersion = (info.HasNeoForge ? info.NeoForge : null),
					cleanroomVersion = (info.HasCleanroom ? info.Cleanroom : null),
					fabricVersion = (info.HasFabric ? info.Fabric : null),
					liteLoaderEntry = (info.HasLiteLoader ? new ModDownload.DlLiteLoaderListEntry
					{
						Inherit = info.VanillaName
					} : null),
					legacyFabricVersion = (info.HasLegacyFabric ? info.LegacyFabric : null)
				}, Lang.Text("Common.Action.Reset")))
				{
					ModMain.frmMain.PageChange(new FormMain.PageStackData
					{
						page = FormMain.PageType.Launch
					});
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "重置实例 " + PageInstanceLeft.McInstance.Name + " 失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnManageTest_Click(object sender, MouseButtonEventArgs e)
	{
		try
		{
			ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
			{
				instance = PageInstanceLeft.McInstance,
				IsTest = true
			});
			ModMain.frmMain.PageChange(FormMain.PageType.Launch);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "测试游戏失败", ModBase.LogLevel.Feedback, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnManageDelete_Click(object sender, MouseButtonEventArgs e)
	{
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			bool flag = Keyboard.IsKeyDown((Key)116) || Keyboard.IsKeyDown((Key)117);
			bool flag2 = PageInstanceLeft.McInstance.state != McInstanceState.Error && !string.Equals(PageInstanceLeft.McInstance.PathIndie, ModFolder.mcFolderSelected, StringComparison.OrdinalIgnoreCase);
			string text;
			if (flag2)
			{
				text = ((!flag) ? "Instance.Overall.Delete.ConfirmMessageIsolated" : "Instance.Overall.Delete.ConfirmMessageIsolatedPermanent");
			}
			else
			{
				text = ((!flag) ? "Instance.Overall.Delete.ConfirmMessage" : "Instance.Overall.Delete.ConfirmMessagePermanent");
			}
			switch (ModMain.MyMsgBox(Lang.Text(text, new object[1] { PageInstanceLeft.McInstance.Name }), Lang.Text("Instance.Overall.Delete.ConfirmTitle"), null, Lang.Text("Common.Action.Cancel"), "", flag2 | flag))
			{
			case 1:
			{
				string pathInstance = PageInstanceLeft.McInstance.PathInstance;
				string name = PageInstanceLeft.McInstance.Name;
				ModBase.IniClearCache(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "options.txt"));
				((DynamicCacheConfigStorage)ConfigService.GetProvider((ConfigSource)3)).InvalidateCache((object)pathInstance);
				if (flag)
				{
					ModBase.DeleteDirectory(pathInstance);
					HintService.Hint(Lang.Text("Instance.Overall.Delete.PermanentSuccess", new object[1] { name }), HintType.Success);
				}
				else
				{
					FileSystem.DeleteDirectory(pathInstance, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
					HintService.Hint(Lang.Text("Instance.Overall.Delete.RecycleBinSuccess", new object[1] { name }), HintType.Success);
				}
				break;
			}
			case 2:
				return;
			}
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
			ModMain.frmMain.PageBack();
		}
		catch (OperationCanceledException ex)
		{
			ModBase.Log(ex, "删除实例 " + PageInstanceLeft.McInstance.Name + " 被主动取消");
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "删除实例 " + PageInstanceLeft.McInstance.Name + " 失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.Error.OperationFailed"));
		}
	}

	private void BtnManagePatch_Click(object sender, MouseButtonEventArgs e)
	{
		int num = ModMain.MyMsgBox(Lang.Text("Instance.Overall.Patch.ConfirmMessage", new object[1] { PageInstanceLeft.McInstance.Name }), Lang.Text("Instance.Overall.Patch.ConfirmTitle"), null, Lang.Text("Common.Action.Cancel"));
		if (num != 1)
		{
			_ = 2;
			return;
		}
		string userInput = SystemDialogs.SelectFile(Lang.Text("Instance.Overall.Patch.SelectFile.Filter"), Lang.Text("Instance.Overall.Patch.SelectFile.Title"), (string)null);
		if (userInput != null && !string.IsNullOrWhiteSpace(userInput))
		{
			HintService.Hint(Lang.Text("Instance.Overall.Patch.Patching"));
			ModBase.RunInNewThread(() =>
			{
				//IL_001e: Unknown result type (might be due to invalid IL or missing references)
				new GameCore(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar").AddToCore(userInput);
				HintService.Hint(Lang.Text("Instance.Overall.Patch.Success"), HintType.Success);
				((ParameterizedProperty<object, bool>)(object)Config.Instance.DisableAssetVerifyV2)[(object)PageInstanceLeft.McInstance.PathInstance] = true;
			});
		}
	}


	private void BtnManageUpdateModpack_Click(object sender, MouseButtonEventArgs e)
	{
		if (!RestrictedFeaturePolicy.Check("modpack-install"))
		{
			return;
		}
		try
		{
			McInstance instance = PageInstanceLeft.McInstance;
			string modpackId = States.Instance.ModpackId[instance.PathInstance];
			string modpackSource = States.Instance.ModpackSource[instance.PathInstance];
			string modpackVersion = States.Instance.ModpackVersion[instance.PathInstance];
			if (string.IsNullOrWhiteSpace(modpackId) || string.IsNullOrWhiteSpace(modpackSource))
			{
				if (ModMain.MyMsgBox(Lang.Text("Instance.Overall.UpdateModpack.NotModpack"), Lang.Text("Instance.Overall.UpdateModpack.Title"), Lang.Text("Instance.Overall.UpdateModpack.SelectLocal"), Lang.Text("Common.Action.Cancel")) != 1)
				{
					return;
				}
				string localFile = SystemDialogs.SelectFile(Lang.Text("Minecraft.Download.Modpack.FileDialog.Filter"), Lang.Text("Instance.Overall.UpdateModpack.SelectLocalTitle"), instance.PathInstance);
				if (string.IsNullOrWhiteSpace(localFile))
				{
					return;
				}
				StartLocalModpackUpdate(instance, localFile);
				return;
			}
			bool fromCurseForge = modpackSource == "CurseForge";
			string taskName = Lang.Text("Instance.Overall.UpdateModpack.TaskName", new object[1] { instance.Name });
			ModComp.CompProject project = null;
			ModComp.CompFile latestFile = null;
			ModComp.CompFile currentFile = null;
			List<ModLoader.LoaderBase> checkLoaders = new List<ModLoader.LoaderBase>
			{
				new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Check"), (ModLoader.LoaderTask<int, int> task) =>
				{
					List<ModComp.CompProject> projects = ModComp.CompRequest.GetCompProjectsByIds(new List<string> { modpackId });
					if (projects.Count == 0)
					{
						throw new Exception(Lang.Text("Instance.Overall.UpdateModpack.ProjectNotFound"));
					}
					project = projects[0];
					List<ModComp.CompFile> files = ModComp.CompFilesGet(modpackId, fromCurseForge);
					latestFile = (from f in files
						where f.Available && f.Status == ModComp.CompFileStatus.Release
						orderby f.ReleaseDate descending
						select f).FirstOrDefault();
					if (latestFile == null)
					{
						latestFile = (from f in files
							where f.Available
							orderby f.Status == ModComp.CompFileStatus.Release descending, f.ReleaseDate descending
							select f).FirstOrDefault();
					}
					if (latestFile == null)
					{
						throw new Exception(Lang.Text("Instance.Overall.UpdateModpack.NoFile"));
					}
					string installedFileId = ModInstanceHelper.ReadInstalledFileMarker(instance.PathInstance);
					currentFile = string.IsNullOrWhiteSpace(installedFileId)
						? files.FirstOrDefault(f => ModInstanceHelper.IsSameModpackVersion(f, modpackVersion, fromCurseForge))
						: files.FirstOrDefault(f => string.Equals(f.Id, installedFileId, StringComparison.OrdinalIgnoreCase));
					if (currentFile == null)
					{
						currentFile = files.FirstOrDefault(f => ModInstanceHelper.IsSameModpackVersion(f, modpackVersion, fromCurseForge));
					}
				})
			};
			ModLoader.LoaderCombo<string> checkCombo = new ModLoader.LoaderCombo<string>(taskName, checkLoaders);
			checkCombo.OnStateChanged = (ModLoader.LoaderBase loader) =>
			{
				switch (loader.State)
				{
				case ModBase.LoadState.Finished:
					HandleModpackCheckResult(instance, project, latestFile, currentFile, modpackVersion, fromCurseForge);
					break;
				case ModBase.LoadState.Failed:
					ModBase.Log(loader.Error, "检查整合包更新失败（" + instance.Name + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.UpdateModpack.Failed"));
					break;
				case ModBase.LoadState.Aborted:
					HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.Cancelled"));
					break;
				}
			};
			checkCombo.Start();
			ModLoader.LoaderTaskbarAdd(checkCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "检查整合包更新失败（" + PageInstanceLeft.McInstance.Name + "）", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.UpdateModpack.Failed"));
		}
	}

	private void HandleModpackCheckResult(McInstance instance, ModComp.CompProject project, ModComp.CompFile latestFile, ModComp.CompFile currentFile, string modpackVersion, bool fromCurseForge)
	{
		try
		{
			if (currentFile != null && currentFile.ReleaseDate >= latestFile.ReleaseDate)
			{
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.NoUpdate", new object[1] { (string.IsNullOrWhiteSpace(modpackVersion) ? currentFile.DisplayName : modpackVersion) }), HintType.Success);
				return;
			}
			string latestVersion = string.IsNullOrWhiteSpace(latestFile.Version) ? latestFile.DisplayName : latestFile.Version;
			if (ModInstanceHelper.IsSameModpackVersion(latestFile, modpackVersion, fromCurseForge))
			{
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.NoUpdate", new object[1] { modpackVersion }), HintType.Success);
				return;
			}
			if (ModMain.MyMsgBox(Lang.Text("Instance.Overall.UpdateModpack.ConfirmMessage", new object[3]
			{
				instance.Name,
				(string.IsNullOrWhiteSpace(modpackVersion) ? Lang.Text("Instance.Overall.UpdateModpack.UnknownVersion") : modpackVersion),
				latestVersion
			}), Lang.Text("Instance.Overall.UpdateModpack.ConfirmTitle"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) != 1)
			{
				return;
			}
			StartOnlineModpackUpdate(instance, project, latestFile, latestVersion);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "处理整合包更新检查结果失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Overall.UpdateModpack.Failed"));
		}
	}

	private void StartOnlineModpackUpdate(McInstance instance, ModComp.CompProject project, ModComp.CompFile file, string latestVersion)
	{
		string instanceName = instance.Name;
		string instancePath = instance.PathInstance;
		string tempName = ModInstanceHelper.BuildTempInstanceName(instanceName);
		string tempPath = Path.Combine(ModFolder.mcFolderSelected, "versions", tempName) + "\\";
		string extension = project.FromCurseForge ? "zip" : "mrpack";
		string archivePath = Path.Combine(tempPath, "原始整合包." + extension);
		string taskName = Lang.Text("Instance.Overall.UpdateModpack.TaskName", new object[1] { instanceName });
		string selectedVersion = ModBase.ReadIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", "");
		Directory.CreateDirectory(tempPath);
		ModBase.WriteFile(Path.Combine(tempPath, ".pclignore"), "正在更新整合包，请稍候。");
		List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>();
		loaders.Add(new LoaderDownload(Lang.Text("Instance.Overall.UpdateModpack.Stage.Download"), new List<DownloadFile> { file.ToNetFile(archivePath, ModComp.DownloadReason.ModPack) })
		{
			ProgressWeight = 20.0
		});
		loaders.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Install"), (ModLoader.LoaderTask<string, string> task) =>
		{
			string logoPath = null;
			try
			{
				if (!string.IsNullOrWhiteSpace(project.LogoUrl))
				{
					string cachedLogo = MyImage.GetTempPath(project.LogoUrl);
					if (!File.Exists(cachedLogo))
					{
						MyImage.DownloadImageAsync(project.LogoUrl).GetAwaiter().GetResult();
					}
					if (File.Exists(cachedLogo))
					{
						logoPath = cachedLogo;
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "获取整合包图标失败");
			}
			try
			{
				ModLoader.LoaderCombo<string> install = ModModpack.ModpackInstall(archivePath, tempName, logoPath, file.ProjectId, isOnlineInstall: true);
				ModLoader.loaderTaskbar.Remove(install);
				install.WaitForExit();
			}
			finally
			{
				if (!string.IsNullOrWhiteSpace(selectedVersion))
				{
					ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", selectedVersion);
				}
				// 整合包安装流程会删除 .pclignore，这里补回来，避免临时实例出现在实例列表中。
				Directory.CreateDirectory(tempPath);
				ModBase.WriteFile(Path.Combine(tempPath, ".pclignore"), "正在更新整合包，请稍候。");
			}
			task.Progress = 0.9;
		})
		{
			ProgressWeight = 60.0
		});
		loaders.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Merge"), (ModLoader.LoaderTask<string, string> task) =>
		{
			task.Progress = 0.0;
			ModInstanceHelper.MergeUserData(instancePath, tempPath, task, 1.0);
			task.Progress = 1.0;
		})
		{
			ProgressWeight = 10.0
		});
		loaders.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Replace"), (ModLoader.LoaderTask<string, string> task) =>
		{
			File.Delete(Path.Combine(tempPath, ".pclignore"));
			string backupPath = ModInstanceHelper.ReplaceInstance(instanceName, instancePath, tempName, tempPath);
			ModInstanceHelper.WriteInstalledFileMarker(instancePath, file);
			ModBase.Log("[ModPack] 整合包已更新到 " + latestVersion + "，备份：" + backupPath);
			task.Progress = 1.0;
		})
		{
			ProgressWeight = 10.0
		});
		ModLoader.LoaderCombo<string> combo = new ModLoader.LoaderCombo<string>(taskName, loaders);
		combo.OnStateChanged = (ModLoader.LoaderBase loader) =>
		{
			switch (loader.State)
			{
			case ModBase.LoadState.Finished:
				RestoreSelectedVersion(selectedVersion);
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.Success", new object[2] { instanceName, latestVersion }), HintType.Success);
				ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
				PageInstanceLeft.McInstance = new McInstance(instanceName).Load();
				Reload();
				break;
			case ModBase.LoadState.Failed:
				RestoreSelectedVersion(selectedVersion);
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.Failed"), HintType.Error);
				CleanTempInstance(tempPath);
				break;
			case ModBase.LoadState.Aborted:
				RestoreSelectedVersion(selectedVersion);
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.Cancelled"));
				CleanTempInstance(tempPath);
				break;
			}
		};
		combo.Start(tempPath);
		ModLoader.LoaderTaskbarAdd(combo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
	}

	private void StartLocalModpackUpdate(McInstance instance, string filePath)
	{
		string instanceName = instance.Name;
		string instancePath = instance.PathInstance;
		string tempName = ModInstanceHelper.BuildTempInstanceName(instanceName);
		string tempPath = Path.Combine(ModFolder.mcFolderSelected, "versions", tempName) + "\\";
		string archivePath = Path.Combine(tempPath, "原始整合包.zip");
		string taskName = Lang.Text("Instance.Overall.UpdateModpack.TaskName", new object[1] { instanceName });
		string selectedVersion = ModBase.ReadIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", "");
		Directory.CreateDirectory(tempPath);
		ModBase.WriteFile(Path.Combine(tempPath, ".pclignore"), "正在更新整合包，请稍候。");
		List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>();
		loaders.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Install"), (ModLoader.LoaderTask<string, string> task) =>
		{
			Directory.CreateDirectory(tempPath);
			ModBase.CopyFile(filePath, archivePath);
			task.Progress = 0.1;
			try
			{
				ModLoader.LoaderCombo<string> install = ModModpack.ModpackInstall(archivePath, tempName, null, null, isOnlineInstall: true);
				ModLoader.loaderTaskbar.Remove(install);
				install.WaitForExit();
			}
			finally
			{
				if (!string.IsNullOrWhiteSpace(selectedVersion))
				{
					ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", selectedVersion);
				}
				Directory.CreateDirectory(tempPath);
				ModBase.WriteFile(Path.Combine(tempPath, ".pclignore"), "正在更新整合包，请稍候。");
			}
			task.Progress = 0.9;
		})
		{
			ProgressWeight = 70.0
		});
		loaders.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Merge"), (ModLoader.LoaderTask<string, string> task) =>
		{
			task.Progress = 0.0;
			ModInstanceHelper.MergeUserData(instancePath, tempPath, task, 1.0);
			task.Progress = 1.0;
		})
		{
			ProgressWeight = 15.0
		});
		loaders.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Instance.Overall.UpdateModpack.Stage.Replace"), (ModLoader.LoaderTask<string, string> task) =>
		{
			File.Delete(Path.Combine(tempPath, ".pclignore"));
			string backupPath = ModInstanceHelper.ReplaceInstance(instanceName, instancePath, tempName, tempPath);
			ModBase.Log("[ModPack] 已使用本地文件更新整合包，备份：" + backupPath);
			task.Progress = 1.0;
		})
		{
			ProgressWeight = 15.0
		});
		ModLoader.LoaderCombo<string> combo = new ModLoader.LoaderCombo<string>(taskName, loaders);
		combo.OnStateChanged = (ModLoader.LoaderBase loader) =>
		{
			switch (loader.State)
			{
			case ModBase.LoadState.Finished:
				RestoreSelectedVersion(selectedVersion);
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.LocalSuccess", new object[1] { instanceName }), HintType.Success);
				ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
				ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
				PageInstanceLeft.McInstance = new McInstance(instanceName).Load();
				Reload();
				break;
			case ModBase.LoadState.Failed:
				RestoreSelectedVersion(selectedVersion);
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.Failed"), HintType.Error);
				CleanTempInstance(tempPath);
				break;
			case ModBase.LoadState.Aborted:
				RestoreSelectedVersion(selectedVersion);
				HintService.Hint(Lang.Text("Instance.Overall.UpdateModpack.Cancelled"));
				CleanTempInstance(tempPath);
				break;
			}
		};
		combo.Start(tempPath);
		ModLoader.LoaderTaskbarAdd(combo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
	}

	private static void RestoreSelectedVersion(string selectedVersion)
	{
		if (!string.IsNullOrWhiteSpace(selectedVersion))
		{
			ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", selectedVersion);
		}
	}

	private static void CleanTempInstance(string tempPath)
	{
		try
		{
			if (Directory.Exists(tempPath))
			{
				((DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance)).InvalidateCache(tempPath);
				ModBase.DeleteDirectory(tempPath);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "清理临时实例文件夹失败：" + tempPath);
		}
	}
}
