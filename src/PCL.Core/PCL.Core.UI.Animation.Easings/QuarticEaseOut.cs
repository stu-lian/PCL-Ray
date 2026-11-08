namespace PCL.Core.UI.Animation.Easings;

public class QuarticEaseOut : Easing
{
	public static QuarticEaseOut Shared { get; } = new QuarticEaseOut();

	protected override double EaseCore(double progress)
	{
		double num = progress - 1.0;
		double num2 = num * num;
		return (0.0 - num2) * num2 + 1.0;
	}
}
