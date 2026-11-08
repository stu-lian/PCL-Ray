using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils.Hash;

namespace PCL.Core.IO.Storage.Cache;

public class FileCacheStorage : IDisposable
{
	private enum ReleaseResult
	{
		Retry,
		Decremented,
		Removed
	}

	private readonly HashStorage _hashStorage;

	private readonly string _basePath;

	private readonly ConcurrentDictionary<string, int> _refCounts = new ConcurrentDictionary<string, int>();

	public FileCacheStorage(string cacheRoot, bool enableCompression = true)
	{
		_basePath = cacheRoot;
		Directory.CreateDirectory(cacheRoot);
		_hashStorage = new HashStorage(cacheRoot, SHA256Provider.Instance, enableCompression, correctMisplacedFile: false);
	}

	public async Task<string> StoreAsync(Stream source, string? knownHash = null)
	{
		string text = await _hashStorage.PutAsync(source, knownHash).ConfigureAwait(continueOnCapturedContext: false);
		if (text != null)
		{
			_refCounts.AddOrUpdate(text, 1, (string _, int count) => count + 1);
		}
		return text;
	}

	public Stream? Retrieve(string hash)
	{
		return _hashStorage.Get(hash);
	}

	public string? GetFilePath(string hash)
	{
		string path = hash.Substring(0, 2);
		string text = Path.Combine(_basePath, path, hash);
		if (!File.Exists(text))
		{
			return null;
		}
		return text;
	}

	public bool Exists(string hash)
	{
		return _hashStorage.Exists(hash);
	}

	public async Task<bool> ReleaseAsync(string hash)
	{
		SpinWait spin = default;
		int value;
		while (_refCounts.TryGetValue(hash, out value))
		{
			switch (TryRelease(hash, value))
			{
			case ReleaseResult.Removed:
				return await _hashStorage.DeleteAsync(hash).ConfigureAwait(continueOnCapturedContext: false);
			case ReleaseResult.Decremented:
				return true;
			}
			if (spin.NextSpinWillYield)
			{
				await Task.Yield();
			}
			else
			{
				spin.SpinOnce();
			}
		}
		return false;
	}

	private ReleaseResult TryRelease(string hash, int expected)
	{
		if (expected <= 1)
		{
			if (!_refCounts.TryRemove(new KeyValuePair<string, int>(hash, expected)))
			{
				return ReleaseResult.Retry;
			}
			return ReleaseResult.Removed;
		}
		if (!_refCounts.TryUpdate(hash, expected - 1, expected))
		{
			return ReleaseResult.Retry;
		}
		return ReleaseResult.Decremented;
	}

	public Task<bool> ForceDeleteAsync(string hash)
	{
		_refCounts.TryRemove(hash, out var _);
		return _hashStorage.DeleteAsync(hash);
	}

	public void Dispose()
	{
		_refCounts.Clear();
	}
}
