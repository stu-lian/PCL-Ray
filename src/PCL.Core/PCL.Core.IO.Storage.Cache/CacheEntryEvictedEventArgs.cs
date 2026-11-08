namespace PCL.Core.IO.Storage.Cache;

public record CacheEntryEvictedEventArgs(string Key, long DataSize, int Priority, long HitCount);
