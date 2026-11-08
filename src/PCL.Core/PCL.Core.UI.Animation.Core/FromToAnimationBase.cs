using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Easings;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation.Core;

public class FromToAnimationBase<T> : AnimationBase, IFromToAnimation, IAnimation
{
	private int _currentFrame;

	private T? _startValue;

	public IEasing Easing { get; set; } = new LinearEasing();

	public T From { get; set; }

	public T? To { get; set; }

	public AnimationValueType ValueType { get; set; }

	public TimeSpan Duration { get; set; }

	public TimeSpan Delay { get; set; }

	public T? CurrentValue { get; internal set; }

	object? IFromToAnimation.CurrentValue
	{
		get
		{
			return CurrentValue;
		}
		set
		{
			if (!(value is T currentValue))
			{
				throw new InvalidCastException($"无法将 {value.GetType()} 转换为 {typeof(T)}");
			}
			CurrentValue = currentValue;
		}
	}

	public int TotalFrames { get; private set; }

	public override int CurrentFrame
	{
		get
		{
			return Interlocked.CompareExchange(ref _currentFrame, 0, 0);
		}
		set
		{
			Interlocked.Exchange(ref _currentFrame, value);
		}
	}

	public override async Task<IAnimation> RunAsync(IAnimatable target)
	{
		_RunCore(target);
		FromToAnimationBase<T> clone = (FromToAnimationBase<T>)this.MemberwiseClone();
		await Task.Delay(Delay);
		await AnimationService.PushAnimationAsync(clone, target);
		return clone;
	}

	public override IAnimation RunFireAndForget(IAnimatable target)
	{
		_RunCore(target);
		FromToAnimationBase<T> clone = (FromToAnimationBase<T>)this.MemberwiseClone();
		Task.Run(async () =>
		{
			await Task.Delay(Delay);
			AnimationService.PushAnimationFireAndForget(clone, target);
		});
		return clone;
	}

	private void _RunCore(IAnimatable target)
	{
		_currentFrame = 0;
		ArgumentNullException.ThrowIfNull(To, "To");
		_startValue = (T)target.GetValue();
		if (!ValueProcessorManager.Equal(_startValue, From))
		{
			From = ((ValueType == AnimationValueType.Relative) ? ValueProcessorManager.DefaultValue<T>() : _startValue);
		}
		TotalFrames = (int)Math.Round(Duration.TotalSeconds * (double)AnimationService.Fps / AnimationService.Scale);
		Status = AnimationStatus.Running;
	}

	public override void Cancel()
	{
		Interlocked.Exchange(ref _currentFrame, TotalFrames);
		Status = AnimationStatus.Canceled;
	}

	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		if (_currentFrame >= TotalFrames)
		{
			Status = AnimationStatus.Completed;
			return null;
		}
		return new FromToAnimationFrame<T>
		{
			Target = target,
			Value = ((ValueType == AnimationValueType.Relative) ? CurrentValue : ValueProcessorManager.Subtract(CurrentValue, From)),
			StartValue = ((ValueType == AnimationValueType.Relative) ? _startValue : From)
		};
	}
}
