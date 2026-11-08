namespace PCL.Network;

public enum NetState
{
	WaitingToCheck = -1,
	WaitingToDownload,
	Connecting,
	Reading,
	Downloading,
	Merging,
	Finished,
	Interrupted
}
