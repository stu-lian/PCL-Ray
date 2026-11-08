using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PCL;

public partial class PageToolsShield : MyPageRight, IComponentConnector
{
	public sealed class LayerItem
	{
		public string Pattern { get; init; } = "stripe_top";
		public string Color { get; init; } = "white";
		public string Description { get; init; } = "";
		public Brush ColorBrush { get; init; } = Brushes.White;
	}

	private readonly List<LayerItem> _layers = new List<LayerItem>();

	private bool _ready;

	public PageToolsShield()
	{
		InitializeComponent();
		foreach (McDye dye in McToolData.Dyes)
		{
			ComboBase.Items.Add(new ComboBoxItem { Content = dye.Chinese, Tag = dye.Key });
			ComboColor.Items.Add(new ComboBoxItem { Content = dye.Chinese, Tag = dye.Key });
		}
		foreach (McPattern pattern in McToolData.Patterns)
		{
			ComboPattern.Items.Add(new ComboBoxItem
			{
				Content = pattern.Emblem ? pattern.Chinese + "（盾徽）" : pattern.Chinese,
				Tag = pattern.Key,
				ToolTip = pattern.Emblem ? pattern.UnlockHint : null
			});
		}
		ComboBase.SelectedIndex = 0;
		ComboColor.SelectedIndex = 0;
		ComboPattern.SelectedIndex = 5;
		BuildPatternPanel();
		_ready = true;
		Refresh();
	}

