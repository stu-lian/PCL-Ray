using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;

namespace PCL;

public partial class PageLoginProfileSkin : Grid, IComponentConnector
{
	public PageLoginProfileSkin()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Reload();
		};
		PanData.MouseEnter += ShowPanel;
		PanData.MouseLeave += HidePanel;
		BtnSkin.Click += BtnSkin_Click;
		BtnEdit.Click += BtnEdit_Click;
		BtnSelect.Click += ChangeProfile;
	}

	public void Reload()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Invalid comparison between Unknown and I4
		ModBase.Log("[Profile] 刷新档案界面");
		Skin.Clear();
		McProfile val = ProfileService.Current ?? throw new InvalidOperationException("No profile selected.");
		if ((int)((SafeProfile)val).ProfileType == 0)
		{
			BtnEdit.Visibility = Visibility.Visible;
			ModBase.Log("[Profile] 使用正版皮肤加载器");
			Skin.loader = PageLaunchLeft.skinMs;
		}
		else
		{
			ProfileType profileType = ((SafeProfile)val).ProfileType;
			if ((int)profileType - 2 <= 1)
			{
				BtnEdit.Visibility = Visibility.Visible;
				ModBase.Log("[Profile] 使用 Authlib 皮肤加载器");
				Skin.loader = PageLaunchLeft.skinAuth;
			}
			else
			{
				BtnEdit.Visibility = Visibility.Collapsed;
				ModBase.Log("[Profile] 使用离线皮肤加载器");
				Skin.loader = PageLaunchLeft.skinLegacy;
			}
		}
		Skin.loader.Start(null, isForceRestart: true);
		TextName.Text = ((SafeProfile)val).UserName;
		TextType.Text = ProfileUi.GetProfileInfo(val);
	}

	private void ShowPanel(object sender, MouseEventArgs e)
	{
		ModAnimation.AniStart(ModAnimation.AaOpacity(PanButtons, 1.0 - PanButtons.Opacity, 120), "PageLoginProfileSkin Button");
	}

	private void HidePanel(object sender, EventArgs e)
	{
		if (!BtnEdit.ContextMenu.IsOpen && !BtnSkin.ContextMenu.IsOpen && !PanData.IsMouseOver)
		{
			ModAnimation.AniStart(ModAnimation.AaOpacity(PanButtons, 0.0 - PanButtons.Opacity, 120), "PageLoginProfileSkin Button");
		}
	}

	private void MenuAccountOptions_Closed(object sender, RoutedEventArgs e)
	{
		HidePanel(sender, e);
	}

	private void BtnSkin_Click(object sender, EventArgs e)
	{
		BtnSkin.ContextMenu.IsOpen = true;
	}

	private void BtnEdit_Click(object sender, EventArgs e)
	{
		BtnEdit.ContextMenu.IsOpen = true;
	}

	private void BtnEditPassword_Click(object sender, RoutedEventArgs e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		McProfile current = ProfileService.Current;
		if (current != null && (int)((SafeProfile)current).ProfileType == 0)
		{
			ModBase.OpenWebsite("https://account.live.com/password/Change");
			return;
		}
		ProfileType? val = ((current != null) ? new ProfileType?(((SafeProfile)current).ProfileType) : ((ProfileType?)null));
		bool flag;
		if (val.HasValue)
		{
			ProfileType valueOrDefault = val.GetValueOrDefault();
			if ((int)valueOrDefault - 2 <= 1)
			{
				flag = true;
				goto IL_0054;
			}
		}
		flag = false;
		goto IL_0054;
		IL_0054:
		if (flag && !string.IsNullOrWhiteSpace(current.Server))
		{
			string server = current.Server;
			ModBase.OpenWebsite(server.Replace("/api/yggdrasil/authserver" + (server.EndsWithF("/") ? "/" : ""), "/user/profile"));
		}
		else
		{
			HintService.Hint(Lang.Text("Launch.Account.ProfileSkin.PasswordUnsupported"));
		}
	}

	private void BtnEditName_Click(object sender, RoutedEventArgs e)
	{
		ProfileUi.EditProfileId();
	}

	private void ChangeProfile(object sender, EventArgs e)
	{
		ProfileService.Select((McProfile)null);
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: true);
			ModMain.frmLaunchLeft.BtnLaunch.IsEnabled = false;
		});
	}

	private void Skin_Click(object sender, RoutedEventArgs e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Invalid comparison between Unknown and I4
		McProfile current = ProfileService.Current;
		if (current != null && (int)((SafeProfile)current).ProfileType == 0)
		{
			ProfileUi.ChangeSkinMs();
			return;
		}
		ProfileType? val = ((current != null) ? new ProfileType?(((SafeProfile)current).ProfileType) : ((ProfileType?)null));
		bool flag;
		if (val.HasValue)
		{
			ProfileType valueOrDefault = val.GetValueOrDefault();
			if ((int)valueOrDefault - 2 <= 1)
			{
				flag = true;
				goto IL_004f;
			}
		}
		flag = false;
		goto IL_004f;
		IL_004f:
		if (flag && !string.IsNullOrWhiteSpace(current.Server))
		{
			ModBase.OpenWebsite(current.Server.BeforeFirst("api/yggdrasil/authserver") + "user/closet");
		}
		else
		{
			HintService.Hint(Lang.Text("Launch.Account.ProfileSkin.SkinUnsupported"));
		}
	}

	private void BtnSkinSave_Click(object sender, RoutedEventArgs e)
	{
		Skin.BtnSkinSave_Click(sender, e);
	}

	private void BtnSkinRefresh_Click(object sender, RoutedEventArgs e)
	{
		Skin.RefreshClick(sender, e);
	}

	private void BtnSkinCape_Click(object sender, RoutedEventArgs e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Invalid comparison between Unknown and I4
		McProfile current = ProfileService.Current;
		if (current != null && (int)((SafeProfile)current).ProfileType == 0)
		{
			Skin.BtnSkinCape_Click(sender, e);
			return;
		}
		ProfileType? val = ((current != null) ? new ProfileType?(((SafeProfile)current).ProfileType) : ((ProfileType?)null));
		bool flag;
		if (val.HasValue)
		{
			ProfileType valueOrDefault = val.GetValueOrDefault();
			if ((int)valueOrDefault - 2 <= 1)
			{
				flag = true;
				goto IL_0057;
			}
		}
		flag = false;
		goto IL_0057;
		IL_0057:
		if (flag && !string.IsNullOrWhiteSpace(current.Server))
		{
			ModBase.OpenWebsite(current.Server.BeforeFirst("api/yggdrasil/authserver") + "user/closet");
		}
		else
		{
			HintService.Hint(Lang.Text("Launch.Account.ProfileSkin.CapeUnsupported"));
		}
	}

}
