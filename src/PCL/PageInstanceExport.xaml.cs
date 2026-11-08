using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using DotNet.Globbing;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.Utils;

namespace PCL;

public partial class PageInstanceExport : MyPageRight, IRefreshable, IComponentConnector
{
	private string currentVersion = "";

	private readonly string[] subOptionBlackList = new string[2] { "Quark Programmer Art.zip", "+ EuphoriaPatches_" };

	private const string sperator = "==============================================================";

	private List<string> extraFiles;

	private string configPackPath;

	private List<string> RulesOverrides
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			if (value == null)
			{
				BtnOverrideCancel.Visibility = Visibility.Collapsed;
				PanOptions.Visibility = Visibility.Visible;
				CardOptions.Inlines.Clear();
				CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionListTitle"))
				{
					FontWeight = FontWeights.Bold
				});
			}
			else
			{
				BtnOverrideCancel.Visibility = Visibility.Visible;
				PanOptions.Visibility = Visibility.Collapsed;
				CardOptions.Inlines.Clear();
				CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionListTitle") + ":\u200a\u200a\u200a\u200a")
				{
					FontWeight = FontWeights.Bold
				});
				CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionList.FromConfig"))
				{
					FontWeight = FontWeights.Normal
				});
			}
		}
	}

	public PageInstanceExport()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			PageInstanceExport_Loaded();
		};
		CardOptions.MouseLeftButtonDown += CardOptions_MouseLeftButtonDown;
		BtnAdvancedExport.Click += ExportConfig;
		BtnAdvancedImport.Click += ImportConfig;
		BtnExport.Click += StartExport;
		TextExportName.GotFocus += TextExportName_GotFocus;
		CheckAdvancedModrinth.Change += CheckAdvancedModrinth_Change;
		CheckAdvancedInclude.Change += CheckAdvancedInclude_Change;
	}

	void IRefreshable.Refresh()
	{
		RefreshAll();
	}

	private void PageInstanceExport_Loaded()
	{
		ModAnimation.AniControlEnabled++;
		if ((currentVersion ?? "") != (PageInstanceLeft.McInstance.PathInstance ?? ""))
		{
			RefreshAll();
		}
		ModAnimation.AniControlEnabled--;
	}

	public void RefreshAll()
	{
		ModBase.Log("[Export] 刷新导出页面");
		HintOptiFine.Visibility = ((!PageInstanceLeft.McInstance.Info.HasOptiFine) ? Visibility.Collapsed : Visibility.Visible);
		currentVersion = PageInstanceLeft.McInstance.PathInstance;
		TextExportName.Text = "";
		TextExportName.HintText = PageInstanceLeft.McInstance.Name;
		TextExportVersion.Text = "";
		TextExportVersion.HintText = "1.0.0";
		CheckAdvancedInclude.Checked = false;
		CheckAdvancedModrinth.Checked = false;
		GetExportOption(CheckOptionsBasic).Description = PageInstanceLeft.McInstance.GetDefaultDescription();
		ResetConfigOverrides();
		ReloadAllSubOptions();
		RefreshAllOptionsUI();
		PanBack.ScrollToHome();
	}

	private void TextExportName_GotFocus(object sender, RoutedEventArgs routedEventArgs)
	{
		if (string.IsNullOrEmpty(TextExportName.Text))
		{
			TextExportName.Text = TextExportName.HintText;
			TextExportName.SelectionStart = TextExportName.Text.Length;
		}
	}

	private void CheckAdvancedModrinth_Change(object sender, bool user)
	{
		if (CheckAdvancedModrinth.Checked == true)
		{
			CheckOptionsPcl.Checked = false;
		}
		CheckOptionsPcl.IsEnabled = (!CheckAdvancedModrinth.Checked).Value;
	}

	private void CheckOptionsOtherFolders_Change(object sender, bool user)
	{
		if (!user)
		{
			return;
		}
		foreach (object child in PanOptionsOtherFolders.Children)
		{
			if (child is MyCheckBox myCheckBox)
			{
				myCheckBox.Checked = CheckOptionsOtherFolders.Checked;
			}
		}
	}

	private void CheckAdvancedInclude_Change(object sender, bool user)
	{
		if (CheckAdvancedInclude.Checked == true)
		{
			CheckAdvancedModrinth.Checked = false;
		}
		CheckAdvancedModrinth.IsEnabled = (!CheckAdvancedInclude.Checked).Value;
	}

	private void ReloadAllSubOptions()
	{
		ReloadSubOptions(PanOptionsResourcePacks, true, true, "resourcepacks", "texturepacks");
		ReloadSubOptions(PanOptionsSaves, false, true, "saves");
		ReloadSubOptions(PanOptionsShaderPacks, true, true, "shaderpacks");
		ReloadOtherFolders();
	}

	private void ReloadOtherFolders()
	{
		PanOptionsOtherFolders.Children.Clear();
		DirectoryInfo directoryInfo = new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie);
		if (!directoryInfo.Exists)
		{
			CheckOptionsOtherFolders.Visibility = Visibility.Collapsed;
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"mods", "coremods", "lib", "addons", "multiblocked", "modpack-update-checker", "global_packs", "global_resource_packs", "global_data_packs", "optional_data_packs",
			"maps", "mods-resourcepacks", "matmos", "resource_assorts", "patchouli_books", "datapacks", "openloader", "worldshape", "resources", "scripts",
			"structures", "fontfiles", "oresources", "packmenu", "craftpresence", "pointblanks", "config", "defaultconfigs", "journeymap", "local",
			"essential", "gg.essential.mod", "CustomSkinLoader", "xaero", "XaeroWaypoints", "XaeroWorldMap", "resourcepacks", "texturepacks", "shaderpacks", "screenshots",
			"schematics", "replay_recordings", "replay_videos", "saves", "configureddefaults", "assets", "versions", "libraries", "structureCacheV1", ".fabric",
			".git", "avatar-cache", "cosmetic-cache", "PCL"
		};
		string[] source = new string[2] { "kubejs", "template" };
		string[] source2 = new string[1] { "-natives" };
		foreach (DirectoryInfo subDir in directoryInfo.EnumerateDirectories())
		{
			if (!hashSet.Contains(subDir.Name) && !source.Any((string p) => subDir.Name.StartsWithF(p)) && !source2.Any((string s) => subDir.Name.EndsWithF(s, ignoreCase: true)))
			{
				PanOptionsOtherFolders.Children.Add(new MyCheckBox
				{
					Tag = new ExportOption
					{
						Title = subDir.Name,
						DefaultChecked = false,
						Rules = ModBase.EscapeLikePattern(subDir.Name + "/")
					}
				});
			}
		}
		CheckOptionsOtherFolders.Visibility = ((PanOptionsOtherFolders.Children.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void ReloadSubOptions(StackPanel panel, bool acceptCompressedFile, bool acceptFolder, params string[] folders)
	{
		panel.Children.Clear();
		foreach (string text in folders)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie + text);
			if (!directoryInfo.Exists)
			{
				continue;
			}
			if (acceptCompressedFile)
			{
				foreach (FileInfo File in directoryInfo.EnumerateFiles("*.zip").Concat(directoryInfo.EnumerateFiles("*.rar")))
				{
					if (subOptionBlackList.Any((string b) => File.Name.ContainsF(b)))
					{
						continue;
					}
					panel.Children.Add(new MyCheckBox
					{
						Tag = new ExportOption
						{
							Title = File.Name,
							DefaultChecked = true,
							Rules = ModBase.EscapeLikePattern(text + "/" + File.Name)
						}
					});
					if (text == "shaderpacks")
					{
						FileInfo fileInfo = new FileInfo(Path.Combine(File.Directory.FullName, File.Name + ".txt"));
						if (fileInfo.Exists)
						{
							panel.Children.Add(new MyCheckBox
							{
								Margin = new Thickness(30.0, 0.0, 0.0, 0.0),
								Tag = new ExportOption
								{
									Title = (fileInfo.Name ?? ""),
									DefaultChecked = true,
									Description = Lang.Text("Instance.Export.Config.ShaderConfigSuffix"),
									Rules = ModBase.EscapeLikePattern(text + "/" + fileInfo.Name)
								}
							});
						}
					}
				}
			}
			if (!acceptFolder)
			{
				continue;
			}
			foreach (DirectoryInfo SubFolder in from f in directoryInfo.EnumerateDirectories()
				orderby f.LastWriteTime descending
				select f)
			{
				if (subOptionBlackList.Any((string b) => SubFolder.Name.ContainsF(b)) || !SubFolder.EnumerateFileSystemInfos().Any())
				{
					continue;
				}
				MyCheckBox myCheckBox = new MyCheckBox
				{
					Tag = new ExportOption
					{
						Title = SubFolder.Name,
						DefaultChecked = true,
						Rules = ModBase.EscapeLikePattern(text + "/" + SubFolder.Name + "/")
					}
				};
				if (panel == PanOptionsSaves)
				{
					GetExportOption(myCheckBox).Description = Lang.Date(SubFolder.LastWriteTime, "g");
				}
				panel.Children.Add(myCheckBox);
				if (text == "shaderpacks")
				{
					FileInfo fileInfo2 = new FileInfo(Path.Combine(directoryInfo.FullName, SubFolder.Name + ".txt"));
					if (fileInfo2.Exists)
					{
						panel.Children.Add(new MyCheckBox
						{
							Margin = new Thickness(30.0, 0.0, 0.0, 0.0),
							Tag = new ExportOption
							{
								Title = (fileInfo2.Name ?? ""),
								DefaultChecked = true,
								Description = Lang.Text("Instance.Export.Config.ShaderConfigSuffix"),
								Rules = ModBase.EscapeLikePattern(text + "/" + fileInfo2.Name)
							}
						});
					}
				}
			}
		}
	}

	private void RefreshAllOptionsUI()
	{
		List<string> allEntries = new List<string>();
		DirectoryInfo directoryInfo = new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie);
		allEntries.AddRange(from f in directoryInfo.EnumerateFiles()
			select f.Name);
		foreach (DirectoryInfo SubFolder in directoryInfo.EnumerateDirectories().Where(IsValidDirectory))
		{
			allEntries.Add(SubFolder.Name + "\\");
			allEntries.AddRange(from f in SubFolder.EnumerateFiles()
				select SubFolder.Name + "\\" + f.Name);
			allEntries.AddRange(from d in SubFolder.EnumerateDirectories().Where(IsValidDirectory)
				select SubFolder.Name + "\\" + d.Name + "\\");
		}
		ModBase.Log($"[Export] 共发现 {allEntries.Count} 个可行的二级文件/文件夹");
		foreach (MyCheckBox allOption in GetAllOptions(includeHidden: true))
		{
			ExportOption exportOption = GetExportOption(allOption);
			allOption.Inlines.Clear();
			allOption.Inlines.Add(new Run(exportOption.Title));
			if (!string.IsNullOrEmpty(exportOption.Description))
			{
				allOption.Inlines.Add(new Run("   " + exportOption.Description)
				{
					Foreground = ThemeManager.colorGray5
				});
			}
			if (string.IsNullOrEmpty(exportOption.Rules) && string.IsNullOrEmpty(exportOption.ShowRules))
			{
				allOption.Visibility = Visibility.Visible;
				allOption.Checked = exportOption.DefaultChecked;
			}
			else
			{
				bool flag = IsVisible(exportOption);
				allOption.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
				allOption.Checked = exportOption.DefaultChecked & flag;
			}
		}
		bool IsValidDirectory(DirectoryInfo folder)
		{
			try
			{
				return folder.Exists && folder.EnumerateFileSystemInfos().Any((FileSystemInfo i) => !subOptionBlackList.Any((string b) => i.Name.ContainsF(b)));
			}
			catch
			{
				return false;
			}
		}
		bool IsVisible(ExportOption targetOption)
		{
			if (targetOption.RequireOptiFine && !PageInstanceLeft.McInstance.Info.HasOptiFine)
			{
				return false;
			}
			if (targetOption.RequireModLoader && !PageInstanceLeft.McInstance.Modable)
			{
				return false;
			}
			if (targetOption.RequireModLoaderOrOptiFine && !PageInstanceLeft.McInstance.Info.HasOptiFine && !PageInstanceLeft.McInstance.Modable)
			{
				return false;
			}
			return StandardizeLines((targetOption.Rules ?? targetOption.ShowRules).Split('|'), addSuffixStarToFolderPath: true).Any((string rule) =>
			{
				if (rule.StartsWithF("!"))
				{
					return false;
				}
				try
				{
					if (allEntries.Any((string entry) => LikeString(entry, rule)))
					{
						return true;
					}
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "错误的规则：" + rule, ModBase.LogLevel.Hint, null, Lang.Text("Instance.Export.Error.OperationFailed"));
					return false;
				}
				rule = rule.Trim("*?".ToCharArray());
				return rule.Split(new char[1] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Count() >= 3 && (rule.EndsWithF("\\") ? IsValidDirectory(new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie + rule)) : File.Exists(PageInstanceLeft.McInstance.PathIndie + rule));
			});
		}
	}

	private IEnumerable<string> StandardizeLines(IEnumerable<string> raw, bool addSuffixStarToFolderPath)
	{
		foreach (string item in raw)
		{
			string current = item.Trim();
			if (!string.IsNullOrEmpty(current) && !current.StartsWithF("#") && !current.StartsWithF("="))
			{
				current = current.Replace("/", "\\");
				yield return current + ((current.EndsWithF("\\") & addSuffixStarToFolderPath) ? "**" : "");
			}
		}
	}

	private IEnumerable<MyCheckBox> GetAllOptions(bool includeHidden)
	{
		foreach (object child in PanOptions.Children)
		{
			if (!includeHidden && ((UIElement)child).Visibility != Visibility.Visible)
			{
				continue;
			}
			if (child is MyCheckBox)
			{
				yield return (MyCheckBox)child;
			}
			else
			{
				if (!(child is StackPanel))
				{
					continue;
				}
				foreach (object child2 in ((StackPanel)child).Children)
				{
					if ((includeHidden || ((UIElement)child2).Visibility == Visibility.Visible) && child2 is MyCheckBox)
					{
						yield return (MyCheckBox)child2;
					}
				}
			}
		}
	}

	private ExportOption GetExportOption(MyCheckBox checkBox)
	{
		return (ExportOption)checkBox.Tag;
	}

	private IEnumerable<string> GetAllRules()
	{
		if (RulesOverrides != null)
		{
			foreach (string rulesOverride in RulesOverrides)
			{
				yield return rulesOverride;
			}
			yield break;
		}
		yield return "";
		yield return "# " + Lang.Text("Instance.Export.Config.Comment.ModifyRules");
		yield return "# " + Lang.Text("Instance.Export.Config.Comment.ReverseMatch");
		yield return "";
		foreach (MyCheckBox allOption in GetAllOptions(includeHidden: false))
		{
			if (allOption.Checked == false)
			{
				continue;
			}
			ExportOption targetOption = GetExportOption(allOption);
			if (targetOption.Rules != null)
			{
				yield return "# " + targetOption.Title;
				string[] array = targetOption.Rules.Split('|');
				for (int i = 0; i < array.Length; i++)
				{
					yield return array[i];
				}
				yield return "";
			}
		}
		yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExcludedFiles");
		yield return "!*.log";
		yield return "!*.dat_old";
		yield return "!*.BakaCoreInfo";
		yield return "!hmclversion.cfg";
		yield return "!log4j2.xml";
		yield return "";
	}

	private IEnumerable<string> GetExtraFileLines()
	{
		if (extraFiles != null)
		{
			foreach (string extraFile in extraFiles)
			{
				yield return extraFile;
			}
		}
		else
		{
			yield return "";
			yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExtraFiles");
			yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExtraFiles2");
			yield return "";
		}
	}

	private void ResetConfigOverrides()
	{
		RulesOverrides = null;
		configPackPath = null;
		extraFiles = null;
		PanBack.ScrollToHome();
	}

	private void CardOptions_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (RulesOverrides != null)
		{
			ResetConfigOverrides();
		}
	}

	private void ExportConfig(object sender, MouseButtonEventArgs e)
	{
		try
		{
			string text = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Export.SelectFileLocation"), "export_config.txt", Lang.Text("Instance.Export.Config.FileFilter"), States.System.ExportConfigPath);
			if (!string.IsNullOrEmpty(text))
			{
				States.System.ExportConfigPath = text;
				List<string> list = new List<string>();
				list.Add("Name:" + TextExportName.Text);
				list.Add("Version:" + TextExportVersion.Text);
				list.Add("");
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.IncludeLauncher"));
				list.Add("IncludeLauncher:" + CheckOptionsPcl.Checked);
				list.Add("");
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.IncludeLauncherCustom"));
				list.Add("IncludeLauncherCustom:" + CheckOptionsPclCustom.Checked);
				list.Add("");
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.BundleFiles"));
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.BundleFiles2"));
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.BundleFiles3"));
				list.Add("DontCheckHostedAssets:" + CheckAdvancedInclude.Checked);
				list.Add("");
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.Modrinth"));
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.Modrinth2"));
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.Modrinth3"));
				list.Add("ModrinthUploadMode:" + CheckAdvancedModrinth.Checked);
				list.Add("");
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.PackPath"));
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.PackPath2"));
				list.Add("# " + Lang.Text("Instance.Export.Config.Comment.PackPath3"));
				list.Add("PackPath:" + configPackPath);
				list.Add("");
				list.Add("==============================================================");
				list.AddRange(GetAllRules());
				list.Add("==============================================================");
				list.AddRange(GetExtraFileLines());
				ModBase.WriteFile(text, list.Join("\r\n"));
				HintService.Hint(Lang.Text("Instance.Export.SaveSuccess", new object[1] { text }), HintType.Success);
				ModBase.OpenExplorer(text);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "保存配置失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Export.Error.OperationFailed"));
		}
	}

	private void ReadConfigFile(string configPath)
	{
		try
		{
			States.System.ExportConfigPath = configPath;
			string[] array = ModBase.ReadFile(configPath).Split("==============================================================");
			if (array.Length == 0)
			{
				HintService.Hint(Lang.Text("Instance.Export.Config.Invalid"), HintType.Error);
				return;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array2 = array[0].Split("\r\n".ToCharArray());
			for (int i = 0; i < array2.Length; i++)
			{
				string text = array2[i];
				text = text.Trim();
				if (!string.IsNullOrEmpty(text) && !text.StartsWithF("#") && !text.StartsWithF("="))
				{
					int num = text.IndexOfF(":");
					if (num > 0)
					{
						dictionary[text.Substring(0, num)] = text.Substring(num + 1);
					}
				}
			}
			TextExportName.Text = dictionary.GetOrDefault("Name", "");
			TextExportVersion.Text = dictionary.GetOrDefault("Version", "");
			CheckOptionsPcl.Checked = Convert.ToBoolean(dictionary.GetOrDefault("IncludeLauncher", false.ToString()));
			CheckOptionsPclCustom.Checked = Convert.ToBoolean(dictionary.GetOrDefault("IncludeLauncherCustom", true.ToString()));
			CheckAdvancedModrinth.Checked = Convert.ToBoolean(dictionary.GetOrDefault("ModrinthUploadMode", false.ToString()));
			CheckAdvancedInclude.Checked = Convert.ToBoolean(dictionary.GetOrDefault("DontCheckHostedAssets", false.ToString()));
			configPackPath = dictionary.GetOrDefault("PackPath");
			RulesOverrides = array[1].Replace("\r", "\n").Replace("\n\n", "\n").Split("\n")
				.ToList();
			if (array.Length > 2)
			{
				extraFiles = array[2].Replace("\r", "\n").Replace("\n\n", "\n").Split("\n")
					.ToList();
			}
			else
			{
				extraFiles = null;
			}
			HintService.Hint(Lang.Text("Instance.Export.ReadSuccess", new object[1] { configPath }), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "读取配置文件失败：" + configPath, ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Export.Error.OperationFailed"));
		}
	}

	private void ImportConfig(object sender, MouseButtonEventArgs e)
	{
		try
		{
			string text = SystemDialogs.SelectFile(Lang.Text("Instance.Export.Config.FileFilter"), Lang.Text("Instance.Export.SelectConfigFile"), States.System.ExportConfigPath);
			if (!string.IsNullOrEmpty(text))
			{
				ReadConfigFile(text);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "选择配置文件失败", ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Export.Error.OperationFailed"));
		}
	}

	private void PanAllBack_DragEnter(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (array.Length == 1 && array[0].EndsWithF(".txt", ignoreCase: true))
			{
				e.Effects = DragDropEffects.Copy;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}
		else
		{
			e.Effects = DragDropEffects.None;
		}
		e.Handled = true;
	}

	private void PanAllBack_Drop(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string configPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
			ReadConfigFile(configPath);
		}
		e.Handled = true;
	}

	private void StartExport(object sender, MouseButtonEventArgs e)
	{
		string packName = (string.IsNullOrEmpty(TextExportName.Text) ? TextExportName.HintText : TextExportName.Text);
		string packVersion = (string.IsNullOrEmpty(TextExportVersion.Text) ? "1.0.0" : TextExportVersion.Text);
		string loaderName = Lang.Text("Instance.Export.ExportTask.Prefix") + packName;
		foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar)
		{
			if (!((item.name ?? "") != (loaderName ?? "")))
			{
				ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
				return;
			}
		}
		string packPath = null;
		if (!string.IsNullOrWhiteSpace(configPackPath) && !configPackPath.EndsWithF("\\") && !configPackPath.EndsWithF("/"))
		{
			try
			{
				Directory.CreateDirectory(ModBase.GetPathFromFullPath(configPackPath));
				packPath = configPackPath;
				ModBase.Log("[Export] 使用配置文件中指定的导出路径：" + configPackPath);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "无法使用配置文件中指定的导出路径（" + configPackPath + "）");
				if (ModMain.MyMsgBox(Lang.Text("Instance.Export.PackPathInvalid.WithDetail", new object[2]
				{
					configPackPath,
					ex.ToString()
				}), Lang.Text("Instance.Export.PackPathInvalid.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) == 2)
				{
					return;
				}
			}
		}
		if (packPath == null)
		{
			List<string> list = new List<string>();
			if (CheckAdvancedModrinth.Checked == false)
			{
				list.Add(Lang.Text("Instance.Export.ZipFilter"));
			}
			if (CheckOptionsPcl.Checked == false)
			{
				list.Add(Lang.Text("Instance.Export.MrpackFilter"));
			}
			packPath = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Export.SelectSaveLocation"), packName + (string.IsNullOrEmpty(TextExportVersion.Text) ? "" : (" " + TextExportVersion.Text)), list.Join("|"), (string)null);
			ModBase.Log("[Export] 手动指定的导出路径：" + packPath);
		}
		if (string.IsNullOrEmpty(packPath))
		{
			return;
		}
		string cacheFolder = ModMain.RequestTaskTempFolder();
		string overridesFolder = Path.Combine(cacheFolder, "modpack", "overrides");
		McInstance mcInstance = PageInstanceLeft.McInstance;
		string pathIndie = mcInstance.PathIndie;
		bool checkHostedAssets = (!CheckAdvancedInclude.Checked).Value;
		bool modrinthUploadMode = CheckAdvancedModrinth.Checked.Value;
		bool includePCL = CheckOptionsPcl.Checked.Value;
		bool includePCLCustom = (includePCL ? CheckOptionsPclCustom.Checked : new bool?(false)).Value;
		List<string> allRules = StandardizeLines(GetAllRules(), addSuffixStarToFolderPath: true).ToList();
		List<string> allExtraFiles = StandardizeLines(GetExtraFileLines(), addSuffixStarToFolderPath: false).ToList();
		ModBase.Log($"[Export] 准备导出整合包，共有 {allRules.Count} 条规则，{allExtraFiles.Count} 条追加内容行");
		List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
		list2.Add(new ModLoader.LoaderTask<int, List<ModLocalComp.LocalCompFile>>(Lang.Text("Instance.Export.Task.CopyContent"), (ModLoader.LoaderTask<int, List<ModLocalComp.LocalCompFile>> loader) =>
		{
			loader.output = new List<ModLocalComp.LocalCompFile>();
			int progress = 0;
			Action<DirectoryInfo> searchFolder = null;
			searchFolder = (DirectoryInfo folder) =>
			{
				foreach (DirectoryInfo item2 in folder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
				{
					if ((!((folder.FullName ?? "") == (pathIndie ?? "")) || !new ReadOnlySpan<string>(new string[3] { "assets", "versions", "libraries" }).Contains(item2.Name)) && !new ReadOnlySpan<string>(new string[5] { "structureCacheV1", ".fabric", ".git", "avatar-cache", "cosmetic-cache" }).Contains(item2.Name))
					{
						searchFolder(item2);
					}
				}
				foreach (FileInfo item3 in folder.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
				{
					string relativePath = item3.FullName.AfterFirst(pathIndie);
					bool flag = false;
					foreach (string item4 in allRules)
					{
						bool flag2 = item4.StartsWith("!");
						if (LikeString(relativePath, item4.TrimStart('!')))
						{
							flag = !flag2;
						}
					}
					if (flag)
					{
						string text = Path.Combine(overridesFolder, relativePath);
						ModBase.CopyFile(item3.FullName, text);
						if (checkHostedAssets && new ReadOnlySpan<string>(new string[5] { ".zip", ".rar", ".jar", ".disabled", ".old" }).Contains(item3.Extension.ToLower()) && new string[4] { "mods", "packs", "openloader", "resource" }.Any((string s) => relativePath.Contains(s)))
						{
							ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(text);
							_ = localCompFile.ModrinthHash;
							localCompFile.CurseForgeHash.ToString();
							loader.output.Add(localCompFile);
						}
						progress++;
						if (progress == 25)
						{
							loader.Progress += (0.94 - loader.Progress) * 0.012;
							progress = 0;
						}
					}
				}
			};
			searchFolder(new DirectoryInfo(pathIndie));
			ModBase.Log($"[Export] 复制 overrides 文件完成，有 {loader.output.Count} 个文件需要联网检查");
			loader.Progress = 0.95;
			string path = (includePCL ? cacheFolder : Path.Combine(cacheFolder, "modpack"));
			foreach (string item5 in allExtraFiles)
			{
				if (item5.EndsWithF("\\") || item5.EndsWithF("/"))
				{
					if (Directory.Exists(item5))
					{
						ModBase.CopyDirectory(item5, Path.Combine(path, ModBase.GetFolderNameFromPath(item5)) + "\\");
					}
					else
					{
						HintService.Hint(Lang.Text("Instance.Export.Config.FolderNotFound", new object[1] { item5 }), HintType.Error);
					}
				}
				else if (File.Exists(item5))
				{
					ModBase.CopyFile(item5, Path.Combine(path, ModBase.GetFileNameFromPath(item5)));
				}
				else
				{
					HintService.Hint(Lang.Text("Instance.Export.Config.FileNotFound", new object[1] { item5 }), HintType.Error);
				}
			}
			loader.Progress = 0.97;
			ModBase.CopyDirectory(Path.Combine(mcInstance.PathInstance, "PCL"), Path.Combine(overridesFolder, "PCL"));
			if (includePCL)
			{
				ModBase.CopyFile(Basics.ExecutablePath, Path.Combine(cacheFolder, Basics.ExecutableName));
			}
			if (includePCLCustom)
			{
				if (Directory.Exists(Path.Combine(ModBase.exePath, "PCL", "Pictures")))
				{
					ModBase.CopyDirectory(Path.Combine(ModBase.exePath, "PCL", "Pictures"), Path.Combine(cacheFolder, "PCL", "Pictures"));
				}
				if (Directory.Exists(Path.Combine(ModBase.exePath, "PCL", "Musics")))
				{
					ModBase.CopyDirectory(Path.Combine(ModBase.exePath, "PCL", "Musics"), Path.Combine(cacheFolder, "PCL", "Musics"));
				}
				if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "Custom.xaml")))
				{
					ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "Custom.xaml"), Path.Combine(cacheFolder, "PCL", "Custom.xaml"));
				}
				if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "Setup.ini")))
				{
					ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "Setup.ini"), Path.Combine(cacheFolder, "PCL", "Setup.ini"));
				}
				if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "hints.txt")))
				{
					ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "hints.txt"), Path.Combine(cacheFolder, "PCL", "hints.txt"));
				}
				if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "Logo.png")))
				{
					ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "Logo.png"), Path.Combine(cacheFolder, "PCL", "Logo.png"));
				}
			}
		})
		{
			ProgressWeight = 5.0
		});
		list2.Add(new ModLoader.LoaderTask<List<ModLocalComp.LocalCompFile>, Dictionary<ModLocalComp.LocalCompFile, List<string>>>(Lang.Text("Instance.Export.Task.FetchFileInfo"), (ModLoader.LoaderTask<List<ModLocalComp.LocalCompFile>, Dictionary<ModLocalComp.LocalCompFile, List<string>>> loader) =>
		{
			loader.output = new Dictionary<ModLocalComp.LocalCompFile, List<string>>();
			if (!checkHostedAssets)
			{
				ModBase.Log("[Export] 要求跳过联网获取步骤");
			}
			else if (!loader.input.Any())
			{
				ModBase.Log("[Export] 没有需要联网检查的文件，跳过联网获取步骤");
			}
			else
			{
				int endedThreadCount = 0;
				List<Exception> failedExceptions = new List<Exception>();
				ModBase.RunInNewThread(() =>
				{
					try
					{
						IEnumerable<string> list3 = loader.input.Select((ModLocalComp.LocalCompFile m) => m.ModrinthHash);
						JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.modrinth.com/v2/version_files", "POST", "{\"hashes\": [\"" + list3.Join("\",\"") + "\"], \"algorithm\": \"sha1\"}", "application/json"));
						foreach (ModLocalComp.LocalCompFile item6 in loader.input)
						{
							if (jsonObject.ContainsKey(item6.ModrinthHash) && !((string?)jsonObject[item6.ModrinthHash]?["files"]?[0]["hashes"]?["sha1"] != item6.ModrinthHash))
							{
								loader.output.AddToList(item6, (string?)jsonObject[item6.ModrinthHash]["files"][0]["url"]);
							}
						}
						ModBase.Log($"[Export] 从 Modrinth 获取到 {jsonObject.Count} 个本地资源项的对应信息");
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "从 Modrinth 获取本地 Mod 信息失败");
						failedExceptions.Add(ex2);
					}
					finally
					{
						endedThreadCount++;
						loader.Progress += 0.45;
					}
				}, "Modrinth - " + loaderName);
				ModBase.RunInNewThread(() =>
				{
					try
					{
						if (!modrinthUploadMode)
						{
							IEnumerable<uint> list3 = loader.input.Select((ModLocalComp.LocalCompFile m) => m.CurseForgeHash);
							JsonNode jsonNode = ((JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.curseforge.com/v1/fingerprints/432/", "POST", "{\"fingerprints\": [" + list3.Join(",") + "]}", "application/json")))["data"]["exactMatches"];
							foreach (JsonObject item7 in jsonNode.AsArray())
							{
								if (item7.ContainsKey("file"))
								{
									JsonObject file = (JsonObject)item7["file"];
									if (!string.IsNullOrEmpty((string?)file["downloadUrl"]))
									{
										ModLocalComp.LocalCompFile localCompFile = loader.input.FirstOrDefault((ModLocalComp.LocalCompFile m) => m.CurseForgeHash == JsonCompat.ToObject<uint>(file["fileFingerprint"]));
										if (localCompFile != null)
										{
											loader.output.AddToList(localCompFile, ModComp.CompFile.HandleCurseForgeDownloadUrls(file["downloadUrl"].ToString()));
										}
									}
								}
							}
							ModBase.Log($"[Export] 从 CurseForge 获取到 {jsonNode.AsArray().Count} 个本地资源项的对应信息");
						}
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "从 CurseForge 获取本地 Mod 信息失败");
						failedExceptions.Add(ex2);
					}
					finally
					{
						endedThreadCount++;
						loader.Progress += 0.45;
					}
				}, "CurseForge - " + loaderName);
				while (endedThreadCount != 2)
				{
					if (loader.IsAborted)
					{
						return;
					}
					Thread.Sleep(10);
				}
				if (failedExceptions.Count == 1)
				{
					if (ModMain.MyMsgBox(Lang.Text("Instance.Export.NetCheckPartialFailed.Message"), Lang.Text("Instance.Export.NetCheckPartialFailed.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) == 2)
					{
						throw failedExceptions.First();
					}
				}
				else if (failedExceptions.Count > 1 && ModMain.MyMsgBox(Lang.Text("Instance.Export.NetCheckAllFailed.Message"), Lang.Text("Instance.Export.NetCheckAllFailed.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) == 2)
				{
					throw failedExceptions.First();
				}
			}
		})
		{
			show = checkHostedAssets,
			ProgressWeight = (checkHostedAssets ? 2.0 : 0.01)
		});
		list2.Add(new ModLoader.LoaderTask<Dictionary<ModLocalComp.LocalCompFile, List<string>>, int>(Lang.Text("Instance.Export.Task.CreateArchive"), (ModLoader.LoaderTask<Dictionary<ModLocalComp.LocalCompFile, List<string>>, int> loader) =>
		{
			JsonArray jsonArray = new JsonArray();
			foreach (KeyValuePair<ModLocalComp.LocalCompFile, List<string>> item8 in loader.input)
			{
				ModLocalComp.LocalCompFile key = item8.Key;
				jsonArray.Add(new JsonObject
				{
					{
						"path",
						Path.GetRelativePath(overridesFolder, key.path).Replace("\\", "/")
					},
					{
						"hashes",
						new JsonObject
						{
							{ "sha1", key.ModrinthHash },
							{
								"sha512",
								ModBase.GetFileSHA512(key.path)
							}
						}
					},
					{
						"downloads",
						new JsonArray(((IEnumerable<string>)item8.Value.OrderByDescending((string u) => u.Contains("modrinth.com"))).Select((Func<string, JsonNode>)((string s) => s)).ToArray())
					},
					{
						"fileSize",
						(JsonNode)new FileInfo(key.path).Length
					}
				});
				File.Delete(key.path);
			}
			loader.Progress = 0.2;
			JsonObject jsonObject = new JsonObject { 
			{
				"minecraft",
				mcInstance.Info.VanillaName
			} };
			if (mcInstance.Info.HasForge)
			{
				jsonObject.Add("forge", mcInstance.Info.Forge);
			}
			if (mcInstance.Info.HasFabric)
			{
				jsonObject.Add("fabric-loader", mcInstance.Info.Fabric);
			}
			if (mcInstance.Info.HasNeoForge)
			{
				jsonObject.Add("neoforge", mcInstance.Info.NeoForge);
			}
			JsonObject jsonObject2 = new JsonObject
			{
				{ "game", "minecraft" },
				{
					"formatVersion",
					(JsonNode)1
				},
				{ "versionId", packVersion },
				{ "name", packName },
				{ "summary", mcInstance.Desc },
				{ "files", jsonArray },
				{ "dependencies", jsonObject }
			};
			File.WriteAllText(Path.Combine(cacheFolder, "modpack", "modrinth.index.json"), jsonObject2.ToJsonString(new JsonSerializerOptions(JsonCompat.SerializerOptions)
			{
				WriteIndented = true
			}));
			Directory.CreateDirectory(ModBase.GetPathFromFullPath(packPath));
			if (File.Exists(packPath))
			{
				File.Delete(packPath);
			}
			if (includePCL)
			{
				ZipFile.CreateFromDirectory(Path.Combine(cacheFolder, "modpack"), Path.Combine(cacheFolder, "modpack.mrpack"));
				loader.Progress = 0.5;
				Directory.Delete(Path.Combine(cacheFolder, "modpack"), recursive: true);
				loader.Progress = 0.6;
				ZipFile.CreateFromDirectory(cacheFolder, packPath);
				loader.Progress = 0.9;
			}
			else
			{
				ZipFile.CreateFromDirectory(Path.Combine(cacheFolder, "modpack"), packPath);
				loader.Progress = 0.8;
			}
			Directory.Delete(cacheFolder, recursive: true);
			ModBase.OpenExplorer(packPath);
		})
		{
			ProgressWeight = 6.0
		});
		ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(loaderName, list2);
		loaderCombo.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
		loaderCombo.Start();
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
		ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
	}

	private static bool LikeString(string input, string pattern)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected Obj, but got Unknown
		pattern = pattern.Replace("#", "[0-9]");
		GlobOptions val = new GlobOptions();
		val.Evaluation.CaseInsensitive = true;
		GlobOptions val2 = val;
		return Glob.Parse(pattern, val2).IsMatch(input);
	}

}
