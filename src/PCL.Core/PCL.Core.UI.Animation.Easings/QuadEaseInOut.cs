namespace PCL.Core.UI.Animation.Easings;

public class QuadEaseInOut : Easing
{
	public static QuadEaseInOut Shared { get; } = new QuadEaseInOut();

	protected override double EaseCore(double progress)
	{
		if (progress < 0.5)
		{
			return 2.0 * progress * progress;
		}
		return progress * (4.0 - 2.0 * progress) - 1.0;
	}
}
