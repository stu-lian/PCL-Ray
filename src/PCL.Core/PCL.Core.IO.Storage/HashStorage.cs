using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;

namespace PCL.Core.IO.Storage;

public class HashStorage(string folder, IHashProvider hashProvider, bool compressObjects = false, bool correctMisplacedFile = true, int prefixLength = 2)
{
	public async Task<string?> PutAsync(string fromPath, string? hash = null)
	{
		ArgumentNullException.ThrowIfNull(fromPath, "fromPath");
		if (!File.Exists(Path.GetFullPath(fromPath)))
		{
			return null;
		}
		string result;
		await using (FileStream originalFs = File.Open(fromPath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			result = await PutAsync(originalFs, hash).ConfigureAwait(continueOnCapturedContext: false);
		}
		return result;
	}

	public async Task<string?> PutAsync(Stream input, string? hash = null)
	{
		ArgumentNullException.ThrowIfNull(input, "input");
		if (hash != null && hash.Length != hashProvider.Length)
		{
			throw new ArgumentException("Provide hash is not correct", "hash");
		}
		if (input.CanSeek)
		{
			input.Position = 0L;
		}
		string text = hash;
		if (text == null)
		{
			text = (await hashProvider.ComputeHashAsync(input).ConfigureAwait(continueOnCapturedContext: false)).ToHexString();
		}
		string fileHash = text;
		string text2 = _GetDestPath(fileHash);
		if (correctMisplacedFile && _CorrectMisplacedFile(fileHash))
		{
			LogWrapper.Info("HashStorage", "Move misplaced file into correct folder");
		}
		if (File.Exists(text2))
		{
			return fileHash;
		}
		string result;
		await using (Stream destinationFs = _GetSaveStream(text2))
		{
			await input.CopyToAsync(destinationFs).ConfigureAwait(continueOnCapturedContext: false);
			result = fileHash;
		}
		return result;
	}

	public Task<bool> DeleteAsync(string hash)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		string text = _GetDestPath(hash);
		if (!File.Exists(text) && correctMisplacedFile)
		{
			text = _GetMisplacedFilePath(hash);
		}
		if (!File.Exists(text))
		{
			return Task.FromResult(result: false);
		}
		try
		{
			File.Delete(text);
		}
		catch (FileNotFoundException)
		{
		}
		catch (DirectoryNotFoundException ex2)
		{
			LogWrapper.Error(ex2, "HashStorage", "Unexpected directory not found " + text);
			return Task.FromResult(result: false);
		}
		catch (IOException ex3)
		{
			LogWrapper.Error(ex3, "HashStorage", "Failed to delete file " + text);
			return Task.FromResult(result: false);
		}
		catch (UnauthorizedAccessException ex4)
		{
			LogWrapper.Error(ex4, "HashStorage", "Access denied when deleting file " + text);
			return Task.FromResult(result: false);
		}
		return Task.FromResult(result: true);
	}

	public Stream? Get(string hash)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		string text = _GetDestPath(hash);
		if (correctMisplacedFile && _CorrectMisplacedFile(hash))
		{
			LogWrapper.Info("HashStorage", "Move misplaced file into correct folder: " + hash);
		}
		if (!File.Exists(text))
		{
			return null;
		}
		return _GetReadStream(text);
	}

	public bool Exists(string hash)
	{
		ArgumentNullException.ThrowIfNull(hash, "hash");
		if (!File.Exists(_GetDestPath(hash)))
		{
			if (correctMisplacedFile)
			{
				return File.Exists(_GetMisplacedFilePath(hash));
			}
			return false;
		}
		return true;
	}

	private Stream _GetSaveStream(string destPath)
	{
		FileStream fileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
		if (compressObjects)
		{
			return new DeflateStream(fileStream, CompressionMode.Compress);
		}
		return fileStream;
	}

	private Stream _GetReadStream(string destPath)
	{
		FileStream fileStream = File.Open(destPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		if (compressObjects)
		{
			return new DeflateStream(fileStream, CompressionMode.Decompress);
		}
		return fileStream;
	}

	private string _GetDestPath(string hash)
	{
		return Path.Combine(folder, _GetPrefixFolder(hash), hash);
	}

	private bool _CorrectMisplacedFile(string hash)
	{
		string text = _GetMisplacedFilePath(hash);
		if (!File.Exists(text))
		{
			return false;
		}
		string destFileName = _GetDestPath(hash);
		File.Move(text, destFileName);
		return true;
	}

	private string _GetMisplacedFilePath(string hash)
	{
		return Path.Combine(folder, hash);
	}

	private string _GetPrefixFolder(string hash)
	{
		if (hash.Length < prefixLength)
		{
			throw new ArgumentException($"Hash length({hash.Length}) is shorter than required prefix length({prefixLength})", "hash");
		}
		string text = hash.Substring(0, prefixLength);
		Directory.CreateDirectory(Path.Combine(folder, text));
		return text;
	}
}
