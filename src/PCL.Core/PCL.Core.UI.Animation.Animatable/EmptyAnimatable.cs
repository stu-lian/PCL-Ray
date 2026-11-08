namespace PCL.Core.UI.Animation.Animatable;

public sealed class EmptyAnimatable : IAnimatable
{
	public static EmptyAnimatable Instance { get; } = new EmptyAnimatable();

	private EmptyAnimatable()
	{
	}

	public object? GetValue()
	{
		return null;
	}

	public void SetValue(object value)
	{
	}

	public void SetValue<T>(T value)
	{
	}
}
