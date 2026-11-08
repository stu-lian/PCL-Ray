using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL;

public partial class PageToolsBeacon : MyPageRight, IComponentConnector
{
	private bool _ready;

	private bool _syncing;

	public PageToolsBeacon()
	{
		InitializeComponent();
		foreach (int layers in new[] { 1, 2, 3, 4 })
		{
			ComboLayers.Items.Add(new ComboBoxItem { Content = layers + " 层", Tag = layers });
		}
		ComboLayers.SelectedIndex = 0;
		BuildQuickColors();
		_ready = true;
		UpdateAll();
	}

	private void BuildQuickColors()
	{
		foreach (McDye dye in McToolData.Dyes)
		{
			Color color = Color.FromRgb((byte)(dye.Rgb >> 16), (byte)(dye.Rgb >> 8), (byte)dye.Rgb);
			Border swatch = new Border
			{
				Width = 34,
				Height = 34,
				CornerRadius = new CornerRadius(4),
				Background = new SolidColorBrush(color),
				Margin = new Thickness(0, 0, 6, 6),
				ToolTip = dye.Chinese,
				Cursor = Cursors.Hand,
				Tag = color
			};
			swatch.MouseLeftButtonUp += (object sender, MouseButtonEventArgs _) =>
			{
				if (((Border)sender).Tag is Color selected)
				{
					SetRgb(selected.R, selected.G, selected.B);
				}
			};
			PanelQuick.Children.Add(swatch);
		}
	}

	private int[] Target => new[] { SliderR.Value, SliderG.Value, SliderB.Value };

	private void Slider_Changed(object sender, bool user)
	{
		if (!_ready || _syncing)
		{
			return;
		}
		UpdateAll();
	}

	private void Option_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (_ready && !_syncing)
		{
			UpdateAll();
		}
	}

	private void TextHex_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!_ready || _syncing)
		{
			return;
		}
		string text = (TextHex.Text ?? "").Trim().TrimStart('#');
		if (text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
		{
			SetRgb((value >> 16) & 255, (value >> 8) & 255, value & 255);
		}
	}

	private void SetRgb(int r, int g, int b)
	{
		_syncing = true;
		try
		{
			SliderR.Value = Math.Clamp(r, 0, 255);
			SliderG.Value = Math.Clamp(g, 0, 255);
			SliderB.Value = Math.Clamp(b, 0, 255);
		}
		finally
		{
			_syncing = false;
		}
		UpdateAll();
	}

	private void BtnRandom_Click(object sender, MouseButtonEventArgs e)
	{
		Random random = new Random();
		SetRgb(random.Next(256), random.Next(256), random.Next(256));
	}

	private void UpdateAll()
	{
		int[] target = Target;
		LabR.Text = target[0].ToString();
		LabG.Text = target[1].ToString();
		LabB.Text = target[2].ToString();
		string hex = McToolBuilders.ColorHex(target);
		_syncing = true;
		TextHex.Text = hex;
		_syncing = false;
		TargetSwatch.Background = new SolidColorBrush(Color.FromRgb((byte)target[0], (byte)target[1], (byte)target[2]));
		LabTarget.Text = hex;
		int maxLayers = (ComboLayers.SelectedItem as ComboBoxItem)?.Tag as int? ?? 2;
		BeaconResult result = McToolBuilders.SolveBeacon(target, maxLayers, McToolData.BeaconGlassOrder);
		ResultSwatch.Background = new SolidColorBrush(Color.FromRgb((byte)result.Color[0], (byte)result.Color[1], (byte)result.Color[2]));
		LabResult.Text = McToolBuilders.ColorHex(result.Color);
		LabDelta.Text = result.Delta < 0.01
			? "完全一致，可以精确还原这个颜色。"
			: $"色差 {result.Delta:0.0}（数值越小越接近，受玻璃颜色限制不一定为 0）";
		RenderLayers(result);
		LabRecommend.Text = result.Layers.Count == 0
			? "没有可用的叠放方案。"
			: $"推荐：从下往上依次放置 {string.Join(" → ", result.Layers.Select(l => McToolData.FindDye(l).Chinese))} 玻璃。";
	}

	private void RenderLayers(BeaconResult result)
	{
		PanelLayers.Children.Clear();
		List<string> commands = new List<string>();
		for (int index = 0; index < result.Layers.Count; index++)
		{
			string key = result.Layers[index];
			McDye dye = McToolData.FindDye(key);
			Color color = Color.FromRgb((byte)(dye.Rgb >> 16), (byte)(dye.Rgb >> 8), (byte)dye.Rgb);
			Grid row = new Grid { Height = 40 };
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			TextBlock label = new TextBlock
			{
				Text = $"第 {index + 1} 层（从下往上）：{dye.Chinese}染色玻璃",
				VerticalAlignment = VerticalAlignment.Center
			};
			Border swatch = new Border
			{
				Width = 26,
				Height = 20,
				CornerRadius = new CornerRadius(3),
				Background = new SolidColorBrush(color),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(swatch, 1);
			row.Children.Add(label);
			row.Children.Add(swatch);
			PanelLayers.Children.Add(row);
			commands.Add($"/give @p minecraft:{key}_stained_glass 1");
		}
		TextCommands.Text = commands.Count == 0 ? "无需放置玻璃。" : string.Join(Environment.NewLine, commands);
	}

	private void BtnCopy_Click(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(TextCommands.Text))
		{
			ModBase.ClipboardSet(TextCommands.Text);
			HintService.Hint("玻璃指令已复制");
		}
	}
}
