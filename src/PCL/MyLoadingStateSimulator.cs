using System;

namespace PCL;

public class MyLoadingStateSimulator : ILoadingTrigger
{
	private MyLoading.MyLoadingState _LoadingState { get; set; } = MyLoading.MyLoadingState.Unloaded;

	public MyLoading.MyLoadingState LoadingState
	{
		get
		{
			return _LoadingState;
		}
		set
		{
			if (_LoadingState != value)
			{
				MyLoading.MyLoadingState loadingState = _LoadingState;
				_LoadingState = value;
				LoadingStateChanged?.Invoke(value, loadingState);
			}
		}
	}

	public bool IsLoader { get; }

	public double Progress => 0.0;

	public Exception? Error => null;

	public event ILoadingTrigger.LoadingStateChangedEventHandler? LoadingStateChanged;

	public event ILoadingTrigger.ProgressChangedEventHandler? ProgressChanged;
}
