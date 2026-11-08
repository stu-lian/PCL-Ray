using System;

namespace PCL.Core.UI.Animation.Easings;

public class SineEaseOut : Easing
{
	public static SineEaseOut Shared { get; } = new SineEaseOut();

	protected override double EaseCore(double progress)
	{
		return Math.Sin(progress * Math.PI / 2.0);
	}
}
