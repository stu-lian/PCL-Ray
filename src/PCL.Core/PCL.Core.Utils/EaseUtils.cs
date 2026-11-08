namespace PCL.Core.Utils;

internal static class EaseUtils
{
	private const double BounceN1 = 7.5625;

	private const double BounceInvD1 = 0.3636363636363636;

	private const double BounceThreshold2 = 744.0 / 1023.0;

	private const double BounceThreshold3 = 930.0 / 1023.0;

	private const double BounceOffset1 = 558.0 / 1023.0;

	private const double BounceOffset2 = 837.0 / 1023.0;

	private const double BounceOffset3 = 21.0 / 22.0;

	internal const double ElasticLn2Times10 = 6.931471805599453;

	internal const double ElasticPiTimes6Point5 = 20.420352248333657;

	internal static double Bounce(double progress)
	{
		if (!(progress < 0.3636363636363636))
		{
			if (!(progress < 744.0 / 1023.0))
			{
				if (progress < 930.0 / 1023.0)
				{
					progress -= 837.0 / 1023.0;
					return 7.5625 * progress * progress + 0.9375;
				}
				progress -= 21.0 / 22.0;
				return 7.5625 * progress * progress + 63.0 / 64.0;
			}
			progress -= 558.0 / 1023.0;
			return 7.5625 * progress * progress + 0.75;
		}
		return 7.5625 * progress * progress;
	}
}
