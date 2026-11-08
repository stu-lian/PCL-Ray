using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Minecraft.IdentityModel;
using PCL.Core.Minecraft.IdentityModel.OAuth;
using PCL.Core.UI.Controls;
using PCL.Core.Utils;

namespace PCL;

public partial class MyMsgLogin : Grid, IComponentConnector
{
	private readonly JsonObject data;

	private string userCode;

	private string website;

	private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

	private readonly ModMain.MyMsgBoxConverter myConverter;

	private readonly int uuid = ModBase.GetUuid();

	internal BlurBorder PanBorder;

	public MyMsgLogin()
	{
		InitializeComponent();
		Loaded += Load;
		Btn1.Click += Btn1_Click;
		Btn3.Click += Btn3_Click;
		((UIElement)(object)PanBorder).MouseLeftButtonDown += Drag;
		LabTitle.MouseLeftButtonDown += Drag;
	}

	private void Finished(object result)
	{
		if (!myConverter.IsExited)
		{
			myConverter.IsExited = true;
			_cancellation.Cancel();
			myConverter.Result = result;
			myConverter.CompletionHandler?.Invoke(result);
			ModBase.RunInUi(Close);
			Thread.Sleep(200);
			ModMain.frmMain.ShowWindowToTop();
		}
	}

	private void Init()
	{
		userCode = (string?)data["user_code"];
		if (data["verification_uri_complete"] != null)
		{
			website = (string?)data["verification_uri_complete"];
			LabCaption.Text = Lang.Text("Launch.Account.LoginDialog.MicrosoftInstructions.WithAutoFill", new object[2] { userCode, website });
		}
		else
		{
			website = (string?)data["verification_uri"];
			LabCaption.Text = Lang.Text("Launch.Account.LoginDialog.MicrosoftInstructions", new object[2] { userCode, website });
		}
		LabTitle.Text = Lang.Text("Launch.Account.LoginDialog.MinecraftLogin");
		CustomEventService.SetEventData((DependencyObject)(object)Btn1, website);
		CustomEventService.SetEventData((DependencyObject)(object)Btn2, userCode);
		WorkThreadAsync();
	}

	private async Task WorkThreadAsync()
	{
		CancellationToken token = _cancellation.Token;
		try
		{
			await Task.Delay(2000, token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException) when (myConverter.IsExited)
		{
			return;
		}
		if (myConverter.IsExited)
		{
			return;
		}
		ModBase.OpenWebsite(website);
		ModBase.ClipboardSet(userCode);
		DeviceCodeData val = JsonCompat.ToObject<DeviceCodeData>((JsonNode)data) ?? throw new InvalidDataException("设备授权数据无效");
		TimeSpan delayTime = TimeSpan.FromSeconds(val.Interval ?? 5);
		int unknownFailureCount = 0;
		while (!myConverter.IsExited && !token.IsCancellationRequested)
		{
			try
			{
				if (myConverter.DeviceCodePoll == null)
				{
					throw new InvalidOperationException("设备授权没有配置轮询处理程序。");
				}
				AuthorizeResult resultJson = await myConverter.DeviceCodePoll(data, token).ConfigureAwait(continueOnCapturedContext: false);
				token.ThrowIfCancellationRequested();
				if (resultJson != null && resultJson.IsError)
				{
					switch (resultJson.Error)
					{
					case "authorization_pending":
						await Task.Delay(delayTime, token).ConfigureAwait(continueOnCapturedContext: false);
						break;
					case "slow_down":
						delayTime += TimeSpan.FromSeconds(5L);
						await Task.Delay(delayTime, token).ConfigureAwait(continueOnCapturedContext: false);
						break;
					case "expired_token":
						Finished((object)new IdentityModelAuthenticationException(resultJson.Error, resultJson.ErrorDescription ?? "设备授权已过期。", (Exception)null));
						return;
					case "access_denied":
					case "authorization_declined":
						Finished((object)new IdentityModelAuthenticationException(resultJson.Error, resultJson.ErrorDescription ?? "用户拒绝了设备授权。", (Exception)null));
						return;
					default:
						throw new IdentityModelAuthenticationException(resultJson.Error ?? "invalid_response", resultJson.ErrorDescription ?? "Unable to get body", (Exception)null);
					}
					continue;
				}
				if (resultJson == null)
				{
					throw new InvalidDataException("微软令牌返回为空");
				}
				if (myConverter.LoginResultHandler != null)
				{
					try
					{
						await myConverter.LoginResultHandler(resultJson, token).ConfigureAwait(continueOnCapturedContext: false);
						token.ThrowIfCancellationRequested();
					}
					catch (Exception result)
					{
						Finished(result);
						break;
					}
				}
				LogWrapper.Info("Profile", $"令牌过期时间：{resultJson.ExpiresIn} 秒");
				HintService.Hint(Lang.Text("Launch.Account.LoginDialog.Success"), HintType.Success);
				Finished(new string[2]
				{
					resultJson.AccessToken ?? string.Empty,
					resultJson.RefreshToken ?? string.Empty
				});
				break;
			}
			catch (OperationCanceledException) when (myConverter.IsExited)
			{
				break;
			}
			catch (Exception ex3)
			{
				if (unknownFailureCount <= 2)
				{
					unknownFailureCount++;
					ModBase.Log(ex3, $"正版验证轮询第 {unknownFailureCount} 次失败");
					ModBase.Log(ex3.Message);
					try
					{
						await Task.Delay(2000, token).ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (OperationCanceledException) when (myConverter.IsExited)
					{
						break;
					}
					continue;
				}
				Finished(new InvalidOperationException(Lang.Text("Launch.Account.LoginDialog.PollingFailed"), ex3));
				break;
			}
		}
	}

	public MyMsgLogin(ModMain.MyMsgBoxConverter converter)
	{
		try
		{
			InitializeComponent();
			Btn1.Name += ModBase.GetUuid();
			Btn2.Name += ModBase.GetUuid();
			Btn3.Name += ModBase.GetUuid();
			myConverter = converter;
			ShapeLine.StrokeThickness = ModBase.GetWPFSize(1.0);
			data = (JsonObject)converter.Content;
			Init();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Launch.Account.LoginDialog.Error.Init"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Account.LoginDialog.Error.Init"));
		}
		Loaded += Load;
	}

