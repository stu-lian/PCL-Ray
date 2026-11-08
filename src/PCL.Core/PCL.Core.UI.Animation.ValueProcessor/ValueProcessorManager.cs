using System;
using System.Collections.Generic;

namespace PCL.Core.UI.Animation.ValueProcessor;

public static class ValueProcessorManager
{
	private static class Cache<T>
	{
		public static IValueProcessor<T>? Processor;
	}

	private static readonly Dictionary<Type, Func<object, object>> _Filters = new Dictionary<Type, Func<object, object>>();

	private static readonly Dictionary<Type, Func<object, object, object>> _Adders = new Dictionary<Type, Func<object, object, object>>();

	private static readonly Dictionary<Type, Func<object, double, object>> _Scalers = new Dictionary<Type, Func<object, double, object>>();

	public static void Register<T>(IValueProcessor<T> processor)
	{
		ArgumentNullException.ThrowIfNull(processor, "processor");
		Cache<T>.Processor = processor;
		_Filters[typeof(T)] = (object o) => processor.Filter((T)o);
		_Adders[typeof(T)] = (object o1, object o2) => processor.Add((T)o1, (T)o2);
		_Scalers[typeof(T)] = (object o, double f) => processor.Scale((T)o, f);
	}

	public static T Filter<T>(T value)
	{
		IValueProcessor<T> processor = Cache<T>.Processor;
		if (processor != null)
		{
			return processor.Filter(value);
		}
		return value;
	}

	public static object Filter(object value)
	{
		Type type = value.GetType();
		if (!_Filters.TryGetValue(type, out Func<object, object> value2))
		{
			return value;
		}
		return value2(value);
	}

	public static T Add<T>(T value1, T value2)
	{
		return (Cache<T>.Processor ?? throw new InvalidOperationException($"类型未注册：{typeof(T)}")).Add(value1, value2);
	}

	public static object Add(object value1, object value2)
	{
		Type type = value1.GetType();
		if (type != value2.GetType())
		{
			throw new InvalidOperationException($"类型不一致：{type} vs {value2.GetType()}");
		}
		if (!_Adders.TryGetValue(type, out Func<object, object, object> value3))
		{
			return value2;
		}
		return value3(value1, value2);
	}

	public static T Subtract<T>(T value1, T value2)
	{
		return (Cache<T>.Processor ?? throw new InvalidOperationException($"类型未注册：{typeof(T)}")).Subtract(value1, value2);
	}

	public static T Scale<T>(T value, double factor)
	{
		return (Cache<T>.Processor ?? throw new InvalidOperationException($"类型未注册：{typeof(T)}")).Scale(value, factor);
	}

	public static object Scale(object value, double factor)
	{
		Type type = value.GetType();
		if (!_Scalers.TryGetValue(type, out Func<object, double, object> value2))
		{
			return value;
		}
		return value2(value, factor);
	}

	public static T DefaultValue<T>()
	{
		return (Cache<T>.Processor ?? throw new InvalidOperationException($"类型未注册：{typeof(T)}")).DefaultValue();
	}

	public static bool Equal<T>(T value1, T value2)
	{
		return (Cache<T>.Processor ?? throw new InvalidOperationException($"类型未注册：{typeof(T)}")).Equal(value1, value2);
	}
}
