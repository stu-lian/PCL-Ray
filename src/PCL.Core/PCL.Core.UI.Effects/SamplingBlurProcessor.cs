using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace PCL.Core.UI.Effects;

internal sealed class SamplingBlurProcessor : IDisposable
{
	private struct CachedBlurResult
	{
		public WriteableBitmap Bitmap;

		public DateTime LastUsed;

		public string Key;
	}

	private static readonly ArrayPool<uint> _UintPool = ArrayPool<uint>.Create();

	private static readonly ArrayPool<float> _FloatPool = ArrayPool<float>.Create();

	private static readonly ConcurrentDictionary<string, CachedBlurResult> _Cache = new ConcurrentDictionary<string, CachedBlurResult>();

	private readonly object _lockObject = new object();

	private bool _disposed;

	private static readonly Vector2[] _PoissonSamples = _GeneratePoissonDiskSamples();

	private static readonly float[] _GaussianWeights = _GenerateGaussianWeights();

	public void InvalidateCache()
	{
		lock (_lockObject)
		{
			_Cache.Clear();
		}
	}

	public WriteableBitmap? ApplySamplingBlur(BitmapSource? source, double radius, double samplingRate, RenderingBias renderingBias, KernelType kernelType)
	{
		if (source == null || radius <= 0.0)
		{
			return null;
		}
		string key = _GenerateCacheKey(source, radius, samplingRate, renderingBias, kernelType);
		lock (_lockObject)
		{
			if (_Cache.TryGetValue(key, out var value))
			{
				value.LastUsed = DateTime.UtcNow;
				_Cache[key] = value;
				return value.Bitmap;
			}
		}
		WriteableBitmap writeableBitmap = _ProcessBlur(source, radius, samplingRate, renderingBias, kernelType);
		lock (_lockObject)
		{
			_Cache[key] = new CachedBlurResult
			{
				Bitmap = writeableBitmap,
				LastUsed = DateTime.UtcNow,
				Key = key
			};
			if (_Cache.Count > 50)
			{
				_CleanExpiredCache();
			}
		}
		return writeableBitmap;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private unsafe WriteableBitmap _ProcessBlur(BitmapSource source, double radius, double samplingRate, RenderingBias renderingBias, KernelType kernelType)
	{
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		int pixelWidth = source.PixelWidth;
		int pixelHeight = source.PixelHeight;
		int num = (pixelWidth * source.Format.BitsPerPixel + 7) / 8;
		uint[] array = _UintPool.Rent(pixelWidth * pixelHeight);
		uint[] array2 = _UintPool.Rent(pixelWidth * pixelHeight);
		try
		{
			byte[] array3 = new byte[num * pixelHeight];
			source.CopyPixels(array3, num, 0);
			_CopyBytesToUints(array3, array, pixelWidth * pixelHeight);
			if (renderingBias == RenderingBias.Quality)
			{
				_ApplyQualityBlur(array, array2, pixelWidth, pixelHeight, radius, samplingRate, kernelType);
			}
			else
			{
				_ApplyPerformanceBlur(array, array2, pixelWidth, pixelHeight, radius, samplingRate, kernelType);
			}
			WriteableBitmap writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgra32, null);
			writeableBitmap.Lock();
			try
			{
				uint* backBuffer = (uint*)writeableBitmap.BackBuffer;
				fixed (uint* source2 = array2)
				{
					Buffer.MemoryCopy(source2, backBuffer, pixelWidth * pixelHeight * 4, pixelWidth * pixelHeight * 4);
				}
				writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
			}
			finally
			{
				writeableBitmap.Unlock();
			}
			return writeableBitmap;
		}
		finally
		{
			_UintPool.Return(array);
			_UintPool.Return(array2);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private void _ApplyQualityBlur(uint[] source, uint[] target, int width, int height, double radius, double samplingRate, KernelType kernelType)
	{
		int intRadius = (int)Math.Ceiling(radius);
		double num = radius / 3.0;
		double twoSigmaSquared = 2.0 * num * num;
		Parallel.For(0, height, (int y) =>
		{
			for (int i = 0; i < width; i++)
			{
				var (a, r, g, b) = _SamplePixelQuality(source, width, height, i, y, intRadius, twoSigmaSquared, samplingRate, kernelType);
				target[y * width + i] = _PackColor(a, r, g, b);
			}
		});
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private void _ApplyPerformanceBlur(uint[] source, uint[] target, int width, int height, double radius, double samplingRate, KernelType kernelType)
	{
		int radius2 = (int)Math.Ceiling(radius * samplingRate);
		uint[] array = _UintPool.Rent(width * height);
		try
		{
			_ApplySeparableBlurHorizontal(source, array, width, height, radius2, samplingRate, kernelType);
			_ApplySeparableBlurVertical(array, target, width, height, radius2, samplingRate, kernelType);
		}
		finally
		{
			_UintPool.Return(array);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private void _ApplySeparableBlurHorizontal(uint[] source, uint[] target, int width, int height, int radius, double samplingRate, KernelType kernelType)
	{
		double[] weights = _GenerateGaussianKernel(radius);
		int kernelRadius = weights.Length / 2;
		Parallel.For(0, height, (int y) =>
		{
			int num = y * width;
			for (int i = 0; i < width; i++)
			{
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				double num6 = 0.0;
				int num7 = ((samplingRate >= 0.8) ? 1 : ((int)Math.Ceiling(2.0 - samplingRate)));
				for (int j = -kernelRadius; j <= kernelRadius; j += num7)
				{
					int num8 = Math.Max(0, Math.Min(width - 1, i + j));
					uint num9 = source[num + num8];
					double num10 = weights[Math.Abs(j) + kernelRadius];
					num2 += (double)((num9 >> 24) & 0xFF) * num10;
					num3 += (double)((num9 >> 16) & 0xFF) * num10;
					num4 += (double)((num9 >> 8) & 0xFF) * num10;
					num5 += (double)(num9 & 0xFF) * num10;
					num6 += num10;
				}
				if (num6 > 0.0)
				{
					double num11 = 1.0 / num6;
					target[num + i] = _PackColor((byte)Math.Min(255.0, num2 * num11), (byte)Math.Min(255.0, num3 * num11), (byte)Math.Min(255.0, num4 * num11), (byte)Math.Min(255.0, num5 * num11));
				}
				else
				{
					target[num + i] = source[num + i];
				}
			}
		});
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private void _ApplySeparableBlurVertical(uint[] source, uint[] target, int width, int height, int radius, double samplingRate, KernelType kernelType)
	{
		double[] weights = _GenerateGaussianKernel(radius);
		int kernelRadius = weights.Length / 2;
		Parallel.For(0, width, (int x) =>
		{
			for (int i = 0; i < height; i++)
			{
				double num = 0.0;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				int num6 = ((samplingRate >= 0.8) ? 1 : ((int)Math.Ceiling(2.0 - samplingRate)));
				for (int j = -kernelRadius; j <= kernelRadius; j += num6)
				{
					int num7 = Math.Max(0, Math.Min(height - 1, i + j));
					uint num8 = source[num7 * width + x];
					double num9 = weights[Math.Abs(j) + kernelRadius];
					num += (double)((num8 >> 24) & 0xFF) * num9;
					num2 += (double)((num8 >> 16) & 0xFF) * num9;
					num3 += (double)((num8 >> 8) & 0xFF) * num9;
					num4 += (double)(num8 & 0xFF) * num9;
					num5 += num9;
				}
				if (num5 > 0.0)
				{
					double num10 = 1.0 / num5;
					target[i * width + x] = _PackColor((byte)Math.Min(255.0, num * num10), (byte)Math.Min(255.0, num2 * num10), (byte)Math.Min(255.0, num3 * num10), (byte)Math.Min(255.0, num4 * num10));
				}
				else
				{
					target[i * width + x] = source[i * width + x];
				}
			}
		});
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static double[] _GenerateGaussianKernel(int radius)
	{
		int num = radius * 2 + 1;
		double[] array = new double[num];
		double num2 = (double)radius / 3.0;
		double num3 = 2.0 * num2 * num2;
		double num4 = 1.0 / Math.Sqrt(Math.PI * num3);
		double num5 = 0.0;
		for (int i = 0; i < num; i++)
		{
			int num6 = i - radius;
			num5 += (array[i] = num4 * Math.Exp((double)(-(num6 * num6)) / num3));
		}
		if (num5 > 0.0)
		{
			double num7 = 1.0 / num5;
			for (int j = 0; j < num; j++)
			{
				array[j] *= num7;
			}
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private (byte a, byte r, byte g, byte b) _SamplePixelQuality(uint[] source, int width, int height, int centerX, int centerY, int radius, double twoSigmaSquared, double samplingRate, KernelType kernelType)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		double num5 = 0.0;
		int num6 = ((kernelType == KernelType.Gaussian) ? Math.Min(_PoissonSamples.Length, (int)(32.0 * samplingRate)) : Math.Min(16, (int)(16.0 * samplingRate)));
		for (int i = 0; i < num6; i++)
		{
			Vector2 vector = _PoissonSamples[i % _PoissonSamples.Length] * radius;
			int num7 = centerX + (int)Math.Round(vector.X);
			int num8 = centerY + (int)Math.Round(vector.Y);
			if (num7 >= 0 && num7 < width && num8 >= 0 && num8 < height)
			{
				uint num9 = source[num8 * width + num7];
				float num10 = vector.Length();
				double num11 = ((kernelType == KernelType.Gaussian) ? Math.Exp((double)((0f - num10) * num10) / twoSigmaSquared) : Math.Max(0.0, 1.0 - (double)(num10 / (float)radius)));
				num += (double)((num9 >> 24) & 0xFF) * num11;
				num2 += (double)((num9 >> 16) & 0xFF) * num11;
				num3 += (double)((num9 >> 8) & 0xFF) * num11;
				num4 += (double)(num9 & 0xFF) * num11;
				num5 += num11;
			}
		}
		if (num5 > 0.0)
		{
			double num12 = 1.0 / num5;
			return (a: (byte)Math.Min(255.0, num * num12), r: (byte)Math.Min(255.0, num2 * num12), g: (byte)Math.Min(255.0, num3 * num12), b: (byte)Math.Min(255.0, num4 * num12));
		}
		uint num13 = source[centerY * width + centerX];
		return (a: (byte)((num13 >> 24) & 0xFF), r: (byte)((num13 >> 16) & 0xFF), g: (byte)((num13 >> 8) & 0xFF), b: (byte)(num13 & 0xFF));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private (byte a, byte r, byte g, byte b) _SamplePixelPerformance(uint[] source, int width, int height, int centerX, int centerY, int radius, double samplingRate, KernelType kernelType)
	{
		int val = Math.Max(4, (int)(8.0 * samplingRate));
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		int num5 = 0;
		int num6 = Math.Min(val, _PoissonSamples.Length);
		for (int i = 0; i < num6; i++)
		{
			Vector2 vector = _PoissonSamples[i] * radius;
			int num7 = centerX + (int)Math.Round(vector.X);
			int num8 = centerY + (int)Math.Round(vector.Y);
			if (num7 >= 0 && num7 < width && num8 >= 0 && num8 < height)
			{
				uint num9 = source[num8 * width + num7];
				float num10 = vector.Length();
				double num11 = Math.Exp((double)((0f - num10) * num10) / (2.0 * (double)radius * (double)radius * 0.25));
				num += (double)((num9 >> 24) & 0xFF) * num11;
				num2 += (double)((num9 >> 16) & 0xFF) * num11;
				num3 += (double)((num9 >> 8) & 0xFF) * num11;
				num4 += (double)(num9 & 0xFF) * num11;
				num5++;
			}
		}
		if (num5 > 0)
		{
			double num12 = 1.0 / (double)num5;
			return (a: (byte)Math.Min(255.0, num * num12), r: (byte)Math.Min(255.0, num2 * num12), g: (byte)Math.Min(255.0, num3 * num12), b: (byte)Math.Min(255.0, num4 * num12));
		}
		uint num13 = source[centerY * width + centerX];
		return (a: (byte)((num13 >> 24) & 0xFF), r: (byte)((num13 >> 16) & 0xFF), g: (byte)((num13 >> 8) & 0xFF), b: (byte)(num13 & 0xFF));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint _PackColor(byte a, byte r, byte g, byte b)
	{
		return (uint)((a << 24) | (r << 16) | (g << 8) | b);
	}

	private static void _CopyBytesToUints(byte[] source, uint[] target, int count)
	{
		for (int i = 0; i < count; i++)
		{
			int num = i * 4;
			if (num + 3 < source.Length)
			{
				target[i] = (uint)((source[num + 3] << 24) | (source[num + 2] << 16) | (source[num + 1] << 8) | source[num]);
			}
		}
	}

	private static Vector2[] _GeneratePoissonDiskSamples()
	{
		Vector2[] array = new Vector2[32];
		Random random = new Random(42);
		int num = 0;
		int num2 = 0;
		while (num2 < 32 && num < 1000)
		{
			Vector2 vector = new Vector2((float)(random.NextDouble() * 2.0 - 1.0), (float)(random.NextDouble() * 2.0 - 1.0));
			if (vector.LengthSquared() > 1f)
			{
				num++;
				continue;
			}
			bool flag = true;
			for (int i = 0; i < num2; i++)
			{
				if (Vector2.DistanceSquared(vector, array[i]) < 0.48999998f)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				array[num2++] = vector;
			}
			num++;
		}
		while (num2 < 32)
		{
			double num3 = Math.PI * 2.0 * (double)num2 / 32.0;
			float num4 = 0.8f + 0.2f * (float)(num2 % 3) / 3f;
			array[num2++] = new Vector2((float)(Math.Cos(num3) * (double)num4), (float)(Math.Sin(num3) * (double)num4));
		}
		return array;
	}

	private static float[] _GenerateGaussianWeights()
	{
		float[] array = new float[33];
		float num = 5.5f;
		float num2 = 2f * num * num;
		float num3 = 1f / (float)Math.Sqrt(Math.PI * (double)num2);
		float num4 = 0f;
		for (int i = 0; i < 33; i++)
		{
			int num5 = i - 16;
			num4 += (array[i] = num3 * (float)Math.Exp((float)(-(num5 * num5)) / num2));
		}
		if (num4 > 0f)
		{
			float num6 = 1f / num4;
			for (int j = 0; j < 33; j++)
			{
				array[j] *= num6;
			}
		}
		return array;
	}

	private static string _GenerateCacheKey(BitmapSource source, double radius, double samplingRate, RenderingBias renderingBias, KernelType kernelType)
	{
		return $"{((object)source).GetHashCode()}_{radius:F1}_{samplingRate:F2}_{renderingBias}_{kernelType}";
	}

	private void _CleanExpiredCache()
	{
		DateTime cutoff = DateTime.UtcNow.AddMinutes(-5.0);
		foreach (string item in _Cache.Where((KeyValuePair<string, CachedBlurResult> kvp) =>
		{
			KeyValuePair<string, CachedBlurResult> keyValuePair = kvp;
			return keyValuePair.Value.LastUsed < cutoff;
		}).Select((KeyValuePair<string, CachedBlurResult> kvp) =>
		{
			KeyValuePair<string, CachedBlurResult> keyValuePair = kvp;
			return keyValuePair.Key;
		}).ToList())
		{
			_Cache.TryRemove(item, out var _);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_Cache.Clear();
			_disposed = true;
		}
		GC.SuppressFinalize(this);
	}

	~SamplingBlurProcessor()
	{
		Dispose();
	}
}
