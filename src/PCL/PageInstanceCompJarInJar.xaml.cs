using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceCompJarInJar : MyPageRight, IComponentConnector
{
	private ModJarInJarIndex _index;

	private string? _instanceMc;

	private string _search = "";

	private static readonly Brush _BrushOk = new SolidColorBrush(Color.FromRgb(76, 175, 80));

	private static readonly Brush _BrushWarn = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 152, 0));

	private static readonly Brush _BrushError = new SolidColorBrush(Color.FromRgb(229, 57, 53));

	public PageInstanceCompJarInJar()
	{
		InitializeComponent();
		BtnBack.Click += (object _, MouseButtonEventArgs _) =>
		{
			GoBack();
		};
		BtnEmptyBack.Click += (object _, MouseButtonEventArgs _) =>
		{
			GoBack();
		};
		BtnRefresh.Click += (object _, MouseButtonEventArgs _) =>
		{
			RefreshList();
		};
		SearchBox.TextChanged += (object _, EventArgs _) =>
		{
			_search = (SearchBox.Text ?? "").Trim();
			RefreshList();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			RefreshList();
		};
	}

	private bool _Match(ModLocalComp.LocalCompFile m)
	{
		if (_search.Length == 0)
		{
			return true;
		}
		if (!_Contains(_DisplayName(m)) && !_Contains(m.ModId))
		{
			return _Contains(m.FileName);
		}
		return true;
	}

	private bool _Contains(string? s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return false;
	}

	private bool _AnyEmbeddedMatch(List<ModLocalComp.LocalCompFile>? embedded)
	{
		return embedded?.Any((ModLocalComp.LocalCompFile e) => _Match(e) || _AnyEmbeddedMatch(e.EmbeddedMods)) ?? false;
	}

	private static void GoBack()
	{
		ModMain.frmInstanceLeft?.PageChange(FormMain.PageSubType.DownloadResourcePack);
	}

	public void OnJijResolved()
	{
		if (IsVisible)
		{
			RefreshList();
		}
	}

	private void RefreshList()
	{
		List<ModLocalComp.LocalCompFile> output = ModLocalComp.compResourceListLoader.output;
		if (!_OutputBelongsToCurrentInstance(output))
		{
			GoBack();
			return;
		}
		List<ModLocalComp.LocalCompFile> list = output.Where((ModLocalComp.LocalCompFile m) => !m.IsFolder && m.State != ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable).ToList();
		_instanceMc = PageInstanceLeft.McInstance?.Info?.VanillaName;
		_index = new ModJarInJarIndex(list, _instanceMc);
		PanLoad.Visibility = Visibility.Collapsed;
		PanWarnList.Children.Clear();
		foreach (ModLocalComp.LocalCompFile item in list)
		{
			if (item.State != ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
			{
				continue;
			}
			if (_Match(item))
			{
				_AppendMissingWarning(item, null, item.DetectedLoader);
			}
			foreach (ModLocalComp.LocalCompFile item2 in _index.GetLoadableEmbedded(item))
			{
				if (_Match(item2))
				{
					_AppendMissingWarning(item2, item, item2.JijLoader);
				}
			}
		}
		bool flag = PanWarnList.Children.Count > 0;
		CardWarnings.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		PanConflictList.Children.Clear();
		int num = 0;
		foreach (var (localCompFile, localCompFile2, hard) in _index.FindActiveConflicts())
		{
			if (_Match(localCompFile) || _Match(localCompFile2))
			{
				PanConflictList.Children.Add(_MakeConflictRow(localCompFile, localCompFile2, hard));
				num++;
			}
		}
		CardConflicts.Visibility = ((num <= 0) ? Visibility.Collapsed : Visibility.Visible);
		PanRelationList.Children.Clear();
		int num2 = 0;
		foreach (ModLocalComp.LocalCompFile item3 in list)
		{
			List<ModJarInJarIndex.DepRow> list2 = (from d in ModJarInJarIndex.BuildOwnDependencies(item3, item3.DetectedLoader)
				where !ModDependencyIds.IsPlatform(d.DepId)
				select d).ToList();
			if (list2.Count > 0 && (_Match(item3) || list2.Any((ModJarInJarIndex.DepRow d) => _Contains(d.DepId))))
			{
				PanRelationList.Children.Add(_MakeRelationCard(item3, null, list2));
				num2++;
			}
			foreach (ModLocalComp.LocalCompFile item4 in _index.GetLoadableEmbedded(item3))
			{
				List<ModJarInJarIndex.DepRow> list3 = (from d in ModJarInJarIndex.BuildOwnDependencies(item4, item4.JijLoader)
					where !ModDependencyIds.IsPlatform(d.DepId)
					select d).ToList();
				if (list3.Count != 0 && (_Match(item4) || list3.Any((ModJarInJarIndex.DepRow d) => _Contains(d.DepId))))
				{
					PanRelationList.Children.Add(_MakeRelationCard(item4, item3, list3));
					num2++;
				}
			}
		}
		SectionRelations.Visibility = ((num2 <= 0) ? Visibility.Collapsed : Visibility.Visible);
		PanBundledList.Children.Clear();
		List<ModLocalComp.LocalCompFile> list4 = list.Where((ModLocalComp.LocalCompFile m) =>
		{
			List<ModLocalComp.LocalCompFile> embeddedMods = m.EmbeddedMods;
			return embeddedMods != null && embeddedMods.Count > 0 && (_Match(m) || _AnyEmbeddedMatch(m.EmbeddedMods));
		}).ToList();
		foreach (ModLocalComp.LocalCompFile item5 in list4)
		{
			PanBundledList.Children.Add(_MakeCard(item5, _BuildBundledContent));
		}
		SectionBundled.Visibility = ((list4.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		PanEmpty.Visibility = ((flag || num > 0 || num2 > 0 || list4.Count > 0 || _search.Length > 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	private MyCard _MakeCard(ModLocalComp.LocalCompFile mod, Action<StackPanel, ModLocalComp.LocalCompFile> build, string? title = null)
	{
		MyCard myCard = new MyCard
		{
			Title = (title ?? _DisplayName(mod)),
			CanSwap = true,
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(20.0, 40.0, 18.0, 12.0),
			VerticalAlignment = VerticalAlignment.Top,
			RenderTransform = new TranslateTransform(0.0, 0.0),
			Tag = mod
		};
		myCard.Children.Add(stackPanel);
		myCard.SwapControl = stackPanel;
		myCard.InstallMethod = (StackPanel s) =>
		{
			build(s, mod);
		};
		myCard.IsSwapped = true;
		return myCard;
	}

	private void _AppendMissingWarning(ModLocalComp.LocalCompFile mod, ModLocalComp.LocalCompFile? parent, string? loader)
	{
		List<(ModJarInJarIndex.DepRow d, JijDepStatus status)> source = (from d in ModJarInJarIndex.BuildOwnDependencies(mod, loader)
			where !ModDependencyIds.IsPlatform(d.DepId) && !d.Optional
			select (d: d, status: _index.Analyze(mod, d))).ToList();
		List<string> list = (from x in source
			where x.status == JijDepStatus.Missing
			select x.d.DepId).ToList();
		List<string> list2 = (from x in source
			where x.status == JijDepStatus.VersionMismatch
			select Lang.Text("Instance.Resource.Mod.JarInJar.Warning.VersionMismatch.Detail", new object[3]
			{
				x.d.DepId,
				x.d.Raw ?? "*",
				string.Join(", ", _index.GetProviderVersions(mod, x.d.DepId))
			})).ToList();
		List<string> list3 = (from x in source
			where x.status == JijDepStatus.Disabled
			select x.d.DepId).ToList();
		if (list.Count != 0 || list2.Count != 0 || list3.Count != 0)
		{
			string text = ((parent == null) ? _DisplayName(mod) : (_DisplayName(mod) + " " + Lang.Text("Instance.Resource.Mod.JarInJar.Bundled.From", new object[1] { _DisplayName(parent) })));
			if (list.Count > 0)
			{
				PanWarnList.Children.Add(_Text(Lang.Text("Instance.Resource.Mod.JarInJar.Warning.Missing", new object[2]
				{
					text,
					string.Join(", ", list)
				}), _BrushError));
			}
			if (list2.Count > 0)
			{
				PanWarnList.Children.Add(_Text(Lang.Text("Instance.Resource.Mod.JarInJar.Warning.VersionMismatch", new object[2]
				{
					text,
					string.Join(", ", list2)
				}), _BrushWarn));
			}
			if (list3.Count > 0)
			{
				PanWarnList.Children.Add(_Text(Lang.Text("Instance.Resource.Mod.JarInJar.Warning.Disabled", new object[2]
				{
					text,
					string.Join(", ", list3)
				}), _BrushWarn));
			}
		}
	}

	private Panel _MakeConflictRow(ModLocalComp.LocalCompFile a, ModLocalComp.LocalCompFile b, bool hard)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
		};
		stackPanel.Children.Add(_Text(Lang.Text(hard ? "Instance.Resource.Mod.JarInJar.Conflict.Incompatible" : "Instance.Resource.Mod.JarInJar.Conflict.Discouraged"), hard ? _BrushError : _BrushWarn));
		stackPanel.Children.Add(_TextRes("  " + Lang.Text("Instance.Resource.Mod.JarInJar.Conflict.Pair", new object[2]
		{
			_DisplayName(a),
			_DisplayName(b)
		}), "ColorBrush1"));
		return stackPanel;
	}

	private MyCard _MakeRelationCard(ModLocalComp.LocalCompFile mod, ModLocalComp.LocalCompFile? parent, List<ModJarInJarIndex.DepRow> deps)
	{
		string title = ((parent == null) ? _DisplayName(mod) : (_DisplayName(mod) + "  " + Lang.Text("Instance.Resource.Mod.JarInJar.Bundled.From", new object[1] { _DisplayName(parent) })));
		return _MakeCard(mod, (StackPanel stack, ModLocalComp.LocalCompFile owner) =>
		{
			foreach (ModJarInJarIndex.DepRow dep in deps)
			{
				stack.Children.Add(BuildDependencyRow(owner, dep));
			}
		}, title);
	}

	private void _BuildBundledContent(StackPanel stack, ModLocalComp.LocalCompFile mod)
	{
		HashSet<ModLocalComp.LocalCompFile> selected = ((mod.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? _index.GetLoadableEmbedded(mod).ToHashSet() : new HashSet<ModLocalComp.LocalCompFile>());
		AppendTreeRows(stack, mod.EmbeddedMods, 0, selected);
	}

	private void AppendTreeRows(StackPanel stack, List<ModLocalComp.LocalCompFile> embedded, int depth, HashSet<ModLocalComp.LocalCompFile> selected)
	{
		if (embedded == null)
		{
			return;
		}
		List<ModLocalComp.LocalCompFile> list = embedded.Where((ModLocalComp.LocalCompFile e) => string.IsNullOrEmpty(e.ModId)).ToList();
		foreach (IGrouping<string, ModLocalComp.LocalCompFile> item in embedded.Where((ModLocalComp.LocalCompFile e) => !string.IsNullOrEmpty(e.ModId)).GroupBy((ModLocalComp.LocalCompFile e) => e.ModId, StringComparer.OrdinalIgnoreCase))
		{
			List<ModLocalComp.LocalCompFile> list2 = item.ToList();
			if (list2.Count == 1)
			{
				stack.Children.Add(BuildTreeRow(list2[0], depth, 0, McConstraintMatcher.MatchKind.None));
				AppendTreeRows(stack, list2[0].EmbeddedMods, depth + 1, selected);
				continue;
			}
			List<(ModLocalComp.LocalCompFile, McConstraintMatcher.MatchKind)> list3 = (from v in list2
				select (v: v, kind: McConstraintMatcher.Match(v.FileName, v.Version, v.JijTargetMcVersion, v.JijLoader, _instanceMc)) into x
				orderby (int)x.kind descending
				select x).ToList();
			ModLocalComp.LocalCompFile localCompFile = list2.FirstOrDefault(selected.Contains);
			ModLocalComp.LocalCompFile localCompFile2 = localCompFile ?? list3[0].Item1;
			McConstraintMatcher.MatchKind kind = ((localCompFile == null) ? list3[0].Item2 : McConstraintMatcher.Match(localCompFile2.FileName, localCompFile2.Version, localCompFile2.JijTargetMcVersion, localCompFile2.JijLoader, _instanceMc));
			string versionsTooltip = string.Join("\n", list2.Select((ModLocalComp.LocalCompFile v) => (!string.IsNullOrWhiteSpace(v.Version)) ? v.Version : v.FileName));
			stack.Children.Add(BuildTreeRow(localCompFile2, depth, list2.Count, kind, versionsTooltip, localCompFile != null));
			AppendTreeRows(stack, localCompFile2.EmbeddedMods, depth + 1, selected);
		}
		foreach (ModLocalComp.LocalCompFile item2 in list)
		{
			stack.Children.Add(BuildTreeRow(item2, depth, 0, McConstraintMatcher.MatchKind.None));
			AppendTreeRows(stack, item2.EmbeddedMods, depth + 1, selected);
		}
	}

	private Panel BuildTreeRow(ModLocalComp.LocalCompFile e, int depth, int versionCount, McConstraintMatcher.MatchKind kind, string? versionsTooltip = null, bool selected = false)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(8 + depth * 20, 2.0, 0.0, 2.0)
		};
		stackPanel.Children.Add(_TextRes("• " + _DisplayName(e), "ColorBrush1", bold: true));
		string text = _CleanPlaceholder(e.Version);
		if (!string.IsNullOrEmpty(text))
		{
			stackPanel.Children.Add(_TextRes("  (" + text + ")", "ColorBrush2"));
		}
		if (!string.IsNullOrEmpty(e.JijLoader))
		{
			stackPanel.Children.Add(_TextRes("  [" + e.JijLoader + "]", "ColorBrush3"));
		}
		string text2 = _CleanPlaceholder(e.JijTargetMcVersion);
		if (!string.IsNullOrEmpty(text2))
		{
			stackPanel.Children.Add(_TextRes("  (MC " + text2 + ")", "ColorBrushInfo"));
		}
		if (versionCount > 1)
		{
			TextBlock textBlock = _TextRes("  " + Lang.Text("Instance.Resource.Mod.JarInJar.Bundled.Versions", new object[1] { versionCount }), "ColorBrush2");
			if (!string.IsNullOrEmpty(versionsTooltip))
			{
				textBlock.ToolTip = versionsTooltip;
			}
			stackPanel.Children.Add(textBlock);
		}
		bool flag = selected;
		if (!flag)
		{
			bool flag2 = (uint)(kind - 3) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			stackPanel.Children.Add(_Text("  " + Lang.Text("Instance.Resource.Mod.JarInJar.Bundled.Current"), _BrushOk));
		}
		else if (kind == McConstraintMatcher.MatchKind.Incompatible)
		{
			stackPanel.Children.Add(_Text("  " + Lang.Text("Instance.Resource.Mod.JarInJar.Bundled.Incompatible"), _BrushWarn));
		}
		return stackPanel;
	}

	private Panel BuildDependencyRow(ModLocalComp.LocalCompFile mod, ModJarInJarIndex.DepRow dep)
	{
		JijDepStatus jijDepStatus = _index.Analyze(mod, dep);
		bool optional = dep.Optional;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(8.0, 2.0, 0.0, 2.0)
		};
		string text = ((dep.Raw == null) ? dep.DepId : (dep.DepId + " " + dep.Raw));
		stackPanel.Children.Add(_TextRes("• " + text, "ColorBrush1"));
		if (optional)
		{
			stackPanel.Children.Add(_TextRes("  " + Lang.Text("Instance.Resource.Mod.JarInJar.Dep.Optional"), "ColorBrush2"));
		}
		string text2 = "  (" + _StatusLabel(jijDepStatus) + ")";
		if (optional || jijDepStatus == JijDepStatus.Bundled)
		{
			stackPanel.Children.Add(_TextRes(text2, "ColorBrush2"));
		}
		else
		{
			stackPanel.Children.Add(_Text(text2, _StatusBrush(jijDepStatus)));
		}
		return stackPanel;
	}

	private static string _StatusLabel(JijDepStatus status)
	{
		return Lang.Text(status switch
		{
			JijDepStatus.Installed => "Instance.Resource.Mod.JarInJar.Dep.Installed", 
			JijDepStatus.Disabled => "Instance.Resource.Mod.JarInJar.Dep.Disabled", 
			JijDepStatus.Bundled => "Instance.Resource.Mod.JarInJar.Dep.Bundled", 
			JijDepStatus.VersionMismatch => "Instance.Resource.Mod.JarInJar.Dep.VersionMismatch", 
			_ => "Instance.Resource.Mod.JarInJar.Dep.Missing", 
		});
	}

	private static Brush _StatusBrush(JijDepStatus status)
	{
		return status switch
		{
			JijDepStatus.Installed => _BrushOk, 
			JijDepStatus.Disabled => _BrushWarn, 
			JijDepStatus.VersionMismatch => _BrushWarn, 
			_ => _BrushError, 
		};
	}

	private static bool _OutputBelongsToCurrentInstance(List<ModLocalComp.LocalCompFile> output)
	{
		McInstance mcInstance = PageInstanceLeft.McInstance;
		if ((object)mcInstance == null || output == null)
		{
			return false;
		}
		try
		{
			ModLocalComp.CompLocalLoaderData input = ModLocalComp.compResourceListLoader.input;
			if (input == null || input.compType != ModComp.CompType.Mod || !string.Equals(Path.GetFullPath(input.gameVersion.PathInstance).TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}), Path.GetFullPath(mcInstance.PathInstance).TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}), StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			ModLocalComp.LocalCompFile localCompFile = output.FirstOrDefault((ModLocalComp.LocalCompFile m) => !string.IsNullOrEmpty(m.path));
			if (localCompFile == null)
			{
				return true;
			}
			string value = Path.GetFullPath(mcInstance.PathIndie).TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}) + Path.DirectorySeparatorChar;
			return Path.GetFullPath(localCompFile.path).StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static string? _CleanPlaceholder(string? v)
	{
		if (!string.IsNullOrEmpty(v) && !v.Contains("${"))
		{
			return v;
		}
		return null;
	}

	private static string _DisplayName(ModLocalComp.LocalCompFile m)
	{
		if ((object)m.Comp != null)
		{
			KeyValuePair<string, string> controlTitle = m.Comp.GetControlTitle(hasModLoaderDescription: false);
			return controlTitle.Key + controlTitle.Value;
		}
		if (!string.IsNullOrWhiteSpace(m.Name))
		{
			return m.Name;
		}
		return m.FileName;
	}

	private static TextBlock _Text(string text, Brush brush, bool bold = false)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = brush,
			VerticalAlignment = VerticalAlignment.Center,
			FontWeight = (bold ? FontWeights.Bold : FontWeights.Normal),
			TextWrapping = TextWrapping.Wrap
		};
	}

	private static TextBlock _TextRes(string text, string resourceKey, bool bold = false)
	{
		TextBlock textBlock = new TextBlock();
		textBlock.Text = text;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.FontWeight = (bold ? FontWeights.Bold : FontWeights.Normal);
		textBlock.TextWrapping = TextWrapping.Wrap;
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, resourceKey);
		return textBlock;
	}

}
