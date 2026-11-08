using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.Minecraft.ResourceProject;

public sealed class ModDependencyResolver
{
	private sealed class ResolutionContext
	{
		private readonly HashSet<string> _installDedupe = new HashSet<string>(Comparer);

		private readonly HashSet<string> _unresolvedDedupe = new HashSet<string>(Comparer);

		private readonly HashSet<string> _satisfiedDedupe = new HashSet<string>(Comparer);

		public ModDependencyRequest Request { get; }

		public ModDependencyResolutionResult Result { get; }

		public HashSet<string> Visited { get; }

		public string TargetMinecraftVersion { get; }

		public HashSet<string> TargetLoaders { get; }

		private string LoaderSetKey { get; }

		public ResolutionContext(ModDependencyRequest request)
		{
			Request = request;
			Result = new ModDependencyResolutionResult();
			Visited = new HashSet<string>(Comparer);
			TargetMinecraftVersion = request.TargetMinecraftVersion ?? string.Empty;
			TargetLoaders = new HashSet<string>(request.TargetLoaders.Where((string loader) => !string.IsNullOrWhiteSpace(loader)), Comparer);
			LoaderSetKey = string.Join(",", TargetLoaders.OrderBy((string loader) => loader, Comparer));
		}

		public string GetVisitedKey(string projectId, string source)
		{
			return $"{source}:{projectId}:{TargetMinecraftVersion}:{LoaderSetKey}";
		}

		public bool IsInstalledCompatible(string projectId, string source)
		{
			return Request.InstalledMods.Any((InstalledModIdentity installed) => Comparer.Equals(installed.SourceProjectId, projectId) && Comparer.Equals(installed.Source, source) && installed.GameVersions.Any((string version) => Comparer.Equals(version, TargetMinecraftVersion)) && LoadersCompatible(installed.Loaders));
		}

		public void AddInstall(ModDependencyProject project, ModDependencyFile file, bool isOptional)
		{
			string projectKey = GetProjectKey(project.ProjectId, project.Source);
			if (_installDedupe.Add(projectKey))
			{
				Result.ToInstall.Add(new ResolvedDependencyInstall
				{
					ProjectId = project.ProjectId,
					Source = project.Source,
					ProjectName = project.ProjectName,
					File = file,
					IsOptional = isOptional
				});
			}
		}

		public void AddUnresolved(string projectId, string source, string reason)
		{
			string projectKey = GetProjectKey(projectId, source);
			if (_unresolvedDedupe.Add(projectKey))
			{
				Result.Unresolved.Add(new UnresolvedDependency
				{
					ProjectId = projectId,
					Source = source,
					Reason = reason
				});
			}
		}

		public void AddSatisfied(string projectId, string source, string reason)
		{
			string projectKey = GetProjectKey(projectId, source);
			if (_satisfiedDedupe.Add(projectKey))
			{
				Result.Satisfied.Add(new IgnoredDependency
				{
					ProjectId = projectId,
					Source = source,
					Reason = reason
				});
			}
		}

		private bool LoadersCompatible(List<string> installedLoaders)
		{
			if (TargetLoaders.Count == 0 || installedLoaders.Count == 0)
			{
				return true;
			}
			return installedLoaders.Any((string loader) => TargetLoaders.Contains(loader));
		}

		private static string GetProjectKey(string projectId, string source)
		{
			return source + ":" + projectId;
		}
	}

	private const int MaxDepth = 32;

	private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

