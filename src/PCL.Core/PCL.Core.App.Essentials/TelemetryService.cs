using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using PCL.Core.App.IoC;
using PCL.Core.IO.Net;
using PCL.Core.IO.Net.Dns;
using PCL.Core.Logging;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;
using STUN.Client;
using STUN.Proxy;
using Sentry;
using Sentry.Extensibility;

namespace PCL.Core.App.Essentials;

[LifecycleScope("telemetry", "遥测", true)]
[LifecycleService(LifecycleState.Running)]
public sealed class TelemetryService : ILifecycleService
{
	private class TelemetryDeviceEnvironment
	{
		public required string Tag { get; set; }

		public required string Id { get; set; }

		[JsonPropertyName("OS")]
		public required int Os { get; set; }

		public required bool Is64Bit { get; set; }

		[JsonPropertyName("IsARM64")]
		public required bool IsArm64 { get; set; }

		public required string Launcher { get; set; }

		public required string LauncherBranch { get; set; }

		[JsonPropertyName("UsedOfficialPCL")]
		public required bool UsedOfficialPcl { get; set; }

		[JsonPropertyName("UsedHMCL")]
		public required bool UsedHmcl { get; set; }

		[JsonPropertyName("UsedBakaXL")]
		public required bool UsedBakaXl { get; set; }

		public required ulong Memory { get; set; }

		public required string? NatMapBehaviour { get; set; }

		public required string? NatFilterBehaviour { get; set; }

		[JsonPropertyName("IPv6Status")]
		public required string Ipv6Status { get; set; }
	}

	private sealed class SocketExceptionFilter : IExceptionFilter
	{
		public bool Filter(Exception ex)
		{
			if (ex is SocketException { SocketErrorCode: var socketErrorCode })
			{
				switch (socketErrorCode)
				{
				case SocketError.NetworkUnreachable:
				case SocketError.ConnectionReset:
				case SocketError.TimedOut:
				case SocketError.ConnectionRefused:
				case SocketError.HostNotFound:
					return true;
				default:
					return false;
				}
			}
			return false;
		}
	}

	private const string STUN_SERVER_ADDR = "stun.miwifi.com";

	public string Identifier => "telemetry";

	public string Name => "遥测";

	public bool SupportAsync => true;

	private static LifecycleContext Context
	{
		get
		{
			return field ?? throw new InvalidOperationException("Not initialized");
		}
		set;
	}

	private static ILifecycleService Service => Context.ServiceInstance;

