using System;

namespace PCL.Core.UI.Animation.Easings;

public class ExponentialEaseOut : Easing
{
	public static ExponentialEaseOut Shared { get; } = new ExponentialEaseOut();

	protected override double EaseCore(double progress)
	{
		if (!(Math.Abs(progress - 1.0) < 0.0001))
		{
			return 1.0 - Math.Pow(2.0, -10.0 * progress);
		}
		return progress;
	}
}
