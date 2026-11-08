namespace PCL.Core.UI.Animation.Easings;

public class QuinticEaseIn : Easing
{
	public static QuinticEaseIn Shared { get; } = new QuinticEaseIn();

	protected override double EaseCore(double progress)
	{
		double num = progress * progress;
		return num * num * progress;
	}
}
