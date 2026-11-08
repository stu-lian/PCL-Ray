using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using PCL.Core.App.IoC;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Clock;
using PCL.Core.UI.Animation.UIAccessProvider;
using PCL.Core.UI.Animation.ValueProcessor;
using PCL.Core.Utils.Threading;

namespace PCL.Core.UI.Animation.Core;

[LifecycleService(LifecycleState.WindowCreating)]
public sealed class AnimationService : GeneralService
{
	private static LifecycleContext? _context;

	private static Channel<(IAnimation Animation, IAnimatable Target)> _animationChannel = null;

	private static Channel<(IAnimationFrame Frame, IAnimation Source)> _frameChannel = null;

	private static ConcurrentDictionary<string, IAnimation> _namedAnimations = new ConcurrentDictionary<string, IAnimation>();

	private static IClock _clock = null;

	private static AsyncCountResetEvent _resetEvent = null;

	private static int _taskCount;

	private static CancellationTokenSource _cts = null;

	private static LifecycleContext Context => _context;

	public static int Fps { get; set; } = 60;

	public static double Scale { get; set; } = 0.1;

	public static IUIAccessProvider UIAccessProvider { get; private set; } = null;

	private AnimationService()
		: base("animation", "动画")
	{
		_context = ServiceContext;
	}

	public override void Start()
	{
		_Initialize();
	}

	public override void Stop()
	{
		_Uninitialize();
	}

	private static void _RegisterValueProcessors()
	{
		ValueProcessorManager.Register(new DoubleValueProcessor());
		ValueProcessorManager.Register(new MatrixValueProcessor());
		ValueProcessorManager.Register(new NColorValueProcessor());
		ValueProcessorManager.Register(new NRotateTransformValueProcessor());
		ValueProcessorManager.Register(new NScaleTransformValueProcessor());
		ValueProcessorManager.Register(new PointValueProcessor());
		ValueProcessorManager.Register(new ThicknessValueProcessor());
	}

	private static void _Initialize()
	{
		_animationChannel = Channel.CreateUnbounded<(IAnimation, IAnimatable)>();
		_frameChannel = Channel.CreateUnbounded<(IAnimationFrame, IAnimation)>();
		_taskCount = Environment.ProcessorCount;
		Context.Info($"以最多 {_taskCount} 个线程初始化动画计算 Task");
		_cts = new CancellationTokenSource();
		_resetEvent = new AsyncCountResetEvent();
		_RegisterValueProcessors();
		UIAccessProvider = new WpfUIAccessProvider(((DispatcherObject)Lifecycle.CurrentApplication).Dispatcher);
		UIAccessProvider.InvokeAsync(async () =>
		{
			if (!_cts.IsCancellationRequested)
			{
				while (await _frameChannel.Reader.WaitToReadAsync())
				{
					(IAnimationFrame, IAnimation) item;
					while (_frameChannel.Reader.TryRead(out item))
					{
						if (item.Item2.Status != AnimationStatus.Canceled)
						{
							item.Item1.GetAction()();
						}
					}
					await Task.Yield();
				}
			}
		});
		_clock = new WinMMClock(Fps);
		_clock.Tick += ClockOnTick;
		_clock.Start();
		for (int num = 0; num < _taskCount; num++)
		{
			Task.Run((Func<Task?>)_AnimationComputeTaskAsync);
		}
	}

	private static void _Uninitialize()
	{
		_cts.Cancel();
		_cts.Dispose();
		_clock.Tick -= ClockOnTick;
		_clock.Stop();
		_resetEvent.Dispose();
		_namedAnimations.Clear();
	}

	private static void ClockOnTick(object? sender, long e)
	{
		_resetEvent.Set(_taskCount);
	}

	private static async Task _AnimationComputeTaskAsync()
	{
		List<(IAnimation Animation, IAnimatable Target)> animationList = new List<(IAnimation, IAnimatable)>(8);
		while (!_cts.IsCancellationRequested)
		{
			(IAnimation, IAnimatable) item;
			while (_animationChannel.Reader.TryRead(out item))
			{
				animationList.Add(item);
			}
			if (animationList.Count == 0)
			{
				await _resetEvent.WaitAsync();
				continue;
			}
			for (int num = animationList.Count - 1; num >= 0; num--)
			{
				(IAnimation, IAnimatable) tuple = animationList[num];
				AnimationStatus status = tuple.Item1.Status;
				if ((uint)(status - 2) <= 1u)
				{
					tuple.Item1.RaiseCompleted();
					if (!string.IsNullOrEmpty(tuple.Item1.Name))
					{
						((ICollection<KeyValuePair<string, IAnimation>>)_namedAnimations).Remove(new KeyValuePair<string, IAnimation>(tuple.Item1.Name, tuple.Item1));
					}
					animationList.RemoveAt(num);
				}
				else
				{
					IAnimationFrame animationFrame = tuple.Item1.ComputeNextFrame(tuple.Item2);
					if (animationFrame != null)
					{
						_frameChannel.Writer.TryWrite((animationFrame, tuple.Item1));
						tuple.Item1.CurrentFrame++;
					}
				}
			}
			await _resetEvent.WaitAsync();
		}
	}

	private static void _HandleNamedAnimationConflict(IAnimation animation)
	{
		if (!string.IsNullOrEmpty(animation.Name))
		{
			_namedAnimations.AddOrUpdate(animation.Name, animation, (string _, IAnimation existingAnimation) =>
			{
				existingAnimation.Cancel();
				return animation;
			});
		}
	}

	internal static Task PushAnimationAsync(IAnimation animation, IAnimatable target)
	{
		_HandleNamedAnimationConflict(animation);
		TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		animation.Completed += (object? _, EventArgs _) =>
		{
			tcs.SetResult();
		};
		_animationChannel.Writer.TryWrite((animation, target));
		return tcs.Task;
	}

	internal static void PushAnimationFireAndForget(IAnimation animation, IAnimatable target)
	{
		_HandleNamedAnimationConflict(animation);
		_animationChannel.Writer.TryWrite((animation, target));
	}

	public static void CancelAnimationByName(string name)
	{
		if (_namedAnimations.TryRemove(name, out IAnimation value))
		{
			value.Cancel();
		}
	}
}
