using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public sealed class SequentialAnimationGroup : AnimationGroup
{
	private TaskCompletionSource? _cancelTcs;

	public override async Task<IAnimation> RunAsync(IAnimatable target)
	{
		Status = AnimationStatus.Running;
		AnimationService.PushAnimationFireAndForget(this, target);
		_cancelTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		List<IAnimation> list = Children.ToList();
		lock (ChildrenCore)
		{
			if (Status == AnimationStatus.Canceled)
			{
				return this;
			}
		}
		try
		{
			foreach (IAnimation item in list)
			{
				if (Status != AnimationStatus.Canceled)
				{
					IAnimatable target2 = AnimationGroup.ResolveTarget(item, target);
					Task task;
					lock (ChildrenCore)
					{
						IAnimation animation = item.RunFireAndForget(target2);
						ChildrenCore.Add(animation);
						task = AnimationGroup.CreateChildAwaiter(animation);
					}
					await Task.WhenAny(task, _cancelTcs.Task);
					if (Status == AnimationStatus.Canceled)
					{
						break;
					}
					continue;
				}
				break;
			}
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
