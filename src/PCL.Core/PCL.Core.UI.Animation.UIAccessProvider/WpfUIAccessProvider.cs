using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace PCL.Core.UI.Animation.UIAccessProvider;

public sealed class WpfUIAccessProvider(Dispatcher dispatcher) : IUIAccessProvider
{
	private readonly Dispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException("dispatcher");

	public event EventHandler FrameTick
	{
		add
		{
			Invoke(() =>
			{
				CompositionTarget.Rendering += value;
			});
		}
		remove
		{
			Invoke(() =>
			{
				CompositionTarget.Rendering -= value;
			});
		}
	}

	public bool CheckAccess()
	{
		return _dispatcher.CheckAccess();
	}

	public void Invoke(Action action)
	{
		if (_dispatcher.CheckAccess())
		{
			action();
		}
		else
		{
			_dispatcher.Invoke(action, (DispatcherPriority)10);
		}
	}

	public Task InvokeAsync(Action action)
	{
		if (!_dispatcher.CheckAccess())
		{
			return _dispatcher.InvokeAsync(action, (DispatcherPriority)10).Task;
		}
		action();
		return Task.CompletedTask;
	}

	public Task<T> InvokeAsync<T>(Func<T> func)
	{
		if (!_dispatcher.CheckAccess())
		{
			return _dispatcher.InvokeAsync<T>(func, (DispatcherPriority)10).Task;
		}
		return Task.FromResult(func());
	}

	public Task InvokeAsync(Func<Task> func)
	{
		if (!_dispatcher.CheckAccess())
		{
			return _dispatcher.InvokeAsync<Task>(func, (DispatcherPriority)10).Task.Unwrap();
		}
		return func();
	}

	public Task<T> InvokeAsync<T>(Func<Task<T>> func)
	{
		if (!_dispatcher.CheckAccess())
		{
			return _dispatcher.InvokeAsync<Task<T>>(func, (DispatcherPriority)10).Task.Unwrap();
		}
		return func();
	}
}
