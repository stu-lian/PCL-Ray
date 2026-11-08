using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL;

public partial class PageToolsFirework : MyPageRight, IComponentConnector
{
	public sealed class ExplosionItem
	{
		public string Shape { get; set; } = "small_ball";
		public HashSet<uint> Colors { get; } = new HashSet<uint>();
		public HashSet<uint> FadeColors { get; } = new HashSet<uint>();
		public bool Trail { get; set; }
		public bool Twinkle { get; set; }
		public string Description => McToolBuilders.ShapeName(Shape) + " · " + ColorText(Colors) + (Trail ? " · 拖曳" : "") + (Twinkle ? " · 闪烁" : "");

		public static string ColorText(IEnumerable<uint> colors)
		{
			List<string> names = new List<string>();
			foreach (uint color in colors)
			{
				McDye dye = McToolData.Dyes.FirstOrDefault(d => d.Rgb == color);
				if (!string.IsNullOrEmpty(dye.Key))
				{
					names.Add(dye.Chinese);
				}
			}
			return names.Count == 0 ? "无颜色" : string.Join("、", names);
		}
	}

	private readonly List<ExplosionItem> _explosions = new List<ExplosionItem>();

	private bool _ready;

	private bool _syncing;

	public PageToolsFirework()
	{
		InitializeComponent();
		for (int flight = 1; flight <= 3; flight++)
		{
			ComboFlight.Items.Add(new ComboBoxItem { Content = flight + " 级（约 " + flight * 10 + " 秒）", Tag = flight });
		}
		ComboFlight.SelectedIndex = 1;
		foreach (string shape in new[] { "small_ball", "large_ball", "star", "creeper", "burst" })
		{
			ComboShape.Items.Add(new ComboBoxItem { Content = McToolBuilders.ShapeName(shape), Tag = shape });
		}
		ComboShape.SelectedIndex = 0;
		BuildColorPanel(PanelColors, true);
		BuildColorPanel(PanelFadeColors, false);
		_ready = true;
		_explosions.Add(new ExplosionItem());
		Refresh();
	}

	private void BuildColorPanel(Panel panel, bool main)
	{
		foreach (McDye dye in McToolData.Dyes)
		{
			Color color = Color.FromRgb((byte)(dye.Rgb >> 16), (byte)(dye.Rgb >> 8), (byte)dye.Rgb);
			Border swatch = new Border
			{
				Width = 30,
				Height = 30,
				CornerRadius = new CornerRadius(4),
				Margin = new Thickness(0, 0, 6, 6),
				Background = new SolidColorBrush(color),
				BorderThickness = new Thickness(2),
				BorderBrush = Brushes.Transparent,
				Tag = (dye.Rgb, main),
				ToolTip = dye.Chinese,
				Cursor = Cursors.Hand
			};
			swatch.MouseLeftButtonUp += (object sender, MouseButtonEventArgs _) => ToggleColor((Border)sender);
			panel.Children.Add(swatch);
		}
	}

	private void ToggleColor(Border swatch)
	{
		if (_syncing || swatch.Tag is not ValueTuple<uint, bool> info)
		{
			return;
		}
		ExplosionItem? explosion = Selected;
		if (explosion == null)
		{
			return;
		}
		HashSet<uint> target = info.Item2 ? explosion.Colors : explosion.FadeColors;
		if (!target.Remove(info.Item1))
		{
			target.Add(info.Item1);
		}
		swatch.BorderBrush = target.Contains(info.Item1)
			? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CC2FF"))
			: Brushes.Transparent;
		RefreshList();
		RefreshOutputs();
	}

	private ExplosionItem? Selected => ListExplosions.SelectedIndex >= 0 && ListExplosions.SelectedIndex < _explosions.Count
		? _explosions[ListExplosions.SelectedIndex]
		: null;

