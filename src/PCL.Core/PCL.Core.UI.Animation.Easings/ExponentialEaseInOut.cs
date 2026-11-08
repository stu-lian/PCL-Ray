using System;

namespace PCL.Core.UI.Animation.Easings;

public class ExponentialEaseInOut : Easing
{
	public static ExponentialEaseInOut Shared { get; } = new ExponentialEaseInOut();

	protected override double EaseCore(double progress)
	{
		if (progress < 0.5)
		{
			return 0.5 * Math.Pow(2.0, 20.0 * progress - 10.0);
		}
		return -0.5 * Math.Pow(2.0, -20.0 * progress + 10.0) + 1.0;
	}
}
