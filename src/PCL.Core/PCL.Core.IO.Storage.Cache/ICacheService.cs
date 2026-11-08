using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.IO.Storage.Cache;

public interface ICacheService
{
	Task SetAsync<T>(string key, T value, CachePolicy? policy = null, CancellationToken ct = default(CancellationToken));

	Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken ct = default(CancellationToken));

	Task<bool> ExistsAsync(string key);

	Task<bool> DeleteAsync(string key);

	Task<string?> GetCachedFilePathAsync(string key);

	Task<string> CacheFileAsync(string key, Stream source, CachePolicy? policy = null, CancellationToken ct = default(CancellationToken));

	Task<int> DeleteByGroupAsync(string groupName);

	Task<int> DeleteByTagAsync(string tag);

	Task<int> DeleteExpiredAsync();

	Task<CacheStats> GetStatsAsync();

	Task ClearAsync();

	Task CompactAsync();
}
