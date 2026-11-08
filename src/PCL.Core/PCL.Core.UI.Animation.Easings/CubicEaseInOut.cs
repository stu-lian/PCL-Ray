namespace PCL.Core.UI.Animation.Easings;

public class CubicEaseInOut : Easing
{
	public static CubicEaseInOut Shared { get; } = new CubicEaseInOut();

	protected override double EaseCore(double progress)
	{
		if (progress < 0.5)
		{
			return 4.0 * progress * progress * progress;
		}
		double num = 2.0 * (progress - 1.0);
		return 0.5 * num * num * num + 1.0;
	}
}
