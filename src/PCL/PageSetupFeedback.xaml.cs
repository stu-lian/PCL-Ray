using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Humanizer;
using PCL.Core.App.Localization;
using PCL.Network;

namespace PCL;

public partial class PageSetupFeedback : MyPageRight, IComponentConnector
{
	public enum TagId : long
	{
		Processing = 6820804544L,
		WaitingProcess = 6820804546L,
		Completed = 6820804547L,
		Decline = 6820804539L,
		Ignored = 8064650117L,
		Duplicate = 6820804541L,
		Wait = 8743070786L,
		Pause = 8558220235L,
		Upnext = 8550609020L
	}

	public class Feedback
	{
		[JsonIgnore]
		public string User { get; set; } = string.Empty;

		[JsonPropertyName("title")]
		public string Title { get; init; } = string.Empty;

		[JsonPropertyName("created_at")]
		public DateTime Time { get; init; }

		[JsonPropertyName("body")]
		public string Content { get; init; } = string.Empty;

		[JsonPropertyName("html_url")]
		public string Url { get; init; } = string.Empty;

		[JsonIgnore]
		public string Id { get; set; } = string.Empty;

		public List<string> Tags { get; } = new List<string>();

		public string Type { get; set; } = string.Empty;
	}

	private bool _isLoaded;

	public ModLoader.LoaderTask<bool, List<Feedback>> Loader;

	private static readonly Dictionary<string, (Func<PageSetupFeedback, StackPanel> Panel, string Icon)> TagMap = new Dictionary<string, (Func<PageSetupFeedback, StackPanel>, string)>
	{
		[6820804544L.ToString()] = ((PageSetupFeedback p) => p.PanListProcessing, "Blocks/CommandBlock.png"),
		[6820804546L.ToString()] = ((PageSetupFeedback p) => p.PanListWaitingProcess, "Blocks/RedstoneBlock.png"),
		[8743070786L.ToString()] = ((PageSetupFeedback p) => p.PanListWait, "Blocks/Anvil.png"),
		[8558220235L.ToString()] = ((PageSetupFeedback p) => p.PanListPause, "Blocks/RedstoneLampOff.png"),
		[8550609020L.ToString()] = ((PageSetupFeedback p) => p.PanListUpnext, "Blocks/RedstoneLampOn.png"),
		[6820804547L.ToString()] = ((PageSetupFeedback p) => p.PanListCompleted, "Blocks/Grass.png"),
		[6820804539L.ToString()] = ((PageSetupFeedback p) => p.PanListDecline, "Blocks/CobbleStone.png"),
		[8064650117L.ToString()] = ((PageSetupFeedback p) => p.PanListIgnored, "Blocks/CobbleStone.png"),
		[6820804541L.ToString()] = ((PageSetupFeedback p) => p.PanListDuplicate, "Blocks/CobbleStone.png")
	};

	public PageSetupFeedback()
	{
		InitializeComponent();
		Loader = new ModLoader.LoaderTask<bool, List<Feedback>>("FeedbackList", FeedbackListGet);
		Loaded += PageOtherFeedback_Loaded;
	}

	private void PageOtherFeedback_Loaded(object sender, RoutedEventArgs e)
	{
		PageLoaderInit(Load, PanLoad, PanContent, PanInfo, Loader, (ModLoader.LoaderBase _) =>
		{
			RefreshList();
		});
		PanBack.ScrollToHome();
		if (!_isLoaded)
		{
			_isLoaded = true;
		}
	}

