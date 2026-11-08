using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public static class AnnouncementService
{
	/// <summary>
	/// PCL Ray 自建公告后端。修改域名时只需改这里。
	/// </summary>
	public const string NoticeEndpoint = "https://notice.1122334.xyz/api/notice";

	public static readonly UpdatesNoticeModel NoticeSource = new UpdatesNoticeModel(NoticeEndpoint);

	/// <summary>
	/// 已读公告 ID 保留上限，避免历史文件无限增长。
	/// </summary>
	private const int HistoryLimit = 200;

	private static readonly object HistoryLock = new object();

	/// <summary>
	/// 已读公告独立存储，不再依赖配置系统的延迟写入，避免异常退出后重复弹窗。
	/// </summary>
	private static string HistoryPath => Path.Combine(Paths.SharedData, "announcement-history.txt");

	public static void Load()
	{
		if (States.System.AnnounceSolution > 1)
		{
			return;
		}
		List<string> showedAnnounced = LoadShownIds();
		VersionAnnouncementDataModel announcementData;
		try
		{
			announcementData = NoticeSource.GetAnnouncementList();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[System] 无法从 PCL Ray 公告后端获取公告，尝试备用公告源");
			try
			{
				announcementData = UpdateManager.remoteServer.GetAnnouncementList();
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[System] 备用公告源同样不可用，本次不展示公告");
				announcementData = new VersionAnnouncementDataModel
				{
					Content = new List<VersionAnnouncementContentModel>()
				};
			}
		}

		// 同一 ID 只展示一次；同时去重同一批返回中的重复公告。
		List<VersionAnnouncementContentModel> showAnnounce = new List<VersionAnnouncementContentModel>();
		foreach (VersionAnnouncementContentModel item in announcementData.Content ?? new List<VersionAnnouncementContentModel>())
		{
			if (item == null || IsExpired(item))
			{
				continue;
			}
			string key = GetAnnouncementKey(item);
			if (string.IsNullOrWhiteSpace(key) || showedAnnounced.Contains(key))
			{
				continue;
			}
			showedAnnounced.Add(key);
			showAnnounce.Add(item);
		}
		ModBase.Log("[System] 需要展示的公告数量：" + showAnnounce.Count);
		if (showAnnounce.Count == 0)
		{
			return;
		}

		// 展示前立即落盘，保证即使启动器异常退出也不会重复弹出同一公告。
		SaveShownIds(showedAnnounced);
		States.Hint.ShowedAnnouncements = showedAnnounced.TakeLast(HistoryLimit).Join("|");

		ModBase.RunInNewThread(() =>
		{
			foreach (VersionAnnouncementContentModel item in showAnnounce)
			{
				ModMain.MyMsgBox(item.Detail, item.Title, (item.Btn1 == null) ? "" : item.Btn1.Text, (item.Btn2 == null) ? "" : item.Btn2.Text, Lang.Text("Common.Action.Close"), isWarn: false, highLight: true, forceWait: false, () =>
				{
					// 没有按钮 1 时（例如公告只带关闭按钮），不要触发任何命令。
					if (item.Btn1 == null)
					{
						return;
					}
					if (EventTypeMapper.TryParse(item.Btn1.Command, out var result))
					{
						CustomEvent.Raise(result, item.Btn1.CommandParameter);
					}
				}, () =>
				{
					// 没有按钮 2 时同理。
					if (item.Btn2 == null)
					{
						return;
					}
					if (EventTypeMapper.TryParse(item.Btn2.Command, out var result))
					{
						CustomEvent.Raise(result, item.Btn2.CommandParameter);
					}
				});
			}
		});
	}

	/// <summary>
	/// 公告唯一标识：优先使用后端下发的 ID；缺失时按标题和正文生成稳定哈希。
	/// </summary>
	private static string GetAnnouncementKey(VersionAnnouncementContentModel item)
	{
		if (item == null)
		{
			return "";
		}
		if (!string.IsNullOrWhiteSpace(item.Id))
		{
			return item.Id.Trim();
		}
		string raw = (item.Title ?? "") + "\n" + (item.Detail ?? "");
		if (raw.Length == 0)
		{
			return "";
		}
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
		return "auto-" + Convert.ToHexString(hash).Substring(0, 16).ToLowerInvariant();
	}

	private static List<string> LoadShownIds()
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		void AddId(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return;
			}
			string id = value.Trim();
			if (seen.Add(id))
			{
				result.Add(id);
			}
		}

		// 兼容旧版本写入配置的已读记录。
		try
		{
			foreach (string id in States.Hint.ShowedAnnouncements.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
			{
				AddId(id);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "[System] 读取旧公告记录失败");
		}

		lock (HistoryLock)
		{
			try
			{
				if (File.Exists(HistoryPath))
				{
					foreach (string line in File.ReadAllLines(HistoryPath))
					{
						AddId(line);
					}
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "[System] 读取公告历史文件失败");
			}
		}
		return result;
	}

	private static void SaveShownIds(List<string> ids)
	{
		lock (HistoryLock)
		{
			try
			{
				string directory = Path.GetDirectoryName(HistoryPath);
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}
				string[] lines = ids.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct().TakeLast(HistoryLimit).ToArray();
				string tempPath = HistoryPath + ".tmp";
				File.WriteAllLines(tempPath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				File.Move(tempPath, HistoryPath, overwrite: true);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[System] 保存公告历史失败，本次公告可能重复弹出");
			}
		}
	}

	private static bool IsExpired(VersionAnnouncementContentModel item)
	{
		if (item == null || string.IsNullOrWhiteSpace(item.ExpiresAt))
		{
			return false;
		}
		return DateTimeOffset.TryParse(item.ExpiresAt, out DateTimeOffset expiresAt) && expiresAt <= DateTimeOffset.Now;
	}
}