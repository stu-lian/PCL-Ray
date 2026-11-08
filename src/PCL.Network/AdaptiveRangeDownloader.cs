using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace PCL.Network;

internal sealed class AdaptiveRangeDownloader
{
	private readonly record struct RangeProbe(long Size);

	private sealed class DownloadSegment(int id, long start, long end)
	{
		public int Id { get; } = id;

		public long Start { get; } = start;

		public long End { get; set; } = end;

		public long Downloaded { get; set; }

		public int FailureCount { get; set; }

		public long LastRateSampleTick { get; set; }

		public long Remaining => End - Start - Downloaded + 1;

		public long CurrentOffset => Start + Downloaded;
	}

	private sealed class DownloadSession
	{
		private readonly record struct RateSample(double BytesPerSecond, long UpdatedAt);

		private readonly AdaptiveRangeDownloader _owner;

		private readonly Channel<DownloadSegment> _segments;

		private readonly long _totalSize;

		private readonly SafeFileHandle _fileHandle;

		private readonly CancellationTokenSource _cancellation;

		private readonly ConcurrentDictionary<int, RateSample> _rates = new ConcurrentDictionary<int, RateSample>();

		private readonly ConcurrentQueue<double> _completedRates = new ConcurrentQueue<double>();

		private readonly object _progressLock = new object();

		private int _outstandingSegments;

		private int _nextSegmentId;

		private int _activeConnections;

		private long _downloadedBytes;

		private long _lastProgressBytes;

		private long _lastProgressTick = Stopwatch.GetTimestamp();

		private Exception? _failure;

		public long DownloadedBytes => Interlocked.Read(in _downloadedBytes);

		public DownloadSession(AdaptiveRangeDownloader owner, Channel<DownloadSegment> segments, long totalSize, int segmentCount, SafeFileHandle fileHandle, CancellationTokenSource cancellation)
		{
			_owner = owner;
			_segments = segments;
			_totalSize = totalSize;
			_outstandingSegments = segmentCount;
			_nextSegmentId = segmentCount;
			_fileHandle = fileHandle;
			_cancellation = cancellation;
		}

