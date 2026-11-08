using System;
using System.Collections.Generic;
using PCL.Core.Logging;

namespace PCL.Core.App.Localization;

public static class WeakLanguageChanged
{
	private static readonly object _Lock = new object();

	private static readonly List<(WeakReference<object> Target, Action<object> Handler)> _Handlers = new List<(WeakReference<object>, Action<object>)>();

	private static bool _hooked;

	public static void Add<T>(T target, Action<T> handler) where T : class
	{
		if (target == null)
		{
			throw new ArgumentNullException("target");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		if (handler.Target != null)
		{
			throw new ArgumentException("handler 必须是不捕获实例的静态委托，请改用形如 static t => t.Foo() 的写法并通过 target 传入实例。", "handler");
		}
		lock (_Lock)
		{
			if (!_hooked)
			{
				LocalizationService.LanguageChanged += _OnLanguageChanged;
				_hooked = true;
			}
			_Handlers.Add((new WeakReference<object>(target), (object o) =>
			{
				handler((T)o);
			}));
		}
	}

	private static void _OnLanguageChanged()
	{
		(WeakReference<object>, Action<object>)[] array;
		lock (_Lock)
		{
			_Handlers.RemoveAll(((WeakReference<object> Target, Action<object> Handler) h) => !h.Target.TryGetTarget(out object _));
			array = _Handlers.ToArray();
		}
		(WeakReference<object>, Action<object>)[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			var (weakReference, action) = array2[num];
			if (weakReference.TryGetTarget(out var target))
			{
				try
				{
					action(target);
				}
				catch (Exception ex)
				{
					LogWrapper.Warn(ex, "WeakLanguageChanged", "语言变更处理器执行出错");
				}
			}
		}
	}
}
