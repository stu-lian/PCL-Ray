using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Utils;
using PCL.Core.Utils.Validate;
using PCL.Network;

namespace PCL;

public static class ProfileUi
{
	private static int _isMsSkinChanging;

	public static bool HandleMicrosoftXstsError(IdentityModelAuthenticationException exception)
	{
		string error = exception.Error;
		if (error == null || !error.StartsWith("xsts_", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string normalizedError = error.ToLowerInvariant();
		bool flag;
		switch (normalizedError)
		{
		case "xsts_2148916227":
		case "xsts_2148916233":
		case "xsts_2148916235":
		case "xsts_2148916238":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return false;
		}
		ModBase.RunInUiWait(() =>
		{
			switch (normalizedError)
			{
			case "xsts_2148916227":
				ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.Banned"), Lang.Text("Minecraft.Launch.Login.Failed"), Lang.Text("Minecraft.Launch.Login.IKnow"), "", "", isWarn: true);
				break;
			case "xsts_2148916233":
				if (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.XboxNotRegistered"), Lang.Text("Minecraft.Launch.Login.Hint"), Lang.Text("Minecraft.Launch.Login.Register"), Lang.Text("Common.Action.Cancel")) == 1)
				{
					ModBase.OpenWebsite("https://signup.live.com/signup");
				}
				break;
			case "xsts_2148916235":
				ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.RegionBlocked"), Lang.Text("Minecraft.Launch.Login.Failed"), Lang.Text("Minecraft.Launch.Login.IKnow"));
				break;
			case "xsts_2148916238":
				if (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.Underage.Message"), Lang.Text("Minecraft.Launch.Login.Hint"), Lang.Text("Minecraft.Launch.Login.Microsoft.Underage.AgeOver13"), Lang.Text("Minecraft.Launch.Login.Microsoft.Underage.AgeUnder13"), Lang.Text("Common.Option.IDontKnow")) == 1)
				{
					ModBase.OpenWebsite("https://account.live.com/editprof.aspx");
					ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.ChangeBirthDate.Message"), Lang.Text("Minecraft.Launch.Login.Hint"));
				}
				else
				{
					ModBase.OpenWebsite("https://support.microsoft.com/zh-cn/account-billing/如何更改-microsoft-帐户上的出生日期-837badbc-999e-54d2-2617-d19206b9540a");
					ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.ChangeBirthDate.SupportMessage"), Lang.Text("Minecraft.Launch.Login.Hint"));
				}
				break;
			}
		});
		return true;
	}

	public static bool HandleMicrosoftNotOwnedError(IdentityModelAuthenticationException exception)
	{
		if (!string.Equals(exception.Error, "minecraft_not_owned", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		ModBase.RunInUiWait(() =>
		{
			if (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.NotPurchased"), Lang.Text("Minecraft.Launch.Login.Failed"), Lang.Text("Minecraft.Launch.Login.Microsoft.PurchaseMinecraft"), Lang.Text("Common.Action.Cancel")) == 1)
			{
				ModBase.OpenWebsite("https://www.xbox.com/zh-cn/games/store/minecraft-java-bedrock-edition-for-pc/9nxp44l49shj");
			}
		});
		return true;
	}

	public static bool HandleMicrosoftCreateProfileError(IdentityModelAuthenticationException exception)
	{
		if (!string.Equals(exception.Error, "profile_not_created", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		ModBase.RunInUiWait(() =>
		{
			if (ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Login.Microsoft.CreateProfile.Message"), Lang.Text("Minecraft.Launch.Login.Failed"), Lang.Text("Minecraft.Launch.Login.Microsoft.CreateProfile.Button"), Lang.Text("Common.Action.Cancel")) == 1)
			{
				ModBase.OpenWebsite("https://www.minecraft.net/zh-hans/msaprofile/mygames/editprofile");
			}
		});
		return true;
	}

	public static Task<AuthorizeResult?> ShowDeviceCodeLoginAsync(DeviceCodeAuthenticationContext context, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		TaskCompletionSource<AuthorizeResult?> completion = new TaskCompletionSource<AuthorizeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		ModMain.MyMsgBoxConverter item = new ModMain.MyMsgBoxConverter
		{
			Content = JsonSerializer.SerializeToNode<DeviceCodeData>(context.Data, JsonCompat.SerializerOptions).AsObject(),
			ForceWait = true,
			Type = ModMain.MyMsgBoxType.Login,
			DeviceCodePoll = (JsonObject _, CancellationToken pollToken) => context.PollAsync(pollToken),
			LoginResultHandler = (AuthorizeResult oauth, CancellationToken _) =>
			{
				completion.TrySetResult(oauth);
				return Task.CompletedTask;
			},
			CompletionHandler = (object result) =>
			{
				if (result is Exception exception)
				{
					completion.TrySetException(exception);
				}
			}
		};
		ModMain.WaitingMyMsgBox.Add(item);
		return completion.Task;
	}

	public static object McLoginMojangUuid(string name, bool throwOnNotFound)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return ModBase.StrFill("", "0", 32);
		}
		string text = ModBase.ReadIni(ModBase.pathTemp + "Cache\\Uuid\\Mojang.ini", name);
		if (text != null && text.Length == 32)
		{
			return text;
		}
		try
		{
			JsonObject json = null;
			bool finished = false;
			ModBase.RunInNewThread(() =>
			{
				try
				{
					json = (JsonObject)ModNet.NetGetCodeByRequestRetry("https://api.mojang.com/users/profiles/minecraft/" + name, null, "", isJson: true);
				}
				catch
				{
				}
				finally
				{
					finished = true;
				}
			}, name + " Uuid Get");
			while (!finished)
			{
				Thread.Sleep(50);
			}
			if (json == null)
			{
				throw new FileNotFoundException("正版玩家档案不存在（" + name + "）");
			}
			text = json["id"]?.ToString() ?? string.Empty;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "从官网获取正版 UUID 失败（" + name + "）");
			if (throwOnNotFound || !(ex is FileNotFoundException))
			{
				throw;
			}
			text = GetOfflineUuid(name, isSplited: false, isLegacy: true);
		}
		if (text.Length != 32)
		{
			throw new FormatException("获取的正版 UUID 长度不足（" + text + "）");
		}
		ModBase.WriteIni(ModBase.pathTemp + "Cache\\Uuid\\Mojang.ini", name, text);
		return text;
	}

	public static bool CanCreateOtherProfile()
	{
		if (!ProfileService.HasMicrosoftProfile && (!Lang.IsFeaturesUnrestricted || ProfileService.Profiles.Count <= 0))
		{
			return !NetworkHelper.IsNetworkAvailable();
		}
		return true;
	}

	public static void CreateProfile()
	{
		int? selected = null;
		ModBase.RunInUiWait(() =>
		{
			bool includeOthers = CanCreateOtherProfile();
			selected = ModMain.MyMsgBoxSelect(_GetAvailableProfileSelection(includeOthers), Lang.Text("Launch.Account.Profile.Create.SelectAuthType.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"));
		});
		if (!selected.HasValue)
		{
			return;
		}
		ProfileService.IsCreatingProfile = true;
		ModLaunch.McLoginType type = selected.Value switch
		{
			0 => ModLaunch.McLoginType.Ms, 
			1 => ModLaunch.McLoginType.Auth, 
			_ => ModLaunch.McLoginType.Legacy, 
		};
		if (type == ModLaunch.McLoginType.Auth)
		{
			string server = null;
			ModBase.RunInUiWait(() => server = ModMain.MyMsgBoxAuthServer("https://littleskin.cn/api/yggdrasil", PageLoginAuth.PredefinedAuthServers));
			if (string.IsNullOrWhiteSpace(server))
			{
				ProfileService.IsCreatingProfile = false;
				return;
			}
			PageLoginAuth.draggedAuthServerOAuthSupported = PageLoginAuth.IsOAuthSupportedAsync(server).GetAwaiter().GetResult();
			PageLoginAuth.draggedAuthServer = server;
		}
		ModBase.RunInUi(() =>
		{
			ModMain.frmLaunchLeft.RefreshPage(anim: true, type);
		});
	}

	private static List<IMyRadio> _GetAvailableProfileSelection(bool includeOthers)
	{
		int num;
		if (!includeOthers)
		{
			num = 1;
			List<IMyRadio> list = new List<IMyRadio>(num);
			CollectionsMarshal.SetCount(list, num);
			CollectionsMarshal.AsSpan(list)[0] = new MyListItem
			{
				Title = Lang.Text("Launch.Account.Type.Microsoft"),
				Type = MyListItem.CheckType.RadioBox,
				SvgIcon = "lucide/shield-check"
			};
			return list;
		}
		num = 3;
		List<IMyRadio> list2 = new List<IMyRadio>(num);
		CollectionsMarshal.SetCount(list2, num);
		Span<IMyRadio> span = CollectionsMarshal.AsSpan(list2);
		span[0] = new MyListItem
		{
			Title = Lang.Text("Launch.Account.Type.Microsoft"),
			Type = MyListItem.CheckType.RadioBox,
			SvgIcon = "lucide/shield-check"
		};
		span[1] = new MyListItem
		{
			Title = Lang.Text("Launch.Account.Type.ThirdParty"),
			Type = MyListItem.CheckType.RadioBox,
			SvgIcon = "lucide/network"
		};
		span[2] = new MyListItem
		{
			Title = Lang.Text("Launch.Account.Type.Offline"),
			Type = MyListItem.CheckType.RadioBox,
			SvgIcon = "lucide/link-2-off"
		};
		return list2;
	}

	public static void EditProfileId()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Invalid comparison between Unknown and I4
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Invalid comparison between Unknown and I4
		McProfile profile = ProfileService.Current;
		if (profile == null)
		{
			return;
		}
		if ((int)((SafeProfile)profile).ProfileType == 0)
		{
			string username = null;
			ModBase.RunInUiWait(() =>
			{
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0038: Expected Obj, but got Unknown
				//IL_0043: Unknown result type (might be due to invalid IL or missing references)
				//IL_004d: Expected Obj, but got Unknown
				return username = ModMain.MyMsgBoxInput(Lang.Text("Launch.Account.Profile.EditPlayerId.Title"), Lang.Text("Launch.Account.Profile.EditPlayerId.MicrosoftWarning"), ((SafeProfile)profile).UserName, new Collection<IValidator<string>>
				{
					(IValidator<string>)new StringLengthValidator(3, 16),
					(IValidator<string>)new RegexValidator("([A-z]|[0-9]|_)+", "正则检查失败！")
				}, Lang.Text("Launch.Account.Profile.EditPlayerId.Hint"), Lang.Text("Common.Action.Confirm"));
			});
			if (string.IsNullOrWhiteSpace(username) || ModMain.MyMsgBox(Lang.Text("Launch.Account.Profile.EditPlayerId.Confirm.Message"), Lang.Text("Launch.Account.Profile.EditPlayerId.Confirm.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"), "", isWarn: true) == 2)
			{
				return;
			}
			ModBase.RunInNewThread(() =>
			{
				try
				{
					string text = ((JsonObject)ModBase.GetJson(Requester.Fetch("https://api.minecraftservices.com/minecraft/profile/name/" + username + "/available", new FetchParam
					{
						Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer " + ((SafeProfile)profile).AccessToken }
					})))["status"]?.ToString();
					if ((text == "DUPLICATE" || text == "NOT_ALLOWED") ? true : false)
					{
						ModMain.MyMsgBox((text == "DUPLICATE") ? Lang.Text("Launch.Account.Profile.EditPlayerId.Duplicate") : Lang.Text("Launch.Account.Profile.EditPlayerId.NotAllowed"), Lang.Text("Launch.Account.Profile.EditPlayerId.Failed.Title"), Lang.Text("Common.Action.Confirm"), "", "", isWarn: true);
					}
					else
					{
						JsonObject jsonObject = (JsonObject)ModBase.GetJson(Requester.Fetch("https://api.minecraftservices.com/minecraft/profile/name/" + username, new FetchParam
						{
							Method = "PUT",
							ContentType = "application/json",
							Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer " + ((SafeProfile)profile).AccessToken }
						}));
						McProfile val = profile.Clone();
						((SafeProfile)val).UserName = jsonObject["name"]?.ToString() ?? username;
						ProfileService.Update(profile, val);
						ProfileService.Select(val);
						HintService.Hint(Lang.Text("Launch.Account.Profile.EditPlayerId.Success", new object[1] { ((SafeProfile)val).UserName }), HintType.Success);
						ModBase.RunInUi(() =>
						{
							ModMain.frmLoginProfileSkin?.Reload();
							ModMain.frmLaunchLeft.RefreshPage(anim: true);
						});
					}
				}
				catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
				{
					ModMain.MyMsgBox(Lang.Text("Launch.Account.Profile.EditPlayerId.Cooldown"), Lang.Text("Launch.Account.Profile.EditPlayerId.Failed.Title"), Lang.Text("Common.Action.Confirm"));
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, Lang.Text("Launch.Account.Profile.Error.ChangeId"), ModBase.LogLevel.Msgbox, null, Lang.Text("Launch.Account.Profile.Error.ChangeId"));
				}
			});
		}
		else if ((int)((SafeProfile)profile).ProfileType == 2 || (int)((SafeProfile)profile).ProfileType == 3)
		{
			if (!string.IsNullOrWhiteSpace(profile.Server))
			{
				ModBase.OpenWebsite(profile.Server.Replace("/api/yggdrasil/authserver", "/user/profile"));
			}
		}
		else
		{
			string newName = null;
			ModBase.RunInUiWait(() =>
			{
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Expected Obj, but got Unknown
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_0043: Expected Obj, but got Unknown
				return newName = ModMain.MyMsgBoxInput(Lang.Text("Launch.Account.Profile.EditPlayerId.Title"), "", ((SafeProfile)profile).UserName, new Collection<IValidator<string>>
				{
					(IValidator<string>)new StringLengthValidator(3, 16),
					(IValidator<string>)new RegexValidator("([A-z]|[0-9]|_)+", "正则检查失败！")
				}, Lang.Text("Launch.Account.Profile.EditPlayerId.Hint"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"));
			});
			if (!string.IsNullOrWhiteSpace(newName))
			{
				EditOfflineUuid(profile, GetOfflineUuid(newName));
			}
		}
	}

	public static void EditOfflineUuid(McProfile profile, string? uuid = null)
	{
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected Obj, but got Unknown
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Expected Obj, but got Unknown
		McProfile val = ProfileService.Profiles.FirstOrDefault((McProfile p) => ((SafeProfile)p).ProfileId == ((SafeProfile)profile).ProfileId);
		if (val == null)
		{
			return;
		}
		string text = uuid;
		string text2;
		if (text == null)
		{
			int? type = null;
			ModBase.RunInUiWait(() =>
			{
				int num = 3;
				List<IMyRadio> list = new List<IMyRadio>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<IMyRadio> span = CollectionsMarshal.AsSpan(list);
				span[0] = new MyRadioBox
				{
					Text = Lang.Text("Launch.Account.Profile.Uuid.Standard")
				};
				span[1] = new MyRadioBox
				{
					Text = Lang.Text("Launch.Account.Profile.Uuid.Legacy")
				};
				span[2] = new MyRadioBox
				{
					Text = Lang.Text("Common.Option.Customize")
				};
				return type = ModMain.MyMsgBoxSelect(list, Lang.Text("Launch.Account.Profile.Uuid.SelectType.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel"));
			});
			if (!type.HasValue)
			{
				return;
			}
			if (!type.HasValue)
			{
				goto IL_00b2;
			}
			switch (type.GetValueOrDefault())
			{
			case 0:
				break;
			case 1:
				goto IL_00a1;
			default:
				goto IL_00b2;
			}
			text2 = GetOfflineUuid(((SafeProfile)val).UserName);
			goto IL_012e;
		}
		goto IL_0131;
		IL_0131:
		if (!string.IsNullOrWhiteSpace(text))
		{
			McProfile val2 = val.Clone();
			((SafeProfile)val2).Uuid = text;
			ProfileService.Update(val, val2);
			ProfileService.Select(val2);
			HintService.Hint(Lang.Text("Launch.Account.Profile.Saved"), HintType.Success);
		}
		return;
		IL_00a1:
		text2 = GetOfflineUuid(((SafeProfile)val).UserName, isSplited: false, isLegacy: true);
		goto IL_012e;
		IL_00b2:
		text2 = ModMain.MyMsgBoxInput(Lang.Text("Launch.Account.Profile.Uuid.ChangeTitle", new object[1] { ((SafeProfile)val).UserName }), "", ((SafeProfile)val).Uuid, hintText: Lang.Text("Launch.Account.Profile.Uuid.Hint"), validateRules: new Collection<IValidator<string>>
		{
			(IValidator<string>)new StringLengthValidator(32, 32),
			(IValidator<string>)new RegexValidator("([A-z]|[0-9]){32}", Lang.Text("Launch.Account.Profile.Uuid.InvalidChars"))
		}, button1: Lang.Text("Common.Action.Continue"), button2: Lang.Text("Common.Action.Cancel"));
		goto IL_012e;
		IL_012e:
		text = text2;
		goto IL_0131;
	}

	public static void EditAuthServerName(McProfile profile, string serverName)
	{
		McProfile val = ProfileService.Profiles.FirstOrDefault((McProfile p) => ((SafeProfile)p).ProfileId == ((SafeProfile)profile).ProfileId);
		if (val != null)
		{
			McProfile val2 = val.Clone();
			val2.ServerName = serverName;
			ProfileService.Update(val, val2);
			if (ProfileService.Current == val)
			{
				ProfileService.Select(val2);
			}
			HintService.Hint(Lang.Text("Launch.Account.Profile.Saved"), HintType.Success);
		}
	}

	public static void RemoveProfile(McProfile profile)
	{
		ProfileService.Remove(profile);
		HintService.Hint(Lang.Text("Launch.Account.Profile.Deleted"), HintType.Success);
	}

	public static string GetOfflineUuid(string userName, bool isSplited = false, bool isLegacy = false)
	{
		if (isLegacy)
		{
			string text = ModBase.StrFill(userName.Length.ToString("X"), "0", 16) + ModBase.StrFill(ModBase.GetHash(userName).ToString("X"), "0", 16);
			return text.Substring(0, 12) + "3" + text.Substring(13, 3) + "9" + text.Substring(17, 15);
		}
		using MD5 mD = MD5.Create();
		byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + userName));
		array[6] = (byte)((array[6] & 0xF) | 0x30);
		array[8] = (byte)((array[8] & 0x3F) | 0x80);
		Guid guid = Guid.Parse(_ToUuidString(array));
		return isSplited ? guid.ToString() : guid.ToString("N");
	}

	private static string _ToUuidString(byte[] bytes)
	{
		long num = 0L;
		long num2 = 0L;
		for (int i = 0; i <= 7; i++)
		{
			num = (num << 8) | (bytes[i] & 0xFF);
		}
		for (int j = 8; j <= 15; j++)
		{
			num2 = (num2 << 8) | (bytes[j] & 0xFF);
		}
		return $"{_Digits(num >> 32, 8)}-{_Digits(num >> 16, 4)}-{_Digits(num, 4)}-{_Digits(num2 >> 48, 4)}-{_Digits(num2, 12)}";
	}

	private static string _Digits(long value, int digits)
	{
		long num = 1L << digits * 4;
		return (num | (value & (num - 1))).ToString("X").Substring(1);
	}

	public static string GetProfileInfo(McProfile profile)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected I4, but got Unknown
		ProfileType profileType = ((SafeProfile)profile).ProfileType;
		string text = (int)profileType switch
		{
			2 => Lang.Text("Launch.Account.Type.ThirdParty") + (string.IsNullOrWhiteSpace(profile.ServerName) ? "" : (" / " + profile.ServerName)), 
			3 => Lang.Text("Launch.Account.Type.ThirdParty") + (string.IsNullOrWhiteSpace(profile.ServerName) ? " / Yggdrasil Connect" : (" / " + profile.ServerName)), 
			0 => Lang.Text("Launch.Account.Type.Microsoft"), 
			_ => Lang.Text("Launch.Account.Type.Offline"), 
		};
		if (!string.IsNullOrWhiteSpace(profile.Description))
		{
			return text + "，" + profile.Description;
		}
		return text;
	}

	public static ModLaunch.McLoginData GetLoginData(ModLaunch.McLoginType targetAuthType = (ModLaunch.McLoginType)0)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected I4, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		McProfile current = ProfileService.Current;
		if (current == null)
		{
			return targetAuthType switch
			{
				ModLaunch.McLoginType.Ms => (ModLaunch.McLoginData)new ModLaunch.McLoginMs(), 
				ModLaunch.McLoginType.Auth => new ModLaunch.McLoginServer(ModLaunch.McLoginType.Auth)
				{
					Description = "Authlib-Injector",
					IsExist = false
				}, 
				_ => new ModLaunch.McLoginLegacy(), 
			};
		}
		ProfileType profileType = ((SafeProfile)current).ProfileType;
		return (int)profileType switch
		{
			0 => (ModLaunch.McLoginData)new ModLaunch.McLoginMs(), 
			2 => new ModLaunch.McLoginServer(ModLaunch.McLoginType.Auth)
			{
				BaseUrl = current.Server,
				UserName = current.LoginName,
				Password = current.Password,
				Description = (current.ServerName ?? "Authlib-Injector"),
				IsExist = true
			}, 
			3 => new ModLaunch.McLoginServer(ModLaunch.McLoginType.Auth)
			{
				BaseUrl = current.Server,
				Description = (current.ServerName ?? "Yggdrasil Connect"),
				IsExist = true,
				ProviderType = (ProfileType)3,
				DiscoveryAddress = current.DiscoveryAddress
			}, 
			_ => new ModLaunch.McLoginLegacy
			{
				UserName = ((SafeProfile)current).UserName,
				Uuid = ((SafeProfile)current).Uuid
			}, 
		};
	}

	public static string IsProfileValid()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		McProfile current = ProfileService.Current;
		if (current == null)
		{
			return Lang.Text("Minecraft.Launch.Precheck.NoProfile");
		}
		if ((int)((SafeProfile)current).ProfileType != 1)
		{
			return string.Empty;
		}
		if (string.IsNullOrWhiteSpace(((SafeProfile)current).UserName))
		{
			return Lang.Text("Launch.Account.Profile.Validation.EmptyUsername");
		}
		if (((SafeProfile)current).UserName.Contains('"'))
		{
			return Lang.Text("Launch.Account.Profile.Validation.QuoteInUsername");
		}
		if ((object)ModInstanceList.McMcInstanceSelected != null && ModInstanceList.McMcInstanceSelected.Info.Drop >= 203 && ((SafeProfile)current).UserName.Trim().Length > 16)
		{
			return Lang.Text("Launch.Account.Profile.Validation.UsernameTooLong");
		}
		return string.Empty;
	}

	public static void ChangeSkinMs()
	{
		if (Interlocked.Exchange(ref _isMsSkinChanging, 1) != 0)
		{
			HintService.Hint(Lang.Text("Launch.Skin.Change.Busy"));
			return;
		}
		McProfile current = ProfileService.Current;
		if (current == null)
		{
			Interlocked.Exchange(ref _isMsSkinChanging, 0);
			return;
		}
		string profileId = ((SafeProfile)current).ProfileId;
		if (ModLaunch.mcLoginLoader.State == ModBase.LoadState.Failed)
		{
			Interlocked.Exchange(ref _isMsSkinChanging, 0);
			HintService.Hint(Lang.Text("Launch.Skin.Change.LoginFailed"), HintType.Error);
			return;
		}
		ModSkin.McSkinInfo skinInfo = ModSkin.McSkinSelect();
		if (!skinInfo.IsVaild)
		{
			Interlocked.Exchange(ref _isMsSkinChanging, 0);
			return;
		}
		HintService.Hint(Lang.Text("Launch.Skin.Change.Starting"));
		ModBase.RunInNewThread(() =>
		{
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				bool flag = false;
				while (true)
				{
					if (ModLaunch.mcLoginMsLoader.State == ModBase.LoadState.Loading)
					{
						ModLaunch.mcLoginMsLoader.WaitForExit();
					}
					if (ModLaunch.mcLoginMsLoader.State != ModBase.LoadState.Finished)
					{
						ModLaunch.mcLoginMsLoader.WaitForExit(GetLoginData());
					}
					if (ModLaunch.mcLoginMsLoader.State != ModBase.LoadState.Finished)
					{
						throw new InvalidOperationException("Microsoft login failed");
					}
					McProfile? val = ProfileService.Profiles.FirstOrDefault((McProfile item) => ((SafeProfile)item).ProfileId == profileId);
					if (val == null)
					{
						break;
					}
					McProfile val2 = val;
					using MultipartFormDataContent content = new MultipartFormDataContent
					{
						{
							new StringContent(skinInfo.IsSlim ? "slim" : "classic"),
							"variant"
						},
						{
							new ByteArrayContent(ModBase.ReadFileBytes(skinInfo.LocalFile)),
							"file",
							ModBase.GetFileNameFromPath(skinInfo.LocalFile)
						}
					};
					string text = Requester.Fetch("https://api.minecraftservices.com/minecraft/profile/skins", new FetchParam
					{
						Method = "POST",
						Content = content,
						Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer " + ((SafeProfile)val2).AccessToken }
					});
					if (!text.Contains("request requires user authentication"))
					{
						JsonObject jsonObject = (JsonObject)ModBase.GetJson(text);
						JsonObject jsonObject2 = jsonObject["skins"]?.AsArray().FirstOrDefault((JsonNode s) => s?["state"]?.ToString() == "ACTIVE")?.AsObject();
						if (jsonObject2?["url"] != null)
						{
							MySkin.ReloadCache(jsonObject2["url"].ToString(), ((SafeProfile)val2).Uuid);
							return;
						}
						throw new InvalidDataException(jsonObject["errorMessage"]?.ToString() ?? text);
					}
					if (flag)
					{
						throw new IdentityModelAuthenticationException("invalid_token", "Microsoft authentication remained invalid.", (Exception)null);
					}
					flag = true;
					HintService.Hint(Lang.Text("Launch.Skin.Change.Reauthenticating"));
					ModLaunch.mcLoginMsLoader.Start(GetLoginData(), isForceRestart: true);
				}
				throw new InvalidOperationException("Microsoft profile no longer exists.");
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, Lang.Text("Launch.Account.Profile.Error.ChangeSkin"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Account.Profile.Error.ChangeSkin"));
			}
			finally
			{
				Interlocked.Exchange(ref _isMsSkinChanging, 0);
			}
		}, "Ms Skin Upload");
	}
}
