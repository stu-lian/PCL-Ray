using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;

namespace PCL.Core.UI.Animation;

public sealed class DoubleFromToAnimation : FromToAnimationBase<double>
{
	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		double num = Easing.Ease(CurrentFrame, TotalFrames);
		CurrentValue = ((ValueType == AnimationValueType.Relative) ? (From + To * num) : (From + (To - From) * num));
		return base.ComputeNextFrame(target);
	}
}
