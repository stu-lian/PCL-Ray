using System;
using System.Numerics;
using System.Windows.Media;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.UIAccessProvider;

namespace PCL.Core.UI;

public struct NRotateTransform : IEquatable<NRotateTransform>, IAdditionOperators<NRotateTransform, NRotateTransform, NRotateTransform>, ISubtractionOperators<NRotateTransform, NRotateTransform, NRotateTransform>, IMultiplyOperators<NRotateTransform, float, NRotateTransform>, IDivisionOperators<NRotateTransform, float, NRotateTransform>
{
	private Vector3 _rotate;

	public float Angle
	{
		get
		{
			return _rotate.X;
		}
		set
		{
			_rotate.X = value;
		}
	}

	public float CenterX
	{
		get
		{
			return _rotate.Y;
		}
		set
		{
			_rotate.Y = value;
		}
	}

	public float CenterY
	{
		get
		{
			return _rotate.Z;
		}
		set
		{
			_rotate.Z = value;
		}
	}

	public NRotateTransform()
	{
		_rotate = new Vector3(0f, 0f, 0f);
	}

	public NRotateTransform(float angle, float centerX = 0f, float centerY = 0f)
	{
		_rotate = new Vector3(angle, centerX, centerY);
	}

	public NRotateTransform(RotateTransform scaleTransform)
	{
		IUIAccessProvider uIAccessProvider = AnimationService.UIAccessProvider;
		if (uIAccessProvider.CheckAccess())
		{
			_rotate = GetVector(scaleTransform);
			return;
		}
		Vector3 localScale = default;
		uIAccessProvider.Invoke(() =>
		{
			localScale = GetVector(scaleTransform);
		});
		_rotate = localScale;
		static Vector3 GetVector(RotateTransform rt)
		{
			return new Vector3((float)rt.Angle, (float)rt.CenterX, (float)rt.CenterY);
		}
	}

	public static NRotateTransform operator +(NRotateTransform a, NRotateTransform b)
	{
		return new NRotateTransform(a.Angle + b.Angle, a.CenterX + b.CenterX, a.CenterY + b.CenterY);
	}

	public static NRotateTransform operator -(NRotateTransform a, NRotateTransform b)
	{
		return new NRotateTransform(a.Angle - b.Angle, a.CenterX - b.CenterX, a.CenterY - b.CenterY);
	}

	public static NRotateTransform operator *(NRotateTransform a, float b)
	{
		return new NRotateTransform(a.Angle * b, a.CenterX * b, a.CenterY * b);
	}

	public static NRotateTransform operator /(NRotateTransform a, float b)
	{
		if (b != 0f)
		{
			return new NRotateTransform(a.Angle / b, a.CenterX / b, a.CenterY / b);
		}
		throw new DivideByZeroException("除数不能为零。");
	}

	public static bool operator ==(NRotateTransform a, NRotateTransform b)
	{
		return a._rotate == b._rotate;
	}

	public static bool operator !=(NRotateTransform a, NRotateTransform b)
	{
		return a._rotate != b._rotate;
	}

	public bool Equals(NRotateTransform other)
	{
		return _rotate.Equals(other._rotate);
	}

	public override bool Equals(object? obj)
	{
		if (obj is NRotateTransform other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Angle, CenterX, CenterY);
	}

	public static implicit operator RotateTransform(NRotateTransform rt)
	{
		return new RotateTransform(rt.Angle, rt.CenterX, rt.CenterY);
	}

	public static implicit operator NRotateTransform(RotateTransform rt)
	{
		return new NRotateTransform(rt);
	}
}