	private void BuildPatternPanel()
	{
		foreach (McPattern pattern in McToolData.Patterns)
		{
			Canvas preview = new Canvas { Width = 20, Height = 40, IsHitTestVisible = false };
			Rectangle background = new Rectangle { Width = 20, Height = 40, Fill = new SolidColorBrush(Color.FromRgb(120, 124, 130)) };
			preview.Children.Add(background);
			AddPattern(pattern.Key, Color.FromRgb(255, 255, 255), preview);

			Border tile = new Border
			{
				Width = 84,
				Height = 108,
				Margin = new Thickness(0, 0, 8, 8),
				CornerRadius = new CornerRadius(4),
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Color.FromRgb(70, 74, 80)),
				Background = new SolidColorBrush(Color.FromRgb(46, 50, 55)),
				Cursor = Cursors.Hand,
				Tag = pattern.Key,
				ToolTip = pattern.Emblem ? pattern.Chinese + "（" + pattern.UnlockHint + "）" : pattern.Chinese
			};
			StackPanel content = new StackPanel { Margin = new Thickness(6) };
			Viewbox viewbox = new Viewbox { Width = 24, Height = 48, HorizontalAlignment = HorizontalAlignment.Center, Stretch = Stretch.Uniform };
			viewbox.Child = preview;
			content.Children.Add(viewbox);
			content.Children.Add(new TextBlock
			{
				Text = pattern.Chinese,
				FontSize = 11,
				TextAlignment = TextAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 4, 0, 0)
			});
			tile.Child = content;
			tile.MouseLeftButtonUp += (object sender, MouseButtonEventArgs _) => SelectPatternTile((Border)sender);
			PanelPatterns.Children.Add(tile);
		}
	}

	private void SelectPatternTile(Border tile)
	{
		string key = tile.Tag as string ?? "stripe_top";
		for (int index = 0; index < ComboPattern.Items.Count; index++)
		{
			if ((ComboPattern.Items[index] as ComboBoxItem)?.Tag as string == key)
			{
				ComboPattern.SelectedIndex = index;
				break;
			}
		}
		BtnAdd_Click(tile, null!);
	}

	private string BaseColor => (ComboBase.SelectedItem as ComboBoxItem)?.Tag as string ?? "white";

	private void ComboBase_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (_ready)
		{
			Refresh();
		}
	}

	private void TextName_Changed(object sender, TextChangedEventArgs e)
	{
		if (_ready)
		{
			Refresh();
		}
	}

	private void Pattern_Changed(object sender, SelectionChangedEventArgs e)
	{
	}

	private void BtnAdd_Click(object sender, MouseButtonEventArgs e)
	{
		string patternKey = (ComboPattern.SelectedItem as ComboBoxItem)?.Tag as string ?? "stripe_top";
		string colorKey = (ComboColor.SelectedItem as ComboBoxItem)?.Tag as string ?? "white";
		McPattern pattern = McToolData.FindPattern(patternKey);
		McDye dye = McToolData.FindDye(colorKey);
		_layers.Add(new LayerItem
		{
			Pattern = pattern.Key,
			Color = dye.Key,
			Description = pattern.Chinese + " · " + dye.Chinese,
			ColorBrush = new SolidColorBrush(Color.FromRgb((byte)(dye.Rgb >> 16), (byte)(dye.Rgb >> 8), (byte)dye.Rgb))
		});
		Refresh();
	}

	private void BtnUp_Click(object sender, MouseButtonEventArgs e) => Move(-1);

	private void BtnDown_Click(object sender, MouseButtonEventArgs e) => Move(1);

	private void Move(int delta)
	{
		int index = ListLayers.SelectedIndex;
		int target = index + delta;
		if (index < 0 || target < 0 || target >= _layers.Count)
		{
			return;
		}
		(_layers[index], _layers[target]) = (_layers[target], _layers[index]);
		Refresh();
		ListLayers.SelectedIndex = target;
	}

	private void BtnDelete_Click(object sender, MouseButtonEventArgs e)
	{
		int index = ListLayers.SelectedIndex;
		if (index >= 0 && index < _layers.Count)
		{
			_layers.RemoveAt(index);
			Refresh();
		}
	}

	private void BtnClear_Click(object sender, MouseButtonEventArgs e)
	{
		_layers.Clear();
		Refresh();
	}

	private void ListLayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
	}

	private void Refresh()
	{
		int selected = ListLayers.SelectedIndex;
		ListLayers.ItemsSource = null;
		ListLayers.ItemsSource = _layers;
		if (selected >= 0 && selected < _layers.Count)
		{
			ListLayers.SelectedIndex = selected;
		}
		UpdatePreview();
		McDye baseDye = McToolData.FindDye(BaseColor);
		TextGive.Text = McToolBuilders.BuildShieldGive(BaseColor, _layers.Select(l => new BannerPattern { Key = l.Pattern, Color = l.Color }).ToList(), TextName.Text ?? "");
		TextLegacy.Text = McToolBuilders.BuildShieldLegacy(BaseColor, _layers.Select(l => new BannerPattern { Key = l.Pattern, Color = l.Color }).ToList(), TextName.Text ?? "");
		LabLayerCount.Text = $"底色 {baseDye.Chinese}，共 {_layers.Count} 层图案";
	}

	private void UpdatePreview()
	{
		PreviewCanvas.Children.Clear();
		McDye baseDye = McToolData.FindDye(BaseColor);
		Color baseRgb = Color.FromRgb((byte)(baseDye.Rgb >> 16), (byte)(baseDye.Rgb >> 8), (byte)baseDye.Rgb);
		Rectangle banner = new Rectangle { Width = 20, Height = 40, Fill = new SolidColorBrush(baseRgb) };
		Canvas.SetLeft(banner, 0);
		Canvas.SetTop(banner, 0);
		PreviewCanvas.Children.Add(banner);
		foreach (LayerItem layer in _layers)
		{
			McDye dye = McToolData.FindDye(layer.Color);
			Color rgb = Color.FromRgb((byte)(dye.Rgb >> 16), (byte)(dye.Rgb >> 8), (byte)dye.Rgb);
			AddPattern(layer.Pattern, rgb);
		}
		// 底部尖角：挖掉两侧，形成旗帜形状
		AddMask();
		PreviewCanvas.LayoutTransform = new ScaleTransform(4, 4);
	}

	private void AddMask()
	{
		PathGeometry mask = new PathGeometry();
		mask.FillRule = FillRule.Nonzero;
		mask.AddGeometry(new RectangleGeometry(new Rect(0, 0, 20, 32)));
		PathGeometry triangle = new PathGeometry();
		PathFigure figure = new PathFigure { StartPoint = new Point(0, 32), IsClosed = true };
		figure.Segments.Add(new LineSegment(new Point(20, 32), true));
		figure.Segments.Add(new LineSegment(new Point(10, 40), true));
		triangle.Figures.Add(figure);
		mask.AddGeometry(triangle);
		// 用背景色遮挡两侧凹口
		Path left = new Path { Fill = new SolidColorBrush(Color.FromRgb(32, 35, 38)), Data = Geometry.Parse("M0,32 L10,40 L0,40 Z") };
		Path right = new Path { Fill = new SolidColorBrush(Color.FromRgb(32, 35, 38)), Data = Geometry.Parse("M20,32 L10,40 L20,40 Z") };
		PreviewCanvas.Children.Add(left);
		PreviewCanvas.Children.Add(right);
	}

	private void AddPattern(string key, Color color)
	{
		AddPattern(key, color, PreviewCanvas);
	}

	private void AddPattern(string key, Color color, Canvas target)
	{
		// 以 20x40 的旗帜坐标绘制图案，和游戏内 20x40 像素的图案布局一致
		string data = key switch
		{
			"square_bottom_left" => "M0,34 H7 V40 H0 Z",
			"square_bottom_right" => "M13,34 H20 V40 H13 Z",
			"square_top_left" => "M0,0 H7 V6 H0 Z",
			"square_top_right" => "M13,0 H20 V6 H13 Z",
			"stripe_bottom" => "M0,34 H20 V40 H0 Z",
			"stripe_top" => "M0,0 H20 V6 H0 Z",
			"stripe_left" => "M0,0 H6 V40 H0 Z",
			"stripe_right" => "M14,0 H20 V40 H14 Z",
			"stripe_center" => "M9,0 H11 V40 H9 Z",
			"stripe_middle" => "M0,18 H20 V22 H0 Z",
			"stripe_downright" => "M0,0 H4 L20,32 V40 H16 L0,8 Z",
			"stripe_downleft" => "M16,0 H20 L0,32 V40 H4 L20,8 Z",
			"small_stripes" => "M2,0 H4 V40 H2 Z M8,0 H10 V40 H8 Z M14,0 H16 V40 H14 Z",
			"cross" => "M8,0 H12 V8 H20 V12 H12 V40 H8 V12 H0 V8 H8 Z",
			"straight_cross" => "M8,0 H12 V40 H8 Z M0,18 H20 V22 H0 Z",
			"triangle_bottom" => "M0,40 L10,20 L20,40 Z",
			"triangle_top" => "M0,0 H20 L10,20 Z",
			"triangles_bottom" => "M0,40 L7,28 L14,40 Z M7,40 L14,28 L20,40 Z",
			"triangles_top" => "M0,0 H14 L7,12 Z M7,0 H20 L14,12 Z",
			"diagonal_left" => "M0,0 H20 L0,40 Z",
			"diagonal_right" => "M0,0 H20 V40 Z",
			"diagonal_up_left" => "M0,0 L20,0 L20,40 Z",
			"diagonal_up_right" => "M0,0 L20,40 L0,40 Z",
			"circle" => "M10,6 A12,12 0 1 1 9.99,6 Z",
			"rhombus" => "M10,4 L20,20 L10,36 L0,20 Z",
			"half_vertical" => "M0,0 H10 V40 H0 Z",
			"half_vertical_right" => "M10,0 H20 V40 H10 Z",
			"half_horizontal" => "M0,0 H20 V20 H0 Z",
			"half_horizontal_bottom" => "M0,20 H20 V40 H0 Z",
			"border" => "M0,0 H20 V40 H0 Z M2,2 H18 V38 H2 Z",
			"gradient" => "M0,0 H20 V40 H0 Z",
			"gradient_up" => "M0,0 H20 V40 H0 Z",
			"bricks" => "M0,0 H20 V40 H0 Z M0,10 H20 V20 H0 Z M0,30 H20 V40 H0 Z",
			"curly_border" => "M0,0 H20 V40 H0 Z M2,2 H18 V38 H2 Z",
			"creeper" => "M4,10 H16 V30 H4 Z M8,14 H12 V26 H8 Z",
			"skull" => "M5,10 H15 V20 H12 V26 H8 V20 H5 Z M7,14 H9 V17 H7 Z M11,14 H13 V17 H11 Z",
			"flower" => "M10,12 A3,3 0 1 1 9.99,12 Z M10,20 A7,7 0 1 1 9.99,20 Z",
			"mojang" => "M4,4 H16 V36 H4 Z M7,8 H13 V14 H7 Z",
			"globe" => "M10,6 A14,14 0 1 1 9.99,6 Z M2,20 H18 M10,6 V34",
			"piglin" => "M4,8 H16 V26 H4 Z M7,14 H9 V18 H7 Z M11,14 H13 V18 H11 Z",
			"flow" => "M4,4 H8 V36 H4 Z M12,4 H16 V36 H12 Z",
			"guster" => "M2,10 H18 V14 H2 Z M6,18 H18 V22 H6 Z M2,26 H14 V30 H2 Z",
			_ => "M0,0 H20 V40 H0 Z"
		};
		Path path = new Path
		{
			Data = Geometry.Parse(data),
			Fill = new SolidColorBrush(color)
		};
		target.Children.Add(path);
	}

	private void BtnCopyGive_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.ClipboardSet(TextGive.Text);
		HintService.Hint("组件指令已复制");
	}

	private void BtnCopyLegacy_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.ClipboardSet(TextLegacy.Text);
		HintService.Hint("旧版指令已复制");
	}
}
