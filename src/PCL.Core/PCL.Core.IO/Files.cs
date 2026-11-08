using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Codecs;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;

namespace PCL.Core.IO;

public static class Files
{
	public static readonly JsonSerializerOptions PrettierJsonOptions = new JsonSerializerOptions(JsonCompat.SerializerOptions)
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static void CreateShortcut(string shortcut, string target, string? arguments = null, string? workingDirectory = null, string? description = null, string? icon = null)
	{
		if (string.IsNullOrWhiteSpace(shortcut))
		{
			throw new ArgumentException("shortcutPath 不能为空", "shortcut");
		}
		if (string.IsNullOrWhiteSpace(target))
		{
			throw new ArgumentException("targetFilePath 不能为空", "target");
		}
		string directoryName = Path.GetDirectoryName(shortcut);
		if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		dynamic val = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", throwOnError: true));
		dynamic val2 = val.CreateShortcut(shortcut);
		val2.TargetPath = target;
		if (!string.IsNullOrEmpty(arguments))
		{
			val2.Arguments = arguments;
		}
		if (string.IsNullOrEmpty(workingDirectory))
		{
			val2.WorkingDirectory = Path.GetDirectoryName(target) ?? Path.GetPathRoot(target);
		}
		else
		{
			val2.WorkingDirectory = workingDirectory;
		}
		if (!string.IsNullOrEmpty(description))
		{
			val2.Description = description;
		}
		if (!string.IsNullOrEmpty(icon))
		{
			val2.IconLocation = icon;
		}
		val2.Save();
	}

