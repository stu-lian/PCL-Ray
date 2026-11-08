namespace PCL.Core.UI.Animation.ValueProcessor;

public class NColorValueProcessor : IValueProcessor<NColor>
{
	public NColor Filter(NColor value)
	{
		if (value.A < 0f)
		{
			value.A = 0f;
		}
		if (value.R < 0f)
		{
			value.R = 0f;
		}
		if (value.G < 0f)
		{
			value.G = 0f;
		}
		if (value.B < 0f)
		{
			value.B = 0f;
		}
		return value;
	}

	public NColor Add(NColor value1, NColor value2)
	{
		return value1 + value2;
	}

	public NColor Subtract(NColor value1, NColor value2)
	{
		return value1 - value2;
	}

	public NColor Scale(NColor value, double factor)
	{
		return value * (float)factor;
	}

	public NColor DefaultValue()
	{
		return new NColor();
	}

	public bool Equal(NColor value1, NColor value2)
	{
		return value1 == value2;
	}
}