	private static void _InitSentry()
	{
		Context.Info("开始初始化 Sentry SDK");
		string dsn = EnvironmentInterop.GetSecret("SENTRY_DSN");
		if (dsn == null)
		{
			Context.Warn("未找到 Sentry DSN");
			return;
		}
		string release = Basics.VersionName ?? "";
		string environment = "Production";
		SentrySdk.Init((Action<SentryOptions>)((SentryOptions options) =>
		{
			options.Dsn = dsn;
			options.Debug = false;
			options.SendDefaultPii = false;
			options.IsGlobalModeEnabled = true;
			options.AutoSessionTracking = true;
			options.Release = release;
			options.Environment = environment;
			options.AddExceptionFilterForType<TimeoutException>();
			options.AddExceptionFilterForType<HttpRequestException>();
			options.AddExceptionFilterForType<WebException>();
			options.AddExceptionFilterForType<TaskCanceledException>();
			options.AddExceptionFilterForType<DirectoryNotFoundException>();
			options.AddExceptionFilterForType<UnauthorizedAccessException>();
			options.AddExceptionFilterForType<FileNotFoundException>();
			options.AddExceptionFilter((IExceptionFilter)(object)new SocketExceptionFilter());
			options.SetBeforeSend((Func<SentryEvent, SentryEvent>)((SentryEvent @event) =>
			{
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Invalid comparison between Unknown and I4
				SentryLevel? level = @event.Level;
				return (!level.HasValue || (int)level.GetValueOrDefault() != 0) ? @event : null;
			}));
		}));
		SentrySdk.ConfigureScope((Action<Scope>)((Scope scope) =>
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Expected Obj, but got Unknown
			scope.User = new SentryUser
			{
				Id = Identify.LauncherId
			};
		}));
		Context.Info("Sentry SDK 初始化完成");
	}

	public static void ReportException(Exception ex, string plain, LogLevel level)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected Obj, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected Obj, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		SentryEvent val = new SentryEvent(ex);
		SentryEvent val2 = val;
		SentryLevel value;
		switch (level.RealLevel())
		{
		case LogLevel.Fatal:
			value = (SentryLevel)4;
			break;
		case LogLevel.Error:
			value = (SentryLevel)3;
			break;
		case LogLevel.Warning:
			value = (SentryLevel)2;
			break;
		case LogLevel.Info:
			value = (SentryLevel)1;
			break;
		case LogLevel.Trace:
		case LogLevel.Debug:
			value = (SentryLevel)0;
			break;
		default:
			throw new ArgumentOutOfRangeException("level");
		}
		val2.Level = value;
		SentryEvent val3 = val;
		if (!string.IsNullOrWhiteSpace(plain))
		{
			val3.Message = new SentryMessage
			{
				Formatted = plain
			};
		}
		SentrySdk.CaptureEvent(val3, (Scope)null, (SentryHint)null);
	}

	private static void _ReportDeviceEnvironment(TelemetryDeviceEnvironment content)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		Context.Info("正在上报设备环境调查数据");
		SentrySdk.ConfigureScope((Action<Scope>)((Scope scope) =>
		{
			scope.Contexts["Telemetry"] = content;
		}));
		try
		{
			SentrySdk.CaptureMessage("设备环境调查", (SentryLevel)1);
			Context.Info("已发送设备环境调查数据");
		}
		catch (Exception ex)
		{
			Context.Error("设备环境调查数据发送失败，请检查网络连接以及使用的版本", ex);
		}
	}

	[LifecycleStart]
	private static async Task _StartAsync()
	{
		if (!Config.System.Telemetry)
		{
			return;
		}
		_InitSentry();
		string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		StunClient5389UDP natTest = null;
		IPAddress[] miWifiIps = await DnsQuery.Instance.QueryForIpAsync("stun.miwifi.com").ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (miWifiIps == null)
			{
				miWifiIps = await Dns.GetHostAddressesAsync("stun.miwifi.com").ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception)
		{
		}
		if (miWifiIps != null && miWifiIps.Length != 0)
		{
			natTest = new StunClient5389UDP(new IPEndPoint(miWifiIps.First(), 3478), new IPEndPoint(IPAddress.Any, 0), (IUdpProxy)null, true);
			await natTest.QueryAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
		}
		TelemetryDeviceEnvironment telemetryDeviceEnvironment = new TelemetryDeviceEnvironment
		{
			Tag = "Telemetry",
			Id = Identify.LauncherId,
			Os = Environment.OSVersion.Version.Build,
			Is64Bit = Environment.Is64BitOperatingSystem,
			IsArm64 = RuntimeInformation.OSArchitecture.Equals(Architecture.Arm64),
			Launcher = Basics.VersionName,
			LauncherBranch = Config.Update.UpdateChannel switch
			{
				UpdateChannel.Release => "Release", 
				UpdateChannel.Beta => "Beta", 
				UpdateChannel.Dev => "Dev", 
				_ => "Unknown", 
			},
			UsedOfficialPcl = bool.TryParse(Registry.GetValue("HKEY_CURRENT_USER\\Software\\PCL", "SystemEula", "false") as string, out var result) & result,
			UsedHmcl = Directory.Exists(Path.Combine(appDataFolder, ".hmcl")),
			UsedBakaXl = Directory.Exists(Path.Combine(appDataFolder, "BakaXL")),
			Memory = KernelInterop.GetPhysicalMemoryBytes().Total,
			NatMapBehaviour = ((natTest != null) ? ((object)natTest.State.MappingBehavior/*cast due to constrained. prefix*/).ToString() : null),
			NatFilterBehaviour = ((natTest != null) ? ((object)natTest.State.FilteringBehavior/*cast due to constrained. prefix*/).ToString() : null),
			Ipv6Status = NetworkInterfaceUtils.GetIPv6Status().ToString()
		};
		_ReportDeviceEnvironment(telemetryDeviceEnvironment);
	}

	[LifecycleStop]
	private static void _StopAsync()
	{
		SentrySdk.Close();
	}

	public TelemetryService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
		_StopAsync();
	}

	public async Task StartAsync()
	{
		await _StartAsync();
	}
}
