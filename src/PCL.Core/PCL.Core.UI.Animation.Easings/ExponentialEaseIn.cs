using System;

namespace PCL.Core.UI.Animation.Easings;

public class ExponentialEaseIn : Easing
{
	public static ExponentialEaseIn Shared { get; } = new ExponentialEaseIn();

	protected override double EaseCore(double progress)
	{
		if (progress != 0.0)
		{
			return Math.Pow(2.0, 10.0 * (progress - 1.0));
		}
		return progress;
	}
}
