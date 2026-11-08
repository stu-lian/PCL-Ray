using System;
using System.Windows.Media.Effects;

namespace PCL.Core.UI.Effects;

public static class OptimizedBlurFactory
{
	public static OptimizedBlurEffect CreateHighPerformance(double radius = 16.0)
	{
		return new OptimizedBlurEffect
		{
			Radius = radius,
			SamplingRate = 0.3,
			RenderingBias = RenderingBias.Performance,
			KernelType = KernelType.Box
		};
	}

	public static OptimizedBlurEffect CreateBalanced(double radius = 16.0)
	{
		return new OptimizedBlurEffect
		{
			Radius = radius,
			SamplingRate = 0.7,
			RenderingBias = RenderingBias.Performance,
			KernelType = KernelType.Gaussian
		};
	}

	public static OptimizedBlurEffect CreateBestQuality(double radius = 16.0)
	{
		return new OptimizedBlurEffect
		{
			Radius = radius,
			SamplingRate = 1.0,
			RenderingBias = RenderingBias.Quality,
			KernelType = KernelType.Gaussian
		};
	}

	public static OptimizedBlurEffect CreateAdaptive(double radius = 16.0)
	{
		double samplingRate = Math.Max(0.3, Math.Min(1.0, 25.0 / radius));
		return new OptimizedBlurEffect
		{
			Radius = radius,
			SamplingRate = samplingRate,
			RenderingBias = ((!(radius > 25.0)) ? RenderingBias.Quality : RenderingBias.Performance),
			KernelType = KernelType.Gaussian
		};
	}

	public static OptimizedBlurEffect CreateRealTimePreview(double radius = 16.0)
	{
		return new OptimizedBlurEffect
		{
			Radius = radius,
			SamplingRate = 0.1,
			RenderingBias = RenderingBias.Performance,
			KernelType = KernelType.Box
		};
	}
}
