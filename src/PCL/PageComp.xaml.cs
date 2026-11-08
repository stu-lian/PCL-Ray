using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;

namespace PCL;

[ContentProperty("SearchTags")]
public partial class PageComp : MyScrollViewer, IComponentConnector
{
	public const int pageSize = 40;

	public int page;

	public ModComp.CompProjectStorage storage = new ModComp.CompProjectStorage();

	public static readonly DependencyProperty SupportCurseForgeProperty = DependencyProperty.Register("SupportCurseForge", typeof(bool), typeof(PageComp), new PropertyMetadata((object)true));

	public static readonly DependencyProperty SupportModrinthProperty = DependencyProperty.Register("SupportModrinth", typeof(bool), typeof(PageComp), new PropertyMetadata((object)true));

	public static McInstance targetVersion;

	public ModLoader.LoaderTask<ModComp.CompProjectRequest, int> loader;

	private bool isLoaderInited;

	public ItemCollection SearchTags => ComboSearchTag.Items;

	public bool SupportCurseForge
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(SupportCurseForgeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(SupportCurseForgeProperty, (object)value);
		}
	}

	public bool SupportModrinth
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(SupportModrinthProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(SupportModrinthProperty, (object)value);
		}
	}

	public string TypeName => ModComp.GetCompTypeName(PageType);

	public string TypeNameSpaced => TypeName;

	public ModComp.CompType PageType
	{
		get
		{
			return field;
		}
		set
		{
			if (field != value)
			{
				field = value;
				BtnSearchInstallModPack.Visibility = ((value != ModComp.CompType.ModPack) ? Visibility.Collapsed : Visibility.Visible);
				loader.name = Lang.Text("Download.Comp.List.Source.ResourceFetch", new object[1] { TypeName });
				PanSearchBox.HintText = ModComp.GetCompSearchName(value);
				Load.Text = ModComp.GetCompLoadingName(value);
			}
		}
	} = ModComp.CompType.Any;

	private void Load_OnFinish()
	{
		try
		{
			ModBase.Log($"[Comp] 开始可视化{TypeNameSpaced}列表，已储藏 {storage.results.Count} 个结果，当前在第 {page + 1} 页");
			PanProjects.Children.Clear();
			int num = Math.Min(page * 40, storage.results.Count - 1);
			foreach (ModComp.CompProject item in storage.results.GetRange(num, Math.Min(storage.results.Count - num, 40)))
			{
				bool showQuickDownload = item.Type != ModComp.CompType.ModPack && item.Type != ModComp.CompType.DataPack;
				PanProjects.Children.Add(item.ToCompItem(loader.input.gameVersion == null, loader.input.modLoader == ModComp.CompLoaderType.Any && (PageType == ModComp.CompType.Mod || PageType == ModComp.CompType.ModPack), showQuickDownload));
			}
			CardPages.Visibility = ((storage.results.Count <= 40 && storage.curseForgeOffset >= storage.curseForgeTotal && storage.modrinthOffset >= storage.modrinthTotal) ? Visibility.Collapsed : Visibility.Visible);
			LabPage.Text = Lang.Number<int>(page + 1, "N0");
			BtnPageFirst.IsEnabled = page > 1;
			BtnPageFirst.Opacity = ((page > 1) ? 1.0 : 0.2);
			BtnPageLeft.IsEnabled = page > 0;
			BtnPageLeft.Opacity = ((page > 0) ? 1.0 : 0.2);
			bool flag = storage.results.Count > 40 * (page + 1) || storage.curseForgeOffset < storage.curseForgeTotal || storage.modrinthOffset < storage.modrinthTotal;
			BtnPageRight.IsEnabled = flag;
			BtnPageRight.Opacity = (flag ? 1.0 : 0.2);
			if (storage.errorMessage == null)
			{
				HintError.Visibility = Visibility.Collapsed;
			}
			else
			{
				HintError.Visibility = Visibility.Visible;
				HintError.Text = storage.errorMessage;
			}
			ScrollToTop();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "可视化" + TypeNameSpaced + "列表出错", ModBase.LogLevel.Feedback, null, Lang.Text("Download.Comp.Error.OperationFailed"));
		}
	}

	private void Load_State(object sender, MyLoading.MyLoadingState state, MyLoading.MyLoadingState oldState)
	{
		if (loader.State == ModBase.LoadState.Failed)
		{
			string text = "";
			if (loader.Error != null)
			{
				text = loader.Error.Message;
			}
			if (text.Contains(Lang.Text("Common.Error.InvalidJson")))
			{
				ModBase.Log("[Download] 下载的" + TypeNameSpaced + "列表 json 文件损坏，已自动重试", ModBase.LogLevel.Debug);
				((MyPageRight)(object)Parent).PageLoaderRestart();
			}
		}
	}

	private void BtnPageFirst_Click(object sender, EventArgs e)
	{
		ChangePage(0);
	}

	private void BtnPageLeft_Click(object sender, EventArgs e)
	{
		ChangePage(page - 1);
	}

	private void BtnPageRight_Click(object sender, EventArgs e)
	{
		ChangePage(page + 1);
	}

	private void ChangePage(int newPage)
	{
		CardPages.IsEnabled = false;
		page = newPage;
		ModMain.frmMain.BackToTop();
		ModBase.Log($"[Download] {TypeName}：切换到第 {page + 1} 页");
		ModBase.RunInThread(() =>
		{
			Thread.Sleep(100);
			ModBase.RunInUi(() =>
			{
				CardPages.IsEnabled = true;
			});
			loader.Start();
		});
	}

	private void BtnSearchInstallModPack_Click(object sender, EventArgs e)
	{
		ModModpack.ModpackInstall();
	}

	public void RefreshAllFavoriteStatus()
	{
		try
		{
			foreach (object child in PanProjects.Children)
			{
				if (child is MyCompItem)
				{
					((MyCompItem)child).RefreshFavoriteStatus();
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新收藏状态时出错");
		}
	}

	public PageComp()
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected Obj, but got Unknown
		loader = new ModLoader.LoaderTask<ModComp.CompProjectRequest, int>(Lang.Text("Download.Comp.List.Source.ResourceFetch", new object[1] { "XXX" }), ModComp.CompProjectsGet, LoaderInput)
		{
			reloadTimeout = 60000
		};
		Loaded += PageCompControls_Inited;
		IsVisibleChanged += PageComp_IsVisibleChanged;
		InitializeComponent();
		Load.StateChanged += Load_State;
		BtnPageFirst.Click += BtnPageFirst_Click;
		BtnPageLeft.Click += BtnPageLeft_Click;
		BtnPageRight.Click += BtnPageRight_Click;
		PanSearchBox.Search += (object _, EventArgs _) =>
		{
			StartNewSearch();
		};
		PanSearchBox.KeyDown += EnterTrigger;
		TextSearchVersion.KeyDown += EnterTrigger;
		BtnSearchReset.Click += (object _, EventArgs _) =>
		{
			ResetFilter();
		};
		BtnSearchInstallModPack.Click += BtnSearchInstallModPack_Click;
	}

	private void PageCompControls_Inited(object sender, EventArgs e)
	{
		if ((object)targetVersion != null)
		{
			ResetFilter();
			TextSearchVersion.Text = targetVersion.Info.VanillaName;
			if (targetVersion.Info.HasForge)
			{
				ComboSearchLoader.SelectedItem = GetTargetItemByName("Forge");
			}
			else if (targetVersion.Info.HasFabric)
			{
				ComboSearchLoader.SelectedItem = GetTargetItemByName("Fabric");
			}
			else if (targetVersion.Info.HasNeoForge)
			{
				ComboSearchLoader.SelectedItem = GetTargetItemByName("NeoForge");
			}
			else if (targetVersion.Info.HasQuilt)
			{
				ComboSearchLoader.SelectedItem = GetTargetItemByName("Quilt");
			}
			targetVersion = null;
			if (isLoaderInited)
			{
				StartNewSearch();
			}
			ScrollToHome();
		}
		if (isLoaderInited)
		{
			return;
		}
		isLoaderInited = true;
		((MyPageRight)(object)Parent).PageLoaderInit(Load, PanLoad, PanContent, PanAlways, loader, (ModLoader.LoaderBase _) =>
		{
			Load_OnFinish();
		}, LoaderInput);
		if (ModDownload.AllDrops != null && ModDownload.AllDrops.Count != 0 && ModDownload.AllDrops.First() > 250)
		{
			string text = McInstanceInfo.DropToVersion(ModDownload.AllDrops.First());
			if ((((MyComboBoxItem)TextSearchVersion.Items[1]).Content.ToString() ?? "") != (text ?? ""))
			{
				TextSearchVersion.Items.Insert(1, new MyComboBoxItem
				{
					Content = text
				});
			}
		}
		if (PageType == ModComp.CompType.Shader)
		{
			LabLoader.Visibility = Visibility.Visible;
			ComboSearchLoader.Visibility = Visibility.Collapsed;
			ComboSearchShaderLoader.Visibility = Visibility.Visible;
		}
		else if (PageType == ModComp.CompType.Mod || PageType == ModComp.CompType.ModPack)
		{
			LabLoader.Visibility = Visibility.Visible;
			ComboSearchLoader.Visibility = Visibility.Visible;
			ComboSearchShaderLoader.Visibility = Visibility.Collapsed;
		}
		else
		{
			LabLoader.Visibility = Visibility.Collapsed;
			ComboSearchLoader.Visibility = Visibility.Collapsed;
			ComboSearchShaderLoader.Visibility = Visibility.Collapsed;
		}
		MyComboBoxItem GetTargetItemByName(string name)
		{
			foreach (MyComboBoxItem item in (IEnumerable)ComboSearchLoader.Items)
			{
				if (string.Equals(item.Content?.ToString(), name, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
			return (MyComboBoxItem)ComboSearchLoader.Items[0];
		}
	}

	private void PageComp_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (IsVisible)
		{
			RefreshAllFavoriteStatus();
		}
	}

	private ModComp.CompProjectRequest LoaderInput()
	{
		ModComp.CompProjectRequest compProjectRequest = new ModComp.CompProjectRequest(PageType, storage, (page + 1) * 40);
		string text;
		if (TextSearchVersion.Text == Lang.Text("Download.Comp.Filter.Version.AllInputAvailable"))
		{
			text = null;
		}
		else
		{
			text = ((TextSearchVersion.Text.Contains(".") || TextSearchVersion.Text.Contains("w")) ? TextSearchVersion.Text : null);
		}
		ModComp.CompLoaderType compLoaderType = ModComp.CompLoaderType.Any;
		if (PageType == ModComp.CompType.Mod || PageType == ModComp.CompType.ModPack)
		{
			compLoaderType = (ModComp.CompLoaderType)ModBase.Val(((MyComboBoxItem)ComboSearchLoader.SelectedItem).Tag);
			if (text != null && text.Contains(".") && ModBase.Val(text.Split(".")[1]) < 14.0 && compLoaderType == ModComp.CompLoaderType.Forge)
			{
				compLoaderType = ModComp.CompLoaderType.Any;
			}
		}
		compProjectRequest.searchText = PanSearchBox.Text;
		compProjectRequest.gameVersion = text;
		string text2 = (ComboSearchTag.SelectedItem as FrameworkElement)?.Tag?.ToString();
		string text3 = (ComboSearchShaderLoader.SelectedItem as FrameworkElement)?.Tag?.ToString();
		ref string tag = ref compProjectRequest.tag;
		string text4;
		if (PageType != ModComp.CompType.Shader)
		{
			text4 = text2;
		}
		else
		{
			text4 = (string.IsNullOrEmpty(text3) ? text2 : (text2 + text3));
		}
		tag = text4;
		compProjectRequest.modLoader = (ModComp.CompLoaderType)((PageType == ModComp.CompType.Mod || PageType == ModComp.CompType.ModPack) ? ModBase.Val(((MyComboBoxItem)ComboSearchLoader.SelectedItem).Tag) : 0.0);
		compProjectRequest.source = (ModComp.CompSourceType)ModBase.Val(((MyComboBoxItem)ComboSearchSource.SelectedItem).Tag);
		compProjectRequest.sort = (ModComp.CompSortType)ModBase.Val(((MyComboBoxItem)ComboSearchSort.SelectedItem).Tag);
		return compProjectRequest;
	}

	private void StartNewSearch()
	{
		page = 0;
		object input = LoaderInput();
		if (loader.ShouldStart(ref input))
		{
			storage = new ModComp.CompProjectStorage();
		}
		loader.Start();
	}

	private void EnterTrigger(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Key == 6)
		{
			StartNewSearch();
		}
	}

	private void ResetFilter()
	{
		PanSearchBox.Text = "";
		TextSearchVersion.Text = Lang.Text("Download.Comp.Filter.Version.AllInputAvailable");
		TextSearchVersion.SelectedIndex = 0;
		ComboSearchSource.SelectedIndex = 0;
		ComboSearchTag.SelectedIndex = 0;
		ComboSearchLoader.SelectedIndex = 0;
		ComboSearchShaderLoader.SelectedIndex = 0;
		ComboSearchSort.SelectedIndex = 0;
		loader.lastFinishedTime = 0L;
	}

	private void BtnSearchReset_Click(object sender, EventArgs e)
	{
		ResetFilter();
	}


	static PageComp()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected Obj, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected Obj, but got Unknown
	}
}
