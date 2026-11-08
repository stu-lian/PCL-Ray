using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using fNbt;
using PCL.Core.UI;

namespace PCL;

public partial class PageToolsPlayerData : MyPageRight, IComponentConnector
{
	public sealed class InventoryItem
	{
		public int Slot { get; init; }
		public string Name { get; init; } = "";
		public string SlotText => "槽位 " + Slot;
		public string Count { get; init; } = "";
	}

	private readonly List<InventoryItem> _inventory = new List<InventoryItem>();

	private NbtFile? _file;

	private string _path = "";

	public PageToolsPlayerData()
	{
		InitializeComponent();
		foreach ((string name, int mode) in new[] { ("生存模式", 0), ("创造模式", 1), ("冒险模式", 2), ("旁观模式", 3) })
		{
			ComboGameMode.Items.Add(new ComboBoxItem { Content = name, Tag = mode });
		}
		ComboGameMode.SelectedIndex = 0;
	}

	private void BtnBrowse_Click(object sender, MouseButtonEventArgs e)
	{
		string path = SystemDialogs.SelectFile("Minecraft 玩家数据 (*.dat)|*.dat|所有文件 (*.*)|*.*", "选择玩家数据文件", (string?)null);
		if (!string.IsNullOrEmpty(path))
		{
			Load(path);
		}
	}

	private void BtnReload_Click(object sender, MouseButtonEventArgs e)
	{
		if (!string.IsNullOrEmpty(_path) && File.Exists(_path))
		{
			Load(_path);
		}
	}

