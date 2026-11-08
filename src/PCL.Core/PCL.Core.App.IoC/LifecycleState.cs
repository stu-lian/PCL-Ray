namespace PCL.Core.App.IoC;

public enum LifecycleState
{
	Manual,
	BeforeLoading,
	Loading,
	Loaded,
	WindowCreating,
	WindowCreated,
	Running,
	Closing,
	Exiting
}
