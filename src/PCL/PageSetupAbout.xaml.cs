using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Utils;

namespace PCL;

public partial class PageSetupAbout : MyPageRight, IComponentConnector
{
	public class GitHubContributor
	{
		[JsonPropertyName("login")]
		public string Login { get; set; }

		[JsonPropertyName("avatar_url")]
		public string AvatarUrl { get; set; }

		[JsonPropertyName("html_url")]
		public string HtmlUrl { get; set; }

		[JsonPropertyName("contributions")]
		public int Contributions { get; set; }
	}

	private int clickCount;

	private bool isLoaded;

	public ObservableCollection<GitHubContributor> Contributors { get; set; } = new ObservableCollection<GitHubContributor>();

	public PageSetupAbout()
	{
		InitializeComponent();
		Loaded += PageOtherAbout_Loaded;
	}

	private void PageOtherAbout_Loaded(object sender, RoutedEventArgs e)
	{
		PanBack.ScrollToHome();
		if (!isLoaded)
		{
			isLoaded = true;
			ItemAboutPcl.Info = ItemAboutPcl.Info.Replace("%VERSION%", ModBase.versionBaseName).Replace("%VERSIONCODE%", ModBase.versionCode.ToString()).Replace("%BRANCH%", "")
				.Replace("%COMMIT_HASH%", ModBase.commitHashShort);
			if (!Lang.IsChineseMainland)
			{
				ItemMcmod.Visibility = Visibility.Collapsed;
				BtnMcmod.Visibility = Visibility.Collapsed;
				ImgMcmod.Visibility = Visibility.Collapsed;
				RowMcmod.Height = new GridLength(0.0);
			}
			LoadContributersAsync();
		}
	}

	private async void LoadContributersAsync()
	{
		_ = 1;
		try
		{
			using HttpResponseMessage response = await HttpSenderExtension.SendAsync(HttpRequest.Create("https://api.github.com/repos/PCL-Community/PCL2-CE/contributors"), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken));
			response.EnsureSuccessStatusCode();
			List<GitHubContributor> list = await HttpResponseExtension.AsJsonAsync<List<GitHubContributor>>(response, JsonCompat.SerializerOptions, default(CancellationToken));
			Contributors.Clear();
			foreach (GitHubContributor item in list)
			{
				Contributors.Add(item);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Setup.About.Error.LoadContributorsFailed"));
		}
	}

	private void ImgPCLCommunity_Click(object sender, MouseButtonEventArgs e)
	{
		ModAnimation.AniStart(new ModAnimation.AniData[1] { ModAnimation.AaRotateTransform(sender, 360.0) });
	}

	private void ImgPCLLogo_Click(object sender, MouseButtonEventArgs e)
	{
		if (clickCount >= 200)
		{
			return;
		}
		clickCount++;
		switch (clickCount)
		{
		case 5:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.NiceClick"));
			break;
		case 15:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.StillClicking"));
			break;
		case 25:
			if (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Bored.Message"), Lang.Text("Setup.About.EasterEgg.Bored.Title"), Lang.Text("Setup.About.EasterEgg.Bored.Yes"), Lang.Text("Setup.About.EasterEgg.Bored.No")) == 2)
			{
				HintService.Hint(Lang.Text("Setup.About.EasterEgg.Bored.Response"));
			}
			break;
		case 50:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.Encouragement"));
			break;
		case 75:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.HiddenTheme"));
			break;
		case 100:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.StillStaring"));
			break;
		case 130:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.NothingBehind"));
			break;
		case 150:
			switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Tired.Message1"), Lang.Text("Setup.About.EasterEgg.Tired.Title1"), Lang.Text("Setup.About.EasterEgg.Tired.Exhausted"), Lang.Text("Setup.About.EasterEgg.Tired.NotTired")))
			{
			case 1:
				HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.StopClicking"));
				break;
			case 2:
				switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Tired.Message2"), Lang.Text("Setup.About.EasterEgg.Tired.Title2"), Lang.Text("Setup.About.EasterEgg.Tired.Exhausted"), Lang.Text("Setup.About.EasterEgg.Tired.NotTired")))
				{
				case 1:
					HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.StopClicking"));
					break;
				case 2:
					switch (ModMain.MyMsgBox(Lang.Text("Setup.About.EasterEgg.Tired.Message3"), Lang.Text("Setup.About.EasterEgg.Tired.Title3"), Lang.Text("Setup.About.EasterEgg.Tired.Exhausted"), Lang.Text("Setup.About.EasterEgg.Tired.ReallyNotTired")))
					{
					case 1:
						HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.StopClicking"));
						break;
					case 2:
						HintService.Hint(Lang.Text("Setup.About.EasterEgg.Tired.FinallyGiveUp"));
						break;
					}
					break;
				}
				break;
			}
			break;
		case 200:
			HintService.Hint(Lang.Text("Setup.About.EasterEgg.ClickDisabled"));
			ImgPCLLogo.IsHitTestVisible = false;
			return;
		}
		Random random = new Random();
		int num = random.Next(-1, 1);
		if (num == 0)
		{
			num = 1;
		}
		int num2 = random.Next(-1, 1);
		if (num2 == 0)
		{
			num2 = 1;
		}
		ModAnimation.AniStart(new ModAnimation.AniData[4]
		{
			ModAnimation.AaTranslateX(sender, num, 0),
			ModAnimation.AaTranslateY(sender, num2, 0),
			ModAnimation.AaTranslateX(sender, -num, 0, 100),
			ModAnimation.AaTranslateY(sender, -num2, 0, 100)
		});
	}

}
