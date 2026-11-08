namespace PCL.Core.UI.Animation.Easings;

public class QuarticEaseInOut : Easing
{
	public static QuarticEaseInOut Shared { get; } = new QuarticEaseInOut();

	protected override double EaseCore(double progress)
	{
		if (progress < 0.5)
		{
			double num = progress * progress;
			return 8.0 * num * num;
		}
		double num2 = progress - 1.0;
		double num3 = num2 * num2;
		return -8.0 * num3 * num3 + 1.0;
	}
}
