using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Saves.Editing;
using PCL.Core.Minecraft.Saves.Editing.Internal;
using PCL.Core.Minecraft.Saves.Exceptions;
using PCL.Core.Minecraft.Saves.Parsing;
using fNbt;

namespace PCL.Core.Minecraft.Saves;

public class SaveManager
{
	private readonly SaveParserFactory _parserFactory;

	private readonly IReadOnlyList<ISaveEditor> _editors;

	public SaveManager(SaveParserFactory? parserFactory = null, IEnumerable<ISaveEditor>? customEditors = null)
	{
		_parserFactory = parserFactory ?? new SaveParserFactory();
		_editors = customEditors?.ToArray() ?? new ISaveEditor[2]
		{
			new Pre261SaveEditor(),
			new Version261PlusSaveEditor()
		};
	}

	public async Task<IReadOnlyList<SaveInfo>> ScanSaveFoldersAsync(string savesPath, CancellationToken ct = default(CancellationToken))
	{
		if (!Directory.Exists(savesPath))
		{
			return Array.Empty<SaveInfo>();
		}
		string[] directories = Directory.GetDirectories(savesPath);
		List<SaveInfo> results = new List<SaveInfo>(directories.Length);
		string[] array = directories;
		foreach (string folder in array)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				SaveInfo saveInfo = await LoadSaveAsync(folder, ct).ConfigureAwait(continueOnCapturedContext: false);
				if ((object)saveInfo != null)
				{
					results.Add(saveInfo);
				}
			}
			catch (SaveNotFoundException)
			{
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex3)
			{
				LogWrapper.Warn(ex3, "Saves", "扫描存档文件夹失败：" + folder);
			}
		}
		return results.OrderByDescending((SaveInfo s) => s.LastPlayedUtc).ToList();
	}

	public Task<SaveInfo> LoadSaveAsync(string folderPath, CancellationToken ct = default(CancellationToken))
	{
		string text = ResolveLevelDatPath(folderPath);
		if (text == null)
		{
			throw new SaveNotFoundException(folderPath);
		}
		return LoadFromPathAsync(folderPath, text, ct);
	}

	public async IAsyncEnumerable<SaveInfo> LoadSavesAsync(string savesPath, [EnumeratorCancellation] CancellationToken ct = default(CancellationToken))
	{
		if (!Directory.Exists(savesPath))
		{
			yield break;
		}
		string[] directories = Directory.GetDirectories(savesPath);
		string[] array = directories;
		foreach (string folder in array)
		{
			ct.ThrowIfCancellationRequested();
			SaveInfo info = null;
			try
			{
				info = await LoadSaveAsync(folder, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (SaveNotFoundException)
			{
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex3)
			{
				LogWrapper.Warn(ex3, "Saves", "加载存档失败：" + folder);
			}
			if ((object)info != null)
			{
				yield return info;
			}
		}
	}

	public async Task<bool> ApplyChangesAsync(string folderPath, SaveChanges changes, CancellationToken ct = default(CancellationToken))
	{
		if (changes.IsEmpty)
		{
			return false;
		}
		string levelDatPath = ResolveLevelDatPath(folderPath) ?? throw new SaveNotFoundException(folderPath);
		NbtFile nbtFile;
		NbtCompound data;
		try
		{
			nbtFile = new NbtFile();
			await Task.Run(() =>
			{
				using FileStream fileStream = new FileStream(levelDatPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
				nbtFile.LoadFromStream((Stream)fileStream, (NbtCompression)0);
			}, ct).ConfigureAwait(continueOnCapturedContext: false);
			data = nbtFile.RootTag.Get<NbtCompound>("Data") ?? throw new InvalidDataException("level.dat 中缺少 Data 复合标签");
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			throw new SaveCorruptedException(folderPath, "解析 level.dat 失败：'" + levelDatPath + "'", ex);
		}
		int? num = ReadDataVersionFromCompound(data);
		foreach (ISaveEditor editor in _editors)
		{
			if (!editor.CanHandle(num))
			{
				continue;
			}
			try
			{
				if (!editor.ApplyChanges(data, changes))
				{
					return false;
				}
			}
			catch (Exception ex2) when (!(ex2 is OperationCanceledException))
			{
				throw new SaveCorruptedException(folderPath, "应用修改失败：'" + folderPath + "'", ex2);
			}
			await WriteLevelDatAtomicallyAsync(levelDatPath, nbtFile, ct).ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}
		throw new SaveCorruptedException(folderPath, $"找不到匹配的存档编辑器（DataVersion: {num}）");
	}

	private static async Task WriteLevelDatAtomicallyAsync(string sourcePath, NbtFile nbtFile, CancellationToken ct)
	{
		string directoryName = Path.GetDirectoryName(sourcePath);
		string tempPath = Path.Combine(directoryName, $"level{Guid.NewGuid():N}.dat");
		string backupPath = Path.Combine(directoryName, "level.dat_old");
		string targetPath = Path.Combine(directoryName, "level.dat");
		try
		{
			await Task.Run(() =>
			{
				using FileStream fileStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
				nbtFile.SaveToStream((Stream)fileStream, (NbtCompression)2);
			}, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (sourcePath == targetPath && File.Exists(targetPath))
			{
				File.Move(targetPath, backupPath, overwrite: true);
			}
			File.Move(tempPath, targetPath);
		}
		catch
		{
			TryDelete(tempPath);
			throw;
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private async Task<SaveInfo> LoadFromPathAsync(string folderPath, string levelDatPath, CancellationToken ct)
	{
		NbtFile val;
		try
		{
			val = await LoadNbtFileAsync(levelDatPath, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception inner)
		{
			throw new SaveCorruptedException(folderPath, "解析 level.dat 失败：'" + levelDatPath + "'", inner);
		}
		NbtCompound data = val.RootTag.Get<NbtCompound>("Data") ?? throw new SaveCorruptedException(folderPath, "level.dat 中缺少 Data 复合标签：" + levelDatPath);
		int? num = ReadDataVersionFromCompound(data);
		DateTime creationTimeUtc = Directory.GetCreationTimeUtc(folderPath);
		DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(levelDatPath);
		return (_parserFactory.Resolve(data, num) ?? throw new SaveCorruptedException(folderPath, $"找不到与存档 '{folderPath}' 匹配的解析器（DataVersion: {num}）")).Parse(folderPath, data, creationTimeUtc, lastWriteTimeUtc);
	}

	private static async Task<NbtFile> LoadNbtFileAsync(string path, CancellationToken ct)
	{
		NbtFile nbtFile = new NbtFile();
		await Task.Run(() =>
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
			nbtFile.LoadFromStream((Stream)fileStream, (NbtCompression)0);
		}, ct).ConfigureAwait(continueOnCapturedContext: false);
		return nbtFile;
	}

	private static string? ResolveLevelDatPath(string folderPath)
	{
		string text = Path.Combine(folderPath, "level.dat");
		if (File.Exists(text))
		{
			return text;
		}
		string text2 = Path.Combine(folderPath, "level.dat_old");
		if (!File.Exists(text2))
		{
			return null;
		}
		return text2;
	}

	private static int? ReadDataVersionFromCompound(NbtCompound data)
	{
		NbtInt val = default;
		if (data.TryGet<NbtInt>("DataVersion", out val))
		{
			return val.Value;
		}
		return null;
	}
}
