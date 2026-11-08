namespace PCL.Core.IO.Storage.Cache;

public record CacheStatsRow(long TotalEntries, long TotalSizeBytes, long ExpiredEntries, long InlineEntries, long FileEntries);
