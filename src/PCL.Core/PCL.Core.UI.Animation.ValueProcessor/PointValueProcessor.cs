using System.Windows;

namespace PCL.Core.UI.Animation.ValueProcessor;

public class PointValueProcessor : IValueProcessor<Point>
{
	public Point Filter(Point value)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return value;
	}

	public Point Add(Point value1, Point value2)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		return new Point(value1.X + value2.X, value1.Y + value2.Y);
	}

	public Point Subtract(Point value1, Point value2)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		return new Point(value1.X - value2.X, value1.Y - value2.Y);
	}

	public Point Scale(Point value, double factor)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		return new Point(value.X * factor, value.Y * factor);
	}

	public Point DefaultValue()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return default;
	}

	public bool Equal(Point value1, Point value2)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return value1 == value2;
	}
}
