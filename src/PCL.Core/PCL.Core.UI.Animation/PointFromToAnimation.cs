using System.Windows;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation;

public class PointFromToAnimation : FromToAnimationBase<Point>
{
	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		double factor = Easing.Ease(CurrentFrame, TotalFrames);
		CurrentValue = ((ValueType == AnimationValueType.Relative) ? ValueProcessorManager.Add<Point>(From, ValueProcessorManager.Scale<Point>(To, factor)) : ValueProcessorManager.Add<Point>(From, ValueProcessorManager.Scale<Point>(ValueProcessorManager.Subtract<Point>(To, From), factor)));
		return base.ComputeNextFrame(target);
	}
}
