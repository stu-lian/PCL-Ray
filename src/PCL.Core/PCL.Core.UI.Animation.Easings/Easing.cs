namespace PCL.Core.UI.Animation.Easings;

public abstract class Easing : IEasing
{
	protected abstract double EaseCore(double progress);

	public double Ease(double progress)
	{
		if (!(progress <= 0.0))
		{
			if (progress >= 1.0)
			{
				return 1.0;
			}
			return EaseCore(progress);
		}
		return 0.0;
	}

	public double Ease(int currentFrame, int totalFrames)
	{
		if (totalFrames > 1)
		{
			return Ease((double)currentFrame / (double)(totalFrames - 1));
		}
		return 1.0;
	}
}
