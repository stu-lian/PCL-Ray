using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.IO;

public static class Directories
{
	public static async Task<bool> CheckPermissionAsync(string? path, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return false;
			}
			if (IsSystemProtectedFolder(path))
			{
				return false;
			}
			if (!Directory.Exists(path))
			{
				return false;
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			AuthorizationRuleCollection accessRules = (await Task.Run(() => directoryInfo.GetAccessControl(), cancellationToken)).GetAccessRules(includeExplicit: true, includeInherited: true, typeof(NTAccount));
			WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			bool flag = false;
			bool flag2 = false;
			foreach (FileSystemAccessRule item in accessRules)
			{
				if (item.FileSystemRights.HasFlag(FileSystemRights.Write) && windowsPrincipal.IsInRole(item.IdentityReference.Value))
				{
					if (item.AccessControlType == AccessControlType.Deny)
					{
						flag = true;
						break;
					}
					if (item.AccessControlType == AccessControlType.Allow)
					{
						flag2 = true;
					}
				}
			}
			if (flag || !flag2)
			{
				return false;
			}
			await Task.Run(() => Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).Any(), cancellationToken);
			return true;
		}
		catch (OperationCanceledException)
		{
			LogWrapper.Warn("权限检查被取消");
			return false;
		}
		catch (Exception ex2)
		{
			LogWrapper.Warn(ex2, "没有对文件夹 " + path + " 的权限，请尝试以管理员权限运行。");
			return false;
		}
	}

	public static async Task CheckPermissionWithExceptionAsync(string? path, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentNullException("path", "文件夹路径不能为空！");
		}
		if (IsSystemProtectedFolder(path))
		{
			throw new UnauthorizedAccessException("无法访问受保护的系统文件夹：" + path);
		}
		if (!Directory.Exists(path))
		{
			throw new DirectoryNotFoundException("文件夹不存在：" + path);
		}
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (!(await Task.Run(() => directoryInfo.GetAccessControl(), cancellationToken)).GetAccessRules(includeExplicit: true, includeInherited: true, typeof(NTAccount)).Cast<FileSystemAccessRule>().Any((FileSystemAccessRule rule) => rule.FileSystemRights.HasFlag(FileSystemRights.Write) && rule.AccessControlType == AccessControlType.Allow))
			{
				throw new UnauthorizedAccessException("没有对文件夹 " + path + " 的写权限");
			}
			await Task.Run(() => Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly).Any(), cancellationToken);
		}
		catch (UnauthorizedAccessException)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex3)
		{
			throw new UnauthorizedAccessException("无法访问文件夹 " + path + "：" + ex3.Message, ex3);
		}
	}

	private static bool IsSystemProtectedFolder(string path)
	{
		if (!path.EndsWith(":\\System Volume Information", StringComparison.OrdinalIgnoreCase))
		{
			return path.EndsWith(":\\$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static async Task<int> DeleteDirectoryAsync(string? path, bool ignoreIssue = false, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
		{
			return 0;
		}
		int deletedCount = 0;
		try
		{
			foreach (string filePath in Directory.EnumerateFiles(path))
			{
				cancellationToken.ThrowIfCancellationRequested();
				for (int attempt = 0; attempt < 2; attempt++)
				{
					try
					{
						await FileDeleteAsync(filePath, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						deletedCount++;
					}
					catch (Exception ex) when (attempt == 0)
					{
						LogWrapper.Error(ex, "删除文件失败，将在 0.3s 后重试（" + filePath + "）");
						await Task.Delay(300, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						continue;
					}
					catch (Exception ex2)
					{
						if (ignoreIssue)
						{
							LogWrapper.Error(ex2, "删除单个文件可忽略地失败");
							continue;
						}
						throw;
					}
					break;
				}
			}
			foreach (string item in Directory.EnumerateDirectories(path))
			{
				cancellationToken.ThrowIfCancellationRequested();
				int attempt = deletedCount;
				deletedCount = attempt + await DeleteDirectoryAsync(item, ignoreIssue, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			for (int attempt = 0; attempt < 2; attempt++)
			{
				try
				{
					Directory.Delete(path, recursive: true);
				}
				catch (Exception ex3) when (attempt == 0)
				{
					LogWrapper.Error(ex3, "删除文件夹失败，将在 0.3s 后重试（" + path + "）");
					await Task.Delay(300, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				catch (Exception ex4)
				{
					if (ignoreIssue)
					{
						LogWrapper.Error(ex4, "删除单个文件夹可忽略地失败");
						continue;
					}
					throw;
				}
				break;
			}
		}
		catch (DirectoryNotFoundException ex5)
		{
			LogWrapper.Error(ex5, "疑似为孤立符号链接，尝试直接删除（" + path + "）", "Developer");
			try
			{
				Directory.Delete(path);
			}
			catch (Exception ex6)
			{
				if (!ignoreIssue)
				{
					throw;
				}
				LogWrapper.Error(ex6, "删除符号链接文件夹失败（" + path + "）");
			}
		}
		return deletedCount;
	}

	public static async Task CopyDirectoryAsync(string? fromPath, string? toPath, Action<double>? progressIncrementHandler = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrEmpty(fromPath))
		{
			throw new ArgumentNullException("fromPath", "源文件夹路径为空");
		}
		if (string.IsNullOrEmpty(toPath))
		{
			throw new ArgumentNullException("toPath", "目标文件夹路径为空");
		}
		fromPath = Path.GetFullPath(fromPath).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}) + Path.DirectorySeparatorChar;
		toPath = Path.GetFullPath(toPath).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}) + Path.DirectorySeparatorChar;
		List<FileInfo> list = (await EnumerateFilesAsync(fromPath, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).ToList();
		int totalFiles = list.Count;
		long copiedFiles = 0L;
		foreach (FileInfo file in list)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string path = file.FullName.Substring(fromPath.Length);
			string destFilePath = Path.Combine(toPath, path);
			Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
			for (int attempt = 0; attempt < 2; attempt++)
			{
				try
				{
					await FileCopyAsync(file.FullName, destFilePath, overwrite: true, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					copiedFiles++;
					progressIncrementHandler?.Invoke((double)copiedFiles / (double)totalFiles);
				}
				catch (Exception ex) when (attempt == 0)
				{
					LogWrapper.Error(ex, $"复制文件失败，将在 0.3s 后重试（{file.FullName} 到 {destFilePath}）");
					await Task.Delay(300, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				catch (Exception ex2)
				{
					LogWrapper.Error(ex2, $"复制文件失败（{file.FullName} 到 {destFilePath}）");
					throw;
				}
				break;
			}
		}
	}

	public static async Task<IEnumerable<FileInfo>> EnumerateFilesAsync(string? directory, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException("目录不存在：" + directory);
		}
		try
		{
			return await Task.Run(() => new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories).ToList(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			LogWrapper.Warn("文件夹遍历被取消");
			return Array.Empty<FileInfo>();
		}
		catch (Exception ex2)
		{
			LogWrapper.Error(ex2, "遍历文件夹失败（" + directory + "）");
			return Array.Empty<FileInfo>();
		}
	}

	private static async Task<FileStream> FileStreamOpenAsync(string path, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellationToken)
	{
		FileStream fs = new FileStream(path, mode, access, share, 4096, useAsync: true);
		await Task.Yield();
		cancellationToken.ThrowIfCancellationRequested();
		return fs;
	}

	private static async Task FileDeleteAsync(string path, CancellationToken cancellationToken)
	{
		await Task.Run(() =>
		{
			File.Delete(path);
		}, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task FileCopyAsync(string sourceFileName, string destFileName, bool overwrite, CancellationToken cancellationToken)
	{
		await using FileStream sourceStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
		await using FileStream destStream = new FileStream(destFileName, (!overwrite) ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
		await sourceStream.CopyToAsync(destStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}
}