	private void Option_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (_ready)
		{
			RefreshOutputs();
		}
	}

	private void Option_Change(object sender, bool user)
	{
		if (!_ready)
		{
			return;
		}
		if (Selected is ExplosionItem explosion)
		{
			explosion.Trail = CheckTrail.Checked == true;
			explosion.Twinkle = CheckTwinkle.Checked == true;
			RefreshList();
		}
		RefreshOutputs();
	}

	private void Shape_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready || _syncing || Selected is not ExplosionItem explosion)
		{
			return;
		}
		explosion.Shape = (ComboShape.SelectedItem as ComboBoxItem)?.Tag as string ?? "small_ball";
		RefreshList();
		RefreshOutputs();
	}

	private void ListExplosions_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}
		_syncing = true;
		try
		{
			if (Selected is ExplosionItem explosion)
			{
				for (int index = 0; index < ComboShape.Items.Count; index++)
				{
					if ((ComboShape.Items[index] as ComboBoxItem)?.Tag as string == explosion.Shape)
					{
						ComboShape.SelectedIndex = index;
						break;
					}
				}
				CheckTrail.Checked = explosion.Trail;
				CheckTwinkle.Checked = explosion.Twinkle;
				ApplySwatchState(PanelColors, explosion.Colors);
				ApplySwatchState(PanelFadeColors, explosion.FadeColors);
			}
		}
		finally
		{
			_syncing = false;
		}
	}

	private void ApplySwatchState(Panel panel, HashSet<uint> colors)
	{
		foreach (object child in panel.Children)
		{
			if (child is Border swatch && swatch.Tag is ValueTuple<uint, bool> info)
			{
				swatch.BorderBrush = colors.Contains(info.Item1)
					? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CC2FF"))
					: Brushes.Transparent;
			}
		}
	}

	private void BtnAddExplosion_Click(object sender, MouseButtonEventArgs e)
	{
		_explosions.Add(new ExplosionItem());
		Refresh();
		ListExplosions.SelectedIndex = _explosions.Count - 1;
	}

	private void BtnDeleteExplosion_Click(object sender, MouseButtonEventArgs e)
	{
		int index = ListExplosions.SelectedIndex;
		if (index < 0 || _explosions.Count <= 1)
		{
			return;
		}
		_explosions.RemoveAt(index);
		Refresh();
	}

	private void Refresh()
	{
		RefreshList();
		if (_explosions.Count > 0)
		{
			ListExplosions.SelectedIndex = Math.Min(Math.Max(ListExplosions.SelectedIndex, 0), _explosions.Count - 1);
		}
		RefreshOutputs();
	}

	private void RefreshList()
	{
		int index = ListExplosions.SelectedIndex;
		ListExplosions.ItemsSource = null;
		ListExplosions.ItemsSource = _explosions;
		ListExplosions.SelectedIndex = index;
	}

	private FireworkOptions BuildOptions()
	{
		FireworkOptions options = new FireworkOptions
		{
			Flight = (ComboFlight.SelectedItem as ComboBoxItem)?.Tag as int? ?? 2
		};
		foreach (ExplosionItem item in _explosions)
		{
			options.Explosions.Add(new FireworkExplosion
			{
				Shape = item.Shape,
				Colors = item.Colors.ToList(),
				FadeColors = item.FadeColors.ToList(),
				Trail = item.Trail,
				Twinkle = item.Twinkle
			});
		}
		return options;
	}

	private void RefreshOutputs()
	{
		if (!_ready)
		{
			return;
		}
		FireworkOptions options = BuildOptions();
		TextRocket.Text = McToolBuilders.BuildFireworkRocket(options);
		TextStars.Text = string.Join(Environment.NewLine, options.Explosions.Select(McToolBuilders.BuildFireworkStar));
		TextCrafting.Text = string.Join(Environment.NewLine, McToolBuilders.FireworkCraftingSteps(options));
	}

	private void BtnCopyRocket_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.ClipboardSet(TextRocket.Text);
		HintService.Hint("火箭指令已复制");
	}

	private void BtnCopyStars_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.ClipboardSet(TextStars.Text);
		HintService.Hint("烟火之星指令已复制");
	}
}
