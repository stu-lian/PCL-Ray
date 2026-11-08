using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using fNbt;

namespace PCL.Core.Minecraft;

public static class NbtFileHandler
{
	public static async Task<T?> ReadTagInNbtFileAsync<T>(string filePath, string tagName, CancellationToken cancelToken = default(CancellationToken)) where T : NbtTag
	{
		try
		{
			string fullPath = Path.GetFullPath(filePath);
			if (!File.Exists(fullPath))
			{
				LogWrapper.Warn("NBT 文件不存在：" + fullPath);
				return default;
			}
			NbtFile nbtFile = new NbtFile();
			await Task.Run(async () =>
			{
				await using FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
				nbtFile.LoadFromStream((Stream)fileStream, (NbtCompression)0);
			}, cancelToken);
			T val = nbtFile.RootTag.Get<T>(tagName);
			if (val == null)
			{
				LogWrapper.Warn("未找到指定的 NBT 标签：" + tagName);
			}
			return val;
		}
		catch (OperationCanceledException)
		{
			LogWrapper.Info("读取 NBT 文件操作被取消：" + filePath);
			return default;
		}
		catch (Exception ex2)
		{
			LogWrapper.Warn(ex2, "读取 NBT 文件出错：" + filePath);
			return default;
		}
	}

	public static async Task<bool> WriteTagInNbtFileAsync(NbtTag nbtTag, string filePath, NbtCompression compression = (NbtCompression)1, CancellationToken cancelToken = default(CancellationToken))
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string fullPath = Path.GetFullPath(filePath);
			string directoryName = Path.GetDirectoryName(fullPath);
			if (string.IsNullOrEmpty(directoryName))
			{
				LogWrapper.Warn("无法获取目标目录：" + fullPath);
				return false;
			}
			Directory.CreateDirectory(directoryName);
			NbtCompound val = new NbtCompound
			{
				Name = ""
			};
			val.Add(nbtTag);
			NbtFile nbtFile = new NbtFile(val);
			await Task.Run(async () =>
			{
				await using FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
				nbtFile.SaveToStream((Stream)fileStream, compression);
			}, cancelToken);
			LogWrapper.Info("NBT 文件成功保存于：" + fullPath);
			return true;
		}
		catch (OperationCanceledException)
		{
			LogWrapper.Info("写入 NBT 文件操作被取消：" + filePath);
			return false;
		}
		catch (NbtFormatException ex2)
		{
			LogWrapper.Warn((Exception?)ex2, "NBT 格式错误：" + filePath);
			return false;
		}
		catch (IOException ex3)
		{
			LogWrapper.Warn(ex3, "文件操作错误：" + filePath);
			return false;
		}
		catch (Exception ex4)
		{
			LogWrapper.Warn(ex4, "写入 NBT 文件出错：" + filePath);
			return false;
		}
	}
}
