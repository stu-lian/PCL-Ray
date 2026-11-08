using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using PCL.Core.UI.Controls.SvgIcon;

namespace PCL;

public partial class MyCompItem : Grid, IComponentConnector
{
	public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);

	private string stateLast;

	public int Uuid = ModBase.GetUuid();

	private bool isMouseDown;

	private Border _RectBack;





	public bool CanInteraction { get; set; } = true;

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
			return LabTitle.Text;
		}
		set
		{
			if (!((LabTitle.Text ?? "") == (value ?? "")))
			{
				LabTitle.Text = value;
			}
		}
	}

	public string SubTitle
	{
		get
		{
			return LabTitleRaw?.Text ?? "";
		}
		set
		{
			if (!((LabTitleRaw.Text ?? "") == (value ?? "")))
			{
				LabTitleRaw.Text = value;
				LabTitleRaw.Visibility = (string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible);
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
					Background = new SolidColorBrush(Color.FromArgb(17, 0, 0, 0)),
					Padding = new Thickness(3.0, 1.0, 3.0, 1.0),
					CornerRadius = new CornerRadius(3.0),
					Margin = new Thickness(0.0, 0.0, 3.0, 0.0),
					SnapsToDevicePixels = true,
					UseLayoutRounding = false
				};
				TextBlock child = new TextBlock
				{
					Text = item,
					Foreground = new SolidColorBrush(Color.FromRgb(134, 134, 134)),
					FontSize = 11.0
				};
				border.Child = child;
				PanTags.Children.Add(border);
			}
		}
	}

	public bool ShowFavoriteBtn
	{
		get
		{
			return BtnDelete.Visibility == Visibility.Visible;
		}
		set
		{
			BtnDelete.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
			_UpdatePanButtons();
		}
	}

	public bool ShowDownloadBtn
	{
		get
		{
			return BtnDownload.Visibility == Visibility.Visible;
		}
		set
		{
			BtnDownload.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
			_UpdatePanButtons();
		}
	}

	private bool _HasActionButtons
	{
		get
		{
			if (!ShowFavoriteBtn)
			{
				return ShowDownloadBtn;
			}
			return true;
		}
	}

	public Border RectBack
	{
		get
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			if (_RectBack == null)
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
				_RectBack = border;
			}
			return _RectBack;
		}
	}

	public event ClickEventHandler? Click;

	public void RefreshColor(object sender, EventArgs e)
	{
		if (!CanInteraction)
		{
			return;
		}
		string text;
		int num;
		if (IsMouseOver)
		{
			if (isMouseDown)
			{
				text = "MouseDown";
				num = 120;
			}
			else
			{
				text = "MouseOver";
				num = 120;
			}
		}
		else
		{
			text = "Idle";
			num = 180;
		}
		if ((stateLast ?? "") == (text ?? ""))
		{
			return;
		}
		stateLast = text;
		if (IsLoaded && ModAnimation.AniControlEnabled == 0)
		{
			List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
			if (IsMouseOver)
			{
				if (PanButtons != null && _HasActionButtons)
				{
					list.Add(ModAnimation.AaOpacity(PanButtons, 1.0 - PanButtons.Opacity, (int)Math.Round((double)num * 0.35), (int)Math.Round((double)num * 0.15)));
				}
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
				if (PanButtons != null && _HasActionButtons)
				{
					list.Add(ModAnimation.AaOpacity(PanButtons, 0.0 - PanButtons.Opacity, (int)Math.Round((double)num * 0.4)));
				}
				list.AddRange(new ModAnimation.AniData[4]
				{
					ModAnimation.AaOpacity(RectBack, 0.0 - RectBack.Opacity, num),
					ModAnimation.AaColor(RectBack, Border.BackgroundProperty, isMouseDown ? "ColorBrush6" : "ColorBrush7", num),
					ModAnimation.AaScaleTransform(RectBack, 0.996 - ((ScaleTransform)RectBack.RenderTransform).ScaleX, num, 0, new ModAnimation.AniEaseOutFluent()),
					ModAnimation.AaScaleTransform(RectBack, -0.196, 1, 0, null, after: true)
				});
			}
			ModAnimation.AniStart(list, "CompItem Color " + Uuid);
		}
		else
		{
			ModAnimation.AniStop("CompItem Color " + Uuid);
			if (_RectBack != null)
			{
				RectBack.Opacity = 0.0;
			}
			if (PanButtons != null)
			{
				PanButtons.Opacity = 0.0;
			}
		}
	}

	public MyCompItem()
	{
		InitializeComponent();
		Click += (object sender, MouseButtonEventArgs e) =>
		{
			MyCompItem_Click((MyCompItem)sender, e);
		};
		PreviewMouseLeftButtonUp += Button_MouseUp;
		PreviewMouseLeftButtonDown += Button_MouseDown;
		MouseLeave += Button_MouseLeave;
		PreviewMouseLeftButtonUp += Button_MouseLeave;
		MouseEnter += RefreshColor;
		MouseLeave += RefreshColor;
		MouseLeftButtonDown += RefreshColor;
		MouseLeftButtonUp += RefreshColor;
		LabInfo.MouseEnter += LabInfo_MouseEnter;
		BtnDelete.Click += BtnDelete_Click;
		BtnDownload.Click += _BtnDownload_Click;
	}

	private void LabInfo_MouseEnter(object sender, MouseEventArgs e)
	{
		if (IsTextTrimmed(LabInfo))
		{
			ToolTipInfo.Content = LabInfo.Text;
			ToolTipInfo.Width = LabInfo.ActualWidth + 25.0;
			LabInfo.ToolTip = ToolTipInfo;
		}
		else
		{
			LabInfo.ToolTip = null;
		}
	}

	private bool IsTextTrimmed(TextBlock textBlock)
	{
		Typeface typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
		return new FormattedText(textBlock.Text, Thread.CurrentThread.CurrentCulture, textBlock.FlowDirection, typeface, textBlock.FontSize, textBlock.Foreground, ModBase.dpi).Width > textBlock.ActualWidth;
	}

	private void _UpdatePanButtons()
	{
		if (PanButtons != null)
		{
			PanButtons.Visibility = ((!_HasActionButtons) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public void RefreshFavoriteStatus()
	{
		if (Tag is ModComp.CompProject compProject)
		{
			bool flag = ModComp.CompFavorites.IsFavourite(compProject.Id);
			BtnDelete.SvgIcon = (flag ? "lucide/heart-filled" : "lucide/heart");
		}
	}

	private void BtnDelete_Click(object sender, EventArgs e)
	{
		if (PanButtons.Opacity > 0.0 && Tag is ModComp.CompProject)
		{
			ModComp.CompFavorites.ShowMenu((ModComp.CompProject)Tag, (UIElement)sender, () =>
			{
				RefreshFavoriteStatus();
			});
		}
	}

	private void _BtnDownload_Click(object sender, EventArgs e)
	{
		if (PanButtons.Opacity > 0.0 && Tag is ModComp.CompProject project)
		{
			ModComp.QuickDownload(project);
		}
	}

	private void MyCompItem_Click(MyCompItem sender, EventArgs e)
	{
		List<string> list = new List<string>();
		if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.CompDetail)
		{
			foreach (MyCard child in ModMain.frmDownloadCompDetail.PanResults.Children)
			{
				if (!string.IsNullOrEmpty(child.Title) && !child.IsSwapped)
				{
					list.Add(child.Title);
				}
			}
			ModBase.Log("[Comp] 记录当前已展开的卡片：" + string.Join("、", list));
			(ModComp.CompProject, List<string>, string, ModComp.CompLoaderType, ModComp.CompType, string) value = ModMain.frmMain.pageCurrent.additional.Value;
			FormMain.PageStackData pageCurrent = ModMain.frmMain.pageCurrent;
			(ModComp.CompProject, List<string>, string, ModComp.CompLoaderType, ModComp.CompType, string) value2 = value;
			value2.Item2 = list;
			pageCurrent.additional = value2;
		}
		ModComp.CompType item = ModComp.CompType.Mod;
		string item2 = null;
		ModComp.CompLoaderType item3 = ModComp.CompLoaderType.Any;
		if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.Download)
		{
			if (ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.DownloadCompFavorites)
			{
				item2 = "";
				item3 = ModComp.CompLoaderType.Any;
			}
			else
			{
				switch (ModMain.frmMain.PageCurrentSub)
				{
				case FormMain.PageSubType.DownloadMod:
					item = ModComp.CompType.Mod;
					item2 = ModMain.frmDownloadMod.Content.loader.input.gameVersion;
					item3 = ModMain.frmDownloadMod.Content.loader.input.modLoader;
					break;
				case FormMain.PageSubType.DownloadPack:
					item = ModComp.CompType.ModPack;
					item2 = ModMain.frmDownloadPack.Content.loader.input.gameVersion;
					break;
				case FormMain.PageSubType.DownloadDataPack:
					item = ModComp.CompType.DataPack;
					item2 = ModMain.frmDownloadDataPack.Content.loader.input.gameVersion;
					break;
				case FormMain.PageSubType.DownloadResourcePack:
					item = ModComp.CompType.ResourcePack;
					item2 = ModMain.frmDownloadResourcePack.Content.loader.input.gameVersion;
					break;
				case FormMain.PageSubType.DownloadShader:
					item = ModComp.CompType.Shader;
					item2 = ModMain.frmDownloadShader.Content.loader.input.gameVersion;
					break;
				case FormMain.PageSubType.DownloadWorld:
					item = ModComp.CompType.World;
					item2 = ModMain.frmDownloadWorld.Content.loader.input.gameVersion;
					break;
				}
			}
		}
		else if (ModMain.frmMain.pageCurrent.page == FormMain.PageType.InstanceSetup)
		{
			item = ModComp.CompType.ModPack;
		}
		else
		{
			item = ModComp.CompType.Any;
			(ModComp.CompProject CompProject, List<string> ExpandedTitles, string TargetVersion, ModComp.CompLoaderType TargetLoader, ModComp.CompType ResourceType, string SavePath) value3 = ModMain.frmMain.pageCurrent.additional.Value;
			item2 = value3.TargetVersion;
			item3 = value3.TargetLoader;
		}
		ModMain.frmMain.PageChange(new FormMain.PageStackData
		{
			page = FormMain.PageType.CompDetail,
			additional = ((ModComp.CompProject)sender.Tag, new List<string>(), item2, item3, item, null)
		});
	}

	private void Button_MouseUp(object sender, MouseButtonEventArgs e)
	{
		if (isMouseDown)
		{
			Click?.Invoke(sender, e);
		}
	}

	private void Button_MouseDown(object sender, MouseButtonEventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (!CanInteraction)
		{
			return;
		}
		Point position = e.GetPosition(this);
		bool flag = false;
		if (PanButtons.Visibility == Visibility.Visible)
		{
			flag = _IsClickOnActionButton(BtnDelete, position) || _IsClickOnActionButton(BtnDownload, position);
		}
		if (!flag)
		{
			bool flag2 = false;
			if (LabInfo.Visibility == Visibility.Visible)
			{
				Rect val = new Rect(LabInfo.TranslatePoint(new Point(0.0, 0.0), this), LabInfo.RenderSize);
				flag2 = val.Contains(position);
			}
			if (IsMouseDirectlyOver | flag2)
			{
				isMouseDown = true;
			}
		}
	}

	private void Button_MouseLeave(object sender, object e)
	{
		isMouseDown = false;
	}

	private bool _IsClickOnActionButton(FrameworkElement button, Point clickPosition)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (button == null || button.Visibility != Visibility.Visible)
		{
			return false;
		}
		Rect val = new Rect(button.TranslatePoint(new Point(0.0, 0.0), this), button.RenderSize);
		return val.Contains(clickPosition);
	}

}
