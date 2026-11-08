namespace PCL.Core.UI.Animation.Easings;

public class CubicEaseOut : Easing
{
	public static CubicEaseOut Shared { get; } = new CubicEaseOut();

	protected override double EaseCore(double progress)
	{
		double num = progress - 1.0;
		return num * num * num + 1.0;
	}
}
