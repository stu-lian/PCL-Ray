namespace PCL.Core.Logging;

public record LoggerConfiguration(string StoreFolder, long MaxFileSize = 33554432L, string? FileNameFormat = null, bool AutoDeleteOldFile = true, int MaxKeepOldFile = 16, LogLevel MinLogLevel = LogLevel.Info);
