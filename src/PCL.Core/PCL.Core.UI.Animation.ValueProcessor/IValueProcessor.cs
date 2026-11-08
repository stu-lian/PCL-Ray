namespace PCL.Core.UI.Animation.ValueProcessor;

public interface IValueProcessor<T>
{
	T Filter(T value);

	T Add(T value1, T value2);

	T Subtract(T value1, T value2);

	T Scale(T value, double factor);

	T DefaultValue();

	bool Equal(T value1, T value2);
}