	public ModDependencyResolutionResult Resolve(ModDependencyRequest request)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		ArgumentNullException.ThrowIfNull(request.ProjectResolver, "request.ProjectResolver");
		ResolutionContext resolutionContext = new ResolutionContext(request);
		foreach (ModDependencyReference requiredDependency in request.RequiredDependencies)
		{
			ResolveDependency(resolutionContext, requiredDependency, 0);
		}
		return resolutionContext.Result;
	}

	private static void ResolveDependency(ResolutionContext context, ModDependencyReference dependency, int depth)
	{
		if (string.IsNullOrWhiteSpace(dependency.ProjectId) || string.IsNullOrWhiteSpace(dependency.Source))
		{
			return;
		}
		if (!dependency.IsRequired && !context.Request.IncludeOptionalDependencies)
		{
			context.AddSatisfied(dependency.ProjectId, dependency.Source, "Optional dependency skipped.");
			return;
		}
		if (depth > 32)
		{
			if (dependency.IsRequired)
			{
				context.AddUnresolved(dependency.ProjectId, dependency.Source, "Maximum dependency depth exceeded.");
			}
			else
			{
				context.AddSatisfied(dependency.ProjectId, dependency.Source, "Optional dependency skipped: maximum depth exceeded.");
			}
			return;
		}
		string visitedKey = context.GetVisitedKey(dependency.ProjectId, dependency.Source);
		if (!context.Visited.Add(visitedKey))
		{
			return;
		}
		if (context.IsInstalledCompatible(dependency.ProjectId, dependency.Source))
		{
			context.AddSatisfied(dependency.ProjectId, dependency.Source, "Already installed and compatible.");
			return;
		}
		ModDependencyProject modDependencyProject = context.Request.ProjectResolver(dependency.Source, dependency.ProjectId);
		if ((object)modDependencyProject == null)
		{
			if (dependency.IsRequired)
			{
				context.AddUnresolved(dependency.ProjectId, dependency.Source, "Dependency project was not found.");
			}
			else
			{
				context.AddSatisfied(dependency.ProjectId, dependency.Source, "Optional dependency unavailable.");
			}
			return;
		}
		ModDependencyFile modDependencyFile = SelectBestFile(modDependencyProject.Files, context.TargetMinecraftVersion, context.TargetLoaders);
		if ((object)modDependencyFile == null)
		{
			if (dependency.IsRequired)
			{
				context.AddUnresolved(modDependencyProject.ProjectId, modDependencyProject.Source, "No compatible file was found.");
			}
			else
			{
				context.AddSatisfied(modDependencyProject.ProjectId, modDependencyProject.Source, "Optional dependency has no compatible file.");
			}
			return;
		}
		context.AddInstall(modDependencyProject, modDependencyFile, !dependency.IsRequired);
		foreach (ModDependencyReference requiredDependency in modDependencyFile.RequiredDependencies)
		{
			ResolveDependency(context, requiredDependency, depth + 1);
		}
	}

	private static ModDependencyFile? SelectBestFile(IEnumerable<ModDependencyFile> files, string targetMinecraftVersion, HashSet<string> targetLoaders)
	{
		return (from file in files
			where IsCompatibleFile(file, targetMinecraftVersion, targetLoaders)
			orderby HasExactGameVersionMatch(file, targetMinecraftVersion) descending, HasLoaderMatch(file, targetLoaders) descending, NormalizeReleaseType(file.ReleaseType), file.ReleaseDate descending
			select file).FirstOrDefault();
	}

	private static bool IsCompatibleFile(ModDependencyFile file, string targetMinecraftVersion, HashSet<string> targetLoaders)
	{
		if (!HasExactGameVersionMatch(file, targetMinecraftVersion))
		{
			return false;
		}
		if (targetLoaders.Count == 0)
		{
			return true;
		}
		if (file.Loaders.Count == 0)
		{
			return true;
		}
		return file.Loaders.Any((string loader) => targetLoaders.Contains(loader));
	}

	private static bool HasExactGameVersionMatch(ModDependencyFile file, string targetMinecraftVersion)
	{
		return file.GameVersions.Any((string version) => Comparer.Equals(version, targetMinecraftVersion));
	}

	private static bool HasLoaderMatch(ModDependencyFile file, HashSet<string> targetLoaders)
	{
		if (targetLoaders.Count == 0)
		{
			return true;
		}
		return file.Loaders.Any((string loader) => targetLoaders.Contains(loader));
	}

	private static int NormalizeReleaseType(int releaseType)
	{
		return releaseType switch
		{
			1 => 1, 
			2 => 2, 
			3 => 3, 
			_ => int.MaxValue, 
		};
	}
}
