using System;

namespace PCL.Core.UI.Animation.Easings;

public class BackEaseOut : Easing
{
	public static BackEaseOut Shared { get; } = new BackEaseOut();

	protected override double EaseCore(double progress)
	{
		double num = 1.0 - progress;
		return 1.0 - num * (num * num - Math.Sin(num * Math.PI));
	}
}
