using PCL.Core.IO.Storage.Cache.Model;

namespace PCL.Core.IO.Storage.Cache;

public record EvictionCandidate(string? CacheKey, EntryType EntryType, string? FileHash, long DataSize, int Priority, long HitCount);
