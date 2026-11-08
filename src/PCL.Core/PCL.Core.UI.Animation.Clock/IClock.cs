using System;

namespace PCL.Core.UI.Animation.Clock;

public interface IClock
{
	bool IsRunning { get; }

	int Fps { get; set; }

	event EventHandler<long>? Tick;

	void Start();

	void Stop();
}
