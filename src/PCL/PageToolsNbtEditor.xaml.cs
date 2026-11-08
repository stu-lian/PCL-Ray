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

public partial class PageToolsNbtEditor : MyPageRight, IComponentConnector
{
	private sealed class NodeInfo
	{
		public NbtTag Tag { get; init; }
		public string Path { get; init; } = "";
		public string Label { get; init; } = "";
	}

	private static readonly NbtTagType[] EditableTypes =
	{
		NbtTagType.Byte, NbtTagType.Short, NbtTagType.Int, NbtTagType.Long,
		NbtTagType.Float, NbtTagType.Double, NbtTagType.String
	};

	private NbtFile? _file;

	private string _path = "";

	private NodeInfo? _selected;

	public PageToolsNbtEditor()
	{
		InitializeComponent();
		foreach (NbtTagType type in EditableTypes)
		{
			ComboNewType.Items.Add(new ComboBoxItem { Content = type.ToString(), Tag = type });
		}
		ComboNewType.SelectedIndex = 2;
	}

	private void BtnOpen_Click(object sender, MouseButtonEventArgs e)
	{
		string path = SystemDialogs.SelectFile("NBT 文件 (*.dat;*.nbt)|*.dat;*.nbt|所有文件 (*.*)|*.*", "打开 NBT 文件", (string?)null);
		if (!string.IsNullOrEmpty(path))
		{
			Load(path);
		}
	}

