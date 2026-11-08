using System;

namespace PCL.Core.UI.Animation.Easings;

public class CircularEaseOut : Easing
{
	public static CircularEaseOut Shared { get; } = new CircularEaseOut();

	protected override double EaseCore(double progress)
	{
		return Math.Sqrt((2.0 - progress) * progress);
	}
}
