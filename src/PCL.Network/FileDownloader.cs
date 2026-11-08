using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO.Net;
using PCL.Core.Utils;

namespace PCL.Network;

public static class FileDownloader
{
	private const int RequestTimeoutMilliseconds = 30000;

	private const int MaxDownloadRetries = 3;

	public static async Task DownloadAsync(string url, string localPath, bool useBrowserUserAgent = false, string customUserAgent = "", CancellationToken cancellationToken = default(CancellationToken), bool enableParallelChunks = true, DownloadFile? trackedFile = null)
	{
		await DownloadCoreAsync(new _003C_003Ez__ReadOnlySingleElementList<string>(url), localPath, useBrowserUserAgent, customUserAgent, cancellationToken, enableParallelChunks, trackedFile).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task DownloadAsync(IEnumerable<string> urls, string localPath, bool useBrowserUserAgent = false, string customUserAgent = "", CancellationToken cancellationToken = default(CancellationToken), bool enableParallelChunks = true, DownloadFile? trackedFile = null)
	{
		await DownloadCoreAsync(urls, localPath, useBrowserUserAgent, customUserAgent, cancellationToken, enableParallelChunks, trackedFile).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static void DownloadByLoader(string url, string localPath, bool useBrowserUserAgent = false, string customUserAgent = "")
	{
		DownloadAsync(url, localPath, useBrowserUserAgent, customUserAgent).GetAwaiter().GetResult();
	}

	public static void DownloadByLoader(IEnumerable<string> urls, string localPath, bool useBrowserUserAgent = false, string customUserAgent = "")
	{
		DownloadAsync(urls, localPath, useBrowserUserAgent, customUserAgent).GetAwaiter().GetResult();
	}

	private static async Task DownloadCoreAsync(IEnumerable<string> urls, string localPath, bool useBrowserUserAgent, string customUserAgent, CancellationToken cancellationToken, bool enableParallelChunks, DownloadFile? trackedFile)
	{
		List<string> urlList = (from text in urls
			select RequestSigning.SecretCdnSign(text.Trim()) into value
			where !string.IsNullOrWhiteSpace(value)
			select value).Distinct().ToList();
		if (urlList.Count == 0)
		{
			throw new ArgumentException("未提供可用的下载地址", "urls");
		}
		Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? throw new ArgumentException("下载路径无效", "localPath"));
		Exception lastException = null;
		for (int retry = 0; retry <= 3; retry++)
		{
			foreach (string url in urlList)
			{
				try
				{
					await DownloadSingleAsync(url, localPath, useBrowserUserAgent, customUserAgent, cancellationToken, enableParallelChunks, trackedFile).ConfigureAwait(continueOnCapturedContext: false);
					return;
				}
				catch (OperationCanceledException)
				{
					CleanupTempFiles(localPath);
					throw;
				}
				catch (Exception ex2)
				{
					lastException = ex2;
					CleanupTempFiles(localPath);
					ModBase.Log(ex2, "[Download] 下载源失败：" + url);
				}
			}
			if (retry >= 3)
			{
				break;
			}
			ModBase.Log(lastException, $"[Download] 重试 {retry + 1}/{3}：{localPath}");
			await Task.Delay(RandomUtils.NextInt(300, 500 + retry * 300), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		throw new IOException("下载失败：" + localPath, lastException);
	}

	private static async Task DownloadSingleAsync(string url, string localPath, bool useBrowserUserAgent, string customUserAgent, CancellationToken cancellationToken, bool enableParallelChunks, DownloadFile? trackedFile)
	{
		ModBase.Log("[Download] 开始下载：" + url + " -> " + localPath);
		CleanupTempFiles(localPath);
		ModBase.FileChecker checker = trackedFile?.Check;
		long num = checker?.actualSize ?? (-1);
		DownloadRequestKind sequentialRequestKind = (((num < 0 || num >= 4194304) && (num >= 0 || enableParallelChunks)) ? DownloadRequestKind.LargeOrUnknown : DownloadRequestKind.SmallOrBatch);
		bool flag = enableParallelChunks;
		if (flag)
		{
			flag = await AdaptiveRangeDownloader.TryDownloadAsync(url, localPath, useBrowserUserAgent, customUserAgent, cancellationToken, trackedFile, num).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (flag)
		{
			await ValidateTempFileAsync(localPath, checker, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			PromoteTempFile(localPath);
			if (!File.Exists(localPath))
			{
				throw new IOException("分段下载未产生任何文件：" + localPath);
			}
			MarkDownloadCompleted(trackedFile);
			ModBase.Log("[Download] 分段下载成功：" + localPath);
		}
		else
		{
			await DownloadSequentiallyAsync(url, localPath, useBrowserUserAgent, customUserAgent, cancellationToken, trackedFile, sequentialRequestKind).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private static async Task DownloadSequentiallyAsync(string url, string localPath, bool useBrowserUserAgent, string customUserAgent, CancellationToken cancellationToken, DownloadFile? trackedFile, DownloadRequestKind requestKind)
	{
		using DownloadConnectionLease connection = await DownloadResourceManager.AcquireConnectionAsync(url, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		using HttpRequestMessage request = CreateDownloadRequest(url, useBrowserUserAgent, customUserAgent, requestKind);
		using HttpResponseMessage response = await SendDownloadRequestAsync(url, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException($"下载请求失败：{(int)response.StatusCode} {response.ReasonPhrase}");
		}
		long responseContentLength = response.Content.Headers.ContentLength ?? (-1);
		ModBase.FileChecker checker = trackedFile?.Check;
		long num = checker?.actualSize ?? (-1);
		if (string.IsNullOrEmpty(checker?.hash) && responseContentLength >= 0 && num >= 0 && responseContentLength != num)
		{
			throw new IOException($"下载大小与清单不一致：响应为 {responseContentLength}，清单为 {num}");
		}
		long totalSize = ((responseContentLength >= 0) ? responseContentLength : num);
		if (trackedFile != null)
		{
			trackedFile.State = NetState.Downloading;
			trackedFile.TotalSize = totalSize;
			trackedFile.IsUnknownSize = totalSize <= 0;
			trackedFile.DownloadedBytes = 0L;
			trackedFile.Speed = 0L;
			trackedFile.ActiveThreads = 1;
		}
		long downloaded = 0L;
		long lastProgressBytes = 0L;
		long lastProgressTick = Stopwatch.GetTimestamp();
		await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		using CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		await using FileStream output = new FileStream(localPath + ".PCLDownloading", FileMode.Create, FileAccess.Write, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
		using DownloadBufferLease bufferLease = await DownloadResourceManager.RentBufferAsync(65536, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		byte[] buffer = bufferLease.Buffer;
		while (true)
		{
			readTimeout.CancelAfter(30000);
			int read;
			try
			{
				read = await input.ReadAsync(buffer.AsMemory(0, 65536), readTimeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && readTimeout.IsCancellationRequested)
			{
				throw new TimeoutException("下载超时（" + url + "）");
			}
			finally
			{
				readTimeout.CancelAfter(Timeout.InfiniteTimeSpan);
			}
			if (read == 0)
			{
				break;
			}
			await DownloadResourceManager.ThrottleAsync(read, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			DownloadResourceManager.RecordDownloadedBytes(read);
			downloaded += read;
			UpdateSequentialProgress(trackedFile, downloaded, totalSize, ref lastProgressBytes, ref lastProgressTick);
		}
		connection.Dispose();
		await output.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (responseContentLength >= 0 && downloaded != responseContentLength)
		{
			throw new IOException($"下载不完整：已写入 {downloaded}，应为响应声明的 {responseContentLength}");
		}
		await ValidateTempFileAsync(localPath, checker, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (trackedFile != null && totalSize <= 0)
		{
			trackedFile.TotalSize = downloaded;
			trackedFile.IsUnknownSize = false;
			trackedFile.DownloadedBytes = downloaded;
		}
		PromoteTempFile(localPath);
		if (!File.Exists(localPath))
		{
			throw new IOException("下载未产生任何文件：" + localPath);
		}
		MarkDownloadCompleted(trackedFile);
		ModBase.Log("[Download] 顺序下载成功：" + localPath);
	}

	private static async Task ValidateTempFileAsync(string localPath, ModBase.FileChecker? checker, CancellationToken cancellationToken)
	{
		if (checker != null)
		{
			string tempPath = localPath + ".PCLDownloading";
			string text = ((!string.IsNullOrEmpty(checker.hash)) ? (await Task.Run(() => checker.Check(tempPath), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) : checker.Check(tempPath));
			string text2 = text;
			if (text2 != null)
			{
				throw new IOException("下载文件校验失败：" + text2);
			}
		}
	}

	private static void UpdateSequentialProgress(DownloadFile? trackedFile, long downloaded, long totalSize, ref long lastProgressBytes, ref long lastProgressTick)
	{
		if (trackedFile != null)
		{
			long timestamp = Stopwatch.GetTimestamp();
			if (timestamp - lastProgressTick >= Stopwatch.Frequency / 5 || (totalSize > 0 && downloaded >= totalSize))
			{
				long num = Math.Max(1L, timestamp - lastProgressTick);
				trackedFile.DownloadedBytes = downloaded;
				trackedFile.Speed = Math.Max(0L, (long)((double)(downloaded - lastProgressBytes) * (double)Stopwatch.Frequency / (double)num));
				trackedFile.ActiveThreads = 1;
				lastProgressBytes = downloaded;
				lastProgressTick = timestamp;
			}
		}
	}

	internal static HttpRequestMessage CreateDownloadRequest(string url, bool useBrowserUserAgent, string customUserAgent, DownloadRequestKind requestKind)
	{
		HttpRequestMessage client = new HttpRequestMessage(HttpMethod.Get, url);
		RequestSigning.SecretHeadersSign(url, ref client, useBrowserUserAgent, customUserAgent);
		ApplyHttpVersion(client, requestKind);
		return client;
	}

	internal static async Task<HttpResponseMessage> SendDownloadRequestAsync(string url, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		using CancellationTokenSource requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		requestTimeout.CancelAfter(30000);
		try
		{
			return await GetHttpClient(url).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestTimeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException innerException) when (!cancellationToken.IsCancellationRequested && requestTimeout.IsCancellationRequested)
		{
			throw new DownloadRequestTimeoutException("等待下载源响应超时（30 秒）：" + url, innerException);
		}
	}

	private static void ApplyHttpVersion(HttpRequestMessage request, DownloadRequestKind requestKind)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		DownloadHttpMode httpMode = Config.Download.HttpMode;
		if ((int)httpMode != 1)
		{
			if ((int)httpMode == 2)
			{
				request.Version = HttpVersion.Version20;
				request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
			}
			else if (requestKind == DownloadRequestKind.SmallOrBatch)
			{
				request.Version = HttpVersion.Version20;
				request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
			}
			else
			{
				request.Version = HttpVersion.Version11;
				request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
			}
		}
		else
		{
			request.Version = HttpVersion.Version11;
			request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
		}
	}

	private static void MarkDownloadCompleted(DownloadFile? trackedFile)
	{
		if (trackedFile != null)
		{
			trackedFile.Speed = 0L;
			trackedFile.ActiveThreads = 0;
			trackedFile.DownloadedBytes = Math.Max(trackedFile.DownloadedBytes, trackedFile.TotalSize);
		}
	}

	private static void PromoteTempFile(string localPath)
	{
		string text = localPath + ".PCLDownloading";
		if (File.Exists(localPath) || !File.Exists(text))
		{
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			try
			{
				File.Move(text, localPath, overwrite: true);
				break;
			}
			catch (IOException) when (i < 4)
			{
				Thread.Sleep(100);
			}
		}
	}

	private static void CleanupTempFiles(string localPath)
	{
		string path = localPath + ".PCLDownloading";
		TryDeleteFile(localPath);
		TryDeleteFile(path);
	}

	private static void TryDeleteFile(string path)
	{
		for (int i = 0; i < 5; i++)
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				break;
			}
			catch (IOException)
			{
				Thread.Sleep(100);
			}
		}
	}

	internal static HttpClient GetHttpClient(string url)
	{
		bool flag = Uri.TryCreate(url, UriKind.Absolute, out Uri result);
		if (flag)
		{
			bool flag2;
			switch (result.Host)
			{
			case "edge.forgecdn.net":
			case "mediafilez.forgecdn.net":
			case "forgecdn.net":
			case "api.curseforge.com":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag)
		{
			return NetworkService.GetClient("curseforge_api");
		}
		return NetworkService.GetClient("default");
	}
}
