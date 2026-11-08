using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Humanizer;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Saves;
using PCL.Core.Minecraft.Saves.Editing;
using PCL.Core.Minecraft.Saves.Parsing;

namespace PCL;

public partial class PageInstanceSavesInfo : MyPageRight, IRefreshable, IComponentConnector
{
	private static readonly SaveManager SaveManager = new SaveManager((SaveParserFactory)null, (IEnumerable<ISaveEditor>)null);

	private static readonly SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);

	private CancellationTokenSource? _cts;

	public PageInstanceSavesInfo()
	{
		InitializeComponent();
		Loaded += async (object _, RoutedEventArgs _) =>
		{
			PanBack.ScrollToHome();
			await RefreshInfoAsync();
		};
	}

	void IRefreshable.Refresh()
	{
		Refresh();
	}

	public void Refresh()
	{
		RefreshInfoAsync().ContinueWith((Task t) =>
		{
			LogWrapper.Warn((Exception)t.Exception, "Saves", "刷新存档信息异常");
		}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
	}

	private async Task RefreshInfoAsync()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = new CancellationTokenSource();
		CancellationToken token = _cts.Token;
		try
		{
			ClearInfoTable();
			PanSettingsList.Children.Clear();
			PanSettingsList.RowDefinitions.Clear();
			Hintversion1_9.Visibility = Visibility.Collapsed;
			Hintversion1_8.Visibility = Visibility.Collapsed;
			Hintversion1_3.Visibility = Visibility.Collapsed;
			PanSettings.Visibility = Visibility.Collapsed;
			SaveInfo val = await SaveManager.LoadSaveAsync(PageInstanceSavesLeft.currentSave, token);
			MyListItem itemDatapack = ModMain.frmInstanceSavesLeft.ItemDatapack;
			int? versionId = val.VersionId;
			bool flag = ((!versionId.HasValue || versionId.GetValueOrDefault() < 1443) ? true : false);
			itemDatapack.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			if (val.VersionName == null)
			{
				if (val.Difficulty.HasValue)
				{
					ShowHint(Hintversion1_9, "Instance.Saves.Info.VersionHint.1_9");
				}
				else if (val.AllowCommands)
				{
					ShowHint(Hintversion1_8, "Instance.Saves.Info.VersionHint.1_8");
				}
				else
				{
					ShowHint(Hintversion1_3, "Instance.Saves.Info.VersionHint.1_3");
				}
			}
			else
			{
				AddInfoRow(Lang.Text("Instance.Saves.Info.Version"), $"{val.VersionName} ({val.VersionId})");
			}
			AddInfoRow(Lang.Text("Instance.Saves.Info.LevelName"), val.LevelName);
			AddInfoRow(Lang.Text("Instance.Saves.Info.Seed"), val.Seed?.ToString() ?? Lang.Text("Instance.Saves.Info.GetFailed"), isSeed: true, val.VersionName);
			AddInfoRow(Lang.Text("Instance.Saves.Info.LastPlayed"), Lang.Date(val.LastPlayedUtc.ToLocalTime(), "g"));
			if (val.Spawn.HasValue)
			{
				Vector3 value = val.Spawn.Value;
				AddInfoRow(Lang.Text("Instance.Saves.Info.SpawnPoint"), $"{value.X:F0} / {value.Y:F0} / {value.Z:F0}");
			}
			AddInfoRow(Lang.Text("Instance.Saves.Info.GameMode"), GameModeName(val.GameMode));
			AddInfoRow(Lang.Text("Instance.Saves.Info.PlayTime"), Lang.TimeSpan(val.PlayTime, 3, false, (TimeUnit)4, (TimeUnit)1));
			if (val.VersionName != null || val.Difficulty.HasValue)
			{
				BuildAllowCommandsSetting(val.AllowCommands);
			}
			if (val.Difficulty.HasValue)
			{
				BuildDifficultySetting(val.IsHardcore, val.IsDifficultyLocked, (int)val.Difficulty.Value);
			}
			PanContent.Visibility = Visibility.Visible;
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Instance.Saves.Info.Error.LoadFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Instance.Saves.Info.Error.LoadFailed"));
			PanContent.Visibility = Visibility.Collapsed;
			PanSettings.Visibility = Visibility.Collapsed;
			PanSettingsList.Children.Clear();
			PanSettingsList.RowDefinitions.Clear();
			Hintversion1_9.Visibility = Visibility.Collapsed;
			Hintversion1_8.Visibility = Visibility.Collapsed;
			Hintversion1_3.Visibility = Visibility.Collapsed;
		}
	}

	private void BuildAllowCommandsSetting(bool allowCommands)
	{
		PanSettings.Visibility = Visibility.Visible;
		string folder = PageInstanceSavesLeft.currentSave;
		MyComboBox combo = new MyComboBox
		{
			Width = 100.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			ToolTip = Lang.Text("Instance.Saves.Info.Modify.BeforeSave"),
			SelectedValuePath = "Value",
			DisplayMemberPath = "Display"
		};
		combo.Items.Add(new
		{
			Value = 0,
			Display = Lang.Text("Instance.Saves.Info.AllowCommands.NotAllowed")
		});
		combo.Items.Add(new
		{
			Value = 1,
			Display = Lang.Text("Instance.Saves.Info.AllowCommands.Allowed")
		});
		combo.SelectedValue = (allowCommands ? 1 : 0);
		combo.SelectionChanged += async (object _, SelectionChangedEventArgs _) =>
		{
			_ = 1;
			try
			{
				if (combo.SelectedValue != null)
				{
					await WriteLock.WaitAsync();
					try
					{
						SaveManager saveManager = SaveManager;
						string text = folder;
						SaveChanges val = default;
						val.AllowCommands = new Editable<bool>((int)combo.SelectedValue == 1);
						await saveManager.ApplyChangesAsync(text, val, default(CancellationToken));
					}
					finally
					{
						WriteLock.Release();
					}
					HintService.Hint(Lang.Text("Instance.Saves.Info.Modify.CheatSuccess"), HintType.Success);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Instance.Saves.Info.Modify.CheatFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Info.Modify.CheatFailed"));
			}
		};
		AddSettingRow(Lang.Text("Instance.Saves.Info.AllowCommands"), combo);
	}

	private void BuildDifficultySetting(bool isHardcore, bool isLocked, int difficultyValue)
	{
		PanSettings.Visibility = Visibility.Visible;
		string folder = PageInstanceSavesLeft.currentSave;
		MyComboBox combo = new MyComboBox
		{
			Width = 100.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			ToolTip = Lang.Text("Instance.Saves.Info.Modify.BeforeSave"),
			SelectedValuePath = "Value",
			DisplayMemberPath = "Display"
		};
		combo.Items.Add(new
		{
			Value = 0,
			Display = Lang.Text("Instance.Saves.Info.Difficulty.Peaceful")
		});
		combo.Items.Add(new
		{
			Value = 1,
			Display = Lang.Text("Instance.Saves.Info.Difficulty.Easy")
		});
		combo.Items.Add(new
		{
			Value = 2,
			Display = Lang.Text("Instance.Saves.Info.Difficulty.Normal")
		});
		combo.Items.Add(new
		{
			Value = 3,
			Display = Lang.Text("Instance.Saves.Info.Difficulty.Hard")
		});
		combo.SelectedValue = difficultyValue;
		MyCheckBox lockCheckBox = new MyCheckBox
		{
			Text = Lang.Text("Instance.Saves.Info.LockDifficulty"),
			ToolTip = Lang.Text("Instance.Saves.Info.LockDifficulty.ToolTip"),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
			Checked = isLocked,
			Visibility = (isHardcore ? Visibility.Collapsed : Visibility.Visible)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		stackPanel.Children.Add(combo);
		stackPanel.Children.Add(lockCheckBox);
		combo.SelectionChanged += async (object _, SelectionChangedEventArgs _) =>
		{
			await ApplyAsync();
		};
		lockCheckBox.Change += async (object _, bool _) =>
		{
			await ApplyAsync();
		};
		AddSettingRow(Lang.Text("Instance.Saves.Info.GameDifficultyLabel"), stackPanel);
		async Task ApplyAsync()
		{
			_ = 1;
			try
			{
				if (combo.SelectedValue != null)
				{
					await WriteLock.WaitAsync();
					try
					{
						SaveManager saveManager = SaveManager;
						string text = folder;
						SaveChanges val = default;
						val.Difficulty = new Editable<Difficulty>((Difficulty)(int)combo.SelectedValue);
						val.LockDifficulty = new Editable<bool>(!isHardcore && lockCheckBox.Checked == true);
						await saveManager.ApplyChangesAsync(text, val, default(CancellationToken));
					}
					finally
					{
						WriteLock.Release();
					}
					HintService.Hint(Lang.Text("Instance.Saves.Info.Modify.DifficultySuccess"), HintType.Success);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Instance.Saves.Info.Modify.DifficultyFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Info.Modify.DifficultyFailed"));
			}
		}
	}

	private static string GameModeName(GameMode mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		return ((int)mode - 1) switch
		{
			3 => Lang.Text("Instance.Saves.Info.GameMode.Hardcore"), 
			0 => Lang.Text("Instance.Saves.Info.GameMode.Creative"), 
			1 => Lang.Text("Instance.Saves.Info.GameMode.Adventure"), 
			2 => Lang.Text("Instance.Saves.Info.GameMode.Spectator"), 
			_ => Lang.Text("Instance.Saves.Info.GameMode.Survival"), 
		};
	}

	private static void ShowHint(MyHint hint, string langKey)
	{
		hint.Text = Lang.Text(langKey);
		hint.Visibility = Visibility.Visible;
	}

	private void ClearInfoTable()
	{
		PanList.Children.Clear();
		PanList.RowDefinitions.Clear();
	}

	private void AddInfoRow(string head, string content, bool isSeed = false, string? versionName = null)
	{
		TextBlock element = new TextBlock
		{
			Text = head,
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		if (isSeed && content != Lang.Text("Instance.Saves.Info.GetFailed"))
		{
			MyTextButton myTextButton = new MyTextButton
			{
				Text = content,
				Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
			};
			myTextButton.Click += (object _, EventArgs _) =>
			{
				try
				{
					ModBase.ClipboardSet(content);
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, Lang.Text("Instance.Saves.Info.Error.ClipboardFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Info.Error.ClipboardFailed"));
				}
			};
			stackPanel.Children.Add(myTextButton);
			MyIconButton myIconButton = new MyIconButton
			{
				SvgIcon = "lucide/external-link",
				Width = 22.0,
				Height = 22.0,
				ToolTip = Lang.Text("Instance.Saves.Info.Chunkbase.ToolTip")
			};
			myIconButton.Click += (object _, EventArgs _) =>
			{
				OpenChunkbase(content, versionName);
			};
			stackPanel.Children.Add(myIconButton);
		}
		else
		{
			stackPanel.Children.Add(new TextBlock
			{
				Text = content,
				Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
			});
		}
		PanList.Children.Add(element);
		PanList.Children.Add(stackPanel);
		RowDefinition value = new RowDefinition();
		PanList.RowDefinitions.Add(value);
		int value2 = PanList.RowDefinitions.IndexOf(value);
		Grid.SetRow(element, value2);
		Grid.SetColumn(element, 0);
		Grid.SetRow(stackPanel, value2);
		Grid.SetColumn(stackPanel, 2);
	}

	private void AddSettingRow(string head, UIElement control)
	{
		int count = PanSettingsList.RowDefinitions.Count;
		PanSettingsList.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Auto)
		});
		TextBlock element = new TextBlock
		{
			Text = head,
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		Grid.SetRow(element, count);
		Grid.SetColumn(element, 0);
		Grid.SetRow(control, count);
		Grid.SetColumn(control, 2);
		PanSettingsList.Children.Add(element);
		PanSettingsList.Children.Add(control);
		PanSettingsList.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(8.0, GridUnitType.Pixel)
		});
	}

	private static void OpenChunkbase(string seed, string? versionName)
	{
		try
		{
			if (versionName == null)
			{
				ModBase.Log(Lang.Text("Instance.Saves.Info.Chunkbase.UnknownVersion"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Info.Chunkbase.UnknownVersion"));
			}
			else if (versionName.Any(char.IsLetter))
			{
				ModBase.Log(Lang.Text("Instance.Saves.Info.Chunkbase.PreviewVersion", new object[1] { versionName }), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Info.Chunkbase.PreviewVersion", new object[1] { versionName }));
			}
			else
			{
				string value;
				if (versionName.StartsWith("1.21"))
				{
					value = versionName.Replace(".", "_");
				}
				else
				{
					value = (versionName.Contains('.') ? string.Join("_", versionName.Split('.').Take(2)) : versionName.Replace(".", "_"));
				}
				ModBase.OpenWebsite($"https://www.chunkbase.com/apps/seed-map#seed={seed}&platform=java_{value}&dimension=overworld");
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Instance.Saves.Info.Error.ChunkbaseFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Saves.Info.Error.ChunkbaseFailed"));
		}
	}


	static PageInstanceSavesInfo()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected Obj, but got Unknown
	}
}
