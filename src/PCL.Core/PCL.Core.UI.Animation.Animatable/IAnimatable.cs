namespace PCL.Core.UI.Animation.Animatable;

public interface IAnimatable
{
	object? GetValue();

	void SetValue(object value);

	void SetValue<T>(T value);
}
