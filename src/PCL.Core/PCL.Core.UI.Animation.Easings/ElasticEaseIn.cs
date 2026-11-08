using System;

namespace PCL.Core.UI.Animation.Easings;

public class ElasticEaseIn : Easing
{
	public static ElasticEaseIn Shared { get; } = new ElasticEaseIn();

	protected override double EaseCore(double progress)
	{
		return Math.Sin(20.420352248333657 * progress) * Math.Exp(6.931471805599453 * (progress - 1.0));
	}
}
