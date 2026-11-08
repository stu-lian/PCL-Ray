using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.UI.Animation.Easings;

public class CompositeEasing : Easing
{
	private readonly List<(IEasing easing, TimeSpan duration, TimeSpan delay, double weight)> _easings;

	private readonly TimeSpan _totalDuration;

	public TimeSpan TotalDuration => _totalDuration;

	public CompositeEasing(params (IEasing easing, TimeSpan duration)[] easings)
		: this(easings.Select(((IEasing easing, TimeSpan duration) e) => (easing: e.easing, duration: e.duration, Zero: TimeSpan.Zero, 1.0 / (double)((easings.Length == 0) ? 1 : easings.Length))).ToArray())
	{
	}

	public CompositeEasing(params (IEasing easing, TimeSpan duration, TimeSpan delay)[] easings)
		: this(easings.Select(((IEasing easing, TimeSpan duration, TimeSpan delay) e) => (easing: e.easing, duration: e.duration, delay: e.delay, 1.0 / (double)((easings.Length == 0) ? 1 : easings.Length))).ToArray())
	{
	}

	public CompositeEasing(params (IEasing easing, TimeSpan duration, double weight)[] easings)
		: this(easings.Select(((IEasing easing, TimeSpan duration, double weight) e) => (easing: e.easing, duration: e.duration, Zero: TimeSpan.Zero, weight: e.weight)).ToArray())
	{
	}

	public CompositeEasing(params (IEasing easing, TimeSpan duration, TimeSpan delay, double weight)[] easings)
	{
		if (easings == null || easings.Length == 0)
		{
			throw new ArgumentException("至少需要一个缓动", "easings");
		}
		_easings = new List<(IEasing, TimeSpan, TimeSpan, double)>(easings.Length);
		long num = 0L;
		for (int i = 0; i < easings.Length; i++)
		{
			var (easing, timeSpan, timeSpan2, item) = easings[i];
			if (easing == null)
			{
				throw new ArgumentNullException("easing");
			}
			if (timeSpan <= TimeSpan.Zero)
			{
				throw new ArgumentException("duration 必须大于 zero");
			}
			_easings.Add((easing, timeSpan, timeSpan2, item));
			long ticks = (timeSpan2 + timeSpan).Ticks;
			if (ticks > num)
			{
				num = ticks;
			}
		}
		_totalDuration = TimeSpan.FromTicks(num);
	}

	protected override double EaseCore(double progress)
	{
		TimeSpan timeSpan = _totalDuration * progress;
		double num = 0.0;
		foreach (var (easing, timeSpan2, timeSpan3, num2) in _easings)
		{
			if (num2 != 0.0)
			{
				TimeSpan timeSpan4 = timeSpan - timeSpan3;
				double num3;
				if (timeSpan4 <= TimeSpan.Zero)
				{
					num3 = 0.0;
				}
				else if (timeSpan4 >= timeSpan2)
				{
					num3 = easing.Ease(1.0);
				}
				else
				{
					double progress2 = timeSpan4.TotalSeconds / timeSpan2.TotalSeconds;
					num3 = easing.Ease(progress2);
				}
				num += num3 * num2;
			}
		}
		return num;
	}
}
