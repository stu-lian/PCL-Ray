using System;
using System.Collections.Generic;
using System.Linq;
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

	public static void Load()
	{
		if (States.System.AnnounceSolution > 1)
		{
			return;
		}
		List<string> showedAnnounced = States.Hint.ShowedAnnouncements.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
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
		List<VersionAnnouncementContentModel> showAnnounce = (announcementData.Content ?? new List<VersionAnnouncementContentModel>()).Where((VersionAnnouncementContentModel x) => !showedAnnounced.Contains(x.Id) && !IsExpired(x)).ToList();
		ModBase.Log("[System] 需要展示的公告数量：" + showAnnounce.Count);
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
		showedAnnounced.AddRange(showAnnounce.Select((VersionAnnouncementContentModel x) => x.Id));
		showedAnnounced = showedAnnounced.Distinct().ToList();
		States.Hint.ShowedAnnouncements = showedAnnounced.Join("|");
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
