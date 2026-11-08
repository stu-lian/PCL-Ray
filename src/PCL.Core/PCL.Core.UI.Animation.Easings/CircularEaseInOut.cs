using System;

namespace PCL.Core.UI.Animation.Easings;

public class CircularEaseInOut : Easing
{
	public static CircularEaseInOut Shared { get; } = new CircularEaseInOut();

	protected override double EaseCore(double progress)
	{
		if (progress < 0.5)
		{
			return 0.5 * (1.0 - Math.Sqrt(1.0 - 4.0 * progress * progress));
		}
		double num = 2.0 * progress;
		return 0.5 * (Math.Sqrt((3.0 - num) * (num - 1.0)) + 1.0);
	}
}
