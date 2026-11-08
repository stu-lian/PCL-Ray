using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public sealed class ParallelAnimationGroup : AnimationGroup
{
	private TaskCompletionSource? _cancelTcs;

	public override async Task<IAnimation> RunAsync(IAnimatable target)
	{
		Status = AnimationStatus.Running;
		AnimationService.PushAnimationFireAndForget(this, target);
		_cancelTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		List<IAnimation> list = Children.ToList();
		List<Task> list2 = new List<Task>();
		lock (ChildrenCore)
		{
			if (Status == AnimationStatus.Canceled)
			{
				return this;
			}
			foreach (IAnimation item in list)
			{
				IAnimatable target2 = AnimationGroup.ResolveTarget(item, target);
				IAnimation animation = item.RunFireAndForget(target2);
				ChildrenCore.Add(animation);
				list2.Add(AnimationGroup.CreateChildAwaiter(animation));
			}
		}
		try
		{
			await Task.WhenAny(Task.WhenAll(list2), _cancelTcs.Task);
		}
		finally
		{
			if (Status != AnimationStatus.Canceled)
			{
				Status = AnimationStatus.Completed;
			}
			_cancelTcs = null;
		}
		return this;
	}

	public override void Cancel()
	{
		base.Cancel();
		_cancelTcs?.TrySetResult();
	}

	public override IAnimation RunFireAndForget(IAnimatable target)
	{
		RunAsync(target);
		return this;
	}
}