	private void Load(string path)
	{
		try
		{
			NbtFile file = new NbtFile();
			using (FileStream stream = File.OpenRead(path))
			{
				file.LoadFromStream(stream, NbtCompression.AutoDetect);
			}
			_file = file;
			_path = path;
			TextPath.Text = path;
			NbtCompound root = file.RootTag;
			LabInfo.Text = $"文件：{Path.GetFileName(path)}　根标签：{root.Name}　标签数：{root.Count}";
			PanEditor.Visibility = Visibility.Visible;
			int gameMode = NbtRead.GetInt(root, "playerGameType", NbtRead.GetInt(root, "GameType", 0));
			for (int index = 0; index < ComboGameMode.Items.Count; index++)
			{
				if ((ComboGameMode.Items[index] as ComboBoxItem)?.Tag as int? == gameMode)
				{
					ComboGameMode.SelectedIndex = index;
					break;
				}
			}
			TextHealth.Text = NbtRead.GetFloat(root, "Health", 20).ToString("0.##", CultureInfo.InvariantCulture);
			TextFood.Text = NbtRead.GetInt(root, "foodLevel", 20).ToString(CultureInfo.InvariantCulture);
			TextLevel.Text = NbtRead.GetInt(root, "XpLevel", 0).ToString(CultureInfo.InvariantCulture);
			TextXp.Text = NbtRead.GetFloat(root, "XpP", 0).ToString("0.###", CultureInfo.InvariantCulture);
			TextSlot.Text = NbtRead.GetInt(root, "SelectedItemSlot", 0).ToString(CultureInfo.InvariantCulture);
			NbtList? pos = root.Get<NbtList>("Pos");
			TextX.Text = pos != null && pos.Count >= 3 ? pos[0].DoubleValue.ToString("0.##", CultureInfo.InvariantCulture) : "0";
			TextY.Text = pos != null && pos.Count >= 3 ? pos[1].DoubleValue.ToString("0.##", CultureInfo.InvariantCulture) : "64";
			TextZ.Text = pos != null && pos.Count >= 3 ? pos[2].DoubleValue.ToString("0.##", CultureInfo.InvariantCulture) : "0";
			TextSpawnX.Text = NbtRead.GetInt(root, "SpawnX", 0).ToString(CultureInfo.InvariantCulture);
			TextSpawnY.Text = NbtRead.GetInt(root, "SpawnY", 64).ToString(CultureInfo.InvariantCulture);
			TextSpawnZ.Text = NbtRead.GetInt(root, "SpawnZ", 0).ToString(CultureInfo.InvariantCulture);
			CheckSpawnForced.Checked = NbtRead.GetByte(root, "SpawnForced", 0) != 0;
			LoadInventory(root);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "读取玩家数据失败", ModBase.LogLevel.Feedback, null, "读取玩家数据失败");
		}
	}

	private void LoadInventory(NbtCompound root)
	{
		_inventory.Clear();
		NbtList? list = root.Get<NbtList>("Inventory");
		if (list != null)
		{
			foreach (NbtTag tag in list)
			{
				if (tag is not NbtCompound item)
				{
					continue;
				}
				_inventory.Add(new InventoryItem
				{
					Slot = item.Get<NbtByte>("Slot")?.Value ?? 0,
					Name = item.Get<NbtString>("id")?.Value ?? "未知物品",
					Count = "×" + (item.Get<NbtByte>("Count")?.Value ?? item.Get<NbtInt>("count")?.Value ?? 1)
				});
			}
		}
		ListInventory.ItemsSource = null;
		ListInventory.ItemsSource = _inventory;
	}

	private void BtnDeleteItem_Click(object sender, MouseButtonEventArgs e)
	{
		if (_file == null || ListInventory.SelectedIndex < 0)
		{
			return;
		}
		InventoryItem selected = _inventory[ListInventory.SelectedIndex];
		NbtList? list = _file.RootTag.Get<NbtList>("Inventory");
		if (list == null)
		{
			return;
		}
		for (int index = list.Count - 1; index >= 0; index--)
		{
			if (list[index] is NbtCompound item && (item.Get<NbtByte>("Slot")?.Value ?? -1) == selected.Slot)
			{
				list.RemoveAt(index);
			}
		}
		LoadInventory(_file.RootTag);
	}

	private void BtnClearInventory_Click(object sender, MouseButtonEventArgs e)
	{
		_file?.RootTag.Get<NbtList>("Inventory")?.Clear();
		if (_file != null)
		{
			LoadInventory(_file.RootTag);
		}
	}

	private void ApplyEdits()
	{
		if (_file == null)
		{
			return;
		}
		NbtCompound root = _file.RootTag;
		int gameMode = (ComboGameMode.SelectedItem as ComboBoxItem)?.Tag as int? ?? 0;
		NbtRead.SetInt(root, "playerGameType", gameMode);
		if (root.Get("GameType") != null)
		{
			NbtRead.SetInt(root, "GameType", gameMode);
		}
		NbtRead.SetFloat(root, "Health", ParseFloat(TextHealth.Text, 20));
		NbtRead.SetInt(root, "foodLevel", ParseInt(TextFood.Text, 20));
		NbtRead.SetInt(root, "XpLevel", ParseInt(TextLevel.Text, 0));
		NbtRead.SetFloat(root, "XpP", Math.Clamp(ParseFloat(TextXp.Text, 0), 0, 0.999f));
		NbtRead.SetInt(root, "SelectedItemSlot", ParseInt(TextSlot.Text, 0));
		NbtList pos = root.Get<NbtList>("Pos") ?? new NbtList("Pos", NbtTagType.Double);
		pos.Clear();
		pos.Add(new NbtDouble(ParseDouble(TextX.Text, 0)));
		pos.Add(new NbtDouble(ParseDouble(TextY.Text, 64)));
		pos.Add(new NbtDouble(ParseDouble(TextZ.Text, 0)));
		if (root.Get("Pos") == null)
		{
			root.Add(pos);
		}
		NbtRead.SetInt(root, "SpawnX", ParseInt(TextSpawnX.Text, 0));
		NbtRead.SetInt(root, "SpawnY", ParseInt(TextSpawnY.Text, 64));
		NbtRead.SetInt(root, "SpawnZ", ParseInt(TextSpawnZ.Text, 0));
		NbtRead.SetByte(root, "SpawnForced", CheckSpawnForced.Checked == true ? (byte)1 : (byte)0);
	}

	private void BtnSave_Click(object sender, MouseButtonEventArgs e)
	{
		if (_file == null || string.IsNullOrEmpty(_path))
		{
			return;
		}
		try
		{
			ApplyEdits();
			string backup = _path + ".pclray-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			File.Copy(_path, backup, true);
			using (FileStream stream = File.Create(_path))
			{
				_file.SaveToStream(stream, NbtCompression.GZip);
			}
			HintService.Hint("已保存，原文件备份为 " + Path.GetFileName(backup), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "保存玩家数据失败", ModBase.LogLevel.Feedback, null, "保存玩家数据失败");
		}
	}

	private void BtnSaveAs_Click(object sender, MouseButtonEventArgs e)
	{
		if (_file == null)
		{
			return;
		}
		string target = SystemDialogs.SelectSaveFile("玩家数据另存为", Path.GetFileName(_path), "Minecraft 玩家数据 (*.dat)|*.dat", (string?)null);
		if (string.IsNullOrEmpty(target))
		{
			return;
		}
		try
		{
			ApplyEdits();
			using (FileStream stream = File.Create(target))
			{
				_file.SaveToStream(stream, NbtCompression.GZip);
			}
			HintService.Hint("已另存为 " + Path.GetFileName(target), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "另存玩家数据失败", ModBase.LogLevel.Feedback, null, "另存玩家数据失败");
		}
	}

	private static int ParseInt(string? text, int fallback) => int.TryParse((text ?? "").Trim(), out int value) ? value : fallback;

	private static float ParseFloat(string? text, float fallback) => float.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;

	private static double ParseDouble(string? text, double fallback) => double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : fallback;
}

internal static class NbtRead
{
	public static int GetInt(NbtCompound root, string name, int fallback)
	{
		NbtTag? tag = root.Get(name);
		return tag == null ? fallback : tag.IntValue;
	}

	public static float GetFloat(NbtCompound root, string name, float fallback)
	{
		NbtTag? tag = root.Get(name);
		return tag == null ? fallback : tag.FloatValue;
	}

	public static byte GetByte(NbtCompound root, string name, byte fallback)
	{
		NbtTag? tag = root.Get(name);
		return tag == null ? fallback : tag.ByteValue;
	}

	public static void SetInt(NbtCompound root, string name, int value)
	{
		if (root.Get(name) is NbtInt existing)
		{
			existing.Value = value;
		}
		else
		{
			root.Add(new NbtInt(name, value));
		}
	}

	public static void SetFloat(NbtCompound root, string name, float value)
	{
		if (root.Get(name) is NbtFloat existing)
		{
			existing.Value = value;
		}
		else
		{
			root.Add(new NbtFloat(name, value));
		}
	}

	public static void SetByte(NbtCompound root, string name, byte value)
	{
		if (root.Get(name) is NbtByte existing)
		{
			existing.Value = value;
		}
		else
		{
			root.Add(new NbtByte(name, value));
		}
	}
}
