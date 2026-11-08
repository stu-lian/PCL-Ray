using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.Utils;

namespace PCL.Core.App.Tools;

[LifecycleScope("dependency-check", "依赖检查", true)]
[LifecycleService(LifecycleState.Running)]
public sealed class DependencyCheckService : ILifecycleService
{
	public string Identifier => "dependency-check";

	public string Name => "依赖检查";

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

	[LifecycleStart]
	private static async Task _StartAsync()
	{
		Context.Info(Lang.Text("Tools.Test.Dependency.Checking"));
		if (RuntimeInformation.OSArchitecture.Equals(Architecture.Arm64))
		{
			await _CheckAndAskAsync("Microsoft.D3DMappingLayers", "OpenGL 兼容包", "9nqpsl29bfff").ConfigureAwait(continueOnCapturedContext: false);
		}
		await _CheckAndAskAsync("Microsoft.WebpImageExtension", "WebP 组件包", "9pg2dk419drg").ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task _CheckAndAskAsync(string packageId, string packageName, string storeId)
	{
		if (!(await _CheckPackageAsync(packageId)))
		{
			Context.Info("检测到依赖缺失 (package-id = " + packageId + ")");
			if (MsgBoxWrapper.Show(Lang.Text("Tools.Test.Dependency.MissingMessage", packageName), null, MsgBoxTheme.Info, true, Lang.Text("Common.Action.Confirm"), Lang.Text("Tools.Test.Dependency.Later")) == 1)
			{
				_LaunchMsStore(storeId);
			}
		}
	}

	private static void _LaunchMsStore(string id)
	{
		Context.Info("正在打开微软应用商店 (id = " + id + ")");
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "ms-windows-store://pdp?launch=true&productid=" + id + "&mode=full",
			UseShellExecute = true
		};
		using Process process = new Process
		{
			StartInfo = startInfo
		};
		process.Start();
	}

	private static async Task<bool> _CheckPackageAsync(string id)
	{
		string text = "Get-AppxPackage -Name *" + id + "* | ConvertTo-Json";
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = "-NoProfile -Command \"" + text + "\"",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		using (Process ps = new Process
		{
			StartInfo = startInfo
		})
		{
			ps.Start();
			JsonNode jsonNode;
			try
			{
				jsonNode = await JsonNode.ParseAsync(ps.StandardOutput.BaseStream, JsonCompat.NodeOptions, JsonCompat.DocumentOptions);
			}
			catch
			{
				return false;
			}
			if (jsonNode == null)
			{
				return false;
			}
			if (jsonNode.GetValueKind().Equals(JsonValueKind.Array))
			{
				bool result = false;
				foreach (JsonNode item in jsonNode.AsArray())
				{
					if (item != null && CheckPackSuit(item, id))
					{
						result = true;
						break;
					}
				}
				return result;
			}
			return CheckPackSuit(jsonNode, id);
		}
		static bool CheckPackSuit(JsonNode jnode, string value)
		{
			return jnode["Name"].ToObject<string>()?.Contains(value) ?? false;
		}
	}

	public DependencyCheckService()
	{
		Context = Lifecycle.GetContext(this);
	}

	public async Task StopAsync()
	{
	}

	public async Task StartAsync()
	{
		await _StartAsync();
		Context.DeclareStopped();
	}
}
