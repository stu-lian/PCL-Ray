using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace PCL;

public partial class PageToolsSummon : MyPageRight, IComponentConnector
{
	private readonly List<(string Slot, MyTextBox Input)> _equipment = new List<(string, MyTextBox)>();

	private bool _ready;

	public PageToolsSummon()
	{
		InitializeComponent();
		BuildEquipment();
		_ready = true;
		FillCategories();
		UpdateOutput();
	}

	private void BuildEquipment()
	{
		string[] slots = { "mainhand", "offhand", "head", "chest", "legs", "feet" };
		for (int index = 0; index < slots.Length; index++)
		{
			string slot = slots[index];
			int row = index;
			int column = 0;
			TextBlock label = new TextBlock
			{
				Text = McToolBuilders.EquipmentSlotName(slot),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 12, 10)
			};
			MyTextBox input = new MyTextBox
			{
				Height = 28,
				Margin = new Thickness(0, 0, 0, 10),
				HintText = "物品 ID，留空不装备"
			};
			input.TextChanged += (object _, TextChangedEventArgs _) => UpdateOutput();
			Grid.SetRow(label, row);
			Grid.SetColumn(label, column);
			Grid.SetRow(input, row);
			Grid.SetColumn(input, column + 1);
			GridEquipment.Children.Add(label);
			GridEquipment.Children.Add(input);
			_equipment.Add((slot, input));
		}
	}

	private void FillCategories()
	{
		ComboCategory.Items.Add(new ComboBoxItem { Content = "敌对生物", Tag = "hostile" });
		ComboCategory.Items.Add(new ComboBoxItem { Content = "被动生物", Tag = "passive" });
		ComboCategory.SelectedIndex = 0;
	}

	private void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready || ComboCategory.SelectedItem is not ComboBoxItem item || item.Tag is not string category)
		{
			return;
		}
		ComboEntity.Items.Clear();
		McEntity[] entities = category == "passive" ? McToolData.PassiveEntities : McToolData.HostileEntities;
		foreach (McEntity entity in entities)
		{
			ComboEntity.Items.Add(new ComboBoxItem { Content = entity.Chinese + "（" + entity.Key + "）", Tag = entity.Key });
		}
		if (ComboEntity.Items.Count > 0)
		{
			ComboEntity.SelectedIndex = 0;
		}
		UpdateOutput();
	}

	private void ComboEntity_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_ready)
		{
			UpdateOutput();
		}
	}

	private void TextPosition_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_ready)
		{
			UpdateOutput();
		}
	}

	private void TextName_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_ready)
		{
			UpdateOutput();
		}
	}

	private void TextNumber_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_ready)
		{
			UpdateOutput();
		}
	}

	private void TextPassenger_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (_ready)
		{
			UpdateOutput();
		}
	}

	private void Option_Change(object sender, bool user)
	{
		if (_ready)
		{
			UpdateOutput();
		}
	}

	private SummonOptions ReadOptions()
	{
		SummonOptions options = new SummonOptions
		{
			Entity = (ComboEntity.SelectedItem as ComboBoxItem)?.Tag as string ?? "zombie",
			X = TextX.Text ?? "~",
			Y = TextY.Text ?? "~",
			Z = TextZ.Text ?? "~",
			Name = TextName.Text ?? "",
			NameVisible = CheckNameVisible.Checked == true,
			Glowing = CheckGlowing.Checked == true,
			Invulnerable = CheckInvulnerable.Checked == true,
			NoAi = CheckNoAi.Checked == true,
			Silent = CheckSilent.Checked == true,
			Baby = CheckBaby.Checked == true,
			PassengerEnabled = CheckPassenger.Checked == true,
			PassengerEntity = TextPassenger.Text ?? "chicken",
			PassengerName = TextPassengerName.Text ?? "",
			PassengerGlowing = CheckPassengerGlowing.Checked == true,
			PassengerNoAi = CheckPassengerNoAi.Checked == true
		};
		options.FireSeconds = ParseInt(TextFire.Text, 0);
		options.Health = ParseDouble(TextHealth.Text, 0);
		foreach ((string slot, MyTextBox input) in _equipment)
		{
			string item = input.Text?.Trim() ?? "";
			if (item.Length > 0)
			{
				options.Equipment[slot] = item;
			}
		}
		return options;
	}

	private void UpdateOutput()
	{
		if (!_ready)
		{
			return;
		}
		TextOutput.Text = McToolBuilders.BuildSummon(ReadOptions());
	}

	private static int ParseInt(string? text, int fallback)
	{
		return int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
	}

	private static double ParseDouble(string? text, double fallback)
	{
		return double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : fallback;
	}

	private void BtnCopy_Click(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(TextOutput.Text))
		{
			ModBase.ClipboardSet(TextOutput.Text);
			HintService.Hint("召唤指令已复制");
		}
	}
}
