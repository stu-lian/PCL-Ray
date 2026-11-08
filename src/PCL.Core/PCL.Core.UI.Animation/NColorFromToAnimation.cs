using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;

namespace PCL.Core.UI.Animation;

public class NColorFromToAnimation : FromToAnimationBase<NColor>
{
	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		double num = Easing.Ease(CurrentFrame, TotalFrames);
		CurrentValue = ((ValueType == AnimationValueType.Relative) ? (From + To * (float)num) : (From + (To - From) * (float)num));
		return base.ComputeNextFrame(target);
	}
}