	private void Load(object sender, EventArgs e)
	{
		try
		{
			Opacity = 0.0;
			ModAnimation.AniStart(ModAnimation.AaColor((FrameworkElement)(object)ModMain.frmMain.PanMsgBackground, Border.BackgroundProperty, (myConverter.IsWarn ? new ModBase.MyColor(140.0, 80.0, 0.0, 0.0) : new ModBase.MyColor(90.0, 0.0, 0.0, 0.0)) - ((Border)(object)ModMain.frmMain.PanMsgBackground).Background, 200), "PanMsgBackground Background");
			ModAnimation.AniStart(new ModAnimation.AniData[3]
			{
				ModAnimation.AaOpacity(this, 1.0, 120, 60),
				ModAnimation.AaDouble((object? i) =>
				{
					TransformPos.Y += (double)i;
				}, 0.0 - TransformPos.Y, 300, 60, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
				ModAnimation.AaDouble((object? i) =>
				{
					TransformRotate.Angle += (double)i;
				}, 0.0 - TransformRotate.Angle, 300, 60, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
			}, "MyMsgBox " + uuid);
			ModBase.Log("[Control] 正版验证弹窗：" + LabTitle.Text + "\r\n" + LabCaption.Text);
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Launch.Account.LoginDialog.Error.Load"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Account.LoginDialog.Error.Load"));
		}
	}

	private void Close()
	{
		ModAnimation.AniStart(new ModAnimation.AniData[5]
		{
			ModAnimation.AaCode(() =>
			{
				if (!ModMain.WaitingMyMsgBox.Any())
				{
					ModAnimation.AniStart(ModAnimation.AaColor((FrameworkElement)(object)ModMain.frmMain.PanMsgBackground, Border.BackgroundProperty, new ModBase.MyColor(0.0, 0.0, 0.0, 0.0) - ((Border)(object)ModMain.frmMain.PanMsgBackground).Background, 200, 0, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
				}
			}, 30),
			ModAnimation.AaOpacity(this, 0.0 - Opacity, 80, 20),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformPos.Y += (double)i;
			}, 20.0 - TransformPos.Y, 150, 0, new ModAnimation.AniEaseOutFluent()),
			ModAnimation.AaDouble((object? i) =>
			{
				TransformRotate.Angle += (double)i;
			}, 6.0 - TransformRotate.Angle, 150, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
			ModAnimation.AaCode(() =>
			{
				((Grid)(object)Parent).Children.Remove(this);
			}, 0, after: true)
		}, "MyMsgBox " + uuid);
	}

	public void Btn1_Click(object sender, MouseButtonEventArgs e)
	{
	}

	public void Btn3_Click(object sender, MouseButtonEventArgs e)
	{
		Finished(new ThreadInterruptedException());
	}

	private void Drag(object sender, MouseButtonEventArgs e)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Point position = e.GetPosition(ShapeLine);
		if (position.Y <= 2.0)
		{
			ModMain.frmMain.DragMove();
		}
	}

}
