using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PCL.Core.UI.Effects;

internal static class PerformanceOptimizations
{
	private static readonly ArrayPool<Vector4> _VectorPool = ArrayPool<Vector4>.Create();

	private static readonly ArrayPool<float> _FloatPool = ArrayPool<float>.Create();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4[] RentVectorArray(int size)
	{
		return _VectorPool.Rent(size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ReturnVectorArray(Vector4[] array)
	{
		_VectorPool.Return(array);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float[] RentFloatArray(int size)
	{
		return _FloatPool.Rent(size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ReturnFloatArray(float[] array)
	{
		_FloatPool.Return(array);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static void FastGaussianBlur(ReadOnlySpan<float> input, Span<float> output, ReadOnlySpan<float> weights, int width, int height, float radius, float samplingRate)
	{
		if (!Vector.IsHardwareAccelerated || input.Length != output.Length)
		{
			_FallbackBlur(input, output, weights, width, height, radius, samplingRate);
			return;
		}
		int count = Vector<float>.Count;
		int num = weights.Length / 2;
		for (int i = 0; i < height; i++)
		{
			int num2 = i * width;
			int num3 = Math.Min(num2 + width, input.Length);
			int num4 = num3 - num2 - (num3 - num2) % count;
			for (int j = 0; j < num4; j += count)
			{
				int num5 = num2 + j;
				Vector<float> zero = Vector<float>.Zero;
				float num6 = 0f;
				for (int k = 0; k < weights.Length; k++)
				{
					int num7 = k - num;
					int start = Math.Max(0, Math.Min(input.Length - count, num5 + num7));
					Vector<float> vector = new Vector<float>(input.Slice(start, count));
					float num8 = weights[k] * samplingRate;
					zero += vector * new Vector<float>(num8);
					num6 += num8;
				}
				if (num6 > 0f)
				{
					zero /= new Vector<float>(num6);
					if (samplingRate < 0.8f)
					{
						Vector<float> vector2 = new Vector<float>(input.Slice(num5, count)) - zero;
						float value = (0.8f - samplingRate) * 0.1f;
						zero += vector2 * new Vector<float>(value);
					}
				}
				zero.CopyTo(output.Slice(num5, count));
			}
			for (int l = num4; l < num3 - num2; l++)
			{
				int num9 = num2 + l;
				output[num9] = _ProcessPixelBlur(input[num9], weights, samplingRate, input, num9, width, height);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector<float> _ProcessVectorizedBlur(Vector<float> input, ReadOnlySpan<float> weights, float samplingRate)
	{
		int num = Math.Min(weights.Length, Vector<float>.Count);
		Vector<float> zero = Vector<float>.Zero;
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			float num3 = weights[i] * samplingRate;
			zero += input * new Vector<float>(num3);
			num2 += num3;
		}
		if (num2 > 0f)
		{
			zero /= new Vector<float>(num2);
		}
		return zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float _ProcessPixelBlur(float centerPixel, ReadOnlySpan<float> weights, float samplingRate, ReadOnlySpan<float> imageData, int centerIndex, int width, int height)
	{
		float num = 0f;
		float num2 = 0f;
		int num3 = weights.Length / 2;
		int num4 = centerIndex / width;
		int num5 = centerIndex % width;
		for (int i = -num3; i <= num3; i++)
		{
			for (int j = -num3; j <= num3; j++)
			{
				int num6 = Math.Max(0, Math.Min(height - 1, num4 + i));
				int num7 = Math.Max(0, Math.Min(width - 1, num5 + j));
				int num8 = num6 * width + num7;
				if (num8 >= 0 && num8 < imageData.Length)
				{
					int index = Math.Min(weights.Length - 1, Math.Abs(i) + Math.Abs(j));
					float num9 = weights[index] * samplingRate;
					num += imageData[num8] * num9;
					num2 += num9;
				}
			}
		}
		if (num2 > 0f)
		{
			num /= num2;
			if (samplingRate < 0.8f)
			{
				float num10 = centerPixel - num;
				float num11 = (0.8f - samplingRate) * 0.15f;
				num += num10 * num11;
			}
		}
		else
		{
			num = centerPixel;
		}
		return num;
	}

	private static void _FallbackBlur(ReadOnlySpan<float> input, Span<float> output, ReadOnlySpan<float> weights, int width, int height, float radius, float samplingRate)
	{
		for (int i = 0; i < input.Length; i++)
		{
			output[i] = _ProcessPixelBlur(input[i], weights, samplingRate, input, i, width, height);
		}
	}
}
