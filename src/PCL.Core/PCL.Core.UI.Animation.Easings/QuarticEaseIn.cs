namespace PCL.Core.UI.Animation.Easings;

public class QuarticEaseIn : Easing
{
	public static QuarticEaseIn Shared { get; } = new QuarticEaseIn();

	protected override double EaseCore(double progress)
	{
		double num = progress * progress;
		return num * num;
	}
}
