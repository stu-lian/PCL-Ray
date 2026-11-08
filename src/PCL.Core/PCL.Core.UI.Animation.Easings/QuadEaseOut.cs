namespace PCL.Core.UI.Animation.Easings;

public class QuadEaseOut : Easing
{
	public static QuadEaseOut Shared { get; } = new QuadEaseOut();

	protected override double EaseCore(double progress)
	{
		return 1.0 - (1.0 - progress) * (1.0 - progress);
	}
}
