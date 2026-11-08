using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using FluentValidation;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Authentication;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.Minecraft.Yggdrasil;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Validate;

namespace PCL;

public partial class PageLoginAuth : Grid, IComponentConnector
{
	public static string draggedAuthServer;

	public static bool? draggedAuthServerOAuthSupported;

	internal const string DefaultAuthServer = "https://littleskin.cn/api/yggdrasil";

	internal static readonly IReadOnlyDictionary<string, string> PredefinedAuthServers = new Dictionary<string, string>
	{
		{
			Lang.Text("Launch.Account.Auth.Preset.LittleSkin"),
			"https://littleskin.cn/api/yggdrasil"
		},
		{
			Lang.Text("Common.Option.Customize"),
			""
		}
	};

	private bool _isRegisterMode = true;

	private bool _isOAuthMode;

	private bool? _oauthSupported;

	private bool _hasRegisterLink;

	private bool _loginModeInitialized;

	private string _authServerUrl = "";

	public PageLoginAuth()
	{
		InitializeComponent();
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Reload();
		};
		Loaded += (object _, RoutedEventArgs _) =>
		{
			ReloadRegisterButton();
		};
		BtnBack.Click += BtnBack_Click;
		BtnBackOAuth.Click += BtnBack_Click;
		BtnLogin.Click += BtnLogin_Click;
		BtnOAuth.Click += BtnLogin_Click;
		BtnUsePassword.Click += (object _, EventArgs _) =>
		{
			_SetLoginMode(useOAuth: false);
		};
		BtnUseOAuth.Click += (object _, EventArgs _) =>
		{
			_SetLoginMode(useOAuth: true);
		};
		BtnWebsite.Click += BtnWebsite_Click;
		BtnPasswordWebsite.Click += BtnWebsite_Click;
		BtnLink.Click += Btn_Click;
	}

	private void Reload()
	{
		_authServerUrl = draggedAuthServer ?? "";
		bool? oauthSupported = draggedAuthServerOAuthSupported;
		draggedAuthServer = null;
		draggedAuthServerOAuthSupported = null;
		_oauthSupported = oauthSupported;
		_UpdateOAuthEntryVisibility();
		_SetLoginMode(oauthSupported == true);
		if (oauthSupported.HasValue || string.IsNullOrWhiteSpace(_authServerUrl))
		{
			return;
		}
		string server = _authServerUrl;
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			bool flag = await IsOAuthSupportedAsync(server).ConfigureAwait(continueOnCapturedContext: true);
			if (string.Equals(_authServerUrl, server, StringComparison.OrdinalIgnoreCase))
			{
				_oauthSupported = flag;
				_UpdateOAuthEntryVisibility();
				_SetLoginMode(flag);
			}
		}), Array.Empty<object>());
	}

	private void BtnBack_Click(object sender, EventArgs e)
	{
		ProfileService.IsCreatingProfile = false;
		_authServerUrl = "";
		TextName.Text = null;
		TextPass.Password = null;
		ModMain.frmLaunchLeft.RefreshPage(anim: true);
	}

	private void BtnLogin_Click(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(_authServerUrl))
		{
			HintService.Hint(Lang.Text("Launch.Account.Auth.EmptyFields"), HintType.Error);
			return;
		}
		if (!StringExtension.IsMatch(_authServerUrl, RegexPatterns.HttpUri))
		{
			HintService.Hint(Lang.Text("Launch.Account.Auth.InvalidServer"), HintType.Error);
			return;
		}
		BtnLogin.IsEnabled = false;
		BtnOAuth.IsEnabled = false;
		BtnBack.IsEnabled = false;
		BtnBackOAuth.IsEnabled = false;
		BtnUsePassword.IsEnabled = false;
		BtnUseOAuth.IsEnabled = false;
		BtnWebsite.IsEnabled = false;
		BtnPasswordWebsite.IsEnabled = false;
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
		{
			bool keepControlsDisabled = false;
			try
			{
				ProfileService.IsCreatingProfile = true;
				if (_isOAuthMode)
				{
					if (!(await _TryStartYggdrasilConnectAsync().ConfigureAwait(continueOnCapturedContext: true)))
					{
						HintService.Hint(Lang.Text("Launch.Account.Auth.LoginFailed"), HintType.Error);
					}
					else
					{
						keepControlsDisabled = true;
					}
				}
				else if (string.IsNullOrWhiteSpace(TextName.Text) || string.IsNullOrWhiteSpace(TextPass.Password))
				{
					HintService.Hint(Lang.Text("Launch.Account.Auth.EmptyFields"), HintType.Error);
				}
				else
				{
					ModLaunch.McLoginServer mcLoginServer = new ModLaunch.McLoginServer(ModLaunch.McLoginType.Auth);
					ModLaunch.McLoginServer mcLoginServer2 = mcLoginServer;
					mcLoginServer2.BaseUrl = await ApiLocation.TryRequestAsync(_authServerUrl).ConfigureAwait(continueOnCapturedContext: true);
					mcLoginServer.UserName = TextName.Text;
					mcLoginServer.Password = TextPass.Password;
					mcLoginServer.Description = "Authlib-Injector";
					mcLoginServer.LoginType = ModLaunch.McLoginType.Auth;
					ModLaunch.mcLoginAuthLoader.Start(mcLoginServer, isForceRestart: true);
					while (ModLaunch.mcLoginAuthLoader.State == ModBase.LoadState.Loading)
					{
						BtnLogin.Text = Lang.Number<double>(ModLaunch.mcLoginAuthLoader.Progress, "P0");
						await Task.Delay(50);
					}
					switch (ModLaunch.mcLoginAuthLoader.State)
					{
					case ModBase.LoadState.Finished:
						ModMain.frmLaunchLeft.RefreshPage(anim: true);
						break;
					case ModBase.LoadState.Aborted:
						HintService.Hint(Lang.Text("Launch.Account.Auth.Cancelled"));
						break;
					default:
						if (ModLaunch.mcLoginAuthLoader.Error == null)
						{
							throw new InvalidOperationException(Lang.Text("Launch.Account.Microsoft.Error.Unknown"));
						}
						throw ModLaunch.mcLoginAuthLoader.Error;
					}
				}
			}
			catch (Exception ex)
			{
				if (!(ex.Message == "$$"))
				{
					if (ex.Message.StartsWith("$"))
					{
						HintService.Hint(Lang.Text("Launch.Account.Auth.LoginFailed.WithDetail", new object[1] { ex.Message.TrimStart('$') }), HintType.Error);
					}
					else
					{
						ModBase.Log(ex, Lang.Text("Launch.Account.Auth.LoginFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Launch.Account.Auth.LoginFailed"));
					}
				}
			}
			finally
			{
				if (!keepControlsDisabled)
				{
					_FinishLoginAttempt();
				}
			}
		}), Array.Empty<object>());
	}

	private void _FinishLoginAttempt()
	{
		ProfileService.IsCreatingProfile = false;
		BtnLogin.IsEnabled = true;
		BtnOAuth.IsEnabled = true;
		BtnBack.IsEnabled = true;
		BtnBackOAuth.IsEnabled = true;
		BtnUsePassword.IsEnabled = true;
		BtnUseOAuth.IsEnabled = true;
		BtnWebsite.IsEnabled = true;
		BtnPasswordWebsite.IsEnabled = true;
		BtnLogin.Text = Lang.Text("Launch.Account.Auth.Login");
	}

	private void _SetLoginMode(bool useOAuth)
	{
		if (_loginModeInitialized && _isOAuthMode == useOAuth)
		{
			return;
		}
		bool flag = _loginModeInitialized && _isOAuthMode;
		_isOAuthMode = useOAuth;
		_loginModeInitialized = true;
		FrameworkElement[] oldElements = ((!flag) ? new FrameworkElement[8] { TextName, TextNameTitle, TextPass, TextPassTitle, BtnLink, BtnLogin, PanPasswordLinks, BtnBack } : new FrameworkElement[4] { PanOAuth, BtnUsePassword, BtnWebsite, BtnBackOAuth });
		FrameworkElement[] newElements = ((!useOAuth) ? new FrameworkElement[8] { TextName, TextNameTitle, TextPass, TextPassTitle, BtnLink, BtnLogin, PanPasswordLinks, BtnBack } : new FrameworkElement[4] { PanOAuth, BtnUsePassword, BtnWebsite, BtnBackOAuth });
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		FrameworkElement[] array;
		if (flag || !_loginModeInitialized)
		{
			array = oldElements;
			foreach (FrameworkElement frameworkElement in array)
			{
				if (frameworkElement.Visibility == Visibility.Visible)
				{
					list.Add(ModAnimation.AaOpacity(frameworkElement, 0.0 - frameworkElement.Opacity, 90));
				}
			}
		}
		list.Add(ModAnimation.AaCode(() =>
		{
			FrameworkElement[] array2 = oldElements;
			foreach (FrameworkElement obj in array2)
			{
				obj.Visibility = Visibility.Collapsed;
				obj.Opacity = 1.0;
			}
			array2 = newElements;
			foreach (FrameworkElement obj2 in array2)
			{
				obj2.Visibility = ((obj2 == BtnLink && !_hasRegisterLink) ? Visibility.Collapsed : Visibility.Visible);
				obj2.Opacity = 0.0;
			}
		}, 0, after: true));
		array = newElements;
		foreach (FrameworkElement frameworkElement2 in array)
		{
			double value = ((frameworkElement2 == BtnBackOAuth || frameworkElement2 == PanPasswordLinks) ? 0.3 : 1.0);
			list.Add(ModAnimation.AaOpacity(frameworkElement2, value, 130, 20, new ModAnimation.AniEaseInFluent()));
		}
		ModAnimation.AniStart(list, "Profile Authentication Mode");
	}

	private void _UpdateOAuthEntryVisibility()
	{
		bool valueOrDefault = _oauthSupported == true;
		BtnUseOAuth.Visibility = ((!valueOrDefault) ? Visibility.Collapsed : Visibility.Visible);
		((DependencyObject)BtnPasswordWebsite).SetValue(Grid.ColumnSpanProperty, (object)(valueOrDefault ? 1 : 3));
	}

	private async Task<bool> _TryStartYggdrasilConnectAsync()
	{
		string server = await ApiLocation.TryRequestAsync(_authServerUrl).ConfigureAwait(continueOnCapturedContext: true);
		using HttpResponseMessage response = await HttpSenderExtension.SendAsync(HttpRequest.Create(server), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: true);
		if (!response.IsSuccess)
		{
			return false;
		}
		JsonObject jsonObject = (JsonObject)ModBase.GetJson(await response.AsStringAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: true));
		string discovery = jsonObject["meta"]?["feature.openid_configuration_url"]?.ToString() ?? jsonObject["feature.openid_configuration_url"]?.ToString();
		if (string.IsNullOrWhiteSpace(discovery))
		{
			return false;
		}
		using HttpResponseMessage discoveryResponse = await HttpSenderExtension.SendAsync(HttpRequest.Create(discovery), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: true);
		if (!discoveryResponse.IsSuccess)
		{
			return false;
		}
		JsonObject discoveryMetadata = (JsonObject)ModBase.GetJson(await discoveryResponse.AsStringAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: true));
		string text = _GetClientId(server, discoveryMetadata);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		ModBase.Log("[Profile] Yggdrasil Connect client id resolved for " + new Uri(server).Host, ModBase.LogLevel.Debug);
		_CompleteYggdrasilConnectLoginAsync(server, discovery, text);
		return true;
	}

	private async Task _CompleteYggdrasilConnectLoginAsync(string server, string discovery, string clientId)
	{
		try
		{
			McProfile val = await ProfileService.AuthenticateAsync((ProfileType)3, new AuthenticationRequest
			{
				Server = server,
				DiscoveryAddress = discovery,
				ClientId = clientId,
				DeviceCodeHandler = ProfileUi.ShowDeviceCodeLoginAsync,
				ProfileSelector = (IReadOnlyList<AuthenticationCandidate> candidates, CancellationToken _) => Task.FromResult<AuthenticationCandidate>(_SelectYggdrasilProfile(candidates))
			}, (McProfile)null, true, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			ModBase.RunInUi(() =>
			{
				ModMain.frmLaunchLeft.RefreshPage(anim: true);
			});
			LogWrapper.Info("Profile", "Yggdrasil Connect 登录成功：" + ((SafeProfile)val).UserName);
		}
		catch (ThreadInterruptedException)
		{
			HintService.Hint(Lang.Text("Launch.Account.Auth.Cancelled"));
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, Lang.Text("Launch.Account.Auth.LoginFailed"), ModBase.LogLevel.Msgbox, null, Lang.Text("Launch.Account.Auth.LoginFailed"));
		}
		finally
		{
			ModBase.RunInUi(_FinishLoginAttempt);
		}
	}

	private static AuthenticationCandidate? _SelectYggdrasilProfile(IReadOnlyList<AuthenticationCandidate> candidates)
	{
		if (candidates.Count == 0)
		{
			return null;
		}
		if (candidates.Count == 1)
		{
			return candidates[0];
		}
		AuthenticationCandidate selected = null;
		ModBase.RunInUiWait(() =>
		{
			int? num = ModMain.MyMsgBoxSelect(((IEnumerable<AuthenticationCandidate>)candidates).Select((Func<AuthenticationCandidate, IMyRadio>)((AuthenticationCandidate candidate) => new MyRadioBox
			{
				Text = candidate.Name
			})).ToList(), Lang.Text("Launch.Account.Auth.ChangeProfile"));
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault >= 0 && valueOrDefault < 100000)
				{
					selected = candidates[num.Value];
				}
			}
		});
		return selected;
	}

	internal static async Task<bool> IsOAuthSupportedAsync(string authServerUrl)
	{
		string text = default;
		if (Uri.TryCreate(authServerUrl, UriKind.Absolute, out Uri result) && YggdrasilConnectProvider.TryGetBuiltInClientId(result.Host, out text))
		{
			return true;
		}
		try
		{
			string server = await ApiLocation.TryRequestAsync(authServerUrl).ConfigureAwait(continueOnCapturedContext: false);
			using HttpResponseMessage response = await HttpSenderExtension.SendAsync(HttpRequest.Create(server), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
			if (!response.IsSuccess)
			{
				return false;
			}
			JsonObject jsonObject = (JsonObject)ModBase.GetJson(await response.AsStringAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false));
			string text2 = jsonObject["meta"]?["feature.openid_configuration_url"]?.ToString() ?? jsonObject["feature.openid_configuration_url"]?.ToString();
			if (string.IsNullOrWhiteSpace(text2))
			{
				return false;
			}
			using HttpResponseMessage discoveryResponse = await HttpSenderExtension.SendAsync(HttpRequest.Create(text2), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
			if (!discoveryResponse.IsSuccess)
			{
				return false;
			}
			JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(await discoveryResponse.AsStringAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false));
			string? value = _GetClientId(server, jsonObject2);
			string value2 = jsonObject2["device_authorization_endpoint"]?.ToString();
			string value3 = jsonObject2["token_endpoint"]?.ToString();
			return !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(value2) && !string.IsNullOrWhiteSpace(value3);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "检测 Yggdrasil Connect 支持失败");
			return false;
		}
	}

	private static string? _GetClientId(string authServerUrl, JsonObject discoveryMetadata)
	{
		if (!Uri.TryCreate(authServerUrl, UriKind.Absolute, out Uri result))
		{
			return null;
		}
		string result2 = default;
		if (YggdrasilConnectProvider.TryGetBuiltInClientId(result.Host, out result2))
		{
			return result2;
		}
		return discoveryMetadata["shared_client_id"]?.ToString();
	}

	private void ComboName_TextChanged(object sender, TextChangedEventArgs e)
	{
		_isRegisterMode = string.IsNullOrEmpty(TextName.Text);
		BtnLink.Content = (_isRegisterMode ? Lang.Text("Launch.Account.Auth.Register") : Lang.Text("Launch.Account.Auth.ForgotPassword"));
	}

	private void Btn_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite(_isRegisterMode ? ((object)Config.InstanceAuth.AuthRegisterAddress).ToString() : ((object)Config.InstanceAuth.AuthRegisterAddress).ToString().Replace("/auth/register", "/auth/forgot"));
	}

	private void BtnWebsite_Click(object sender, EventArgs e)
	{
		ModBase.OpenWebsite(new UriBuilder(new Uri(_authServerUrl))
		{
			Path = "/",
			Query = "",
			Fragment = ""
		}.Uri.ToString());
	}

	private void ReloadRegisterButton()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected Obj, but got Unknown
		string text = ((object)Config.InstanceAuth.AuthRegisterAddress).ToString();
		_hasRegisterLink = ((AbstractValidator<string>)new HttpValidator()).Validate(text).IsValid;
		BtnLink.Visibility = ((_isOAuthMode || !_hasRegisterLink) ? Visibility.Collapsed : Visibility.Visible);
	}

}
