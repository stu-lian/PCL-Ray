namespace PCL.Core.IO.Download;

public record NDlSourceReport(int MaxSegmentCount = 1, int RetryCount = 0, long AverageSpeed = -1L);
