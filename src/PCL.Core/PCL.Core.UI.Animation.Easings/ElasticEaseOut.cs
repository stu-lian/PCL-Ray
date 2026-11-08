using System;

namespace PCL.Core.UI.Animation.Easings;

public class ElasticEaseOut : Easing
{
	public static ElasticEaseOut Shared { get; } = new ElasticEaseOut();

	protected override double EaseCore(double progress)
	{
		return Math.Sin(-20.420352248333657 * (progress + 1.0)) * Math.Exp(-6.931471805599453 * progress) + 1.0;
	}
}
