using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Humanizer;
using Microsoft.VisualBasic;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils.Exts;

namespace PCL;

public partial class MyLocalCompItem : Grid, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	public class SwipeSelect
	{
		public int Start { get; set; }

		public int End { get; set; }

		public bool Swiping
		{
			get
			{
				return field;
			}
			set
			{
				field = value;
				if (TargetFrm != null)
				{
					try
					{
						Interaction.CallByName(Interaction.CallByName(TargetFrm, "CardSelect", CallType.Get), "IsHitTestVisible", CallType.Set, !value);
					}
					catch
					{
					}
				}
			}
		}

		public bool SwipeToState { get; set; }

		public object TargetFrm { get; set; }
	}

	public delegate void CheckEventHandler(object sender, ModBase.RouteEventArgs e);

	public delegate void ChangedEventHandler(object sender, ModBase.RouteEventArgs e);

	private string? _displayVersion;

	public int Uuid = ModBase.GetUuid();

	private bool isMouseDown;

	private Image imgState;

	public Action<MyLocalCompItem, EventArgs> buttonHandler;

	public FrameworkElement buttonStack;

	public string Logo
	{
		get
		{
			return PathLogo.Source;
		}
		set
		{
			PathLogo.Source = value;
		}
	}

	public string Title
	{
		get
		{
			return field;
		}
		set
		{
			string text = value;
			switch (Entry.State)
			{
			case ModLocalComp.LocalCompFile.LocalFileStatus.Fine:
				LabTitle.TextDecorations = null;
				break;
			case ModLocalComp.LocalCompFile.LocalFileStatus.Disabled:
				LabTitle.TextDecorations = TextDecorations.Strikethrough;
				break;
			case ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable:
				LabTitle.TextDecorations = TextDecorations.Strikethrough;
				value += Lang.Text("Instance.Resource.Item.ErrorSuffix");
				break;
			}
			if (!((LabTitle.Text ?? "") == (value ?? "")))
			{
				LabTitle.Text = value;
				field = text;
			}
		}
	}

	public string SubTitle
	{
		get
		{
			return LabSubtitle?.Text ?? "";
		}
		set
		{
			if (!((LabSubtitle.Text ?? "") == (value ?? "")))
			{
				LabSubtitle.Text = value;
				LabSubtitle.Visibility = (string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible);
			}
		}
	}

	public string Description
	{
		get
		{
			return LabInfo.Text;
		}
		set
		{
			if (!((LabInfo.Text ?? "") == (value ?? "")))
			{
				LabInfo.Text = value;
			}
		}
	}

	public List<string> Tags
	{
		set
		{
			PanTags.Children.Clear();
			PanTags.Visibility = ((!value.Any()) ? Visibility.Collapsed : Visibility.Visible);
			foreach (string item in value)
			{
				Border border = new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)),
					Padding = new Thickness(3.0, 1.0, 3.0, 1.0),
					CornerRadius = new CornerRadius(3.0),
					Margin = new Thickness(0.0, 0.0, 3.0, 0.0),
					SnapsToDevicePixels = true,
					UseLayoutRounding = false
				};
				TextBlock child = new TextBlock
				{
					Text = item,
					Foreground = new SolidColorBrush(ThemeManager.IsDarkMode ? Color.FromArgb(88, byte.MaxValue, byte.MaxValue, byte.MaxValue) : Color.FromArgb(88, 136, 136, 136)),
					FontSize = 11.0
				};
				border.Child = child;
				PanTags.Children.Add(border);
			}
		}
	}

	public ModLocalComp.LocalCompFile Entry
	{
		get
		{
			return (ModLocalComp.LocalCompFile)Tag;
		}
		set
		{
			Tag = value;
		}
	}

	public SwipeSelect CurrentSwipe { get; set; }

	public bool Checked
	{
		get
		{
			return field;
		}
		set
		{
			try
			{
				bool flag = field;
				if (value == field)
				{
					return;
				}
				field = value;
				ModBase.RouteEventArgs e = new ModBase.RouteEventArgs();
				if (IsInitialized)
				{
					Changed?.Invoke(this, e);
					if (e.handled)
					{
						field = flag;
						return;
					}
				}
				if (value)
				{
					ModBase.RouteEventArgs e2 = new ModBase.RouteEventArgs();
					Check?.Invoke(this, e2);
					if (e2.handled)
					{
						return;
					}
				}
				if (UiExtension.IsVisibleInWindow((FrameworkElement)this, (Window)ModMain.frmMain))
				{
					List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
					if (Checked)
					{
						double num = 32.0 - RectCheck.ActualHeight;
						list.Add(ModAnimation.AaHeight(RectCheck, num * 0.4, 200, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
						list.Add(ModAnimation.AaHeight(RectCheck, num * 0.6, 300, 0, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)));
						list.Add(ModAnimation.AaOpacity(RectCheck, 1.0 - RectCheck.Opacity, 30));
						RectCheck.VerticalAlignment = VerticalAlignment.Center;
						RectCheck.Margin = new Thickness(-3.0, 0.0, 0.0, 0.0);
						list.Add(ModAnimation.AaColor(LabTitle, TextBlock.ForegroundProperty, (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? "ColorBrush2" : "ColorBrush5", 200));
					}
					else
					{
						list.Add(ModAnimation.AaHeight(RectCheck, 0.0 - RectCheck.ActualHeight, 120, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)));
						list.Add(ModAnimation.AaOpacity(RectCheck, 0.0 - RectCheck.Opacity, 70, 40));
						RectCheck.VerticalAlignment = VerticalAlignment.Center;
						list.Add(ModAnimation.AaColor(LabTitle, TextBlock.ForegroundProperty, (LabTitle.TextDecorations == null) ? "ColorBrush1" : "ColorBrushGray4", 120));
					}
					ModAnimation.AniStart(list, "MyLocalCompItem Checked " + Uuid);
				}
				else
				{
					RectCheck.VerticalAlignment = VerticalAlignment.Center;
					RectCheck.Margin = new Thickness(-3.0, 0.0, 0.0, 0.0);
					if (Checked)
					{
						RectCheck.Height = 32.0;
						RectCheck.Opacity = 1.0;
						LabTitle.SetResourceReference(TextBlock.ForegroundProperty, (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? "ColorBrush2" : "ColorBrush5");
					}
					else
					{
						RectCheck.Height = 0.0;
						RectCheck.Opacity = 0.0;
						LabTitle.SetResourceReference(TextBlock.ForegroundProperty, (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? "ColorBrush1" : "ColorBrushGray4");
					}
					ModAnimation.AniStop("MyLocalCompItem Checked " + Uuid);
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "设置 Checked 失败");
			}
		}
	}

	public Border RectBack
	{
		get
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			if (field == null)
			{
				Border border = new Border
				{
					Name = "RectBack",
					CornerRadius = new CornerRadius(3.0),
					RenderTransform = new ScaleTransform(0.8, 0.8),
					RenderTransformOrigin = new Point(0.5, 0.5),
					BorderThickness = new Thickness(ModBase.GetWPFSize(1.0)),
					SnapsToDevicePixels = true,
					IsHitTestVisible = false,
					Opacity = 0.0
				};
				border.SetResourceReference(Border.BackgroundProperty, "ColorBrush7");
				border.SetResourceReference(Border.BorderBrushProperty, "ColorBrush6");
				Grid.SetColumnSpan(border, 999);
				Grid.SetRowSpan(border, 999);
				Children.Insert(0, border);
				field = border;
			}
			return field;
		}
	}

	public IEnumerable<MyIconButton> Buttons
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
			if (buttonStack != null)
			{
				Children.Remove(buttonStack);
				buttonStack = null;
			}
			if (!value.Any())
			{
				return;
			}
			buttonStack = new StackPanel
			{
				Opacity = 0.0,
				Margin = new Thickness(0.0, 0.0, 5.0, 0.0),
				SnapsToDevicePixels = false,
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				UseLayoutRounding = false
			};
			Grid.SetColumnSpan(buttonStack, 10);
			Grid.SetRowSpan(buttonStack, 10);
			foreach (MyIconButton item in value)
			{
				if (item.Height.Equals(double.NaN))
				{
					item.Height = 25.0;
				}
				if (item.Width.Equals(double.NaN))
				{
					item.Width = 25.0;
				}
				((StackPanel)buttonStack).Children.Add(item);
			}
			Children.Add(buttonStack);
		}
	}

	public Border RectCheck
	{
		get
		{
			if (field == null)
			{
				field = new Border
				{
					Width = 5.0,
					Height = (Checked ? double.NaN : 0.0),
					CornerRadius = new CornerRadius(2.0, 2.0, 2.0, 2.0),
					VerticalAlignment = ((!Checked) ? VerticalAlignment.Center : VerticalAlignment.Stretch),
					HorizontalAlignment = HorizontalAlignment.Left,
					UseLayoutRounding = false,
					SnapsToDevicePixels = false,
					Margin = (Checked ? new Thickness(-3.0, 6.0, 0.0, 6.0) : new Thickness(-3.0, 0.0, 0.0, 0.0))
				};
				field.SetResourceReference(Border.BackgroundProperty, "ColorBrush3");
				Grid.SetRowSpan(field, 10);
				Children.Add(field);
			}
			return field;
		}
	}

	public event ClickEventHandler? Click;

	public event CheckEventHandler? Check;

	public event ChangedEventHandler? Changed;

	private string GetUpdateCompareDescription()
	{
		_displayVersion = null;
		string text = Entry.compFile.FileName.Replace(".jar", "");
		string text2 = Entry.UpdateFile.FileName.Replace(".jar", "");
		List<string> list = text.Split('-').ToList();
		List<string> list2 = text2.Split('-').ToList();
		bool flag = false;
		foreach (string item in list.ToList())
		{
			if (list2.Contains(item))
			{
				list.Remove(item);
				list2.Remove(item);
				flag = true;
			}
		}
		if (flag && list.Any() && list2.Any())
		{
			text = list.Join("-");
			text2 = list2.Join("-");
			_displayVersion = text;
		}
		return Lang.Text("Instance.Resource.Item.UpdateCompare", new object[4]
		{
			text,
			Lang.TimeSpan(Entry.compFile.ReleaseDate - DateTime.Now, 2, true, (TimeUnit)7, (TimeUnit)3),
			text2,
			Lang.TimeSpan(Entry.UpdateFile.ReleaseDate - DateTime.Now, 2, true, (TimeUnit)7, (TimeUnit)3)
		});
	}

	public void Refresh()
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			if (Entry.CanUpdate)
			{
				BtnUpdate.Visibility = Visibility.Visible;
				BtnUpdate.ToolTip = GetUpdateCompareDescription() + "\r\n" + Lang.Text("Instance.Resource.Item.UpdateToolTip");
			}
			else
			{
				BtnUpdate.Visibility = Visibility.Collapsed;
				_displayVersion = null;
			}
			string descFileName = (Entry.IsFolder ? Entry.Name : (Entry.State switch
			{
				ModLocalComp.LocalCompFile.LocalFileStatus.Fine => ModBase.GetFileNameWithoutExtentionFromPath(Entry.path), 
				ModLocalComp.LocalCompFile.LocalFileStatus.Disabled => ModBase.GetFileNameWithoutExtentionFromPath(Entry.path.Replace(".disabled", "").Replace(".old", "")), 
				_ => ModBase.GetFileNameFromPath(Entry.path), 
			}));
			string displayVersion = _displayVersion ?? Entry.Version;
			ModComp.CompProject compTemp = Entry.Comp;
			string text;
			if (Entry.IsFolder)
			{
				Title = Entry.Name;
				text = Entry.Description;
			}
			else if (Config.Download.Comp.UiCompNameSolution == 1)
			{
				Title = descFileName;
				SubTitle = "";
				if ((object)Entry.Comp == null)
				{
					text = Entry.Name;
				}
				else
				{
					KeyValuePair<string, string> keyValuePair = await Task.Run(() => compTemp.GetControlTitle(hasModLoaderDescription: false));
					text = keyValuePair.Key + keyValuePair.Value;
				}
				text = text.Replace("  |  ", " / ");
				if (displayVersion != null)
				{
					text = text + " (" + displayVersion + ")";
				}
			}
			else
			{
				if ((object)Entry.Comp == null)
				{
					Title = Entry.Name;
					SubTitle = ((displayVersion == null) ? "" : ("  |  " + displayVersion));
				}
				else
				{
					KeyValuePair<string, string> keyValuePair2 = await Task.Run(() => compTemp.GetControlTitle(hasModLoaderDescription: false));
					Title = keyValuePair2.Key;
					SubTitle = keyValuePair2.Value + ((displayVersion == null) ? "" : ("  |  " + displayVersion));
				}
				text = descFileName;
			}
			if ((object)Entry.Comp != null)
			{
				text = text + ": " + Entry.Comp.Description.Replace("\r", "").Replace("\n", "");
			}
			else if (Entry.Description != null)
			{
				text = text + ": " + Entry.Description.Replace("\r", "").Replace("\n", "");
			}
			else if (!Entry.IsFileAvailable)
			{
				text = text + ": " + Lang.Text("Instance.Resource.Item.InfoUnavailable");
			}
			Description = text;
			if (Checked)
			{
				LabTitle.SetResourceReference(TextBlock.ForegroundProperty, (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? "ColorBrush2" : "ColorBrush5");
			}
			else
			{
				LabTitle.SetResourceReference(TextBlock.ForegroundProperty, (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) ? "ColorBrush1" : "ColorBrushGray4");
			}
			Logo = Entry.GetLogo();
			if (Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
			{
				if (imgState != null)
				{
					Children.Remove(imgState);
					imgState = null;
				}
			}
			else
			{
				if (imgState == null)
				{
					imgState = new Image
					{
						Width = 20.0,
						Height = 20.0,
						Margin = new Thickness(0.0, 0.0, -5.0, -3.0),
						IsHitTestVisible = false,
						HorizontalAlignment = HorizontalAlignment.Right,
						VerticalAlignment = VerticalAlignment.Bottom
					};
					RenderOptions.SetBitmapScalingMode((DependencyObject)(object)imgState, BitmapScalingMode.HighQuality);
					Grid.SetColumn(imgState, 1);
					Grid.SetRow(imgState, 1);
					Grid.SetRowSpan(imgState, 2);
					Children.Add(imgState);
				}
				imgState.Source = new MyBitmap(ModBase.pathImage + $"Icons/{Entry.State}.png");
			}
			if (Entry.IsFolder)
			{
				Tags = new List<string> { Lang.Text("Instance.Resource.Item.FolderTag") };
			}
			else if ((object)Entry.Comp != null)
			{
				Tags = Entry.Comp.Tags;
			}
		}), Array.Empty<object>());
	}

	public void RefreshColor(object sender, EventArgs e)
	{
		InitLate(sender, e);
		int num = (IsMouseOver ? 120 : 180);
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		if (buttonStack != null)
		{
			if (IsMouseOver)
			{
				list.Add(ModAnimation.AaOpacity(buttonStack, 1.0 - buttonStack.Opacity, (int)Math.Round((double)num * 0.7), (int)Math.Round((double)num * 0.3)));
				list.Add(ModAnimation.AaDouble((object? i) =>
				{
					ColumnPaddingRight.Width = new GridLength(Math.Max(0.0, ColumnPaddingRight.Width.Value + (double)i));
				}, (double)(5 + Buttons.Count() * 25) - ColumnPaddingRight.Width.Value, (int)Math.Round((double)num * 0.3), (int)Math.Round((double)num * 0.7)));
			}
			else
			{
				list.Add(ModAnimation.AaOpacity(buttonStack, 0.0 - buttonStack.Opacity, (int)Math.Round((double)num * 0.4)));
				list.Add(ModAnimation.AaDouble((object? i) =>
				{
					ColumnPaddingRight.Width = new GridLength(Math.Max(0.0, ColumnPaddingRight.Width.Value + (double)i));
				}, 4.0 - ColumnPaddingRight.Width.Value, (int)Math.Round((double)num * 0.4)));
			}
		}
		if (IsMouseOver || Checked)
		{
			list.AddRange(new ModAnimation.AniData[2]
			{
				ModAnimation.AaColor(RectBack, Border.BackgroundProperty, isMouseDown ? "ColorBrush6" : "ColorBrushBg1", num),
				ModAnimation.AaOpacity(RectBack, 1.0 - RectBack.Opacity, num, 0, new ModAnimation.AniEaseOutFluent())
			});
			if (isMouseDown)
			{
				list.Add(ModAnimation.AaScaleTransform(RectBack, 0.996 - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round((double)num * 1.2), 0, new ModAnimation.AniEaseOutFluent()));
			}
			else
			{
				list.Add(ModAnimation.AaScaleTransform(RectBack, 1.0 - ((ScaleTransform)RectBack.RenderTransform).ScaleX, (int)Math.Round((double)num * 1.2), 0, new ModAnimation.AniEaseOutFluent()));
			}
		}
		else
		{
			list.AddRange(new ModAnimation.AniData[3]
			{
				ModAnimation.AaOpacity(RectBack, 0.0 - RectBack.Opacity, num),
				ModAnimation.AaScaleTransform(RectBack, 0.996 - ((ScaleTransform)RectBack.RenderTransform).ScaleX, num, 0, new ModAnimation.AniEaseOutFluent()),
				ModAnimation.AaScaleTransform(RectBack, -0.196, 1, 0, null, after: true)
			});
		}
		ModAnimation.AniStart(list, "LocalModItem Color " + Uuid);
	}

	private void InitLate(object sender, EventArgs e)
	{
		if (buttonHandler != null)
		{
			buttonHandler((MyLocalCompItem)sender, e);
			buttonHandler = null;
		}
	}

	private void BtnUpdate_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		ShowUpdateLog();
	}

	private void ShowUpdateLog()
	{
		if ((object)Entry.Comp != null)
		{
			if (!Information.IsNumeric(Entry.Comp.Id))
			{
				string text = Entry.changelogUrls.FirstOrDefault((string x) => x.Contains("modrinth.com"));
				if (text != null)
				{
					ModBase.OpenWebsite(text);
					return;
				}
			}
			else
			{
				string text2 = Entry.changelogUrls.FirstOrDefault((string x) => x.Contains("curseforge.com"));
				if (text2 != null)
				{
					ModBase.OpenWebsite(text2);
					return;
				}
			}
		}
		ModBase.Log(Lang.Text("Instance.Resource.Item.OpenChangelogFailed"), ModBase.LogLevel.Hint, null, Lang.Text("Instance.Resource.Item.OpenChangelogFailed"));
	}

	private void BtnUpdate_Click(object sender, EventArgs e)
	{
		switch (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Item.UpdateConfirm.Message", new object[1] { Entry.Name }) + "\r\n\r\n" + GetUpdateCompareDescription(), Lang.Text("Instance.Resource.Item.UpdateConfirm.Title"), Lang.Text("Instance.Resource.Item.Update"), Lang.Text("Instance.Resource.Item.ViewChangelog"), Lang.Text("Common.Action.Cancel")))
		{
		case 1:
			switch (Entry.Comp.Type)
			{
			case ModComp.CompType.Mod:
				if (ModMain.frmInstanceMod == null)
				{
					ModMain.frmInstanceMod = new PageInstanceCompResource(ModComp.CompType.Mod);
				}
				ModMain.frmInstanceMod.UpdateResource(new ModLocalComp.LocalCompFile[1] { Entry });
				break;
			case ModComp.CompType.ResourcePack:
				if (ModMain.frmInstanceResourcePack == null)
				{
					ModMain.frmInstanceResourcePack = new PageInstanceCompResource(ModComp.CompType.ResourcePack);
				}
				ModMain.frmInstanceResourcePack.UpdateResource(new ModLocalComp.LocalCompFile[1] { Entry });
				break;
			case ModComp.CompType.Shader:
				if (ModMain.frmInstanceShader == null)
				{
					ModMain.frmInstanceShader = new PageInstanceCompResource(ModComp.CompType.Shader);
				}
				ModMain.frmInstanceShader.UpdateResource(new ModLocalComp.LocalCompFile[1] { Entry });
				break;
			case ModComp.CompType.DataPack:
				if (ModMain.frmInstanceSavesDatapack == null)
				{
					ModMain.frmInstanceSavesDatapack = new PageInstanceSavesDatapack();
				}
				ModMain.frmInstanceSavesDatapack.UpdateResource(new ModLocalComp.LocalCompFile[1] { Entry });
				break;
			case ModComp.CompType.ModPack:
				break;
			}
			break;
		case 2:
			ShowUpdateLog();
			break;
		case 3:
			break;
		}
	}

	private void PanTitle_SizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
	{
		int num = ((!ColumnExtend.Width.IsStar) ? ((!ColumnTitle.Width.IsStar) ? 1 : 2) : 0);
		int num2 = 0;
		switch (num)
		{
		case 0:
			if (ColumnExtend.ActualWidth < 0.5)
			{
				num2 = ((LabSubtitle.Visibility != Visibility.Collapsed) ? 1 : 2);
				break;
			}
			return;
		case 1:
			if (ColumnSubtitle.ActualWidth < 0.5)
			{
				num2 = 2;
				break;
			}
			if (!UiExtension.IsTextTrimmed(LabSubtitle))
			{
				num2 = 0;
				break;
			}
			return;
		case 2:
			if (!UiExtension.IsTextTrimmed(LabTitle))
			{
				num2 = ((LabSubtitle.Visibility != Visibility.Collapsed) ? 1 : 0);
				break;
			}
			return;
		}
		switch (num2)
		{
		case 0:
			ColumnTitle.Width = GridLength.Auto;
			ColumnSubtitle.Width = GridLength.Auto;
			ColumnExtend.Width = new GridLength(1.0, GridUnitType.Star);
			break;
		case 1:
			ColumnTitle.Width = GridLength.Auto;
			ColumnSubtitle.Width = new GridLength(1.0, GridUnitType.Star);
			ColumnExtend.Width = new GridLength(0.0, GridUnitType.Pixel);
			break;
		case 2:
			ColumnTitle.Width = new GridLength(1.0, GridUnitType.Star);
			ColumnSubtitle.Width = new GridLength(0.0, GridUnitType.Pixel);
			ColumnExtend.Width = new GridLength(0.0, GridUnitType.Pixel);
			break;
		}
	}

	public MyLocalCompItem()
	{
		InitializeComponent();
		PreviewMouseLeftButtonUp += Button_MouseUp;
		PreviewMouseLeftButtonDown += Button_MouseDown;
		MouseLeave += Button_MouseLeave;
		PreviewMouseLeftButtonUp += Button_MouseLeave;
		MouseLeftButtonDown += Button_MouseSwipeStart;
		MouseEnter += Button_MouseSwipe;
		MouseLeave += Button_MouseSwipe;
		MouseLeftButtonUp += Button_MouseSwipe;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Refresh();
		};
		MouseEnter += RefreshColor;
		MouseLeave += RefreshColor;
		MouseLeftButtonDown += RefreshColor;
		MouseLeftButtonUp += RefreshColor;
		Changed += RefreshColor;
		BtnUpdate.PreviewMouseRightButtonUp += BtnUpdate_PreviewMouseRightButtonUp;
		BtnUpdate.Click += BtnUpdate_Click;
		PanTitle.SizeChanged += PanTitle_SizeChanged;
	}

	private void Button_MouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isMouseDown)
		{
			Click?.Invoke(sender, e);
			if (!e.Handled)
			{
				ModBase.Log("[Control] 按下本地 Mod 列表项：" + LabTitle.Text);
			}
		}
	}

	private void Button_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (IsMouseDirectlyOver)
		{
			isMouseDown = true;
			if (buttonStack != null)
			{
				buttonStack.IsHitTestVisible = false;
			}
		}
	}

	private void Button_MouseLeave(object sender, object e)
	{
		isMouseDown = false;
		if (buttonStack != null)
		{
			buttonStack.IsHitTestVisible = true;
		}
	}

	private void Button_MouseSwipeStart(object sender, object e)
	{
		if (Parent != null)
		{
			int num = ((StackPanel)(object)Parent).Children.IndexOf(this);
			CurrentSwipe.Start = num;
			CurrentSwipe.End = num;
			CurrentSwipe.Swiping = true;
			CurrentSwipe.SwipeToState = !Checked;
		}
	}

	private void Button_MouseSwipe(object sender, object e)
	{
		if (Parent == null)
		{
			return;
		}
		if (Mouse.LeftButton != MouseButtonState.Pressed || !(Mouse.DirectlyOver is MyLocalCompItem))
		{
			CurrentSwipe.Swiping = false;
			return;
		}
		UIElementCollection children = ((StackPanel)(object)Parent).Children;
		int val = children.IndexOf(this);
		CurrentSwipe.Start = (int)Math.Round(ModBase.MathClamp(Math.Min(CurrentSwipe.Start, val), 0.0, children.Count - 1));
		CurrentSwipe.End = (int)Math.Round(ModBase.MathClamp(Math.Max(CurrentSwipe.End, val), 0.0, children.Count - 1));
		if (CurrentSwipe.Start != CurrentSwipe.End)
		{
			int i = CurrentSwipe.Start;
			for (int end = CurrentSwipe.End; i <= end; i++)
			{
				MyLocalCompItem myLocalCompItem = (MyLocalCompItem)children[i];
				myLocalCompItem.InitLate(myLocalCompItem, (EventArgs)e);
				myLocalCompItem.Checked = CurrentSwipe.SwipeToState;
			}
		}
	}

}