	public static bool ArePathsEqual(string path1, string path2)
	{
		string a = Path.GetFullPath(path1).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		});
		string b = Path.GetFullPath(path2).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		});
		return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
	}

	public static async Task<bool> ExportAsZipArchiveAsync(IEnumerable<string> sourceFiles, string dialogTitle, string defaultFileName, string fileFilter, string defaultDirectory, string tempDirPrefix, CancellationToken cancellationToken = default(CancellationToken))
	{
		string tempDirName = tempDirPrefix + ".tmp";
		string selectedPath = SystemDialogs.SelectSaveFile(dialogTitle, defaultFileName, fileFilter, defaultDirectory);
		if (string.IsNullOrEmpty(selectedPath))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(tempDirName);
			if (File.Exists(selectedPath))
			{
				File.Delete(selectedPath);
				LogWrapper.Info("Files", "删除已有文件：" + selectedPath);
			}
			bool result;
			await using (FileStream fileStream = new FileStream(selectedPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
			{
				ZipOutputStream zipStream = new ZipOutputStream((Stream)fileStream);
				bool flag;
				try
				{
					foreach (string sourceFile in sourceFiles)
					{
						cancellationToken.ThrowIfCancellationRequested();
						string itemFileName = Path.GetFileName(sourceFile);
						string tempPath = Path.Combine(tempDirName, itemFileName);
						await CopyFileAsync(sourceFile, tempPath, cancellationToken);
						await using (FileStream sourceStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
						{
							ZipEntry val = new ZipEntry(itemFileName);
							await zipStream.PutNextEntryAsync(val, cancellationToken);
							await sourceStream.CopyToAsync((Stream)(object)zipStream, cancellationToken);
						}
						File.Delete(tempPath);
					}
					await ((DeflaterOutputStream)zipStream).FinishAsync(cancellationToken);
					LogWrapper.Info("Files", "导出 Zip 成功：" + selectedPath);
					flag = true;
				}
				finally
				{
					if (zipStream != null)
					{
						await ((Stream)(object)zipStream).DisposeAsync();
					}
				}
				result = flag;
			}
			return result;
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "Log", "导出 Zip 失败");
			return false;
		}
		finally
		{
			if (Directory.Exists(tempDirName))
			{
				Directory.Delete(tempDirName, recursive: true);
				LogWrapper.Debug("Log", "清理临时文件夹：" + tempDirName);
			}
		}
	}

	public static async Task CopyFileAsync(string fromPath, string toPath, CancellationToken cancelToken = default(CancellationToken))
	{
		_ = 2;
		try
		{
			string fullPath = GetFullPath(fromPath);
			string fullPath2 = GetFullPath(toPath);
			if (fullPath == fullPath2)
			{
				return;
			}
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath2) ?? throw new InvalidOperationException("无法获取目标目录"));
			await using FileStream sourceStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
			await using FileStream destinationStream = new FileStream(fullPath2, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
			await sourceStream.CopyToAsync(destinationStream, cancelToken);
		}
		catch (Exception innerException)
		{
			throw new IOException("复制文件出错：" + fromPath + " -> " + toPath, innerException);
		}
	}

	public static async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken cancelToken = default(CancellationToken))
	{
		Directory.CreateDirectory(destDir);
		string[] files = Directory.GetFiles(sourceDir);
		string[] directories = Directory.GetDirectories(sourceDir);
		ParallelOptions parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount),
			CancellationToken = cancelToken
		};
		await Parallel.ForEachAsync(files, parallelOptions, async (string file, CancellationToken ct) =>
		{
			string toPath = Path.Combine(destDir, Path.GetFileName(file));
			await CopyFileAsync(file, toPath, ct);
		});
		await Parallel.ForEachAsync(directories, parallelOptions, async (string subDir, CancellationToken ct) =>
		{
			string destDir2 = Path.Combine(destDir, Path.GetFileName(subDir));
			await CopyDirectoryAsync(subDir, destDir2, ct);
		});
	}

	public static async Task<byte[]> ReadAllBytesOrEmptyAsync(string filePath, CancellationToken cancelToken = default(CancellationToken))
	{
		try
		{
			string fullPath = GetFullPath(filePath);
			if (File.Exists(fullPath))
			{
				return await File.ReadAllBytesAsync(fullPath, cancelToken);
			}
			throw new FileNotFoundException(fullPath);
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "读取文件出错：" + filePath);
			return Array.Empty<byte>();
		}
	}

	public static async Task<string> ReadAllTextOrEmptyAsync(string filePath, Encoding? encoding = null, CancellationToken cancelToken = default(CancellationToken))
	{
		_ = 1;
		try
		{
			string fullPath = GetFullPath(filePath);
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException(fullPath);
			}
			if (encoding == null)
			{
				return await File.ReadAllTextAsync(fullPath, cancelToken);
			}
			return await File.ReadAllTextAsync(fullPath, encoding, cancelToken);
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "读取文件出错：" + filePath);
			return "";
		}
	}

	public static async Task<string> ReadAllTextOrEmptyAsync(Stream stream, Encoding? encoding = null, CancellationToken cancelToken = default(CancellationToken))
	{
		try
		{
			ArgumentNullException.ThrowIfNull(stream, "stream");
			using MemoryStream memoryStream = new MemoryStream();
			await stream.CopyToAsync(memoryStream, cancelToken);
			byte[] buffer = memoryStream.GetBuffer();
			int count = (int)memoryStream.Length;
			return (encoding ?? EncodingDetector.DetectEncoding(buffer)).GetString(buffer, 0, count);
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, $"读取流出错: {stream}");
			return string.Empty;
		}
	}

	public static async Task<MemoryStream> ReadFileToStreamOrEmptyAsync(string filePath, CancellationToken cancelToken = default(CancellationToken))
	{
		_ = 1;
		try
		{
			string fullPath = GetFullPath(filePath);
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException(fullPath);
			}
			MemoryStream result;
			await using (FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
			{
				MemoryStream memoryStream = new MemoryStream();
				await fileStream.CopyToAsync(memoryStream, cancelToken);
				memoryStream.Position = 0L;
				result = memoryStream;
			}
			return result;
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "读取文件到流出错：" + filePath);
			return new MemoryStream();
		}
	}

	public static async Task WriteFileAsync(string filePath, string text, bool append = false, Encoding? encoding = null, CancellationToken cancelToken = default(CancellationToken))
	{
		string fullPath = GetFullPath(filePath);
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("无法获取目标目录"));
		if (append)
		{
			await using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				if (encoding == null)
				{
					encoding = EncodingDetector.DetectEncoding(stream);
				}
			}
			await File.AppendAllTextAsync(fullPath, text, encoding, cancelToken);
		}
		else
		{
			if (encoding == null)
			{
				encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
			}
			await File.WriteAllTextAsync(fullPath, text, encoding, cancelToken);
		}
	}

	public static async Task WriteFileAsync(string filePath, byte[] content, bool append = false, CancellationToken cancelToken = default(CancellationToken))
	{
		string fullPath = GetFullPath(filePath);
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("无法获取目标目录"));
		FileMode mode = (append ? FileMode.Append : FileMode.Create);
		await using FileStream fileStream = new FileStream(fullPath, mode, FileAccess.Write, FileShare.Read);
		await fileStream.WriteAsync(content.AsMemory(), cancelToken);
	}

	public static async Task<bool> WriteFileAsync(string filePath, Stream? stream, CancellationToken cancelToken = default(CancellationToken))
	{
		if (stream == null)
		{
			return false;
		}
		try
		{
			string fullPath = GetFullPath(filePath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("无法获取目标目录"));
			bool result;
			await using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				fileStream.SetLength(0L);
				await stream.CopyToAsync(fileStream, cancelToken).ConfigureAwait(continueOnCapturedContext: false);
				result = true;
			}
			return result;
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "保存流出错");
			return false;
		}
	}

	public static async Task ExtractFileAsync(string? compressFilePath, string? destDirectory, Action<double>? progressIncrementHandler = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrEmpty(compressFilePath))
		{
			LogWrapper.Error(new ArgumentNullException("compressFilePath"), "压缩文件路径为空");
			return;
		}
		if (string.IsNullOrEmpty(destDirectory))
		{
			LogWrapper.Error(new ArgumentNullException("destDirectory"), "目标目录路径为空");
			return;
		}
		try
		{
			Directory.CreateDirectory(destDirectory);
			if (compressFilePath.EndsWithF(".gz") || compressFilePath.EndsWithF(".tgz"))
			{
				await _ExtractGZipAsync(compressFilePath, destDirectory, progressIncrementHandler, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			if (compressFilePath.EndsWithF(".bz2"))
			{
				await _ExtractBZip2Async(compressFilePath, destDirectory, progressIncrementHandler, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			if (compressFilePath.EndsWithF(".tar"))
			{
				await _ExtractTarAsync(compressFilePath, destDirectory, progressIncrementHandler, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			if (!compressFilePath.EndsWithF(".zip") && !compressFilePath.EndsWithF(".jar"))
			{
				throw new NotSupportedException("不支持的压缩文件格式");
			}
			await _ExtractZipAsync(compressFilePath, destDirectory, progressIncrementHandler, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "解压文件 " + compressFilePath + " 失败");
			throw;
		}
	}

	private static async Task _ExtractGZipAsync(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler, CancellationToken cancellationToken)
	{
		string text = Path.GetFileName(compressFilePath).ToLower();
		bool flag = text.EndsWithF(".tar.gz") || text.EndsWithF(".tgz");
		if (flag)
		{
			text = text.Replace(".tar.gz", "").Replace(".tgz", "");
		}
		else if (text.EndsWithF(".gz"))
		{
			text = text.Replace(".gz", "");
		}
		string path = _GetSafePath(destDirectory, text);
		await using FileStream compressedFile = new FileStream(compressFilePath, FileMode.Open, FileAccess.Read);
		GZipInputStream gzipStream = new GZipInputStream((Stream)compressedFile);
		try
		{
			if (flag)
			{
				TarInputStream tarStream = new TarInputStream((Stream)(object)gzipStream, Encoding.UTF8);
				try
				{
					await _ExtractTarStreamAsync(tarStream, destDirectory, progressIncrementHandler, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (tarStream != null)
					{
						await ((Stream)(object)tarStream).DisposeAsync();
					}
				}
			}
			else
			{
				await using FileStream outputStream = new FileStream(path, FileMode.Create, FileAccess.Write);
				await ((Stream)(object)gzipStream).CopyToAsync((Stream)outputStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				progressIncrementHandler?.Invoke(1.0);
			}
		}
		finally
		{
			if (gzipStream != null)
			{
				await ((Stream)(object)gzipStream).DisposeAsync();
			}
		}
	}

	private static async Task _ExtractBZip2Async(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler, CancellationToken cancellationToken)
	{
		string entryName = Path.GetFileName(compressFilePath).ToLower().Replace(".bz2", "");
		string path = _GetSafePath(destDirectory, entryName);
		await using FileStream compressedFile = new FileStream(compressFilePath, FileMode.Open, FileAccess.Read);
		BZip2InputStream bzip2Stream = new BZip2InputStream((Stream)compressedFile);
		try
		{
			await using FileStream outputStream = new FileStream(path, FileMode.Create, FileAccess.Write);
			await ((Stream)(object)bzip2Stream).CopyToAsync((Stream)outputStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			progressIncrementHandler?.Invoke(1.0);
		}
		finally
		{
			if (bzip2Stream != null)
			{
				await ((Stream)(object)bzip2Stream).DisposeAsync();
			}
		}
	}

	private static async Task _ExtractTarAsync(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler, CancellationToken cancellationToken)
	{
		await using FileStream compressedFile = new FileStream(compressFilePath, FileMode.Open, FileAccess.Read);
		TarInputStream tarStream = new TarInputStream((Stream)compressedFile, Encoding.UTF8);
		try
		{
			await _ExtractTarStreamAsync(tarStream, destDirectory, progressIncrementHandler, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (tarStream != null)
			{
				await ((Stream)(object)tarStream).DisposeAsync();
			}
		}
	}

	private static async Task _ExtractTarStreamAsync(TarInputStream tarStream, string destDirectory, Action<double>? progressIncrementHandler, CancellationToken cancellationToken)
	{
		List<TarEntry> entries = new List<TarEntry>();
		long totalBytes = 0L;
		while (true)
		{
			TarEntry val = await _GetNextEntryAsync(tarStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (val == null)
			{
				break;
			}
			cancellationToken.ThrowIfCancellationRequested();
			entries.Add(val);
			totalBytes += val.Size;
		}
		long processedBytes = 0L;
		tarStream.Reset();
		foreach (TarEntry entry in entries)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				string destinationPath = _GetSafePath(destDirectory, entry.Name);
				if (entry.IsDirectory)
				{
					await _CreateDirectoryAsync(destinationPath, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				await _CreateDirectoryAsync(Path.GetDirectoryName(destinationPath), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (entry.Size == 0L)
				{
					await File.Create(destinationPath).DisposeAsync();
					continue;
				}
				await using FileStream outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
				await tarStream.CopyEntryContentsAsync((Stream)outputStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				processedBytes += entry.Size;
				progressIncrementHandler?.Invoke((double)processedBytes / (double)totalBytes);
			}
			catch (IOException ex)
			{
				throw new InvalidOperationException("Failed to extract entry " + entry.Name + ": " + ex.Message, ex);
			}
		}
	}

	private static async Task<TarEntry?> _GetNextEntryAsync(TarInputStream tarStream, CancellationToken cancellationToken)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		Task<TarEntry> task = Task.Run((Func<TarEntry>)tarStream.GetNextEntry, cts.Token);
		if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10L), cts.Token)).ConfigureAwait(continueOnCapturedContext: false) == task)
		{
			return await task.ConfigureAwait(continueOnCapturedContext: false);
		}
		throw new TimeoutException("Operation timed out while reading next Tar entry.");
	}

	private static string _GetSafePath(string destDirectory, string entryName)
	{
		string text = Path.GetFullPath(destDirectory).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}) + Path.DirectorySeparatorChar;
		string fullPath = Path.GetFullPath(Path.Combine(text, entryName));
		if (!fullPath.StartsWith(text, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Invalid path detected: " + entryName);
		}
		return fullPath;
	}

	private static async Task _CreateDirectoryAsync(string path, CancellationToken cancellationToken)
	{
		await Task.Run(() => Directory.CreateDirectory(path), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static async Task _ExtractZipAsync(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler, CancellationToken cancellationToken)
	{
		ZipFile zipFile = new ZipFile(compressFilePath, (StringCodec)null);
		try
		{
			long totalEntries = zipFile.Count;
			long currentEntry = 0L;
			foreach (ZipEntry item in zipFile)
			{
				ZipEntry val = item;
				string path = _GetSafePath(destDirectory, val.Name);
				if (val.IsDirectory)
				{
					Directory.CreateDirectory(path);
					continue;
				}
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				await using Stream zipStream = zipFile.GetInputStream(val);
				await using FileStream outputStream = new FileStream(path, FileMode.Create, FileAccess.Write);
				await zipStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				currentEntry++;
				progressIncrementHandler?.Invoke((double)currentEntry / (double)totalEntries);
			}
		}
		finally
		{
			((IDisposable)zipFile)?.Dispose();
		}
	}

	public static async Task<string> ComputeFileHashAsync(string? filePath, IHashProvider hashProvider, bool ignoreIfBusy = false)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			LogWrapper.Warn(new ArgumentNullException("filePath"), "文件路径为空");
			return string.Empty;
		}
		bool flag = ignoreIfBusy;
		if (flag)
		{
			flag = await CheckFileBusyAsync(filePath).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (flag)
		{
			return string.Empty;
		}
		for (int attempt = 0; attempt < 2; attempt++)
		{
			try
			{
				using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return (await hashProvider.ComputeHashAsync(fs).ConfigureAwait(continueOnCapturedContext: false)).ToHexString();
			}
			catch (Exception ex) when ((ex is FileNotFoundException || ex is DirectoryNotFoundException) ? true : false)
			{
				LogWrapper.Warn(ex, "计算文件哈希失败：" + filePath);
				return string.Empty;
			}
			catch (Exception ex2)
			{
				if (attempt == 0)
				{
					LogWrapper.Warn(ex2, "计算文件哈希可重试失败：" + filePath);
					await Task.Delay(Random.Shared.Next(200, 500)).ConfigureAwait(continueOnCapturedContext: false);
					continue;
				}
				LogWrapper.Warn(ex2, "计算文件哈希失败：" + filePath);
				return string.Empty;
			}
		}
		return string.Empty;
	}

	public static Task<string> GetFileMD5Async(string? filePath)
	{
		return ComputeFileHashAsync(filePath, MD5Provider.Instance);
	}

	public static Task<string> GetFileSHA1Async(string? filePath)
	{
		return ComputeFileHashAsync(filePath, SHA1Provider.Instance);
	}

	public static Task<string> GetFileSHA256Async(string? filePath, bool ignoreIfBusy = false)
	{
		return ComputeFileHashAsync(filePath, SHA256Provider.Instance, ignoreIfBusy);
	}

	public static Task<string> GetFileSHA512Async(string? filePath, bool ignoreIfBusy = false)
	{
		return ComputeFileHashAsync(filePath, SHA512Provider.Instance, ignoreIfBusy);
	}

	public static string GetFullPath(string filePath)
	{
		ArgumentNullException.ThrowIfNull(filePath, "filePath");
		if (!Path.IsPathRooted(filePath))
		{
			return Path.Combine(Paths.DefaultDirectory, filePath);
		}
		return filePath;
	}

	public static async Task<bool> CheckFileBusyAsync(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return false;
			}
			bool result;
			await using (new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete))
			{
				result = false;
			}
			return result;
		}
		catch (IOException)
		{
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static async Task<int> PasteFromClipboardAsync(string dest, bool copyFile, bool copyDir)
	{
		if (string.IsNullOrEmpty(dest))
		{
			throw new ArgumentException("Destination folder cannot be null or empty.", "dest");
		}
		if (!Directory.Exists(dest))
		{
			Directory.CreateDirectory(dest);
		}
		IDataObject dataObject = Clipboard.GetDataObject();
		if (dataObject == null || !dataObject.GetDataPresent(DataFormats.FileDrop))
		{
			return 0;
		}
		if (!(dataObject.GetData(DataFormats.FileDrop) is string[] array))
		{
			return 0;
		}
		if (array.Length == 0)
		{
			return 0;
		}
		int count = 0;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (File.Exists(text) & copyFile)
			{
				string toPath = Path.Combine(dest, Path.GetFileName(text));
				await CopyFileAsync(text, toPath);
				count++;
			}
			else if (Directory.Exists(text) & copyDir)
			{
				string destDir = Path.Combine(dest, new DirectoryInfo(text).Name);
				await CopyDirectoryAsync(text, destDir);
				count++;
			}
		}
		return count;
	}

	public static JsonNode MergeJson(JsonNode target, JsonNode source)
	{
		if (target == null && source == null)
		{
			throw new ArgumentNullException("target", "目标和源 JSON 不能同时为 null。");
		}
		if (target == null)
		{
			return source.DeepClone();
		}
		if (!(target is JsonObject jsonObject) || !(source is JsonObject jsonObject2))
		{
			return source.DeepClone();
		}
		JsonObject jsonObject3 = (JsonObject)jsonObject.DeepClone();
		foreach (var (propertyName, jsonNode2) in jsonObject2)
		{
			JsonNode jsonNode3 = jsonObject3[propertyName];
			if (jsonNode2 == null)
			{
				continue;
			}
			if (jsonNode2 is JsonObject && jsonNode3 is JsonObject)
			{
				jsonObject3[propertyName] = MergeJson(jsonNode3, jsonNode2);
			}
			else if (jsonNode2 is JsonArray jsonArray && jsonNode3 is JsonArray jsonArray2)
			{
				HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
				JsonArray jsonArray3 = new JsonArray();
				foreach (JsonNode item3 in jsonArray2)
				{
					if (item3 != null)
					{
						string item = item3.ToJsonString();
						if (hashSet.Add(item))
						{
							jsonArray3.Add(item3.DeepClone());
						}
					}
				}
				foreach (JsonNode item4 in jsonArray)
				{
					if (item4 != null)
					{
						string item2 = item4.ToJsonString();
						if (hashSet.Add(item2))
						{
							jsonArray3.Add(item4.DeepClone());
						}
					}
				}
				jsonObject3[propertyName] = jsonArray3;
			}
			else
			{
				jsonObject3[propertyName] = jsonNode2.DeepClone();
			}
		}
		return jsonObject3;
	}

	public static async Task<string?> CheckAsync(string localPath, long minSize = -1L, long actualSize = -1L, string? hash = null, bool isJson = false)
	{
		_ = 5;
		try
		{
			LogWrapper.Debug("Checker", "开始校验文件 " + localPath);
			FileInfo fileInfo = new FileInfo(localPath);
			if (!fileInfo.Exists)
			{
				return "文件不存在：" + localPath;
			}
			long fileSize = fileInfo.Length;
			StringBuilder errors = new StringBuilder();
			bool allowSizeMismatch = false;
			if (!string.IsNullOrEmpty(hash))
			{
				int length = hash.Length;
				string text;
				if (length < 35)
				{
					text = await GetFileMD5Async(localPath);
				}
				else
				{
					text = ((length != 64) ? (await GetFileSHA1Async(localPath)) : (await GetFileSHA256Async(localPath)));
				}
				string text2 = text;
				if (!string.Equals(hash, text2, StringComparison.OrdinalIgnoreCase))
				{
					length = hash.Length;
					if (length < 35)
					{
						text = "MD5";
					}
					else
					{
						text = ((length != 64) ? "SHA1" : "SHA256");
					}
					string value = text;
					StringBuilder stringBuilder = errors;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 3, stringBuilder);
					handler.AppendLiteral("文件 ");
					handler.AppendFormatted(value);
					handler.AppendLiteral(" 应为 ");
					handler.AppendFormatted(hash);
					handler.AppendLiteral("，实际为 ");
					handler.AppendFormatted(text2);
					stringBuilder.AppendLine(ref handler);
				}
				else
				{
					allowSizeMismatch = true;
				}
			}
			if (actualSize >= 0 && actualSize != fileSize && !allowSizeMismatch)
			{
				string text = ((fileSize >= 2000) ? "" : (await ReadAllTextOrEmptyAsync(localPath)));
				string text3 = text;
				errors.AppendLine($"文件大小应为 {actualSize} B，实际为 {fileSize} B" + (string.IsNullOrEmpty(text3) ? "" : ("，内容为 " + text3)));
			}
			if (minSize >= 0 && minSize > fileSize)
			{
				string text = ((fileSize >= 2000) ? "" : (await ReadAllTextOrEmptyAsync(localPath)));
				string text4 = text;
				errors.AppendLine($"文件大小应大于 {minSize} B，实际为 {fileSize} B" + (string.IsNullOrEmpty(text4) ? "" : ("，内容为 " + text4)));
			}
			if (isJson)
			{
				string text5 = await ReadAllTextOrEmptyAsync(localPath);
				if (string.IsNullOrEmpty(text5))
				{
					throw new Exception("读取到的文件为空");
				}
				try
				{
					using (JsonDocument.Parse(text5, JsonCompat.DocumentOptions))
					{
					}
				}
				catch (JsonException innerException)
				{
					throw new Exception(Lang.Text("Common.Error.InvalidJson"), innerException);
				}
			}
			if (errors.Length <= 0)
			{
				return null;
			}
			errors.Insert(0, "实际校验地址：" + localPath + "\n");
			return errors.ToString().TrimEnd();
		}
		catch (Exception ex)
		{
			LogWrapper.Warn("Checker", $"检查文件出错: {ex}");
			return ex.ToString();
		}
	}

	public static bool IsPathWithinDirectory(string childPath, string baseDirectory)
	{
		string fullPath = Path.GetFullPath(baseDirectory);
		return Path.GetFullPath(childPath).StartsWith(fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}
}
