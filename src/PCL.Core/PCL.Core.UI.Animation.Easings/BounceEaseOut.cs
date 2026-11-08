using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Easings;

public class BounceEaseOut : Easing
{
	public static BounceEaseOut Shared { get; } = new BounceEaseOut();

	protected override double EaseCore(double progress)
	{
		return EaseUtils.Bounce(progress);
	}
}
