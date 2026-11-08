using System;

namespace PCL.Core.UI.Animation.Easings;

public class SineEaseIn : Easing
{
	public static SineEaseIn Shared { get; } = new SineEaseIn();

	protected override double EaseCore(double progress)
	{
		return 1.0 - Math.Cos(progress * Math.PI / 2.0);
	}
}
