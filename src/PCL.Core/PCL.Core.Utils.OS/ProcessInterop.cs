using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32;
using PCL.Core.Logging;

namespace PCL.Core.Utils.OS;

public class ProcessInterop
{
	public static bool IsAdmin()
	{
		return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
	}

	public static string? GetCommandLine(int processId)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected Obj, but got Unknown
		ManagementObjectSearcher val = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
		try
		{
			return val.Get().GetEnumerator().Current["CommandLine"].ToString();
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	public static Process? Start(string path, string? arguments = null, bool runAsAdmin = false)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(path);
		if (arguments != null)
		{
			processStartInfo.Arguments = arguments;
		}
		if (runAsAdmin)
		{
			processStartInfo.UseShellExecute = true;
			processStartInfo.Verb = "runas";
		}
		if (Directory.Exists(path))
		{
			processStartInfo.UseShellExecute = true;
		}
		return Process.Start(processStartInfo);
	}

	public static string? GetExecutablePath(Process process)
	{
		try
		{
			string text = process.MainModule?.FileName;
			return (text == null) ? null : Path.GetFullPath(text);
		}
		catch
		{
			return null;
		}
	}

	public static Process? StartAsAdmin(string path, string? arguments = null)
	{
		return Start(path, arguments, runAsAdmin: true);
	}

	public static int Kill(Process process, int timeout = 3000, bool force = false)
	{
		if (force)
		{
			Process.Start(new ProcessStartInfo("TASKKILL.EXE", $"/PID {process.Id} /F")
			{
				UseShellExecute = false
			});
		}
		else
		{
			process.Kill();
		}
		switch (timeout)
		{
		case -1:
			process.WaitForExit();
			break;
		default:
			process.WaitForExit(timeout);
			break;
		case 0:
			break;
		}
		if (!process.HasExited)
		{
			return int.MinValue;
		}
		return process.ExitCode;
	}

	public static void SetGpuPreference(string executable, bool wantHighPerformance = true)
	{
		if (string.IsNullOrWhiteSpace(executable))
		{
			throw new ArgumentException("可执行文件路径不能为空或仅包含空白字符", "executable");
		}
		try
		{
			if (!File.Exists(Path.GetFullPath(executable)))
			{
				LogWrapper.Warn("System", "指定的可执行文件不存在: " + executable);
			}
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			throw new ArgumentException("无效的可执行文件路径: " + executable, "executable", ex);
		}
		try
		{
			bool flag = _GetCurrentGpuPreference(executable, "Software\\Microsoft\\DirectX\\UserGpuPreferences", "GpuPreference=2;");
			LogWrapper.Info("System", $"当前程序 ({executable}) 的显卡设置为高性能: {flag}");
			if (flag == wantHighPerformance)
			{
				LogWrapper.Info("System", "程序 (" + executable + ") 的显卡设置已经是期望的设置，无需修改");
			}
			else
			{
				_SetGpuPreferenceValue(executable, wantHighPerformance, "Software\\Microsoft\\DirectX\\UserGpuPreferences", "GpuPreference=2;", "GpuPreference=0;");
			}
		}
		catch (UnauthorizedAccessException ex2)
		{
			string text = "没有足够的权限访问注册表。请以管理员身份运行程序或检查用户权限设置。";
			LogWrapper.Error(ex2, "System", text);
			throw new UnauthorizedAccessException(text, ex2);
		}
		catch (SecurityException ex3)
		{
			string text2 = "安全策略不允许访问注册表。请联系系统管理员检查安全设置。";
			LogWrapper.Error(ex3, "System", text2);
			throw new SecurityException(text2, ex3);
		}
		catch (Exception ex4)
		{
			string text3 = "设置 GPU 偏好时发生未预期的错误: " + ex4.Message;
			LogWrapper.Error(ex4, "System", text3);
			throw new InvalidOperationException(text3, ex4);
		}
	}

	private static bool _GetCurrentGpuPreference(string executable, string regKey, string highPerfValue)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(regKey, writable: false);
			if (registryKey == null)
			{
				LogWrapper.Info("System", "GPU 偏好注册表键不存在，将在需要时创建");
				return false;
			}
			return string.Equals(registryKey.GetValue(executable)?.ToString(), highPerfValue, StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "System", "读取当前 GPU 偏好设置时出现错误: " + ex.Message);
			return false;
		}
	}

	private static bool _SetGpuPreferenceValue(string executable, bool wantHighPerformance, string regKey, string highPerfValue, string defaultValue)
	{
		RegistryKey registryKey = null;
		try
		{
			registryKey = Registry.CurrentUser.OpenSubKey(regKey, writable: true);
			if (registryKey == null)
			{
				LogWrapper.Info("System", "创建 GPU 偏好注册表键");
				registryKey = Registry.CurrentUser.CreateSubKey(regKey);
				if (registryKey == null)
				{
					throw new InvalidOperationException("无法创建注册表键: " + regKey);
				}
			}
			string value = (wantHighPerformance ? highPerfValue : defaultValue);
			registryKey.SetValue(executable, value, RegistryValueKind.String);
			LogWrapper.Info("System", "成功设置程序 (" + executable + ") 的GPU偏好: " + (wantHighPerformance ? "高性能" : "默认"));
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			throw;
		}
		catch (SecurityException)
		{
			throw;
		}
		catch (Exception ex3)
		{
			string text = "写入注册表时发生错误: " + ex3.Message;
			LogWrapper.Error(ex3, "System", text);
			throw new InvalidOperationException(text, ex3);
		}
		finally
		{
			registryKey?.Dispose();
		}
	}
}
