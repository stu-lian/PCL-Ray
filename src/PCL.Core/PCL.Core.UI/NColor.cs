using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Media;
using PCL.Core.App.IoC;

namespace PCL.Core.UI;

public struct NColor : IEquatable<NColor>, IAdditionOperators<NColor, NColor, NColor>, ISubtractionOperators<NColor, NColor, NColor>, IMultiplyOperators<NColor, float, NColor>, IDivisionOperators<NColor, float, NColor>
{
	private Vector4 _color;

	public float R
	{
		get
		{
			return _color.X;
		}
		set
		{
			_color.X = value;
		}
	}

	public float G
	{
		get
		{
			return _color.Y;
		}
		set
		{
			_color.Y = value;
		}
	}

	public float B
	{
		get
		{
			return _color.Z;
		}
		set
		{
			_color.Z = value;
		}
	}

	public float A
	{
		get
		{
			return _color.W;
		}
		set
		{
			_color.W = value;
		}
	}

	public NColor()
	{
		_color = new Vector4(0f, 0f, 0f, 255f);
	}

	public NColor(float r, float g, float b, float a = 255f)
	{
		_color = new Vector4(r, g, b, a);
	}

	public NColor(System.Windows.Media.Color color)
		: this((int)color.R, (int)color.G, (int)color.B, (int)color.A)
	{
	}

	public NColor(System.Drawing.Color color)
		: this((int)color.R, (int)color.G, (int)color.B, (int)color.A)
	{
	}

	public NColor(string str)
	{
		try
		{
			object obj = Lifecycle.CurrentApplication.FindResource(str);
			if (obj is System.Windows.Media.Color color)
			{
				_color = new Vector4((int)color.R, (int)color.G, (int)color.B, (int)color.A);
				return;
			}
			if (obj is SolidColorBrush { Color: var color2 })
			{
				_color = new Vector4((int)color2.R, (int)color2.G, (int)color2.B, (int)color2.A);
				return;
			}
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(str))
		{
			throw new ArgumentException("颜色字符串不能为空。", "str");
		}
		string text = str.Trim();
		if (!text.StartsWith('#'))
		{
			throw new ArgumentException("颜色字符串必须以 '#' 开头。", "str");
		}
		text = text.Substring(1);
		int num;
		int num2;
		int num3;
		int num4;
		switch (text.Length)
		{
		case 3:
			num = Convert.ToInt32($"{text[0]}{text[0]}", 16);
			num2 = Convert.ToInt32($"{text[1]}{text[1]}", 16);
			num3 = Convert.ToInt32($"{text[2]}{text[2]}", 16);
			num4 = 255;
			break;
		case 4:
			num = Convert.ToInt32($"{text[0]}{text[0]}", 16);
			num2 = Convert.ToInt32($"{text[1]}{text[1]}", 16);
			num3 = Convert.ToInt32($"{text[2]}{text[2]}", 16);
			num4 = Convert.ToInt32($"{text[3]}{text[3]}", 16);
			break;
		case 6:
			num = Convert.ToInt32(text.Substring(0, 2), 16);
			num2 = Convert.ToInt32(text.Substring(2, 2), 16);
			num3 = Convert.ToInt32(text.Substring(4, 2), 16);
			num4 = 255;
			break;
		case 8:
			num = Convert.ToInt32(text.Substring(0, 2), 16);
			num2 = Convert.ToInt32(text.Substring(2, 2), 16);
			num3 = Convert.ToInt32(text.Substring(4, 2), 16);
			num4 = Convert.ToInt32(text.Substring(6, 2), 16);
			break;
		default:
			throw new ArgumentException($"无效的颜色字符串长度：{text.Length}。", "str");
		}
		_color = new Vector4(num, num2, num3, num4);
	}

	public NColor(float a, NColor color)
		: this(color.R, color.G, color.B, a)
	{
	}

	public NColor(float r, float g, float b)
		: this(r, g, b, 255f)
	{
	}

	public NColor(SolidColorBrush brush)
		: this(brush.Color)
	{
	}

	public NColor(System.Windows.Media.Brush brush)
		: this((SolidColorBrush)brush)
	{
	}

	private NColor(Vector4 v)
	{
		_color = v;
	}

	public static NColor operator +(NColor a, NColor b)
	{
		return new NColor(a._color + b._color);
	}

	public static NColor operator -(NColor a, NColor b)
	{
		return new NColor(a._color - b._color);
	}

	public static NColor operator *(NColor a, float b)
	{
		return new NColor(a._color * b);
	}

	public static NColor operator /(NColor a, float b)
	{
		if (b != 0f)
		{
			return new NColor(a._color / b);
		}
		throw new DivideByZeroException("除数不能为零。");
	}

	public static bool operator ==(NColor a, NColor b)
	{
		return a._color == b._color;
	}

	public static bool operator !=(NColor a, NColor b)
	{
		return a._color != b._color;
	}

	public bool Equals(NColor other)
	{
		return _color.Equals(other._color);
	}

	public override bool Equals(object? obj)
	{
		if (obj is NColor other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(R, G, B, A);
	}

	public static NColor FromHsl(double sH, double sS, double sL)
	{
		NColor result = new NColor();
		if (sS == 0.0)
		{
			result.R = (float)(sL * 2.55);
			result.G = result.R;
			result.B = result.R;
		}
		else
		{
			double num = sH / 360.0;
			double num2 = sS / 100.0;
			double num3 = sL / 100.0;
			num2 = ((num3 < 0.5) ? (num2 * num3 + num3) : (num2 * (1.0 - num3) + num3));
			num3 = 2.0 * num3 - num2;
			result.R = (float)(255.0 * _Hue(num3, num2, num + 1.0 / 3.0));
			result.G = (float)(255.0 * _Hue(num3, num2, num));
			result.B = (float)(255.0 * _Hue(num3, num2, num - 1.0 / 3.0));
		}
		result.A = 255f;
		return result;
	}

	private static double _Hue(double v1, double v2, double vH)
	{
		if (vH < 0.0)
		{
			vH++;
		}
		if (vH > 1.0)
		{
			vH--;
		}
		if (!(vH < 0.16667))
		{
			if (!(vH < 0.5))
			{
				if (vH < 0.66667)
				{
					return v1 + (v2 - v1) * (4.0 - vH * 6.0);
				}
				return v1;
			}
			return v2;
		}
		return v1 + (v2 - v1) * 6.0 * vH;
	}

	public static implicit operator System.Windows.Media.Color(NColor color)
	{
		return System.Windows.Media.Color.FromArgb((byte)Math.Clamp(color.A, 0f, 255f), (byte)Math.Clamp(color.R, 0f, 255f), (byte)Math.Clamp(color.G, 0f, 255f), (byte)Math.Clamp(color.B, 0f, 255f));
	}

	public static implicit operator System.Windows.Media.Brush(NColor color)
	{
		return new SolidColorBrush(color);
	}

	public static implicit operator SolidColorBrush(NColor color)
	{
		return new SolidColorBrush(color);
	}

	public static implicit operator NColor(System.Windows.Media.Color color)
	{
		return new NColor(color);
	}

	public static implicit operator NColor(System.Windows.Media.Brush brush)
	{
		return new NColor(brush);
	}

	public static implicit operator NColor(SolidColorBrush brush)
	{
		return new NColor(brush);
	}
}
