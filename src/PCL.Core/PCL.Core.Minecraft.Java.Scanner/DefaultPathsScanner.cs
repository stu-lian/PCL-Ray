using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PCL.Core.App;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Java.Scanner;

public class DefaultPathsScanner : IJavaScanner
{
	private const int MaxSearchDepth = 6;

	public void Scan(ICollection<string> results)
	{
		try
		{
			HashSet<string> hashSet = _GetSearchRoots();
			LogWrapper.Info("[Java] 对下列目录进行广度关键词搜索:" + Environment.NewLine + string.Join(Environment.NewLine, hashSet));
			foreach (string item in hashSet)
			{
				_BfsSearch(item, results);
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "Java", "默认路径扫描失败");
		}
	}

	private static HashSet<string> _GetSearchRoots()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "runtime"),
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			Path.Combine(Basics.ExecutableDirectory, "PCL")
		};
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			string[] array = new string[2] { "Program Files", "Program Files (x86)" };
			foreach (string item in from d in DriveInfo.GetDrives()
				where d.DriveType.Equals(DriveType.Fixed) && d.IsReady
				select d.Name)
			{
				string[] array2 = array;
				foreach (string path in array2)
				{
					hashSet.Add(Path.Combine(item, path));
				}
				try
				{
					foreach (string item2 in from dir in Directory.EnumerateDirectories(item)
						where JavaConsts.MostPossibleKeywords.Any((string k) => Path.GetFileName(dir).Contains(k, StringComparison.OrdinalIgnoreCase))
						select dir)
					{
						hashSet.Add(item2);
					}
				}
				catch (UnauthorizedAccessException)
				{
				}
				catch (IOException)
				{
				}
			}
		}
		else
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
			{
				hashSet.Add(folderPath);
			}
			if (!string.IsNullOrEmpty(folderPath2) && Directory.Exists(folderPath2))
			{
				hashSet.Add(folderPath2);
			}
		}
		return hashSet;
	}

	private static void _BfsSearch(string rootPath, ICollection<string> results)
	{
		if (!Directory.Exists(rootPath))
		{
			return;
		}
		Queue<(string, int)> queue = new Queue<(string, int)>();
		queue.Enqueue((rootPath, 0));
		while (queue.Count > 0)
		{
			var (text, num) = queue.Dequeue();
			if (num > 6 || !Directory.Exists(text))
			{
				continue;
			}
			try
			{
				foreach (string item in Directory.EnumerateDirectories(text))
				{
					string text2 = Path.Combine(item, "java.exe");
					if (File.Exists(text2))
					{
						results.Add(text2);
					}
					else if (_ShouldExploreDeeper(item))
					{
						queue.Enqueue((item, num + 1));
					}
				}
			}
			catch (Exception ex) when ((ex is UnauthorizedAccessException || ex is IOException) ? true : false)
			{
				LogWrapper.Debug("跳过目录 " + text + ": " + ex.Message);
			}
			catch (Exception ex2)
			{
				LogWrapper.Error(ex2, "Java", "搜索目录 " + text + " 时出错");
			}
		}
	}

	private static bool _ShouldExploreDeeper(string path)
	{
		ReadOnlySpan<char> readOnlySpan = Path.GetFileName(path);
		string[] excludeFolderNames = JavaConsts.ExcludeFolderNames;
		foreach (string text in excludeFolderNames)
		{
			if (readOnlySpan.Contains(text, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		excludeFolderNames = JavaConsts.AllKeywords;
		foreach (string text2 in excludeFolderNames)
		{
			if (readOnlySpan.Contains(text2, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return _IsVersionLikeDirectory(readOnlySpan);
	}

	private static bool _IsVersionLikeDirectory(ReadOnlySpan<char> name)
	{
		if (name.IsEmpty || name.Length > 20)
		{
			return false;
		}
		bool result = false;
		ReadOnlySpan<char> readOnlySpan = name;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (char.IsDigit(c))
			{
				result = true;
			}
			else if (c != '.' && c != '_' && c != '-')
			{
				return false;
			}
		}
		return result;
	}
}
