using System;

namespace PCL.Core.UI.Animation.Easings;

public class CircularEaseIn : Easing
{
	public static CircularEaseIn Shared { get; } = new CircularEaseIn();

	protected override double EaseCore(double progress)
	{
		return 1.0 - Math.Sqrt(1.0 - progress * progress);
	}
}
