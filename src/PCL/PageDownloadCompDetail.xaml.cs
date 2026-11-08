using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Minecraft.ResourceProject;
using PCL.Core.UI;
using PCL.Core.Utils.Validate;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public partial class PageDownloadCompDetail : MyPageRight, IComponentConnector
{
	private class CardSorter : IComparer<string>
	{
		public readonly string topmost = "";

		public CardSorter(string topmost = "")
		{
			this.topmost = topmost ?? "";
		}

		public int Compare(string x, string y)
		{
			if ((x ?? "") == (y ?? ""))
			{
				return 0;
			}
			if ((x ?? "") == (topmost ?? ""))
			{
				return -1;
			}
			if ((y ?? "") == (topmost ?? ""))
			{
				return 1;
			}
			bool flag = !x.Contains(".");
			bool flag2 = !y.Contains(".");
			if (flag & flag2)
			{
				return string.Compare(x, y, StringComparison.Ordinal);
			}
			if (flag)
			{
				return 1;
			}
			if (flag2)
			{
				return -1;
			}
			int num = -McVersionComparer.CompareVersion(x.Replace(x.BeforeFirst(" ") + " ", ""), y.Replace(y.BeforeFirst(" ") + " ", ""));
			if (num != 0)
			{
				return num;
			}
			return -McVersionComparer.CompareVersion(x, y);
		}
	}

	public static Dictionary<ModComp.CompType, string> cachedFolder = new Dictionary<ModComp.CompType, string>();

	private MyCompItem _compItem;

	private bool _isFirstInit = true;

	private readonly ModLoader.LoaderTask<int, List<ModComp.CompFile>> _compFileLoader;

	private ModComp.CompProject _project;

	private string _targetInstance;

	private ModComp.CompLoaderType _targetLoader;

	private ModComp.CompType _pageType;

	private string? _instanceFilter;

	private string? _modLoaderFilter;

	private bool groupedDrop;

	private bool groupedOld;

	private void Init()
	{
		ModAnimation.AniControlEnabled++;
		_project = ModMain.frmMain.pageCurrent.additional.Value.CompProject;
		PanBack.ScrollToHome();
		if (_isFirstInit)
		{
			_isFirstInit = false;
		}
		else
		{
			PageLoaderRestart();
		}
		if (_compItem != null)
		{
			PanIntro.Children.Remove(_compItem);
		}
		_compItem = _project.ToCompItem(showMcVersionDesc: true, showLoaderDesc: true);
		_compItem.CanInteraction = false;
		_compItem.ShowFavoriteBtn = false;
		_compItem.Margin = new Thickness(-7.0, -7.0, 0.0, 8.0);
		PanIntro.Children.Insert(0, _compItem);
		BtnIntroWeb.Text = (_project.FromCurseForge ? "CurseForge" : "Modrinth");
		BtnIntroWiki.Visibility = ((!Lang.IsChineseMainland || _project.WikiId == 0) ? Visibility.Collapsed : Visibility.Visible);
		BtnTranslate.Visibility = ((!Lang.IsChineseMainland) ? Visibility.Collapsed : Visibility.Visible);
		RefreshFavoriteButton();
		ModAnimation.AniControlEnabled--;
	}

	public void Install_Click(MyListItem sender, EventArgs e)
	{
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Expected Obj, but got Unknown
		if (!RestrictedFeaturePolicy.Check("online-modpack-install"))
		{
			return;
		}
		try
		{
			ModComp.CompFile file = (ModComp.CompFile)sender.Tag;
			string name = $"{(_project.FromCurseForge ? "CurseForge" : "Modrinth")} {Lang.Text("Download.Comp.Detail.ModpackDownload")}：{_project.TranslatedName} ";
			string text = _project.TranslatedName.Replace(".zip", "").Replace(".rar", "").Replace(".mrpack", "")
				.Replace("\\", "＼")
				.Replace("/", "／")
				.Replace("|", "｜")
				.Replace(":", "：")
				.Replace("<", "＜")
				.Replace(">", "＞")
				.Replace("*", "＊")
				.Replace("?", "？")
				.Replace("\"", "")
				.Replace("： ", "：");
			FolderNameValidator val = new FolderNameValidator(ModFolder.mcFolderSelected + "versions", true, true, false);
			if (!((AbstractValidator<string>)(object)val).Validate(text).IsValid)
			{
				text = "";
			}
			string instanceName = ModMain.MyMsgBoxInput(Lang.Text("Download.Comp.Detail.InputInstanceName"), "", text, new Collection<IValidator<string>> { (IValidator<string>)(object)val });
			if (string.IsNullOrEmpty(instanceName))
			{
				return;
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			string target = $"{ModFolder.mcFolderSelected}versions\\{instanceName}\\原始整合包.{(_project.FromCurseForge ? "zip" : "mrpack")}";
			string logoFileAddress = MyImage.GetTempPath(_compItem.Logo);
			list.Add(new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadModpackFile"), new List<DownloadFile> { file.ToNetFile(target, ModComp.DownloadReason.ModPack) })
			{
				ProgressWeight = 10.0,
				block = true
			});
			list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Download.Comp.Detail.PrepareModpackInstall"), (ModLoader.LoaderTask<int, int> _) =>
			{
				ModModpack.ModpackInstall(target, instanceName, File.Exists(logoFileAddress) ? logoFileAddress : null, file.ProjectId, isOnlineInstall: true);
			})
			{
				ProgressWeight = 0.1
			});
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(name, list);
			loaderCombo.OnStateChanged = (ModLoader.LoaderBase myLoader) =>
			{
				switch (myLoader.State)
				{
				case ModBase.LoadState.Failed:
					HintService.Hint(Lang.Text("Download.Comp.Detail.Task.Failed", new object[2]
					{
						myLoader.name,
						myLoader.Error.ToString()
					}), HintType.Error);
					break;
				case ModBase.LoadState.Aborted:
					HintService.Hint(Lang.Text("Download.Comp.Detail.Task.Cancelled", new object[1] { myLoader.name }));
					break;
				case ModBase.LoadState.Loading:
					return;
				}
				ModDownloadLib.McInstallFailedClearFolder(myLoader);
			};
			loaderCombo.Start(Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName));
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "下载资源整合包失败", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Comp.Error.OperationFailed"));
		}
	}

	public void InstallWorld_Click(MyListItem sender, EventArgs e)
	{
		try
		{
			ModComp.CompFile file = (ModComp.CompFile)sender.Tag;
			string name = $"{(_project.FromCurseForge ? "CurseForge" : "Modrinth")} {Lang.Text("Download.Comp.Detail.WorldDownload")}：{_project.TranslatedName} ";
			string text = null;
			string subFolder = "saves\\";
			Func<McInstance, bool> isVersionSuitable = null;
			List<ModComp.CompLoaderType> allowedLoaders = new List<ModComp.CompLoaderType>();
			if (file.ModLoaders.Any())
			{
				allowedLoaders = file.ModLoaders;
			}
			else if (_project.ModLoaders.Any())
			{
				allowedLoaders = _project.ModLoaders;
			}
			ModBase.Log("[Comp] 世界要求的加载器种类：" + (allowedLoaders.Any() ? allowedLoaders.Join(" / ") : "无要求"));
			isVersionSuitable = (McInstance version) =>
			{
				if ((object)version == null)
				{
					return false;
				}
				if (!version.IsLoaded)
				{
					version.Load();
				}
				if (file.GameVersions.Any((string v) => v.Contains(".")) && !file.GameVersions.Any((string v) => v.Contains(".") && (v ?? "") == (version.Info.VanillaName ?? "")))
				{
					return false;
				}
				return !allowedLoaders.Any();
			};
			if (cachedFolder.ContainsKey(file.Type) && !string.IsNullOrEmpty(cachedFolder[file.Type]))
			{
				text = cachedFolder.GetOrDefault(file.Type, ModInstanceList.McMcInstanceSelected?.PathIndie ?? ModBase.exePath);
				ModBase.Log("[Comp] 使用上次下载时的文件夹作为默认下载位置：" + text);
			}
			else if ((object)ModInstanceList.McMcInstanceSelected != null && isVersionSuitable(ModInstanceList.McMcInstanceSelected))
			{
				text = ModInstanceList.McMcInstanceSelected.PathIndie + subFolder;
				Directory.CreateDirectory(text);
				ModBase.Log("[Comp] 使用当前实例作为默认下载位置：" + text);
			}
			else
			{
				bool flag = ModInstanceList.mcInstanceListLoader.State != ModBase.LoadState.Finished;
				if (flag)
				{
					HintService.Hint(Lang.Text("Download.Comp.Detail.FindingApplicableInstance"));
					ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\", waitForExit: true);
				}
				IEnumerable<DirectoryInfo> source = from v in ModInstanceList.mcInstanceList.Values.SelectMany((List<McInstance> l) => l)
					where isVersionSuitable(v)
					select new DirectoryInfo(v.PathIndie + subFolder);
				if (source.Any())
				{
					text = (from dir in source
						orderby (!dir.Exists) ? DateTime.MinValue : dir.LastWriteTimeUtc descending, (!dir.Exists) ? (-1) : dir.GetFiles().Length descending
						select dir).First().FullName;
					Directory.CreateDirectory(text);
					ModBase.Log("[Comp] 使用适合的游戏实例作为默认下载位置：" + text);
				}
				else
				{
					text = ModFolder.mcFolderSelected;
					if (flag)
					{
						HintService.Hint(Lang.Text("Download.Comp.Detail.NoApplicableInstance"));
					}
					else
					{
						ModBase.Log("[Comp] 由于当前实例不兼容，使用当前的 MC 文件夹作为默认下载位置");
					}
				}
			}
			string target = SystemDialogs.SelectSaveFile(Lang.Text("Download.Comp.Detail.SelectWorldInstallLocation"), file.FileName, Lang.Text("Download.Comp.Detail.WorldFile.Filter"), text);
			if (!string.IsNullOrEmpty(target))
			{
				List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
				string targetPath = target.BeforeLast("\\");
				MyImage.GetTempPath(_compItem.Logo);
				list.Add(new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadWorldFile"), new List<DownloadFile> { file.ToNetFile(target, ModComp.DownloadReason.Standalone, file.RawGameVersions.FirstOrDefault()) })
				{
					ProgressWeight = 10.0,
					block = true
				});
				list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Download.Comp.Detail.InstallWorld"), (ModLoader.LoaderTask<int, int> _) =>
				{
					ModBase.ExtractFile(target, targetPath, Encoding.UTF8);
				})
				{
					ProgressWeight = 0.1,
					block = true
				});
				list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Download.Comp.Detail.CleanCache"), (ModLoader.LoaderTask<int, int> _) =>
				{
					File.Delete(target);
				}));
				ModLoader.LoaderCombo<int> loaderCombo = new ModLoader.LoaderCombo<int>(name, list);
				loaderCombo.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
				loaderCombo.Start();
				ModLoader.LoaderTaskbarAdd(loaderCombo);
				ModMain.frmMain.BtnExtraDownload.ShowRefresh();
				ModMain.frmMain.BtnExtraDownload.Ribble();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "下载世界资源失败", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Comp.Error.OperationFailed"));
		}
	}

	public void Save_Click(object sender, EventArgs e)
	{
		if (!(sender is FrameworkElement frameworkElement))
		{
			goto IL_0088;
		}
		ModComp.CompFile compFile2;
		if (frameworkElement.Tag is ModComp.CompFile compFile)
		{
			compFile2 = compFile;
		}
		else if (frameworkElement.Parent is FrameworkElement { Tag: ModComp.CompFile tag })
		{
			compFile2 = tag;
		}
		else
		{
			if (!(frameworkElement.Parent is FrameworkElement { Parent: FrameworkElement { Tag: ModComp.CompFile tag2 } }))
			{
				goto IL_0088;
			}
			compFile2 = tag2;
		}
		goto IL_008b;
		IL_0088:
		compFile2 = null;
		goto IL_008b;
		IL_008b:
		ModComp.CompFile file = compFile2;
		ModBase.RunInNewThread(() =>
		{
			try
			{
				string desc = file.Type switch
				{
					ModComp.CompType.ModPack => Lang.Text("Download.Comp.Type.Modpack"), 
					ModComp.CompType.Mod => Lang.Text("Download.Comp.Type.Mod"), 
					ModComp.CompType.ResourcePack => Lang.Text("Download.Comp.Type.ResourcePack"), 
					ModComp.CompType.Shader => Lang.Text("Download.Comp.Type.Shader"), 
					ModComp.CompType.DataPack => Lang.Text("Download.Comp.Type.DataPack"), 
					ModComp.CompType.World => Lang.Text("Download.Comp.Type.World"), 
					_ => "", 
				};
				string defaultFolder = null;
				List<ModComp.CompLoaderType> allowedLoaders = new List<ModComp.CompLoaderType>();
				if (file.Type != ModComp.CompType.ModPack)
				{
					string subFolder = "";
					switch (file.Type)
					{
					case ModComp.CompType.Mod:
						subFolder = "mods\\";
						break;
					case ModComp.CompType.ResourcePack:
						subFolder = "resourcepacks\\";
						break;
					case ModComp.CompType.Shader:
						subFolder = "shaderpacks\\";
						break;
					case ModComp.CompType.World:
						subFolder = "saves\\";
						break;
					case ModComp.CompType.DataPack:
						subFolder = "";
						break;
					}
					if (file.ModLoaders.Any())
					{
						allowedLoaders = file.ModLoaders;
					}
					else if (_project.ModLoaders.Any())
					{
						allowedLoaders = _project.ModLoaders;
					}
					ModBase.Log("[Comp] " + desc + "要求的加载器种类：" + (allowedLoaders.Any() ? string.Join(" / ", allowedLoaders) : "无要求"));
					Func<McInstance, bool> isVersionSuitable = (McInstance version) =>
					{
						if ((object)version == null)
						{
							return false;
						}
						if (!version.IsLoaded)
						{
							version.Load();
						}
						if ((file.Type == ModComp.CompType.Mod || file.Type == ModComp.CompType.DataPack) && file.GameVersions.Any((string v) => v.Contains(".")) && !file.GameVersions.Any((string v) => v.Contains(".") && v == version.Info.VanillaName))
						{
							return false;
						}
						if (!allowedLoaders.Any())
						{
							return true;
						}
						if (allowedLoaders.Contains(ModComp.CompLoaderType.Forge) && version.Info.HasForge)
						{
							return true;
						}
						if (allowedLoaders.Contains(ModComp.CompLoaderType.Forge) && version.Info.HasCleanroom)
						{
							return true;
						}
						if (allowedLoaders.Contains(ModComp.CompLoaderType.Fabric) && (version.Info.HasFabric || version.Info.HasLegacyFabric))
						{
							return true;
						}
						if (allowedLoaders.Contains(ModComp.CompLoaderType.NeoForge) && version.Info.HasNeoForge)
						{
							return true;
						}
						return (allowedLoaders.Contains(ModComp.CompLoaderType.LiteLoader) && version.Info.HasLiteLoader) ? true : false;
					};
					if (cachedFolder.ContainsKey(file.Type) && !string.IsNullOrEmpty(cachedFolder[file.Type]))
					{
						defaultFolder = cachedFolder.GetOrDefault(file.Type, ModInstanceList.McMcInstanceSelected?.PathIndie ?? ModBase.exePath);
						ModBase.Log("[Comp] 使用上次下载时的文件夹作为默认下载位置：" + defaultFolder);
					}
					else if ((object)ModInstanceList.McMcInstanceSelected != null && isVersionSuitable(ModInstanceList.McMcInstanceSelected))
					{
						defaultFolder = ModInstanceList.McMcInstanceSelected.PathIndie + subFolder;
						Directory.CreateDirectory(defaultFolder);
						ModBase.Log("[Comp] 使用当前实例作为默认下载位置：" + defaultFolder);
					}
					else
					{
						bool flag = ModInstanceList.mcInstanceListLoader.State != ModBase.LoadState.Finished;
						if (flag)
						{
							HintService.Hint(Lang.Text("Download.Comp.Detail.FindingApplicableInstance"));
							ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\", waitForExit: true);
						}
						IEnumerable<DirectoryInfo> source = from v in ModInstanceList.mcInstanceList.Values.SelectMany((List<McInstance> l) => l)
							where isVersionSuitable(v)
							select new DirectoryInfo(v.PathIndie + subFolder);
						if (source.Any())
						{
							DirectoryInfo directoryInfo = (from dir in source
								orderby (!dir.Exists) ? DateTime.MinValue : dir.LastWriteTimeUtc descending, (!dir.Exists) ? (-1) : dir.GetFiles().Length descending
								select dir).First();
							defaultFolder = directoryInfo.FullName;
							Directory.CreateDirectory(defaultFolder);
							ModBase.Log("[Comp] 使用适合的游戏实例作为默认下载位置：" + defaultFolder);
						}
						else
						{
							defaultFolder = ModFolder.mcFolderSelected;
							if (flag)
							{
								HintService.Hint(Lang.Text("Download.Comp.Detail.NoApplicableInstance"));
							}
							else
							{
								ModBase.Log("[Comp] 由于当前实例不兼容，使用当前的 MC 文件夹作为默认下载位置");
							}
						}
					}
				}
				string fileName = ModComp.CompFileNameGet(_project, file);
				ModBase.RunInUi(() =>
				{
					//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
					//IL_03a9: Expected Obj, but got Unknown
					string text = Lang.Text("Download.Comp.Detail.SelectSaveLocation");
					string text2 = fileName;
					string text3 = Lang.Text("Download.Comp.Detail.ResourceFile.Filter", new object[1] { desc });
					string text4 = ((file.Type != ModComp.CompType.Mod) ? (file.FileName.EndsWith(".mrpack") ? "*.mrpack" : "*.zip") : (file.FileName.EndsWith(".litemod") ? "*.litemod" : "*.jar"));
					string text5 = SystemDialogs.SelectSaveFile(text, text2, text3 + "|" + text4, defaultFolder);
					if (text5.Contains("\\"))
					{
						string targetDir = ModBase.GetPathFromFullPath(text5);
						if (text5 != defaultFolder)
						{
							if (cachedFolder.ContainsKey(file.Type))
							{
								cachedFolder[file.Type] = targetDir;
							}
							else
							{
								cachedFolder.Add(file.Type, targetDir);
							}
						}
						if (file.Type == ModComp.CompType.Mod && Config.Download.Comp.AutoInstallDependencies && (file.Dependencies.Any() || (Config.Download.Comp.AutoInstallOptionalDependencies && file.OptionalDependencies.Any())))
						{
							try
							{
								McInstance mcInstance = null;
								List<McInstance> list = new List<McInstance>();
								if ((object)ModInstanceList.McMcInstanceSelected != null)
								{
									list.Add(ModInstanceList.McMcInstanceSelected);
								}
								list.AddRange(from instance in ModInstanceList.mcInstanceList.Values.SelectMany((List<McInstance> result2) => result2)
									where (object)instance != null
									select instance);
								mcInstance = list.Distinct().FirstOrDefault((McInstance instance) => targetDir.StartsWith(instance.PathIndie, StringComparison.OrdinalIgnoreCase));
								if ((object)mcInstance != null && !mcInstance.IsLoaded)
								{
									mcInstance.Load();
								}
								string targetMinecraftVersion = mcInstance?.Info?.VanillaName ?? file.GameVersions.FirstOrDefault((string version) => version.Contains(".")) ?? string.Empty;
								List<ModComp.CompLoaderType> list2 = new List<ModComp.CompLoaderType>();
								if ((object)mcInstance != null)
								{
									if (mcInstance.Info.HasForge || mcInstance.Info.HasCleanroom)
									{
										list2.Add(ModComp.CompLoaderType.Forge);
									}
									if (mcInstance.Info.HasFabric || mcInstance.Info.HasLegacyFabric)
									{
										list2.Add(ModComp.CompLoaderType.Fabric);
									}
									if (mcInstance.Info.HasQuilt)
									{
										list2.Add(ModComp.CompLoaderType.Quilt);
									}
									if (mcInstance.Info.HasNeoForge)
									{
										list2.Add(ModComp.CompLoaderType.NeoForge);
									}
									if (mcInstance.Info.HasLiteLoader)
									{
										list2.Add(ModComp.CompLoaderType.LiteLoader);
									}
								}
								if (!list2.Any())
								{
									list2 = allowedLoaders.ToList();
								}
								ModBase.Log($"[CompDeps] 开始解析必需前置: {file.Dependencies.Count} 个依赖");
								ModDependencyRequest val = ModCompDependency.BuildRequest(file, _project, targetMinecraftVersion, list2, targetDir);
								ModDependencyResolver val2 = new ModDependencyResolver();
								ModDependencyResolutionResult result = val2.Resolve(val);
								if (result.Unresolved.Any() || result.ToInstall.Any())
								{
									ModComp.CompDepsInstallTypes compDepsInstallTypes = ModCompDependency.ConfirmDependencyInstall(result);
									switch (compDepsInstallTypes)
									{
									case ModComp.CompDepsInstallTypes.Unresolved:
										ModBase.Log("[CompDeps] 发现无法解析的前置");
										DownloadDependencies();
										break;
									case ModComp.CompDepsInstallTypes.WithDeps:
										DownloadDependencies();
										break;
									case ModComp.CompDepsInstallTypes.WithoutDeps:
										ModBase.Log("[CompDeps] 用户选择仅下载 Mod 本体，跳过前置下载");
										break;
									case ModComp.CompDepsInstallTypes.Cancel:
										ModBase.Log("[CompDeps] 用户取消安装");
										return;
									default:
										ModBase.Log($"[CompDeps] 未知返回值: {compDepsInstallTypes} ，终止下载");
										return;
									}
								}
								else
								{
									ModBase.Log("[CompDeps] 已满足: 所有必需前置已安装");
								}
								void DownloadDependencies()
								{
									if (!result.ToInstall.Any())
									{
										ModBase.Log("[CompDeps] 所有前置均无法解析，仅下载 Mod 本体");
										return;
									}
									ModBase.Log($"[CompDeps] 准备下载: {result.ToInstall.Count} 个前置");
									foreach (var item3 in ModCompDependency.BuildDependencyDownloads(result, targetDir))
									{
										string item = item3.Filename;
										DownloadFile item2 = item3.File;
										string name2 = Lang.Text("Download.Comp.Detail.DownloadResource", new object[2]
										{
											desc,
											ModBase.GetFileNameWithoutExtentionFromPath(item)
										});
										List<ModLoader.LoaderBase> loaders2 = new List<ModLoader.LoaderBase>
										{
											new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadFile"), new List<DownloadFile> { item2 })
											{
												ProgressWeight = 6.0,
												block = true
											}
										};
										ModLoader.LoaderCombo<int> loaderCombo2 = new ModLoader.LoaderCombo<int>(name2, loaders2);
										loaderCombo2.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
										loaderCombo2.Start(1);
										ModLoader.LoaderTaskbarAdd(loaderCombo2);
									}
								}
							}
							catch (Exception ex2)
							{
								ModBase.Log(ex2, "[CompDeps] 依赖解析失败，跳过前置安装");
								ModMain.MyMsgBox(ExceptionDetails.Compose(Lang.Text("Download.Comp.Dependency.ResolveFailed.Message"), ex2), Lang.Text("Download.Comp.Dependency.ResolveFailed.Title"), Lang.Text("Download.Comp.Dependency.ResolveFailed.Continue"), "", "", isWarn: true, highLight: true, forceWait: true);
							}
						}
						string name = Lang.Text("Download.Comp.Detail.DownloadResource", new object[2]
						{
							desc,
							ModBase.GetFileNameWithoutExtentionFromPath(text5)
						});
						List<ModLoader.LoaderBase> loaders = new List<ModLoader.LoaderBase>
						{
							new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadFile"), new List<DownloadFile> { (file.Type == ModComp.CompType.Mod) ? file.ToNetFile(text5) : file.ToNetFile(text5, ModComp.DownloadReason.Standalone, null) })
							{
								ProgressWeight = 6.0,
								block = true
							}
						};
						ModLoader.LoaderCombo<int> loaderCombo = new ModLoader.LoaderCombo<int>(name, loaders);
						loaderCombo.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
						loaderCombo.Start(1);
						ModLoader.LoaderTaskbarAdd(loaderCombo);
						ModMain.frmMain.BtnExtraDownload.ShowRefresh();
						ModMain.frmMain.BtnExtraDownload.Ribble();
					}
				});
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "保存资源文件失败", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Comp.Error.OperationFailed"));
			}
		}, "Download CompDetail Save");
	}

	private void BtnIntroWeb_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite(_project.Website);
	}

	private void BtnIntroWiki_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite("https://www.mcmod.cn/class/" + _project.WikiId + ".html");
	}

	private void BtnIntroCopy_Click(object sender, EventArgs e)
	{
		ModBase.ClipboardSet(_compItem.LabTitle.Text + _compItem.LabTitleRaw.Text);
	}

	private void BtnFavorites_Click(object sender, EventArgs e)
	{
		ModComp.CompFavorites.ShowMenu(_project, (UIElement)sender, RefreshFavoriteButton);
	}

	private void BtnIntroLinkCopy_Click(object sender, EventArgs e)
	{
		ModComp.CompClipboard.currentText = _project.Website;
		ModBase.ClipboardSet(_project.Website);
	}

	private async void BtnTranslate_Click(object sender, EventArgs e)
	{
		HintService.Hint(Lang.Text("Download.Comp.Detail.DescriptionTranslating", new object[1] { _project.TranslatedName }));
		string text = await _project.ChineseDescription;
		if (text != null)
		{
			ModMain.MyMsgBox(Lang.Text("Download.Comp.Detail.DescriptionTranslationResult", new object[2] { _project.Description, text }));
		}
	}

	public void RefreshFavoriteButton()
	{
		try
		{
			if ((object)_project != null)
			{
				bool flag = ModComp.CompFavorites.IsFavourite(_project.Id);
				BtnFavorites.SvgIcon = (flag ? "lucide/heart-filled" : "lucide/heart");
				if (_compItem != null)
				{
					_compItem.RefreshFavoriteStatus();
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新收藏按钮状态时出错");
		}
	}

	public PageDownloadCompDetail()
	{
		_compFileLoader = new ModLoader.LoaderTask<int, List<ModComp.CompFile>>("Comp File", (ModLoader.LoaderTask<int, List<ModComp.CompFile>> task) =>
		{
			LoadTargetFromAdditional();
			List<ModComp.CompFile> output = ModComp.CompFilesGet(_project.Id, _project.FromCurseForge);
			if (!task.IsAborted)
			{
				task.output = output;
			}
		});
		Initialized += PageDownloadCompDetail_Inited;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			LoadTargetFromAdditional();
		};
		PageEnter += Init;
		InitializeComponent();
		Load.StateChanged += Load_State;
		BtnIntroWeb.Click += BtnIntroWeb_Click;
		BtnIntroWiki.Click += BtnIntroWiki_Click;
		BtnIntroCopy.Click += BtnIntroCopy_Click;
		BtnFavorites.Click += BtnFavorites_Click;
		BtnIntroLinkCopy.Click += BtnIntroLinkCopy_Click;
		BtnTranslate.Click += BtnTranslate_Click;
	}

	private void PageDownloadCompDetail_Inited(object sender, EventArgs e)
	{
		LoadTargetFromAdditional();
		PageLoaderInit(Load, PanLoad, PanMain, CardIntro, _compFileLoader, (ModLoader.LoaderBase _) =>
		{
			Load_OnFinish();
		});
	}

	public void LoadTargetFromAdditional()
	{
		(_project, _, _targetInstance, _targetLoader, _pageType, _) = ModMain.frmMain.pageCurrent.additional.Value;
	}

	private void Load_State(object sender, MyLoading.MyLoadingState state, MyLoading.MyLoadingState oldState)
	{
		if (_compFileLoader.State == ModBase.LoadState.Failed)
		{
			string text = "";
			if (_compFileLoader.Error != null)
			{
				text = _compFileLoader.Error.Message;
			}
			if (text.Contains(Lang.Text("Common.Error.InvalidJson")))
			{
				ModBase.Log("[Comp] 下载的文件 Json 列表损坏，已自动重试", ModBase.LogLevel.Debug);
				PageLoaderRestart();
			}
		}
	}

	private List<ModComp.CompFile> GetResults()
	{
		List<ModComp.CompFile> list = _compFileLoader.output;
		if (_pageType == ModComp.CompType.Any)
		{
			list = list.Where((ModComp.CompFile r) => r.Type != ModComp.CompType.Plugin).ToList();
		}
		else if (_pageType != ModComp.CompType.Shader && _pageType != ModComp.CompType.ResourcePack)
		{
			list = list.Where((ModComp.CompFile r) => r.Type == _pageType).ToList();
		}
		return list;
	}

	private void Load_OnFinish()
	{
		List<ModComp.CompFile> results = GetResults();
		List<string> instanceFilters = null;
		List<string> modLoaderFilters = null;
		groupedDrop = false;
		groupedOld = false;
		updateFilters();
		if (instanceFilters.Count >= 9)
		{
			groupedDrop = true;
			groupedOld = false;
			updateFilters();
			if (instanceFilters.Count >= 9)
			{
				groupedDrop = false;
				groupedOld = true;
				updateFilters();
				if (instanceFilters.Count >= 9)
				{
					groupedDrop = true;
					groupedOld = true;
					updateFilters();
				}
			}
		}
		PanInstanceFilter.Children.Clear();
		PanModLoaderFilter.Children.Clear();
		if (_pageType == ModComp.CompType.Mod)
		{
			PanInstanceFilter.Margin = new Thickness(10.0, 10.0, 0.0, 5.0);
			PanModLoaderFilter.Margin = new Thickness(10.0, 5.0, 0.0, 10.0);
		}
		else
		{
			PanInstanceFilter.Margin = new Thickness(10.0, 10.0, 0.0, 10.0);
			PanModLoaderFilter.Margin = new Thickness(0.0);
		}
		if (instanceFilters.Count < 2)
		{
			CardFilter.Visibility = Visibility.Collapsed;
			_instanceFilter = null;
		}
		else
		{
			CardFilter.Visibility = Visibility.Visible;
			if (_pageType == ModComp.CompType.Mod)
			{
				TextBlock element = new TextBlock
				{
					Text = Lang.Text("Download.Comp.Detail.InstanceFilter"),
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
				};
				PanInstanceFilter.Children.Add(element);
				TextBlock element2 = new TextBlock
				{
					Text = Lang.Text("Download.Comp.Detail.LoaderFilter"),
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(2.0, 0.0, 0.0, 0.0)
				};
				PanModLoaderFilter.Children.Add(element2);
			}
			instanceFilters.Insert(0, Lang.Text("Common.Option.All"));
			modLoaderFilters.Insert(0, Lang.Text("Common.Option.All"));
			foreach (string item in instanceFilters)
			{
				MyRadioButton myRadioButton = new MyRadioButton
				{
					Text = item,
					Margin = new Thickness(2.0, 0.0, 2.0, 0.0),
					ColorType = MyRadioButton.ColorState.Highlight
				};
				myRadioButton.LabText.Margin = new Thickness(-2.0, 0.0, 10.0, 0.0);
				myRadioButton.Check += (MyRadioButton sender, bool raiseByMouse) =>
				{
					_instanceFilter = ((sender.Text == Lang.Text("Common.Option.All")) ? null : sender.Text);
					UpdateFilterResult();
				};
				PanInstanceFilter.Children.Add(myRadioButton);
			}
			if (_pageType == ModComp.CompType.Mod)
			{
				foreach (string item2 in modLoaderFilters)
				{
					MyRadioButton myRadioButton2 = new MyRadioButton
					{
						Text = item2,
						Margin = new Thickness(2.0, 0.0, 2.0, 0.0),
						ColorType = MyRadioButton.ColorState.Highlight
					};
					myRadioButton2.LabText.Margin = new Thickness(-2.0, 0.0, 10.0, 0.0);
					myRadioButton2.Check += (MyRadioButton sender, bool raiseByMouse) =>
					{
						_modLoaderFilter = ((sender.Text == Lang.Text("Common.Option.All")) ? null : sender.Text);
						UpdateFilterResult();
					};
					PanModLoaderFilter.Children.Add(myRadioButton2);
				}
			}
			MyRadioButton myRadioButton3 = null;
			MyRadioButton myRadioButton4 = null;
			if (!string.IsNullOrEmpty(_targetInstance) && results.FirstOrDefault((ModComp.CompFile v) => v.GameVersions.Contains(_targetInstance)) != null)
			{
				string groupedVersionName = GetGroupedVersionName(_targetInstance, groupedDrop, groupedOld);
				IEnumerable<UIElement> enumerable2;
				if (_pageType != ModComp.CompType.Mod)
				{
					IEnumerable<UIElement> enumerable = PanInstanceFilter.Children.Cast<UIElement>();
					enumerable2 = enumerable;
				}
				else
				{
					enumerable2 = PanInstanceFilter.Children.Cast<UIElement>().Skip(1);
				}
				foreach (MyRadioButton item3 in (IEnumerable)enumerable2)
				{
					if (!((item3.Text ?? "") != (groupedVersionName ?? "")))
					{
						myRadioButton3 = item3;
						break;
					}
				}
			}
			if (_pageType == ModComp.CompType.Mod && _targetLoader != ModComp.CompLoaderType.Any && results.FirstOrDefault((ModComp.CompFile v) => v.ModLoaders.Contains(_targetLoader)) != null)
			{
				IEnumerable<UIElement> enumerable3;
				if (_pageType != ModComp.CompType.Mod)
				{
					IEnumerable<UIElement> enumerable = PanInstanceFilter.Children.Cast<UIElement>();
					enumerable3 = enumerable;
				}
				else
				{
					enumerable3 = PanInstanceFilter.Children.Cast<UIElement>().Skip(1);
				}
				foreach (MyRadioButton item4 in (IEnumerable)enumerable3)
				{
					if (!((item4.Text ?? "") != (_targetLoader.ToString() ?? "")))
					{
						myRadioButton4 = item4;
						break;
					}
				}
			}
			int index = ((_pageType == ModComp.CompType.Mod) ? 1 : 0);
			if (myRadioButton3 == null)
			{
				myRadioButton3 = (MyRadioButton)PanInstanceFilter.Children[index];
			}
			if (myRadioButton4 == null && _pageType == ModComp.CompType.Mod)
			{
				myRadioButton4 = (MyRadioButton)PanModLoaderFilter.Children[index];
			}
			myRadioButton3.Checked = true;
			if (_pageType == ModComp.CompType.Mod)
			{
				myRadioButton4.Checked = true;
			}
		}
		UpdateFilterResult();
		void updateFilters()
		{
			instanceFilters = (from v in results.SelectMany((ModComp.CompFile v) => v.GameVersions)
				select GetGroupedVersionName(v, groupedDrop, groupedOld)).Distinct().OrderByDescending((string s) => s, new McVersionComparer.VersionComparer()).ToList();
			modLoaderFilters = (from s in (from l in results.SelectMany((ModComp.CompFile v) => v.ModLoaders)
					select l.ToString()).Distinct()
				orderby s descending
				select s).ToList();
		}
	}

	private void UpdateFilterResult()
	{
		List<ModComp.CompFile> results = GetResults();
		if (results == null)
		{
			return;
		}
		string text = ((_targetLoader != ModComp.CompLoaderType.Any) ? (_targetLoader.ToString() + " ") : "");
		string text2 = ((!string.IsNullOrEmpty(_targetInstance) || _targetLoader != ModComp.CompLoaderType.Any) ? Lang.Text("Download.Comp.Detail.SelectedVersion", new object[2] { text, _targetInstance }) : "");
		HashSet<ModComp.CompLoaderType> hashSet = new HashSet<ModComp.CompLoaderType>(Enum.GetValues(typeof(ModComp.CompLoaderType)).Cast<ModComp.CompLoaderType>());
		bool ignoreQuilt = Config.Download.Comp.IgnoreQuilt;
		bool flag = _project.ModLoaders.Count > 1;
		SortedDictionary<string, List<ModComp.CompFile>> sortedDictionary = new SortedDictionary<string, List<ModComp.CompFile>>(new CardSorter(text2));
		sortedDictionary.Add(Lang.Text("Download.Comp.Detail.VersionGroup.Other"), new List<ModComp.CompFile>());
		Dictionary<string, HashSet<ModComp.CompFile>> checker = new Dictionary<string, HashSet<ModComp.CompFile>>();
		foreach (ModComp.CompFile item in results)
		{
			foreach (string gameVersion in item.GameVersions)
			{
				string groupedVersionName = GetGroupedVersionName(gameVersion, groupedDrop, groupedOld);
				if (_instanceFilter != null && (groupedVersionName ?? "") != (_instanceFilter ?? ""))
				{
					continue;
				}
				string groupedVersionName2 = GetGroupedVersionName(gameVersion, groupedByDrop: false, foldOld: false);
				List<string> list = new List<string>();
				if (flag && item.Type == ModComp.CompType.Mod && McInstanceInfo.IsFormatFit(groupedVersionName2))
				{
					foreach (ModComp.CompLoaderType modLoader in item.ModLoaders)
					{
						if (!((modLoader == ModComp.CompLoaderType.Quilt) & ignoreQuilt) && hashSet.Contains(modLoader) && (_modLoaderFilter == null || !((modLoader.ToString() ?? "") != (_modLoaderFilter ?? ""))))
						{
							list.Add(modLoader.ToString() + " ");
						}
					}
					if (list.Count == 0 && _modLoaderFilter != null)
					{
						continue;
					}
				}
				if (list.Count == 0)
				{
					list.Add("");
				}
				foreach (string item2 in list)
				{
					string key = item2 + groupedVersionName2;
					AddVersionToDict(sortedDictionary, checker, key, item);
				}
			}
			if (!string.IsNullOrEmpty(text2) && (_instanceFilter == null || GetGroupedVersionName(_targetInstance, groupedDrop, groupedOld).StartsWithF(_instanceFilter)) && item.GameVersions.Contains(_targetInstance) && (_targetLoader == ModComp.CompLoaderType.Any || item.ModLoaders.Contains(_targetLoader)) && (_instanceFilter == null || item.GameVersions.Any((string v) => (GetGroupedVersionName(v, groupedDrop, groupedOld) ?? "") == (_instanceFilter ?? ""))))
			{
				AddVersionToDict(sortedDictionary, checker, text2, item);
			}
		}
		try
		{
			PanResults.Children.Clear();
			(ModComp.CompProject, List<string>, string, ModComp.CompLoaderType, ModComp.CompType, string)? additional = ModMain.frmMain.pageCurrent.additional;
			List<string> list2 = (additional.HasValue ? additional.Value.Item2 : new List<string>());
			foreach (KeyValuePair<string, List<ModComp.CompFile>> item3 in sortedDictionary)
			{
				if (item3.Value.Count == 0)
				{
					continue;
				}
				MyCard myCard = new MyCard
				{
					Title = item3.Key,
					Margin = new Thickness(0.0, 0.0, 0.0, 15.0)
				};
				List<ModComp.CompFile> value = item3.Value;
				string key2 = item3.Key;
				StackPanel stackPanel = new StackPanel
				{
					Margin = new Thickness(20.0, 40.0, 18.0, 0.0),
					VerticalAlignment = VerticalAlignment.Top,
					Tag = value
				};
				myCard.Children.Add(stackPanel);
				myCard.SwapControl = stackPanel;
				myCard.InstallMethod = (StackPanel stack) =>
				{
					List<ModComp.CompFile> list3 = (List<ModComp.CompFile>)stack.Tag;
					list3.Sort((ModComp.CompFile a, ModComp.CompFile b) => b.ReleaseDate.CompareTo(a.ReleaseDate));
					bool badDisplayName = list3.Select((ModComp.CompFile f) => f.DisplayName).Distinct().Count() != list3.Count;
					switch (_project.Type)
					{
					case ModComp.CompType.ModPack:
					{
						foreach (ModComp.CompFile item4 in list3)
						{
							stack.Children.Add(item4.ToListItem((object sender, MouseButtonEventArgs e) =>
							{
								ModMain.frmDownloadCompDetail.Install_Click((MyListItem)sender, e);
							}, ModMain.frmDownloadCompDetail.Save_Click, badDisplayName));
						}
						break;
					}
					case ModComp.CompType.World:
					{
						foreach (ModComp.CompFile item5 in list3)
						{
							stack.Children.Add(item5.ToListItem((object sender, MouseButtonEventArgs e) =>
							{
								ModMain.frmDownloadCompDetail.InstallWorld_Click((MyListItem)sender, e);
							}, ModMain.frmDownloadCompDetail.Save_Click, badDisplayName));
						}
						break;
					}
					default:
						ModComp.CompFilesCardPreload(stack, list3);
						{
							foreach (ModComp.CompFile item6 in list3)
							{
								stack.Children.Add(item6.ToListItem(ModMain.frmDownloadCompDetail.Save_Click, null, badDisplayName));
							}
							break;
						}
					}
				};
				PanResults.Children.Add(myCard);
				if ((key2 ?? "") == (text2 ?? "") || list2.Contains(myCard.Title))
				{
					myCard.StackInstall();
				}
				else
				{
					myCard.IsSwapped = true;
				}
				if (key2 == Lang.Text("Download.Comp.Detail.VersionGroup.Other"))
				{
					stackPanel.Children.Add(new MyHint
					{
						Text = Lang.Text("Download.Comp.Detail.VersionRecognitionDelayHint"),
						Theme = MyHint.Themes.Yellow,
						Margin = new Thickness(5.0, 0.0, 0.0, 8.0)
					});
				}
			}
			if (PanResults.Children.Count == 1)
			{
				((MyCard)PanResults.Children[0]).IsSwapped = false;
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化工程下载列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Comp.Error.OperationFailed"));
		}
	}

	private void AddVersionToDict(SortedDictionary<string, List<ModComp.CompFile>> dict, Dictionary<string, HashSet<ModComp.CompFile>> checker, string key, ModComp.CompFile version)
	{
		if (!dict.ContainsKey(key))
		{
			dict.Add(key, new List<ModComp.CompFile>());
			checker.Add(key, new HashSet<ModComp.CompFile>());
		}
		if (checker[key].Add(version))
		{
			dict[key].Add(version);
		}
	}

	private string GetGroupedVersionName(string name, bool groupedByDrop, bool foldOld)
	{
		if (name == null)
		{
			return Lang.Text("Download.Comp.Detail.VersionGroup.Other");
		}
		if (name.Contains('w'))
		{
			return Lang.Text("Download.Comp.Detail.VersionGroup.Snapshot");
		}
		if (foldOld && McInstanceInfo.VersionToDrop(name, allowSnapshot: true) < 120)
		{
			return Lang.Text("Download.Comp.Detail.VersionGroup.Old");
		}
		if (groupedByDrop)
		{
			int num = McInstanceInfo.VersionToDrop(name, allowSnapshot: true);
			if (num >= 0)
			{
				return McInstanceInfo.DropToVersion(num);
			}
			return name;
		}
		return name;
	}

}
