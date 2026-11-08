using System;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public interface IAnimation
{
	string Name { get; set; }

	AnimationStatus Status { get; }

	int CurrentFrame { get; set; }

	event EventHandler Started;

	event EventHandler Completed;

	Task<IAnimation> RunAsync(IAnimatable target);

	IAnimation RunFireAndForget(IAnimatable target);

	void Cancel();

	IAnimationFrame? ComputeNextFrame(IAnimatable target);

	void RaiseStarted();

	void RaiseCompleted();
}
