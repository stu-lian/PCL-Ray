using System;

namespace PCL.Core.UI.Animation.Easings;

public class BackEaseWithPowerInOut(EasePower power = EasePower.Middle) : Easing
{
	private readonly double _p = 3.0 - (double)power * 0.5;

	protected override double EaseCore(double progress)
	{
		double num = Math.Clamp(progress, 0.0, 1.0);
		if (num < 0.5)
		{
			double num2 = 2.0 * num;
			return 0.5 * (Math.Pow(num2, _p) * Math.Cos(4.71238898038469 * (1.0 - num2)));
		}
		double num3 = 2.0 * (num - 0.5);
		double x = 1.0 - num3;
		return 0.5 * (1.0 - Math.Pow(x, _p) * Math.Cos(4.71238898038469 * num3)) + 0.5;
	}
}
