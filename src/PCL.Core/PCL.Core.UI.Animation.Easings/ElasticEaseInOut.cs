using System;

namespace PCL.Core.UI.Animation.Easings;

public class ElasticEaseInOut : Easing
{
	public static ElasticEaseInOut Shared { get; } = new ElasticEaseInOut();

	protected override double EaseCore(double progress)
	{
		if (progress < 0.5)
		{
			double num = progress * 2.0;
			return 0.5 * Math.Sin(20.420352248333657 * num) * Math.Exp(6.931471805599453 * (num - 1.0));
		}
		double num2 = progress * 2.0 - 1.0;
		return 0.5 * (Math.Sin(-20.420352248333657 * (num2 + 1.0)) * Math.Exp(-6.931471805599453 * num2) + 2.0);
	}
}
