namespace PCL.Core.IO.Download;

public record NDlConnectionInfo(long Length, long BeginOffset, long EndOffset, bool IsSupportSegment);