	public void FeedbackListGet(ModLoader.LoaderTask<bool, List<Feedback>> task)
	{
		JsonArray obj = (Requester.FetchJson("https://api.github.com/repos/stu-lian/PCL-Ray/issues?state=all&sort=created&per_page=200", new RequestParam
		{
			Retries = 3,
			UseBrowserUserAgent = true
		}) as JsonArray) ?? throw new Exception(Lang.Text("Setup.Feedback.LoadFailed"));
		List<Feedback> list = new List<Feedback>();
		foreach (JsonNode item in obj)
		{
			if (!(item is JsonObject jsonObject))
			{
				continue;
			}
			JsonNode jsonNode = jsonObject["pull_request"];
			if (jsonNode != null && jsonNode.GetValueKind() != JsonValueKind.Null)
			{
				continue;
			}
			Feedback feedback = jsonObject.Deserialize<Feedback>();
			feedback.User = jsonObject["user"]["login"].ToString();
			feedback.Id = jsonObject["number"].ToString();
			string type = Lang.Text("Setup.Feedback.Uncategorized");
			JsonNode jsonNode2 = jsonObject["type"];
			if (jsonNode2 != null && jsonNode2.GetValueKind() == JsonValueKind.Object)
			{
				JsonNode jsonNode3 = jsonNode2["name"];
				if (jsonNode3 != null)
				{
					type = jsonNode3.ToString().ToLower();
				}
			}
			feedback.Type = type;
			if (jsonObject["labels"] is JsonArray jsonArray)
			{
				foreach (JsonNode item2 in jsonArray)
				{
					if (item2 is JsonObject jsonObject2)
					{
						feedback.Tags.Add(jsonObject2["id"].ToString());
					}
				}
			}
			list.Add(feedback);
		}
		task.output = list;
	}

	private MyListItem CreateFeedbackItem(Feedback item, string logo)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = item.Title;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Info = item.User + " | " + Lang.Date(item.Time, "G");
		myListItem.Logo = ModBase.pathImage + logo;
		myListItem.Tags = item.Type;
		myListItem.Click += (object _, MouseButtonEventArgs _) =>
		{
			ShowFeedbackDetail(item);
		};
		return myListItem;
	}

	private void ShowFeedbackDetail(Feedback item)
	{
		string text = Lang.TimeSpan(item.Time - DateTime.Now, 2, true, (TimeUnit)7, (TimeUnit)3);
		if (ModMain.MyMsgBoxMarkdown(Lang.Text("Setup.Feedback.Item.Submitter", new object[2] { item.User, text }) + "\n" + Lang.Text("Setup.Feedback.Item.Type", new object[1] { item.Type }) + "\n\n" + item.Content, "#" + item.Id + " " + item.Title, null, Lang.Text("Setup.Feedback.Item.ViewDetail")) == 2)
		{
			ModBase.OpenWebsite(item.Url);
		}
	}

	private void SetPanelVisibility(StackPanel panel, MyCard card)
	{
		card.Visibility = ((panel.Children.Count == 0) ? Visibility.Collapsed : Visibility.Visible);
	}

	public void RefreshList()
	{
		PanListProcessing.Children.Clear();
		PanListWaitingProcess.Children.Clear();
		PanListWait.Children.Clear();
		PanListPause.Children.Clear();
		PanListUpnext.Children.Clear();
		PanListCompleted.Children.Clear();
		PanListDecline.Children.Clear();
		PanListIgnored.Children.Clear();
		PanListDuplicate.Children.Clear();
		foreach (Feedback item in Loader.output)
		{
			string text = item.Tags.Find(TagMap.ContainsKey);
			if (text != null)
			{
				var (func, logo) = TagMap[text];
				func(this).Children.Add(CreateFeedbackItem(item, logo));
			}
		}
		SetPanelVisibility(PanListProcessing, PanContentProcessing);
		SetPanelVisibility(PanListWaitingProcess, PanContentWaitingProcess);
		SetPanelVisibility(PanListWait, PanContentWait);
		SetPanelVisibility(PanListPause, PanContentPause);
		SetPanelVisibility(PanListUpnext, PanContentUpnext);
		SetPanelVisibility(PanListCompleted, PanContentCompleted);
		SetPanelVisibility(PanListDecline, PanContentDecline);
		SetPanelVisibility(PanListIgnored, PanContentIgnored);
		SetPanelVisibility(PanListDuplicate, PanContentDuplicate);
	}

	private void Feedback_Click(object sender, MouseButtonEventArgs e)
	{
		PageSetupLeft.TryFeedback();
	}

}
