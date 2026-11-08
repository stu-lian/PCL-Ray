using System;

namespace PCL.Core.UI.Animation.Easings;

public class SineEaseInOut : Easing
{
	public static SineEaseInOut Shared { get; } = new SineEaseInOut();

	protected override double EaseCore(double progress)
	{
		return (0.0 - (Math.Cos(Math.PI * progress) - 1.0)) / 2.0;
	}
}
