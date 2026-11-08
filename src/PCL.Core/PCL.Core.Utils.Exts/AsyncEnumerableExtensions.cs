using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Exts;

public static class AsyncEnumerableExtensions
{
	extension<T>(IEnumerable<T> source)
	{
		public async Task ForEachAsync(Func<T, Task> action, int maxDegreeOfParallelism = 10)
		{
			ArgumentNullException.ThrowIfNull(source, "source");
			ArgumentNullException.ThrowIfNull(action, "action");
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism, "maxDegreeOfParallelism");
			SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
			await Task.WhenAll(source.Select((Func<T, Task>)(async (T item) =>
			{
				await semaphore.WaitAsync();
				try
				{
					await action(item);
				}
				finally
				{
					semaphore.Release();
				}
			})));
		}

		public async Task ForEachAsync(Func<T, CancellationToken, Task> action, int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default(CancellationToken))
		{
			ArgumentNullException.ThrowIfNull(source, "source");
			ArgumentNullException.ThrowIfNull(action, "action");
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism, "maxDegreeOfParallelism");
			SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
			await Task.WhenAll(source.Select((Func<T, Task>)(async (T item) =>
			{
				await semaphore.WaitAsync(cancellationToken);
				try
				{
					await action(item, cancellationToken);
				}
				finally
				{
					semaphore.Release();
				}
			})));
		}

		public async Task<IEnumerable<TResult>> SelectAsync<TResult>(Func<T, Task<TResult>> selector, int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default(CancellationToken))
		{
			ArgumentNullException.ThrowIfNull(source, "source");
			ArgumentNullException.ThrowIfNull(selector, "selector");
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism, "maxDegreeOfParallelism");
			SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
			return await Task.WhenAll(source.Select(async (T item) =>
			{
				await semaphore.WaitAsync(cancellationToken);
				try
				{
					return await selector(item);
				}
				finally
				{
					semaphore.Release();
				}
			}));
		}
	}
}
