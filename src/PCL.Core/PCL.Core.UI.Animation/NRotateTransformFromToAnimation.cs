using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation;

public class NRotateTransformFromToAnimation : FromToAnimationBase<NRotateTransform>
{
	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		double factor = Easing.Ease(CurrentFrame, TotalFrames);
		CurrentValue = ((ValueType == AnimationValueType.Relative) ? ValueProcessorManager.Add(From, ValueProcessorManager.Scale(To, factor)) : ValueProcessorManager.Add(From, ValueProcessorManager.Scale(ValueProcessorManager.Subtract(To, From), factor)));
		return base.ComputeNextFrame(target);
	}
}
