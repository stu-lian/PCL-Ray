using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using fNbt;
using PCL.Core.UI;

namespace PCL;

public partial class PageToolsHardcoreReviver : MyPageRight, IComponentConnector
{
	private NbtFile? _file;

	private string _path = "";

	public PageToolsHardcoreReviver()
	{
		InitializeComponent();
	}

	private void BtnBrowse_Click(object sender, MouseButtonEventArgs e)
	{
		string path = SystemDialogs.SelectFile("Minecraft 存档数据 (*.dat)|*.dat|所有文件 (*.*)|*.*", "选择 level.dat", (string?)null);
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
			NbtCompound source = root.Get<NbtCompound>("Data") ?? root;
			string levelName = source.Get<NbtString>("LevelName")?.Value ?? "(未知)";
			string version = source.Get<NbtCompound>("Version") is NbtCompound versionTag
				? (versionTag.Get<NbtString>("Name")?.Value ?? "")
				: "";
			bool hardcore = (source.Get<NbtByte>("hardcore")?.Value ?? 0) != 0;
			NbtCompound? player = source.Get<NbtCompound>("Player");
			bool hasPlayer = player != null;
			int health = player?.Get<NbtFloat>("Health")?.Value is float h ? (int)h : 0;
			bool dead = health <= 0;
			int gameMode = source.Get<NbtInt>("GameType")?.Value ?? 0;
			LabInfo.Text = $"世界：{levelName}\n版本：{version}\n极限模式：{(hardcore ? "是" : "否")}\n玩家数据：{(hasPlayer ? $"存在（生命 {health}，游戏模式 {gameMode}）" : "不存在")}";
			LabStatus.Text = hardcore || dead
				? "检测到极限模式或死亡状态，可以选择下方的处理方式。"
				: "这个存档看起来没有触发极限锁档，仍然可以手动解除极限标记。";
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "读取 level.dat 失败", ModBase.LogLevel.Feedback, null, "读取 level.dat 失败");
		}
	}

	private bool Apply()
	{
		if (_file == null)
		{
			return false;
		}
		NbtCompound root = _file.RootTag;
		NbtCompound source = root.Get<NbtCompound>("Data") ?? root;
		if (RadioReset.IsChecked == true)
		{
			source.Remove("Player");
			if (source.Get("hardcore") is NbtByte hardcore)
			{
				hardcore.Value = 0;
			}
			return true;
		}
		if (RadioUnlock.IsChecked == true)
		{
			if (source.Get("hardcore") is NbtByte hardcore)
			{
				hardcore.Value = 0;
			}
			else
			{
				source.Add(new NbtByte("hardcore", 0));
			}
			NbtRead.SetInt(source, "GameType", 0);
		}
		NbtCompound? player = source.Get<NbtCompound>("Player");
		if (player != null)
		{
			NbtRead.SetFloat(player, "Health", 20f);
			if (player.Get("DeathTime") is NbtShort deathTime)
			{
				deathTime.Value = 0;
			}
			else
			{
				player.Add(new NbtShort("DeathTime", 0));
			}
			if (player.Get("HurtTime") is NbtShort hurtTime)
			{
				hurtTime.Value = 0;
			}
			else
			{
				player.Add(new NbtShort("HurtTime", 0));
			}
		}
		return true;
	}

	private void BtnSave_Click(object sender, MouseButtonEventArgs e)
	{
		if (_file == null || string.IsNullOrEmpty(_path))
		{
			return;
		}
		try
		{
			if (!Apply())
			{
				return;
			}
			string backup = _path + ".pclray-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			File.Copy(_path, backup, true);
			using (FileStream stream = File.Create(_path))
			{
				_file.SaveToStream(stream, NbtCompression.GZip);
			}
			HintService.Hint("已保存，原文件备份为 " + Path.GetFileName(backup), HintType.Success);
			Load(_path);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "保存 level.dat 失败", ModBase.LogLevel.Feedback, null, "保存 level.dat 失败");
		}
	}

	private void BtnSaveAs_Click(object sender, MouseButtonEventArgs e)
	{
		if (_file == null)
		{
			return;
		}
		string target = SystemDialogs.SelectSaveFile("level.dat 另存为", "level.dat", "Minecraft 存档数据 (*.dat)|*.dat", (string?)null);
		if (string.IsNullOrEmpty(target))
		{
			return;
		}
		try
		{
			if (!Apply())
			{
				return;
			}
			using (FileStream stream = File.Create(target))
			{
				_file.SaveToStream(stream, NbtCompression.GZip);
			}
			HintService.Hint("已另存为 " + Path.GetFileName(target), HintType.Success);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "另存 level.dat 失败", ModBase.LogLevel.Feedback, null, "另存 level.dat 失败");
		}
	}
}
