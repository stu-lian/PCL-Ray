using System;

namespace PCL;

public interface ILoadingTrigger
{
	public delegate void LoadingStateChangedEventHandler(MyLoading.MyLoadingState newState, MyLoading.MyLoadingState oldState);

	public delegate void ProgressChangedEventHandler(double newProgress, double oldProgress);

	bool IsLoader { get; }

	double Progress { get; }

	Exception? Error { get; }

	MyLoading.MyLoadingState LoadingState { get; set; }

	event LoadingStateChangedEventHandler? LoadingStateChanged;

	event ProgressChangedEventHandler? ProgressChanged;
}
