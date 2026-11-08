using System;
using System.Windows.Media;
using Wacton.Unicolour;

namespace PCL.Core.UI.Theme;

public class LabColor
{
	public enum ScRgbMappingMode
	{
		Disable = -1,
		Clip,
		ReduceChroma,
		ReducePurity
	}

	private bool _isScRgbInitialized;

	internal Unicolour UnicolourInstance { get; }

	private Oklab _Oklab => field ?? (field = UnicolourInstance.Oklab);

	public double L => _Oklab.L;

	public double A => _Oklab.A;

	public double B => _Oklab.B;

	public double Alpha { get; }

	public static ScRgbMappingMode ScRgbMapping { get; set; } = ScRgbMappingMode.ReduceChroma;

	public (float Alpha, float Red, float Green, float Blue) ScRgb
	{
		get
		{
			if (_isScRgbInitialized)
			{
				return field;
			}
			_isScRgbInitialized = true;
			return field = ToScRgb();
		}
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(L, A, B, Alpha);
	}

	public override bool Equals(object? obj)
	{
		if (obj is LabColor labColor && labColor.L == L && labColor.A == A && labColor.B == B)
		{
			return labColor.Alpha == Alpha;
		}
		return false;
	}

	private LabColor(double l, double a, double b, double alpha)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected Obj, but got Unknown
		Alpha = alpha;
		UnicolourInstance = new Unicolour((ColourSpace)29, l, a, b, alpha);
	}

	private LabColor(Unicolour instance)
	{
		Alpha = instance.Alpha.A;
		UnicolourInstance = instance;
	}

	internal static LabColor CreateInternal(Unicolour instance)
	{
		return new LabColor(instance);
	}

	public static LabColor Create(double l, double a, double b, double alpha = 1.0)
	{
		return new LabColor(l, a, b, alpha);
	}

	public static LabColor FromLch(double l, double c = 0.0, double h = 0.0, double alpha = 1.0)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		return CreateInternal(new Unicolour((ColourSpace)30, l, c, h, alpha));
	}

	public static LabColor FromRgb(byte r, byte g, byte b, double alpha = 1.0)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		return CreateInternal(new Unicolour((ColourSpace)1, (double)(int)r, (double)(int)g, (double)(int)b, alpha));
	}

	public static LabColor FromRgb(double r, double g, double b, double alpha = 1.0)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected Obj, but got Unknown
		return CreateInternal(new Unicolour((ColourSpace)2, r, g, b, alpha));
	}

	public static LabColor FromWpfColor(Color color)
	{
		return FromRgb(color.ScR, color.ScG, color.ScB, color.ScA);
	}

	public static implicit operator LabColor(Color color)
	{
		return FromWpfColor(color);
	}

	public (float Alpha, float Red, float Green, float Blue) ToScRgb()
	{
		double a = UnicolourInstance.Alpha.A;
		double num = default;
		double num2 = default;
		double num3 = default;
		((ColourRepresentation)((Unicolour)((ScRgbMapping == ScRgbMappingMode.Disable) ? ((object)UnicolourInstance) : ((object)UnicolourInstance.MapToRgbGamut((GamutMap)ScRgbMapping)))).RgbLinear).Deconstruct(out num, out num2, out num3);
		double num4 = num;
		double num5 = num2;
		double num6 = num3;
		return (Alpha: (float)a, Red: (float)num4, Green: (float)num5, Blue: (float)num6);
	}

	public Color ToWpfColor()
	{
		var (a, r, g, b) = ScRgb;
		return Color.FromScRgb(a, r, g, b);
	}

	public static implicit operator Color(LabColor lab)
	{
		return lab.ToWpfColor();
	}

	public CatColorResource ToCatColor(string suffix, bool applyToColor = true, bool applyToBrush = true)
	{
		var (alpha, red, green, blue) = ScRgb;
		return new CatColorResource
		{
			Red = red,
			Green = green,
			Blue = blue,
			Alpha = alpha,
			ApplyToBrush = applyToBrush,
			ApplyToColor = applyToColor,
			Suffix = suffix
		};
	}

	internal static Unicolour MixInternal(LabColor c1, LabColor c2, double c1Amount = 0.5)
	{
		return c1.UnicolourInstance.Mix(c2.UnicolourInstance, (ColourSpace)29, c1Amount, (HueSpan)0, true);
	}

	public static LabColor Mix(LabColor c1, LabColor c2, double c1Amount = 0.5)
	{
		return CreateInternal(MixInternal(c1, c2, c1Amount));
	}

	public static LabColor operator +(LabColor c1, LabColor c2)
	{
		return Mix(c1, c2);
	}
}
