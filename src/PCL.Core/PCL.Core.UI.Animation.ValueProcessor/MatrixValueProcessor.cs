using System.Windows.Media;

namespace PCL.Core.UI.Animation.ValueProcessor;

public class MatrixValueProcessor : IValueProcessor<Matrix>
{
	public Matrix Filter(Matrix value)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return value;
	}

	public Matrix Add(Matrix value1, Matrix value2)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		return new Matrix(value1.M11 + value2.M11, value1.M12 + value2.M12, value1.M21 + value2.M21, value1.M22 + value2.M22, value1.OffsetX + value2.OffsetX, value1.OffsetY + value2.OffsetY);
	}

	public Matrix Subtract(Matrix value1, Matrix value2)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		return new Matrix(value1.M11 - value2.M11, value1.M12 - value2.M12, value1.M21 - value2.M21, value1.M22 - value2.M22, value1.OffsetX - value2.OffsetX, value1.OffsetY - value2.OffsetY);
	}

	public Matrix Scale(Matrix value, double factor)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		return new Matrix(value.M11 * factor, value.M12 * factor, value.M21 * factor, value.M22 * factor, value.OffsetX * factor, value.OffsetY * factor);
	}

	public Matrix DefaultValue()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return default;
	}

	public bool Equal(Matrix value1, Matrix value2)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return value1 == value2;
	}
}
