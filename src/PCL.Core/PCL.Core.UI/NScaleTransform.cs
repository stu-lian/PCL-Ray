using System;
using System.Numerics;
using System.Windows.Media;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.UIAccessProvider;

namespace PCL.Core.UI;

public struct NScaleTransform : IEquatable<NScaleTransform>, IAdditionOperators<NScaleTransform, NScaleTransform, NScaleTransform>, ISubtractionOperators<NScaleTransform, NScaleTransform, NScaleTransform>, IMultiplyOperators<NScaleTransform, float, NScaleTransform>, IDivisionOperators<NScaleTransform, float, NScaleTransform>
{
	private Vector4 _scale;

	public float ScaleX
	{
		get
		{
			return _scale.X;
		}
		set
		{
			_scale.X = value;
		}
	}

	public float ScaleY
	{
		get
		{
			return _scale.Y;
		}
		set
		{
			_scale.Y = value;
		}
	}

	public float CenterX
	{
		get
		{
			return _scale.Z;
		}
		set
		{
			_scale.Z = value;
		}
	}

	public float CenterY
	{
		get
		{
			return _scale.W;
		}
		set
		{
			_scale.W = value;
		}
	}

	public NScaleTransform()
	{
		_scale = new Vector4(1f, 1f, 0f, 0f);
	}

	public NScaleTransform(float scaleX, float scaleY, float centerX = 0f, float centerY = 0f)
	{
		_scale = new Vector4(scaleX, scaleY, centerX, centerY);
	}

	public NScaleTransform(ScaleTransform scaleTransform)
	{
		IUIAccessProvider uIAccessProvider = AnimationService.UIAccessProvider;
		if (uIAccessProvider.CheckAccess())
		{
			_scale = GetVector(scaleTransform);
			return;
		}
		Vector4 localScale = default;
		uIAccessProvider.Invoke(() =>
		{
			localScale = GetVector(scaleTransform);
		});
		_scale = localScale;
		static Vector4 GetVector(ScaleTransform st)
		{
			return new Vector4((float)st.ScaleX, (float)st.ScaleY, (float)st.CenterX, (float)st.CenterY);
		}
	}

	public static NScaleTransform operator +(NScaleTransform a, NScaleTransform b)
	{
		return new NScaleTransform(a.ScaleX + b.ScaleX, a.ScaleY + b.ScaleY, a.CenterX + b.CenterX, a.CenterY + b.CenterY);
	}

	public static NScaleTransform operator -(NScaleTransform a, NScaleTransform b)
	{
		return new NScaleTransform(a.ScaleX - b.ScaleX, a.ScaleY - b.ScaleY, a.CenterX - b.CenterX, a.CenterY - b.CenterY);
	}

	public static NScaleTransform operator *(NScaleTransform a, float b)
	{
		return new NScaleTransform(a.ScaleX * b, a.ScaleY * b, a.CenterX * b, a.CenterY * b);
	}

	public static NScaleTransform operator /(NScaleTransform a, float b)
	{
		if (b != 0f)
		{
			return new NScaleTransform(a.ScaleX / b, a.ScaleY / b, a.CenterX / b, a.CenterY / b);
		}
		throw new DivideByZeroException("除数不能为零。");
	}

	public static bool operator ==(NScaleTransform a, NScaleTransform b)
	{
		return a._scale == b._scale;
	}

	public static bool operator !=(NScaleTransform a, NScaleTransform b)
	{
		return a._scale != b._scale;
	}

	public bool Equals(NScaleTransform other)
	{
		return _scale.Equals(other._scale);
	}

	public override bool Equals(object? obj)
	{
		if (obj is NScaleTransform other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(ScaleX, ScaleY, CenterX, CenterY);
	}

	public static implicit operator ScaleTransform(NScaleTransform st)
	{
		return new ScaleTransform(st.ScaleX, st.ScaleY, st.CenterX, st.CenterY);
	}

	public static implicit operator NScaleTransform(ScaleTransform st)
	{
		return new NScaleTransform(st);
	}
}
