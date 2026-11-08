using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL;

public partial class PageLoginProfile : Grid, IComponentConnector
{
	public class ProfileItem
	{
		public string Info { get; private set; }

		public string Logo { get; private set; } = string.Empty;

		public string SvgIcon { get; private set; } = string.Empty;

		public McProfile Profile { get; }

		public string Username => ((SafeProfile)Profile).UserName;

		public ProfileItem(McProfile profile)
		{
			Profile = profile;
			Info = ProfileUi.GetProfileInfo(profile);
			string text = ModBase.pathTemp + "Cache\\Skin\\Head\\" + profile.SkinHeadId + ".png";
			if (File.Exists(text) && new FileInfo(text).Length != 0L)
			{
				Logo = text;
				SvgIcon = string.Empty;
			}
			else
			{
				Logo = string.Empty;
				SvgIcon = "lucide/user";
			}
		}
	}

	public ObservableCollection<ProfileItem> ProfileCollection { get; set; } = new ObservableCollection<ProfileItem>();

	public PageLoginProfile()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Reload();
		};
	}

	public void Reload()
	{
		RefreshProfileList();
		ModMain.frmLoginProfileSkin = null;
	}

	public void RefreshProfileList()
	{
		ModBase.Log("[Profile] 刷新档案列表");
		ProfileCollection.Clear();
		ProfileService.Load();
		try
		{
			foreach (McProfile profile in ProfileService.Profiles)
			{
				ProfileCollection.Add(new ProfileItem(profile));
			}
			HintMicrosoft.Visibility = ((ProfileService.Profiles.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
			ModBase.Log("[Profile] 档案列表刷新完成");
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Launch.Account.Profile.Error.Read"), ModBase.LogLevel.Feedback, null, Lang.Text("Launch.Account.Profile.Error.Read"));
		}
		if (!ProfileService.Profiles.Any())
		{
			States.Hint.LaunchWithProfile = true;
			HintCreate.Visibility = Visibility.Visible;
		}
		else
		{
			HintCreate.Visibility = Visibility.Collapsed;
		}
	}

	private void SelectProfile(object sender, MouseButtonEventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		McProfile val = (McProfile)((MyListItem)sender).Tag;
		ProfileService.Select(val);
		ModBase.Log($"[Profile] 选定档案: {((SafeProfile)val).UserName}, 以 {((SafeProfile)val).ProfileType} 方式验证");
		ModLaunch.mcLoginMsLoader.State = ModBase.LoadState.Waiting;
		ModLaunch.mcLoginAuthLoader.State = ModBase.LoadState.Waiting;
		ModLaunch.mcLoginLegacyLoader.State = ModBase.LoadState.Waiting;
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: true);
			ModMain.frmLaunchLeft.BtnLaunch.IsEnabled = true;
		});
	}

	private void ProfileContMenuBuild(MyListItem sender, EventArgs e)
	{
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Invalid comparison between Unknown and I4
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/pencil",
			ToolTip = Lang.Text("Launch.Account.Profile.ChangeUuid"),
			Tag = sender.Tag
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += EditProfileUuid;
		MyIconButton myIconButton2 = new MyIconButton
		{
			SvgIcon = "lucide/copy",
			ToolTip = Lang.Text("Launch.Account.Profile.CopyUuid"),
			Tag = sender.Tag
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += CopyProfileUuid;
		MyIconButton myIconButton3 = new MyIconButton
		{
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Launch.Account.Profile.ChangeAuthServerName"),
			Tag = sender.Tag
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton3, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton3, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton3, 2.0);
		myIconButton3.Click += EditProfileServer;
		MyIconButton myIconButton4 = new MyIconButton
		{
			SvgIcon = "lucide/trash-2",
			ToolTip = Lang.Text("Launch.Account.Profile.Delete"),
			Tag = sender.Tag
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton4, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton4, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton4, 2.0);
		myIconButton4.Click += DeleteProfile;
		if ((int)((SafeProfile)(McProfile)sender.Tag).ProfileType == 1)
		{
			sender.Buttons = new MyIconButton[2] { myIconButton, myIconButton4 };
		}
		else
		{
			sender.Buttons = new MyIconButton[2] { myIconButton2, myIconButton4 };
		}
	}

	private void BtnNew_Click(object sender, EventArgs e)
	{
		ModBase.RunInNewThread(() =>
		{
			ProfileUi.CreateProfile();
			ModBase.RunInUi(() =>
			{
				RefreshProfileList();
			});
		});
	}

	private void EditProfileUuid(object sender, EventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected Obj, but got Unknown
		ProfileUi.EditOfflineUuid((McProfile)((MyIconButton)sender).Tag);
	}

	private void CopyProfileUuid(object sender, EventArgs e)
	{
		if (sender is MyIconButton { Tag: var tag })
		{
			McProfile val = (McProfile)((tag is McProfile) ? tag : null);
			if (val != null)
			{
				ModBase.ClipboardSet(((SafeProfile)val).Uuid);
			}
		}
	}

	private void EditProfileServer(object sender, EventArgs e)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		McProfile val = (McProfile)((MyIconButton)sender).Tag;
		string text = ModMain.MyMsgBoxInput(Lang.Text("Launch.Account.Profile.EditServerName.Title"), Lang.Text("Launch.Account.Profile.EditServerName.Message"), val.ServerName);
		if (text != null)
		{
			ProfileUi.EditAuthServerName(val, text);
		}
	}

	private void DeleteProfile(object sender, EventArgs e)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected Obj, but got Unknown
		if (ModMain.MyMsgBox(Lang.Text("Launch.Account.Profile.DeleteConfirm.Message"), Lang.Text("Launch.Account.Profile.DeleteConfirm.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"), "", isWarn: true, highLight: true, forceWait: true) != 2)
		{
			ProfileUi.RemoveProfile((McProfile)((MyIconButton)sender).Tag);
			ModBase.RunInUi(() =>
			{
				RefreshProfileList();
			});
		}
	}

}
