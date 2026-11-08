using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.ResourceProject;
using PCL.Network;

namespace PCL;

public static class ModCompDependency
{
	public static ModDependencyRequest BuildRequest(ModComp.CompFile file, ModComp.CompProject project, string targetMinecraftVersion, List<ModComp.CompLoaderType> targetLoaders, string targetModsFolder)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected Obj, but got Unknown
		ArgumentNullException.ThrowIfNull(file, "file");
		ArgumentNullException.ThrowIfNull(project, "project");
		if (targetLoaders == null)
		{
			targetLoaders = new List<ModComp.CompLoaderType>();
		}
		string source = GetSource(project.FromCurseForge);
		List<ModDependencyReference> requiredDependencies = file.Dependencies.Where((string dependencyId) => !string.IsNullOrWhiteSpace(dependencyId)).Select((Func<string, ModDependencyReference>)((string dependencyId) =>
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected Obj, but got Unknown
			return new ModDependencyReference
			{
				ProjectId = dependencyId,
				Source = source,
				IsRequired = true
			};
		})).Concat(file.OptionalDependencies.Where((string dependencyId) => !string.IsNullOrWhiteSpace(dependencyId)).Select((Func<string, ModDependencyReference>)((string dependencyId) =>
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected Obj, but got Unknown
			return new ModDependencyReference
			{
				ProjectId = dependencyId,
				Source = source,
				IsRequired = false
			};
		})))
			.ToList();
		return new ModDependencyRequest
		{
			TargetMinecraftVersion = (targetMinecraftVersion ?? string.Empty),
			TargetLoaders = ToLoaderNames(targetLoaders),
			RequiredDependencies = requiredDependencies,
			IncludeOptionalDependencies = Config.Download.Comp.AutoInstallOptionalDependencies,
			InstalledMods = ScanInstalledMods(targetModsFolder),
			ProjectResolver = ResolveProjectFiles
		};
	}

	public static List<InstalledModIdentity> ScanInstalledMods(string targetModsFolder)
	{
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Expected Obj, but got Unknown
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Expected Obj, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected Obj, but got Unknown
		List<InstalledModIdentity> list = new List<InstalledModIdentity>();
		if (string.IsNullOrWhiteSpace(targetModsFolder) || !Directory.Exists(targetModsFolder))
		{
			return list;
		}
		string[] files = Directory.GetFiles(targetModsFolder);
		foreach (string path in files)
		{
			if (ModLocalComp.LocalCompFile.IsModFile(path))
			{
				ModLocalComp.LocalCompFile localCompFile = new ModLocalComp.LocalCompFile(path);
				localCompFile.Load();
				string text = (((object)localCompFile.Comp == null) ? null : GetSource(localCompFile.Comp.FromCurseForge));
				List<string> gameVersions = localCompFile.compFile?.GameVersions?.Where((string version) => !string.IsNullOrWhiteSpace(version)).ToList() ?? new List<string>();
				List<string> loaders = ToLoaderNames(localCompFile.compFile?.ModLoaders);
				if (!string.IsNullOrWhiteSpace(localCompFile.Comp?.Id) && !string.IsNullOrWhiteSpace(text))
				{
					list.Add(new InstalledModIdentity
					{
						SourceProjectId = localCompFile.Comp.Id,
						Source = text,
						ModId = localCompFile.ModId,
						GameVersions = gameVersions,
						Loaders = loaders
					});
				}
				else if (!string.IsNullOrWhiteSpace(localCompFile.compFile?.ProjectId))
				{
					string source = GetSource(localCompFile.compFile.FromCurseForge);
					list.Add(new InstalledModIdentity
					{
						SourceProjectId = localCompFile.compFile.ProjectId,
						Source = source,
						ModId = localCompFile.ModId,
						GameVersions = gameVersions,
						Loaders = loaders
					});
				}
				else
				{
					list.Add(new InstalledModIdentity
					{
						SourceProjectId = null,
						Source = null,
						ModId = localCompFile.ModId,
						GameVersions = gameVersions,
						Loaders = loaders
					});
				}
			}
		}
		return list;
	}

	public static ModDependencyProject? ResolveProjectFiles(string source, string projectId)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected Obj, but got Unknown
		if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(projectId))
		{
			return null;
		}
		bool flag = string.Equals(source, "CurseForge", StringComparison.OrdinalIgnoreCase);
		List<ModComp.CompFile> source2 = ModComp.CompFilesGet(projectId, flag);
		if (!ModComp.compProjectCache.TryGetValue(projectId, out ModComp.CompProject value))
		{
			return null;
		}
		if (value.FromCurseForge != flag)
		{
			return null;
		}
		return new ModDependencyProject
		{
			ProjectId = value.Id,
			Source = source,
			ProjectName = (value.TranslatedName ?? value.RawName),
			Files = ((IEnumerable<ModComp.CompFile>)source2).Select((Func<ModComp.CompFile, ModDependencyFile>)((ModComp.CompFile compFile) =>
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				//IL_0005: Unknown result type (might be due to invalid IL or missing references)
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0029: Unknown result type (might be due to invalid IL or missing references)
				//IL_006e: Unknown result type (might be due to invalid IL or missing references)
				//IL_007f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0090: Unknown result type (might be due to invalid IL or missing references)
				//IL_009c: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f6: Expected Obj, but got Unknown
				return new ModDependencyFile
				{
					Id = compFile.Id,
					DisplayName = compFile.DisplayName,
					Version = compFile.Version,
					GameVersions = (compFile.GameVersions?.Where((string version) => !string.IsNullOrWhiteSpace(version)).ToList() ?? new List<string>()),
					Loaders = ToLoaderNames(compFile.ModLoaders),
					ReleaseType = MapReleaseType(compFile.Status),
					ReleaseDate = compFile.ReleaseDate,
					RequiredDependencies = compFile.Dependencies.Where((string dependencyId) => !string.IsNullOrWhiteSpace(dependencyId)).Select((Func<string, ModDependencyReference>)((string dependencyId) =>
					{
						//IL_0000: Unknown result type (might be due to invalid IL or missing references)
						//IL_0005: Unknown result type (might be due to invalid IL or missing references)
						//IL_000c: Unknown result type (might be due to invalid IL or missing references)
						//IL_0018: Unknown result type (might be due to invalid IL or missing references)
						//IL_0020: Expected Obj, but got Unknown
						return new ModDependencyReference
						{
							ProjectId = dependencyId,
							Source = source,
							IsRequired = true
						};
					})).ToList()
				};
			})).ToList()
		};
	}

	public static ModDependencyFile? SelectCompatibleDependencyFile(ModDependencyResolutionResult result, string projectId, string source)
	{
		ArgumentNullException.ThrowIfNull(result, "result");
		ResolvedDependencyInstall? val = result.ToInstall.FirstOrDefault((ResolvedDependencyInstall install) => string.Equals(install.ProjectId, projectId, StringComparison.OrdinalIgnoreCase) && string.Equals(install.Source, source, StringComparison.OrdinalIgnoreCase));
		if (val == null)
		{
			return null;
		}
		return val.File;
	}

	public static List<(string Filename, DownloadFile File)> BuildDependencyDownloads(ModDependencyResolutionResult result, string targetModsFolder)
	{
		ArgumentNullException.ThrowIfNull(result, "result");
		List<(string, DownloadFile)> list = new List<(string, DownloadFile)>();
		foreach (ResolvedDependencyInstall install in result.ToInstall.AsEnumerable().Reverse())
		{
			if (!ModComp.compProjectCache.TryGetValue(install.ProjectId, out ModComp.CompProject value))
			{
				continue;
			}
			bool flag = string.Equals(install.Source, "CurseForge", StringComparison.OrdinalIgnoreCase);
			if (value.FromCurseForge == flag)
			{
				ModComp.CompFile compFile = ModComp.CompFilesGet(install.ProjectId, flag).FirstOrDefault((ModComp.CompFile file) => string.Equals(file.Id, install.File.Id, StringComparison.OrdinalIgnoreCase));
				if (compFile != null)
				{
					string text = ModComp.CompFileNameGet(value, compFile);
					string localAddress = Path.Combine(targetModsFolder ?? string.Empty, text);
					list.Add((text, compFile.ToNetFile(localAddress, ModComp.DownloadReason.Dependency)));
				}
			}
		}
		return list;
	}

	public static ModComp.CompDepsInstallTypes ConfirmDependencyInstall(ModDependencyResolutionResult result)
	{
		ArgumentNullException.ThrowIfNull(result, "result");
		List<UnresolvedDependency> unresolved = result.Unresolved;
		if (unresolved != null && unresolved.Count > 0)
		{
			ModBase.Log($"[CompDeps] 无法解析: {result.Unresolved.Count} 个必需前置");
			string text = string.Join(Environment.NewLine, result.Unresolved.Select((UnresolvedDependency dep) => Lang.Text("Download.Comp.Dependency.Unresolved.ListItem", new object[3] { dep.Source, dep.ProjectId, dep.Reason })));
			if (ModMain.MyMsgBox(Lang.Text("Download.Comp.Dependency.Unresolved.Message", new object[1] { text }), Lang.Text("Download.Comp.Dependency.Unresolved.Title"), Lang.Text("Download.Comp.Dependency.Unresolved.ContinueWithoutDependencies"), Lang.Text("Common.Action.Cancel"), "", isWarn: true, highLight: true, forceWait: true) != 1)
			{
				return ModComp.CompDepsInstallTypes.Cancel;
			}
			return ModComp.CompDepsInstallTypes.Unresolved;
		}
		List<ResolvedDependencyInstall> toInstall = result.ToInstall;
		if (toInstall != null && toInstall.Count > 0 && toInstall.All((ResolvedDependencyInstall install) => install.IsOptional))
		{
			ModBase.Log($"[CompDeps] 仅包含 {toInstall.Count} 个可选前置，按设置自动下载");
			return ModComp.CompDepsInstallTypes.WithDeps;
		}
		if (toInstall != null && toInstall.Count > 0)
		{
			string text2 = string.Join(Environment.NewLine, result.ToInstall.Select((ResolvedDependencyInstall install) => Lang.Text(install.IsOptional ? "Download.Comp.Dependency.Install.OptionalListItem" : "Download.Comp.Dependency.Install.ListItem", new object[4]
			{
				install.ProjectName,
				install.Source,
				install.File.DisplayName,
				install.File.Version
			})));
			return ModMain.MyMsgBox(Lang.Text("Download.Comp.Dependency.Install.Message", new object[1] { text2 }), Lang.Text("Download.Comp.Dependency.Install.Title"), Lang.Text("Download.Comp.Dependency.Install.WithDependencies"), Lang.Text("Download.Comp.Dependency.Install.WithoutDependencies"), Lang.Text("Download.Comp.Dependency.Install.Cancel"), isWarn: false, highLight: true, forceWait: true) switch
			{
				1 => ModComp.CompDepsInstallTypes.WithDeps, 
				2 => ModComp.CompDepsInstallTypes.WithoutDeps, 
				3 => ModComp.CompDepsInstallTypes.Cancel, 
				_ => ModComp.CompDepsInstallTypes.Cancel, 
			};
		}
		return ModComp.CompDepsInstallTypes.WithDeps;
	}

	public static void ShowDependencyAbortMessage(string reason)
	{
		ModMain.MyMsgBox(Lang.Text("Download.Comp.Dependency.Abort.Message", new object[1] { reason }), Lang.Text("Download.Comp.Dependency.Abort.Title"), Lang.Text("Common.Action.Confirm"), "", "", isWarn: false, highLight: true, forceWait: true);
	}

	private static string GetSource(bool fromCurseForge)
	{
		if (!fromCurseForge)
		{
			return "Modrinth";
		}
		return "CurseForge";
	}

	private static List<string> ToLoaderNames(IEnumerable<ModComp.CompLoaderType>? loaders)
	{
		if (loaders == null)
		{
			return new List<string>();
		}
		return (from loader in loaders
			where loader != ModComp.CompLoaderType.Any
			select loader.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static int MapReleaseType(ModComp.CompFileStatus status)
	{
		return status switch
		{
			ModComp.CompFileStatus.Release => 1, 
			ModComp.CompFileStatus.Beta => 2, 
			ModComp.CompFileStatus.Alpha => 3, 
			_ => 1, 
		};
	}
}
