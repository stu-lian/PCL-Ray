using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public class ActionAnimation : AnimationBase
{
	private CancellationTokenSource? _cts = new CancellationTokenSource();

	private TaskCompletionSource? _tcs;

	private int _called;

	public Action<CancellationToken> Action { get; set; }

	public override int CurrentFrame { get; set; }

	public TimeSpan Delay { get; set; }

	public ActionAnimation()
	{
	}

	public ActionAnimation(Action action)
	{
		Action = (CancellationToken _) =>
		{
			action();
		};
	}

	public ActionAnimation(Action<CancellationToken> action)
	{
		Action = action;
	}

	public override async Task<IAnimation> RunAsync(IAnimatable target)
	{
		ArgumentNullException.ThrowIfNull(Action, "Action");
		_tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_cts = new CancellationTokenSource();
		ActionAnimation clone = (ActionAnimation)this.MemberwiseClone();
		clone.Status = AnimationStatus.Running;
		await Task.Delay(Delay);
		Interlocked.Exchange(ref _called, 0);
		AnimationService.PushAnimationAsync(clone, target);
		return await _tcs.Task.ContinueWith((Func<Task, IAnimation>)((Task _) => clone));
	}

	public override IAnimation RunFireAndForget(IAnimatable target)
	{
		ArgumentNullException.ThrowIfNull(Action, "Action");
		_cts = new CancellationTokenSource();
		ActionAnimation clone = (ActionAnimation)this.MemberwiseClone();
		clone.Status = AnimationStatus.Running;
		Task.Run(async () =>
		{
			await Task.Delay(Delay);
			Interlocked.Exchange(ref _called, 0);
			AnimationService.PushAnimationFireAndForget(clone, target);
		});
		return clone;
	}

	public override void Cancel()
	{
		Status = AnimationStatus.Canceled;
		_cts?.Cancel();
		_tcs?.TrySetCanceled();
	}

	public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
	{
		AnimationStatus status = Status;
		if ((uint)(status - 2) <= 1u)
		{
			return null;
		}
		if (Interlocked.CompareExchange(ref _called, 1, 0) == 0)
		{
			return new ActionAnimationFrame(() =>
			{
				Action(_cts.Token);
				Status = AnimationStatus.Completed;
				_tcs?.TrySetResult();
			});
		}
		return null;
	}
}