	private void BtnClose_Click(object sender, MouseButtonEventArgs e)
	{
		_file = null;
		_path = "";
		_selected = null;
		TreeNbt.Items.Clear();
		TextPath.Text = "";
		LabInfo.Text = "";
		LabNodePath.Text = "";
		LabNodeType.Text = "";
		TextValue.Text = "";
		TextValue.IsEnabled = false;
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
			LabInfo.Text = $"根标签：{file.RootTag.Name}　子标签：{file.RootTag.Count}　压缩：{file.FileCompression}";
			BuildTree();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "读取 NBT 失败", ModBase.LogLevel.Feedback, null, "读取 NBT 失败");
		}
	}

	private void BuildTree()
	{
		TreeNbt.Items.Clear();
		if (_file == null)
		{
			return;
		}
		TreeViewItem root = BuildNode(_file.RootTag, _file.RootTag.Name, true);
		root.IsExpanded = true;
		TreeNbt.Items.Add(root);
	}

	private TreeViewItem BuildNode(NbtTag tag, string label, bool expanded)
	{
		TreeViewItem item = new TreeViewItem
		{
			Header = Describe(tag, label),
			Tag = new NodeInfo { Tag = tag, Path = tag.Path, Label = label },
			IsExpanded = expanded
		};
		if (tag is NbtCompound compound)
		{
			foreach (NbtTag child in compound.Tags)
			{
				item.Items.Add(BuildNode(child, child.Name, false));
			}
		}
		else if (tag is NbtList list)
		{
			for (int index = 0; index < list.Count; index++)
			{
				item.Items.Add(BuildNode(list[index], "[" + index + "]", false));
			}
		}
		return item;
	}

	private static string Describe(NbtTag tag, string label)
	{
		string name = string.IsNullOrEmpty(label) ? tag.Name : label;
		string value = tag.TagType switch
		{
			NbtTagType.Byte => tag.ByteValue.ToString(CultureInfo.InvariantCulture),
			NbtTagType.Short => tag.ShortValue.ToString(CultureInfo.InvariantCulture),
			NbtTagType.Int => tag.IntValue.ToString(CultureInfo.InvariantCulture),
			NbtTagType.Long => tag.LongValue.ToString(CultureInfo.InvariantCulture),
			NbtTagType.Float => tag.FloatValue.ToString("0.######", CultureInfo.InvariantCulture),
			NbtTagType.Double => tag.DoubleValue.ToString("0.######", CultureInfo.InvariantCulture),
			NbtTagType.String => "\"" + (tag.StringValue.Length > 60 ? tag.StringValue.Substring(0, 60) + "..." : tag.StringValue) + "\"",
			NbtTagType.Compound => "{" + ((NbtCompound)tag).Count + "}",
			NbtTagType.List => "[" + ((NbtList)tag).Count + "]",
			NbtTagType.ByteArray => "<" + tag.ByteArrayValue.Length + " bytes>",
			NbtTagType.IntArray => "<" + tag.IntArrayValue.Length + " ints>",
			NbtTagType.LongArray => "<" + tag.LongArrayValue.Length + " longs>",
			_ => ""
		};
		return string.IsNullOrEmpty(name) ? value : name + ": " + value;
	}

	private void TreeNbt_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		if (e.NewValue is not TreeViewItem item || item.Tag is not NodeInfo info)
		{
			_selected = null;
			TextValue.IsEnabled = false;
			return;
		}
		_selected = info;
		LabNodePath.Text = "路径：" + info.Path;
		LabNodeType.Text = "类型：" + info.Tag.TagType;
		bool editable = EditableTypes.Contains(info.Tag.TagType);
		TextValue.IsEnabled = editable;
		TextValue.Text = editable ? TagToString(info.Tag) : "";
	}

	private static string TagToString(NbtTag tag) => tag.TagType switch
	{
		NbtTagType.Byte => tag.ByteValue.ToString(CultureInfo.InvariantCulture),
		NbtTagType.Short => tag.ShortValue.ToString(CultureInfo.InvariantCulture),
		NbtTagType.Int => tag.IntValue.ToString(CultureInfo.InvariantCulture),
		NbtTagType.Long => tag.LongValue.ToString(CultureInfo.InvariantCulture),
		NbtTagType.Float => tag.FloatValue.ToString("0.######", CultureInfo.InvariantCulture),
		NbtTagType.Double => tag.DoubleValue.ToString("0.######", CultureInfo.InvariantCulture),
		NbtTagType.String => tag.StringValue,
		_ => ""
	};

	private void BtnApply_Click(object sender, MouseButtonEventArgs e)
	{
		if (_selected == null)
		{
			return;
		}
		if (!TryParseValue(_selected.Tag.TagType, TextValue.Text, out object parsed))
		{
			HintService.Hint("值格式不正确", HintType.Error);
			return;
		}
		bool ok = true;
		switch (_selected.Tag)
		{
			case NbtByte tag: ok = byte.TryParse(parsed.ToString(), out byte b); if (ok) tag.Value = b; break;
			case NbtShort tag: ok = short.TryParse(parsed.ToString(), out short s); if (ok) tag.Value = s; break;
			case NbtInt tag: ok = int.TryParse(parsed.ToString(), out int i); if (ok) tag.Value = i; break;
			case NbtLong tag: ok = long.TryParse(parsed.ToString(), out long l); if (ok) tag.Value = l; break;
			case NbtFloat tag: ok = float.TryParse(parsed.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float f); if (ok) tag.Value = f; break;
			case NbtDouble tag: ok = double.TryParse(parsed.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d); if (ok) tag.Value = d; break;
			case NbtString tag: tag.Value = TextValue.Text ?? ""; break;
			default: ok = false; break;
		}
		if (!ok)
		{
			HintService.Hint("值格式不正确", HintType.Error);
			return;
		}
		BuildTree();
		HintService.Hint("已应用修改", HintType.Success);
	}

	private static bool TryParseValue(NbtTagType type, string? text, out object value)
	{
		value = 0;
		string input = (text ?? "").Trim();
		switch (type)
		{
			case NbtTagType.Byte:
				if (byte.TryParse(input, out byte b)) { value = b; return true; }
				return false;
			case NbtTagType.Short:
				if (short.TryParse(input, out short s)) { value = s; return true; }
				return false;
			case NbtTagType.Int:
				if (int.TryParse(input, out int i)) { value = i; return true; }
				return false;
			case NbtTagType.Long:
				if (long.TryParse(input, out long l)) { value = l; return true; }
				return false;
			case NbtTagType.Float:
				if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) { value = f; return true; }
				return false;
			case NbtTagType.Double:
				if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) { value = d; return true; }
				return false;
			case NbtTagType.String:
				value = input;
				return true;
			default:
				return false;
		}
	}

	private void BtnAdd_Click(object sender, MouseButtonEventArgs e)
	{
		if (_selected == null)
		{
			HintService.Hint("请先在左侧选择一个 Compound 或 List 节点", HintType.Info);
			return;
		}
		NbtTagType type = (ComboNewType.SelectedItem as ComboBoxItem)?.Tag as NbtTagType? ?? NbtTagType.Int;
		if (_selected.Tag is NbtCompound compound)
		{
			string name = (TextNewName.Text ?? "").Trim();
			if (string.IsNullOrEmpty(name))
			{
				HintService.Hint("请填写键名", HintType.Error);
				return;
			}
			if (compound.Contains(name))
			{
				HintService.Hint("这个键已经存在", HintType.Error);
				return;
			}
			NbtTag? tag = CreateTag(type, name, TextNewValue.Text);
			if (tag != null)
			{
				compound.Add(tag);
			}
		}
		else if (_selected.Tag is NbtList list)
		{
			NbtTag? tag = CreateTag(list.ListType == NbtTagType.Unknown ? type : list.ListType, "", TextNewValue.Text);
			if (tag != null)
			{
				list.Add(tag);
			}
		}
		else
		{
			HintService.Hint("只有 Compound 或 List 可以添加子节点", HintType.Error);
			return;
		}
		BuildTree();
	}

	private static NbtTag? CreateTag(NbtTagType type, string name, string? text)
	{
		string input = (text ?? "").Trim();
		switch (type)
		{
			case NbtTagType.Byte: return new NbtByte(name, byte.TryParse(input, out byte b) ? b : (byte)0);
			case NbtTagType.Short: return new NbtShort(name, short.TryParse(input, out short s) ? s : (short)0);
			case NbtTagType.Int: return new NbtInt(name, int.TryParse(input, out int i) ? i : 0);
			case NbtTagType.Long: return new NbtLong(name, long.TryParse(input, out long l) ? l : 0L);
			case NbtTagType.Float: return new NbtFloat(name, float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f);
			case NbtTagType.Double: return new NbtDouble(name, double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0d);
			case NbtTagType.String: return new NbtString(name, text ?? "");
			case NbtTagType.Compound: return new NbtCompound(name);
			case NbtTagType.List: return new NbtList(name, NbtTagType.Compound);
			default: return null;
		}
	}

	private void BtnDelete_Click(object sender, MouseButtonEventArgs e)
	{
		if (_selected == null)
		{
			return;
		}
		NbtTag tag = _selected.Tag;
		if (tag is NbtCompound compound)
		{
			foreach (NbtTag child in compound.Tags.ToList())
			{
				compound.Remove(child);
			}
			BuildTree();
			return;
		}
		if (tag.Parent is NbtCompound parent)
		{
			parent.Remove(tag);
		}
		else if (tag.Parent is NbtList list)
		{
			list.Remove(tag);
		}
		_selected = null;
		TextValue.Text = "";
		TextValue.IsEnabled = false;
		LabNodePath.Text = "";
		LabNodeType.Text = "";
		BuildTree();
	}

	private void BtnExportGzip_Click(object sender, MouseButtonEventArgs e) => Export(NbtCompression.GZip);

	private void BtnExportPlain_Click(object sender, MouseButtonEventArgs e) => Export(NbtCompression.None);

	private void Export(NbtCompression compression)
	{
		if (_file == null)
		{
			return;
		}
		string target = SystemDialogs.SelectSaveFile("导出 NBT", Path.GetFileName(_path), "NBT 文件 (*.dat;*.nbt)|*.dat;*.nbt", (string?)null);
		if (string.IsNullOrEmpty(target))
		{
			return;
		}
		try
		{
			using (FileStream stream = File.Create(target))
			{
				_file.SaveToStream(stream, compression);
			}
			HintService.Hint("已导出到 " + Path.GetFileName(target), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "导出 NBT 失败", ModBase.LogLevel.Feedback, null, "导出 NBT 失败");
		}
	}
}
