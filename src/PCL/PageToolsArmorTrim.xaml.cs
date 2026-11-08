using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Tools;

namespace PCL;

public partial class PageToolsArmorTrim : MyPageRight, IComponentConnector
{
	private bool _ready;

	public PageToolsArmorTrim()
	{
		InitializeComponent();
		foreach ((string key, string chinese, string note) in McToolData.ArmorMaterials)
		{
			string text = string.IsNullOrEmpty(note) ? chinese : chinese + "（" + note + "）";
			ComboArmor.Items.Add(new ComboBoxItem { Content = text, ToolTip = text, Tag = key });
		}
		MyComboBox[] patterns = { ComboHelmetPattern, ComboChestPattern, ComboLeggingsPattern, ComboBootsPattern };
		foreach (MyComboBox combo in patterns)
		{
			foreach (TrimPattern pattern in McToolData.TrimPatterns)
			{
				combo.Items.Add(new ComboBoxItem { Content = pattern.Chinese, ToolTip = pattern.Chinese, Tag = pattern.Key });
			}
		}
		MyComboBox[] materials = { ComboHelmetMaterial, ComboChestMaterial, ComboLeggingsMaterial, ComboBootsMaterial };
		foreach (MyComboBox combo in materials)
		{
			foreach (TrimMaterial material in McToolData.TrimMaterials)
			{
				combo.Items.Add(new ComboBoxItem { Content = material.Chinese, ToolTip = material.Chinese, Tag = material.Key });
			}
		}
		ComboArmor.SelectedIndex = 4;
		foreach (MyComboBox combo in patterns) combo.SelectedIndex = 0;
		foreach (MyComboBox combo in materials) combo.SelectedIndex = 4;
		CheckHelmet.Checked = true;
		CheckChestplate.Checked = true;
		CheckLeggings.Checked = true;
		CheckBoots.Checked = true;
		_ready = true;
		Refresh();
	}

	private string ArmorKey => (ComboArmor.SelectedItem as ComboBoxItem)?.Tag as string ?? "iron";

	private static string KeyOf(MyComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Tag as string ?? "";

	private void Option_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (_ready) Refresh();
	}

	private void Option_Change(object sender, bool user)
	{
		if (_ready) Refresh();
	}

	private void Target_Changed(object sender, TextChangedEventArgs e)
	{
		if (_ready) Refresh();
	}

	private void Refresh()
	{
		List<string> commands = new List<string>();
		List<string> parts = new List<string>();
		AddPart("helmet", "头盔", CheckHelmet, ComboHelmetPattern, ComboHelmetMaterial, commands, parts);
		AddPart("chestplate", "胸甲", CheckChestplate, ComboChestPattern, ComboChestMaterial, commands, parts);
		AddPart("leggings", "护腿", CheckLeggings, ComboLeggingsPattern, ComboLeggingsMaterial, commands, parts);
		AddPart("boots", "靴子", CheckBoots, ComboBootsPattern, ComboBootsMaterial, commands, parts);

		TextCommands.Text = commands.Count == 0 ? "请至少勾选一个部位。" : string.Join(Environment.NewLine, commands);
		LabPatternInfo.Text = parts.Count == 0 ? "" : string.Join("　｜　", parts);

		List<string> palettes = new List<string>();
		foreach (MyComboBox combo in new[] { ComboHelmetMaterial, ComboChestMaterial, ComboLeggingsMaterial, ComboBootsMaterial })
		{
			TrimMaterial material = McToolData.TrimMaterials.FirstOrDefault(m => m.Key == KeyOf(combo));
			if (!string.IsNullOrEmpty(material.Key) && !palettes.Contains(string.Join(" 或 ", material.Palette)))
			{
				palettes.Add(string.Join(" 或 ", material.Palette));
			}
		}
		LabRecipe.Text = palettes.Count == 0
			? ""
			: "锻造台：盔甲 + 对应纹饰锻造模板 + " + string.Join("；或 ", palettes) + "\n\n纹饰图案决定形状，纹饰材料决定颜色；模板可在各种宝箱与考古点找到。";
		DrawPreview();
	}

	private void AddPart(string piece, string chinese, MyCheckBox check, MyComboBox pattern, MyComboBox material,
		List<string> commands, List<string> parts)
	{
		if (check.Checked != true) return;
		string patternKey = KeyOf(pattern);
		string materialKey = KeyOf(material);
		if (string.IsNullOrEmpty(patternKey) || string.IsNullOrEmpty(materialKey)) return;
		commands.Add(McToolBuilders.BuildTrimGive(piece, patternKey, materialKey, ArmorKey, TextTarget.Text));
		parts.Add(chinese + " · " + PatternName(patternKey) + " · " + MaterialName(materialKey));
	}

	private static string PatternName(string key)
	{
		TrimPattern pattern = McToolData.TrimPatterns.FirstOrDefault(p => p.Key == key);
		return string.IsNullOrEmpty(pattern.Key) ? key : pattern.Chinese;
	}

	private static string MaterialName(string key)
	{
		TrimMaterial material = McToolData.TrimMaterials.FirstOrDefault(m => m.Key == key);
		return string.IsNullOrEmpty(material.Key) ? key : material.Chinese;
	}

	private void DrawPreview()
	{
		if (PreviewImage.Source is not System.Windows.Media.Imaging.WriteableBitmap bitmap || bitmap.PixelWidth != 96 || bitmap.PixelHeight != 192)
		{
			bitmap = new System.Windows.Media.Imaging.WriteableBitmap(96, 192, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null);
			PreviewImage.Source = bitmap;
		}
		ArmorPreviewRenderer.Render(
			bitmap,
			ArmorKey,
			Piece(CheckHelmet, ComboHelmetPattern, ComboHelmetMaterial),
			Piece(CheckChestplate, ComboChestPattern, ComboChestMaterial),
			Piece(CheckLeggings, ComboLeggingsPattern, ComboLeggingsMaterial),
			Piece(CheckBoots, ComboBootsPattern, ComboBootsMaterial));
	}

	private static ArmorPieceOptions Piece(MyCheckBox check, MyComboBox pattern, MyComboBox material)
	{
		return new ArmorPieceOptions
		{
			Enabled = check.Checked == true,
			Pattern = KeyOf(pattern),
			Material = KeyOf(material)
		};
	}

	private void BtnCopy_Click(object sender, MouseButtonEventArgs e)
	{
		ModBase.ClipboardSet(TextCommands.Text);
		HintService.Hint("盔甲指令已复制");
	}
}
