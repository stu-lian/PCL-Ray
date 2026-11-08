using System;
using System.Threading.Tasks;

namespace PCL.Core.UI.Animation.UIAccessProvider;

public interface IUIAccessProvider
{
	event EventHandler FrameTick;

	bool CheckAccess();

	void Invoke(Action action);

	Task InvokeAsync(Action action);

	Task<T> InvokeAsync<T>(Func<T> func);

	Task InvokeAsync(Func<Task> func);

	Task<T> InvokeAsync<T>(Func<Task<T>> func);
}
