namespace PCL.Core.UI.Animation.Easings;

public class QuinticEaseOut : Easing
{
	public static QuinticEaseOut Shared { get; } = new QuinticEaseOut();

	protected override double EaseCore(double progress)
	{
		double num = progress - 1.0;
		double num2 = num * num;
		return num2 * num2 * num + 1.0;
	}
}
