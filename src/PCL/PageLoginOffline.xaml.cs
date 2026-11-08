using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using FluentValidation;
using FluentValidation.Results;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Utils.Validate;

namespace PCL;

public partial class PageLoginOffline : Grid, IComponentConnector
{
	public PageLoginOffline()
	{
		InitializeComponent();
		BtnBack.Click += BtnBack_Click;
		RadioUuidCustom.Check += RadioUuid_Checked;
		RadioUuidStandard.Check += RadioUuid_Checked;
		RadioUuidLegacy.Check += RadioUuid_Checked;
		BtnLogin.Click += BtnLogin_Click;
	}

	private void BtnBack_Click(object sender, EventArgs e)
	{
		ProfileService.IsCreatingProfile = false;
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: true);
		});
	}

	private void RadioUuid_Checked(object sender, ModBase.RouteEventArgs e)
	{
		if (RadioUuidCustom.Checked)
		{
			TextUuidTitle.Visibility = Visibility.Visible;
			TextUuid.Visibility = Visibility.Visible;
		}
		else
		{
			TextUuidTitle.Visibility = Visibility.Collapsed;
			TextUuid.Visibility = Visibility.Collapsed;
		}
	}

	private void BtnLogin_Click(object sender, EventArgs e)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected Obj, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected Obj, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected Obj, but got Unknown
		string text = TextName.Text;
		if (!((AbstractValidator<string>)new RegexValidator("^[A-z0-9_]{3,16}$", "正则检查失败！")).Validate(text).IsValid && ModMain.MyMsgBox(Lang.Text("Launch.Account.Offline.InvalidPlayerId.Message"), Lang.Text("Launch.Account.Offline.InvalidPlayerId.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"), "", isWarn: true, highLight: true, forceWait: true) == 2)
		{
			return;
		}
		string text2 = null;
		if (!RadioUuidCustom.Checked)
		{
			text2 = ((!RadioUuidLegacy.Checked) ? ProfileUi.GetOfflineUuid(text) : ProfileUi.GetOfflineUuid(text, isSplited: false, isLegacy: true));
		}
		else
		{
			string text3 = TextUuid.Text.Replace("-", "");
			FluentValidation.Results.ValidationResult val = ((AbstractValidator<string>)new RegexValidator("^[a-fA-F0-9]{32}$", "正则检查失败！")).Validate(text3);
			if (RadioUuidCustom.Checked && !val.IsValid)
			{
				HintService.Hint(Lang.Text("Launch.Account.Offline.InvalidUuid", new object[1] { val }), HintType.Error);
				return;
			}
			text2 = text3;
		}
		ProfileService.Add(new McProfile
		{
			ProfileType = (ProfileType)1,
			Uuid = text2,
			UserName = text,
			Description = "",
			AccessToken = text2,
			ClientToken = text2,
			ProfileId = Guid.NewGuid().ToString("N")
		}, true);
		ProfileService.IsCreatingProfile = false;
		HintService.Hint(Lang.Text("Launch.Account.Profile.Created"), HintType.Success);
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: true);
		});
	}

}
