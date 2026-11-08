using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using NAudio;
using NAudio.Wave;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public static class ModMusic
{
	public enum MusicStates
	{
		Stop,
		Play,
		Pause
	}

	public static WaveOutEvent musicNAudio;

	private static string musicCurrent = "";

	public static List<string> musicWaitingList;

	public static List<string> musicAllList;

	public static MusicStates MusicState
	{
		get
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Invalid comparison between Unknown and I4
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			if (musicNAudio == null)
			{
				return MusicStates.Stop;
			}
			if ((int)musicNAudio.PlaybackState != 2)
			{
				if ((int)musicNAudio.PlaybackState != 0)
				{
					return MusicStates.Play;
				}
				return MusicStates.Stop;
			}
			return MusicStates.Pause;
		}
	}

	private static void MusicLoop(bool isFirstLoad = false)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected Obj, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected Obj, but got Unknown
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		WaveOutEvent val = null;
		AudioFileReader val2 = null;
		try
		{
			val = (musicNAudio = new WaveOutEvent());
			val.DeviceNumber = -1;
			val2 = new AudioFileReader(musicCurrent);
			val.Init((IWaveProvider)(object)val2);
			val.Play();
			if (isFirstLoad && !Config.Preference.Music.StartOnStartup)
			{
				val.Pause();
			}
			MusicRefreshUI();
			int num = Config.Preference.Music.Volume;
			val.Volume = (float)num / 1000f;
			while (((object)val).Equals((object?)musicNAudio) && (int)val.PlaybackState != 0)
			{
				int volume = Config.Preference.Music.Volume;
				if (volume != num)
				{
					num = volume;
					val.Volume = (float)volume / 1000f;
				}
				if (((WaveStream)val2).TotalTime.TotalMilliseconds > 0.0)
				{
					double progress = ((WaveStream)val2).CurrentTime.TotalMilliseconds / ((WaveStream)val2).TotalTime.TotalMilliseconds;
					ModBase.RunInUi(() =>
					{
						ModMain.frmMain.BtnExtraMusic.Progress = progress;
					});
				}
				Thread.Sleep(100);
			}
			if ((int)val.PlaybackState == 0)
			{
				bool? flag = musicAllList?.Any();
				bool? flag2;
				if (flag.HasValue)
				{
					bool valueOrDefault = flag == true;
					flag2 = valueOrDefault;
				}
				else
				{
					flag2 = null;
				}
				flag = flag2;
				if (flag == true)
				{
					MusicStartPlay(DequeueNextMusicAddress(), isFirstLoad);
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "播放音乐出现内部错误（" + musicCurrent + "）", ModBase.LogLevel.Developer);
			string fileNameFromPath = ModBase.GetFileNameFromPath(musicCurrent);
			if (ex is MmException)
			{
				string message = ex.Message;
				if (message.Contains("AlreadyAllocated"))
				{
					HintService.Hint(Lang.Text("Music.Error.DeviceBusy"), HintType.Error);
				}
				else if (message.Contains("NoDriver") || message.Contains("BadDeviceId"))
				{
					HintService.Hint(Lang.Text("Music.Error.DeviceChanged"), HintType.Error);
				}
				else
				{
					ModBase.Log(ex, "播放失败（" + fileNameFromPath + "）", ModBase.LogLevel.Hint, null, Lang.Text("Music.Error.PlaybackFailed", new object[1] { fileNameFromPath }));
				}
			}
			else if (ex.Message.Contains("Got a frame at sample rate") || ex.Message.Contains("does not support changes to"))
			{
				HintService.Hint(Lang.Text("Music.Error.PropertyChangeUnsupported", new object[1] { fileNameFromPath }), HintType.Error);
			}
			else if ((!musicCurrent.EndsWithF(".wav", ignoreCase: true) && !musicCurrent.EndsWithF(".mp3", ignoreCase: true) && !musicCurrent.EndsWithF(".flac", ignoreCase: true)) || ex.Message.Contains("0xC00D36C4"))
			{
				HintService.Hint(Lang.Text("Music.Error.UnsupportedFormat", new object[1] { fileNameFromPath }), HintType.Error);
			}
			else
			{
				ModBase.Log(ex, "播放失败（" + fileNameFromPath + "）", ModBase.LogLevel.Hint, null, Lang.Text("Music.Error.PlaybackFailed", new object[1] { fileNameFromPath }));
			}
			musicAllList?.Remove(musicCurrent);
			musicWaitingList?.Remove(musicCurrent);
			MusicRefreshUI();
			Thread.Sleep(2000);
			if (ex is FileNotFoundException)
			{
				MusicRefreshPlay(showHint: true, isFirstLoad);
			}
			else
			{
				MusicStartPlay(DequeueNextMusicAddress(), isFirstLoad);
			}
		}
		finally
		{
			if (val != null)
			{
				val.Dispose();
			}
			((Stream)(object)val2)?.Dispose();
			MusicRefreshUI();
		}
	}

	private static void MusicListInit(bool forceReload, string preventFirst = null)
	{
		if (forceReload)
		{
			musicAllList = null;
		}
		try
		{
			if (musicAllList == null)
			{
				musicAllList = new List<string>();
				string text = Path.Combine(ModBase.exePath, "PCL", "Musics");
				Directory.CreateDirectory(text);
				foreach (FileInfo item in ModBase.EnumerateFiles(text))
				{
					string value = item.Extension.ToLowerInvariant();
					if (!new ReadOnlySpan<string>(new string[7] { ".ini", ".jpg", ".txt", ".cfg", ".lrc", ".db", ".png" }).Contains(value))
					{
						musicAllList.Add(item.FullName);
					}
				}
			}
			if (Config.Preference.Music.ShufflePlayback)
			{
				musicWaitingList = RandomUtils.Shuffle<string>((IList<string>)new List<string>(musicAllList));
			}
			else
			{
				musicWaitingList = new List<string>(musicAllList);
			}
			if (preventFirst != null && musicWaitingList.Count > 0 && string.Equals(musicWaitingList[0], preventFirst, StringComparison.OrdinalIgnoreCase))
			{
				musicWaitingList.RemoveAt(0);
				musicWaitingList.Add(preventFirst);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "初始化音乐列表失败", ModBase.LogLevel.Feedback, null, Lang.Text("Music.Error.OperationFailed"));
		}
	}

	private static string DequeueNextMusicAddress()
	{
		if (musicAllList == null || !musicAllList.Any() || !musicWaitingList.Any())
		{
			MusicListInit(forceReload: false);
		}
		if (musicWaitingList.Any())
		{
			string text = musicWaitingList[0];
			musicWaitingList.RemoveAt(0);
			if (!musicWaitingList.Any())
			{
				MusicListInit(forceReload: false, text);
			}
			return text;
		}
		return null;
	}

	private static void MusicRefreshUI()
	{
		ModBase.RunInUi(() =>
		{
			try
			{
				bool? flag = musicAllList?.Any();
				bool? flag2;
				if (flag.HasValue)
				{
					bool valueOrDefault = flag == true;
					flag2 = valueOrDefault;
				}
				else
				{
					flag2 = null;
				}
				if (flag2 == false)
				{
					ModMain.frmMain.BtnExtraMusic.Show = false;
				}
				else
				{
					ModMain.frmMain.BtnExtraMusic.Show = true;
					string fileNameWithoutExtentionFromPath = ModBase.GetFileNameWithoutExtentionFromPath(musicCurrent);
					bool flag3 = musicAllList.Count == 1;
					string text;
					if (MusicState == MusicStates.Pause)
					{
						ModMain.frmMain.BtnExtraMusic.SvgIcon = "lucide/play";
						ModMain.frmMain.BtnExtraMusic.LogoScale = 0.8;
						text = Lang.Text(flag3 ? "Music.ToolTip.Paused.Single" : "Music.ToolTip.Paused.Multiple", new object[1] { fileNameWithoutExtentionFromPath });
					}
					else
					{
						ModMain.frmMain.BtnExtraMusic.SvgIcon = "lucide/music";
						ModMain.frmMain.BtnExtraMusic.LogoScale = 1.0;
						text = Lang.Text(flag3 ? "Music.ToolTip.Playing.Single" : "Music.ToolTip.Playing.Multiple", new object[1] { fileNameWithoutExtentionFromPath });
					}
					ModMain.frmMain.BtnExtraMusic.ToolTip = text;
					ToolTipService.SetVerticalOffset((DependencyObject)(object)ModMain.frmMain.BtnExtraMusic, text.Contains("\n") ? 10 : 16);
				}
				ModMain.frmSetupUI?.MusicRefreshUI();
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "刷新背景音乐 UI 失败", ModBase.LogLevel.Feedback, null, Lang.Text("Music.Error.OperationFailed"));
			}
		});
	}

	public static void MusicControlPause()
	{
		if (musicNAudio == null)
		{
			HintService.Hint(Lang.Text("Music.Error.NotStarted"), HintType.Error);
			return;
		}
		switch (MusicState)
		{
		case MusicStates.Pause:
			MusicResume();
			break;
		case MusicStates.Play:
			MusicPause();
			break;
		default:
			ModBase.Log("[Music] 音乐目前为停止状态，已强制尝试开始播放", ModBase.LogLevel.Debug);
			MusicRefreshPlay(showHint: false);
			break;
		}
	}

	public static void MusicControlNext()
	{
		int? num = musicAllList?.Count;
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault == 1)
			{
				MusicStartPlay(musicCurrent);
				HintService.Hint(Lang.Text("Music.Hint.Replaying", new object[1] { ModBase.GetFileNameFromPath(musicCurrent) }), HintType.Success);
				goto IL_00a8;
			}
		}
		string text = DequeueNextMusicAddress();
		if (text == null)
		{
			HintService.Hint(Lang.Text("Music.Error.NoAvailable"), HintType.Error);
		}
		else
		{
			MusicStartPlay(text);
			HintService.Hint(Lang.Text("Music.Hint.Playing", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success);
		}
		goto IL_00a8;
		IL_00a8:
		MusicRefreshUI();
	}

	public static void MusicRefreshPlay(bool showHint, bool isFirstLoad = false)
	{
		try
		{
			MusicListInit(forceReload: true);
			bool? flag = musicAllList?.Any();
			bool? flag2;
			if (flag.HasValue)
			{
				bool valueOrDefault = flag == true;
				flag2 = valueOrDefault;
			}
			else
			{
				flag2 = null;
			}
			if (flag2 == false)
			{
				if (musicNAudio != null)
				{
					musicNAudio = null;
					if (showHint)
					{
						HintService.Hint(Lang.Text("Music.Hint.Cleared"), HintType.Success);
					}
				}
				else if (showHint)
				{
					HintService.Hint(Lang.Text("Music.Error.NotDetected"), HintType.Error);
				}
			}
			else
			{
				string text = DequeueNextMusicAddress();
				if (text == null)
				{
					if (showHint)
					{
						HintService.Hint(Lang.Text("Music.Error.NoAvailable"), HintType.Error);
					}
				}
				else
				{
					try
					{
						MusicStartPlay(text, isFirstLoad);
						if (showHint)
						{
							HintService.Hint(Lang.Text("Music.Hint.Refreshed", new object[1] { ModBase.GetFileNameFromPath(text) }), HintType.Success, log: false);
						}
					}
					catch
					{
					}
				}
			}
			MusicRefreshUI();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "刷新背景音乐播放失败", ModBase.LogLevel.Feedback, null, Lang.Text("Music.Error.OperationFailed"));
		}
	}

	private static void MusicStartPlay(string address, bool isFirstLoad = false)
	{
		if (!string.IsNullOrEmpty(address))
		{
			ModBase.Log("[Music] 播放开始：" + address);
			musicCurrent = address;
			ModBase.RunInNewThread(() =>
			{
				MusicLoop(isFirstLoad);
			}, "Music", ThreadPriority.BelowNormal);
		}
	}

	public static bool MusicPause()
	{
		if (MusicState != MusicStates.Play)
		{
			ModBase.Log($"[Music] 无需暂停播放，当前状态为 {MusicState}");
			return false;
		}
		ModBase.RunInThread(() =>
		{
			ModBase.Log("[Music] 已暂停播放");
			WaveOutEvent val = musicNAudio;
			if (val != null)
			{
				val.Pause();
			}
			MusicRefreshUI();
		});
		return true;
	}

	public static bool MusicResume()
	{
		if (MusicState == MusicStates.Play || musicAllList.Count == 0)
		{
			ModBase.Log($"[Music] 无需继续播放，当前状态为 {MusicState}");
			return false;
		}
		ModBase.RunInThread(() =>
		{
			ModBase.Log("[Music] 已恢复播放");
			try
			{
				WaveOutEvent val = musicNAudio;
				if (val != null)
				{
					val.Play();
				}
			}
			catch
			{
				WaveOutEvent val2 = musicNAudio;
				if (val2 != null)
				{
					val2.Stop();
				}
				WaveOutEvent val3 = musicNAudio;
				if (val3 != null)
				{
					val3.Play();
				}
			}
			MusicRefreshUI();
		});
		return true;
	}
}
