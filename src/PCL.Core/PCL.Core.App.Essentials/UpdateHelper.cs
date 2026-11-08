using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PCL.Core.App.Essentials;

public static class UpdateHelper
{
	public static Exception? Replace(string source, string target)
	{
		string text = target + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		Exception result = null;
		try
		{
			source = Path.GetFullPath(source);
			target = Path.GetFullPath(target);
			File.Copy(target, text);
			if (!File.Exists(text))
			{
				throw new FileNotFoundException("备份目标文件失败", text);
			}
			FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(Basics.GetParentPathOrEmpty(target), Path.GetFileName(target));
			ManualResetEventSlim deletedEvent = new ManualResetEventSlim(initialState: false);
			fileSystemWatcher.Deleted += (object _, FileSystemEventArgs _) =>
			{
				deletedEvent.Set();
			};
			fileSystemWatcher.EnableRaisingEvents = true;
			File.Delete(target);
			if (!deletedEvent.Wait(TimeSpan.FromSeconds(3L)))
			{
				throw new TimeoutException("删除目标文件失败");
			}
			fileSystemWatcher.EnableRaisingEvents = false;
			fileSystemWatcher.Dispose();
			File.Copy(source, target);
			if (!File.Exists(target))
			{
				throw new FileNotFoundException("复制到目标文件失败", target);
			}
		}
		catch (Exception ex)
		{
			if (File.Exists(text) && !File.Exists(target))
			{
				File.Move(text, target);
			}
			result = ex;
		}
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		return result;
	}
}
