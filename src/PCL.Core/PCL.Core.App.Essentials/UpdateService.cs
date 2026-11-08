using System;
using System.Diagnostics;
using System.IO;
using PCL.Core.App.IoC;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App.Essentials;

[LifecycleService(LifecycleState.BeforeLoading)]
public sealed class UpdateService : GeneralService
{
	private static LifecycleContext? _context;

	private static LifecycleContext Context => _context;

	private UpdateService()
		: base("update", "更新", asyncStart: false)
	{
		_context = ServiceContext;
	}

	public override void Start()
	{
		string[] commandLineArguments = Basics.CommandLineArguments;
		if (commandLineArguments == null || commandLineArguments.Length != 5 || !(commandLineArguments[0] == "update"))
		{
			if (commandLineArguments == null || commandLineArguments.Length != 2)
			{
				goto IL_0097;
			}
			string text = commandLineArguments[0];
			if (!(text == "update_finished"))
			{
				if (!(text == "update_failed"))
				{
					goto IL_0097;
				}
				string text2 = commandLineArguments[1];
				Context.Error("更新失败: " + text2 + "\n你可以手动将 exe 文件替换为 PCL 目录中的新版本或再次尝试更新，若再次尝试仍然失败，请尽快反馈这个问题");
			}
			else
			{
				File.Delete(commandLineArguments[1]);
				Context.Debug("更新来源文件已删除");
			}
			goto IL_00b0;
		}
		try
		{
			Context.Info("开始更新");
			Lifecycle.PendingLogDirectory = Path.Combine(Basics.ExecutableDirectory, "Log");
			Lifecycle.PendingLogFileName = "LastPending_Update.log";
			int num = commandLineArguments[1].Convert<int>();
			Context.Debug($"旧版本进程 ID: {num}");
			try
			{
				Process processById = Process.GetProcessById(num);
				Context.Debug("正在等待旧版本进程退出");
				processById.WaitForExit();
				Context.Trace("旧版本进程已退出");
			}
			catch
			{
			}
			Context.Debug("正在替换文件");
			string text3 = commandLineArguments[2];
			Context.Trace("目标: " + text3);
			string text4 = commandLineArguments[3];
			Context.Trace("来源: " + text4);
			Exception ex = UpdateHelper.Replace(text4, text3);
			if (ex == null)
			{
				Context.Trace("替换完成");
			}
			else
			{
				Context.Error("替换文件出错", ex);
			}
			if (commandLineArguments[4].Convert<bool>())
			{
				string text5 = ((ex == null) ? ("finished \"" + text4 + "\"") : ("failed \"" + ex.Message + "\""));
				text5 = "update_" + text5;
				Context.Debug("重启中，使用参数: " + text5);
				Process.Start(text3, text5);
			}
		}
		catch (Exception ex2)
		{
			Context.Error("更新过程出错", ex2);
		}
		Context.RequestExit();
		return;
		IL_00b0:
		Context.DeclareStopped();
		return;
		IL_0097:
		Context.Debug("无更新任务");
		goto IL_00b0;
	}
}
