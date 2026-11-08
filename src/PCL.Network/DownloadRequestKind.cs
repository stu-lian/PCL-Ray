namespace PCL.Network;

internal enum DownloadRequestKind
{
	SmallOrBatch,
	LargeOrUnknown,
	RangeProbe,
	RangeSegment
}