		public async Task RunWorkerAsync()
		{
			_ = 2;
			try
			{
				while (await _segments.Reader.WaitToReadAsync(_cancellation.Token).ConfigureAwait(continueOnCapturedContext: false))
				{
					DownloadSegment segment;
					while (_segments.Reader.TryRead(out segment))
					{
						try
						{
							await DownloadSegmentAsync(segment, _cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
							if (Interlocked.Decrement(ref _outstandingSegments) == 0)
							{
								_segments.Writer.TryComplete();
							}
						}
						catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
						{
							throw;
						}
						catch (Exception ex2)
						{
							if (TryRecover(segment, ex2))
							{
								ModBase.Log(ex2, "[Download] 分段下载缓慢或失败，正在重试：" + _owner._url);
								await Task.Delay(Random.Shared.Next(250, 1001), _cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
								continue;
							}
							Interlocked.CompareExchange(ref _failure, ex2, null);
							_segments.Writer.TryComplete(ex2);
							_cancellation.Cancel();
							throw;
						}
					}
				}
			}
			catch (OperationCanceledException) when (_failure != null)
			{
				throw _failure;
			}
		}

		private bool TryRecover(DownloadSegment segment, Exception exception)
		{
			if ((!(exception is SlowSegmentException) && !(exception is IOException) && !(exception is HttpRequestException) && !(exception is TaskCanceledException) && !(exception is DownloadRequestTimeoutException)) || 1 == 0)
			{
				return false;
			}
			if (++segment.FailureCount > 2 || segment.Remaining <= 0)
			{
				return false;
			}
			if (segment.Remaining >= 2097152 && Volatile.Read(in _nextSegmentId) < 2048)
			{
				long num = segment.CurrentOffset + segment.Remaining / 2;
				DownloadSegment item = new DownloadSegment(Interlocked.Increment(ref _nextSegmentId), num, segment.End);
				segment.End = num - 1;
				_segments.Writer.TryWrite(segment);
				_segments.Writer.TryWrite(item);
				Interlocked.Increment(ref _outstandingSegments);
			}
			else
			{
				_segments.Writer.TryWrite(segment);
			}
			return true;
		}

		private async Task DownloadSegmentAsync(DownloadSegment segment, CancellationToken cancellationToken)
		{
			using (await DownloadResourceManager.AcquireConnectionAsync(_owner._url, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
			{
				Interlocked.Increment(ref _activeConnections);
				long startedAt = Stopwatch.GetTimestamp();
				long attemptBytes = 0L;
				try
				{
					using HttpRequestMessage request = _owner.CreateRequest(HttpMethod.Get, DownloadRequestKind.RangeSegment);
					request.Headers.Range = new RangeHeaderValue(segment.CurrentOffset, segment.End);
					using HttpResponseMessage response = await FileDownloader.SendDownloadRequestAsync(_owner._url, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					ValidateRangeResponse(response, segment);
					await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					using CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
					using DownloadBufferLease bufferLease = await DownloadResourceManager.RentBufferAsync(65536, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					byte[] buffer = bufferLease.Buffer;
					while (segment.Remaining > 0)
					{
						readTimeout.CancelAfter(15000);
						int read;
						try
						{
							read = await input.ReadAsync(buffer.AsMemory(0, 65536), readTimeout.Token).ConfigureAwait(continueOnCapturedContext: false);
						}
						catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && readTimeout.IsCancellationRequested)
						{
							throw new SlowSegmentException("分段在等待数据时超时");
						}
						finally
						{
							readTimeout.CancelAfter(Timeout.InfiniteTimeSpan);
						}
						if (read == 0)
						{
							break;
						}
						read = (int)Math.Min(read, segment.Remaining);
						await DownloadResourceManager.ThrottleAsync(read, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						await RandomAccess.WriteAsync(_fileHandle, buffer.AsMemory(0, read), segment.CurrentOffset, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						DownloadResourceManager.RecordDownloadedBytes(read);
						segment.Downloaded += read;
						attemptBytes += read;
						long downloaded = Interlocked.Add(ref _downloadedBytes, read);
						ReportRateAndProgress(segment, attemptBytes, startedAt, downloaded);
					}
					if (segment.Remaining > 0)
					{
						throw new IOException($"分段 {segment.Id} 提前结束，还剩 {segment.Remaining} 字节");
					}
					AddCompletedRate(attemptBytes, startedAt);
				}
				finally
				{
					_rates.TryRemove(segment.Id, out var _);
					int activeConnections = Interlocked.Decrement(ref _activeConnections);
					ReportProgress(Interlocked.Read(in _downloadedBytes), activeConnections, force: true);
				}
			}
		}

		private void ReportRateAndProgress(DownloadSegment segment, long attemptBytes, long startedAt, long downloaded)
		{
			long timestamp = Stopwatch.GetTimestamp();
			double num = Math.Max(0.001, (double)(timestamp - startedAt) / (double)Stopwatch.Frequency);
			if (timestamp - segment.LastRateSampleTick >= Stopwatch.Frequency / 2)
			{
				segment.LastRateSampleTick = timestamp;
				_rates[segment.Id] = new RateSample((double)attemptBytes / num, timestamp);
				if (IsSignificantlySlow(segment, attemptBytes, startedAt, timestamp))
				{
					throw new SlowSegmentException("分段速度明显低于其他活跃连接");
				}
			}
			ReportProgress(downloaded, Volatile.Read(in _activeConnections));
		}

		private bool IsSignificantlySlow(DownloadSegment segment, long attemptBytes, long startedAt, long now)
		{
			if (ModNet.NetTaskSpeedLimitHigh > 0 || segment.Remaining < 1048576)
			{
				return false;
			}
			double num = (double)(now - startedAt) / (double)Stopwatch.Frequency;
			if (num < 8.0)
			{
				return false;
			}
			double[] array = (from pair in _rates
				where pair.Key != segment.Id && now - pair.Value.UpdatedAt <= Stopwatch.Frequency * 2 && pair.Value.BytesPerSecond > 0.0
				select pair.Value.BytesPerSecond).ToArray();
			if (array.Length == 0)
			{
				array = _completedRates.Where((double rate) => rate > 0.0).ToArray();
			}
			if (array.Length == 0)
			{
				return false;
			}
			Array.Sort(array);
			double num2 = array[array.Length / 2];
			double num3 = array[^1];
			double num4 = (double)attemptBytes / num;
			bool flag = num3 >= 131072.0 && num4 * 4.0 < num3;
			bool num5 = num2 >= 131072.0 && num4 * 4.0 < num2;
			bool flag2 = (num4 < 51200.0) & flag;
			return num5 | flag2;
		}

		private void AddCompletedRate(long attemptBytes, long startedAt)
		{
			double num = Math.Max(0.001, (double)(Stopwatch.GetTimestamp() - startedAt) / (double)Stopwatch.Frequency);
			_completedRates.Enqueue((double)attemptBytes / num);
			while (_completedRates.Count > 8)
			{
				_completedRates.TryDequeue(out var _);
			}
		}

		private void ReportProgress(long downloaded, int activeConnections, bool force = false)
		{
			lock (_progressLock)
			{
				long timestamp = Stopwatch.GetTimestamp();
				if (force || timestamp - _lastProgressTick >= Stopwatch.Frequency / 5)
				{
					long num = Math.Max(1L, timestamp - _lastProgressTick);
					long speed = Math.Max(0L, (long)((double)(downloaded - _lastProgressBytes) * (double)Stopwatch.Frequency / (double)num));
					_owner.NotifyProgress(downloaded, activeConnections, force);
					if (_owner._trackedFile != null)
					{
						_owner._trackedFile.Speed = speed;
					}
					_lastProgressBytes = downloaded;
					_lastProgressTick = timestamp;
				}
			}
		}

		private void ValidateRangeResponse(HttpResponseMessage response, DownloadSegment segment)
		{
			if (response.StatusCode != HttpStatusCode.PartialContent)
			{
				throw new RangeNotSupportedException($"服务器未按 Range 返回分段，状态码：{(int)response.StatusCode}");
			}
			ContentRangeHeaderValue contentRange = response.Content.Headers.ContentRange;
			if (contentRange?.Unit != "bytes" || contentRange.From != segment.CurrentOffset || contentRange.To != segment.End || contentRange.Length != _totalSize)
			{
				throw new RangeNotSupportedException("服务器返回的 Content-Range 与请求分段不一致");
			}
		}
	}

	private sealed class SlowSegmentException : IOException
	{
		public SlowSegmentException(string message)
			: base(message)
		{
		}
	}

	private sealed class RangeNotSupportedException : Exception
	{
		public RangeNotSupportedException(string message)
			: base(message)
		{
		}
	}

	internal const long SmallFileThreshold = 4194304L;

	private const long TargetSegmentSize = 8388608L;

	private const int MaxSegmentCount = 1024;

	private const int MaxExpandedSegmentCount = 2048;

	private const int BufferSize = 65536;

	private const int ReadTimeoutMilliseconds = 15000;

	private const int SlowCheckSeconds = 8;

	private const long SlowSpeedBytesPerSecond = 51200L;

	private const long ClearlyFastSegmentBytesPerSecond = 131072L;

	private const long SlowSplitThreshold = 2097152L;

	private const long MinimumRestartRemaining = 1048576L;

	private const int MaxSegmentRetries = 2;

	private readonly string _url;

	private readonly string _tempPath;

	private readonly bool _useBrowserUserAgent;

	private readonly string _customUserAgent;

	private readonly DownloadFile? _trackedFile;

	private AdaptiveRangeDownloader(string url, string localPath, bool useBrowserUserAgent, string customUserAgent, DownloadFile? trackedFile)
	{
		_url = url;
		_tempPath = localPath + ".PCLDownloading";
		_useBrowserUserAgent = useBrowserUserAgent;
		_customUserAgent = customUserAgent;
		_trackedFile = trackedFile;
	}

	public static async Task<bool> TryDownloadAsync(string url, string localPath, bool useBrowserUserAgent, string customUserAgent, CancellationToken cancellationToken, DownloadFile? trackedFile, long expectedSize = -1L)
	{
		if (expectedSize >= 0 && expectedSize < 4194304)
		{
			return false;
		}
		AdaptiveRangeDownloader downloader = new AdaptiveRangeDownloader(url, localPath, useBrowserUserAgent, customUserAgent, trackedFile);
		long num = expectedSize;
		if (num < 0)
		{
			RangeProbe? rangeProbe = await downloader.ProbeAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (!rangeProbe.HasValue || rangeProbe.Value.Size < 4194304)
			{
				return false;
			}
			num = rangeProbe.Value.Size;
		}
		try
		{
			await downloader.DownloadAsync(num, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}
		catch (RangeNotSupportedException ex)
		{
			ModBase.Log(ex, "[Download] 下载源不支持可靠的 Range，改用顺序下载：" + url);
			return false;
		}
	}

	private async Task<RangeProbe?> ProbeAsync(CancellationToken cancellationToken)
	{
		using (await DownloadResourceManager.AcquireConnectionAsync(_url, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			using HttpRequestMessage request = CreateRequest(HttpMethod.Get, DownloadRequestKind.RangeProbe);
			request.Headers.Range = new RangeHeaderValue(0L, 0L);
			using HttpResponseMessage httpResponseMessage = await FileDownloader.SendDownloadRequestAsync(_url, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (httpResponseMessage.StatusCode != HttpStatusCode.PartialContent)
			{
				return null;
			}
			ContentRangeHeaderValue contentRange = httpResponseMessage.Content.Headers.ContentRange;
			if (!(contentRange?.Unit != "bytes") && contentRange.From == 0 && contentRange.To == 0)
			{
				long? length = contentRange.Length;
				if (length.HasValue && length.GetValueOrDefault() > 0)
				{
					return new RangeProbe(contentRange.Length.Value);
				}
			}
			return null;
		}
	}

	private async Task DownloadAsync(long totalSize, CancellationToken cancellationToken)
	{
		NotifyStarted(totalSize);
		Channel<DownloadSegment> segments = Channel.CreateUnbounded<DownloadSegment>(new UnboundedChannelOptions
		{
			SingleReader = false,
			SingleWriter = false,
			AllowSynchronousContinuations = false
		});
		int num = EnqueueInitialSegments(segments.Writer, totalSize);
		int count = Math.Min(num, Math.Clamp(ModNet.NetTaskSingleFileConnectionLimit, 1, 8));
		using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		await using FileStream output = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.Read, 1, FileOptions.Asynchronous | FileOptions.RandomAccess);
		output.SetLength(totalSize);
		DownloadSession session = new DownloadSession(this, segments, totalSize, num, output.SafeFileHandle, linkedCancellation);
		Task[] tasks = (from _ in Enumerable.Range(0, count)
			select session.RunWorkerAsync()).ToArray();
		try
		{
			await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
			await output.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			segments.Writer.TryComplete();
		}
		if (session.DownloadedBytes != totalSize)
		{
			throw new IOException($"分段下载不完整：已写入 {session.DownloadedBytes}，应为 {totalSize}");
		}
		NotifyProgress(totalSize, 0, force: true);
	}

	private int EnqueueInitialSegments(ChannelWriter<DownloadSegment> writer, long totalSize)
	{
		int num = (int)Math.Clamp((totalSize + 8388608 - 1) / 8388608, 1L, 1024L);
		long num2 = (totalSize + num - 1) / num;
		int location = 0;
		for (long num3 = 0L; num3 < totalSize; num3 += num2)
		{
			long end = Math.Min(totalSize - 1, num3 + num2 - 1);
			writer.TryWrite(new DownloadSegment(Interlocked.Increment(ref location), num3, end));
		}
		return location;
	}

	private HttpRequestMessage CreateRequest(HttpMethod method, DownloadRequestKind requestKind)
	{
		HttpRequestMessage httpRequestMessage = FileDownloader.CreateDownloadRequest(_url, _useBrowserUserAgent, _customUserAgent, requestKind);
		httpRequestMessage.Method = method;
		httpRequestMessage.Headers.AcceptEncoding.Clear();
		httpRequestMessage.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));
		return httpRequestMessage;
	}

	private void NotifyStarted(long totalSize)
	{
		if (_trackedFile != null)
		{
			_trackedFile.State = NetState.Downloading;
			_trackedFile.TotalSize = totalSize;
			_trackedFile.IsUnknownSize = false;
			_trackedFile.DownloadedBytes = 0L;
			_trackedFile.Speed = 0L;
			_trackedFile.ActiveThreads = 0;
		}
	}

	private void NotifyProgress(long downloaded, int activeConnections, bool force = false)
	{
		if (_trackedFile != null)
		{
			_trackedFile.State = NetState.Downloading;
			_trackedFile.DownloadedBytes = downloaded;
			_trackedFile.ActiveThreads = activeConnections;
		}
	}
}
